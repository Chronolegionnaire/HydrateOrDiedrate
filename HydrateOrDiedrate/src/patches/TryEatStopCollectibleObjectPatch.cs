using HarmonyLib;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Patches;

[HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
public class TryEatStopCollectibleObjectPatch
{
    private static bool alreadyCalled = false;

    private static bool ShouldSkipPatch()
    {
        return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
    }

    [HarmonyPrefix]
    static void Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity, out PatchState __state)
    {
        __state = null;

        if (ShouldSkipPatch())
        {
            return;
        }

        alreadyCalled = false;

        var api = byEntity?.World?.Api;
        if (api == null || api.Side != EnumAppSide.Server || slot?.Itemstack == null)
        {
            return;
        }

        FoodNutritionProperties nutriProps = slot.Itemstack.Collectible.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
        if (nutriProps != null && secondsUsed >= 0.95f)
        {
            float hydrationAmount = HydrationManager.GetHydration(slot.Itemstack);

            if (hydrationAmount != 0 && byEntity is EntityPlayer player)
            {
                __state = new PatchState
                {
                    Player = player,
                    HydrationAmount = hydrationAmount
                };
            }
        }
    }

    [HarmonyPostfix]
    static void Postfix(PatchState __state)
    {
        if (ShouldSkipPatch() || alreadyCalled)
        {
            return;
        }
        alreadyCalled = true;

        if (__state == null || __state.Player == null || __state.HydrationAmount == 0)
        {
            return;
        }

        var api = __state.Player?.World?.Api;
        if (api == null || api.Side != EnumAppSide.Server)
        {
            return;
        }

        var thirstBehavior = __state.Player.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior != null)
        {
            thirstBehavior.ModifyThirst(__state.HydrationAmount);
        }
    }

    private class PatchState
    {
        public EntityPlayer Player { get; set; }
        public float HydrationAmount { get; set; }
    }
}
