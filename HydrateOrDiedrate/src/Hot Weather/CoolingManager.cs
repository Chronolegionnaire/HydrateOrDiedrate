using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate
{
    public static class CoolingManager
    {
        private static readonly Dictionary<string, float> ItemCoolingDict = new();

        public static void SetCooling(ICoreAPI api, string itemCode, float coolingValue)
        {
            ItemCoolingDict[itemCode] = coolingValue;
        }

        public static float GetCooling(ICoreAPI api, string itemCode)
        {
            if (ItemCoolingDict.TryGetValue(itemCode, out var value))
            {
                return value;
            }
            return 0f;
        }

        public static void ApplyCoolingPatches(ICoreAPI api, List<JObject> patches)
        {
            foreach (var collectible in api.World.Collectibles)
            {
                string itemName = collectible.Code?.ToString() ?? "Unknown Item";
                foreach (var patch in patches)
                {
                    string patchItemName = patch["itemname"]?.ToString();
                    if (IsMatch(itemName, patchItemName))
                    {
                        if (patch.ContainsKey("cooling"))
                        {
                            float cooling = patch["cooling"].ToObject<float>();
                            SetCooling(api, itemName, cooling);
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
