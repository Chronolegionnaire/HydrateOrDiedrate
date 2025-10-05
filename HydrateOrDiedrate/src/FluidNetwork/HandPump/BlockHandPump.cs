using HydrateOrDiedrate.FluidNetwork;
using HydrateOrDiedrate.FluidNetwork.HandPump;
using HydrateOrDiedrate.Pipes.Pipe;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.FluidNetwork.HandPump
{
    public class BlockHandPump : Block, FluidInterfaces.IFluidBlock
    {
        // ----- IFluidBlock -----
        public void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            world.BlockAccessor.GetBlockEntity(pos)?.MarkDirty();
        }

        public bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            // Optional: gate by shape/variant; default: all faces open.
            return true;
        }

        public FluidNetwork GetNetwork(IWorldAccessor world, BlockPos pos)
        {
            var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityHandPump;
            return be?.Fluid?.Network;
        }

        // ----- Placement / connect -----
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, pos, byItemStack);

            var beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorPipe>();
            if (beh != null)
            {
                foreach (var f in BlockFacing.ALLFACES) beh.TryConnect(f);
            }
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
            var beh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorPipe>();
            if (beh != null)
            {
                foreach (var f in BlockFacing.ALLFACES) beh.TryConnect(f);
            }
        }

        // ----- Interactions -----

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return false;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHandPump be)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 0: // container slot UX (like winch)
                    {
                        var sourceSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                        if (sourceSlot == null) break;

                        // flip with BE slot if types match-ish
                        if ((sourceSlot.Empty != be.ContainerSlot.Empty) && sourceSlot.TryFlipWith(be.ContainerSlot)) return true;

                        // try transfer liquid (winch helper reused)
                        if (BlockHandPumpHelpers.TryTransferLiquidInto(sourceSlot.Itemstack, be.ContainerSlot.Itemstack))
                        {
                            sourceSlot.MarkDirty();
                            be.ContainerSlot.MarkDirty();
                            world.PlaySoundAt(new AssetLocation("game", "sounds/effect/water-fill.ogg"),
                                blockSel.Position.X + 0.5, blockSel.Position.Y + 0.5, blockSel.Position.Z + 0.5);
                            return true;
                        }

                        // or try from BE to player’s container
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

                    case 1: // start pumping
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
                // keep demanding + filling
                return be.ContinuePumping(0.1f);
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
