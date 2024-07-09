using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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
                        
                        var item = api.World.GetItem(new AssetLocation(itemName));
                        if (item == null)
                        {
                            continue;
                        }

                        if (item.Attributes == null)
                        {
                            continue;
                        }

                        float currentWarmth = item.Attributes["warmth"]?.AsFloat(0f) ?? 0f;
                        if (currentWarmth <= 0f)
                        {
                            var clonedAttributes = item.Attributes.Token.DeepClone() as JObject;
                            if (clonedAttributes != null)
                            {
                                clonedAttributes["warmth"] = 0.01f;
                                item.Attributes = new JsonObject(clonedAttributes);
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
                string pattern = "^" + Regex.Escape(patchItemName).Replace("\\*", ".*") + "$";
                return Regex.IsMatch(itemName, pattern);
            }
            return itemName == patchItemName;
        }
    }
}
