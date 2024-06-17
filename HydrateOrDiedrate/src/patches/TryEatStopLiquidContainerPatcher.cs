using System;
using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(BlockLiquidContainerBase), "tryEatStop")]
public class TryEatStopBlockLiquidContainerBasePatch
{
    static bool Prefix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var api = byEntity?.World?.Api;
        if (api?.Side == EnumAppSide.Server)
        {
            if (slot?.Itemstack == null) return true;

            BlockLiquidContainerBase block = slot.Itemstack.Block as BlockLiquidContainerBase;
            if (block == null) return true;

            float currentLitres = block.GetCurrentLitres(slot.Itemstack);
            ItemStack contentStack = block.GetContent(slot.Itemstack);
            if (contentStack == null) return true;

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

                    // Reduce the liquid content after drinking
                    block.SetCurrentLitres(slot.Itemstack, currentLitres - litresToDrink);
                }
            }
        }

        return true;
    }
}