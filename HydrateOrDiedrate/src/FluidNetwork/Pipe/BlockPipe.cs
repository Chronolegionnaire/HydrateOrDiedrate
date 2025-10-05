using HydrateOrDiedrate.FluidNetwork;  // <- important
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Pipes.Pipe
{
    public class BlockPipe : Block, FluidInterfaces.IFluidBlock
    {
        static readonly BlockFacing[] Faces =
        {
            BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH,
            BlockFacing.WEST,  BlockFacing.UP,   BlockFacing.DOWN
        };

        // ===== IFluidBlock =====

        // All 6 faces connect if the neighbor is also a fluid block / pipe
        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            // Optional: gate by shape/variant; default: all faces open.
            return true;
        }

        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.MarkBlockDirty(pos);
        }

        public FluidNetwork.FluidNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos);
            // Prefer the pipe behavior, but fall back to any fluid base behavior
            return be?.GetBehavior<BEBehaviorPipe>()?.Network
                   ?? be?.GetBehavior<BEBehaviorFluidBase>()?.Network;
        }

        // ===== vanilla hooks (unchanged) =====

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);

            var beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorPipe>();
            if (beh != null)
            {
                foreach (var f in BlockFacing.ALLFACES) beh.TryConnect(f);
            }
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            var beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorPipe>();
            if (beh != null)
            {
                foreach (var f in BlockFacing.ALLFACES) beh.TryConnect(f);
            }
            MarkSelfAndNeighborsDirty(world, pos);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack stack, BlockSelection sel, ref string failureCode)
        {
            if (!base.TryPlaceBlock(world, byPlayer, stack, sel, ref failureCode)) return false;
            return true;
        }

        void MarkSelfAndNeighborsDirty(IWorldAccessor world, BlockPos pos)
        {
            world.BlockAccessor.MarkBlockDirty(pos);
            foreach (var f in Faces) world.BlockAccessor.MarkBlockDirty(pos.AddCopy(f));
        }
    }
}
