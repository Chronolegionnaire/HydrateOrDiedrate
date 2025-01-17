using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

public static class CoolingManager
{
    public const string CoolingAttributeKey = "cooling";
    public static List<JObject> _lastAppliedPatches = new List<JObject>();

    public static void SetCooling(ICoreAPI api, CollectibleObject collectible, float coolingValue)
    {
        if (collectible.Attributes == null)
        {
            collectible.Attributes = new JsonObject(new JObject());
        }

        collectible.Attributes.Token[CoolingAttributeKey] = JToken.FromObject(coolingValue);
    }

    public static float GetCooling(ItemStack itemStack)
    {
        CollectibleObject collectible = itemStack?.Collectible;
        if (collectible?.Attributes == null)
        {
            return 0f;
        }

        return collectible.Attributes.Token[CoolingAttributeKey]?.ToObject<float>() ?? 0f;
    }

    public static void ApplyCoolingPatches(ICoreAPI api, List<JObject> patches)
    {
        foreach (var patch in patches)
        {
            string itemName = patch["itemname"]?.ToString();
            if (string.IsNullOrEmpty(itemName)) continue;

            var matchingCollectibles = GetMatchingCollectibles(api, itemName);
            foreach (var collectible in matchingCollectibles)
            {
                if (patch.ContainsKey(CoolingAttributeKey))
                {
                    float coolingValue = patch[CoolingAttributeKey].ToObject<float>();
                    SetCooling(api, collectible, coolingValue);
                }
                else if (patch.ContainsKey("coolingByType"))
                {
                    var coolingByType = patch["coolingByType"].ToObject<Dictionary<string, float>>();
                    foreach (var entry in coolingByType)
                    {
                        string typeKey = entry.Key;
                        float coolingValue = entry.Value;

                        if (IsMatch(collectible.Code.ToString(), typeKey))
                        {
                            SetCooling(api, collectible, coolingValue);
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
