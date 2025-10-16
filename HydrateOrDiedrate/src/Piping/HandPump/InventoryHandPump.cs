using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class InventoryHandPump : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        public InventoryHandPump(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            slots = GenEmptySlots(Count);
        }

        public override int Count => 1;

        public ItemSlot ContainerSlot => slots[0];

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= Count) return null;
                return slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                slots[slotId] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree) => slots = SlotsFromTreeAttributes(tree, slots, null);
        public override void ToTreeAttributes(ITreeAttribute tree) => SlotsToTreeAttributes(slots, tree);

        protected override ItemSlot NewSlot(int i) => new ItemSlotHandPump(this)
        {
            MaxSlotStackSize = 1
        };
        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot) =>
            sourceSlot?.Itemstack?.Collectible is BlockLiquidContainerTopOpened
            && base.CanContain(sinkSlot, sourceSlot);

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot) => slots[0];
    }

    public class ItemSlotHandPump : ItemSlot
    {
        public ItemSlotHandPump(InventoryBase inventory) : base(inventory) { }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (sourceSlot?.Itemstack?.Collectible is not BlockLiquidContainerTopOpened) return false;
            if (!base.CanHold(sourceSlot)) return false;
            if (!Empty && StackSize >= 1) return false;
            return true;
        }
    }
}
