using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

public static class HydrationManager
{
    private const string HydrationAttributeKey = "hydration";
    private static List<JObject> _lastAppliedPatches = new List<JObject>();
    private static readonly Dictionary<string, System.Func<ItemStack, float>> CustomHydrationEvaluators = new();

    public static void SetHydration(ICoreAPI api, CollectibleObject collectible, float hydrationValue)
    {
        if (collectible.Attributes == null)
        {
            collectible.Attributes = new JsonObject(new JObject());
        }

        collectible.Attributes.Token[HydrationAttributeKey] = JToken.FromObject(hydrationValue);
    }

    public static float GetHydration(ItemStack itemStack)
    {
        CollectibleObject collectible = itemStack?.Collectible;

        if (collectible == null)
        {
            return 0f;
        }
        string collectibleCode = collectible.Code?.ToString() ?? string.Empty;
        if (CustomHydrationEvaluators.TryGetValue(collectibleCode, out var evaluator))
        {
            return evaluator(itemStack);
        }
        if (collectible.Attributes == null)
        {
            return 0f;
        }
        return collectible.Attributes.Token[HydrationAttributeKey]?.ToObject<float>() ?? 0f;
    }

    public static void RegisterHydrationEvaluator(string collectibleCode, System.Func<ItemStack, float> evaluator)
    {
        if (string.IsNullOrEmpty(collectibleCode) || evaluator == null)
        {
            throw new ArgumentException("Collectible code and evaluator must not be null or empty.");
        }

        CustomHydrationEvaluators[collectibleCode] = evaluator;
    }

    public static void ApplyHydrationPatches(ICoreAPI api, List<JObject> patches)
    {
        foreach (var patch in patches)
        {
            string itemName = patch["itemname"]?.ToString();
            if (string.IsNullOrEmpty(itemName)) continue;

            var matchingCollectibles = GetMatchingCollectibles(api, itemName);
            foreach (var collectible in matchingCollectibles)
            {
                if (patch.ContainsKey(HydrationAttributeKey))
                {
                    float hydrationValue = patch[HydrationAttributeKey].ToObject<float>();
                    SetHydration(api, collectible, hydrationValue);
                }
                else if (patch.ContainsKey("hydrationByType"))
                {
                    var hydrationByType = patch["hydrationByType"].ToObject<Dictionary<string, float>>();
                    foreach (var entry in hydrationByType)
                    {
                        string typeKey = entry.Key;
                        float hydrationValue = entry.Value;

                        if (IsMatch(collectible.Code.ToString(), typeKey))
                        {
                            SetHydration(api, collectible, hydrationValue);
                        }
                    }
                }
            }
        }
        _lastAppliedPatches = patches;
    }
    public static List<JObject> GetLastAppliedPatches()
    {
        return _lastAppliedPatches;
    }

    private static List<CollectibleObject> GetMatchingCollectibles(ICoreAPI api, string pattern)
    {
        var matchingCollectibles = new List<CollectibleObject>();
        string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new Regex(regexPattern);

        foreach (var collectible in api.World.Collectibles)
        {
            if (collectible.Code != null && regex.IsMatch(collectible.Code.ToString()))
            {
                matchingCollectibles.Add(collectible);
            }
        }

        return matchingCollectibles;
    }
    private static bool IsMatch(string itemName, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return false;

        if (pattern.Contains("*"))
        {
            string regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            return Regex.IsMatch(itemName, regexPattern);
        }
        return itemName == pattern;
    }
}
