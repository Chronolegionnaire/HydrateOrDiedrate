// HydrateOrDiedrate/FluidNetwork/FluidSearch.cs
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

        public static bool TryFindWellSpring(IWorldAccessor world, BlockPos start, out BlockEntityWellSpring well, int maxVisited = 512)
        {
            well = null;

            var open = new Queue<BlockPos>();
            var seen = new HashSet<BlockPos>(new PosCmp());

            var first = start.DownCopy();
            open.Enqueue(first);
            seen.Add(first);

            while (open.Count > 0 && seen.Count <= maxVisited)
            {
                var cur = open.Dequeue();

                if (world.BlockAccessor.GetBlockEntity(cur) is BlockEntityWellSpring beHere)
                {
                    well = beHere;
                    return true;
                }

                foreach (var face in BlockFacing.ALLFACES)
                {
                    var next = cur.AddCopy(face);
                    if (!seen.Add(next)) continue;

                    var beNext = world.BlockAccessor.GetBlockEntity(next) as BlockEntityWellSpring;
                    if (beNext != null)
                    {
                        well = beNext;
                        return true;
                    }

                    var nb = world.BlockAccessor.GetBlock(next);
                    if (nb is IFluidBlock nFluid && nFluid.HasFluidConnectorAt(world, next, face.Opposite))
                    {
                        open.Enqueue(next);
                    }
                }
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
