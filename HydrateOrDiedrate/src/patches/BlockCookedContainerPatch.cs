using System;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate
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

                string hydrationText = Lang.Get("Hydration Per Serving: {0}", totalHydration);
                if (!dsc.ToString().Contains(hydrationText))
                {
                    dsc.AppendLine(hydrationText);
                }
            }
            catch (Exception ex)
            {
                world.Logger.Error($"[HydrateOrDiedrate] Error in BlockCookedContainerGetHeldItemInfoPatch: {ex}");
            }
        }
    }
}