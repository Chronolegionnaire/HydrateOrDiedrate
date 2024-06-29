using System;
using HarmonyLib;
using Vintagestory.API.Common;
using System.Text;
using HydrateOrDiedrate;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

[HarmonyPatch(typeof(ItemWearable), "GetHeldItemInfo")]
public static class ItemWearableGetHeldItemInfoPatch
{
    private static readonly MethodInfo ensureConditionExistsMethod = typeof(ItemWearable).GetMethod("ensureConditionExists", BindingFlags.NonPublic | BindingFlags.Instance);

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
                // Item has warmth
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

                        if (!existingText.Contains("Max Cooling:"))
                            dsc.Replace(maxWarmthLine, updatedMaxWarmthLine);
                    }
                }
                else if (maxCooling != 0)
                {
                    if (!existingText.Contains("Max Cooling:"))
                    {
                        dsc.AppendLine(Lang.Get("Max Cooling: {0:0.#}°C", maxCooling));
                    }
                }
            }
            else
            {
                // Item does not have warmth
                StringBuilder conditionCoolingLine = new StringBuilder();

                // Ensure condition exists if the item should have it
                ensureConditionExistsMethod.Invoke(itemWearable, new object[] { inSlot });
                if (inSlot.Itemstack.Attributes.HasAttribute("condition") && !existingText.Contains("Condition:"))
                {
                    float condition = inSlot.Itemstack.Attributes.GetFloat("condition", 1f);
                    string condStr;
                    if ((double)condition > 0.5)
                    {
                        condStr = Lang.Get("clothingcondition-good", new object[]
                        {
                            (int)(condition * 100f)
                        });
                    }
                    else if ((double)condition > 0.4)
                    {
                        condStr = Lang.Get("clothingcondition-worn", new object[]
                        {
                            (int)(condition * 100f)
                        });
                    }
                    else if ((double)condition > 0.3)
                    {
                        condStr = Lang.Get("clothingcondition-heavilyworn", new object[]
                        {
                            (int)(condition * 100f)
                        });
                    }
                    else if ((double)condition > 0.2)
                    {
                        condStr = Lang.Get("clothingcondition-tattered", new object[]
                        {
                            (int)(condition * 100f)
                        });
                    }
                    else if ((double)condition > 0.1)
                    {
                        condStr = Lang.Get("clothingcondition-heavilytattered", new object[]
                        {
                            (int)(condition * 100f)
                        });
                    }
                    else
                    {
                        condStr = Lang.Get("clothingcondition-terrible", new object[]
                        {
                            (int)(condition * 100f)
                        });
                    }
                    string color = ColorUtil.Int2Hex(GuiStyle.DamageColorGradient[(int)Math.Min(99f, condition * 200f)]);
                    conditionCoolingLine.Append(Lang.Get("Condition:", Array.Empty<object>()) + " ");
                    conditionCoolingLine.Append($"<font color=\"{color}\">{condStr}</font>");
                }

                if (!existingText.Contains("Cooling:") && cooling != 0)
                {
                    string coolingText = $"<font color=\"#84dfff\"> Cooling: +{cooling:0.#}°C</font>";
                    if (conditionCoolingLine.Length > 0)
                    {
                        conditionCoolingLine.Append(", ");
                    }
                    conditionCoolingLine.Append(coolingText);
                }

                if (conditionCoolingLine.Length > 0)
                {
                    dsc.AppendLine(conditionCoolingLine.ToString());
                    dsc.AppendLine();  // Add an empty line after condition and cooling
                }

                if (!existingText.Contains("Max Cooling:") && maxCooling != 0)
                {
                    string maxCoolingText = Lang.Get("Max Cooling: {0:0.#}°C", maxCooling);
                    dsc.AppendLine(maxCoolingText);
                }
            }
        }
    }
}
