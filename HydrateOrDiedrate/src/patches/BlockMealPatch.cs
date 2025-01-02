using System;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch(typeof(BlockMeal))]
    public static class BlockMealPatches
    {
        private static bool alreadyCalled = false;

        private static bool ShouldSkipPatch()
        {
            return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
        }

        [HarmonyPatch("tryFinishEatMeal")]
        [HarmonyPrefix]
        public static void TryFinishEatMealPrefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed, out PatchState __state)
        {
            __state = null;

            if (ShouldSkipPatch())
            {
                return;
            }

            alreadyCalled = false;

            var api = byEntity?.World?.Api;

            // Ensure this logic only runs on the server side
            if (api == null || api.Side != EnumAppSide.Server)
            {
                return;
            }

            float totalHydration = HydrationCalculator.GetTotalHydration(byEntity.World, (slot.Itemstack.Collectible as BlockMeal)?.GetNonEmptyContents(byEntity.World, slot.Itemstack));
            if (totalHydration != 0)
            {
                var player = byEntity as EntityPlayer;
                if (player == null) return;

                float servingsBeforeConsume = (slot.Itemstack.Collectible as BlockMeal).GetQuantityServings(byEntity.World, slot.Itemstack);
                float configHydrationLossDelayMultiplier = HydrateOrDiedrateModSystem.LoadedConfig.HydrationLossDelayMultiplier;

                __state = new PatchState
                {
                    Player = player,
                    TotalHydration = totalHydration,
                    ServingsBeforeConsume = servingsBeforeConsume,
                    HydLossDelay = (totalHydration / servingsBeforeConsume / 2) * configHydrationLossDelayMultiplier
                };
            }
        }

        [HarmonyPatch("tryFinishEatMeal")]
        [HarmonyPostfix]
        public static void TryFinishEatMealPostfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed, PatchState __state)
        {
            if (ShouldSkipPatch() || alreadyCalled || __state == null)
            {
                return;
            }
            alreadyCalled = true;

            var api = byEntity?.World?.Api;

            // Ensure this logic only runs on the server side
            if (api == null || api.Side != EnumAppSide.Server)
            {
                return;
            }

            try
            {
                float servingsAfterConsume = (slot.Itemstack.Collectible as BlockMeal)?.GetQuantityServings(byEntity.World, slot.Itemstack) ?? 0;
                float servingsConsumed = __state.ServingsBeforeConsume - servingsAfterConsume;

                if (servingsConsumed <= 0) return;

                float hydrationPerServing = __state.TotalHydration / __state.ServingsBeforeConsume;
                float totalHydrationConsumed = hydrationPerServing * servingsConsumed;

                var thirstBehavior = __state.Player.GetBehavior<EntityBehaviorThirst>();
                thirstBehavior?.ModifyThirst(totalHydrationConsumed, __state.HydLossDelay);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TryFinishEatMealPostfix: {ex.Message}");
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

            float totalHydration = HydrationCalculator.GetTotalHydration(world, contentStacks);
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

        public class PatchState
        {
            public EntityPlayer Player { get; set; }
            public float TotalHydration { get; set; }
            public float ServingsBeforeConsume { get; set; }
            public float HydLossDelay { get; set; }
        }
    }
}
