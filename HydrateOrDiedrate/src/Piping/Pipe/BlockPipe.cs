using HydrateOrDiedrate.Piping.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public class BlockPipe : Block, IFluidBlock
    {

        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) => true;

        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.MarkBlockDirty(pos);
        }
        
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            FluidNetworkState.InvalidateNetwork();
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            base.OnBlockRemoved(world, pos);
            FluidNetworkState.InvalidateNetwork();
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            FluidNetworkState.InvalidateNetwork();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer?.InventoryManager?.ActiveTool == EnumTool.Wrench)
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

            if (disguise != null && disguise != this)
            {
                var boxes = disguise.GetCollisionBoxes(blockAccessor, pos);
                if (boxes != null && boxes.Length > 0)
                {
                    return boxes;
                }
            }
            return PipeCollision.BuildPipeBoxes(this, api.World, pos);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;

            if (disguise != null && disguise != this)
            {
                var boxes = disguise.GetSelectionBoxes(blockAccessor, pos);
                if (boxes != null && boxes.Length > 0)
                {
                    return boxes;
                }
            }
            return PipeCollision.BuildPipeBoxes(this, api.World, pos);
        }
    }
}