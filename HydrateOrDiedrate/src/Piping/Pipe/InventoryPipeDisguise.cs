using HydrateOrDiedrate.Piping.Pipe;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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

            var stack = sourceSlot.Itemstack;
            var block = stack.Block;
            if (block == null) return false;
            if (block is BlockPipe) return false;
            var invBase = inventory as InventoryBase;
            var api = invBase?.Api;
            if (api != null)
            {
                var ba = api.World.BlockAccessor;
                var dummyPos = new BlockPos(0, 0, 0);

                var collBoxes = block.GetCollisionBoxes(ba, dummyPos);
                var selBoxes  = block.GetSelectionBoxes(ba, dummyPos);

                if (!HasUsableBoxes(collBoxes) || !HasUsableBoxes(selBoxes))
                {
                    return false;
                }
            }

            return base.CanHold(sourceSlot);
        }

        private bool HasUsableBoxes(Cuboidf[] boxes)
        {
            if (boxes == null || boxes.Length == 0) return false;

            for (int i = 0; i < boxes.Length; i++)
            {
                var b = boxes[i];
                if (b.X2 > b.X1 && b.Y2 > b.Y1 && b.Z2 > b.Z1)
                {
                    return true;
                }
            }

            return false;
        }
    }
    public class InventoryPipeDisguise : InventoryGeneric
    {
        public InventoryPipeDisguise(string invID, ICoreAPI api) : base(1, invID, api) { }
        protected override ItemSlot NewSlot(int i) => new ItemSlotPipeDisguise(this);
    }

}