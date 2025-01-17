using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

public static class BlockHydrationManager
{
    private const string HydrationByTypeKey = "hydrationByType";
    private const string IsBoilingKey = "HoDisBoiling";
    private const string HungerReductionKey = "hungerReduction";
    private const string HealthKey = "health";
    private static ICoreAPI Api;
    private static List<JsonObject> _lastAppliedPatches = new List<JsonObject>();

    public static void SetHydrationAttributes(ICoreAPI api, CollectibleObject collectible, float hydrationValue = 0f, bool isBoiling = false, int hungerReduction = 0, int healthEffect = 0)
    {
        if (collectible.Attributes == null)
        {
            collectible.Attributes = new JsonObject(new JObject());
        }
        if (collectible.Attributes.Token[HydrationByTypeKey] == null)
        {
            collectible.Attributes.Token[HydrationByTypeKey] = new JObject();
        }
        var hydrationByType = collectible.Attributes.Token[HydrationByTypeKey] as JObject;
        hydrationByType["*"] = JToken.FromObject(hydrationValue);
        collectible.Attributes.Token[IsBoilingKey] = JToken.FromObject(isBoiling); // Use HoDisBoiling instead of isBoiling
        collectible.Attributes.Token[HungerReductionKey] = JToken.FromObject(hungerReduction);
        collectible.Attributes.Token[HealthKey] = JToken.FromObject(healthEffect);
    }

    public static void ApplyBlockHydrationPatches(ICoreAPI api, IEnumerable<JsonObject> patches,
        IEnumerable<CollectibleObject> collectibles)
    {
        foreach (var patch in patches)
        {
            string collectibleCode = patch["blockCode"]?.AsString();
            if (string.IsNullOrEmpty(collectibleCode))
            {
                continue;
            }

            var hydrationByType = patch[HydrationByTypeKey]?.AsObject<Dictionary<string, float>>();
            bool isBoiling = patch[IsBoilingKey]?.AsBool(false) ?? false;
            int hungerReduction = patch[HungerReductionKey]?.AsInt(0) ?? 0;
            int healthEffect = patch[HealthKey]?.AsInt(0) ?? 0;

            foreach (var collectible in collectibles)
            {
                if (collectible == null || collectible.Code == null)
                {
                    continue;
                }

                if (IsWildcardMatch(collectible.Code.Path, collectibleCode))
                {
                    float hydrationValue = 0f;

                    if (hydrationByType != null && hydrationByType.ContainsKey(collectible.Code.Path))
                    {
                        hydrationValue = hydrationByType[collectible.Code.Path];
                    }
                    else if (hydrationByType != null && hydrationByType.ContainsKey("*"))
                    {
                        hydrationValue = hydrationByType["*"];
                    }

                    SetHydrationAttributes(api, collectible, hydrationValue, isBoiling, hungerReduction, healthEffect);
                }
            }
        }
    }

    public static float GetHydrationValue(CollectibleObject collectible, string type)
    {
        try
        {
            if (collectible == null)
            {
                return 0f;
            }
            if (collectible.Code == null)
            {
                return 0f;
            }
            if (collectible.Attributes == null)
            {
                return 0f;
            }
            var hydrationByTypeToken = collectible.Attributes.Token[HydrationByTypeKey];
            if (hydrationByTypeToken == null)
            {
                return 0f;
            }
            var hydrationByType = hydrationByTypeToken.ToObject<Dictionary<string, float>>();
            if (hydrationByType == null)
            {
                return 0f;
            }
            if (hydrationByType.TryGetValue(type, out float hydrationValue))
            {
                return hydrationValue;
            }
            if (hydrationByType.TryGetValue("*", out float wildcardHydrationValue))
            {
                return wildcardHydrationValue;
            }
            return 0f;
        }
        catch (Exception ex)
        {
            Api.Logger.Error("An error occurred while retrieving hydration value for collectible: {0}. Exception: {1}",
                collectible?.Code?.Path ?? "Unknown", ex);
            return 0f;
        }
    }

    public static bool IsBlockBoiling(CollectibleObject collectible)
    {
        return collectible?.Attributes?.Token[IsBoilingKey]?.ToObject<bool>() ?? false;
    }

    public static int GetBlockHungerReduction(CollectibleObject collectible)
    {
        return collectible?.Attributes?.Token[HungerReductionKey]?.ToObject<int>() ?? 0;
    }

    public static int GetBlockHealth(CollectibleObject collectible)
    {
        return collectible?.Attributes?.Token[HealthKey]?.ToObject<int>() ?? 0;
    }


    private static bool IsWildcardMatch(string text, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(text, regexPattern);
    }

    public static List<JsonObject> GetLastAppliedPatches()
    {
        return _lastAppliedPatches;
    }
}
