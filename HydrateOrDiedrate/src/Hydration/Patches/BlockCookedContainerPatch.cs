using System;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(BlockCookedContainer), "GetHeldItemInfo")]
    public static class BlockCookedContainerGetHeldItemInfoPatch
    {
        private static bool ShouldSkipPatch()
        {
            return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
        }

        [HarmonyPostfix]
        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (ShouldSkipPatch())
            {
                return;
            }
            try
            {
                BlockCookedContainer blockCookedContainer = inSlot.Itemstack.Collectible as BlockCookedContainer;
                ItemStack[] contentStacks = blockCookedContainer?.GetNonEmptyContents(world, inSlot.Itemstack);
                if (contentStacks == null) return;

                float totalHydration = HydrationCalculator.GetTotalHydration(world, contentStacks);

                string hydrationText = Lang.Get("hydrateordiedrate:blockcookedcontainer-hydration", totalHydration);
                if (!dsc.ToString().Contains(hydrationText))
                {
                    dsc.AppendLine(hydrationText);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}