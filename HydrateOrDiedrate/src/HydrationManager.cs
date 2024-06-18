using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate
{
    public static class HydrationManager
    {
        private static readonly Dictionary<string, float> ItemHydrationDict = new();

        public static void SetHydration(ICoreAPI api, string itemCode, float hydrationValue)
        {
            ItemHydrationDict[itemCode] = hydrationValue;
        }

        public static float GetHydration(ICoreAPI api, string itemCode)
        {
            return ItemHydrationDict.TryGetValue(itemCode, out var value) ? value : 0f;
        }

        public static void ApplyHydrationPatches(ICoreAPI api, List<JObject> patches)
        {
            foreach (var collectible in api.World.Collectibles)
            {
                string itemName = collectible.Code?.ToString() ?? "Unknown Item";
                foreach (var patch in patches)
                {
                    string patchItemName = patch["itemname"]?.ToString();
                    if (IsMatch(itemName, patchItemName))
                    {
                        if (patch.ContainsKey("hydration"))
                        {
                            float hydration = patch["hydration"].ToObject<float>();
                            SetHydration(api, itemName, hydration);
                        }
                        else if (patch.ContainsKey("hydrationByType"))
                        {
                            var hydrationByType = patch["hydrationByType"].ToObject<Dictionary<string, float>>();
                            foreach (var entry in hydrationByType)
                            {
                                string key = entry.Key;
                                float hydration = entry.Value;

                                if (IsMatch(itemName, key))
                                {
                                    SetHydration(api, itemName, hydration);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsMatch(string itemName, string patchItemName)
        {
            if (string.IsNullOrEmpty(patchItemName)) return false;
            if (patchItemName.Contains("*"))
            {
                string pattern = "^" + patchItemName.Replace("*", ".*") + "$";
                return System.Text.RegularExpressions.Regex.IsMatch(itemName, pattern);
            }
            return itemName == patchItemName;
        }
    }
}
