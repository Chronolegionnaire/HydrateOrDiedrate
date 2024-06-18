using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Configuration
{
    public static class HydrationConfigLoader
    {
        public static List<JObject> LoadHydrationPatches(ICoreAPI api)
        {
            List<JObject> allPatches = new List<JObject>();
            string configFolder = ModConfig.GetConfigPath(api);
            List<string> configFiles = Directory.GetFiles(configFolder, "HoD.AddHydration*.json").ToList();
            string defaultConfigPath = Path.Combine(configFolder, "HoD.AddHydration.json");
            if (!File.Exists(defaultConfigPath))
            {
                GenerateDefaultHydrationConfig(api);
            }
            configFiles.Insert(0, defaultConfigPath);
            var sortedPatches = new SortedDictionary<int, List<JObject>>();

            foreach (string file in configFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    JObject parsedFile = JObject.Parse(json);
                    int priority = parsedFile["priority"]?.Value<int>() ?? 5;

                    if (!sortedPatches.ContainsKey(priority))
                    {
                        sortedPatches[priority] = new List<JObject>();
                    }

                    var patches = parsedFile["patches"].ToObject<List<JObject>>();
                    sortedPatches[priority].AddRange(patches);
                }
                catch (System.Exception ex)
                {
                    api.Logger.Error($"Error reading hydration config file {file}: {ex.Message}");
                }
            }
            Dictionary<string, JObject> mergedPatches = new Dictionary<string, JObject>();

            foreach (var priorityLevel in sortedPatches.Keys.OrderByDescending(k => k))
            {
                foreach (var patch in sortedPatches[priorityLevel])
                {
                    string itemname = patch["itemname"].ToString();
                    mergedPatches[itemname] = patch;
                }
            }

            return mergedPatches.Values.ToList();
        }

        public static void GenerateDefaultHydrationConfig(ICoreAPI api)
        {
            string configPath = Path.Combine(ModConfig.GetConfigPath(api), "HoD.AddHydration.json");
            if (!File.Exists(configPath))
            {
                var defaultConfig = new JObject
                {
                    ["priority"] = 5,
                    ["patches"] = new JArray
                    {
                        new JObject
                        {
                            ["itemname"] = "game:juiceportion-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:juiceportion-cranberry"] = 600,
                                ["game:juiceportion-blueberry"] = 700,
                                ["game:juiceportion-pinkapple"] = 750,
                                ["game:juiceportion-lychee"] = 850,
                                ["game:juiceportion-redcurrant"] = 800,
                                ["game:juiceportion-breadfruit"] = 500,
                                ["game:juiceportion-pineapple"] = 950,
                                ["game:juiceportion-blackcurrant"] = 800,
                                ["game:juiceportion-saguaro"] = 600,
                                ["game:juiceportion-whitecurrant"] = 800,
                                ["game:juiceportion-redapple"] = 900,
                                ["game:juiceportion-yellowapple"] = 900,
                                ["game:juiceportion-cherry"] = 800,
                                ["game:juiceportion-peach"] = 950,
                                ["game:juiceportion-pear"] = 950,
                                ["game:juiceportion-orange"] = 1000,
                                ["game:juiceportion-mango"] = 950,
                                ["game:juiceportion-pomegranate"] = 850,
                                ["*"] = 750
                            },
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:fruit-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:fruit-cranberry"] = 10,
                                ["game:fruit-blueberry"] = 7,
                                ["game:fruit-pinkapple"] = 12,
                                ["game:fruit-lychee"] = 20,
                                ["game:fruit-redcurrant"] = 15,
                                ["game:fruit-breadfruit"] = 5,
                                ["game:fruit-pineapple"] = 20,
                                ["game:fruit-blackcurrant"] = 15,
                                ["game:fruit-saguaro"] = 10,
                                ["game:fruit-whitecurrant"] = 15,
                                ["game:fruit-redapple"] = 18,
                                ["game:fruit-yellowapple"] = 18,
                                ["game:fruit-cherry"] = 15,
                                ["game:fruit-peach"] = 25,
                                ["game:fruit-pear"] = 20,
                                ["game:fruit-orange"] = 30,
                                ["game:fruit-mango"] = 25,
                                ["game:fruit-pomegranate"] = 20,
                                ["*"] = 8
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:bambooshoot",
                            ["hydration"] = 15,
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:bread-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:bread-spelt"] = -5,
                                ["game:bread-rye"] = -4,
                                ["game:bread-flax"] = -3,
                                ["game:bread-rice"] = -6,
                                ["game:bread-cassava"] = -5,
                                ["game:bread-amaranth"] = -4,
                                ["game:bread-sunflower"] = -3,
                                ["game:bread-spelt-partbaked"] = -5,
                                ["game:bread-rye-partbaked"] = -4,
                                ["game:bread-flax-partbaked"] = -3,
                                ["game:bread-rice-partbaked"] = -6,
                                ["game:bread-cassava-partbaked"] = -5,
                                ["game:bread-amaranth-partbaked"] = -4,
                                ["game:bread-sunflower-partbaked"] = -3,
                                ["game:bread-spelt-perfect"] = -5,
                                ["game:bread-rye-perfect"] = -4,
                                ["game:bread-flax-perfect"] = -3,
                                ["game:bread-rice-perfect"] = -6,
                                ["game:bread-cassava-perfect"] = -5,
                                ["game:bread-amaranth-perfect"] = -4,
                                ["game:bread-sunflower-perfect"] = -3,
                                ["game:bread-spelt-charred"] = -10,
                                ["game:bread-rye-charred"] = -9,
                                ["game:bread-flax-charred"] = -8,
                                ["game:bread-rice-charred"] = -11,
                                ["game:bread-cassava-charred"] = -10,
                                ["game:bread-amaranth-charred"] = -9,
                                ["game:bread-sunflower-charred"] = -8,
                                ["*"] = -5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:bushmeat-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:bushmeat-raw"] = -2,
                                ["game:bushmeat-cooked"] = -5,
                                ["game:bushmeat-cured"] = -10,
                                ["*"] = -5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:butter",
                            ["hydration"] = -5,
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:cheese-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:cheese-blue-1slice"] = -4,
                                ["game:cheese-cheddar-1slice"] = -3
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:dough-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:dough-spelt"] = 5,
                                ["game:dough-rye"] = 5,
                                ["game:dough-flax"] = 5,
                                ["game:dough-rice"] = 5,
                                ["game:dough-cassava"] = 5,
                                ["game:dough-amaranth"] = 5,
                                ["game:dough-sunflower"] = 5,
                                ["*"] = 5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:fish-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:fish-raw"] = 5,
                                ["game:fish-cooked"] = -2,
                                ["game:fish-cured"] = -10,
                                ["game:fish-smoked"] = -8,
                                ["game:fish-cured-smoked"] = -12,
                                ["*"] = -5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:grain-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:grain-spelt"] = -3,
                                ["game:grain-rice"] = -3,
                                ["game:grain-flax"] = -3,
                                ["game:grain-rye"] = -3,
                                ["game:grain-amaranth"] = -3,
                                ["game:grain-sunflower"] = -3,
                                ["*"] = -3
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:insect-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:insect-grub"] = 2,
                                ["game:insect-termite"] = 2,
                                ["game:insect-termite-stick"] = 2,
                                ["*"] = 2
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:legume-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:legume-soybean"] = -3,
                                ["game:legume-peanut"] = -3,
                                ["*"] = -3
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:pemmican-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:pemmican-raw-basic"] = -5,
                                ["game:pemmican-raw-salted"] = -7,
                                ["game:pemmican-dried-basic"] = -10,
                                ["game:pemmican-dried-salted"] = -12,
                                ["*"] = -5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:pickledlegume-soybean",
                            ["hydration"] = 2,
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:pickledvegetable-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:pickledvegetable-carrot"] = 3,
                                ["game:pickledvegetable-cabbage"] = 3,
                                ["game:pickledvegetable-onion"] = 2,
                                ["game:pickledvegetable-turnip"] = 3,
                                ["game:pickledvegetable-parsnip"] = 3,
                                ["game:pickledvegetable-pumpkin"] = 4,
                                ["game:pickledvegetable-bellpepper"] = 4,
                                ["game:pickledvegetable-olive"] = 1,
                                ["*"] = 3
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:poultry-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:poultry-cooked"] = -5,
                                ["game:poultry-cured"] = -10,
                                ["*"] = -5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:redmeat-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:redmeat-cooked"] = -5,
                                ["game:redmeat-vintage"] = -8,
                                ["game:redmeat-cured"] = -10,
                                ["*"] = -5
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:vegetable-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:vegetable-carrot"] = 8,
                                ["game:vegetable-cabbage"] = 10,
                                ["game:vegetable-onion"] = 6,
                                ["game:vegetable-turnip"] = 8,
                                ["game:vegetable-parsnip"] = 7,
                                ["game:vegetable-cookedcattailroot"] = 5,
                                ["game:vegetable-pumpkin"] = 12,
                                ["game:vegetable-cassava"] = 6,
                                ["game:vegetable-cookedpapyrusroot"] = 5,
                                ["game:vegetable-bellpepper"] = 12,
                                ["game:vegetable-olive"] = 4,
                                ["*"] = 7
                            },
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:alcoholportion",
                            ["hydration"] = -20,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:boilingwaterportion",
                            ["hydration"] = 1500,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:ciderportion-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:ciderportion-apple"] = 500,
                                ["game:ciderportion-mead"] = 400,
                                ["game:ciderportion-spelt"] = 450,
                                ["game:ciderportion-rice"] = 450,
                                ["game:ciderportion-rye"] = 450,
                                ["game:ciderportion-amaranth"] = 450,
                                ["game:ciderportion-cassava"] = 450,
                                ["*"] = 450
                            },
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:honeyportion",
                            ["hydration"] = 300,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:jamhoneyportion",
                            ["hydration"] = 350,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:saltwaterportion",
                            ["hydration"] = -1000,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:spirit-*",
                            ["hydrationByType"] = new JObject
                            {
                                ["game:spirit-apple"] = 250,
                                ["game:spirit-mead"] = 200,
                                ["game:spirit-spelt"] = 225,
                                ["game:spirit-rice"] = 225,
                                ["game:spirit-rye"] = 225,
                                ["game:spirit-amaranth"] = 225,
                                ["game:spirit-cassava"] = 225,
                                ["*"] = 225
                            },
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:vinegarportion",
                            ["hydration"] = 50,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:cottagecheeseportion",
                            ["hydration"] = 50,
                            ["IsLiquid"] = false
                        },
                        new JObject
                        {
                            ["itemname"] = "game:milkportion",
                            ["hydration"] = 500,
                            ["IsLiquid"] = true
                        },
                        new JObject
                        {
                            ["itemname"] = "game:waterportion",
                            ["hydration"] = 1500,
                            ["IsLiquid"] = true
                        }
                    }
                };

                try
                {
                    File.WriteAllText(configPath, defaultConfig.ToString());
                }
                catch (System.Exception ex)
                {
                    api.Logger.Error($"Error writing default hydration config file: {ex.Message}");
                }
            }
        }
    }
}
