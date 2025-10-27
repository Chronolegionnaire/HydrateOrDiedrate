using System.Collections.Generic;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.FluidNetwork
{
    public static class FluidSearch
    {
        public static bool TryFindWellSpring(IWorldAccessor world, BlockPos start, out Wells.WellWater.BlockEntityWellSpring well, int maxVisited = 2048)
        {
            well = null;
            var first = start.DownCopy();

            bool found = PipeTraversal.TryFind(
                world,
                first,
                BlockFacing.UP,
                (w, p) => w.BlockAccessor.GetBlockEntity(p) is Wells.WellWater.BlockEntityWellSpring,
                maxVisited);

            if (!found) return false;
            if (world.BlockAccessor.GetBlockEntity(first) is Wells.WellWater.BlockEntityWellSpring beHere)
            {
                well = beHere;
                return true;
            }
            return false;
        }
    }

    public interface IFluidBlock
    {
        bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
        void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
    }
}
