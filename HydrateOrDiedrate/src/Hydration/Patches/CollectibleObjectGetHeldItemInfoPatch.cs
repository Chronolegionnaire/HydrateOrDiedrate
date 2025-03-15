using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
    public static class CollectibleObjectGetHeldItemInfoPatch
    {
        private static bool ShouldSkipPatch()
        {
            return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
        }

        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (ShouldSkipPatch()) return;
            if (inSlot?.Itemstack == null) return;

            ItemStack stack = inSlot.Itemstack;
            bool isLiquidContainer = false;
            float hydrationValue = HydrationManager.GetHydration(stack);
            float containerHydrationValue = 0f;

            if (stack.Block is BlockLiquidContainerBase containerBlock)
            {
                isLiquidContainer = true;
                ItemStack contentStack = containerBlock.GetContent(stack);
                if (contentStack != null)
                {
                    float contentHyd = HydrationManager.GetHydration(contentStack);
                    float litres = containerBlock.GetCurrentLitres(stack);
                    containerHydrationValue = contentHyd * litres;
                }
            }

            string fullText = dsc.ToString();

            if (isLiquidContainer)
            {
                float finalHydValue = (containerHydrationValue != 0f) ? containerHydrationValue : hydrationValue;
                const string searchString = "When eaten:";
                int startIndex = fullText.IndexOf(searchString);

                if (startIndex >= 0)
                {
                    int endIndex = fullText.IndexOf('\n', startIndex);
                    if (endIndex < 0) endIndex = fullText.Length;

                    string oldLine = fullText.Substring(startIndex, endIndex - startIndex);
                    float? satVal = null;
                    float? hpVal = null;
                    string afterLabel = oldLine.Substring(searchString.Length).Trim();
                    string[] parts = afterLabel.Split(',');
                    foreach (string part in parts)
                    {
                        string p = part.Trim();
                        if (p.EndsWith("sat"))
                        {
                            string numeric = p.Substring(0, p.Length - 3).Trim();
                            if (float.TryParse(numeric, out float s)) satVal = s;
                        }
                        else if (p.EndsWith("hp"))
                        {
                            string numeric = p.Substring(0, p.Length - 2).Trim();
                            if (float.TryParse(numeric, out float h)) hpVal = h;
                        }
                    }
                    fullText = fullText.Remove(startIndex, endIndex - startIndex);
                    StringBuilder newLineSB = new StringBuilder(Lang.Get("hydrateordiedrate:hydrateordiedrate-whenDrunk"));
                    bool appendedValue = false;
                    if (satVal != null)
                    {
                        newLineSB.Append(" ");
                        newLineSB.Append(Lang.Get("hydrateordiedrate:hydrateordiedrate-sat", satVal));
                        appendedValue = true;
                    }
                    if (hpVal != null)
                    {
                        if (appendedValue) newLineSB.Append(", ");
                        newLineSB.Append(Lang.Get("hydrateordiedrate:hydrateordiedrate-hp", hpVal));
                        appendedValue = true;
                    }
                    if (finalHydValue != 0f)
                    {
                        if (appendedValue) newLineSB.Append(", ");
                        newLineSB.Append(Lang.Get("hydrateordiedrate:hydrateordiedrate-hyd", finalHydValue));
                        appendedValue = true;
                    }
                    fullText = fullText.Insert(startIndex, newLineSB.ToString());
                }
            }
            else
            {
                float normalHyd = hydrationValue;
                if (normalHyd != 0f)
                {
                    const string searchString = "When eaten:";
                    int startIndex = fullText.IndexOf(searchString);
                    if (startIndex >= 0)
                    {
                        int endIndex = fullText.IndexOf('\n', startIndex);
                        if (endIndex < 0) endIndex = fullText.Length;

                        string oldLine = fullText.Substring(startIndex, endIndex - startIndex);
                        if (!oldLine.Contains("hyd"))
                        {
                            fullText = fullText.Remove(startIndex, endIndex - startIndex);

                            StringBuilder newLineSB = new StringBuilder(Lang.Get("hydrateordiedrate:hydrateordiedrate-whenEaten"));
                            newLineSB.Append(" ");
                            string statsAfterLabel = oldLine.Substring(searchString.Length).TrimEnd();
                            newLineSB.Append(statsAfterLabel);
                            newLineSB.Append($", {Lang.Get("hydrateordiedrate:hydrateordiedrate-hyd", normalHyd)}");
                            fullText = fullText.Insert(startIndex, newLineSB.ToString());
                        }
                    }
                    else
                    {
                        fullText += $"\n{Lang.Get("hydrateordiedrate:hydrateordiedrate-whenEaten")} {Lang.Get("hydrateordiedrate:hydrateordiedrate-hyd", normalHyd)}";
                    }
                }
            }

            dsc.Clear();
            dsc.Append(fullText);
        }
    }
}
