using System;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells;

public static class WellBlockUtils
{
    public static bool SolidAllows(Block solid) =>
        solid == null || solid.Code == null || solid.Code.Path == "air" || solid.Replaceable >= 500;

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
        var sentinel = ba.GetBlockEntity<BlockEntityWellWaterSentinel>(probe)
                       ?? (probe.Equals(pos) ? null : ba.GetBlockEntity<BlockEntityWellWaterSentinel>(pos));
        return sentinel?.TryGetGoverningSpring();
    }
}