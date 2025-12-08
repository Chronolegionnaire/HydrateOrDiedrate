using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            var wellSpring = GetBlockEntity<BlockEntityWellSpring>(pos);
            if(wellSpring is null) return base.GetPlacedBlockName(world, pos);

            StringBuilder stringBuilder = new();
            stringBuilder.Append(Lang.Get(
                wellSpring.IsShallow
                ? "hydrateordiedrate:block-wellspring-shallow"
                : "hydrateordiedrate:block-wellspring-deep"
            ));

            BlockBehavior[] blockBehaviors = BlockBehaviors;
            for (int i = 0; i < blockBehaviors.Length; i++)
            {
                blockBehaviors[i].GetPlacedBlockName(stringBuilder, world, pos);
            }

            return stringBuilder.ToString().TrimEnd();
        }
    }
}