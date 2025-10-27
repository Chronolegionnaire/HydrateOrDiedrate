// BlockShutoffValve.cs
using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Piping.ShutoffValve
{
    public enum ValveAxis { EW, NS, UD }

    public class BlockShutoffValve : Block, IFluidBlock, IFluidGate
    {
        // Order matters: we’ll scan in this order and pick the first connector we find.
        static readonly (ValveAxis axis, BlockFacing a, BlockFacing b)[] ScanOrder = new[]
        {
            (ValveAxis.EW, BlockFacing.EAST,  BlockFacing.WEST),
            (ValveAxis.NS, BlockFacing.NORTH, BlockFacing.SOUTH),
            (ValveAxis.UD, BlockFacing.UP,    BlockFacing.DOWN)
        };
        static readonly BlockFacing[] Faces =
        {
            BlockFacing.NORTH, BlockFacing.EAST, BlockFacing.SOUTH,
            BlockFacing.WEST,  BlockFacing.UP,   BlockFacing.DOWN
        };

        void MarkSelfAndNeighborsDirty(IWorldAccessor world, BlockPos pos)
        {
            var ba = world.BlockAccessor;
            ba.MarkBlockDirty(pos);
            foreach (var f in Faces)
            {
                ba.MarkBlockDirty(pos.AddCopy(f));
            }
        }

        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            if (be == null) return false;
            switch (be.Axis)
            {
                case ValveAxis.EW: return face == BlockFacing.EAST || face == BlockFacing.WEST;
                case ValveAxis.NS: return face == BlockFacing.NORTH || face == BlockFacing.SOUTH;
                default:           return face == BlockFacing.UP   || face == BlockFacing.DOWN;
            }
        }

        public bool AllowsFluidPassage(IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            if (be == null) return false;
            return be.Enabled; // open when enabled, closed when disabled
        }

        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.MarkBlockDirty(pos);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack,
            BlockSelection blockSel, ref string failureCode)
        {
            if (!base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode)) return false;

            var pos = blockSel.Position;

            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityShutoffValve be)
            {
                // 1) pick the pipe axis from neighbors
                be.Axis = ChooseAxisOnPlacement(world, pos);
                be.AxisInitialized = true;

                // 2) choose desired facing based on rule
                var desiredFacing = IsConnectorFace(be.Axis, blockSel.Face)
                    ? HorizontalFacingTowardPlayer(pos, byPlayer) // toward player when clicking a connector side
                    : blockSel.Face.Opposite; // otherwise opposite of the face placed against

                // 3) compute the roll (calibration + offset folded inside)
                be.RollSteps = RollFor(be.Axis, desiredFacing);

                be.MarkDirty(true);
            }

            MarkSelfAndNeighborsDirty(world, pos);
            return true;
        }

        // --- Axis/facing helpers -----------------------------------------------------

        static bool IsConnectorFace(ValveAxis axis, BlockFacing face)
        {
            return axis switch
            {
                ValveAxis.EW => face == BlockFacing.EAST || face == BlockFacing.WEST,
                ValveAxis.NS => face == BlockFacing.NORTH || face == BlockFacing.SOUTH,
                _            => face == BlockFacing.UP || face == BlockFacing.DOWN
            };
        }

        // Rotation orders per-axis AFTER calibration, so offsets can be applied uniformly.
        static readonly Dictionary<ValveAxis, BlockFacing[]> RotOrder = new()
        {
            // UD rolls around Y: E -> S -> W -> N
            [ValveAxis.UD] = new[] { BlockFacing.EAST, BlockFacing.SOUTH, BlockFacing.WEST, BlockFacing.NORTH },

            // NS rolls around Z: E -> Up -> W -> Down
            [ValveAxis.NS] = new[] { BlockFacing.EAST, BlockFacing.UP, BlockFacing.WEST, BlockFacing.DOWN },

            // EW rolls around X: Up -> S -> Down -> N
            [ValveAxis.EW] = new[] { BlockFacing.UP, BlockFacing.SOUTH, BlockFacing.DOWN, BlockFacing.NORTH }
        };

        // Axis-specific roll offsets
        static readonly int[] AxisRollOffsets = new int[3];

        static BlockShutoffValve()
        {
            AxisRollOffsets[(int)ValveAxis.EW] = -1; // modulo 4
            AxisRollOffsets[(int)ValveAxis.NS] = +1;
            AxisRollOffsets[(int)ValveAxis.UD] = -1;
        }

        // Single entry: calibration + indexing + offset
        static int RollFor(ValveAxis axis, BlockFacing desiredFacing)
        {
            var calibrated = CalibrateForAxis(axis, desiredFacing);
            var order = RotOrder[axis];

            int idx = Array.IndexOf(order, calibrated);
            if (idx < 0) idx = 0;

            int off = AxisRollOffsets[(int)axis] & 3;
            return (idx + off) & 3;
        }

        // Same calibration as earlier logic
        static BlockFacing CalibrateForAxis(ValveAxis axis, BlockFacing facing)
        {
            if (axis != ValveAxis.UD) return facing.Opposite;  // flip for EW + NS
            if (facing == BlockFacing.NORTH || facing == BlockFacing.SOUTH) return facing.Opposite; // UD: flip N/S
            return facing; // keep E/W
        }

        // Non-nullable; pick the dominant horizontal toward the player.
        static BlockFacing HorizontalFacingTowardPlayer(BlockPos pos, IPlayer byPlayer)
        {
            var agent = byPlayer.Entity as EntityAgent;
            var p = (agent != null ? agent.Pos.XYZ : byPlayer.Entity.Pos.XYZ);
            var c = pos.ToVec3d().Add(0.5, 0.5, 0.5);

            double dx = p.X - c.X;
            double dz = p.Z - c.Z;

            return Math.Abs(dx) > Math.Abs(dz)
                ? (dx >= 0 ? BlockFacing.EAST : BlockFacing.WEST)
                : (dz >= 0 ? BlockFacing.SOUTH : BlockFacing.NORTH);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);

            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            if (be != null && !be.AxisInitialized)
            {
                be.Axis = ChooseAxisOnPlacement(world, pos);
                be.AxisInitialized = true;
                be.MarkDirty(true);
            }

            // Make sure neighbors recompute their pipe boxes
            MarkSelfAndNeighborsDirty(world, pos);
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            // Recompute us + neighbors (e.g., a pipe got placed/removed next to the valve)
            MarkSelfAndNeighborsDirty(world, pos);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityShutoffValve;
            if (be == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            var held = byPlayer?.InventoryManager?.ActiveHotbarSlot;

            // Wrench: rotate axis or roll (server authoritative)
            if (IsWrench(held))
            {
                if (world.Side == EnumAppSide.Server)
                {
                    bool sprint = byPlayer?.Entity?.Controls?.Sprint == true;
                    if (sprint) { be.RollSteps = (be.RollSteps + 1) & 3; }
                    else
                    {
                        be.Axis = NextAxis(be.Axis);
                        be.AxisInitialized = true;
                        be.RollSteps &= 3;
                    }
                    be.MarkDirty(true);
                    MarkSelfAndNeighborsDirty(world, blockSel.Position);
                }
                return true;
            }

            // Toggle on server; packet drives anim+SFX on clients
            if (world.Side == EnumAppSide.Server)
            {
                be.ServerToggleEnabled(byPlayer);
                MarkSelfAndNeighborsDirty(world, blockSel.Position);
            }
            return true;
        }

        // ---- Selection / collision boxes: fixed straight 2-way boxes matching axis ----

        const float px = 1f / 16f;
        static readonly Cuboidf Center = new Cuboidf(6*px,  6*px,  6*px, 10*px, 10*px, 10*px);
        static readonly Cuboidf ArmN   = new Cuboidf(6*px,  6*px,  0*px, 10*px, 10*px,  6*px);
        static readonly Cuboidf ArmS   = new Cuboidf(6*px,  6*px, 10*px, 10*px, 10*px, 16*px);
        static readonly Cuboidf ArmE   = new Cuboidf(10*px, 6*px,  6*px, 16*px, 10*px, 10*px);
        static readonly Cuboidf ArmW   = new Cuboidf(0*px,  6*px,  6*px,  6*px, 10*px, 10*px);
        static readonly Cuboidf ArmU   = new Cuboidf(6*px, 10*px,  6*px, 10*px, 16*px, 10*px);
        static readonly Cuboidf ArmD   = new Cuboidf(6*px,  0*px,  6*px, 10*px,  6*px, 10*px);

        static Cuboidf[] BoxesFor(ValveAxis axis)
        {
            return axis switch
            {
                ValveAxis.EW => new[] { Center, ArmE, ArmW },
                ValveAxis.NS => new[] { Center, ArmN, ArmS },
                _            => new[] { Center, ArmU, ArmD }
            };
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            return BoxesFor(be?.Axis ?? ValveAxis.UD);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            return BoxesFor(be?.Axis ?? ValveAxis.UD);
        }

        // ---- Helpers ----

        ValveAxis ChooseAxisOnPlacement(IWorldAccessor world, BlockPos pos)
        {
            foreach (var (axis, a, b) in ScanOrder)
            {
                if (HasNeighborConnector(world, pos, a) || HasNeighborConnector(world, pos, b))
                    return axis;
            }
            return ValveAxis.UD; // fallback
        }

        bool HasNeighborConnector(IWorldAccessor world, BlockPos pos, BlockFacing towards)
        {
            var npos = pos.AddCopy(towards);
            var nb   = world.BlockAccessor.GetBlock(npos);
            // Treat wells as connectable (mirroring your pipe logic)
            if (nb is BlockWellSpring) return true;
            if (world.BlockAccessor.GetBlockEntity(npos) is BlockEntityWellSpring) return true;

            // Fluid block that accepts a connector facing us
            if (nb is IFluidBlock nFluid)
                return nFluid.HasFluidConnectorAt(world, npos, towards.Opposite);

            return false;
        }

        static bool IsWrench(ItemSlot slot)
        {
            var code = slot?.Itemstack?.Collectible?.Code?.ToString();
            return code != null && WildcardUtil.Match("game:wrench-*", code);
        }

        static ValveAxis NextAxis(ValveAxis current)
        {
            return current switch
            {
                ValveAxis.UD => ValveAxis.EW,
                ValveAxis.EW => ValveAxis.NS,
                _            => ValveAxis.UD
            };
        }
    }
}
