using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    public static class Patch_AqueductOnServerTick1s_Reimpl_NoRef
    {
        static bool Prepare()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "HardcoreWater");
        }

        static MethodBase TargetMethod()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "HardcoreWater");
            if (assembly == null) return null;
            var aqueductType = assembly.GetType("HardcoreWater.ModBlockEntity.BlockEntityAqueduct");
            if (aqueductType == null) return null;
            return aqueductType.GetMethod(
                "onServerTick1s",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(float) },
                null
            );
        }

        private static bool TryGetMember<T>(object obj, string name, out T value)
        {
            value = default;
            if (obj == null) return false;
            var t = obj.GetType();
            var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null && typeof(T).IsAssignableFrom(pi.PropertyType))
            {
                var v = pi.GetValue(obj);
                if (v is T tv) { value = tv; return true; }
                if (v == null && !typeof(T).IsValueType) { value = default; return true; }
            }
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null && typeof(T).IsAssignableFrom(fi.FieldType))
            {
                var v = fi.GetValue(obj);
                if (v is T tv) { value = tv; return true; }
                if (v == null && !typeof(T).IsValueType) { value = default; return true; }
            }

            return false;
        }

        private static bool HasMember(object obj, string name)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            return t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null
                || t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
        }

        private static bool IsTypeOrImplements(Block b, string fullName)
        {
            if (b == null) return false;
            var t = b.GetType();

            if (t.FullName == fullName) return true;
            if (t.GetInterfaces().Any(i => i.FullName == fullName)) return true;
            for (var bt = t.BaseType; bt != null; bt = bt.BaseType)
                if (bt.FullName == fullName) return true;
            return t.Name.Contains("Aqueduct", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAirOrReplaceable(ICoreAPI api, BlockPos pos)
        {
            var ba = api.World.BlockAccessor;
            var solid = ba.GetBlock(pos);
            var fluid = ba.GetBlock(pos, BlockLayersAccess.Fluid);
            return (fluid == null || !fluid.IsLiquid()) &&
                   (solid == null || solid.BlockId == 0 || solid.Replaceable > 0);
        }

        private static bool IsOpenBelow(ICoreAPI api, BlockPos pos)
        {
            var ba = api.World.BlockAccessor;
            var below = pos.DownCopy();
            var mostSolid = ba.GetMostSolidBlock(below);
            if (mostSolid != null)
            {
                if (IsAqueductBlock(mostSolid)) return false;
                double barrier = mostSolid.GetLiquidBarrierHeightOnSide(BlockFacing.UP, below);
                if (barrier >= 1.0) return false;
            }
            var belowFluid = ba.GetBlock(below, BlockLayersAccess.Fluid);
            var belowSolid = ba.GetBlock(below);
            bool belowEmpty = (belowFluid == null || !belowFluid.IsLiquid()) &&
                              (belowSolid == null || belowSolid.BlockId == 0 || belowSolid.Replaceable > 0);
            return belowEmpty;
        }
        private static AssetLocation ResolveSpreadingWater(ICoreAPI api, Block currentFluid, int level, string flow)
        {
            level = GameMath.Clamp(level, 1, 6);
            if (IsHydrateWell(currentFluid))
            {
                string[] tokens = currentFluid.Code.Path.Split('-');
                if (tokens.Length >= 3 && tokens[0] == "wellwater")
                {
                    string type = tokens[1];
                    string pollution = tokens[2];
                    string path = $"wellwater-{type}-{pollution}-spreading-{flow}-{level}";
                    var nb = api.World.GetBlock(new AssetLocation("hydrateordiedrate", path));
                    if (nb != null) return nb.Code;
                }
            }
            string family = "water";
            if (currentFluid != null)
            {
                if (currentFluid.Code.BeginsWith("game", "salt")) family = "saltwater";
                else if (currentFluid.Code.BeginsWith("game", "boiling")) family = "boilingwater";
            }
            var loc = new AssetLocation("game", $"{family}-spreading-{flow}-{level}");
            var block = api.World.GetBlock(loc);
            if (block != null) return loc;
            loc = new AssetLocation("game", $"{family}-flowing-{level}");
            block = api.World.GetBlock(loc);
            if (block != null) return loc;

            return null;
        }

        private static void TrySpillFromEnd(ICoreAPI api, BlockPos endPos, Block currentFluid, int level)
        {
            var ba = api.World.BlockAccessor;

            if (!IsAirOrReplaceable(api, endPos)) return;
            if (IsAqueductBlock(ba.GetBlock(endPos))) return;
            var existingFluid = ba.GetBlock(endPos, BlockLayersAccess.Fluid);
            if (existingFluid != null && existingFluid.IsLiquid()) return;
            string flow = IsOpenBelow(api, endPos) ? "d" : "h";

            var spreadingLoc = ResolveSpreadingWater(api, currentFluid, Math.Min(6, level), flow);
            if (spreadingLoc == null) return;

            var spreading = api.World.GetBlock(spreadingLoc);
            if (spreading == null) return;

            ba.SetBlock(spreading.BlockId, endPos, BlockLayersAccess.Fluid);
            ba.TriggerNeighbourBlockUpdate(endPos);
            if (flow == "d")
            {
                var below = endPos.DownCopy();
                if (IsAirOrReplaceable(api, below))
                {
                    var nextLoc = ResolveSpreadingWater(api, spreading, Math.Max(1, Math.Min(5, level - 1)), "d");
                    if (nextLoc != null)
                    {
                        var next = api.World.GetBlock(nextLoc);
                        if (next != null)
                        {
                            ba.SetBlock(next.BlockId, below, BlockLayersAccess.Fluid);
                            ba.TriggerNeighbourBlockUpdate(below);
                        }
                    }
                }
            }
        }

        private static bool IsAqueductBlock(Block b)
            => IsTypeOrImplements(b, "HardcoreWater.ModBlock.BlockAqueduct")
               || IsTypeOrImplements(b, "HardcoreWater.ModBlock.IAqueduct");

        private static string GetAqueductOrientation(Block b)
        {
            if (b == null) return null;
            var t = b.GetType();
            var pi = t.GetProperty("Orientation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var val = pi?.GetValue(b);
            return val?.ToString();
        }

        private static bool GetAqueductIsEnclosed(Block b)
        {
            if (b == null) return false;
            var t = b.GetType();
            var pi = t.GetProperty("IsEnclosed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var val = pi?.GetValue(b);
            return val is bool bv && bv;
        }

        private static bool IsHydrateWell(Block bl)
            => bl?.Code?.Domain == "hydrateordiedrate"
               && bl.Code?.Path?.StartsWith("wellwater", StringComparison.OrdinalIgnoreCase) == true;

        private static int ParseWellwaterLevel(Block bl)
        {
            if (bl?.Code?.Path == null) return bl?.LiquidLevel ?? 0;
            var tokens = bl.Code.Path.Split('-');
            if (tokens.Length >= 6 && int.TryParse(tokens[5], out var lvl)) return lvl;
            return bl.LiquidLevel;
        }

        private static string ParseWellwaterFlow(Block bl)
        {
            if (bl?.Code?.Path == null) return null;
            var tokens = bl.Code.Path.Split('-');
            if (tokens.Length >= 5) return tokens[4];
            return null;
        }

        private static bool IsValidWaterSource(ICoreAPI api, BlockPos pos, int minLevel = 7)
        {
            var ba = api.World.BlockAccessor;
            var block = ba.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block == null) return false;

            if (IsHydrateWell(block))
            {
                var lvl = ParseWellwaterLevel(block);
                return lvl >= minLevel;
            }

            return block.IsLiquid() && (block.LiquidLevel >= minLevel);
        }

        private static bool IsValidWaterFall(ICoreAPI api, BlockPos pos)
        {
            var ba = api.World.BlockAccessor;
            var block = ba.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block == null) return false;

            if (IsHydrateWell(block))
            {
                var flow = ParseWellwaterFlow(block);
                var lvl = ParseWellwaterLevel(block);
                return (flow == "d" && lvl >= 6);
            }

            return block.IsLiquid() && block.LiquidLevel >= 6
                   && (block.Variant?["flow"] == "d");
        }

        private static bool IsValidWaterSourceOrWaterFall(ICoreAPI api, BlockPos pos, int minLevel = 7)
            => IsValidWaterSource(api, pos, minLevel) || IsValidWaterFall(api, pos);

        private static bool DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(ICoreAPI api, BlockPos pos)
        {
            var ba = api.World.BlockAccessor;
            var below = pos.DownCopy();
            var mostSolid = ba.GetMostSolidBlock(below);
            if (IsAqueductBlock(mostSolid)) return true;

            double barrier = mostSolid.GetLiquidBarrierHeightOnSide(BlockFacing.UP, below);
            return barrier >= 1.0;
        }

        private static bool IsPerpendicular(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            bool aNS = a == "n" || a == "s";
            bool bNS = b == "n" || b == "s";
            return aNS != bNS;
        }

        private static bool IsValidFilledAqueduct(ICoreAPI api, object self, BlockPos pos, int minLevel = 7)
        {
            var ba = api.World.BlockAccessor;
            var neighborBlock = ba.GetBlock(pos);
            if (!IsAqueductBlock(neighborBlock)) return false;
            var neighborBE = ba.GetBlockEntity(pos);
            if (neighborBE == null) return true;

            var selfPos = GetSelfPos(self);
            var ourBlock = ba.GetBlock(selfPos);
            string ourO = GetAqueductOrientation(ourBlock);
            string theirO = GetAqueductOrientation(neighborBlock);
            bool theirEnclosed = GetAqueductIsEnclosed(neighborBlock);
            TryGetMember<BlockPos>(neighborBE, "WaterSourcePos", out var adjWsp);
            TryGetMember<bool>(neighborBE, "HasWaterSource", out var neighborHasWater);
            TryGetMember<int>(neighborBE, "WaterLevel", out var neighborLevel);
            if (adjWsp != null && adjWsp.Equals(selfPos)) return false;
            int neighborFluidLevel = ba.GetBlock(pos, BlockLayersAccess.Fluid)?.LiquidLevel ?? 0;
            bool neighborFluidLooksFilled = neighborFluidLevel >= Math.Max(5, minLevel - 1);

            bool neighborLooksFilled =
                neighborHasWater || neighborLevel >= minLevel || neighborFluidLooksFilled;
            var selfWsp = GetSelfWaterSourcePos(self);
            bool mutualDependency =
                adjWsp != null && selfWsp != null &&
                adjWsp.Equals(selfPos) && selfWsp.Equals(GetEntityPos(neighborBE));
            bool orientationOk =
                (theirO == ourO) || !theirEnclosed || (IsPerpendicular(ourO, theirO) && neighborLooksFilled);

            return orientationOk && neighborLooksFilled && !mutualDependency;
        }

        private static IEnumerable<BlockPos> AllHorizNeighbors(BlockPos p)
        {
            yield return p.NorthCopy();
            yield return p.SouthCopy();
            yield return p.EastCopy();
            yield return p.WestCopy();
        }

        private static bool HasInvalidSourceDependency(ICoreAPI api, object self)
        {
            var ba = api.World.BlockAccessor;
            var selfPos = GetSelfPos(self);
            var selfWsp = GetSelfWaterSourcePos(self);

            var neighbors = AllHorizNeighbors(selfPos)
                .Select(bp => ba.GetBlockEntity(bp))
                .Where(be => be != null && HasMember(be, "WaterSourcePos") && HasMember(be, "Pos"))
                .ToArray();
            var neighborsUsingMe = neighbors.Where(be =>
            {
                TryGetMember<BlockPos>(be, "WaterSourcePos", out var wsp);
                return Equals(wsp, selfPos);
            }).ToList();

            if (neighborsUsingMe.Count < 2) return false;
            return neighborsUsingMe.Any(be => Equals(selfWsp, GetEntityPos(be)));
        }

        private static BlockPos GetEntityPos(object be)
        {
            return TryGetMember<BlockPos>(be, "Pos", out var p) ? p : null;
        }

        private static BlockPos GetSelfPos(object self)
        {
            return TryGetMember<BlockPos>(self, "Pos", out var p) ? p : null;
        }

        private static BlockPos GetSelfWaterSourcePos(object self)
        {
            return TryGetMember<BlockPos>(self, "WaterSourcePos", out var p) ? p : null;
        }

        private static void PlaceFluidForState(ICoreAPI api, dynamic self, int waterLevel)
        {
            var ba = api.World.BlockAccessor;
            var pos = (BlockPos)self.Pos;

            AssetLocation targetLoc = null;

            BlockPos srcPos = GetSelfWaterSourcePos(self);
            Block src = (srcPos != null) ? ba.GetBlock(srcPos, BlockLayersAccess.Fluid) : null;

            Block ourFluid = ba.GetBlock(pos, BlockLayersAccess.Fluid);
            int wl = Math.Max(1, Math.Min(7, waterLevel));

            if (IsHydrateWell(src))
            {
                var tokens = src.Code.Path.Split('-');
                if (tokens.Length >= 6 && tokens[0] == "wellwater")
                {
                    string type = tokens[1];
                    string pollution = tokens[2];
                    string flow = tokens[4];
                    int lvl = Math.Min(wl, 6);
                    string codePath = $"wellwater-{type}-{pollution}-spreading-{flow}-{lvl}";
                    var nb = api.World.GetBlock(new AssetLocation("hydrateordiedrate", codePath));
                    if (nb != null) targetLoc = nb.Code;
                }
            }

            if (targetLoc == null)
            {
                bool isSalt = ourFluid != null && ourFluid.Code.BeginsWith("game", "salt");
                bool isBoiling = ourFluid != null && ourFluid.Code.BeginsWith("game", "boiling");
                string family = isSalt ? "saltwater" : (isBoiling ? "boilingwater" : "water");
                string code = $"{family}-still-{Math.Min(7, wl)}";
                var nb = api.World.GetBlock(new AssetLocation("game", code));
                if (nb != null) targetLoc = nb.Code;
            }

            if (targetLoc != null)
            {
                var nb = api.World.GetBlock(targetLoc);
                if (nb != null)
                {
                    ba.SetBlock(nb.BlockId, pos, BlockLayersAccess.Fluid);
                    ba.TriggerNeighbourBlockUpdate(pos);
                    self.MarkDirty(true);
                }
            }
        }

        private static FieldInfo _fiReacquireTimeout;

        private static int GetReacquireTimeout(object aqueduct)
        {
            var t = aqueduct.GetType();
            _fiReacquireTimeout ??= t.GetField("WaterSourceReacquireTimeout",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (_fiReacquireTimeout == null) return 0;
            var val = _fiReacquireTimeout.GetValue(aqueduct);
            return val is int i ? i : 0;
        }

        private static void SetReacquireTimeout(object aqueduct, int value)
        {
            var t = aqueduct.GetType();
            _fiReacquireTimeout ??= t.GetField("WaterSourceReacquireTimeout",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (_fiReacquireTimeout == null) return;
            _fiReacquireTimeout.SetValue(aqueduct, value);
        }

        [HarmonyPrefix]
        static bool Prefix(object __instance, float dt)
        {
            dynamic aqueduct = __instance;
            ICoreAPI api = aqueduct.Api;
            if (api == null) return false;

            var ba = api.World.BlockAccessor;
            var ourBlock = ba.GetBlock((BlockPos)aqueduct.Pos);
            if (!IsAqueductBlock(ourBlock)) return false;
            var o = GetAqueductOrientation(ourBlock)?.Trim();
            var face = BlockFacing.NORTH;
            if (!string.IsNullOrEmpty(o))
            {
                face = BlockFacing.FromFirstLetter(o[0]);
            }

            BlockPos inlineA, inlineB, sideA, sideB;
            if (face == BlockFacing.NORTH)
            {
                inlineA = ((BlockPos)aqueduct.Pos).NorthCopy();
                inlineB = ((BlockPos)aqueduct.Pos).SouthCopy();
                sideA   = ((BlockPos)aqueduct.Pos).EastCopy();
                sideB   = ((BlockPos)aqueduct.Pos).WestCopy();
            }
            else
            {
                inlineA = ((BlockPos)aqueduct.Pos).WestCopy();
                inlineB = ((BlockPos)aqueduct.Pos).EastCopy();
                sideA   = ((BlockPos)aqueduct.Pos).NorthCopy();
                sideB   = ((BlockPos)aqueduct.Pos).SouthCopy();
            }

            IEnumerable<BlockPos> AcquireCandidates()
            {
                yield return inlineA;
                yield return inlineB;
                yield return sideA;
                yield return sideB;
            }

            if ((bool)aqueduct.HasWaterSource)
            {
                bool hasSource = false;
                BlockPos srcPos = GetSelfWaterSourcePos(aqueduct);
                bool unloadedSource = (srcPos != null) && (ba.GetChunkAtBlockPos(srcPos) == null);

                if (IsValidWaterSource(api, (BlockPos)aqueduct.Pos, 7) || unloadedSource)
                    hasSource = true;
                else if ((srcPos != null) && (IsValidWaterSource(api, srcPos) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, srcPos) || unloadedSource))
                    hasSource = true;
                else if ((srcPos != null) && (IsValidWaterFall(api, srcPos) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, srcPos) || unloadedSource))
                    hasSource = true;
                else if ((srcPos != null) && (IsValidWaterSource(api, srcPos, 5) && IsValidWaterSourceOrWaterFall(api, srcPos.UpCopy(), 5) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, srcPos) || unloadedSource))
                    hasSource = true;
                else if ((srcPos != null) && (IsValidWaterSource(api, (BlockPos)aqueduct.Pos, 6) && IsValidWaterSourceOrWaterFall(api, srcPos, 6) && (((BlockPos)aqueduct.Pos).Y == srcPos.Y - 1) || unloadedSource))
                    hasSource = true;
                else if ((srcPos != null) && (IsValidFilledAqueduct(api, aqueduct, srcPos, 6) || unloadedSource))
                    hasSource = true;

                if (!hasSource || HasInvalidSourceDependency(api, aqueduct))
                {
                    SetReacquireTimeout(aqueduct, 4);
                    aqueduct.HasWaterSource = false;
                    aqueduct.WaterSourcePos = null;
                    aqueduct.MarkDirty(true);
                    return false;
                }

                return false;
            }

            int reacq = GetReacquireTimeout(aqueduct);
            if (reacq > 0)
            {
                SetReacquireTimeout(aqueduct, reacq - 1);
                aqueduct.WaterLevel = Math.Max(0, (int)aqueduct.WaterLevel - 1);
                ba.TriggerNeighbourBlockUpdate((BlockPos)aqueduct.Pos);
                aqueduct.MarkDirty(true);
                return false;
            }

            bool acquired = false;
            BlockPos up = ((BlockPos)aqueduct.Pos).UpCopy();

            if (IsValidWaterSource(api, (BlockPos)aqueduct.Pos, 7))
            {
                aqueduct.WaterSourcePos = (BlockPos)aqueduct.Pos;
                aqueduct.WaterLevel = 7;
                acquired = true;
                aqueduct.HasWaterSource = true;
            }
            else if (IsValidWaterSource(api, up))
            {
                aqueduct.WaterSourcePos = up;
                aqueduct.WaterLevel = 6;
                acquired = true;
                aqueduct.HasWaterSource = true;
            }
            else if (IsValidWaterSourceOrWaterFall(api, up, 6))
            {
                aqueduct.WaterSourcePos = up;
                aqueduct.WaterLevel = 6;
                acquired = true;
                aqueduct.HasWaterSource = true;
            }
            else if (IsValidFilledAqueduct(api, aqueduct, up, 6))
            {
                aqueduct.WaterSourcePos = up;
                aqueduct.WaterLevel = 6;
                acquired = true;
                aqueduct.HasWaterSource = true;
            }

            if (!acquired)
            {
                foreach (var endPos in AcquireCandidates())
                {
                    if (IsValidWaterSource(api, endPos))
                    {
                        aqueduct.WaterSourcePos = endPos;
                        aqueduct.WaterLevel = 6;
                        acquired = true;
                        aqueduct.HasWaterSource = true;
                        break;
                    }
                    else if (IsValidWaterFall(api, endPos) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, endPos))
                    {
                        aqueduct.WaterSourcePos = endPos;
                        aqueduct.WaterLevel = 6;
                        acquired = true;
                        aqueduct.HasWaterSource = true;
                        break;
                    }
                    else if (IsValidWaterSource(api, endPos, 5) && IsValidWaterSourceOrWaterFall(api, endPos.UpCopy(), 5) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, endPos))
                    {
                        aqueduct.WaterSourcePos = endPos;
                        aqueduct.WaterLevel = 6;
                        acquired = true;
                        aqueduct.HasWaterSource = true;
                        break;
                    }
                    else if (IsValidFilledAqueduct(api, aqueduct, endPos, 6))
                    {
                        aqueduct.WaterSourcePos = endPos;
                        aqueduct.WaterLevel = 6;
                        acquired = true;
                        aqueduct.HasWaterSource = true;
                        break;
                    }
                }
            }

            if (acquired)
            {
                var ourFluid = ba.GetBlock((BlockPos)aqueduct.Pos, BlockLayersAccess.Fluid);
                bool notIced = ourFluid == null || !ourFluid.Code.Path.Contains("ice");
                if (notIced && (ourFluid == null || ourFluid.LiquidLevel < (int)aqueduct.WaterLevel)
                    && !HasInvalidSourceDependency(api, aqueduct))
                {
                    PlaceFluidForState(api, aqueduct, (int)aqueduct.WaterLevel);
                    var currentFluid = ba.GetBlock((BlockPos)aqueduct.Pos, BlockLayersAccess.Fluid);
                    TrySpillFromEnd(api, inlineA, currentFluid, (int)aqueduct.WaterLevel);
                    TrySpillFromEnd(api, inlineB, currentFluid, (int)aqueduct.WaterLevel);
                }
            }
            else
            {
                aqueduct.WaterLevel = Math.Max(0, (int)aqueduct.WaterLevel - 1);
                ba.TriggerNeighbourBlockUpdate((BlockPos)aqueduct.Pos);
                aqueduct.MarkDirty(true);
            }

            return false;
        }
    }
}
