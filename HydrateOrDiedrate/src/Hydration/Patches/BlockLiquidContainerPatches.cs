using HarmonyLib;
using HydrateOrDiedrate.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hydration.Patches;

[HarmonyPatch]
public static class BlockLiquidContainerPatches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.SplitStackAndPerformAction))]
    [HarmonyPrefix]
    public static bool StopStackSplitIfLockedDummySlot(Entity byEntity, ItemSlot slot, Func<ItemStack, int> action, ref int __result)
    {
        if(slot is LockedDummySlot)
        {
            __result = 0;
            if (slot.Itemstack is not null)
			{
                __result = action(slot.Itemstack);
			}

            return false;
        }

        return true;
    }
}
