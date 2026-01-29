using System.Linq;
using HydrateOrDiedrate.Piping.FluidNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

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
        
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) =>
        [
            new WorldInteraction
            {
                ActionLangCode = "hydrateordiedrate:blockhelp-pipe-disguise",
                MouseButton = EnumMouseButton.Right,
                Itemstacks = ObjectCacheUtil.GetOrCreate(api, "hod-pipe-wrenchstacks", () =>
                {
                    return api.World.Collectibles
                        .Where(c => c.Tool == EnumTool.Wrench)
                        .Select(c => new ItemStack(c))
                        .ToArray();
                })
            },

            ..base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
        ];
        
        public override bool SideIsSolid(IBlockAccessor blockAccess, BlockPos pos, int faceIndex)
        {
            if (blockAccess == null || pos == null) return base.SideIsSolid(pos, faceIndex);

            var be = blockAccess.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;

            return (disguise != null)
                ? disguise.SideIsSolid(blockAccess, pos, faceIndex)
                : base.SideIsSolid(blockAccess, pos, faceIndex);
        }
        public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
        {
            if (pos == null) return base.GetRetention(pos, facing, type);

            var accessor = api.World.BlockAccessor;
            if (accessor == null) return base.GetRetention(pos, facing, type);

            var be = accessor.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;

            if (disguise != null)
            {
                return disguise.GetRetention(pos, facing, type);
            }

            return base.GetRetention(pos, facing, type);
        }
        public override EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
        {
            if (blockAccessor == null || pos == null) return base.GetBlockMaterial(blockAccessor, pos, stack);

            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;

            return (disguise != null)
                ? disguise.GetBlockMaterial(blockAccessor, pos, stack)
                : base.GetBlockMaterial(blockAccessor, pos, stack);
        }
        public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
        {
            if (pos == null) return base.GetLiquidBarrierHeightOnSide(face, pos);

            var accessor = api.World.BlockAccessor;
            if (accessor == null) return base.GetLiquidBarrierHeightOnSide(face, pos);

            var be = accessor.GetBlockEntity(pos) as BlockEntityPipe;
            var disguise = be?.DisguiseSlot?.Itemstack?.Block;

            return (disguise != null)
                ? disguise.GetLiquidBarrierHeightOnSide(face, pos)
                : base.GetLiquidBarrierHeightOnSide(face, pos);
        }
    }
}