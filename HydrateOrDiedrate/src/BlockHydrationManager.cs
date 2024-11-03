using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public static class BlockHydrationManager
{
    private static readonly Dictionary<string, BlockHydrationConfig> BlockHydrationDict = new();
    private static List<JObject> LastAppliedPatches = new();

    public static void SetBlockHydration(string blockCode, BlockHydrationConfig config)
    {
        BlockHydrationDict[blockCode] = config;
    }

    public static BlockHydrationConfig GetBlockHydration(string blockCode)
    {
        if (BlockHydrationDict.TryGetValue(blockCode, out var config))
        {
            return config;
        }

        foreach (var key in BlockHydrationDict.Keys)
        {
            if (IsWildcardMatch(blockCode, key))
            {
                return BlockHydrationDict[key];
            }
        }

        return null;
    }

    public static void ApplyBlockHydrationPatches(List<JObject> newPatches)
    {
        var oldPatches = LastAppliedPatches;
        var changedPatches = GetChangedPatches(oldPatches, newPatches);

        LastAppliedPatches = newPatches;

        if (changedPatches.Count == 0)
        {
            return;
        }

        var affectedBlocks = GetAffectedBlocks(changedPatches);

        foreach (var blockCode in affectedBlocks)
        {
            UpdateBlockHydration(blockCode, newPatches);
        }
    }
    private static List<JObject> GetChangedPatches(List<JObject> oldPatches, List<JObject> newPatches)
    {
        var changedPatches = new List<JObject>();
        var oldPatchDict = oldPatches.ToDictionary(p => p["blockCode"].ToString());
        var newPatchDict = newPatches.ToDictionary(p => p["blockCode"].ToString());
        foreach (var kvp in newPatchDict)
        {
            if (!oldPatchDict.TryGetValue(kvp.Key, out var oldPatch) || !JToken.DeepEquals(oldPatch, kvp.Value))
            {
                changedPatches.Add(kvp.Value);
            }
        }

        return changedPatches;
    }
    private static HashSet<string> GetAffectedBlocks(List<JObject> changedPatches)
    {
        var affectedBlocks = new HashSet<string>();

        foreach (var patch in changedPatches)
        {
            string patchBlockCode = patch["blockCode"]?.ToString();
            if (string.IsNullOrEmpty(patchBlockCode)) continue;
            var matchingBlockCodes = GetMatchingBlockCodes(patchBlockCode);
            foreach (var blockCode in matchingBlockCodes)
            {
                affectedBlocks.Add(blockCode);
            }
        }

        return affectedBlocks;
    }
    private static List<string> GetMatchingBlockCodes(string pattern)
    {
        var matchingBlockCodes = new List<string>();
        string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var regex = new System.Text.RegularExpressions.Regex(regexPattern);

        foreach (var blockCode in BlockHydrationDict.Keys)
        {
            if (regex.IsMatch(blockCode))
            {
                matchingBlockCodes.Add(blockCode);
            }
        }

        return matchingBlockCodes;
    }
    private static void UpdateBlockHydration(string blockCode, List<JObject> patches)
    {
        foreach (var patch in patches)
        {
            string patchBlockCode = patch["blockCode"]?.ToString();
            if (IsWildcardMatch(blockCode, patchBlockCode))
            {
                var config = patch.ToObject<BlockHydrationConfig>();
                SetBlockHydration(blockCode, config);
                return;
            }
        }
        BlockHydrationDict.Remove(blockCode);
    }

    public static List<JObject> GetLastAppliedPatches()
    {
        return LastAppliedPatches;
    }

    private static bool IsWildcardMatch(string text, string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern);
    }

    public class BlockHydrationConfig
    {
        public Dictionary<string, float> HydrationByType { get; set; }
        public bool IsBoiling { get; set; }
        public int HungerReduction { get; set; }
    }
}