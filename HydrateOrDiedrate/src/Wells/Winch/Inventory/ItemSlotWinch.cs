using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Wells.Winch.Inventory;

public class ItemSlotWinch(InventoryBase inventory, BlockEntityWinch winch) : ItemSlot(inventory)
{
    public override bool CanTake() => winch.IsWinchItemAtTop() && base.CanTake();
    
    //TODO maybe prevent player from inputting items when the block directly below is occupied
    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) => winch.IsWinchItemAtTop() && base.CanTakeFrom(sourceSlot, priority);
}
