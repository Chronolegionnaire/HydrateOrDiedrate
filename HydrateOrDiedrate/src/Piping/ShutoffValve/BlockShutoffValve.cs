using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Piping.ShutoffValve
{
    public class BlockShutoffValve : Block, IFluidBlock, IFluidGate
    {
        static readonly (EValveAxis axis, BlockFacing a, BlockFacing b)[] ScanOrder = new[]
        {
            (EValveAxis.EW, BlockFacing.EAST,  BlockFacing.WEST),
            (EValveAxis.NS, BlockFacing.NORTH, BlockFacing.SOUTH),
            (EValveAxis.UD, BlockFacing.UP,    BlockFacing.DOWN)
        };

        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is not BlockEntityShutoffValve be) return false;

            return be.Axis switch
            {
                EValveAxis.EW => face == BlockFacing.EAST  || face == BlockFacing.WEST,
                EValveAxis.NS => face == BlockFacing.NORTH || face == BlockFacing.SOUTH,
                _             => face == BlockFacing.UP    || face == BlockFacing.DOWN,
            };
        }

        public bool AllowsFluidPassage(IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            if (be == null) return false;
            if (to != from.Opposite) return false;

            bool alongAxis = be.Axis switch
            {
                EValveAxis.EW => to == BlockFacing.EAST  || to == BlockFacing.WEST,
                EValveAxis.NS => to == BlockFacing.NORTH || to == BlockFacing.SOUTH,
                EValveAxis.UD => to == BlockFacing.UP    || to == BlockFacing.DOWN,
                _             => false
            };
            if (!alongAxis) return false;

            return be.Enabled;
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
                be.Axis = ChooseAxisOnPlacement(world, pos);
                be.AxisInitialized = true;

                var desiredFacing = IsConnectorFace(be.Axis, blockSel.Face)
                    ? HorizontalFacingTowardPlayer(pos, byPlayer)
                    : blockSel.Face.Opposite;

                be.RollSteps = RollFor(be.Axis, desiredFacing);

                be.MarkDirty(true);
            }
            FluidNetworkState.InvalidateNetwork();
            return true;
        }

        static bool IsConnectorFace(EValveAxis axis, BlockFacing face)
        {
            return axis switch
            {
                EValveAxis.EW => face == BlockFacing.EAST  || face == BlockFacing.WEST,
                EValveAxis.NS => face == BlockFacing.NORTH || face == BlockFacing.SOUTH,
                _             => face == BlockFacing.UP    || face == BlockFacing.DOWN
            };
        }

        static readonly Dictionary<EValveAxis, BlockFacing[]> RotOrder = new()
        {
            [EValveAxis.UD] = new[] { BlockFacing.EAST,  BlockFacing.SOUTH, BlockFacing.WEST,  BlockFacing.NORTH },
            [EValveAxis.NS] = new[] { BlockFacing.EAST,  BlockFacing.UP,    BlockFacing.WEST,  BlockFacing.DOWN  },
            [EValveAxis.EW] = new[] { BlockFacing.UP,    BlockFacing.SOUTH, BlockFacing.DOWN,  BlockFacing.NORTH }
        };

        static readonly int[] AxisRollOffsets = new int[3];

        static BlockShutoffValve()
        {
            AxisRollOffsets[(int)EValveAxis.EW] = -1;
            AxisRollOffsets[(int)EValveAxis.NS] = +1;
            AxisRollOffsets[(int)EValveAxis.UD] = -1;
        }

        static int RollFor(EValveAxis axis, BlockFacing desiredFacing)
        {
            var calibrated = CalibrateForAxis(axis, desiredFacing);
            var order      = RotOrder[axis];

            int idx = Array.IndexOf(order, calibrated);
            if (idx < 0) idx = 0;

            int off = AxisRollOffsets[(int)axis] & 3;
            return (idx + off) & 3;
        }

        static BlockFacing CalibrateForAxis(EValveAxis axis, BlockFacing facing)
        {
            if (axis != EValveAxis.UD) return facing.Opposite;
            if (facing == BlockFacing.NORTH || facing == BlockFacing.SOUTH) return facing.Opposite;
            return facing;
        }

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
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityShutoffValve;
            if (be == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            var held = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (IsWrench(held))
            {
                if (world.Side == EnumAppSide.Server)
                {
                    bool sprint = byPlayer?.Entity?.Controls?.Sprint == true;
                    if (sprint)
                    {
                        be.RollSteps = (be.RollSteps + 1) & 3;
                    }
                    else
                    {
                        be.Axis = NextAxis(be.Axis);
                        be.AxisInitialized = true;
                        be.RollSteps &= 3;
                    }
                    be.MarkDirty(true);
                    FluidNetworkState.InvalidateNetwork();
                }
                return true;
            }

            if (world.Side == EnumAppSide.Server)
            {
                be.ServerToggleEnabled(byPlayer);
            }
            return true;
        }

        const float px = 1f / 16f;
        static readonly Cuboidf Center = new Cuboidf(6*px,  6*px,  6*px, 10*px, 10*px, 10*px);
        static readonly Cuboidf ArmN   = new Cuboidf(6*px,  6*px,  0*px, 10*px, 10*px,  6*px);
        static readonly Cuboidf ArmS   = new Cuboidf(6*px,  6*px, 10*px, 10*px, 10*px, 16*px);
        static readonly Cuboidf ArmE   = new Cuboidf(10*px, 6*px,  6*px, 16*px, 10*px, 10*px);
        static readonly Cuboidf ArmW   = new Cuboidf(0*px,  6*px,  6*px,  6*px, 10*px, 10*px);
        static readonly Cuboidf ArmU   = new Cuboidf(6*px, 10*px,  6*px, 10*px, 16*px, 10*px);
        static readonly Cuboidf ArmD   = new Cuboidf(6*px,  0*px,  6*px, 10*px,  6*px, 10*px);

        static Cuboidf[] BoxesFor(EValveAxis axis)
        {
            return axis switch
            {
                EValveAxis.EW => new[] { Center, ArmE, ArmW },
                EValveAxis.NS => new[] { Center, ArmN, ArmS },
                _             => new[] { Center, ArmU, ArmD }
            };
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            return BoxesFor(be?.Axis ?? EValveAxis.UD);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            return BoxesFor(be?.Axis ?? EValveAxis.UD);
        }

        EValveAxis ChooseAxisOnPlacement(IWorldAccessor world, BlockPos pos)
        {
            foreach (var (axis, a, b) in ScanOrder)
            {
                if (HasNeighborConnector(world, pos, a) || HasNeighborConnector(world, pos, b))
                    return axis;
            }
            return EValveAxis.UD;
        }

        bool HasNeighborConnector(IWorldAccessor world, BlockPos pos, BlockFacing towards)
        {
            var npos = pos.AddCopy(towards);
            var nb   = world.BlockAccessor.GetBlock(npos);

            if (nb is BlockWellSpring) return true;
            if (world.BlockAccessor.GetBlockEntity(npos) is BlockEntityWellSpring) return true;

            if (nb is IFluidBlock nFluid)
            {
                return nFluid.HasFluidConnectorAt(world, npos, towards.Opposite);
            }

            return false;
        }

        static bool IsWrench(ItemSlot slot)
        {
            var code = slot?.Itemstack?.Collectible?.Code?.ToString();
            return code != null && WildcardUtil.Match("game:wrench-*", code);
        }

        static EValveAxis NextAxis(EValveAxis current)
        {
            return current switch
            {
                EValveAxis.UD => EValveAxis.EW,
                EValveAxis.EW => EValveAxis.NS,
                _             => EValveAxis.UD
            };
        }
    }
}
