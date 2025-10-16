using HydrateOrDiedrate.Piping.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

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

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityHandPump bePump)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 0:
                        var sourceSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                        if (sourceSlot is not null)
                        {
                            if ((sourceSlot.Empty != bePump.ContainerSlot.Empty) && sourceSlot.TryFlipWith(bePump.ContainerSlot)) return true;

                            if (TryTransferLiquid(bePump.ContainerSlot.Itemstack, sourceSlot.Itemstack))
                            {
                                sourceSlot.MarkDirty();
                                bePump.ContainerSlot.MarkDirty();
                                world.PlaySoundAt(
                                    new AssetLocation("game", "sounds/effect/water-fill.ogg"),
                                    blockSel.Position.X + 0.5,
                                    blockSel.Position.Y + 0.5,
                                    blockSel.Position.Z + 0.5
                                );
                                return true;
                            }
                        }
                        break;
                    

                    case 1:
                        if (bePump.TryStartPumping(byPlayer)) return true;
                        break;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
        
        public static bool TryTransferLiquid(ItemStack source, ItemStack target)
        {
            if(source?.Collectible is not BlockLiquidContainerBase sourceContainer || target?.Collectible is not BlockLiquidContainerBase targetContainer) return false;
            var existingLiters = targetContainer.GetCurrentLitres(target);
            var remainingSpace = targetContainer.CapacityLitres - existingLiters;
            if(remainingSpace <= 0) return false;
            remainingSpace *= target.StackSize;

            var existingContent = targetContainer.GetContent(target);
            var newContent = sourceContainer.GetContent(source);
            if(newContent is null || (existingContent is not null && existingContent.Collectible.Code != newContent.Collectible.Code)) return false;

            var addedLiquid = sourceContainer.TryTakeLiquid(source, GameMath.Min(remainingSpace, sourceContainer.GetCurrentLitres(source)));
            if(addedLiquid is null) return false;
            addedLiquid.StackSize /= target.StackSize;
            if(existingContent is not null) addedLiquid.StackSize += existingContent.StackSize;

            targetContainer.SetContent(target, addedLiquid);
            return true;
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
