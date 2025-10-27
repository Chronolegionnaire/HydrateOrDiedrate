using System.Collections.Generic;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.FluidNetwork
{
    public static class FluidSearch
    {
        public sealed class PosCmp : IEqualityComparer<BlockPos>
        {
            public bool Equals(BlockPos a, BlockPos b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
            public int GetHashCode(BlockPos p) => (p.X * 397) ^ (p.Y * 17) ^ p.Z;
        }

        public static bool TryFindWellSpring(
            IWorldAccessor world,
            BlockPos start,
            out BlockEntityWellSpring well,
            int maxVisited = 4096)
        {
            well = null;
            BlockEntityWellSpring found = null;

            bool ok = PipeTraversal.TryFind(
                world,
                start,
                BlockFacing.DOWN,
                (w, p) =>
                {
                    var be = w.BlockAccessor.GetBlockEntity(p);
                    if (be is BlockEntityWellSpring ws)
                    {
                        found = ws;
                        return true;
                    }
                    return false;
                },
                maxVisited
            );

            if (!ok || found == null) return false;
            well = found;
            return true;
        }
    }

    public interface IFluidBlock
    {
        bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
        void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
    }
}
