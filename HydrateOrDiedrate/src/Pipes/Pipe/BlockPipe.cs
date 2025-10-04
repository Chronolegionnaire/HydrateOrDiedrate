using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Pipes.Pipe
{
    public class BlockPipe : Block
    {
        static readonly BlockFacing[] Faces =
        {
            BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH,
            BlockFacing.WEST, BlockFacing.UP, BlockFacing.DOWN
        };
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            MarkSelfAndNeighborsDirty(world, pos);
        }
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack stack, BlockSelection sel, ref string failureCode)
        {
            if (!base.TryPlaceBlock(world, byPlayer, stack, sel, ref failureCode)) return false;
            return true;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            MarkSelfAndNeighborsDirty(world, pos);
        }

        void MarkSelfAndNeighborsDirty(IWorldAccessor world, BlockPos pos)
        {
            world.BlockAccessor.MarkBlockDirty(pos);
            foreach (var f in Faces) world.BlockAccessor.MarkBlockDirty(pos.AddCopy(f));
        }
    }
}