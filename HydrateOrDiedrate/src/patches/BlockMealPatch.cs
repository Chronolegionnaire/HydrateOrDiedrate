using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Configuration;
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
        private static bool ShouldSkipPatch()
        {
            return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
        }
        static bool alreadyCalled = false;
        static float capturedTotalHydration;
        static float capturedHydLossDelay;
        static EntityPlayer capturedPlayer;
        static float capturedServingsInMeal;
        static float servingsBeforeConsume;

        [HarmonyPatch("tryFinishEatMeal")]
        [HarmonyPrefix]
        public static void TryFinishEatMealPrefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
        {
            if (ShouldSkipPatch())
            {
                return;
            }
            alreadyCalled = false;
            capturedTotalHydration = 0;
            capturedHydLossDelay = 0;
            capturedPlayer = null;
            servingsBeforeConsume = 0;
            capturedServingsInMeal = 0;

            var api = byEntity?.World?.Api;
            if (api == null || api.Side != EnumAppSide.Server) return;

            float totalHydration = CalculateTotalHydration(byEntity.World, (slot.Itemstack.Collectible as BlockMeal).GetNonEmptyContents(byEntity.World, slot.Itemstack));
            if (totalHydration != 0)
            {
                capturedTotalHydration = totalHydration;
                capturedPlayer = byEntity as EntityPlayer;
                if (capturedPlayer == null) return;

                servingsBeforeConsume = (slot.Itemstack.Collectible as BlockMeal).GetQuantityServings(byEntity.World, slot.Itemstack);
                capturedServingsInMeal = servingsBeforeConsume;

                var config = HydrateOrDiedrateModSystem.LoadedConfig;
                float configHydrationLossDelayMultiplier = config.HydrationLossDelayMultiplier;

                float capturedHydrationAmount = totalHydration / capturedServingsInMeal;
                capturedHydLossDelay = (capturedHydrationAmount/2) * configHydrationLossDelayMultiplier;
            }
        }

        [HarmonyPatch("tryFinishEatMeal")]
        [HarmonyPostfix]
        public static void TryFinishEatMealPostfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
        {
            if (ShouldSkipPatch())
            {
                return;
            }
            if (alreadyCalled) return;
            alreadyCalled = true;

            var api = byEntity?.World?.Api;
            if (api == null || api.Side != EnumAppSide.Server) return;

            try
            {
                if (capturedPlayer == null)
                {
                    return;
                }
                if (capturedTotalHydration == 0 || capturedServingsInMeal == 0)
                {
                    return;
                }

                float servingsAfterConsume = (slot.Itemstack.Collectible as BlockMeal)?.GetQuantityServings(byEntity.World, slot.Itemstack) ?? 0;
                float servingsConsumed = servingsBeforeConsume - servingsAfterConsume;

                if (servingsConsumed <= 0)
                {
                    return;
                }

                float hydrationPerServing = capturedTotalHydration / capturedServingsInMeal;
                float totalHydrationConsumed = hydrationPerServing * servingsConsumed;

                var config = HydrateOrDiedrateModSystem.LoadedConfig;
                float configHydrationLossDelayMultiplier = config.HydrationLossDelayMultiplier;

                float capturedHydLossDelay = (float)Math.Floor(totalHydrationConsumed * configHydrationLossDelayMultiplier);

                if (capturedPlayer != null)
                {
                    var thirstBehavior = capturedPlayer.GetBehavior<EntityBehaviorThirst>();
                    if (thirstBehavior != null)
                    {
                        thirstBehavior.ModifyThirst(totalHydrationConsumed, capturedHydLossDelay);
                    }
                }

                capturedTotalHydration = 0;
                capturedHydLossDelay = 0;
                capturedPlayer = null;
                servingsBeforeConsume = 0;
                capturedServingsInMeal = 0;
            }
            catch (Exception ex)
            {
                byEntity.World.Logger.Error($"TryFinishEatMealPostfix: Exception occurred - {ex.Message}");
            }
        }

        [HarmonyPatch("GetHeldItemInfo")]
        [HarmonyPostfix]
        public static void GetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            if (ShouldSkipPatch())
            {
                return;
            }
            ItemStack[] contentStacks = (inSlot.Itemstack.Collectible as BlockMeal)?.GetNonEmptyContents(world, inSlot.Itemstack);
            if (contentStacks == null)
            {
                return;
            }

            float totalHydration = CalculateTotalHydration(world, contentStacks);
            if (totalHydration != 0)
            {
                float servings = (inSlot.Itemstack.Collectible as BlockMeal).GetQuantityServings(world, inSlot.Itemstack);
                float hydrationPerServing = servings > 1 ? totalHydration / servings : totalHydration;

                string hydrationText = $"Hydration Per Serving: {hydrationPerServing}";
                if (!dsc.ToString().Contains(hydrationText))
                {
                    dsc.AppendLine(hydrationText);
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
                    float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);

                    if (itemCode.ToLower().Contains("portion"))
                    {
                        WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(contentStack);
                        if (props != null && props.ItemsPerLitre > 0)
                        {
                            float litres = (float)contentStack.StackSize / props.ItemsPerLitre;
                            hydrationValue *= litres;
                        }
                    }

                    if (hydrationValue != 0)
                    {
                        totalHydration += hydrationValue;
                    }
                }
            }
            return totalHydration;
        }
    }
}
