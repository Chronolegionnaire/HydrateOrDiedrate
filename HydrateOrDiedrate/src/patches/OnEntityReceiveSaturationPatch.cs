using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch(typeof(EntityBehaviorHunger), "OnEntityReceiveSaturation")]
    public class EntityBehaviorHungerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float saturation, ref EnumFoodCategory foodCat, float saturationLossDelay, ref float nutritionGainMultiplier, EntityBehaviorHunger __instance)
        {
            try
            {
                var thirstBehavior = __instance.entity.GetBehavior<EntityBehaviorThirst>();
                if (thirstBehavior != null)
                {
                    float hungerReductionAmount = thirstBehavior.HungerReductionAmount;
                    if (hungerReductionAmount > 0)
                    {
                        if (saturation > 0f)
                        {
                            if (hungerReductionAmount >= saturation)
                            {
                                thirstBehavior.HungerReductionAmount -= saturation;
                                saturation = 0f;
                            }
                            else
                            {
                                saturation -= hungerReductionAmount;
                                thirstBehavior.HungerReductionAmount = 0f;
                            }
                        }
                        else if (saturation < 0f)
                        {
                            thirstBehavior.HungerReductionAmount += Math.Abs(saturation);
                        }
                    }
                    else if (saturation < 0f)
                    {
                        thirstBehavior.HungerReductionAmount += Math.Abs(saturation); }
                    thirstBehavior.HasProcessedSaturation = true;
                }
                float maxsat = __instance.MaxSaturation;
                bool full = __instance.Saturation >= maxsat;
                __instance.Saturation = Math.Min(maxsat, __instance.Saturation + saturation);
                switch (foodCat)
                {
                    case EnumFoodCategory.Fruit:
                        if (!full)
                        {
                            __instance.FruitLevel = Math.Min(maxsat, __instance.FruitLevel + saturation / 2.5f * nutritionGainMultiplier);
                        }
                        __instance.SaturationLossDelayFruit = Math.Max(__instance.SaturationLossDelayFruit, saturationLossDelay);
                        break;
                    case EnumFoodCategory.Vegetable:
                        if (!full)
                        {
                            __instance.VegetableLevel = Math.Min(maxsat, __instance.VegetableLevel + saturation / 2.5f * nutritionGainMultiplier);
                        }
                        __instance.SaturationLossDelayVegetable = Math.Max(__instance.SaturationLossDelayVegetable, saturationLossDelay);
                        break;
                    case EnumFoodCategory.Protein:
                        if (!full)
                        {
                            __instance.ProteinLevel = Math.Min(maxsat, __instance.ProteinLevel + saturation / 2.5f * nutritionGainMultiplier);
                        }
                        __instance.SaturationLossDelayProtein = Math.Max(__instance.SaturationLossDelayProtein, saturationLossDelay);
                        break;
                    case EnumFoodCategory.Grain:
                        if (!full)
                        {
                            __instance.GrainLevel = Math.Min(maxsat, __instance.GrainLevel + saturation / 2.5f * nutritionGainMultiplier);
                        }
                        __instance.SaturationLossDelayGrain = Math.Max(__instance.SaturationLossDelayGrain, saturationLossDelay);
                        break;
                    case EnumFoodCategory.Dairy:
                        if (!full)
                        {
                            __instance.DairyLevel = Math.Min(maxsat, __instance.DairyLevel + saturation / 2.5f * nutritionGainMultiplier);
                        }
                        __instance.SaturationLossDelayDairy = Math.Max(__instance.SaturationLossDelayDairy, saturationLossDelay);
                        break;
                    case EnumFoodCategory.NoNutrition:
                        break;
                }
                
                __instance.UpdateNutrientHealthBoost();
            }
            catch (Exception ex)
            {
                __instance.entity.World.Logger.Error($"[EntityBehaviorHungerPatch] Exception: {ex.Message}\n{ex.StackTrace}");
            }

            return false;
        }
    }
}
