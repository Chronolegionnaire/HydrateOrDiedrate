using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    public static class EntityBehaviorSDHungerPatch
    {
        public static void Apply(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server)
            {
                return;
            }

            var smoothDigestionAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                    a.GetName().Name.Equals("SmoothDigestion", StringComparison.InvariantCultureIgnoreCase));

            if (smoothDigestionAssembly == null)
            {
                return;
            }

            var smoothDigestionType =
                smoothDigestionAssembly.GetType("SmoothDigestion.Behaviors.EntityBehaviorSDHunger");
            if (smoothDigestionType == null)
            {
                return;
            }

            var targetMethod = smoothDigestionType.GetMethod("OnEntityReceiveSaturation",
                BindingFlags.Public | BindingFlags.Instance);

            if (targetMethod == null)
            {
                return;
            }

            var harmony = new Harmony("com.chronolegionnaire.hydrateordiedrate.betterdigestion");
            var prefix = new HarmonyMethod(
                typeof(EntityBehaviorSDHungerPatch).GetMethod(nameof(OnEntityReceiveSaturationPrefix),
                    BindingFlags.Public | BindingFlags.Static));
            var patchProcessor = new PatchProcessor(harmony, targetMethod);
            patchProcessor.AddPrefix(prefix);
            patchProcessor.Patch();
        }

        public static bool OnEntityReceiveSaturationPrefix(ref float saturation, ref EnumFoodCategory foodCat, float saturationLossDelay, ref float nutritionGainMultiplier, EntityBehaviorHunger __instance)
        {
            try
            {
                float gainNutritionRate = 0.4f;
                float gainSatietyRate = 1f;
                try
                {
                    var hungerConfigType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == "SmoothDigestion.Config.HungerConfig");

                    if (hungerConfigType != null)
                    {
                        var currentProperty = hungerConfigType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
                        if (currentProperty != null)
                        {
                            var hungerConfigInstance = currentProperty.GetValue(null);
                            if (hungerConfigInstance != null)
                            {
                                var gainNutritionField = hungerConfigType.GetField("GainNutritionRate", BindingFlags.Public | BindingFlags.Instance);
                                if (gainNutritionField != null)
                                {
                                    gainNutritionRate = (float)gainNutritionField.GetValue(hungerConfigInstance);
                                }
                                var gainSatietyField = hungerConfigType.GetField("GainSatietyRate", BindingFlags.Public | BindingFlags.Instance);
                                if (gainSatietyField != null)
                                {
                                    gainSatietyRate = (float)gainSatietyField.GetValue(hungerConfigInstance);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }

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
                __instance.Saturation = Math.Min(maxsat, __instance.Saturation + saturation * gainSatietyRate);

                if (applyNutrition)
                {
                    switch (foodCat)
                    {
                        case EnumFoodCategory.Fruit:
                            __instance.FruitLevel = Math.Min(maxsat, __instance.FruitLevel + saturation * gainNutritionRate * nutritionGainMultiplier);
                            __instance.SaturationLossDelayFruit = Math.Max(__instance.SaturationLossDelayFruit, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Vegetable:
                            __instance.VegetableLevel = Math.Min(maxsat, __instance.VegetableLevel + saturation * gainNutritionRate * nutritionGainMultiplier);
                            __instance.SaturationLossDelayVegetable = Math.Max(__instance.SaturationLossDelayVegetable, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Protein:
                            __instance.ProteinLevel = Math.Min(maxsat, __instance.ProteinLevel + saturation * gainNutritionRate * nutritionGainMultiplier);
                            __instance.SaturationLossDelayProtein = Math.Max(__instance.SaturationLossDelayProtein, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Grain:
                            __instance.GrainLevel = Math.Min(maxsat, __instance.GrainLevel + saturation * gainNutritionRate * nutritionGainMultiplier);
                            __instance.SaturationLossDelayGrain = Math.Max(__instance.SaturationLossDelayGrain, saturationLossDelay);
                            break;
                        case EnumFoodCategory.Dairy:
                            __instance.DairyLevel = Math.Min(maxsat, __instance.DairyLevel + saturation * gainNutritionRate * nutritionGainMultiplier);
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
