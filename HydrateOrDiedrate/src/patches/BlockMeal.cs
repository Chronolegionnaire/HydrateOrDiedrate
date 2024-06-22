using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using System.Text;
using Vintagestory.API.Config;
using System;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch(typeof(BlockMeal))]
    public static class BlockMealPatches
    {
        [HarmonyPatch("Consume")]
        [HarmonyPostfix]
        public static void ConsumePostfix(IWorldAccessor world, IPlayer eatingPlayer, ItemSlot inSlot, ItemStack[] contentStacks, ref float __result)
        {
            world.Logger.Notification("[HydrateOrDiedrate] ConsumePostfix called.");
            float totalHydration = CalculateTotalHydration(world, contentStacks);

            if (totalHydration != 0)
            {
                float servingsConsumed = __result; // The amount of servings consumed
                float hydrationPerServing = totalHydration / (inSlot.Itemstack.Collectible as BlockMeal).GetQuantityServings(world, inSlot.Itemstack); 
                float totalHydrationConsumed = hydrationPerServing * servingsConsumed; // Total hydration for the consumed servings
                world.Logger.Notification($"[HydrateOrDiedrate] Total Hydration: {totalHydration}, Servings Consumed: {servingsConsumed}, Hydration Per Serving: {hydrationPerServing}, Total Hydration Consumed: {totalHydrationConsumed}");

                EntityBehaviorThirst thirstBehavior = eatingPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
                if (thirstBehavior != null)
                {
                    thirstBehavior.ModifyThirst(totalHydrationConsumed); // Use ModifyThirst to add calculated total hydration
                    world.Logger.Notification($"[Thirst] Added {totalHydrationConsumed} hydration in total. Current Thirst: {thirstBehavior.CurrentThirst}");
                }
            }
        }

        [HarmonyPatch("GetHeldItemInfo")]
        [HarmonyPostfix]
        public static void GetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            world.Logger.Notification("[HydrateOrDiedrate] GetHeldItemInfoPostfix called.");
            ItemStack[] contentStacks = (inSlot.Itemstack.Collectible as BlockMeal)?.GetNonEmptyContents(world, inSlot.Itemstack);
            if (contentStacks == null)
            {
                world.Logger.Notification("[HydrateOrDiedrate] No content stacks found.");
                return;
            }

            float totalHydration = CalculateTotalHydration(world, contentStacks);
            if (totalHydration != 0)
            {
                float servings = (inSlot.Itemstack.Collectible as BlockMeal).GetQuantityServings(world, inSlot.Itemstack);
                float hydrationPerServing = totalHydration / servings;
                world.Logger.Notification($"[HydrateOrDiedrate] Total Hydration: {totalHydration}, Servings: {servings}, Hydration Per Serving: {hydrationPerServing}");

                string hydrationText = $"Hydration Per Serving {hydrationPerServing}";
                if (!dsc.ToString().Contains(hydrationText))
                {
                    dsc.AppendLine(hydrationText);
                    world.Logger.Notification($"[HydrateOrDiedrate] Added new description line: {hydrationText}");
                }
            }
        }

        private static float CalculateTotalHydration(IWorldAccessor world, ItemStack[] contentStacks)
        {
            float totalHydration = 0f;

            foreach (ItemStack contentStack in contentStacks)
            {
                if (contentStack != null)
                {
                    string itemCode = contentStack.Collectible.Code.ToString();
                    world.Logger.Notification($"[HydrateOrDiedrate] Processing item: {itemCode}, StackSize: {contentStack.StackSize}");
                    float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);
                    world.Logger.Notification($"[HydrateOrDiedrate] Hydration value for {itemCode}: {hydrationValue}");

                    // Check if the item name contains the word "portion"
                    if (itemCode.ToLower().Contains("portion"))
                    {
                        WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(contentStack);
                        if (props != null && props.ItemsPerLitre > 0)
                        {
                            float litres = (float)contentStack.StackSize / props.ItemsPerLitre;
                            hydrationValue *= litres;
                            world.Logger.Notification($"[HydrateOrDiedrate] {itemCode} is detected as a liquid portion. Litres: {litres}, Adjusted Hydration: {hydrationValue}");
                        }
                        else
                        {
                            world.Logger.Notification($"[HydrateOrDiedrate] {itemCode} props.ItemsPerLitre is null or zero, falling back to default method.");
                        }
                    }

                    if (hydrationValue != 0)
                    {
                        totalHydration += hydrationValue;
                        world.Logger.Notification($"[HydrateOrDiedrate] Total hydration for {itemCode}: {totalHydration}");
                    }
                }
                else
                {
                    world.Logger.Notification("[HydrateOrDiedrate] Found null contentStack.");
                }
            }

            return totalHydration;
        }
    }
}
