using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BlockWellSpring : Block
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

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (!TryGetOriginBlock(pos, out var originBlock))
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }
            else originBlock.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
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
    }
}