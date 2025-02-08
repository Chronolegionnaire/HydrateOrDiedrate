using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config
{
    public static class CoolingConfigLoader
    {
        public static List<JObject> LoadCoolingPatches(ICoreAPI api)
        {
            string defaultConfigName = "HoD.AddCooling.json";
            JObject userConfig = api.LoadModConfig<JObject>(defaultConfigName);
            if (userConfig == null)
            {
                userConfig = GenerateDefaultCoolingConfig();
                api.StoreModConfig(userConfig, defaultConfigName);
            }
            else
            {
                JObject defaultConfig = GenerateDefaultCoolingConfig();
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
                            string itemname = sourceItem["itemname"]?.ToString();
                            if (string.IsNullOrEmpty(itemname))
                                continue;

                            var targetItem = targetArr.OfType<JObject>()
                                .FirstOrDefault(x => x["itemname"]?.ToString() == itemname);
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
                            if (!targetArray.Any(t => JToken.DeepEquals(t, item)))
                            {
                                targetArray.Add(item.DeepClone());
                            }
                        }
                    }
                }
            }
        }

        public static JObject GenerateDefaultCoolingConfig()
        {
            var defaultConfig = new JObject
            {
                ["priority"] = 5,
                ["patches"] = new JArray
                {
                    new JObject { ["itemname"] = "game:clothes-upperbody-tattered-crimson-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-tattered-linen-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-peasent-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-pastoral-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-chateau-blouse", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-aristocrat-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-azure-embroidered-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-hunter-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-great-steppe-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-earth-toned-robe", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-jailor-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-lackey-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-linen-tunic", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-merchant-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-messenger-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-minstrel-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-noble-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-pearl-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-prince-tunic", ["cooling"] = 2 },
                    new JObject
                    {
                        ["itemname"] = "game:clothes-upperbody-reindeer-herder-collared-shirt", ["cooling"] = 1.5
                    },
                    new JObject { ["itemname"] = "game:clothes-upperbody-scarlet-ornate-linen-tunic", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-shepherd-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-squire-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-temptress-velvet-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-woolen-shirt", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-blackguard-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-clockmaker-shirt", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-malefactor-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-forlorn-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-commoner-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-raw-hide-mantle", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-tailor-blouse", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-homespun-shirt", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-marketeer", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-rottenking", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-king", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-surgeon", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-miner", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-alchemist", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-forgotten", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-deep", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-survivor", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbody-scribe", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-great-steppe-mantle", ["cooling"] = 1.5 },
                    new JObject
                    {
                        ["itemname"] = "game:clothes-upperbodyover-cerise-embroidered-reindeer-herder-coat",
                        ["cooling"] = 1
                    },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-commoner-coat", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-fur-coat", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-huntsmans-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-cobalt-mantle", ["cooling"] = 2 },
                    new JObject
                    {
                        ["itemname"] = "game:clothes-upperbodyover-reindeer-herder-fur-coat", ["cooling"] = 0.5
                    },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-spice-merchants-coat", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-nomad-mantle", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-travelers-earthrobe", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-warm-robe", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-clockmaker-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-hunter-coat", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-malefactor-tunic", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-tailor-jacket", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-marketeer", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-rotwalker", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-rottenking", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-king", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-surgeon", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-miner", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-upperbodyover-forgotten", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-winter2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-tailor", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-beekeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-musician", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-miner-clean", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-coat-verylong", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-coat-short", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-coat-long", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbodyover-alchemist", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-winter2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-tailor", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-beekeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-fisher", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-musician", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-miner-clean", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-guard", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-blacksmith", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-innkeeper", ["cooling"] = 3 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-barber", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-alchemist", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-peasant1", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-peasant2", ["cooling"] = 3 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-peasant3", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-upperbody-peasant4", ["cooling"] = 3 },
                    new JObject { ["itemname"] = "game:clothes-waist-aristocrat-belt", ["cooling"] = 0.5 },
                    new JObject
                    {
                        ["itemname"] = "game:clothes-waist-cerise-embroidered-reindeer-herder-waistband",
                        ["cooling"] = 0.5
                    },
                    new JObject { ["itemname"] = "game:clothes-waist-fancy-royal-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-fortune-teller-hip-scarf", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-waist-gold-waist-chain", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-heavy-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-heavy-tool-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-jailor-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-linen-rope", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-waist-merchant-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-messenger-belt", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-waist-moss-embroidered-belt", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-waist-noble-sash", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-waist-peasant-strap", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-prince-waistband", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-reindeer-herder-waistband", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-waist-squire-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-sturdy-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-sturdy-leather-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-blackguard-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-clockmaker-belt", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-malefactor-sash", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-waist-forlorn-belt", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-waist-tailor-belt", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-waist-marketeer", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-waist-rottenking", ["cooling"] = 0.1 },
                    new JObject { ["itemname"] = "game:clothes-waist-king", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-waist-miner", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-alchemist", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-waist-scribe", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-aristocrat-mantle", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-artisans-scarf", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-blacksmith-apron", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-embroidered-collar", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-embroidered-tartarean-scarf", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-hooded-cape", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-jord-robe", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-hunter-poncho", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-longsleeve-pearl-moonrobe", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-minstrel-coat", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-mooncloth-robe", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-noble-fur-collar", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-peasent-kaftan", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-royal-fur", ["cooling"] = 0.5 },
                    new JObject
                    {
                        ["itemname"] = "game:clothes-shoulder-shortsleeve-pearl-moonrobe", ["cooling"] = 1.5
                    },
                    new JObject { ["itemname"] = "game:clothes-shoulder-stained-leather-poncho", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-woolen-scarf", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-squire-tunic", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-clockmaker-apron", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-forlorn-sash", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-malefactor-cloak", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-marketeer", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-rotwalker", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-surgeon", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-miner", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-alchemist", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-survivor", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-shoulder-scribe", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-barber", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-blacksmith", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-innkeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-musician", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-fisher", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-guard", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-peasant1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-peasant2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-shoulder-peasant3", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-aristocrat-leggings", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-dirty-linen-trousers", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-fine-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-jailor-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-lackey-breeches", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-merchant-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-messenger-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-minstrel-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-noble-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-prince-breeches", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-reindeer-herder-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-raw-hide-trousers", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-shepherd-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-squire-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-nomad-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-tattered-peasent-gown", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-torn-riding-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-warm-woolen-pants", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-woolen-leggings", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-workmans-gown", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-blackguard-leggings", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-clockmaker-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-hunter-leggings", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-malefactor-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-forlorn-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-commoner-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-tailor-trousers", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-homespun-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-pastoral-pants", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-chateau-pants", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-marketeer", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-rotwalker", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-rottenking", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-king", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-surgeon", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-miner", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-alchemist", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-forgotten", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-deep", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-survivor", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-lowerbody-scribe", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-winter2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-tailor", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-peasant1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-peasant2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-peasant3", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-peasant-skirt1", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-peasant-skirt2", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-peasant-skirt3", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-musician", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-miner-clean", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-innkeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-guard", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-fisher", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-blacksmith", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-beekeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-barber", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-lowerbody-alchemist", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-head-embroidered-coif", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-fancy-head-dress", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-fortune-tellers-scarf", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-gem-encrusted-fur-hat", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-head-gold-coronet", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-head-jailors-hat", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-lackey-hat", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-merchant-hat", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-messengers-hat", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-minstrel-hat", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-noble-fillet", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-head-peasent-head-scarf", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-roll-hat", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-shepherds-cowl", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-silver-diadem", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-head-squire-hood", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-hunter-cape", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-straw-hat", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-head-tophat", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-head-bamboo-conehat", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-head-bamboo-conehat-large", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-head-crown", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-head-mianguan", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-marketeer", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-rotwalker", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-rottenking", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-head-surgeon", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-head-alchemist", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-winter2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-tailor", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-peasantwhite", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-peasantbrown", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-peasantblue", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-peasantbeige", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-peasanttan", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-peasantred", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-musician", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-innkeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-guard", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-fisher", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-blacksmith", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-beekeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-barber", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-head-alchemist", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-hand-fur-gloves", ["cooling"] = 0.1 },
                    new JObject { ["itemname"] = "game:clothes-hand-heavy-leather-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-laced-handsome-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-lackey-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-minstrel-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-noble-riding-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-prince-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-clockmaker-wristguard", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-hunter-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-malefactor-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-forlorn-bracer", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-commoner-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-tailor-gloves", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-rottenking", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-king", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-surgeon", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-miner", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-forgotten", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-deep", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-survivor", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-hand-scribe", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-winter2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-peasant2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-peasant1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-miner-clean", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-gloves", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-fisher", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-blacksmith", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-beekeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-barber", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-hand-alchemist", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-foot-aristocrat-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-great-steppe-boots", ["cooling"] = 0.5 },
                    new JObject
                    {
                        ["itemname"] = "game:clothes-foot-fur-lined-reindeer-herder-shoes", ["cooling"] = 0.1
                    },
                    new JObject { ["itemname"] = "game:clothes-foot-high-leather-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-jailor-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-knee-high-fur-boots", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-foot-lackey-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-merchant-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-messenger-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-metalcap-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-minstrel-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-noble-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-peasent-slippers", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-prince-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-prisoner-binds", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-shepherd-sandals", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "game:clothes-foot-soldier-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-squire-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-nomad-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-temptress-velvet-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-tigh-high-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-wool-lined-knee-high-boots", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-foot-wool-lined-knee-high-boots", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-foot-worn-leather-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-blackguard-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-clockmaker-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-hunter-boots", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-foot-malefactor-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-forlorn-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-commoner-boots", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-tailor-shoes", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-marketeer", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-rotwalker", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-rottenking", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-king", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-surgeon", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-foot-alchemist", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-forgotten", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-survivor", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-foot-scribe", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-winter2", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-winter1", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-tailor", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-shepherd", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-peasantwhite", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-peasantbrown", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-peasantblue", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-peasantbeige", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-musician", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-miner-clean", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-innkeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-guard", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-fisher", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-blacksmith", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-beekeeper", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-barber", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-foot-alchemist", ["cooling"] = 4 },

                    new JObject { ["itemname"] = "game:clothes-face-leather-reinforced-mask", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-face-hunter-mask", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-face-malefactor-mask", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-forlorn-veil", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-glasses", ["cooling"] = 0.3 },
                    new JObject { ["itemname"] = "game:clothes-face-tailor-mask", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-sheepskull", ["cooling"] = 0.1 },
                    new JObject { ["itemname"] = "game:clothes-face-crow", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-hummingbird", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-corroded", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-cat", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-festivecat", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-marketeer", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-rotwalker", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-rottenking", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-surgeon", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-surgeonhood", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-miner", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-forgotten", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-face-survivor", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-face-snow-goggles", ["cooling"] = 0.4 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-face-alchemist", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-face-barber", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-face-blacksmith", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-face-hunter", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-face-miner", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "game:clothes-nadiya-face-grain", ["cooling"] = 2 },
                    new JObject
                    {
                        ["itemname"] = "eldritchcuteclothing:clothes-upperbody-kosovorotka-*", ["cooling"] = 2
                    },
                    new JObject { ["itemname"] = "eldritchcuteclothing:clothes-lowerbody-*", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-upperbodyover-*", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-upperbody-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-upperbody-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-shoulder-*", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-neck-*", ["cooling"] = 0.5 },
                    new JObject
                    {
                        ["itemname"] = "hideandfabric:clothes-lowerbody-homespun-cottonpants", ["cooling"] = 1.5
                    },
                    new JObject
                    {
                        ["itemname"] = "hideandfabric:clothes-lowerbody-homespun-woolenleggings", ["cooling"] = 0.5
                    },
                    new JObject { ["itemname"] = "hideandfabric:clothes-lowerbody-denim-pants", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-lowerbody-denim-shorts", ["cooling"] = 2.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-head-hidecap-*", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-head-brimmedhat-*", ["cooling"] = 3.5 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-hand-*", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "hideandfabric:clothes-foot-*", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "simplecloth:clothes-face-*", ["cooling"] = 0.2 },
                    new JObject { ["itemname"] = "simplecloth:clothes-foot-*", ["cooling"] = 4 },
                    new JObject { ["itemname"] = "simplecloth:clothes-lowerbody-furl*", ["cooling"] = 1 },
                    new JObject { ["itemname"] = "simplecloth:clothes-upperbodyover-furt*", ["cooling"] = 0.5 },
                    new JObject { ["itemname"] = "simplecloth:clothes-head-*", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "simplecloth:clothes-lowerbody-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "simplecloth:clothes-shoulder-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "simplecloth:clothes-upperbody-shirt-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "simplecloth:clothes-upperbody-tunic-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "simplecloth:clothes-upperbody-exomis-*", ["cooling"] = 2 },
                    new JObject { ["itemname"] = "simplecloth:clothes-upperbodyover-*", ["cooling"] = 1.5 },
                    new JObject { ["itemname"] = "simplecloth:clothes-waist-*", ["cooling"] = 0.3 }
                },
            };
            return defaultConfig;
        }
    }
}

