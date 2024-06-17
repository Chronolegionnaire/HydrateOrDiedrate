using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using System.Text;
using HydrateOrDiedrate;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(CollectibleObject), "GetHeldItemInfo")]
public static class GetHeldItemInfoPatch
{
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        // Hydration info for the held item itself
        string itemCode = inSlot.Itemstack.Collectible.Code.ToString();
        float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);

        // Check if the item is a container and get the contents' hydration value if applicable
        if (hydrationValue == 0 && inSlot.Itemstack.Block is BlockLiquidContainerBase block)
        {
            ItemStack contentStack = block.GetContent(inSlot.Itemstack);
            if (contentStack != null)
            {
                string contentItemCode = contentStack.Collectible.Code.ToString();
                float contentHydrationValue = HydrationManager.GetHydration(world.Api, contentItemCode);

                // Get the volume of the liquid in the container in liters
                float litres = block.GetCurrentLitres(inSlot.Itemstack);
                hydrationValue = contentHydrationValue * litres;
            }
        }

        // Update the existing "When eaten: ..." line or add it if it doesn't exist
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
