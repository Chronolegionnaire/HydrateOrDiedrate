using System;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells;

public static class WellBlockUtils
{
    public static bool IsValidShaftPosition(IBlockAccessor ba, BlockPos pos)
    {
        if(!SolidAllows(ba.GetSolid(pos))) return false;

        var tmpPos = pos.Copy();
        for(var i = 0; i < 4; i++)
        {
            tmpPos.IterateHorizontalOffsets(i);
            var block = ba.GetSolid(tmpPos);
            //TODO we can use this to allow slabs to be valid well walls and only fill up to the slab level
            if(block.GetLiquidBarrierHeightOnSide(BlockFacing.HORIZONTALS[i].Opposite, tmpPos) < 1) return false;
        }
        return true;
    }

    public static bool SolidAllows(Block solid) => solid == null || solid.Code == null || solid.Code.Path == "air" || solid.Replaceable >= 500;

    public static bool FluidIsLiquid(this IBlockAccessor accessor, BlockPos pos) =>
        accessor.GetBlock(pos, BlockLayersAccess.Fluid)?.IsLiquid() == true;

    public static bool CellAllowsMove(this IBlockAccessor accessor, BlockPos pos) =>
        SolidAllows(accessor.GetBlock(pos, BlockLayersAccess.Solid)) || accessor.FluidIsLiquid(pos);

    public static Block GetFluid(this IBlockAccessor accessor, BlockPos p) =>
        accessor.GetBlock(p, BlockLayersAccess.Fluid);

    public static Block GetSolid(this IBlockAccessor accessor, BlockPos p) =>
        accessor.GetBlock(p, BlockLayersAccess.Solid);

    public static void SetFluid(this IBlockAccessor accessor, int blockId, BlockPos p) =>
        accessor.SetBlock(blockId, p, BlockLayersAccess.Fluid);

    public static bool IsOurWellwater(Block b) =>
        b?.Code != null
        && string.Equals(b.Code.Domain, "hydrateordiedrate", StringComparison.OrdinalIgnoreCase)
        && b.Code.Path.StartsWith("wellwater", StringComparison.Ordinal);
    
    public static BlockEntityWellSpring FindGoverningSpring(ICoreAPI api, Block blockAtPos, BlockPos pos)
    {
        if (api == null || pos == null) return null;
        var ba = api.World.BlockAccessor;
        var behavior = blockAtPos?.GetBehavior<BlockBehaviorWellWaterFinite>();
        BlockPos probe = behavior?.FindNaturalSourceInLiquidChain(ba, pos) ?? pos;
        var scan = probe.DownCopy();

        for (int i = 0; i < 64; i++)
        {
            var block = ba.GetBlock(scan);
            if (block is BlockWellSpring)
            {
                return ba.GetBlockEntity<BlockEntityWellSpring>(scan);
            }
            if (!SolidAllows(ba.GetSolid(scan)))
                break;

            scan.Y--;
            if (scan.Y <= 0) break;
        }

        return null;
    }
}