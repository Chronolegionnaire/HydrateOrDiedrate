using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch;

//TODO: realistically speaking wouldn't it start going up again if you keep turning the handle? rope would then coil the other way around. Would also make the mechanical connection part easier
public class BlockWinch : BlockMPBase
{
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

        if (flag && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch blockEntityWinch)
        {
            float playerYaw = byPlayer.Entity.Pos.Yaw;
            float snappedYaw = (float)(Math.Round(playerYaw / Constants.SnapAngle) * Constants.SnapAngle);
            blockEntityWinch.MeshAngle = snappedYaw;
            blockEntityWinch.MarkDirty(true, null);
        }

        return flag;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        bool flag = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        if (flag && !tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
        {
            tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
        }
        return flag;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return false;

        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch && beWinch.CanTurn() && (blockSel.SelectionBoxIndex == 1))
        {
            beWinch.SetPlayerTurning(byPlayer, true);
            return true;
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch && (blockSel.SelectionBoxIndex == 1 || beWinch.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
        {
            beWinch.SetPlayerTurning(byPlayer, true);
            return beWinch.CanTurn();
        }
        return false;
    }

    public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch)
        {
            beWinch.SetPlayerTurning(byPlayer, false);
        }
    }

    public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch)
        {
            beWinch.SetPlayerTurning(byPlayer, false);
        }
        return true;
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        if (selection.SelectionBoxIndex == 0)
        {
            return [
                new WorldInteraction
                {
                    ActionLangCode = "hydrateordiedrate:blockhelp-winch-addremoveitems",
                    MouseButton = EnumMouseButton.Right
                },
                ..base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
            ];
        }

        return [
            new WorldInteraction
            {
                ActionLangCode = "hydrateordiedrate:blockhelp-winch-lower",
                MouseButton = EnumMouseButton.Right,
                ShouldApply = (wi, bs, es) => world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWinch beWinch && beWinch.CanTurn()
            },
            new WorldInteraction
            {
                ActionLangCode = "hydrateordiedrate:blockhelp-winch-raise",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "sneak",
                ShouldApply = (wi, bs, es) => world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWinch beWinch && !beWinch.InputSlot.Empty
            },
            ..base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
        ];
    }


    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
        //Empty (required by base class)
    }

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) => Variant["side"] switch
    {
        "north" => face == BlockFacing.WEST,
        "south" => face == BlockFacing.EAST,
        "east" => face == BlockFacing.SOUTH,
        "west" => face == BlockFacing.NORTH,
        _ => false,
    };

    public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
    {
        base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);

        if (facing != BlockFacing.UP) return;

        BEBehaviorMPConsumer mpc = GetBEBehavior<BEBehaviorMPConsumer>(pos);
        if(mpc is null) return;

        if (api is ICoreClientAPI capi)
        {
            var playerEntity = capi.World.Player.Entity;
            if (playerEntity.EntityId == entity.EntityId)
            {
                var modification = GetYawModification(mpc);

                if (capi.World.Player.CameraMode != EnumCameraMode.Overhead) capi.Input.MouseYaw += modification;
                playerEntity.BodyYaw += modification;
                playerEntity.WalkYaw += modification;
                playerEntity.Pos.Yaw += modification;
            }
        }
        else
        {
            entity.SidedPos.Yaw += GetYawModification(mpc);
        }
    }

    private static float GetYawModification(BEBehaviorMPConsumer mpc) => GlobalConstants.PhysicsFrameTime * mpc.TrueSpeed * 2.5f * (mpc.isRotationReversed() ? -1 : 1);
}
