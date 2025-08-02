using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch;

public class BlockWinch : BlockMPBase
{
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

        if (blockSel.SelectionBoxIndex == 1 && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch && beWinch.TryStartTurning(byPlayer)) return true;

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch && beWinch.RotationPlayer == byPlayer)
        {
            //TODO: maybe pass real delta time here (by tracking last turn time in blockEntity)
            return beWinch.ContinueTurning(0.1f);
        }

        return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
    }

    public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch && byPlayer == beWinch.RotationPlayer)
        {
            beWinch.StopTurning();
            return;
        }

        base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
    }

    public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWinch beWinch && byPlayer == beWinch.RotationPlayer)
        {
            beWinch.StopTurning();
            return true;
        }

        return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) => selection.SelectionBoxIndex switch
    {
        0 => [
            new WorldInteraction
            {
                ActionLangCode = "hydrateordiedrate:blockhelp-winch-addremoveitems",
                MouseButton = EnumMouseButton.Right
            },
            ..base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
        ],
        _ => [
            new WorldInteraction
            {
                ActionLangCode = "hydrateordiedrate:blockhelp-winch-lower",
                MouseButton = EnumMouseButton.Right,
                ShouldApply = (wi, bs, es) => world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWinch beWinch && !beWinch.InputSlot.Empty
            },
            new WorldInteraction
            {
                ActionLangCode = "hydrateordiedrate:blockhelp-winch-raise",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "sneak",
                ShouldApply = (wi, bs, es) => world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWinch beWinch && !beWinch.InputSlot.Empty
            },
            ..base.GetPlacedBlockInteractionHelp(world, selection, forPlayer)
        ]
    };


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
}
