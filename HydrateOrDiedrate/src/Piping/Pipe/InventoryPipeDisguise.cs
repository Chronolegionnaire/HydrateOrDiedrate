using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public class ItemSlotPipeDisguise : ItemSlot
    {
        public ItemSlotPipeDisguise(InventoryBase inv) : base(inv)
        {
            MaxSlotStackSize = 1;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (sourceSlot?.Itemstack == null) return false;
            return sourceSlot.Itemstack.Block != null && base.CanHold(sourceSlot);
        }
    }
    public class InventoryPipeDisguise : InventoryGeneric
    {
        public InventoryPipeDisguise(string invID, ICoreAPI api) : base(1, invID, api) { }
        protected override ItemSlot NewSlot(int i) => new ItemSlotPipeDisguise(this);
    }
}