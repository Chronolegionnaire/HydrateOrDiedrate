using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;
using HydrateOrDiedrate;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(EntityBehaviorHunger))]
public class EntityBehaviorHungerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OnEntityReceiveSaturation")]
    public static void OnEntityReceiveSaturation_Prefix(EntityBehaviorHunger __instance, ref float saturation, EnumFoodCategory foodCat, float saturationLossDelay, ref float nutritionGainMultiplier)
    {
        var thirstBehavior = __instance.entity.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior != null && thirstBehavior.HungerReductionAmount > 0)
        {
            float hungerReductionAmount = thirstBehavior.HungerReductionAmount;
            
            if (hungerReductionAmount >= saturation)
            {
                thirstBehavior.HungerReductionAmount -= saturation;
                saturation = 0;
            }
            else
            {
                saturation -= hungerReductionAmount;
                thirstBehavior.HungerReductionAmount = 0;
            }
            nutritionGainMultiplier = 0; 
        }
    }
}