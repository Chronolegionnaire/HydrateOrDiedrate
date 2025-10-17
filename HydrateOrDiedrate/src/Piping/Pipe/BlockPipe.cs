using HydrateOrDiedrate.Piping.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

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

        static bool IsWrench(ItemSlot slot)
        {
            var code = slot?.Itemstack?.Collectible?.Code?.ToString();
            return code != null && WildcardUtil.Match("game:wrench-*", code);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (IsWrench(byPlayer?.InventoryManager?.ActiveHotbarSlot))
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityPipe be)
                {
                    be.TryOpenGui(byPlayer);
                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;
            if (disguise != null)
                return disguise.GetCollisionBoxes(blockAccessor, pos);
            return PipeCollision.BuildPipeBoxes(this, blockAccessor, pos);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;
            if (disguise != null)
                return disguise.GetSelectionBoxes(blockAccessor, pos);

            return PipeCollision.BuildPipeBoxes(this, blockAccessor, pos);
        }
    }
}