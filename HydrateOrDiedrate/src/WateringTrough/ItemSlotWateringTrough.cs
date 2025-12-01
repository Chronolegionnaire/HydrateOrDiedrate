using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough;

public class ItemSlotWateringTrough : ItemSlotSurvival
{
    private readonly BlockEntityWateringTrough be;

    public ItemSlotWateringTrough(BlockEntityWateringTrough be, InventoryGeneric inventory) : base(inventory)
    {
        this.be = be;
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        return base.CanTakeFrom(sourceSlot, priority) && this.Troughable(sourceSlot);
    }

    public override bool CanHold(ItemSlot itemstackFromSourceSlot)
    {
        return base.CanHold(itemstackFromSourceSlot) && this.Troughable(itemstackFromSourceSlot);
    }

    /// <summary>
    /// Accepts:
    ///  - normal trough feed (via ContentConfig), OR
    ///  - watertight liquid items (water etc.), up to MaxWaterLitres
    /// but never both types in the same slot.
    /// </summary>
    public bool Troughable(ItemSlot sourceSlot)
    {
        if (sourceSlot.Empty) return false;

        IWorldAccessor world = be.Api.World;
        ItemStack stack = sourceSlot.Itemstack;

        // If this slot already has something, require same collectible for merging
        if (!Empty && !stack.Equals(world, this.itemstack, GlobalConstants.IgnoredStackAttributes))
        {
            return false;
        }

        // 1. Try as normal trough feed (ContentConfig)
        ContentConfig[] contentConfigs = be.contentConfigs;
        ContentConfig feedCfg = ItemSlotTrough.getContentConfig(world, contentConfigs, sourceSlot);

        if (feedCfg != null)
        {
            // Slot currently holds feed OR is empty → use vanilla capacity logic
            int maxItems = feedCfg.MaxFillLevels * feedCfg.QuantityPerFillLevel;
            return StackSize < maxItems;
        }

        // 2. Try as liquid / watertight containable
        var containableProps = BlockLiquidContainerBase.GetContainableProps(stack);
        bool isLiquid = containableProps != null && stack.Collectible.IsLiquid();

        if (!isLiquid)
        {
            // Not feed, not liquid -> not allowed
            return false;
        }

        // If there is something already in the slot and it's not liquid, we can't mix
        if (!Empty)
        {
            var curProps = BlockLiquidContainerBase.GetContainableProps(this.itemstack);
            if (curProps == null || !this.itemstack.Collectible.IsLiquid())
            {
                // Slot currently holds feed; don't allow liquid in the same slot
                return false;
            }
        }

        // Compute litres currently in this slot (if any)
        float currentLitres = 0f;
        if (!Empty)
        {
            var curProps = BlockLiquidContainerBase.GetContainableProps(this.itemstack);
            if (curProps != null && this.itemstack.Collectible.IsLiquid())
            {
                currentLitres = (float)this.itemstack.StackSize / curProps.ItemsPerLitre;
            }
        }

        // Litres the incoming stack would represent
        float incomingLitres = (float)stack.StackSize / containableProps.ItemsPerLitre;

        // Total must not exceed trough capacity
        return currentLitres + incomingLitres <= be.MaxWaterLitres + 0.0001f;
    }

    // Optional helper if you still want a local getContentConfig
    public static ContentConfig getContentConfig(IWorldAccessor world, ContentConfig[] contentConfigs, ItemSlot sourceSlot)
    {
        if (sourceSlot.Empty)
        {
            return null;
        }

        foreach (ContentConfig cfg in contentConfigs)
        {
            if (cfg.Content.Code.Path.Contains('*'))
            {
                if (WildcardUtil.Match(cfg.Content.Code, sourceSlot.Itemstack.Collectible.Code))
                {
                    return cfg;
                }
            }
            else if (sourceSlot.Itemstack.Equals(world, cfg.Content.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes))
            {
                return cfg;
            }
        }

        return null;
    }
}
