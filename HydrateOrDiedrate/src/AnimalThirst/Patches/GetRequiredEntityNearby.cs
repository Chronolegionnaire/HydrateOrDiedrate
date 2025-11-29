using HarmonyLib;
using HydrateOrDiedrate.AnimalThirst;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.AnimalThirst.Patches
{
    [HarmonyPatch(typeof(EntityBehaviorMultiply), "GetRequiredEntityNearby")]
    public static class MultiplyHydrationMalePatch
    {
        static void Postfix(EntityBehaviorMultiply __instance, ref Entity __result)
        {
            if (__result == null) return;
            const float minHydration = 1f;
            const double maxHoursSinceDrink = 24.0;

            if (!HydrationHelpers.IsHydratedForBreeding(__result, minHydration, maxHoursSinceDrink))
            {
                __result = null;
            }
        }
    }
}