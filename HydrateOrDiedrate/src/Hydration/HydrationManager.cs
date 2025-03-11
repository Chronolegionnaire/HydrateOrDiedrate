using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate;

public static class HydrationManager
{
    private const string HydrationAttributeKey = "hydration";
    private static List<JObject> _lastAppliedPatches = new List<JObject>();
    private static readonly Dictionary<string, Vintagestory.API.Common.Func<ItemStack, float>> CustomHydrationEvaluators = new();
    public static void RegisterHydrationEvaluator(string collectibleCode, Vintagestory.API.Common.Func<ItemStack, float> evaluator)
    {
        if (string.IsNullOrEmpty(collectibleCode) || evaluator == null)
        {
            throw new ArgumentException("Collectible code and evaluator must not be null or empty.");
        }
        CustomHydrationEvaluators[collectibleCode] = evaluator;
    }
    public static float GetHydration(ItemStack itemStack)
    {
        var collectible = itemStack?.Collectible;
        if (collectible == null) return 0f;

        var collectibleCode = collectible.Code?.ToString() ?? string.Empty;
        if (CustomHydrationEvaluators.TryGetValue(collectibleCode, out var evaluator))
        {
            return evaluator(itemStack);
        }

        if (collectible.Attributes == null) return 0f;
        return collectible.Attributes.Token[HydrationAttributeKey]?.ToObject<float>() ?? 0f;
    }
    public static void SetHydration(ICoreAPI api, CollectibleObject collectible, float hydrationValue)
    {
        if (collectible.Attributes == null)
        {
            collectible.Attributes = new JsonObject(new JObject());
        }
        collectible.Attributes.Token[HydrationAttributeKey] = JToken.FromObject(hydrationValue);
    }
    public static List<JObject> GetLastAppliedPatches()
    {
        return _lastAppliedPatches;
    }
    public static void ApplyHydrationPatches(ICoreAPI api, List<JObject> patches)
    {
        var compiledPatches = PreCompilePatches(patches);
        var allPrefixes = compiledPatches
            .Where(cp => cp.Prefix != null)
            .Select(cp => cp.Prefix!)
            .ToHashSet();

        var prefixItemMap = BuildPrefixItemMap(api, allPrefixes);

        foreach (var cp in compiledPatches)
        {
            if (cp.Prefix != null && prefixItemMap.TryGetValue(cp.Prefix, out var prefixMatches))
            {
                foreach (var collectible in prefixMatches)
                {
                    if (cp.MainRegex.IsMatch(collectible.Code.ToString()))
                    {
                        ApplyCompiledPatchToItem(api, collectible, cp);
                    }
                }
            }
            else
            {
                foreach (var collectible in api.World.Collectibles)
                {
                    if (collectible.Code != null && cp.MainRegex.IsMatch(collectible.Code.ToString()))
                    {
                        ApplyCompiledPatchToItem(api, collectible, cp);
                    }
                }
            }
        }

        _lastAppliedPatches = patches;
    }
    private static List<CompiledPatch> PreCompilePatches(List<JObject> patches)
    {
        var compiledPatches = new List<CompiledPatch>();

        foreach (var patch in patches)
        {
            var itemNamePattern = patch["itemname"]?.ToString();
            if (string.IsNullOrEmpty(itemNamePattern)) continue;

            var cp = new CompiledPatch
            {
                MainRegex = new Regex("^" + Regex.Escape(itemNamePattern).Replace("\\*", ".*") + "$", RegexOptions.Compiled),
                Prefix = itemNamePattern.EndsWith("-*") ? itemNamePattern[..^1] : null
            };

            if (patch.ContainsKey(HydrationAttributeKey))
            {
                cp.DirectHydration = patch[HydrationAttributeKey].ToObject<float>();
            }
            else if (patch.ContainsKey("hydrationByType"))
            {
                var hydrationDict = patch["hydrationByType"].ToObject<Dictionary<string, float>>();
                foreach (var (key, value) in hydrationDict)
                {
                    if (key == "*")
                    {
                        cp.CatchAll = value;
                    }
                    else if (key.Contains("*"))
                    {
                        var subPattern = "^" + Regex.Escape(key).Replace("\\*", ".*") + "$";
                        cp.SubWildcard.Add((new Regex(subPattern, RegexOptions.Compiled), value));
                    }
                    else
                    {
                        cp.SubExactMatches[key] = value;
                    }
                }
            }

            compiledPatches.Add(cp);
        }

        return compiledPatches;
    }
    private static Dictionary<string, List<CollectibleObject>> BuildPrefixItemMap(ICoreAPI api, HashSet<string> allPrefixes)
    {
        var prefixMap = allPrefixes.ToDictionary(prefix => prefix, _ => new List<CollectibleObject>());

        foreach (var collectible in api.World.Collectibles)
        {
            if (collectible.Code == null) continue;

            var code = collectible.Code.ToString();
            foreach (var prefix in allPrefixes)
            {
                if (code.StartsWith(prefix))
                {
                    prefixMap[prefix].Add(collectible);
                }
            }
        }

        return prefixMap;
    }
    private static void ApplyCompiledPatchToItem(ICoreAPI api, CollectibleObject collectible, CompiledPatch cp)
    {
        if (cp.DirectHydration.HasValue)
        {
            SetHydration(api, collectible, cp.DirectHydration.Value);
            return;
        }

        var code = collectible.Code.ToString();
        if (cp.SubExactMatches.TryGetValue(code, out var exactVal))
        {
            SetHydration(api, collectible, exactVal);
            return;
        }

        foreach (var (subRegex, val) in cp.SubWildcard)
        {
            if (subRegex.IsMatch(code))
            {
                SetHydration(api, collectible, val);
                return;
            }
        }

        if (cp.CatchAll.HasValue)
        {
            SetHydration(api, collectible, cp.CatchAll.Value);
        }
    }
    private class CompiledPatch
    {
        public Regex MainRegex { get; set; }
        public string? Prefix { get; set; }
        public float? DirectHydration { get; set; }
        public Dictionary<string, float> SubExactMatches { get; } = new();
        public List<(Regex SubRegex, float Value)> SubWildcard { get; } = new();
        public float? CatchAll { get; set; }
    }
}