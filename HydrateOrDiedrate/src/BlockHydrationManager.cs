using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HydrateOrDiedrate
{
    public static class BlockHydrationManager
    {
        private static readonly Dictionary<string, BlockHydrationConfig> BlockHydrationDict = new();

        public static void SetBlockHydration(string blockCode, BlockHydrationConfig config)
        {
            BlockHydrationDict[blockCode] = config;
        }

        public static BlockHydrationConfig GetBlockHydration(string blockCode)
        {
            // Exact match
            if (BlockHydrationDict.TryGetValue(blockCode, out var config))
            {
                return config;
            }

            // Wildcard match
            foreach (var key in BlockHydrationDict.Keys)
            {
                if (IsWildcardMatch(blockCode, key))
                {
                    return BlockHydrationDict[key];
                }
            }

            return null;
        }

        public static void ApplyBlockHydrationPatches(List<JObject> patches)
        {
            foreach (var patch in patches)
            {
                string blockCode = patch["blockCode"].ToString();
                var config = patch.ToObject<BlockHydrationConfig>();
                SetBlockHydration(blockCode, config);
            }
        }

        private static bool IsWildcardMatch(string text, string pattern)
        {
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern);
        }
    }

    public class BlockHydrationConfig
    {
        public Dictionary<string, float> HydrationByType { get; set; }
        public bool IsBoiling { get; set; }
        public int HungerReduction { get; set; }
    }
}