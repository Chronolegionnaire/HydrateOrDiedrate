using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.ConfigOld
{
    public static class BlockHydrationConfigLoader
    {
        public static List<JObject> LoadBlockHydrationConfig(ICoreAPI api)
        {
            string configName = "HoD.AddBlockHydration.json";
            JObject userConfig = api.LoadModConfig<JObject>(configName);
            if (userConfig == null)
            {
                userConfig = GenerateDefaultBlockHydrationConfig();
                api.StoreModConfig(userConfig, configName);
            }
            else
            {
                JObject defaultConfig = GenerateDefaultBlockHydrationConfig();
                DeepMerge(defaultConfig, userConfig);
                api.StoreModConfig(userConfig, configName);
            }
            var sortedPatches = new SortedDictionary<int, List<JObject>>();
            int priority = userConfig["priority"]?.Value<int>() ?? 5;

            if (!sortedPatches.ContainsKey(priority))
            {
                sortedPatches[priority] = new List<JObject>();
            }
            var patches = userConfig["patches"]?.ToObject<List<JObject>>();
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
        
        private static void DeepMerge(JObject source, JObject target)
        {
            foreach (var prop in source.Properties())
            {
                if (!target.ContainsKey(prop.Name))
                {
                    target[prop.Name] = prop.Value.DeepClone();
                }
                else
                {
                    if (prop.Name == "patches" && prop.Value is JArray sourceArr && target[prop.Name] is JArray targetArr)
                    {
                        foreach (var sourceItem in sourceArr.OfType<JObject>())
                        {
                            string blockCode = sourceItem["blockCode"]?.ToString();
                            if (string.IsNullOrEmpty(blockCode))
                            {
                                continue;
                            }
                            
                            var targetItem = targetArr.OfType<JObject>()
                                .FirstOrDefault(x => x["blockCode"]?.ToString() == blockCode);
                            if (targetItem == null)
                            {
                                targetArr.Add(sourceItem.DeepClone());
                            }
                            else
                            {
                                DeepMerge(sourceItem, targetItem);
                            }
                        }
                    }
                    else if (prop.Value is JObject sourceObj && target[prop.Name] is JObject targetObj)
                    {
                        DeepMerge(sourceObj, targetObj);
                    }
                    else if (prop.Value is JArray sourceArray && target[prop.Name] is JArray targetArray)
                    {
                        foreach (var item in sourceArray)
                        {
                            if (!targetArray.Any(x => JToken.DeepEquals(x, item)))
                            {
                                targetArray.Add(item.DeepClone());
                            }
                        }
                    }
                }
            }
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
                            ["*"] = 600
                        },
                        ["HoDisBoiling"] = true,
                        ["hungerReduction"] = 0
                    },
                    new JObject
                    {
                        ["blockCode"] = "water*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 100
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwaterfresh*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 750
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 0
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwatersalt*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 100
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwatermuddy*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 50
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwatertainted*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 750
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 400,
                        ["Health"] = -5
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwaterpoisoned*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 750
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 0,
                        ["Health"] = -20
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwatermuddysalt*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 50
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwatertaintedsalt*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 400,
                        ["Health"] = -5
                    },
                    new JObject
                    {
                        ["blockCode"] = "wellwaterpoisonedsalt*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 0,
                        ["Health"] = -20
                    },
                    new JObject
                    {
                        ["blockCode"] = "saltwater*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 100
                    },
                    new JObject
                    {
                        ["blockCode"] = "distilledwater*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600
                        },
                        ["HoDisBoiling"] = false,
                        ["hungerReduction"] = 0
                    }
                }
            };

            return defaultConfig;
        }
    }
}
