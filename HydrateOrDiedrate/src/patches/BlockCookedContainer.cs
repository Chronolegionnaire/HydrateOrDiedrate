using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using HydrateOrDiedrate;

namespace HydrateOrDiedrate
{
    [HarmonyPatch(typeof(BlockCookedContainer), "GetHeldItemInfo")]
    public static class BlockCookedContainerGetHeldItemInfoPatch
    {
        public static void Postfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            try
            {
                BlockCookedContainer blockCookedContainer = inSlot.Itemstack.Collectible as BlockCookedContainer;
                ItemStack[] contentStacks = blockCookedContainer?.GetNonEmptyContents(world, inSlot.Itemstack);
                if (contentStacks == null) return;

                float totalHydration = 0f;
                foreach (ItemStack contentStack in contentStacks)
                {
                    if (contentStack != null)
                    {
                        string itemCode = contentStack.Collectible.Code.ToString();
                        float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);
                        if (IsLiquidPortion(contentStack))
                        {
                            hydrationValue *= contentStack.StackSize * 0.01f;  // Adjust for liquid portions
                        }
                        else
                        {
                            hydrationValue *= contentStack.StackSize;
                        }
                        totalHydration += hydrationValue;
                        world.Logger.Notification($"[HydrateOrDiedrate] {itemCode} provides {hydrationValue} hydration in total for stack size: {contentStack.StackSize}.");
                    }
                }

                float servings = blockCookedContainer.GetQuantityServings(world, inSlot.Itemstack);
                world.Logger.Notification($"[HydrateOrDiedrate] Total hydration: {totalHydration}, servings: {servings}.");

                if (servings > 0)
                {
                    float hydrationPerServing = totalHydration / servings;
                    string hydrationText = Lang.Get("Per Serving: {0} Hyd", hydrationPerServing);
                    if (!dsc.ToString().Contains(hydrationText))
                    {
                        dsc.AppendLine(hydrationText);
                        world.Logger.Notification($"[HydrateOrDiedrate] Total hydration per serving: {hydrationPerServing}.");
                    }
                }
            }
            catch (Exception ex)
            {
                world.Logger.Error($"[HydrateOrDiedrate] Error in BlockCookedContainerGetHeldItemInfoPatch: {ex}");
            }
        }

        private static bool IsLiquidPortion(ItemStack itemStack)
        {
            return itemStack?.Collectible?.GetType()?.Name == "ItemLiquidPortion";
        }
    }
}
