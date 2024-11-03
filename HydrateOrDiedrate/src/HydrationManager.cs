using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

public static class HydrationManager
{
    private static readonly Dictionary<string, float> ItemHydrationDict = new();
    private static List<JObject> LastAppliedPatches = new();

    public static void SetHydration(ICoreAPI api, string itemCode, float hydrationValue)
    {
        ItemHydrationDict[itemCode] = hydrationValue;
    }

    public static float GetHydration(ICoreAPI api, string itemCode)
    {
        return ItemHydrationDict.TryGetValue(itemCode, out var value) ? value : 0f;
    }

    public static void ApplyHydrationPatches(ICoreAPI api, List<JObject> newPatches)
    {
        var oldPatches = LastAppliedPatches;
        var changedPatches = GetChangedPatches(oldPatches, newPatches);
        LastAppliedPatches = newPatches;
        if (changedPatches.Count == 0)
        {
            return;
        }
        var affectedItems = GetAffectedItems(api, changedPatches);
        foreach (var itemCode in affectedItems)
        {
            UpdateItemHydration(api, itemCode, newPatches);
        }
    }
    
    private static List<JObject> GetChangedPatches(List<JObject> oldPatches, List<JObject> newPatches)
    {
        var changedPatches = new List<JObject>();
        var oldPatchDict = oldPatches.ToDictionary(p => p["itemname"].ToString());
        var newPatchDict = newPatches.ToDictionary(p => p["itemname"].ToString());
        foreach (var kvp in newPatchDict)
        {
            if (!oldPatchDict.TryGetValue(kvp.Key, out var oldPatch) || !JToken.DeepEquals(oldPatch, kvp.Value))
            {
                changedPatches.Add(kvp.Value);
            }
        }
        return changedPatches;
    }
    
    private static HashSet<string> GetAffectedItems(ICoreAPI api, List<JObject> changedPatches)
    {
        var affectedItems = new HashSet<string>();

        foreach (var patch in changedPatches)
        {
            string patchItemName = patch["itemname"]?.ToString();
            if (string.IsNullOrEmpty(patchItemName)) continue;
            var matchingItemCodes = GetMatchingItemCodes(api, patchItemName);
            foreach (var itemCode in matchingItemCodes)
            {
                affectedItems.Add(itemCode);
            }
        }

        return affectedItems;
    }
    
    private static List<string> GetMatchingItemCodes(ICoreAPI api, string pattern)
    {
        var matchingItemCodes = new List<string>();
        string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new System.Text.RegularExpressions.Regex(regexPattern);

        foreach (var collectible in api.World.Collectibles)
        {
            string itemCode = collectible.Code?.ToString();
            if (itemCode != null && regex.IsMatch(itemCode))
            {
                matchingItemCodes.Add(itemCode);
            }
        }

        return matchingItemCodes;
    }
    
    private static void UpdateItemHydration(ICoreAPI api, string itemCode, List<JObject> patches)
    {
        foreach (var patch in patches)
        {
            string patchItemName = patch["itemname"]?.ToString();
            if (IsMatch(itemCode, patchItemName))
            {
                if (patch.ContainsKey("hydration"))
                {
                    float hydration = patch["hydration"].ToObject<float>();
                    SetHydration(api, itemCode, hydration);
                    return;
                }
                else if (patch.ContainsKey("hydrationByType"))
                {
                    var hydrationByType = patch["hydrationByType"].ToObject<Dictionary<string, float>>();
                    foreach (var entry in hydrationByType)
                    {
                        string key = entry.Key;
                        float hydration = entry.Value;

                        if (IsMatch(itemCode, key))
                        {
                            SetHydration(api, itemCode, hydration);
                            return;
                        }
                    }
                }
            }
        }
        ItemHydrationDict.Remove(itemCode);
    }

    public static List<JObject> GetLastAppliedPatches()
    {
        return LastAppliedPatches;
    }

    private static bool IsMatch(string itemName, string patchItemName)
    {
        if (string.IsNullOrEmpty(patchItemName)) return false;
        if (patchItemName.Contains("*"))
        {
            string pattern = "^" + patchItemName.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(itemName, pattern);
        }
        return itemName == patchItemName;
    }
}
