using System;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
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
                bool applyNutrition = true;

                if (thirstBehavior != null)
                {
                    float nutritionDeficitAmount = thirstBehavior.NutritionDeficitAmount;

                    if (saturation > 0f)
                    {

                        thirstBehavior.NutritionDeficitAmount = Math.Max(0f, nutritionDeficitAmount - saturation);

                        if (nutritionDeficitAmount > 0f)
                        {

                            applyNutrition = false;
                        }
                        else
                        {

                            applyNutrition = true;
                        }
                    }
                    else if (saturation < 0f)
                    {
                        float deficitMul = ModConfig.Instance.Thirst.NutritionDeficitMultiplier;
                        if (!float.IsFinite(deficitMul) || deficitMul <= 0f)
                        {
                            deficitMul = 1f;
                        }

                        thirstBehavior.NutritionDeficitAmount += Math.Abs(saturation) * deficitMul;

                        applyNutrition = false;
                    }
                }

                float maxsat = __instance.MaxSaturation;
                __instance.Saturation = Math.Min(maxsat, __instance.Saturation + saturation);

                if (applyNutrition)
                {
                    switch (foodCat)
                    {
                        case EnumFoodCategory.Fruit:
                            __instance.FruitLevel = Math.Min(maxsat, __instance.FruitLevel + saturation / 2.5f * nutritionGainMultiplier);
                            __instance.SaturationLossDelayFruit = Math.Max(__instance.SaturationLossDelayFruit, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Vegetable:
                            __instance.VegetableLevel = Math.Min(maxsat, __instance.VegetableLevel + saturation / 2.5f * nutritionGainMultiplier);
                            __instance.SaturationLossDelayVegetable = Math.Max(__instance.SaturationLossDelayVegetable, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Protein:
                            __instance.ProteinLevel = Math.Min(maxsat, __instance.ProteinLevel + saturation / 2.5f * nutritionGainMultiplier);
                            __instance.SaturationLossDelayProtein = Math.Max(__instance.SaturationLossDelayProtein, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Grain:
                            __instance.GrainLevel = Math.Min(maxsat, __instance.GrainLevel + saturation / 2.5f * nutritionGainMultiplier);
                            __instance.SaturationLossDelayGrain = Math.Max(__instance.SaturationLossDelayGrain, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Dairy:
                            __instance.DairyLevel = Math.Min(maxsat, __instance.DairyLevel + saturation / 2.5f * nutritionGainMultiplier);
                            __instance.SaturationLossDelayDairy = Math.Max(__instance.SaturationLossDelayDairy, saturationLossDelay);
                            break;
                        case EnumFoodCategory.NoNutrition:
                            break;
                    }
                }
                else
                {
                    switch (foodCat)
                    {
                        case EnumFoodCategory.Fruit:
                            __instance.SaturationLossDelayFruit = Math.Max(__instance.SaturationLossDelayFruit, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Vegetable:
                            __instance.SaturationLossDelayVegetable = Math.Max(__instance.SaturationLossDelayVegetable, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Protein:
                            __instance.SaturationLossDelayProtein = Math.Max(__instance.SaturationLossDelayProtein, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Grain:
                            __instance.SaturationLossDelayGrain = Math.Max(__instance.SaturationLossDelayGrain, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Dairy:
                            __instance.SaturationLossDelayDairy = Math.Max(__instance.SaturationLossDelayDairy, saturationLossDelay);
                            break;
                        case EnumFoodCategory.NoNutrition:
                            break;
                    }
                }
                
                __instance.UpdateNutrientHealthBoost();
            }
            catch (Exception ex)
            {
            }

            return false;
        }
    }
}
