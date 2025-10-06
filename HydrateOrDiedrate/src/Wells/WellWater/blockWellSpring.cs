using System;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BlockWellSpring : Block, FluidInterfaces.IFluidBlock
    {
        public override BlockSounds GetSounds(IBlockAccessor blockAccessor, BlockSelection blockSel, ItemStack stack = null)
        {
            if (!TryGetOriginBlock(blockSel?.Position, out var originBlock)) return base.GetSounds(blockAccessor, blockSel, stack);

            return originBlock.GetSounds(blockAccessor, blockSel, stack);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (!TryGetOriginBlock(pos, out var originBlock)) return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

            return originBlock.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        }

        private bool TryGetOriginBlock(BlockPos pos, out Block result)
        {
            result = GetBlockEntity<BlockEntityWellSpring>(pos)?.OriginBlock;
            return result is not null;
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            if (!TryGetOriginBlock(pos, out var originBlock)) return base.GetRandomColor(capi, pos, facing, rndIndex);

            return originBlock.GetRandomColor(capi, pos, facing, rndIndex);
        }
        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) => true;

        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.GetBlockEntity(pos)?.MarkDirty();
        }

        public FluidNetwork.FluidNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
        {
            // Prefer our well-spring node behavior if present
            var be = world.BlockAccessor.GetBlockEntity(pos);
            return be?.GetBehavior<BEBehaviorWellSpringNode>()?.Network
                   ?? be?.GetBehavior<BEBehaviorFluidBase>()?.Network;
        }

        // On placement/neighbour changes, try to connect to nearby pipes/nodes on all faces
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            var beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorWellSpringNode>();
            if (beh != null)
            {
                foreach (var f in BlockFacing.ALLFACES) beh.TryConnect(f);
            }
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            var beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorWellSpringNode>();
            if (beh != null)
            {
                foreach (var f in BlockFacing.ALLFACES) beh.TryConnect(f);
            }
        }
    }
}