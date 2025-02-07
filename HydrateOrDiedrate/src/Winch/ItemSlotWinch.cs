using Vintagestory.API.Common;

namespace HydrateOrDiedrate.winch
{
    public class ItemSlotWinch : ItemSlot
    {
        public ItemSlotWinch(InventoryBase inventory) : base(inventory)
        {
        }
        public override int MaxSlotStackSize
        {
            get => 1;
            set {}
        }
    }
}