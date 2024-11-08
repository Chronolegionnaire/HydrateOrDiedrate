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

namespace HydrateOrDiedrate.patches;

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
            if (itemWearable != null)
            {
                float cooling = CustomItemWearableExtensions.GetCooling(inSlot, world.Api);
                string itemCode = inSlot.Itemstack.Collectible.Code.ToString();
                float maxCooling = CoolingManager.GetCooling(world.Api, itemCode);
                string existingText = dsc.ToString();

                ensureConditionExistsMethod.Invoke(itemWearable, new object[] { inSlot });

                if (inSlot.Itemstack.Attributes.HasAttribute("condition"))
                {
                    float condition = inSlot.Itemstack.Attributes.GetFloat("condition", 1f);
                    if (float.IsNaN(condition))
                    {
                        condition = 0;
                    }
                    
                    float maxWarmth = inSlot.Itemstack.ItemAttributes["warmth"].AsFloat(0f);
                    float actualWarmth = maxWarmth * condition;
                    
                    if (maxWarmth > 0 || maxCooling > 0)
                    {
                        string updatedWarmthLine = $"Warmth:<font color=\"#ff8444\"> +{actualWarmth:0.#}°C</font>";
                        if (cooling != 0)
                        {
                            updatedWarmthLine += $", Cooling:<font color=\"#84dfff\"> +{cooling:0.#}°C</font>";
                        }
                        
                        if (!existingText.Contains(updatedWarmthLine))
                        {
                            string greenWarmthPattern = "<font color=\"#84ff84\">+";
                            string redWarmthPattern = "<font color=\"#ff8484\">+";
                            int warmthLineStart = existingText.IndexOf(greenWarmthPattern);
                            if (warmthLineStart == -1)
                            {
                                warmthLineStart = existingText.IndexOf(redWarmthPattern);
                            }

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

                        StringBuilder conditionCoolingLine = new StringBuilder();

                        if (!existingText.Contains("Condition:"))
                        {
                            string condStr = GetConditionString(condition);
                            string color = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, condition * 200f)]);
                            conditionCoolingLine.Append(Lang.Get("Condition:", Array.Empty<object>()) + " ");
                            conditionCoolingLine.Append($"<font color=\"{color}\">{condStr}</font>");
                        }

                        if (conditionCoolingLine.Length > 0)
                        {
                            dsc.AppendLine(conditionCoolingLine.ToString());
                            dsc.AppendLine();
                        }
                    }
                }
                
                if ((inSlot.Itemstack.ItemAttributes["warmth"].Exists || maxCooling > 0) && !existingText.Contains("Max Cooling:"))
                {
                    string maxWarmthLinePrefix = "Max warmth:";
                    int maxWarmthIndex = existingText.IndexOf(maxWarmthLinePrefix);

                    if (maxWarmthIndex != -1)
                    {
                        int endOfMaxWarmthLine = existingText.IndexOf("\n", maxWarmthIndex);
                        if (endOfMaxWarmthLine == -1) endOfMaxWarmthLine = existingText.Length;

                        string maxWarmthLine = existingText.Substring(maxWarmthIndex, endOfMaxWarmthLine - maxWarmthIndex).Trim();
                        string updatedMaxWarmthLine = $"{maxWarmthLine}";
                        
                        string maxCoolingText = $"Max Cooling: {maxCooling:0.#}°C";
                        updatedMaxWarmthLine += $" | {maxCoolingText}";

                        dsc.Replace(maxWarmthLine, updatedMaxWarmthLine);
                    }
                    else
                    {
                        string maxCoolingText = $"Max Cooling: {maxCooling:0.#}°C";
                        dsc.AppendLine(maxCoolingText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private static string GetConditionString(float condition)
    {
        if (condition > 0.5)
            return Lang.Get("clothingcondition-good", (int)(condition * 100f));
        if (condition > 0.4)
            return Lang.Get("clothingcondition-worn", (int)(condition * 100f));
        if (condition > 0.3)
            return Lang.Get("clothingcondition-heavilyworn", (int)(condition * 100f));
        if (condition > 0.2)
            return Lang.Get("clothingcondition-tattered", (int)(condition * 100f));
        if (condition > 0.1)
            return Lang.Get("clothingcondition-heavilytattered", (int)(condition * 100f));
        return Lang.Get("clothingcondition-terrible", (int)(condition * 100f));
    }
}