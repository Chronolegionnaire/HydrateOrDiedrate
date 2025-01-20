using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Hot_Weather;

public static class CoolingManager
{
    public const string CoolingAttributeKey = "cooling";
    private static List<JObject> _lastAppliedPatches = new List<JObject>();

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
        var collectible = itemStack?.Collectible;
        if (collectible?.Attributes == null)
        {
            return 0f;
        }

        return collectible.Attributes.Token[CoolingAttributeKey]?.ToObject<float>() ?? 0f;
    }

    public static List<JObject> GetLastAppliedPatches()
    {
        return _lastAppliedPatches;
    }

    public static void ApplyCoolingPatches(ICoreAPI api, List<JObject> patches)
    {
        var compiledPatches = PreCompilePatches(patches);
        var allPrefixes = compiledPatches
            .Where(cp => cp.Prefix != null)
            .Select(cp => cp.Prefix!)
            .ToHashSet();

        var prefixItemMap = BuildPrefixItemMap(api, allPrefixes);
        foreach (var cp in compiledPatches)
        {
            if (cp.Prefix != null && prefixItemMap.TryGetValue(cp.Prefix, out var items))
            {
                foreach (var collectible in items)
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

    private class CompiledPatch
    {
        public Regex MainRegex;
        public string? Prefix;
        public float? DirectCooling;
        public Dictionary<string, float> SubExactMatches = new();
        public List<(Regex SubRegex, float Value)> SubWildcard = new();
        public float? CatchAll;
    }

    private static List<CompiledPatch> PreCompilePatches(List<JObject> patches)
    {
        var list = new List<CompiledPatch>();

        foreach (var patch in patches)
        {
            var itemNamePattern = patch["itemname"]?.ToString();
            if (string.IsNullOrEmpty(itemNamePattern)) continue;

            var cp = new CompiledPatch
            {
                MainRegex = new Regex("^" + Regex.Escape(itemNamePattern).Replace("\\*", ".*") + "$", RegexOptions.Compiled)
            };

            if (itemNamePattern.EndsWith("-*"))
            {
                cp.Prefix = itemNamePattern.Substring(0, itemNamePattern.Length - 1);
            }

            if (patch.ContainsKey(CoolingAttributeKey))
            {
                cp.DirectCooling = patch[CoolingAttributeKey].ToObject<float>();
            }
            else if (patch.ContainsKey("coolingByType"))
            {
                var coolingByType = patch["coolingByType"].ToObject<Dictionary<string, float>>();
                foreach (var kvp in coolingByType)
                {
                    if (kvp.Key == "*")
                    {
                        cp.CatchAll = kvp.Value;
                    }
                    else if (kvp.Key.Contains("*"))
                    {
                        var subPattern = "^" + Regex.Escape(kvp.Key).Replace("\\*", ".*") + "$";
                        cp.SubWildcard.Add((new Regex(subPattern, RegexOptions.Compiled), kvp.Value));
                    }
                    else
                    {
                        cp.SubExactMatches[kvp.Key] = kvp.Value;
                    }
                }
            }

            list.Add(cp);
        }

        return list;
    }

    private static Dictionary<string, List<CollectibleObject>> BuildPrefixItemMap(ICoreAPI api, HashSet<string> allPrefixes)
    {
        var map = allPrefixes.ToDictionary(prefix => prefix, _ => new List<CollectibleObject>());

        foreach (var collectible in api.World.Collectibles)
        {
            if (collectible.Code == null) continue;

            var code = collectible.Code.ToString();
            foreach (var prefix in allPrefixes)
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
        if (cp.DirectCooling.HasValue)
        {
            SetCooling(api, collectible, cp.DirectCooling.Value);
            return;
        }

        var code = collectible.Code.ToString();

        if (cp.SubExactMatches.TryGetValue(code, out var exactValue))
        {
            SetCooling(api, collectible, exactValue);
            return;
        }

        foreach (var (subRegex, value) in cp.SubWildcard)
        {
            if (subRegex.IsMatch(code))
            {
                SetCooling(api, collectible, value);
                return;
            }
        }

        if (cp.CatchAll.HasValue)
        {
            SetCooling(api, collectible, cp.CatchAll.Value);
        }
    }
}
