using System;
using HarmonyLib;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

[HarmonyPatch(typeof(EntityBehavior), "OnEntityReceiveSaturation")]
public class OnEntityReceiveSaturationPatch
{
    [HarmonyPrefix]
    public static void Prefix(ref float saturation, EnumFoodCategory foodCat, float saturationLossDelay, ref float nutritionGainMultiplier, EntityBehavior __instance)
    {
        var thirstBehavior = __instance.entity.GetBehavior<EntityBehaviorThirst>();

        if (thirstBehavior != null)
        {
            thirstBehavior.CheckAndResetFlag();
            if (thirstBehavior.HasProcessedSaturation)
            {
                return;
            }

            float hungerReductionAmount = thirstBehavior.HungerReductionAmount;

            if (saturation <= hungerReductionAmount)
            {
                nutritionGainMultiplier = 0;
                thirstBehavior.HungerReductionAmount = Math.Max(0, hungerReductionAmount - saturation); 
            }
            else if (hungerReductionAmount > 0)
            {
                float remainingSaturation = saturation - hungerReductionAmount;
                thirstBehavior.HungerReductionAmount = 0;
                nutritionGainMultiplier *= (remainingSaturation / saturation);
            }
            thirstBehavior.HasProcessedSaturation = true;
        }
        else
        {
        }
    }
}
