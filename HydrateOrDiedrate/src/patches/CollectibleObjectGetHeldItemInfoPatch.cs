using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches;

[HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
public static class CollectibleObjectGetHeldItemInfoPatch
{
    private static bool ShouldSkipPatch()
    {
        return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
    }

    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (ShouldSkipPatch())
        {
            return;
        }

        float hydrationValue = 0f;

        // Check hydration value directly from the ItemStack
        if (inSlot.Itemstack != null)
        {
            hydrationValue = HydrationManager.GetHydration(inSlot.Itemstack);
        }

        // If hydration value is 0 and the item is a liquid container, get content hydration
        if (hydrationValue == 0 && inSlot.Itemstack?.Block is BlockLiquidContainerBase block)
        {
            ItemStack contentStack = block.GetContent(inSlot.Itemstack);
            if (contentStack != null)
            {
                float contentHydrationValue = HydrationManager.GetHydration(contentStack);
                float litres = block.GetCurrentLitres(inSlot.Itemstack);
                hydrationValue = contentHydrationValue * litres;
            }
        }

        // Update the description with hydration value
        string existingText = dsc.ToString();
        string whenEatenLine = "When eaten: ";
        int startIndex = existingText.IndexOf(whenEatenLine);

        if (startIndex != -1)
        {
            int endIndex = existingText.IndexOf("\n", startIndex);
            if (endIndex == -1) endIndex = existingText.Length;
            string existingLine = existingText.Substring(startIndex, endIndex - startIndex);

            if (hydrationValue != 0 && !existingLine.Contains("hyd"))
            {
                string updatedLine = existingLine.TrimEnd() + $", {hydrationValue} hyd";
                dsc.Replace(existingLine, updatedLine);
            }
        }
        else if (hydrationValue != 0)
        {
            string hydrationText = Lang.Get("When eaten: {0} hyd", hydrationValue);
            dsc.AppendLine(hydrationText);
        }
    }
}
