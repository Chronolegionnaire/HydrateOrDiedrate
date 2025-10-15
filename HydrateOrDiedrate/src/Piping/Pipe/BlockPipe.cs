using HydrateOrDiedrate.Piping.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public class BlockPipe : Block, IFluidBlock
    {
        static readonly BlockFacing[] Faces =
        {
            BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH,
            BlockFacing.WEST,  BlockFacing.UP,   BlockFacing.DOWN
        };

        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) => true;

        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.MarkBlockDirty(pos);
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