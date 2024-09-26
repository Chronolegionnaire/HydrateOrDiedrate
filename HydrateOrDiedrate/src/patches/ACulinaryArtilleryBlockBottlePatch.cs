using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch]
public class TryEatStopBlockBottlePatch
{
    private static bool ShouldSkipPatch()
    {
        return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
    }   
    static bool alreadyCalled = false;
    static float capturedHydrationAmount;
    static float capturedHydLossDelay;
    static float capturedHungerReduction;
    static EntityPlayer capturedPlayer;

    static bool Prepare()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "ACulinaryArtillery");
    }

    static MethodBase TargetMethod()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ACulinaryArtillery");
        if (assembly == null) return null;

        var blockBottleType = assembly.GetType("ACulinaryArtillery.BlockBottle");
        if (blockBottleType == null) return null;

        return blockBottleType.GetMethod("tryEatStop", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    [HarmonyPrefix]
    static void Prefix(object __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch())
        {
            return;
        }
        alreadyCalled = false;
        capturedHydrationAmount = 0;
        capturedHydLossDelay = 0;
        capturedHungerReduction = 0;
        capturedPlayer = null;

        var api = byEntity?.World?.Api;
        if (api == null || api.Side != EnumAppSide.Server || slot?.Itemstack == null) return;

        var blockType = __instance.GetType();
        var getCurrentLitresMethod = blockType.GetMethod("GetCurrentLitres", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(ItemStack) }, null);
        var getContentMethod = blockType.GetMethod("GetContent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(ItemStack) }, null);

        if (getCurrentLitresMethod == null || getContentMethod == null) return;

        float currentLitres = (float)getCurrentLitresMethod.Invoke(__instance, new object[] { slot.Itemstack });
        if (currentLitres <= 0) return;

        ItemStack contentStack = (ItemStack)getContentMethod.Invoke(__instance, new object[] { slot.Itemstack });
        if (contentStack == null) return;

        FoodNutritionProperties nutriProps = slot.Itemstack.Collectible.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
        if (nutriProps != null && secondsUsed >= 0.95f)
        {
            string itemCode = contentStack.Collectible.Code?.ToString() ?? "Unknown Item";
            float hydrationValue = HydrationManager.GetHydration(api, itemCode);
            if (hydrationValue != 0 && byEntity is EntityPlayer player)
            {
                float drinkCapLitres = 0.25f;
                float litresToDrink = Math.Min(drinkCapLitres, currentLitres);

                float litresMult = litresToDrink / 1.0f;
                capturedHydrationAmount = hydrationValue * litresMult;

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

                // Only accumulate hunger reduction if it's negative
                if (nutriProps.Satiety < 0)
                {
                    capturedHungerReduction = nutriProps.Satiety * GlobalConstants.FoodSpoilageSatLossMul(0, slot.Itemstack, byEntity);
                }
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
        if (api == null || api.Side != EnumAppSide.Server) return;

        var thirstBehavior = capturedPlayer.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior != null)
        {
            if (thirstBehavior.HungerReductionAmount < 0)
            {
                thirstBehavior.HungerReductionAmount = 0;
            }
            thirstBehavior.ModifyThirst(capturedHydrationAmount, capturedHydLossDelay);

            // Apply capturedHungerReduction only if it's negative
            if (capturedHungerReduction < 0)
            {
                thirstBehavior.HungerReductionAmount += capturedHungerReduction;
            }
        }

        capturedHydrationAmount = 0;
        capturedHydLossDelay = 0;
        capturedHungerReduction = 0;
        capturedPlayer = null;
    }
}