using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Configuration;

[HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
public class TryEatStopCollectibleObjectPatch
{
    static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var api = byEntity?.World?.Api;
        if (api?.Side == EnumAppSide.Server)
        {
            if (slot?.Itemstack == null)
            {
                return;
            }

            FoodNutritionProperties nutriProps = slot.Itemstack.Collectible.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
            if (nutriProps != null && secondsUsed >= 0.95f)
            {
                string itemCode = slot.Itemstack.Collectible.Code?.ToString() ?? "Unknown Item";
                float hydration = HydrationManager.GetHydration(api, itemCode);

                if (hydration != 0 && byEntity is EntityPlayer player)
                {
                    var thirstBehavior = byEntity.GetBehavior<EntityBehaviorThirst>();
                    if (thirstBehavior != null)
                    {
                        thirstBehavior.ModifyThirst(hydration);
                    }
                }
            }
        }
    }
}