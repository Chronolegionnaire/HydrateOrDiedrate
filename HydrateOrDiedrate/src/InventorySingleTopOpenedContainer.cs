using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate
{
    public class InventorySingleTopOpenedContainer : InventoryBase, ISlotProvider
    {
        protected ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        private readonly Func<bool> canTakePredicate;

        public InventorySingleTopOpenedContainer(string inventoryID, ICoreAPI api, Func<bool> canTakePredicate = null)
            : base(inventoryID, api)
        {
            this.canTakePredicate = canTakePredicate;
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
                if (slotId < 0 || slotId >= Count)
                    throw new ArgumentOutOfRangeException(nameof(slotId));

                slots[slotId] = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots, null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }
        protected override ItemSlot NewSlot(int i) =>
            new TopOpenedContainerSlot(this, canTakePredicate);

        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot) =>
            sourceSlot?.Itemstack?.Collectible is BlockLiquidContainerTopOpened
            && base.CanContain(sinkSlot, sourceSlot);

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot) => slots[0];
    }

    public class TopOpenedContainerSlot : ItemSlot
    {
        private readonly Func<bool> canTakePredicate;
        public TopOpenedContainerSlot(InventoryBase inventory, Func<bool> canTakePredicate = null)
            : base(inventory)
        {
            this.canTakePredicate = canTakePredicate;
            MaxSlotStackSize = 1;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (sourceSlot?.Itemstack?.Collectible is not BlockLiquidContainerTopOpened)
                return false;

            if (!base.CanHold(sourceSlot))
                return false;

            if (!Empty && StackSize >= 1)
                return false;

            return true;
        }

        public override bool CanTake()
        {
            if (canTakePredicate != null && !canTakePredicate())
                return false;

            return base.CanTake();
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            if (canTakePredicate != null && !canTakePredicate())
                return false;

            return base.CanTakeFrom(sourceSlot, priority);
        }
    }
}
