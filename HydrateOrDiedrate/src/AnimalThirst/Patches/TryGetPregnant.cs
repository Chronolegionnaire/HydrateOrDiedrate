using HarmonyLib;
using HydrateOrDiedrate.AnimalThirst;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.AnimalThirst.Patches
{
    [HarmonyPatch(typeof(EntityBehaviorMultiply), "TryGetPregnant")]
    public static class MultiplyHydrationPatch
    {
        static bool Prefix(EntityBehaviorMultiply __instance, ref bool __result)
        {
            Entity entity = __instance.entity;
            const float minHydration = 1f;
            const double maxHoursSinceDrink = 24.0;

            if (!HydrationHelpers.IsHydratedForBreeding(entity, minHydration, maxHoursSinceDrink))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}