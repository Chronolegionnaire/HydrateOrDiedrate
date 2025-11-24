using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.FluidNetwork
{
    public static class FluidNetworkState
    {
        private static int networkVersion;

        public static int NetworkVersion => networkVersion;

        public static void InvalidateNetwork()
        {
            networkVersion++;
        }
    }
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
                other != null
                && Pos.X == other.Pos.X
                && Pos.Y == other.Pos.Y
                && Pos.Z == other.Pos.Z
                && CameFrom == other.CameFrom;

            public override bool Equals(object obj) => Equals(obj as EdgeState);

            public override int GetHashCode() =>
                (Pos.X * 73856093) ^ (Pos.Y * 19349663) ^ (Pos.Z * 83492791) ^ CameFrom.Index;
        }
        public static bool TryFind(
            IWorldAccessor world,
            BlockPos startPos,
            BlockFacing startCameFrom,
            Vintagestory.API.Common.Func<IWorldAccessor, BlockPos, Block, bool> matchNonPipe,
            int maxVisited = 2048)
        {
            if (world == null || startPos == null || matchNonPipe == null)
                return false;

            var q = new Queue<EdgeState>();
            var seen = new HashSet<EdgeState>();
            var start = new EdgeState(startPos.Copy(), startCameFrom);

            q.Enqueue(start);
            seen.Add(start);

            var blockAccessor = world.BlockAccessor;
            var nextPos = startPos.Copy();

            while (q.Count > 0 && seen.Count <= maxVisited)
            {
                var cur = q.Dequeue();
                var curBlock = blockAccessor.GetBlock(cur.Pos);
                if (!(curBlock is IFluidBlock) && matchNonPipe(world, cur.Pos, curBlock))
                    return true;

                foreach (var dir in BlockFacing.ALLFACES)
                {
                    if (!HasConnector(curBlock, world, cur.Pos, dir)) continue;
                    if (!AllowsPassageThrough(curBlock, world, cur.Pos, cur.CameFrom, dir)) continue;
                    nextPos.Set(cur.Pos).Add(dir);

                    var nextBlock = blockAccessor.GetBlock(nextPos);

                    if (nextBlock is IFluidBlock)
                    {
                        if (!HasConnector(nextBlock, world, nextPos, dir.Opposite)) continue;

                        if (nextBlock is IFluidGate pipeGate &&
                            !pipeGate.AllowsFluidPassage(world, nextPos, dir.Opposite, dir))
                        {
                            continue;
                        }
                        var next = new EdgeState(nextPos.Copy(), dir.Opposite);
                        if (!seen.Add(next)) continue;
                        q.Enqueue(next);
                    }
                    else
                    {
                        if (nextBlock is IFluidGate entryGate &&
                            !entryGate.AllowsFluidPassage(world, nextPos, dir.Opposite, dir))
                        {
                            continue;
                        }
                        if (matchNonPipe(world, nextPos, nextBlock))
                            return true;
                    }
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
            if (world == null || startPos == null || match == null)
                return -1;

            var q = new Queue<(EdgeState st, int dist)>();
            var seen = new HashSet<EdgeState>();

            var start = new EdgeState(startPos.Copy(), startCameFrom);

            q.Enqueue((start, 0));
            seen.Add(start);

            var blockAccessor = world.BlockAccessor;
            var nextPos = startPos.Copy();

            while (q.Count > 0 && seen.Count <= maxVisited)
            {
                var (cur, dist) = q.Dequeue();
                var curBlock = blockAccessor.GetBlock(cur.Pos);

                if (!(curBlock is IFluidBlock))
                {
                    if (match(world, cur.Pos))
                        return dist;

                    continue;
                }

                foreach (var dir in BlockFacing.ALLFACES)
                {
                    if (!HasConnector(curBlock, world, cur.Pos, dir)) continue;
                    if (!AllowsPassageThrough(curBlock, world, cur.Pos, cur.CameFrom, dir)) continue;

                    nextPos.Set(cur.Pos).Add(dir);
                    var nextBlock = blockAccessor.GetBlock(nextPos);

                    if (nextBlock is IFluidBlock)
                    {
                        if (!HasConnector(nextBlock, world, nextPos, dir.Opposite)) continue;

                        if (nextBlock is IFluidGate pipeGate &&
                            !pipeGate.AllowsFluidPassage(world, nextPos, dir.Opposite, dir))
                        {
                            continue;
                        }

                        var next = new EdgeState(nextPos.Copy(), dir.Opposite);
                        if (!seen.Add(next)) continue;
                        q.Enqueue((next, dist + 1));
                    }
                    else
                    {
                        if (nextBlock is IFluidGate entryGate &&
                            !entryGate.AllowsFluidPassage(world, nextPos, dir.Opposite, dir))
                        {
                            continue;
                        }

                        if (match(world, nextPos))
                            return dist + 1;
                    }
                }
            }

            return -1;
        }

        static bool HasConnector(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            var b = world.BlockAccessor.GetBlock(pos);
            return HasConnector(b, world, pos, face);
        }

        static bool HasConnector(Block block, IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            return (block as IFluidBlock)?.HasFluidConnectorAt(world, pos, face) ?? false;
        }

        static bool AllowsPassageThrough(IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to)
        {
            var b = world.BlockAccessor.GetBlock(pos);
            return AllowsPassageThrough(b, world, pos, from, to);
        }

        static bool AllowsPassageThrough(Block block, IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to)
        {
            if (block is IFluidGate gate) return gate.AllowsFluidPassage(world, pos, from, to);
            return true;
        }
    }

    public static class FluidSearch
    {
        public static bool TryFindWellSpring(
            IWorldAccessor world,
            BlockPos start,
            out BlockEntityWellSpring well,
            int maxVisited = 4096)
        {
            return TryFindWellSpring(world, start, BlockFacing.DOWN, out well, maxVisited);
        }

        public static bool TryFindWellSpring(
            IWorldAccessor world,
            BlockPos start,
            BlockFacing startFace,
            out BlockEntityWellSpring well,
            int maxVisited = 4096)
        {
            well = null;
            BlockEntityWellSpring found = null;

            bool ok = PipeTraversal.TryFind(
                world,
                start,
                startFace,
                (w, p, block) =>
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
}
