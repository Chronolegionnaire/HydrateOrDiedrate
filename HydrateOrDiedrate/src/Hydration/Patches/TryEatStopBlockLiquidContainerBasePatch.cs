using HarmonyLib;
using HydrateOrDiedrate.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), "tryEatStop")]
    public class TryEatStopBlockLiquidContainerBasePatch
    {
        private static bool ShouldSkipPatch() => !ModConfig.Instance.Thirst.Enabled;

        private static bool alreadyCalled = false;

        [HarmonyPrefix]
        static void Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, out PatchState __state)
        {
            __state = null;

            if (ShouldSkipPatch())
            {
                return;
            }

            alreadyCalled = false;

            var api = byEntity?.World?.Api;
            if (api == null || api.Side != EnumAppSide.Server || slot?.Itemstack == null)
            {
                return;
            }

            BlockLiquidContainerBase block = slot.Itemstack.Block as BlockLiquidContainerBase;
            if (block == null)
            {
                return;
            }

            FoodNutritionProperties nutriProps = slot.Itemstack.Collectible.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
            if (nutriProps != null && secondsUsed >= 0.95f)
            {
                float currentLitres = block.GetCurrentLitres(slot.Itemstack);
                if (currentLitres <= 0) return;

                ItemStack contentStack = block.GetContent(slot.Itemstack);
                if (contentStack == null)
                {
                    return;
                }
                float hydrationValue = HydrationManager.GetHydration(contentStack);

                if (hydrationValue != 0 && byEntity is EntityPlayer player)
                {
                    float drinkCapLitres = 1f;
                    float litresToDrink = Math.Min(drinkCapLitres, currentLitres);
                    float calculatedHydration = (hydrationValue * litresToDrink) / drinkCapLitres;
                    float intoxicationValue = nutriProps.Intoxication;
                    var config = ModConfig.Instance.Thirst;
                    float baseMultiplier = 0.05f;
                    float effectiveMultiplier = baseMultiplier * config.HydrationLossDelayMultiplierNormalized;
                    float maxDelay = 600f; 
                    float hydLossDelay = 0;

                    if (intoxicationValue == 0)
                    {
                        hydLossDelay = (calculatedHydration / 2) * effectiveMultiplier;
                    }
                    else if (intoxicationValue < 0.2)
                    {
                        hydLossDelay = (calculatedHydration / 2) * effectiveMultiplier * (float)(Math.Log(1 + intoxicationValue * 100f));
                    }
                    else if (intoxicationValue > 0.7)
                    {
                        hydLossDelay = (calculatedHydration / 2) * effectiveMultiplier * (float)Math.Pow(5f, intoxicationValue);
                    }
                    else
                    {
                        float logValue_0_2 = (float)Math.Log(1 + 0.2 * 100f);
                        float expValue_0_7 = (float)Math.Pow(5f, 0.7);
                        float blendRatio = (intoxicationValue - 0.2f) / 0.5f;
                        float blendedValue = logValue_0_2 * (1 - blendRatio) + expValue_0_7 * blendRatio;
                        hydLossDelay = (calculatedHydration / 2) * effectiveMultiplier * blendedValue;
                    }

                    float nutritionDeficit = 0;
                    if (nutriProps.Satiety < 0)
                    {
                        nutritionDeficit = nutriProps.Satiety * GlobalConstants.FoodSpoilageSatLossMul(0, slot.Itemstack, byEntity);
                    }
                    hydLossDelay = Math.Min(hydLossDelay, maxDelay);
                    __state = new PatchState
                    {
                        Player = player,
                        HydrationAmount = calculatedHydration,
                        HydLossDelay = hydLossDelay,
                        NutritionDeficit = nutritionDeficit
                    };
                }
            }
        }

        [HarmonyPostfix]
        static void Postfix(PatchState __state)
        {
            if (ShouldSkipPatch() || alreadyCalled)
            {
                return;
            }
            alreadyCalled = true;

            if (__state == null || __state.Player == null || __state.HydrationAmount == 0)
            {
                return;
            }

            var thirstBehavior = __state.Player.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                if (thirstBehavior.NutritionDeficitAmount < 0)
                {
                    thirstBehavior.NutritionDeficitAmount = 0;
                }

                thirstBehavior.ModifyThirst(__state.HydrationAmount, __state.HydLossDelay);

                if (__state.NutritionDeficit > 0)
                {
                    float deficitMul = ModConfig.Instance.Thirst.NutritionDeficitMultiplier;
                    if (!float.IsFinite(deficitMul) || deficitMul <= 0f)
                    {
                        deficitMul = 1f;
                    }

                    thirstBehavior.NutritionDeficitAmount += __state.NutritionDeficit * deficitMul;
                }
            }
        }

        private class PatchState
        {
            public EntityPlayer Player;
            public float HydrationAmount;
            public float HydLossDelay;
            public float NutritionDeficit;
        }
    }
}
