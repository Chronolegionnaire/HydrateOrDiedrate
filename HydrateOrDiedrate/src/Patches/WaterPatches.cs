using HydrateOrDiedrate.Config;
using System;
using System.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate;

public static class WaterPatches
{
    public static void ApplyConfigSettings(ICoreAPI api)
    {
        foreach((var code, var satiety) in ModConfig.Instance.Satiety.ItemSatietyMapping)
        {
            var item = api.World.GetItem(code);
            if (item is null) continue;

            var nutrients = item.Attributes?.Token["waterTightContainerProps"]?["nutritionPropsPerLitre"];
            if(nutrients is null) continue;
            nutrients["satiety"] = satiety;
        }

        foreach(var collectible in api.World.Collectibles)
        {
            if (collectible.Code is null || !collectible.Code.Path.Contains("water") || collectible.ItemClass != EnumItemClass.Item || !collectible.GetType().Name.Equals("ItemLiquidPortion", StringComparison.OrdinalIgnoreCase)) continue;

            if(collectible.TransitionableProps is not null)
            {
                if (!ModConfig.Instance.PerishRates.Enabled)
                {
                    collectible.TransitionableProps = [.. collectible.TransitionableProps.Where(t => t.Type != EnumTransitionType.Perish)];
                }
                else if (ModConfig.Instance.PerishRates.TransitionConfig.TryGetValue(collectible.Code, out var config))
                {
                    var perishtTransition = collectible.TransitionableProps.FirstOrDefault(static item => item.Type == EnumTransitionType.Perish);
                    if (perishtTransition is not null)
                    {
                        perishtTransition.FreshHours.avg = config.FreshHours;
                        perishtTransition.TransitionHours.avg = config.TransitionHours;
                    }
                }
            }
            
            var waterTightProps = collectible.Attributes?.Token["waterTightContainerProps"];
            var nutrientProps = waterTightProps?["nutritionPropsPerLitre"];
            if (nutrientProps is not null && waterTightProps["NutritionPropsPerLitreWhenInMeal"] is null)
            {
                //NutritionPropsPerLitreWhenInMeal should be present when health is non-zero, otherwise food recipes using this water will heal/damage the player
                var nutrientsPropsWhenInMeal = nutrientProps.DeepClone();
                nutrientsPropsWhenInMeal["satiety"]?.Parent.Remove();
                waterTightProps["NutritionPropsPerLitreWhenInMeal"] = nutrientsPropsWhenInMeal;
            }
        }
    }
}