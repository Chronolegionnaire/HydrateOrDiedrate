using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using System.Text;
using HydrateOrDiedrate;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(BlockLiquidContainerBase), "GetContentInfo")]
public static class GetContentInfoPatch
{
    public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
    {
        BlockLiquidContainerBase block = inSlot.Itemstack.Block as BlockLiquidContainerBase;
        if (block == null) return;

        ItemStack contentStack = block.GetContent(inSlot.Itemstack);
        if (contentStack == null) return;

        string itemCode = contentStack.Collectible.Code.ToString();
        float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);

        string hydrationText = Lang.Get("When eaten: {0} Hyd", hydrationValue);

        if (hydrationValue > 0 && !dsc.ToString().Contains(hydrationText))
        {
            dsc.AppendLine(hydrationText);
        }
    }
}