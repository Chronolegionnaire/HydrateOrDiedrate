using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.winch
{
    public class InventoryWinch : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;
        
        public ItemSlot[] Slots
        {
            get { return this.slots; }
        }
        
        public InventoryWinch(string inventoryID, ICoreAPI api)
            : base(inventoryID, api)
        {
            this.slots = base.GenEmptySlots(2);
        }
        
        public InventoryWinch(string className, string instanceID, ICoreAPI api)
            : base(className, instanceID, api)
        {
            this.slots = base.GenEmptySlots(2);
        }
        
        public override int Count
        {
            get { return 2; }
        }
        
        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= this.Count)
                {
                    return null;
                }
                return this.slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= this.Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.slots[slotId] = value;
            }
        }
        
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this.slots = this.SlotsFromTreeAttributes(tree, this.slots, null);
        }
        
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(this.slots, tree);
        }
        
        protected override ItemSlot NewSlot(int i)
        {
            return new ItemSlotSurvival(this);
        }
        
        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == this.slots[0] && sourceSlot.Itemstack != null)
            {
                string itemCode = sourceSlot.Itemstack.Collectible.Code.ToString();
                if (itemCode == "game:woodbucket" || itemCode.StartsWith("vanvar:bucket-"))
                {
                    if (sourceSlot.Itemstack.StackSize > 1)
                    {
                        return 0f;
                    }
                    if (!sourceSlot.Itemstack.Attributes.HasAttribute("contents"))
                    {
                        return 4f;
                    }
                }
            }
            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        }


        
        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return this.slots[0];
        }
    }
}
