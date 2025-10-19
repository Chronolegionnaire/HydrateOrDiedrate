using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatchCategory("HydrateOrDiedrate.HardcoreWater")]
    [HarmonyPatch]
    public static class BlockEntityAqueductPatch
    {
        private static readonly Type TAqueductEntity = AccessTools.TypeByName("HardcoreWater.ModBlockEntity.BlockEntityAqueduct");
        private static readonly Type TIAqueduct      = AccessTools.TypeByName("HardcoreWater.ModBlock.IAqueduct");
        private static readonly Type TBlockAqueduct  = AccessTools.TypeByName("HardcoreWater.ModBlock.BlockAqueduct");

        private static readonly PropertyInfo PI_WaterLevel     = TAqueductEntity != null ? AccessTools.Property(TAqueductEntity, "WaterLevel")     : null;
        private static readonly PropertyInfo PI_WaterSourcePos = TAqueductEntity != null ? AccessTools.Property(TAqueductEntity, "WaterSourcePos") : null;
        private static readonly PropertyInfo PI_HasWaterSource = TAqueductEntity != null ? AccessTools.Property(TAqueductEntity, "HasWaterSource") : null;

        private static int GetWaterLevel(object inst) => (int)PI_WaterLevel.GetValue(inst);
        private static void SetWaterLevel(object inst, int v) => PI_WaterLevel.SetValue(inst, v);
        private static BlockPos GetWaterSourcePos(object inst) => (BlockPos)PI_WaterSourcePos.GetValue(inst);
        private static void SetWaterSourcePos(object inst, BlockPos v) => PI_WaterSourcePos.SetValue(inst, v);
        private static bool GetHasWaterSource(object inst) => (bool)PI_HasWaterSource.GetValue(inst);
        private static void SetHasWaterSource(object inst, bool v) => PI_HasWaterSource.SetValue(inst, v);

        [HarmonyPrepare]
        static bool Prepare() => TAqueductEntity != null;

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => AccessTools.Method(TAqueductEntity, "onServerTick1s", new[] { typeof(float) });

        [HarmonyPrefix]
        static bool Prefix(
            object __instance,
            float dt,
            ref int ___WaterSourceReacquireTimeout,
            object ___blockAqueduct
        )
        {
            var be = (BlockEntity)__instance;
            ICoreAPI api = be.Api;
            if (api == null) return false;

            var ba = api.World.BlockAccessor;
            var ourBlock = ba.GetBlock(be.Pos);
            if (!IsAqueductBlock(ourBlock)) return false;

            string o = GetOrientationForSelfOrBlock(___blockAqueduct, ourBlock);
            var face = string.IsNullOrEmpty(o) ? BlockFacing.NORTH : BlockFacing.FromFirstLetter(o[0]);

            BlockPos inlineA, inlineB, sideA, sideB;
            if (face == BlockFacing.NORTH)
            {
                inlineA = be.Pos.NorthCopy();
                inlineB = be.Pos.SouthCopy();
                sideA   = be.Pos.EastCopy();
                sideB   = be.Pos.WestCopy();
            }
            else
            {
                inlineA = be.Pos.WestCopy();
                inlineB = be.Pos.EastCopy();
                sideA   = be.Pos.NorthCopy();
                sideB   = be.Pos.SouthCopy();
            }

            IEnumerable<BlockPos> AcquireCandidates()
            {
                yield return inlineA; yield return inlineB; yield return sideA; yield return sideB;
            }

            if (GetHasWaterSource(__instance))
            {
                bool hasSource = false;
                BlockPos srcPos = GetWaterSourcePos(__instance);
                bool unloadedSource = (srcPos != null) && (ba.GetChunkAtBlockPos(srcPos) == null);

                if (IsValidWaterSource(api, be.Pos, 7) || unloadedSource) hasSource = true;
                else if (srcPos != null && (IsValidWaterSource(api, srcPos) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, srcPos) || unloadedSource)) hasSource = true;
                else if (srcPos != null && (IsValidWaterFall(api, srcPos) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, srcPos) || unloadedSource)) hasSource = true;
                else if (srcPos != null && (IsValidWaterSource(api, srcPos, 5) && IsValidWaterSourceOrWaterFall(api, srcPos.UpCopy(), 5) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, srcPos) || unloadedSource)) hasSource = true;
                else if (srcPos != null && (IsValidWaterSource(api, be.Pos, 6) && IsValidWaterSourceOrWaterFall(api, srcPos, 6) && (be.Pos.Y == srcPos.Y - 1) || unloadedSource)) hasSource = true;
                else if (srcPos != null && (IsValidFilledAqueduct(api, __instance, srcPos, 6) || unloadedSource)) hasSource = true;

                if (!hasSource || HasInvalidSourceDependency(api, __instance))
                {
                    ___WaterSourceReacquireTimeout = 4;
                    SetHasWaterSource(__instance, false);
                    SetWaterSourcePos(__instance, null);
                    be.MarkDirty(true);
                }
                return false;
            }

            if (___WaterSourceReacquireTimeout > 0)
            {
                ___WaterSourceReacquireTimeout--;
                SetWaterLevel(__instance, Math.Max(0, GetWaterLevel(__instance) - 1));
                ba.TriggerNeighbourBlockUpdate(be.Pos);
                be.MarkDirty(true);
                return false;
            }

            bool acquired = false;
            BlockPos up = be.Pos.UpCopy();

            if (IsValidWaterSource(api, be.Pos, 7))
            {
                SetWaterSourcePos(__instance, be.Pos); SetWaterLevel(__instance, 7);
                SetHasWaterSource(__instance, true); acquired = true;
            }
            else if (IsValidWaterSource(api, up) || IsValidWaterSourceOrWaterFall(api, up, 6) || IsValidFilledAqueduct(api, __instance, up, 6))
            {
                SetWaterSourcePos(__instance, up); SetWaterLevel(__instance, 6);
                SetHasWaterSource(__instance, true); acquired = true;
            }

            if (!acquired)
            {
                foreach (var endPos in AcquireCandidates())
                {
                    if (IsValidWaterSource(api, endPos) ||
                        (IsValidWaterFall(api, endPos) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, endPos)) ||
                        (IsValidWaterSource(api, endPos, 5) && IsValidWaterSourceOrWaterFall(api, endPos.UpCopy(), 5) && DoesBlockBelowPosHaveUpSolidFaceOrAqueduct(api, endPos)) ||
                        IsValidFilledAqueduct(api, __instance, endPos, 6))
                    {
                        SetWaterSourcePos(__instance, endPos); SetWaterLevel(__instance, 6);
                        SetHasWaterSource(__instance, true); acquired = true;
                        break;
                    }
                }
            }

            if (acquired)
            {
                var ourFluid = ba.GetBlock(be.Pos, BlockLayersAccess.Fluid);
                bool notIced = ourFluid == null || !ourFluid.Code.Path.Contains("ice");
                if (notIced && (ourFluid == null || ourFluid.LiquidLevel < GetWaterLevel(__instance))
                    && !HasInvalidSourceDependency(api, __instance))
                {
                    PlaceFluidForState(api, be, __instance, GetWaterLevel(__instance));
                    var currentFluid = ba.GetBlock(be.Pos, BlockLayersAccess.Fluid);
                    TrySpillFromEnd(api, inlineA, currentFluid, GetWaterLevel(__instance));
                    TrySpillFromEnd(api, inlineB, currentFluid, GetWaterLevel(__instance));
                }
            }
            else
            {
                SetWaterLevel(__instance, Math.Max(0, GetWaterLevel(__instance) - 1));
                ba.TriggerNeighbourBlockUpdate(be.Pos);
                be.MarkDirty(true);
            }

            return false;
        }

        private static bool IsAqueductBlock(Block b)
            => b != null && (
                   (TIAqueduct != null && TIAqueduct.IsAssignableFrom(b.GetType()))
                || (TBlockAqueduct != null && b.GetType() == TBlockAqueduct)
                || b.GetType().Name.Contains("Aqueduct", StringComparison.OrdinalIgnoreCase)
            );

        private static string GetOrientationForSelfOrBlock(object injectedIAqueduct, Block block)
        {
            if (injectedIAqueduct != null)
            {
                var pi = AccessTools.Property(injectedIAqueduct.GetType(), "Orientation");
                var v = pi?.GetValue(injectedIAqueduct);
                if (v != null) return v.ToString();
            }
            var p = AccessTools.Property(block.GetType(), "Orientation");
            var val = p?.GetValue(block);
            return val?.ToString();
        }

        private static bool GetAqueductIsEnclosed(Block b)
        {
            if (b == null) return false;
            var pi = AccessTools.Property(b.GetType(), "IsEnclosed");
            var v = pi?.GetValue(b);
            return v is bool bv && bv;
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

        private static bool IsValidWaterSource(ICoreAPI api, BlockPos pos, int minLevel = 7)
        {
            var block = api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block == null) return false;

            if (IsHydrateWell(block))
                return ParseWellwaterLevel(block) >= minLevel;

            return block.IsLiquid() && block.LiquidLevel >= minLevel;
        }

        private static bool IsValidWaterFall(ICoreAPI api, BlockPos pos)
        {
            var block = api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block == null) return false;

            if (IsHydrateWell(block))
                return (ParseWellwaterFlow(block) == "d" && ParseWellwaterLevel(block) >= 6);

            return block.IsLiquid() && block.LiquidLevel >= 6 && (block.Variant?["flow"] == "d");
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

            if (neighborBE.GetType() != TAqueductEntity) return false;

            var selfPos = ((BlockEntity)self).Pos;
            var ourBlock = ba.GetBlock(selfPos);

            string ourO = GetOrientationForSelfOrBlock(null, ourBlock);
            string theirO = GetOrientationForSelfOrBlock(null, neighborBlock);
            bool theirEnclosed = GetAqueductIsEnclosed(neighborBlock);

            var adjWsp = GetWaterSourcePos(neighborBE);
            bool neighborHasWater = GetHasWaterSource(neighborBE);
            int neighborLevel = GetWaterLevel(neighborBE);

            if (adjWsp != null && adjWsp.Equals(selfPos)) return false;

            int neighborFluidLevel = ba.GetBlock(pos, BlockLayersAccess.Fluid)?.LiquidLevel ?? 0;
            bool neighborFluidLooksFilled = neighborFluidLevel >= Math.Max(5, minLevel - 1);
            bool neighborLooksFilled = neighborHasWater || neighborLevel >= minLevel || neighborFluidLooksFilled;

            var selfWsp = GetWaterSourcePos(self);
            bool mutualDependency = adjWsp != null && selfWsp != null &&
                                    adjWsp.Equals(selfPos) && selfWsp.Equals(((BlockEntity)neighborBE).Pos);

            bool orientationOk = (theirO == ourO) || !theirEnclosed || (IsPerpendicular(ourO, theirO) && neighborLooksFilled);

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
            var selfPos = ((BlockEntity)self).Pos;
            var selfWsp = GetWaterSourcePos(self);

            var neighborsUsingMe = AllHorizNeighbors(selfPos)
                .Select(bp => ba.GetBlockEntity(bp))
                .Where(be => be != null && be.GetType() == TAqueductEntity)
                .Where(be => Equals(GetWaterSourcePos(be), selfPos))
                .ToList();

            if (neighborsUsingMe.Count < 2) return false;
            return neighborsUsingMe.Any(be => Equals(selfWsp, ((BlockEntity)be).Pos));
        }

        private static void PlaceFluidForState(ICoreAPI api, BlockEntity be, object aqueductInst, int waterLevel)
        {
            var ba = api.World.BlockAccessor;
            var pos = be.Pos;

            AssetLocation targetLoc = null;

            BlockPos srcPos = GetWaterSourcePos(aqueductInst);
            Block src = (srcPos != null) ? ba.GetBlock(srcPos, BlockLayersAccess.Fluid) : null;

            Block ourFluid = ba.GetBlock(pos, BlockLayersAccess.Fluid);
            int wl = GameMath.Clamp(waterLevel, 1, 7);

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
                    be.MarkDirty(true);
                }
            }
        }
    }
}
