using HarmonyLib;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
public class TryEatStopCollectibleObjectPatch
{
    private static bool ShouldSkipPatch()
    {
        return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
    }
    static bool alreadyCalled = false;
    static float capturedHydrationAmount;
    static EntityPlayer capturedPlayer;
    private Config.Config _config
    {
        get
        {
            // Use ServerConfig if available, otherwise fall back to LoadedConfig
            return HydrateOrDiedrateModSystem.ServerConfig ?? HydrateOrDiedrateModSystem.LoadedConfig;
        }
    }

    [HarmonyPrefix]
    static void Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        if (ShouldSkipPatch())
        {
            return;
        }   
        if (HydrateOrDiedrateModSystem.ThirstConfigHelper.ShouldSkipThirstMechanics())
        {
            return;
        }
        alreadyCalled = false;
        capturedHydrationAmount = 0;
        capturedPlayer = null;

        var api = byEntity?.World?.Api;

        if (api == null || api.Side != EnumAppSide.Server || slot?.Itemstack == null)
        {
            return;
        }

        FoodNutritionProperties nutriProps = slot.Itemstack.Collectible.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
        if (nutriProps != null && secondsUsed >= 0.95f)
        {
            string itemCode = slot.Itemstack.Collectible.Code?.ToString() ?? "Unknown Item";
            capturedHydrationAmount = HydrationManager.GetHydration(api, itemCode);

            if (capturedHydrationAmount != 0 && byEntity is EntityPlayer player)
            {
                capturedPlayer = player;
            }
        }
    }

    [HarmonyPostfix]
    static void Postfix()
    {
        if (ShouldSkipPatch())
        {
            return;
        }
        if (HydrateOrDiedrateModSystem.ThirstConfigHelper.ShouldSkipThirstMechanics())
        {
            return;
        }
        if (alreadyCalled) return;
        alreadyCalled = true;

        if (capturedPlayer == null || capturedHydrationAmount == 0) return;

        var api = capturedPlayer?.World?.Api;

        if (api == null || api.Side != EnumAppSide.Server)
        {
            return;
        }

        var thirstBehavior = capturedPlayer.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior != null)
        {
            thirstBehavior.ModifyThirst(capturedHydrationAmount);
        }

        capturedHydrationAmount = 0;
        capturedPlayer = null;
    }
}