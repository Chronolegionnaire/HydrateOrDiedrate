using System;
using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(BlockLiquidContainerBase), "tryEatStop")]
public class TryEatStopBlockLiquidContainerBasePatch
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

            BlockLiquidContainerBase block = slot.Itemstack.Block as BlockLiquidContainerBase;
            if (block == null)
            {
                return;
            }

            FoodNutritionProperties nutriProps = slot.Itemstack.Collectible.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
            if (nutriProps != null && secondsUsed >= 0.95f)
            {
                float currentLitres = block.GetCurrentLitres(slot.Itemstack);
                ItemStack contentStack = block.GetContent(slot.Itemstack);
                if (contentStack == null)
                {
                    return;
                }

                string itemCode = contentStack.Collectible.Code?.ToString() ?? "Unknown Item";
                float hydrationValue = HydrationManager.GetHydration(api, itemCode);

                if (hydrationValue != 0 && byEntity is EntityPlayer player)
                {
                    float drinkCapLitres = 1f;
                    float litresToDrink = Math.Min(drinkCapLitres, currentLitres);
                    float hydrationAmount = (hydrationValue * litresToDrink) / drinkCapLitres;
                    var handler = new WaterInteractionHandler(api, HydrateOrDiedrateModSystem.LoadedConfig);
                    var playerByUid = api.World.PlayerByUid(player.PlayerUID);
                    if (playerByUid != null)
                    {
                        handler.ModifyThirst(playerByUid, hydrationAmount);
                    }
                }
            }
        }
    }
}
