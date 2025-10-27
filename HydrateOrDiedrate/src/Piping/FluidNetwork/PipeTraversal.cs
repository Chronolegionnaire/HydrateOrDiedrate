using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.FluidNetwork
{
    public static class PipeTraversal
    {
        public sealed class EdgeState : IEquatable<EdgeState>
        {
            public readonly BlockPos Pos;
            public readonly BlockFacing CameFrom;

            public EdgeState(BlockPos pos, BlockFacing cameFrom)
            {
                Pos = pos;
                CameFrom = cameFrom;
            }

            public bool Equals(EdgeState other) =>
                other != null && Pos.X == other.Pos.X && Pos.Y == other.Pos.Y && Pos.Z == other.Pos.Z && CameFrom == other.CameFrom;

            public override bool Equals(object obj) => Equals(obj as EdgeState);
            public override int GetHashCode() => (Pos.X * 73856093) ^ (Pos.Y * 19349663) ^ (Pos.Z * 83492791) ^ CameFrom.Index;
        }

        public static bool TryFind(
            IWorldAccessor world,
            BlockPos startPos,
            BlockFacing startCameFrom,
            Vintagestory.API.Common.Func<IWorldAccessor, BlockPos, bool> match,
            int maxVisited = 2048)
        {
            if (world == null || startPos == null || match == null) return false;

            var q = new Queue<EdgeState>();
            var seen = new HashSet<EdgeState>();
            var start = new EdgeState(startPos, startCameFrom);

            q.Enqueue(start);
            seen.Add(start);

            while (q.Count > 0 && seen.Count <= maxVisited)
            {
                var cur = q.Dequeue();
                if (match(world, cur.Pos)) return true;
                foreach (var dir in BlockFacing.ALLFACES)
                {
                    var nextPos = cur.Pos.AddCopy(dir);
                    if (!HasConnector(world, cur.Pos, dir)) continue;
                    if (!AllowsPassageThrough(world, cur.Pos, cur.CameFrom, dir)) continue;
                    if (match(world, nextPos))
                    {
                        var nb = world.BlockAccessor.GetBlock(nextPos);
                        if (nb is IFluidGate ngate)
                        {
                            if (!ngate.AllowsFluidPassage(world, nextPos, dir.Opposite, dir))
                                continue;
                        }

                        return true;
                    }
                    if (!HasConnector(world, nextPos, dir.Opposite)) continue;
                    var nb2 = world.BlockAccessor.GetBlock(nextPos);
                    if (nb2 is IFluidGate entryGate)
                    {
                        if (!entryGate.AllowsFluidPassage(world, nextPos, dir.Opposite, dir)) continue;
                    }

                    var next = new EdgeState(nextPos, dir.Opposite);
                    if (!seen.Add(next)) continue;
                    q.Enqueue(next);
                }
            }

            return false;
        }

        public static int Distance(
            IWorldAccessor world,
            BlockPos startPos,
            BlockFacing startCameFrom,
            Vintagestory.API.Common.Func<IWorldAccessor, BlockPos, bool> match,
            int maxVisited = 4096)
        {
            var q = new Queue<(EdgeState st, int dist)>();
            var seen = new HashSet<EdgeState>();
            var start = new EdgeState(startPos, startCameFrom);

            q.Enqueue((start, 0));
            seen.Add(start);

            while (q.Count > 0 && seen.Count <= maxVisited)
            {
                var (cur, dist) = q.Dequeue();
                if (match(world, cur.Pos)) return dist;

                foreach (var dir in BlockFacing.ALLFACES)
                {
                    var nextPos = cur.Pos.AddCopy(dir);

                    if (!HasConnector(world, cur.Pos, dir)) continue;
                    if (!HasConnector(world, nextPos, dir.Opposite)) continue;
                    if (!AllowsPassageThrough(world, cur.Pos, cur.CameFrom, dir)) continue;

                    var next = new EdgeState(nextPos, dir.Opposite);
                    if (!seen.Add(next)) continue;
                    q.Enqueue((next, dist + 1));
                }
            }

            return -1;
        }

        static bool HasConnector(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            var b = world.BlockAccessor.GetBlock(pos);
            return (b as IFluidBlock)?.HasFluidConnectorAt(world, pos, face) ?? false;
        }

        static bool AllowsPassageThrough(IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to)
        {
            var b = world.BlockAccessor.GetBlock(pos);
            if (b is IFluidGate gate) return gate.AllowsFluidPassage(world, pos, from, to);
            return true;
        }
    }
    public interface IFluidGate
    {
        bool AllowsFluidPassage(IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to);
    }
}
