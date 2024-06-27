using System;
using HarmonyLib;
using HydrateOrDiedrate;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(BlockLiquidContainerBase), "tryEatStop")]
public class TryEatStopBlockLiquidContainerBasePatch
{
    private static bool ShouldSkipPatch()
    {
        return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
    }
    static bool alreadyCalled = false;
    static float capturedHydrationAmount;
    static float capturedHydLossDelay;
    static EntityPlayer capturedPlayer;

    [HarmonyPrefix]
    static void Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch())
        {
            return;
        }
        alreadyCalled = false;
        capturedHydrationAmount = 0;
        capturedHydLossDelay = 0;
        capturedPlayer = null;

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

            string itemCode = contentStack.Collectible.Code?.ToString() ?? "Unknown Item";
            float hydrationValue = HydrationManager.GetHydration(api, itemCode);

            if (hydrationValue != 0 && byEntity is EntityPlayer player)
            {
                float drinkCapLitres = 1f;
                float litresToDrink = Math.Min(drinkCapLitres, currentLitres);
                capturedHydrationAmount = (hydrationValue * litresToDrink) / drinkCapLitres;
                float intoxicationValue = nutriProps.Intoxication;
                var config = HydrateOrDiedrateModSystem.LoadedConfig;
                float scalingFactorLow = 100f;
                float scalingFactorHigh = 5f;

                if (intoxicationValue == 0)
                {
                    capturedHydLossDelay = (capturedHydrationAmount / 2) * config.HydrationLossDelayMultiplier;
                }
                else if (intoxicationValue < 0.2)
                {
                    capturedHydLossDelay = (capturedHydrationAmount / 2) * config.HydrationLossDelayMultiplier * (float)(Math.Log(1 + intoxicationValue * scalingFactorLow));
                }
                else if (intoxicationValue > 0.7)
                {
                    capturedHydLossDelay = (capturedHydrationAmount / 2) * config.HydrationLossDelayMultiplier * (float)Math.Pow(scalingFactorHigh, intoxicationValue);
                }
                else
                {
                    float logValue_0_2 = (float)Math.Log(1 + 0.2 * scalingFactorLow);
                    float expValue_0_7 = (float)Math.Pow(scalingFactorHigh, 0.7);
                    float blendRatio = (intoxicationValue - 0.2f) / 0.5f;
                    float blendedValue = logValue_0_2 * (1 - blendRatio) + expValue_0_7 * blendRatio;
                    capturedHydLossDelay = (capturedHydrationAmount / 2) * config.HydrationLossDelayMultiplier * blendedValue;
                }
                capturedPlayer = player;
            }
        }
    }

    [HarmonyPostfix]
    static void Postfix()
    {
        if (ShouldSkipPatch())
        {
            return;
        }
        if (alreadyCalled) return;
        alreadyCalled = true;

        if (capturedPlayer == null || capturedHydrationAmount == 0) return;

        var api = capturedPlayer?.World?.Api;

        if (api == null || api.Side != EnumAppSide.Server)
        {
            return;
        }

        var thirstBehavior = capturedPlayer.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior != null)
        {
            thirstBehavior.ModifyThirst(capturedHydrationAmount, capturedHydLossDelay);
        }

        capturedHydrationAmount = 0;
        capturedHydLossDelay = 0;
        capturedPlayer = null;
    }
}
