using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config
{
    public static class BlockHydrationConfigLoader
    {
        public static List<JObject> LoadBlockHydrationConfig(ICoreAPI api)
        {
            List<JObject> allPatches = new List<JObject>();
            string configName = "HoD.AddBlockHydration.json";
            JObject config = api.LoadModConfig<JObject>(configName);

            if (config == null)
            {
                config = GenerateDefaultBlockHydrationConfig();
                api.StoreModConfig(config, configName);
            }
            var sortedPatches = new SortedDictionary<int, List<JObject>>();
            int priority = config["priority"]?.Value<int>() ?? 5;

            if (!sortedPatches.ContainsKey(priority))
            {
                sortedPatches[priority] = new List<JObject>();
            }

            var patches = config["patches"]?.ToObject<List<JObject>>();
            if (patches != null)
            {
                sortedPatches[priority].AddRange(patches);
            }
            Dictionary<string, JObject> mergedPatches = new Dictionary<string, JObject>();

            foreach (var priorityLevel in sortedPatches.Keys.OrderByDescending(k => k))
            {
                foreach (var patch in sortedPatches[priorityLevel])
                {
                    string blockCode = patch["blockCode"]?.ToString();
                    if (blockCode != null)
                    {
                        mergedPatches[blockCode] = patch;
                    }
                }
            }

            return mergedPatches.Values.ToList();
        }

        public static JObject GenerateDefaultBlockHydrationConfig()
        {
            var defaultConfig = new JObject
            {
                ["priority"] = 5,
                ["patches"] = new JArray
                {
                    new JObject
                    {
                        ["blockCode"] = "boilingwater*",
                        ["hydrationByType"] = new JObject
                        {
                            ["boilingwater-*"] = 600,
                            ["*"] = 600
                        },
                        ["isBoiling"] = true,
                        ["hungerReduction"] = 0
                    },
                    new JObject
                    {
                        ["blockCode"] = "water*",
                        ["hydrationByType"] = new JObject
                        {
                            ["water-*"] = 600,
                            ["*"] = 600
                        },
                        ["isBoiling"] = false,
                        ["hungerReduction"] = 100
                    },
                    new JObject
                    {
                        ["blockCode"] = "saltwater*",
                        ["hydrationByType"] = new JObject
                        {
                            ["saltwater-*"] = -600,
                            ["*"] = -600
                        },
                        ["isBoiling"] = false,
                        ["hungerReduction"] = 100
                    },
                    new JObject
                    {
                        ["blockCode"] = "distilledwater*",
                        ["hydrationByType"] = new JObject
                        {
                            ["distilledwater-*"] = 600,
                            ["*"] = 600
                        },
                        ["isBoiling"] = false,
                        ["hungerReduction"] = 0
                    }
                }
            };

            return defaultConfig;
        }
    }
}
