using System;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using HydrateOrDiedrate;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch(typeof(BlockCrock), "GetHeldItemInfo")]
    public static class BlockCrockPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            try
            {
                BlockCrock blockCrock = inSlot.Itemstack.Collectible as BlockCrock;
                ItemStack[] contentStacks = blockCrock?.GetNonEmptyContents(world, inSlot.Itemstack);
                if (contentStacks == null) return;

                float totalHydration = CalculateTotalHydration(world, contentStacks);

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

        private static float CalculateTotalHydration(IWorldAccessor world, ItemStack[] contentStacks)
        {
            float totalHydration = 0f;

            foreach (var stack in contentStacks)
            {
                if (stack != null)
                {
                    string itemCode = stack.Collectible.Code.ToString();
                    float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);
                    if (IsLiquidPortion(stack))
                    {
                        hydrationValue *= stack.StackSize * 0.01f;  // Adjust for liquid portions
                    }
                    else
                    {
                        hydrationValue *= stack.StackSize;
                    }
                    totalHydration += hydrationValue;
                }
            }

            return totalHydration;
        }

        private static bool IsLiquidPortion(ItemStack itemStack)
        {
            return itemStack?.Collectible?.GetType()?.Name == "ItemLiquidPortion";
        }
    }
}
