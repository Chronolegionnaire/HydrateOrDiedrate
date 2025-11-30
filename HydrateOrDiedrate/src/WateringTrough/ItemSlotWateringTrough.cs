using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough;

public class ItemSlotWateringTrough : ItemSlotSurvival
{
	public ItemSlotWateringTrough(BlockEntityWateringTrough be, InventoryGeneric inventory) : base(inventory)
	{
		this.be = be;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		return base.CanTakeFrom(sourceSlot, priority) && this.troughable(sourceSlot);
	}
	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		return base.CanHold(itemstackFromSourceSlot) && this.troughable(itemstackFromSourceSlot);
	}
	public bool troughable(ItemSlot sourceSlot)
	{
		if (!this.Empty &&
		    !sourceSlot.Itemstack.Equals(this.be.Api.World, this.itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			return false;
		}

		ContentConfig[] contentConfigs = this.be.contentConfigs;
		ContentConfig config = ItemSlotTrough.getContentConfig(this.be.Api.World, contentConfigs, sourceSlot);
		return config != null && config.MaxFillLevels * config.QuantityPerFillLevel > base.StackSize;
	}
	public static ContentConfig getContentConfig(IWorldAccessor world, ContentConfig[] contentConfigs,
		ItemSlot sourceSlot)
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
			else if (sourceSlot.Itemstack.Equals(world, cfg.Content.ResolvedItemstack,
				         GlobalConstants.IgnoredStackAttributes))
			{
				return cfg;
			}
		}

		return null;
	}
	private BlockEntityWateringTrough be;
}