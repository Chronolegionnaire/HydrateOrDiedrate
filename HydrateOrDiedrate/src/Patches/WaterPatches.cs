using HydrateOrDiedrate.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace HydrateOrDiedrate;

public static class WaterPatches
{
    public static void ApplyConfigSettings(ICoreAPI api)
    {
        foreach(var collectible in api.World.Collectibles)
        {
            if(collectible.Code is null || collectible.Code.Domain != "hydrateordiedrate") continue;

            if(collectible is BlockForFluidsLayer)
            {
                //TODO maybe add similar logic for the fluid blocks
            }
            else if(collectible.Class.Equals("ItemLiquidPortion", StringComparison.OrdinalIgnoreCase))
            {
                var containerProps = collectible.Attributes?.Token["waterTightContainerProps"];
                if(containerProps is null) continue;
                
                var nutrientProps =  containerProps["nutritionPropsPerLitre"];
                if(nutrientProps is null) continue;
                
                if (nutrientProps.Value<float>("satiety") == 0) //Otherwise satiety was manually configured
                {
                    var modifier = 0f;

                    modifier += GetTypeSatietyModifier(collectible.Variant["type"]);
                    modifier += GetSourceSatietyModifier(collectible.Variant["source"]);
                    modifier += GetPolutionSatietyModifier(collectible.Variant["pollution"]);

                    modifier = Math.Min(modifier, 0);

                    nutrientProps["satiety"] = JToken.FromObject(modifier);
                }

                if(nutrientProps.Value<float>("health") != 0 && nutrientProps["NutritionPropsPerLitreWhenInMeal"] is null)
                {
                    //NutritionPropsPerLitreWhenInMeal should be present when health is non-zero, otherwise food recipes using this water will heal/damage the player
                    var nutrientsPropsWhenInMeal = nutrientProps.DeepClone();
                    nutrientsPropsWhenInMeal["satiety"]?.Parent.Remove();
                    nutrientProps["NutritionPropsPerLitreWhenInMeal"] = nutrientsPropsWhenInMeal;
                }
            }
        }
    }

    public static float GetTypeSatietyModifier(string waterType) => waterType switch
    {
        "fresh" => ModConfig.Instance.Satiety.FreshWaterSatietyModifier,
        "salt" => ModConfig.Instance.Satiety.SaltWaterSatietyModifier,
        "boiled" => ModConfig.Instance.Satiety.BoiledWaterSatietyModifier,
        _ => 0f
    };

    public static float GetSourceSatietyModifier(string source) => source switch
    {
        "natural" => ModConfig.Instance.Satiety.NaturalWaterSatietyModifier,
        "well" => ModConfig.Instance.Satiety.WellWaterSatietyModifier,
        "rain" => ModConfig.Instance.Satiety.RainWaterSatietyModifier,
        "distilled" => ModConfig.Instance.Satiety.DistilledWaterSatietyModifier,
        _ => 0f
    };

    public static float GetPolutionSatietyModifier(string pollutionLevel) => pollutionLevel switch
    {
        "clean" => ModConfig.Instance.Satiety.CleanWaterSatietyModifier,
        "muddy" => ModConfig.Instance.Satiety.MuddyWaterSatietyModifier,
        "tainted" => ModConfig.Instance.Satiety.TaintedWaterSatietyModifier,
        "poisoned" => ModConfig.Instance.Satiety.PoisonedWaterSatietyModifier,
        _ => 0f
    };
}