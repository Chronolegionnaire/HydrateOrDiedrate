using HarmonyLib;
using HydrateOrDiedrate.Config;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(BlockCrock), "GetHeldItemInfo")]
    public static class BlockCrockPatch
    {
        private static bool ShouldSkipPatch() => !ModConfig.Instance.Thirst.Enabled;

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
                    string hydrationText = Lang.Get("hydrateordiedrate:blockcrock-hydration", totalHydration);
                    if (!dsc.ToString().Contains(hydrationText))
                    {
                        dsc.AppendLine(hydrationText);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}