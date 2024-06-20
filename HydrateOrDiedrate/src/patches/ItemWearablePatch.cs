using HarmonyLib;
using Vintagestory.API.Common;
using System.Text;
using HydrateOrDiedrate;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(ItemWearable), "GetHeldItemInfo")]
public static class ItemWearableGetHeldItemInfoPatch
{
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        var itemWearable = inSlot.Itemstack.Collectible as ItemWearable;
        if (itemWearable != null)
        {
            float cooling = CustomItemWearableExtensions.GetCooling(inSlot, world.Api);
            string itemCode = inSlot.Itemstack.Collectible.Code.ToString();
            float maxCooling = CoolingManager.GetCooling(world.Api, itemCode);
            string existingText = dsc.ToString();
            string warmthLinePrefix = "<font color=\"#84ff84\">+";
            int warmthIndex = existingText.IndexOf(warmthLinePrefix);
            if (warmthIndex != -1)
            {
                int endOfWarmthLine = existingText.IndexOf("\n", warmthIndex);
                if (endOfWarmthLine == -1) endOfWarmthLine = existingText.Length;

                string warmthLine = existingText.Substring(warmthIndex, endOfWarmthLine - warmthIndex).Trim();
                string warmthValue = warmthLine.Substring(warmthLinePrefix.Length).Split('°')[0].Trim();
                string updatedWarmthLine = $"<font color=\"#ff8444\">Warmth: +{warmthValue}°C</font>";
                if (cooling != 0)
                {
                    string coolingText = $"<font color=\"#84dfff\"> Cooling: +{cooling:0.#}°C</font>";
                    updatedWarmthLine += "," + coolingText;
                }

                dsc.Replace(warmthLine, updatedWarmthLine);
                string maxWarmthLinePrefix = "Max warmth:";
                int maxWarmthIndex = existingText.IndexOf(maxWarmthLinePrefix);

                if (maxWarmthIndex != -1)
                {
                    int endOfMaxWarmthLine = existingText.IndexOf("\n", maxWarmthIndex);
                    if (endOfMaxWarmthLine == -1) endOfMaxWarmthLine = existingText.Length;
                    if (maxCooling != 0)
                    {
                        string maxCoolingText = Lang.Get("Max Cooling: {0:0.#}°C", maxCooling);
                        string maxWarmthLine = existingText.Substring(maxWarmthIndex, endOfMaxWarmthLine - maxWarmthIndex).Trim();
                        string updatedMaxWarmthLine = $"{maxWarmthLine} | {maxCoolingText}";

                        if (maxCooling != 0 && !existingText.Contains("Max Cooling:"))
                            dsc.Replace(maxWarmthLine, updatedMaxWarmthLine);
                    }
                }
                else if (maxCooling != 0)
                {
                    dsc.AppendLine(Lang.Get("Max Cooling: {0:0.#}°C", maxCooling));
                }
            }
        }
    }
}
