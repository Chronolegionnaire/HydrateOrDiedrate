using System;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch(typeof(BlockCrock), "GetHeldItemInfo")]
    public static class BlockCrockPatch
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
                BlockCrock blockCrock = inSlot.Itemstack.Collectible as BlockCrock;
                ItemStack[] contentStacks = blockCrock?.GetNonEmptyContents(world, inSlot.Itemstack);
                if (contentStacks == null) return;

                float totalHydration = HydrationCalculator.GetTotalHydration(world, contentStacks);

                if (totalHydration != 0)
                {
                    string hydrationText = Lang.Get("Hydration Per Serving: {0}", totalHydration);
                    if (!dsc.ToString().Contains(hydrationText))
                    {
                        dsc.AppendLine(hydrationText);
                    }
                }
            }
            catch (Exception ex)
            {
                world.Logger.Error($"[HydrateOrDiedrate] Error in BlockCrockPatch: {ex}");
            }
        }
    }
}