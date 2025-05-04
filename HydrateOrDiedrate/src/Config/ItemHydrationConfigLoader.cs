using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config
{
    public static class ItemHydrationConfigLoader
    {
        public static List<JObject> LoadHydrationPatches(ICoreAPI api)
        {
            string defaultConfigName = "HoD.AddItemHydration.json";
            JObject userConfig = api.LoadModConfig<JObject>(defaultConfigName);
            if (userConfig == null)
            {
                userConfig = GenerateDefaultHydrationConfig();
                api.StoreModConfig(userConfig, defaultConfigName);
            }
            else
            {
                JObject defaultConfig = GenerateDefaultHydrationConfig();
                DeepMerge(defaultConfig, userConfig);
                api.StoreModConfig(userConfig, defaultConfigName);
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
                    string itemname = patch["itemname"]?.ToString();
                    if (itemname != null)
                    {
                        mergedPatches[itemname] = patch;
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
                            string itemName = sourceItem["itemname"]?.ToString();
                            if (string.IsNullOrEmpty(itemName))
                                continue;

                            var targetItem = targetArr.OfType<JObject>()
                                .FirstOrDefault(x => x["itemname"]?.ToString() == itemName);
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
        public static JObject GenerateDefaultHydrationConfig()
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
                            ["*"] = 750,
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
                            ["game:juiceportion-apple"] = 900,
                            ["game:juiceportion-cherry"] = 800,
                            ["game:juiceportion-peach"] = 950,
                            ["game:juiceportion-pear"] = 950,
                            ["game:juiceportion-orange"] = 1000,
                            ["game:juiceportion-mango"] = 950,
                            ["game:juiceportion-pomegranate"] = 850,
                            ["game:juiceportion-redgrapes"] = 950,
                            ["game:juiceportion-greengrapes"] = 950
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 40,
                            ["game:fruit-cranberry"] = 40,
                            ["game:fruit-blueberry"] = 40,
                            ["game:fruit-pinkapple"] = 30,
                            ["game:fruit-lychee"] = 30,
                            ["game:fruit-redcurrant"] = 40,
                            ["game:fruit-breadfruit"] = 5,
                            ["game:fruit-pineapple"] = 35,
                            ["game:fruit-blackcurrant"] = 40,
                            ["game:fruit-saguaro"] = 15,
                            ["game:fruit-whitecurrant"] = 40,
                            ["game:fruit-redapple"] = 30,
                            ["game:fruit-yellowapple"] = 30,
                            ["game:fruit-cherry"] = 40,
                            ["game:fruit-peach"] = 50,
                            ["game:fruit-pear"] = 35,
                            ["game:fruit-orange"] = 35,
                            ["game:fruit-mango"] = 50,
                            ["game:fruit-pomegranate"] = 40,
                            ["game:fruit-redgrapes"] = 40,
                            ["game:fruit-greengrapes"] = 40
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:bambooshoot",
                        ["hydration"] = 15
                    },
                    new JObject
                    {
                        ["itemname"] = "game:bread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -50,
                            ["game:bread-spelt"] = -50,
                            ["game:bread-rye"] = -50,
                            ["game:bread-flax"] = -40,
                            ["game:bread-rice"] = -60,
                            ["game:bread-cassava"] = -50,
                            ["game:bread-amaranth"] = -40,
                            ["game:bread-sunflower"] = -50,
                            ["game:bread-spelt-partbaked"] = -40,
                            ["game:bread-rye-partbaked"] = -40,
                            ["game:bread-flax-partbaked"] = -35,
                            ["game:bread-rice-partbaked"] = -50,
                            ["game:bread-cassava-partbaked"] = -40,
                            ["game:bread-amaranth-partbaked"] = -35,
                            ["game:bread-sunflower-partbaked"] = -40,
                            ["game:bread-spelt-perfect"] = -50,
                            ["game:bread-rye-perfect"] = -50,
                            ["game:bread-flax-perfect"] = -40,
                            ["game:bread-rice-perfect"] = -60,
                            ["game:bread-cassava-perfect"] = -50,
                            ["game:bread-amaranth-perfect"] = -40,
                            ["game:bread-sunflower-perfect"] = -50,
                            ["game:bread-spelt-charred"] = -80,
                            ["game:bread-rye-charred"] = -100,
                            ["game:bread-flax-charred"] = -70,
                            ["game:bread-rice-charred"] = -80,
                            ["game:bread-cassava-charred"] = -80,
                            ["game:bread-amaranth-charred"] = -70,
                            ["game:bread-sunflower-charred"] = -80
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:bushmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -80,
                            ["game:bushmeat-cooked"] = -70,
                            ["game:bushmeat-cured"] = -90
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:butter",
                        ["hydration"] = -80
                    },
                    new JObject
                    {
                        ["itemname"] = "game:cheese-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -50,
                            ["game:cheese-blue-1slice"] = -60,
                            ["game:cheese-cheddar-1slice"] = -45
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:dough-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5,
                            ["game:dough-spelt"] = 5,
                            ["game:dough-rye"] = 5,
                            ["game:dough-flax"] = 5,
                            ["game:dough-rice"] = 5,
                            ["game:dough-cassava"] = 5,
                            ["game:dough-amaranth"] = 5,
                            ["game:dough-sunflower"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:fish-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5,
                            ["game:fish-raw"] = 15,
                            ["game:fish-cooked"] = -20,
                            ["game:fish-cured"] = -70,
                            ["game:fish-smoked"] = -80,
                            ["game:fish-cured-smoked"] = -120
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:grain-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -30
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:insect-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:legume-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -30
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:pemmican-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -150,
                            ["game:pemmican-raw-basic"] = -75,
                            ["game:pemmican-raw-salted"] = -120,
                            ["game:pemmican-dried-basic"] = -150,
                            ["game:pemmican-dried-salted"] = -200
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:pickledlegume-soybean",
                        ["hydration"] = 20
                    },
                    new JObject
                    {
                        ["itemname"] = "game:pickledvegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 30,
                            ["game:pickledvegetable-carrot"] = 30,
                            ["game:pickledvegetable-cabbage"] = 30,
                            ["game:pickledvegetable-onion"] = 10,
                            ["game:pickledvegetable-turnip"] = 15,
                            ["game:pickledvegetable-parsnip"] = 10,
                            ["game:pickledvegetable-pumpkin"] = 40,
                            ["game:pickledvegetable-bellpepper"] = 50,
                            ["game:pickledvegetable-olive"] = 10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:poultry-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -40,
                            ["game:poultry-cooked"] = -50,
                            ["game:poultry-cured"] = -70
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:redmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -50,
                            ["game:redmeat-cooked"] = -100,
                            ["game:redmeat-vintage"] = -150,
                            ["game:redmeat-cured"] = -150
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 20,
                            ["game:vegetable-carrot"] = 20,
                            ["game:vegetable-cabbage"] = 40,
                            ["game:vegetable-onion"] = 20,
                            ["game:vegetable-turnip"] = 15,
                            ["game:vegetable-parsnip"] = 15,
                            ["game:vegetable-cookedcattailroot"] = 100,
                            ["game:vegetable-pumpkin"] = 20,
                            ["game:vegetable-cassava"] = 200,
                            ["game:vegetable-cookedpapyrusroot"] = 25,
                            ["game:vegetable-bellpepper"] = 30,
                            ["game:vegetable-olive"] = 15
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:alcoholportion",
                        ["hydration"] = -40
                    },
                    new JObject
                    {
                        ["itemname"] = "game:boilingwaterportion",
                        ["hydration"] = 600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:boiledwaterportion",
                        ["hydration"] = 600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:boiledrainwaterportion",
                        ["hydration"] = 800
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:distilledwaterportion",
                        ["hydration"] = 900
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:rainwaterportion",
                        ["hydration"] = 750
                    },
                    new JObject
                    {
                        ["itemname"] = "game:ciderportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 500,
                            ["game:ciderportion-cranberry"] = 450,
                            ["game:ciderportion-blueberry"] = 500,
                            ["game:ciderportion-pinkapple"] = 500,
                            ["game:ciderportion-lychee"] = 550,
                            ["game:ciderportion-redcurrant"] = 500,
                            ["game:ciderportion-breadfruit"] = 250,
                            ["game:ciderportion-pineapple"] = 500,
                            ["game:ciderportion-blackcurrant"] = 450,
                            ["game:ciderportion-saguaro"] = 300,
                            ["game:ciderportion-whitecurrant"] = 550,
                            ["game:ciderportion-redapple"] = 500,
                            ["game:ciderportion-yellowapple"] = 500,
                            ["game:ciderportion-cherry"] = 400,
                            ["game:ciderportion-peach"] = 475,
                            ["game:ciderportion-pear"] = 525,
                            ["game:ciderportion-orange"] = 400,
                            ["game:ciderportion-mango"] = 525,
                            ["game:ciderportion-pomegranate"] = 425,
                            ["game:ciderportion-apple"] = 500,
                            ["game:ciderportion-mead"] = 400,
                            ["game:ciderportion-spelt"] = 450,
                            ["game:ciderportion-rice"] = 450,
                            ["game:ciderportion-rye"] = 450,
                            ["game:ciderportion-amaranth"] = 450,
                            ["game:ciderportion-cassava"] = 450,
                            ["game:ciderportion-redgrapes"] = 550,
                            ["game:ciderportion-greengrapes"] = 550
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:honeyportion",
                        ["hydration"] = -75
                    },
                    new JObject
                    {
                        ["itemname"] = "game:jamhoneyportion",
                        ["hydration"] = -60
                    },
                    new JObject
                    {
                        ["itemname"] = "game:saltwaterportion",
                        ["hydration"] = -600
                    },
                    new JObject
                    {
                        ["itemname"] = "game:vinegarportion",
                        ["hydration"] = -50
                    },
                    new JObject
                    {
                        ["itemname"] = "game:cottagecheeseportion",
                        ["hydration"] = 50
                    },
                    new JObject
                    {
                        ["itemname"] = "game:milkportion",
                        ["hydration"] = 800
                    },
                    new JObject
                    {
                        ["itemname"] = "game:waterportion",
                        ["hydration"] = 600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-fresh",
                        ["hydration"] = 750
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-salt",
                        ["hydration"] = -600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-muddy",
                        ["hydration"] = 600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-tainted",
                        ["hydration"] = 750
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-poisoned",
                        ["hydration"] = 750
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-muddysalt",
                        ["hydration"] = -600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-taintedsalt",
                        ["hydration"] = -600
                    },
                    new JObject
                    {
                        ["itemname"] = "hydrateordiedrate:wellwaterportion-poisonedsalt",
                        ["hydration"] = -600
                    },
                    new JObject
                    {
                        ["itemname"] = "game:mushroom-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 0,
                            ["game:mushroom-flyagaric"] = -25,
                            ["game:mushroom-earthball"] = -30,
                            ["game:mushroom-deathcap"] = -35,
                            ["game:mushroom-elfinsaddle"] = -30,
                            ["game:mushroom-jackolantern"] = -25,
                            ["game:mushroom-devilbolete"] = -80,
                            ["game:mushroom-bitterbolete"] = -15,
                            ["game:mushroom-devilstooth"] = -25,
                            ["game:mushroom-golddropmilkcap"] = -20,
                            ["game:mushroom-beardedtooth"] = 10,
                            ["game:mushroom-whiteoyster"] = 10,
                            ["game:mushroom-pinkoyster"] = 10,
                            ["game:mushroom-dryadsaddle"] = 10,
                            ["game:mushroom-tinderhoof"] = 10,
                            ["game:mushroom-chickenofthewoods"] = 10,
                            ["game:mushroom-reishi"] = 5,
                            ["game:mushroom-funeralbell"] = -20,
                            ["game:mushroom-livermushroom"] = 10,
                            ["game:mushroom-pinkbonnet"] = -25,
                            ["game:mushroom-shiitake"] = 5,
                            ["game:mushroom-deerear"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "game:spiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 350,
                            ["game:spiritportion-cranberry"] = 320,
                            ["game:spiritportion-blueberry"] = 360,
                            ["game:spiritportion-pinkapple"] = 380,
                            ["game:spiritportion-lychee"] = 440,
                            ["game:spiritportion-redcurrant"] = 350,
                            ["game:spiritportion-breadfruit"] = 230,
                            ["game:spiritportion-pineapple"] = 400,
                            ["game:spiritportion-blackcurrant"] = 340,
                            ["game:spiritportion-saguaro"] = 260,
                            ["game:spiritportion-whitecurrant"] = 370,
                            ["game:spiritportion-redapple"] = 400,
                            ["game:spiritportion-yellowapple"] = 400,
                            ["game:spiritportion-cherry"] = 330,
                            ["game:spiritportion-peach"] = 400,
                            ["game:spiritportion-pear"] = 450,
                            ["game:spiritportion-orange"] = 370,
                            ["game:spiritportion-mango"] = 450,
                            ["game:spiritportion-pomegranate"] = 390,
                            ["game:spiritportion-apple"] = 400,
                            ["game:spiritportion-mead"] = 200,
                            ["game:spiritportion-spelt"] = 225,
                            ["game:spiritportion-rice"] = 225,
                            ["game:spiritportion-rye"] = 225,
                            ["game:spiritportion-amaranth"] = 225,
                            ["game:spiritportion-cassava"] = 225,
                            ["game:spiritportion-redgrapes"] = 380,
                            ["game:spiritportion-greengrapes"] = 380
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "alchemy:potionportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -200,
                            ["alchemy:potionportion-all-medium"] = -100,
                            ["alchemy:potionportion-all-strong"] = -300,
                            ["alchemy:potionportion-alltick-medium"] = -100,
                            ["alchemy:potionportion-alltick-strong"] = -300,
                            ["alchemy:potionportion-archer-medium"] = -100,
                            ["alchemy:potionportion-archer-strong"] = -300,
                            ["alchemy:potionportion-healingeffect-medium"] = -100,
                            ["alchemy:potionportion-healingeffect-strong"] = -300,
                            ["alchemy:potionportion-hungerenhance-medium"] = -100,
                            ["alchemy:potionportion-hungerenhance-strong"] = -300,
                            ["alchemy:potionportion-hungersupress-medium"] = -100,
                            ["alchemy:potionportion-hungersupress-strong"] = -300,
                            ["alchemy:potionportion-hunter-medium"] = -100,
                            ["alchemy:potionportion-hunter-strong"] = -300,
                            ["alchemy:potionportion-looter-medium"] = -100,
                            ["alchemy:potionportion-looter-strong"] = -300,
                            ["alchemy:potionportion-melee-medium"] = -100,
                            ["alchemy:potionportion-melee-strong"] = -300,
                            ["alchemy:potionportion-mining-medium"] = -100,
                            ["alchemy:potionportion-mining-strong"] = -300,
                            ["alchemy:potionportion-poison-medium"] = -100,
                            ["alchemy:potionportion-poison-strong"] = -300,
                            ["alchemy:potionportion-predator-medium"] = -100,
                            ["alchemy:potionportion-predator-strong"] = -300,
                            ["alchemy:potionportion-regen-medium"] = -100,
                            ["alchemy:potionportion-regen-strong"] = -300,
                            ["alchemy:potionportion-scentmask-medium"] = -100,
                            ["alchemy:potionportion-scentmask-strong"] = -300,
                            ["alchemy:potionportion-speed-medium"] = -100,
                            ["alchemy:potionportion-speed-strong"] = -300,
                            ["alchemy:potionportion-vitality-medium"] = -100,
                            ["alchemy:potionportion-vitality-strong"] = -300
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "alchemy:potionteaportion",
                        ["hydration"] = 300
                    },
                    new JObject
                    {
                        ["itemname"] = "alchemy:utilitypotionportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -200,
                            ["alchemy:utilitypotionportion-recall"] = -200,
                            ["alchemy:utilitypotionportion-glow"] = -200,
                            ["alchemy:utilitypotionportion-waterbreathe"] = -200,
                            ["alchemy:utilitypotionportion-nutrition"] = -200,
                            ["alchemy:utilitypotionportion-temporal"] = -200
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "butchering:bloodportion",
                        ["hydration"] = 50
                    },
                    new JObject
                    {
                        ["itemname"] = "butcher:smoked-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -50,
                            ["butcher:smoked-none-redmeat"] = -60,
                            ["butcher:smoked-cured-redmeat"] = -80,
                            ["butcher:smoked-none-bushmeat"] = -50,
                            ["butcher:smoked-cured-bushmeat"] = -70,
                            ["butcher:smoked-none-fish"] = -50,
                            ["butcher:smoked-cured-fish"] = -80,
                            ["butcher:smoked-none-primemeat"] = -50,
                            ["butcher:smoked-healing-primemeat"] = -80
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "butchering:sausage-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -80,
                            ["butchering:sausage-bloodsausage-raw"] = -50,
                            ["butchering:sausage-bloodsausage-cooked"] = -80,
                            ["butchering:sausage-blackpudding-raw"] = -50,
                            ["butchering:sausage-blackpudding-cooked"] = -80
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "butchering:primemeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -100,
                            ["butchering:primemeat-raw"] = -50,
                            ["butchering:primemeat-curedhealing"] = -80,
                            ["butchering:primemeat-cooked"] = -100,
                            ["butchering:primemeat-healing"] = -100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "butchering:offal",
                        ["hydration"] = 3
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:birchsapportion",
                        ["hydration"] = 500
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadstarter",
                        ["hydration"] = -75
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:brothportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 400,
                            ["expandedfoods:brothportion-bone"] = 400,
                            ["expandedfoods:brothportion-vegetable"] = 450,
                            ["expandedfoods:brothportion-meat"] = 425,
                            ["expandedfoods:brothportion-fish"] = 420
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:clarifiedbrothportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600,
                            ["expandedfoods:clarifiedbrothportion-bone"] = 600,
                            ["expandedfoods:clarifiedbrothportion-vegetable"] = 650,
                            ["expandedfoods:clarifiedbrothportion-meat"] = 625,
                            ["expandedfoods:clarifiedbrothportion-fish"] = 620
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:dressing-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:fishsauce",
                        ["hydration"] = 60
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:foodoilportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -30
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:fruitsyrupportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:lard",
                        ["hydration"] = -5
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:maplesapportion",
                        ["hydration"] = 600
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:pasteurizedmilkportion",
                        ["hydration"] = 800
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:peanutliquid-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5,
                            ["expandedfoods:peanutliquid-paste"] = -5,
                            ["expandedfoods:peanutliquid-butter"] = -30,
                            ["expandedfoods:peanutliquid-sauce"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:potentspiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 170,
                            ["expandedfoods:potentspiritportion-apple"] = 170,
                            ["expandedfoods:potentspiritportion-mead"] = 180,
                            ["expandedfoods:potentspiritportion-spelt"] = 175,
                            ["expandedfoods:potentspiritportion-rice"] = 175,
                            ["expandedfoods:potentspiritportion-rye"] = 175,
                            ["expandedfoods:potentspiritportion-amaranth"] = 175,
                            ["expandedfoods:potentspiritportion-cassava"] = 175
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:potentwineportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 160,
                            ["expandedfoods:potentwineportion-apple"] = 160,
                            ["expandedfoods:potentwineportion-mead"] = 170,
                            ["expandedfoods:potentwineportion-spelt"] = 165,
                            ["expandedfoods:potentwineportion-rice"] = 165,
                            ["expandedfoods:potentwineportion-rye"] = 165,
                            ["expandedfoods:potentwineportion-amaranth"] = 165,
                            ["expandedfoods:potentwineportion-cassava"] = 165
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:softresin",
                        ["hydration"] = -10
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:soulstormbrew-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100,
                            ["expandedfoods:soulstormbrew-slop"] = 100,
                            ["expandedfoods:soulstormbrew-refinedslop"] = 150,
                            ["expandedfoods:soulstormbrew-basic"] = 200
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:soymilk-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600,
                            ["expandedfoods:soymilk-raw"] = 600,
                            ["expandedfoods:soymilk-edible"] = 700
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:soysauce",
                        ["hydration"] = -20
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:strongspiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 150,
                            ["expandedfoods:strongspiritportion-apple"] = 150,
                            ["expandedfoods:strongspiritportion-mead"] = 160,
                            ["expandedfoods:strongspiritportion-spelt"] = 155,
                            ["expandedfoods:strongspiritportion-rice"] = 155,
                            ["expandedfoods:strongspiritportion-rye"] = 155,
                            ["expandedfoods:strongspiritportion-amaranth"] = 155,
                            ["expandedfoods:strongspiritportion-cassava"] = 155
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:strongwineportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 150,
                            ["expandedfoods:strongwineportion-apple"] = 150,
                            ["expandedfoods:strongwineportion-mead"] = 160,
                            ["expandedfoods:strongwineportion-spelt"] = 155,
                            ["expandedfoods:strongwineportion-rice"] = 155,
                            ["expandedfoods:strongwineportion-rye"] = 155,
                            ["expandedfoods:strongwineportion-amaranth"] = 155,
                            ["expandedfoods:strongwineportion-cassava"] = 155
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:treesyrupportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -20
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:vegetablejuiceportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 150,
                            ["expandedfoods:vegetablejuiceportion-carrot"] = 200,
                            ["expandedfoods:vegetablejuiceportion-cabbage"] = 300,
                            ["expandedfoods:vegetablejuiceportion-onion"] = 150,
                            ["expandedfoods:vegetablejuiceportion-turnip"] = 150,
                            ["expandedfoods:vegetablejuiceportion-parsnip"] = 150,
                            ["expandedfoods:vegetablejuiceportion-pumpkin"] = 150,
                            ["expandedfoods:vegetablejuiceportion-cassava"] = 150,
                            ["expandedfoods:vegetablejuiceportion-bellpepper"] = 150
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:yeastwaterportion",
                        ["hydration"] = 50
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:yogurt-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acorncoffee",
                        ["hydration"] = 75
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildfruitsyrupportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildpotentspiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildpotentwineportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildstrongspiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildstrongwineportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreefruitsyrupportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreepotentspiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreepotentwineportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreestrongspiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreestrongwineportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:agedmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadcrumbs-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:candiedfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:choppedmushroom-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5,
                            ["expandedfoods:choppedmushroom-earthball"] = -4,
                            ["expandedfoods:choppedmushroom-deathcap"] = -25,
                            ["expandedfoods:choppedmushroom-funeralbell"] = -20,
                            ["expandedfoods:choppedmushroom-elfinsaddle"] = -3,
                            ["expandedfoods:choppedmushroom-jackolantern"] = -3,
                            ["expandedfoods:choppedmushroom-devilbolete"] = -5,
                            ["expandedfoods:choppedmushroom-pinkbonnet"] = -10,
                            ["expandedfoods:choppedmushroom-flyagaric"] = -3,
                            ["expandedfoods:choppedmushroom-bitterbolete"] = -1,
                            ["expandedfoods:choppedmushroom-devilstooth"] = -1,
                            ["expandedfoods:choppedmushroom-golddropmilkcap"] = -1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:choppedvegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5,
                            ["expandedfoods:choppedvegetable-cassavadried"] = 5,
                            ["expandedfoods:choppedvegetable-cabbage"] = 8,
                            ["expandedfoods:choppedvegetable-pumpkin"] = 7,
                            ["expandedfoods:choppedvegetable-pickledcarrot"] = 6,
                            ["expandedfoods:choppedvegetable-pickledonion"] = 6,
                            ["expandedfoods:choppedvegetable-pickledparsnip"] = 6,
                            ["expandedfoods:choppedvegetable-pickledturnip"] = 6,
                            ["expandedfoods:choppedvegetable-pickledpumpkin"] = 6,
                            ["expandedfoods:choppedvegetable-papyrusroot"] = 7,
                            ["expandedfoods:choppedvegetable-cattailroot"] = 100,
                            ["expandedfoods:choppedvegetable-bellpepper"] = 7
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:cookedchoppedmushroom-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:cookedchoppedvegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:cookedmushroom-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:cookedveggie-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:dehydratedfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:dryfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:driedseaweed-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:fermentedfish-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:fishnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:gelatin-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:gelatinfish-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:limeegg-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:meatnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:peanut-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -3
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:soyprep-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornbreadcrumbs-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornpowdered-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acorns-roasted-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornberrybread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornbreadedball-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornbreadedfishnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornbreadedmeatnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornbreadedmushroom-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornbreadedvegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acorndoughball-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acorndumpling-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornhardtack-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornmuffin-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornpasta-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:acornstuffedpepper-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:saltedmeatnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -15
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:berrybread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadedball-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadedfishnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadedmeatnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadedmushroom-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:breadedvegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:candy-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:dumpling-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -3
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:fruitbar-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:gozinaki-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:hardtack-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:muffin-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:pasta-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -3
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:pemmican-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:pemmicanfish-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:plaindoughball-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausage-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausagefish-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:stuffedpepper-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sushi-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sushiveg-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -1
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:trailmix-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:trailmixvegetarian-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:crabnugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:snakenugget-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:pemmicancrab-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:pemmicansnake-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausagecrab-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausagesnake-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:herbalcornbreadcrumbs-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:herbalbreadcrumbs-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildcandiedfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wilddehydratedfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wilddryfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:gozinakiherb-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:herbalacornbread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:herbalacornhardtack-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -25
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:herbalabread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausagecrabherb-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausagefishherb-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausageherb-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:sausagesnakeherb-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreecandiedfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreedehydratedfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "expandedfoods:wildtreedryfruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaperegion:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 30
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaperegion:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 15
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 15
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:vegetable-nonpalm-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 15
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:flour-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -7
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:dough-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:bread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:spiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:juiceportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 50
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescaribbeanregion:ciderportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 300
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescentralaustralianregion:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescentralaustralianregion:legume-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonescentralaustralianregion:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:rawkudzu-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:flour-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -8
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:dough-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:bread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:spiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:juiceportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 700
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzoneseastasiaticregion:ciderportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 350
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonesneozeylandicregion:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "floralzonesneozeylandicregion:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:pickledlegume-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:pickledlegrain-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:legume-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -3
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:grain-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -3
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:flour-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -8
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:dough-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "newworldcrops:bread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "warriordrink:spiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "warriordrink:ciderportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 300
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:vegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:nut-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:fruit-*",
                        ["hydrationByType"] = new JObject
                        {
                        ["*"] = 10,
                        ["wildcraftfruit:fruit-watermelon"] = 50,
                        ["wildcraftfruit:fruit-illawarra"] = 15,
                        ["wildcraftfruit:fruit-creepingpine"] = 8,
                        ["wildcraftfruit:fruit-pandanbits"] = 12,
                        ["wildcraftfruit:fruit-maritabits"] = 10,
                        ["wildcraftfruit:fruit-crowseye"] = 7,
                        ["wildcraftfruit:fruit-kakaha"] = 20,
                        ["wildcraftfruit:fruit-flaxlily"] = 10,
                        ["wildcraftfruit:fruit-engkala"] = 18,
                        ["wildcraftfruit:fruit-kawakawa"] = 15,
                        ["wildcraftfruit:fruit-gooseberry"] = 12,
                        ["wildcraftfruit:fruit-blackgrape"] = 30,
                        ["wildcraftfruit:fruit-redgrape"] = 30,
                        ["wildcraftfruit:fruit-whitegrape"] = 30,
                        ["wildcraftfruit:fruit-foxgrape"] = 28,
                        ["wildcraftfruit:fruit-virgingrape"] = 25,
                        ["wildcraftfruit:fruit-gardenstrawberry"] = 18,
                        ["wildcraftfruit:fruit-strawberry"] = 18,
                        ["wildcraftfruit:fruit-falsestrawberry"] = 15,
                        ["wildcraftfruit:fruit-raspberry"] = 20,
                        ["wildcraftfruit:fruit-blueraspberry"] = 20,
                        ["wildcraftfruit:fruit-brambleberry"] = 16,
                        ["wildcraftfruit:fruit-cloudberry"] = 15,
                        ["wildcraftfruit:fruit-knyazberry"] = 22,
                        ["wildcraftfruit:fruit-bushlawyer"] = 10,
                        ["wildcraftfruit:fruit-dogrose"] = 14,
                        ["wildcraftfruit:fruit-hawthorn"] = 12,
                        ["wildcraftfruit:fruit-rowanberry"] = 12,
                        ["wildcraftfruit:fruit-quince"] = 18,
                        ["wildcraftfruit:fruit-loquat"] = 16,
                        ["wildcraftfruit:fruit-pittedcherry"] = 15,
                        ["wildcraftfruit:fruit-apricot"] = 22,
                        ["wildcraftfruit:fruit-pittedapricot"] = 22,
                        ["wildcraftfruit:fruit-purpleplum"] = 25,
                        ["wildcraftfruit:fruit-cherryplum"] = 20,
                        ["wildcraftfruit:fruit-sallowthorn"] = 10,
                        ["wildcraftfruit:fruit-jujube"] = 18,
                        ["wildcraftfruit:fruit-fig"] = 20,
                        ["wildcraftfruit:fruit-falseorange"] = 25,
                        ["wildcraftfruit:fruit-pittedbreadfruit"] = 15,
                        ["wildcraftfruit:fruit-silvernettle"] = 8,
                        ["wildcraftfruit:fruit-cucumber"] = 35,
                        ["wildcraftfruit:fruit-muskmelon"] = 45,
                        ["wildcraftfruit:fruit-mirzamelon"] = 40,
                        ["wildcraftfruit:fruit-bryony"] = 10,
                        ["wildcraftfruit:fruit-passionfruit"] = 22,
                        ["wildcraftfruit:fruit-achacha"] = 18,
                        ["wildcraftfruit:fruit-spindle"] = 10,
                        ["wildcraftfruit:fruit-ugni"] = 15,
                        ["wildcraftfruit:fruit-midyimberry"] = 18,
                        ["wildcraftfruit:fruit-munthari"] = 15,
                        ["wildcraftfruit:fruit-guajava"] = 20,
                        ["wildcraftfruit:fruit-feijoa"] = 22,
                        ["wildcraftfruit:fruit-roseapple"] = 25,
                        ["wildcraftfruit:fruit-lillypillypink"] = 20,
                        ["wildcraftfruit:fruit-lillypillyblue"] = 20,
                        ["wildcraftfruit:fruit-lillypillywhite"] = 20,
                        ["wildcraftfruit:fruit-beachalmondwhole"] = 12,
                        ["wildcraftfruit:fruit-pittedbeachalmond"] = 12,
                        ["wildcraftfruit:fruit-bluetongue"] = 18,
                        ["wildcraftfruit:fruit-turkscap"] = 14,
                        ["wildcraftfruit:fruit-cocoa"] = 15,
                        ["wildcraftfruit:fruit-wolfberry"] = 10,
                        ["wildcraftfruit:fruit-caperberry"] = 12,
                        ["wildcraftfruit:fruit-citron"] = 25,
                        ["wildcraftfruit:fruit-lemon"] = 30,
                        ["wildcraftfruit:fruit-pomelo"] = 35,
                        ["wildcraftfruit:fruit-fingerlime"] = 28,
                        ["wildcraftfruit:fruit-kumquat"] = 20,
                        ["wildcraftfruit:fruit-lemonaspen"] = 30,
                        ["wildcraftfruit:fruit-cashewwhole"] = 8,
                        ["wildcraftfruit:fruit-cashewapple"] = 25,
                        ["wildcraftfruit:fruit-chinaberry"] = 10,
                        ["wildcraftfruit:fruit-pittedchinaberry"] = 10,
                        ["wildcraftfruit:fruit-redquandong"] = 20,
                        ["wildcraftfruit:fruit-rubysaltbush"] = 12,
                        ["wildcraftfruit:fruit-pokeberry"] = 8,
                        ["wildcraftfruit:fruit-bunchberry"] = 15,
                        ["wildcraftfruit:fruit-cheeseberry"] = 15,
                        ["wildcraftfruit:fruit-pineheath"] = 10,
                        ["wildcraftfruit:fruit-pricklyheath"] = 10,
                        ["wildcraftfruit:fruit-honeypots"] = 12,
                        ["wildcraftfruit:fruit-crowberry"] = 10,
                        ["wildcraftfruit:fruit-lingonberry"] = 15,
                        ["wildcraftfruit:fruit-huckleberry"] = 18,
                        ["wildcraftfruit:fruit-kiwi"] = 20,
                        ["wildcraftfruit:fruit-honeysuckle"] = 12,
                        ["wildcraftfruit:fruit-snowberry"] = 8,
                        ["wildcraftfruit:fruit-blackelder"] = 10,
                        ["wildcraftfruit:fruit-elderberry"] = 15,
                        ["wildcraftfruit:fruit-ivy"] = 10,
                        ["wildcraftfruit:fruit-blacknightshade"] = 10,
                        ["wildcraftfruit:fruit-blacknightshadeunripe"] = 8,
                        ["wildcraftfruit:fruit-bitternightshade"] = 8,
                        ["wildcraftfruit:fruit-naranjilla"] = 18,
                        ["wildcraftfruit:fruit-husktomato"] = 15,
                        ["wildcraftfruit:fruit-beautyberry"] = 15,
                        ["wildcraftfruit:fruit-numnum"] = 15,
                        ["wildcraftfruit:fruit-seamango"] = 20,
                        ["wildcraftfruit:fruit-coralbead"] = 10,
                        ["wildcraftfruit:fruit-pilo"] = 15,
                        ["wildcraftfruit:fruit-mingimingi"] = 15,
                        ["wildcraftfruit:fruit-fractureberry"] = 10
                    }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:flour-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -8
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:dough-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:bread-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:berrymush-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 20
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:spiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 100
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:finespiritportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 300
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:juiceportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 800,
                            ["wildcraftfruit:juiceportion-watermelon"] = 1000
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:ciderportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 400
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:fineciderportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftherb:root-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftherb:herb-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 4
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:flower-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:pickledvegetable-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 15
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:sweetwaterportion",
                        ["hydration"] = 650
                    },
                    new JObject
                    {
                        ["itemname"] = "wildcraftfruit:lemonade-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 850
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "acorns:flour-acorn-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -8
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "acorns:acorn-meal-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:trussedrot-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:trussedmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -5
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:snakemeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -2
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:smokedmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -6
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:jerky-redmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:jerky-fish-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:jerky-bushmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -10
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:curedsmokedmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -12
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "primitivesurvival:crabmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -4
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "ancienttools:saltedmeat-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = -12
                        }
                    },
                    new JObject
                    {
                        ["itemname"] = "maketea:teaportion-*",
                        ["hydrationByType"] = new JObject
                        {
                            ["*"] = 600
                        }
                    }
                }
            };
            return defaultConfig;
        }
    }
}
