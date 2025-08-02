using HydrateOrDiedrate.winch;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Winch.Inventory;

public class ItemSlotWinch : ItemSlot
{
    public readonly BlockEntityWinch Winch;

    public ItemSlotWinch(InventoryBase inventory, BlockEntityWinch winch) : base(inventory)
    {
        Winch = winch;
    }

    public override bool CanTake() => Winch.bucketDepth < 1f && base.CanTake();

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) => (Winch.bucketDepth < 1f || Empty) && base.CanTakeFrom(sourceSlot, priority);
}
