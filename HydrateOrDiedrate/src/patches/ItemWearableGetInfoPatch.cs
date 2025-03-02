using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using HydrateOrDiedrate.Hot_Weather;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(ItemWearable), "GetHeldItemInfo")]
    public static class ItemWearableGetHeldItemInfoPatch
    {
        private static readonly MethodInfo ensureConditionExistsMethod =
            typeof(ItemWearable).GetMethod("ensureConditionExists", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            try
            {
                var itemWearable = inSlot.Itemstack.Collectible as ItemWearable;
                if (itemWearable == null) return;
                ItemStack itemStack = inSlot.Itemstack;
                ensureConditionExistsMethod?.Invoke(itemWearable, new object[] { inSlot });
                float condition = inSlot.Itemstack.Attributes.GetFloat("condition", 1f);
                if (float.IsNaN(condition))
                {
                    condition = 0f;
                }
                float maxWarmth = inSlot.Itemstack.ItemAttributes["warmth"].AsFloat(0f);
                if (float.IsNaN(maxWarmth))
                {
                    maxWarmth = 0f;
                }
                float actualWarmth = maxWarmth * condition;
                if (float.IsNaN(actualWarmth))
                {
                    actualWarmth = 0f;
                }
                float maxCooling = CoolingManager.GetMaxCooling(itemStack);
                if (float.IsNaN(maxCooling))
                {
                    maxCooling = 0f;
                }
                float actualCooling = maxCooling * condition;
                if (float.IsNaN(actualCooling))
                {
                    actualCooling = 0f;
                }

                string existingText = dsc.ToString();
                if (maxWarmth > 0 || maxCooling > 0)
                {
                    string updatedWarmthLine = $"Warmth:<font color=\"#ff8444\"> +{actualWarmth:0.#}°C</font>";
                    if (actualCooling != 0)
                    {
                        updatedWarmthLine += $", Cooling:<font color=\"#84dfff\"> +{actualCooling:0.#}°C</font>";
                    }
                    if (!existingText.Contains(updatedWarmthLine))
                    {
                        UpdateWarmthLine(dsc, existingText, updatedWarmthLine);
                    }
                    AppendConditionInfo(dsc, existingText, condition);
                }
                AppendMaxCoolingInfo(dsc, existingText, maxCooling, inSlot);
            }
            catch (Exception ex)
            {
                world.Logger.Warning($"Error in ItemWearableGetHeldItemInfoPatch: {ex}");
            }
        }

        private static void UpdateWarmthLine(StringBuilder dsc, string existingText, string updatedWarmthLine)
        {
            string greenWarmthPattern = "<font color=\"#84ff84\">+";
            string redWarmthPattern = "<font color=\"#ff8484\">+";
            int warmthLineStart = existingText.IndexOf(greenWarmthPattern);
            if (warmthLineStart == -1) warmthLineStart = existingText.IndexOf(redWarmthPattern);

            if (warmthLineStart != -1)
            {
                int endOfWarmthLine = existingText.IndexOf("\n", warmthLineStart);
                if (endOfWarmthLine == -1) endOfWarmthLine = existingText.Length;

                string warmthLine = existingText.Substring(warmthLineStart, endOfWarmthLine - warmthLineStart);
                dsc.Replace(warmthLine, updatedWarmthLine);
            }
            else
            {
                dsc.AppendLine(updatedWarmthLine);
            }
        }

        private static void AppendConditionInfo(StringBuilder dsc, string existingText, float condition)
        {
            if (!existingText.Contains("Condition:"))
            {
                string condStr = GetConditionString(condition);
                string color = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, condition * 200f)]);
                dsc.AppendLine(Lang.Get("Condition:") + $" <font color=\"{color}\">{condStr}</font>");
                dsc.AppendLine();
            }
        }

        private static string GetConditionString(float condition)
        {
            if (condition > 0.5f) return Lang.Get("clothingcondition-good", (int)(condition * 100f));
            if (condition > 0.4f) return Lang.Get("clothingcondition-worn", (int)(condition * 100f));
            if (condition > 0.3f) return Lang.Get("clothingcondition-heavilyworn", (int)(condition * 100f));
            if (condition > 0.2f) return Lang.Get("clothingcondition-tattered", (int)(condition * 100f));
            if (condition > 0.1f) return Lang.Get("clothingcondition-heavilytattered", (int)(condition * 100f));
            return Lang.Get("clothingcondition-terrible", (int)(condition * 100f));
        }

        private static void AppendMaxCoolingInfo(StringBuilder dsc, string existingText, float maxCooling, ItemSlot inSlot)
        {
            if ((inSlot.Itemstack.ItemAttributes["warmth"].Exists || maxCooling > 0) &&
                !existingText.Contains("Max Cooling:"))
            {
                string maxWarmthLinePrefix = "Max warmth:";
                int maxWarmthIndex = existingText.IndexOf(maxWarmthLinePrefix);

                if (maxWarmthIndex != -1)
                {
                    int endOfMaxWarmthLine = existingText.IndexOf("\n", maxWarmthIndex);
                    if (endOfMaxWarmthLine == -1) endOfMaxWarmthLine = existingText.Length;

                    string maxWarmthLine = existingText.Substring(maxWarmthIndex, endOfMaxWarmthLine - maxWarmthIndex).Trim();
                    string updatedMaxWarmthLine = $"{maxWarmthLine} | Max Cooling: {maxCooling:0.#}°C";

                    dsc.Replace(maxWarmthLine, updatedMaxWarmthLine);
                }
                else if (maxCooling > 0)
                {
                    dsc.AppendLine($"Max Cooling: {maxCooling:0.#}°C");
                }
            }
        }
    }
}
