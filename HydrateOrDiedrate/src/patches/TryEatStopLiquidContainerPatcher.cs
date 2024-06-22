using System;
using HarmonyLib;
using HydrateOrDiedrate;
using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
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
                hydrationValue = Math.Max(0, hydrationValue);  // Ignoring negatives

                if (hydrationValue != 0 && byEntity is EntityPlayer player)
                {
                    float drinkCapLitres = 1f;
                    float litresToDrink = Math.Min(drinkCapLitres, currentLitres);
                    float hydrationAmount = (hydrationValue * litresToDrink) / drinkCapLitres;

                    // Calculate hydLossDelay
                    var nutriPropsPerLitre = contentStack.Collectible.Attributes?["watertightcontainableprops"]?["nutritionPropsPerlitre"];
                    float intoxicationValue = nutriPropsPerLitre?["intoxication"]?.AsFloat(0) ?? 0;
                    intoxicationValue = Math.Max(0, intoxicationValue);  // Ignoring negatives

                    // Use the configured multiplier
                    var config = HydrateOrDiedrateModSystem.LoadedConfig;
                    float hydLossDelay = hydrationAmount * config.HydrationLossDelayMultiplier * (1 + intoxicationValue);

                    // Log the calculated delay
                    api.Logger.Notification($"[HydrateOrDiedrate] Hydration loss delayed for: {hydLossDelay} seconds.");

                    // Apply hydration and hydLossDelay directly to EntityBehaviorThirst
                    var thirstBehavior = player.GetBehavior<EntityBehaviorThirst>();
                    if (thirstBehavior != null)
                    {
                        thirstBehavior.ModifyThirst(hydrationAmount, hydLossDelay);
                        api.Logger.Notification($"[HydrateOrDiedrate] Applied thirst modification directly. Amount: {hydrationAmount}, Delay: {hydLossDelay}");
                    }
                    else
                    {
                        api.Logger.Warning("[HydrateOrDiedrate] EntityBehaviorThirst not found on player.");
                    }
                }
            }
        }
    }
}
