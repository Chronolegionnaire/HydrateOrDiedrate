using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate
{
    public static class BlockHydrationManager
    {
        private const string HydrationByTypeKey = "hydrationByType";
        private const string IsBoilingKey = "HoDisBoiling";
        private const string HungerReductionKey = "hungerReduction";
        private const string HealthKey = "health";
        private static List<JsonObject> _lastAppliedPatches = new List<JsonObject>();

        public static void SetHydrationAttributes(ICoreAPI api, CollectibleObject collectible,
            float hydrationValue = 0f, bool isBoiling = false, int hungerReduction = 0, int healthEffect = 0)
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
            collectible.Attributes.Token[IsBoilingKey] = JToken.FromObject(isBoiling);
            collectible.Attributes.Token[HungerReductionKey] = JToken.FromObject(hungerReduction);
            collectible.Attributes.Token[HealthKey] = JToken.FromObject(healthEffect);
        }

        public static void ApplyBlockHydrationPatches(ICoreAPI api, IEnumerable<JsonObject> patches,
            IEnumerable<CollectibleObject> collectibles)
        {
            var compiledPatches = PreCompilePatches(patches);
            var allPrefixes = compiledPatches.Where(cp => cp.Prefix != null).Select(cp => cp.Prefix!).ToHashSet();
            var prefixItemMap = BuildPrefixItemMap(collectibles, allPrefixes);

            foreach (var cp in compiledPatches)
            {
                if (cp.Prefix != null && prefixItemMap.TryGetValue(cp.Prefix, out var matchingItems))
                {
                    foreach (var collectible in matchingItems)
                    {
                        if (cp.MainRegex.IsMatch(collectible.Code.Path))
                        {
                            ApplyCompiledPatchToItem(api, collectible, cp);
                        }
                    }
                }
                else
                {
                    foreach (var collectible in collectibles)
                    {
                        if (collectible.Code != null && cp.MainRegex.IsMatch(collectible.Code.Path))
                        {
                            ApplyCompiledPatchToItem(api, collectible, cp);
                        }
                    }
                }
            }

            _lastAppliedPatches = patches.ToList();
        }

        public static float GetHydrationValue(CollectibleObject collectible, string type)
        {
            try
            {
                if (collectible?.Attributes?.Token[HydrationByTypeKey] is JObject hydrationByType)
                {
                    if (hydrationByType.TryGetValue(type, out var token)) return token.ToObject<float>();
                    if (hydrationByType.TryGetValue("*", out var wildcard)) return wildcard.ToObject<float>();
                }
            }
            catch (Exception)
            {
            }

            return 0f;
        }

        public static bool IsBlockBoiling(CollectibleObject collectible) =>
            collectible?.Attributes?.Token[IsBoilingKey]?.ToObject<bool>() ?? false;

        public static int GetBlockHungerReduction(CollectibleObject collectible) =>
            collectible?.Attributes?.Token[HungerReductionKey]?.ToObject<int>() ?? 0;

        public static int GetBlockHealth(CollectibleObject collectible) =>
            collectible?.Attributes?.Token[HealthKey]?.ToObject<int>() ?? 0;

        public static List<JsonObject> GetLastAppliedPatches() => _lastAppliedPatches;

        private class CompiledPatch
        {
            public Regex MainRegex;
            public string? Prefix;
            public Dictionary<string, float> HydrationByTypeExact = new();
            public List<(Regex SubRegex, float Value)> HydrationByTypeWildcard = new();
            public float? CatchAll;
            public bool IsBoiling;
            public int HungerReduction;
            public int HealthEffect;
        }

        private static List<CompiledPatch> PreCompilePatches(IEnumerable<JsonObject> patches)
        {
            var compiledPatches = new List<CompiledPatch>();

            foreach (var patch in patches)
            {
                string blockCodePattern = patch["blockCode"]?.AsString();
                if (string.IsNullOrEmpty(blockCodePattern)) continue;

                var cp = new CompiledPatch
                {
                    MainRegex = new Regex("^" + Regex.Escape(blockCodePattern).Replace("\\*", ".*") + "$",
                        RegexOptions.Compiled),
                    IsBoiling = patch[IsBoilingKey]?.AsBool(false) ?? false,
                    HungerReduction = patch[HungerReductionKey]?.AsInt(0) ?? 0,
                    HealthEffect = patch[HealthKey]?.AsInt(0) ?? 0
                };

                if (blockCodePattern.EndsWith("-*"))
                {
                    cp.Prefix = blockCodePattern.Substring(0, blockCodePattern.Length - 1);
                }

                var hydrationByType = patch[HydrationByTypeKey]?.AsObject<Dictionary<string, float>>();
                if (hydrationByType != null)
                {
                    foreach (var kvp in hydrationByType)
                    {
                        if (kvp.Key == "*")
                        {
                            cp.CatchAll = kvp.Value;
                        }
                        else if (kvp.Key.Contains("*"))
                        {
                            string subPattern = "^" + Regex.Escape(kvp.Key).Replace("\\*", ".*") + "$";
                            cp.HydrationByTypeWildcard.Add((new Regex(subPattern, RegexOptions.Compiled), kvp.Value));
                        }
                        else
                        {
                            cp.HydrationByTypeExact[kvp.Key] = kvp.Value;
                        }
                    }
                }

                compiledPatches.Add(cp);
            }

            return compiledPatches;
        }

        private static Dictionary<string, List<CollectibleObject>> BuildPrefixItemMap(
            IEnumerable<CollectibleObject> collectibles, HashSet<string> prefixes)
        {
            var map = prefixes.ToDictionary(prefix => prefix, prefix => new List<CollectibleObject>());

            foreach (var collectible in collectibles)
            {
                if (collectible.Code == null) continue;
                string code = collectible.Code.Path;

                foreach (var prefix in prefixes)
                {
                    if (code.StartsWith(prefix))
                    {
                        map[prefix].Add(collectible);
                    }
                }
            }

            return map;
        }

        private static void ApplyCompiledPatchToItem(ICoreAPI api, CollectibleObject collectible, CompiledPatch cp)
        {
            float hydrationValue = 0f;

            if (cp.HydrationByTypeExact.TryGetValue(collectible.Code.Path, out var exactValue))
            {
                hydrationValue = exactValue;
            }
            else
            {
                foreach (var (subRegex, value) in cp.HydrationByTypeWildcard)
                {
                    if (subRegex.IsMatch(collectible.Code.Path))
                    {
                        hydrationValue = value;
                        break;
                    }
                }

                if (hydrationValue == 0f && cp.CatchAll.HasValue)
                {
                    hydrationValue = cp.CatchAll.Value;
                }
            }

            SetHydrationAttributes(api, collectible, hydrationValue, cp.IsBoiling, cp.HungerReduction, cp.HealthEffect);
        }
    }
}