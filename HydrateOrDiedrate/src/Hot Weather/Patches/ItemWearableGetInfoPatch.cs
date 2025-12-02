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
        private const string ModLangPrefix = "hydrateordiedrate:";
        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            try
            {
                var itemWearable = inSlot.Itemstack?.Collectible as ItemWearable;
                if (itemWearable == null) return;
                ItemStack itemStack = inSlot.Itemstack;
                ensureConditionExistsMethod?.Invoke(itemWearable, new object[] { inSlot, true });
                float condition = inSlot.Itemstack.Attributes.GetFloat("condition", 1f);
                if (float.IsNaN(condition)) condition = 0f;
                float maxWarmth = inSlot.Itemstack.ItemAttributes["warmth"].AsFloat(0f);
                if (float.IsNaN(maxWarmth)) maxWarmth = 0f;
                float actualWarmth = Math.Min(maxWarmth, condition * 2f * maxWarmth);
                if (float.IsNaN(actualWarmth)) actualWarmth = 0f;
                float maxCooling = CoolingManager.GetMaxCooling(itemStack);
                if (float.IsNaN(maxCooling)) maxCooling = 0f;
                float actualCooling = Math.Min(maxCooling, condition * 2f * maxCooling);
                if (float.IsNaN(actualCooling)) actualCooling = 0f;
                if (maxWarmth > 0 || maxCooling > 0)
                {
                    string warmthLabel = Lang.Get(ModLangPrefix + "itemwearable-warmth");
                    string coolingLabel = Lang.Get(ModLangPrefix + "itemwearable-cooling");
                    string warmthValue = Lang.Get("+{0:0.#}°C", actualWarmth);
                    string coolingValue = Lang.Get("hydrateordiedrate:cooling-format", actualCooling);

                    string updatedWarmthLine =
                        $"{warmthLabel}:<font color=\"#ff8444\"> {warmthValue}</font>";

                    if (actualCooling != 0)
                    {
                        updatedWarmthLine +=
                            $", {coolingLabel}:<font color=\"#84dfff\"> {coolingValue}</font>";
                    }
                    string existingText = dsc.ToString();

                    if (!existingText.Contains(updatedWarmthLine))
                    {
                        UpdateWarmthLine(dsc, existingText, updatedWarmthLine);
                    }
                    existingText = dsc.ToString();
                    AppendConditionInfo(dsc, existingText, condition);
                    existingText = dsc.ToString();
                    AppendMaxCoolingInfo(dsc, existingText, maxCooling, inSlot);
                }
                else
                {
                    string existingText = dsc.ToString();
                    AppendConditionInfo(dsc, existingText, condition);
                    existingText = dsc.ToString();
                    AppendMaxCoolingInfo(dsc, existingText, maxCooling, inSlot);
                }
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

            int warmthLineStart = existingText.IndexOf(greenWarmthPattern, StringComparison.Ordinal);
            if (warmthLineStart == -1)
                warmthLineStart = existingText.IndexOf(redWarmthPattern, StringComparison.Ordinal);

            if (warmthLineStart != -1)
            {
                int endOfWarmthLine = existingText.IndexOf("\n", warmthLineStart, StringComparison.Ordinal);
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
            string conditionLabel = Lang.Get("Condition:");

            if (!existingText.Contains(conditionLabel))
            {
                string condStr = GetConditionString(condition);
                string color = ColorUtil.Int2Hex(
                    GuiStyle.DamageColorGradient[(int)Math.Min(99f, condition * 200f)]
                );

                dsc.AppendLine(conditionLabel + $" <font color=\"{color}\">{condStr}</font>");
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
            float maxWarmth = inSlot.Itemstack.ItemAttributes["warmth"].AsFloat(0f);
            string maxCoolingLabel = Lang.Get(ModLangPrefix + "itemwearable-maxcooling");
            if ((maxWarmth != 0f || maxCooling > 0f) &&
                !existingText.Contains(maxCoolingLabel))
            {
                string maxWarmthLine = null;
                if (maxWarmth != 0f)
                {
                    maxWarmthLine = Lang.Get("clothing-maxwarmth", maxWarmth);
                }
                if (!string.IsNullOrEmpty(maxWarmthLine))
                {
                    int index = existingText.IndexOf(maxWarmthLine, StringComparison.Ordinal);
                    if (index != -1)
                    {
                        int endOfLine = existingText.IndexOf("\n", index, StringComparison.Ordinal);
                        if (endOfLine == -1) endOfLine = existingText.Length;
                        string fullLine = existingText.Substring(index, endOfLine - index)
                            .TrimEnd('\r', '\n');
                        string coolingValue = Lang.Get("hydrateordiedrate:cooling-format", maxCooling);
                        string updatedLine =
                            $"<font size=\"15\">{fullLine} | {maxCoolingLabel}: {coolingValue}</font>";
                        dsc.Replace(fullLine, updatedLine);
                        return;
                    }
                }
                if (maxCooling > 0f)
                {
                    string coolingValue = Lang.Get("hydrateordiedrate:cooling-format", maxCooling);
                    dsc.AppendLine($"<font size=\"15\">{maxCoolingLabel}: {coolingValue}</font>");
                }
            }
        }
    }
}