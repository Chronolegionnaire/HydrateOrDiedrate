using HydrateOrDiedrate.Piping.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class BlockHandPump : Block, IFluidBlock
    {
        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face) =>
            face == BlockFacing.DOWN;

        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.GetBlockEntity(pos)?.MarkDirty();
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);
            world.BlockAccessor.MarkBlockDirty(pos);
            world.BlockAccessor.MarkBlockDirty(pos.DownCopy());
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityHandPump be) be.InvalidateCachedWell();
            world.BlockAccessor.MarkBlockDirty(pos);
            world.BlockAccessor.MarkBlockDirty(pos.DownCopy());
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return false;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHandPump be)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 0:
                    {
                        var sourceSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                        if (sourceSlot == null) break;
                        if ((sourceSlot.Empty != be.ContainerSlot.Empty) && sourceSlot.TryFlipWith(be.ContainerSlot)) return true;
                        if (BlockHandPumpHelpers.TryTransferLiquidInto(sourceSlot.Itemstack, be.ContainerSlot.Itemstack))
                        {
                            sourceSlot.MarkDirty();
                            be.ContainerSlot.MarkDirty();
                            world.PlaySoundAt(new AssetLocation("game", "sounds/effect/water-fill.ogg"),
                                blockSel.Position.X + 0.5, blockSel.Position.Y + 0.5, blockSel.Position.Z + 0.5);
                            return true;
                        }
                        if (BlockHandPumpHelpers.TryTransferLiquidInto(be.ContainerSlot.Itemstack, sourceSlot.Itemstack))
                        {
                            sourceSlot.MarkDirty();
                            be.ContainerSlot.MarkDirty();
                            world.PlaySoundAt(new AssetLocation("game", "sounds/effect/water-fill.ogg"),
                                blockSel.Position.X + 0.5, blockSel.Position.Y + 0.5, blockSel.Position.Z + 0.5);
                            return true;
                        }

                        break;
                    }

                    case 1:
                        if (be.TryStartPumping(byPlayer)) return true;
                        break;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHandPump be && be.PumpingPlayer == byPlayer)
            {
                float dt = secondsUsed - be.lastSecondsUsed;
                be.lastSecondsUsed = secondsUsed;
                return be.ContinuePumping(dt);
            }
            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHandPump be && be.PumpingPlayer == byPlayer)
            {
                be.StopPumping();
                return;
            }
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHandPump be && be.PumpingPlayer == byPlayer)
            {
                be.StopPumping();
                return true;
            }
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }
    }
}
