using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using System.Text.RegularExpressions;

namespace HydrateOrDiedrate.Configuration
{
    public static class CoolingConfigLoader
    {
        public static List<JObject> LoadCoolingPatches(ICoreAPI api)
        {
            List<JObject> allPatches = new List<JObject>();
            string configFolder = ModConfig.GetConfigPath(api);
            List<string> configFiles = Directory.GetFiles(configFolder, "*AddCooling*.json").ToList();
            string defaultConfigPath = Path.Combine(configFolder, "HoD.AddCooling.json");
            if (!File.Exists(defaultConfigPath))
            {
                GenerateDefaultCoolingConfig(api);
            }

            configFiles.Insert(0, defaultConfigPath);
            var sortedPatches = new SortedDictionary<int, List<JObject>>();

            foreach (string file in configFiles)
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

            Dictionary<string, JObject> mergedPatches = new Dictionary<string, JObject>();

            foreach (var priorityLevel in sortedPatches.Keys.OrderByDescending(k => k))
            {
                foreach (var patch in sortedPatches[priorityLevel])
                {
                    string itemname = patch["itemname"].ToString();
                    mergedPatches[itemname] = patch;
                }
            }

            // Detect items from all domains
            var allItemCodes = api.World.Collectibles
                .Where(c => c is Item || c is Block)
                .Select(c => c.Code.ToString())
                .ToList();

            List<JObject> finalPatches = new List<JObject>();

            foreach (var patch in mergedPatches.Values)
            {
                string itemname = patch["itemname"].ToString();
                if (itemname.Contains("*"))
                {
                    string pattern = "^" + Regex.Escape(itemname).Replace("\\*", ".*") + "$";
                    Regex regex = new Regex(pattern);

                    foreach (var code in allItemCodes)
                    {
                        if (regex.IsMatch(code))
                        {
                            JObject newPatch = new JObject(patch);
                            newPatch["itemname"] = code;
                            finalPatches.Add(newPatch);
                        }
                    }
                }
                else
                {
                    if (itemname == "eldritchcuteclothing:clothes-kosovorotka-blue")
                    {
                        if (allItemCodes.Contains(itemname))
                        {
                            finalPatches.Add(patch);
                        }
                    }
                    else
                    {
                        if (allItemCodes.Contains(itemname))
                        {
                            finalPatches.Add(patch);
                        }
                    }
                }
            }

            return finalPatches;
        }

        public static void GenerateDefaultCoolingConfig(ICoreAPI api)
        {
            string configPath = Path.Combine(ModConfig.GetConfigPath(api), "HoD.AddCooling.json");
            if (!File.Exists(configPath))
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
                        new JObject { ["itemname"] = "game:clothes-upperbody-reindeer-herder-collared-shirt", ["cooling"] = 1.5 },
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
                        new JObject { ["itemname"] = "game:clothes-upperbodyover-cerise-embroidered-reindeer-herder-coat", ["cooling"] = 1 },
                        new JObject { ["itemname"] = "game:clothes-upperbodyover-commoner-coat", ["cooling"] = 1.5 },
                        new JObject { ["itemname"] = "game:clothes-upperbodyover-fur-coat", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-upperbodyover-huntsmans-tunic", ["cooling"] = 1.5 },
                        new JObject { ["itemname"] = "game:clothes-upperbodyover-cobalt-mantle", ["cooling"] = 2 },
                        new JObject { ["itemname"] = "game:clothes-upperbodyover-reindeer-herder-fur-coat", ["cooling"] = 0.5 },
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
                        new JObject { ["itemname"] = "game:clothes-waist-aristocrat-belt", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-waist-cerise-embroidered-reindeer-herder-waistband", ["cooling"] = 0.5 },
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
                        new JObject { ["itemname"] = "game:clothes-shoulder-shortsleeve-pearl-moonrobe", ["cooling"] = 1.5 },
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
                        new JObject { ["itemname"] = "game:clothes-head-tophat", ["cooling"] = 2 },
                        new JObject { ["itemname"] = "game:clothes-head-bamboo-conehat", ["cooling"] = 4 },
                        new JObject { ["itemname"] = "game:clothes-head-bamboo-conehat-large", ["cooling"] = 4 },
                        new JObject { ["itemname"] = "game:clothes-head-crown", ["cooling"] = 1 },
                        new JObject { ["itemname"] = "game:clothes-head-mianguan", ["cooling"] = 2 },
                        new JObject { ["itemname"] = "game:clothes-head-marketeer", ["cooling"] = 2 },
                        new JObject { ["itemname"] = "game:clothes-head-rotwalker", ["cooling"] = 2 },
                        new JObject { ["itemname"] = "game:clothes-head-rottenking", ["cooling"] = 1 },
                        new JObject { ["itemname"] = "game:clothes-head-surgeon", ["cooling"] = 2 },
                        new JObject { ["itemname"] = "game:clothes-head-alchemist", ["cooling"] = 2 },
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
                        new JObject { ["itemname"] = "game:clothes-foot-aristocrat-shoes", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-foot-great-steppe-boots", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-foot-fur-lined-reindeer-herder-shoes", ["cooling"] = 0.1 },
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
                        new JObject { ["itemname"] = "game:clothes-foot-miner", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-foot-alchemist", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-foot-forgotten", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-foot-survivor", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-foot-scribe", ["cooling"] = 0.5 },
                        new JObject { ["itemname"] = "game:clothes-face-leather-reinforced-mask", ["cooling"] = 0.1 },
                        new JObject { ["itemname"] = "game:clothes-face-hunter-mask", ["cooling"] = 0.2 },
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
                        new JObject { ["itemname"] = "game:clothes-face-miner", ["cooling"] = 0.2 },
                        new JObject { ["itemname"] = "game:clothes-face-forgotten", ["cooling"] = 0.2 },
                        new JObject { ["itemname"] = "game:clothes-face-survivor", ["cooling"] = 0.2 },
                        new JObject { ["itemname"] = "game:clothes-face-snow-goggles", ["cooling"] = 0.4 }
                    },
                };
                    File.WriteAllText(configPath, defaultConfig.ToString());
            }
        }
    }
}
