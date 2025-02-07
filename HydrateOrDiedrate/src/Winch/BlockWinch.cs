using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch
{
    public class BlockWinch : BlockMPBase
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public string Direction
        {
            get { return this.LastCodePart(0); }
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if (flag && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch blockEntityWinch)
            {
                float playerYaw = byPlayer.Entity.Pos.Yaw;
                float snapAngle = 0.3926991f;
                float snappedYaw = (float)(Math.Round(playerYaw / snapAngle) * snapAngle);
                blockEntityWinch.MeshAngle = snappedYaw;
                blockEntityWinch.MarkDirty(true, null);
            }

            return flag;
        }
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool flag = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (flag && !this.tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
            {
                this.tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
            }
            return flag;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            BlockEntityWinch beWinch = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWinch;
            if (beWinch != null && beWinch.CanTurn() 
                && (blockSel.SelectionBoxIndex == 1 
                    || beWinch.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
            {
                beWinch.SetPlayerTurning(byPlayer, true);
                return true;
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityWinch beWinch = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWinch;
            if (beWinch != null 
                && (blockSel.SelectionBoxIndex == 1 || beWinch.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
            {
                beWinch.SetPlayerTurning(byPlayer, true);
                return beWinch.CanTurn();
            }
            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityWinch beWinch = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWinch;
            if (beWinch != null)
            {
                beWinch.SetPlayerTurning(byPlayer, false);
            }
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            BlockEntityWinch beWinch = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityWinch;
            if (beWinch != null)
            {
                beWinch.SetPlayerTurning(byPlayer, false);
            }
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (selection.SelectionBoxIndex == 0)
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-winch-addremoveitems",
                        MouseButton = EnumMouseButton.Right
                    }
                }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            }
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-winch-turn",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) =>
                    {
                        BlockEntityWinch beWinch = world.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityWinch;
                        return beWinch != null && beWinch.CanTurn();
                    }
                }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
        }

        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            switch (Direction)
            {
                case "north": return face == BlockFacing.WEST;
                case "south": return face == BlockFacing.EAST;
                case "east":  return face == BlockFacing.SOUTH;
                case "west":  return face == BlockFacing.NORTH;
                default:      return false;
            }
        }

        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);

            if (facing == BlockFacing.UP)
            {
                if (entity.World.Side == EnumAppSide.Server)
                {
                    float frameTime = GlobalConstants.PhysicsFrameTime;
                    BEBehaviorMPConsumer mpc = this.GetBEBehavior<BEBehaviorMPConsumer>(pos);
                    if (mpc != null)
                    {
                        entity.SidedPos.Yaw += frameTime * mpc.TrueSpeed * 2.5f * (mpc.isRotationReversed() ? -1 : 1);
                    }
                }
                else
                {
                    float frameTime2 = GlobalConstants.PhysicsFrameTime;
                    BEBehaviorMPConsumer mpc2 = this.GetBEBehavior<BEBehaviorMPConsumer>(pos);
                    ICoreClientAPI capi = this.api as ICoreClientAPI;
                    if (capi.World.Player.Entity.EntityId == entity.EntityId && mpc2 != null)
                    {
                        int sign = (mpc2.isRotationReversed() ? -1 : 1);
                        if (capi.World.Player.CameraMode != EnumCameraMode.Overhead)
                        {
                            capi.Input.MouseYaw += frameTime2 * mpc2.TrueSpeed * 2.5f * sign;
                        }
                        capi.World.Player.Entity.BodyYaw += frameTime2 * mpc2.TrueSpeed * 2.5f * sign;
                        capi.World.Player.Entity.WalkYaw += frameTime2 * mpc2.TrueSpeed * 2.5f * sign;
                        capi.World.Player.Entity.Pos.Yaw  += frameTime2 * mpc2.TrueSpeed * 2.5f * sign;
                    }
                }
            }
        }
    }
}
