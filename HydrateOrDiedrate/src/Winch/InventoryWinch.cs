using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.winch
{
    public class InventoryWinch : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        public InventoryWinch(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            slots = GenEmptySlots(Count);
        }

        public InventoryWinch(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
            slots = GenEmptySlots(Count);
        }

        public override int Count => 1;

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

        protected override ItemSlot NewSlot(int i) => new(this)
        {
            MaxSlotStackSize = 1
        };

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == slots[0] && sourceSlot.Itemstack?.Collectible is BlockBucket)
            {
                if (sourceSlot.Itemstack.StackSize > 1) return 0f;

                if (!sourceSlot.Itemstack.Attributes.HasAttribute("contents")) return 4f;
            }

            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        }

        //TODO: maybe come up with some better more complex constraints
        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot) => sourceSlot.Itemstack?.Collectible is BlockLiquidContainerTopOpened && base.CanContain(sinkSlot, sourceSlot);

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot) => slots[0];
    }
}
