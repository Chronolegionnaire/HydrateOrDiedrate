using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Tun
{
    public class BlockTun : BlockLiquidContainerBase
    {
        private float tunCapacityLitres;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            tunCapacityLitres = Attributes?["tunCapacityLitres"].AsFloat(1100.0f) ?? 1100.0f;
        }

        public override float CapacityLitres => tunCapacityLitres;
        public override bool CanDrinkFrom => false;

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            return;
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (itemslot.Itemstack?.Block is BlockTun)
            {
                handHandling = EnumHandHandling.PreventDefaultAction;
                return;
            }

            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "drink", int eatSoundRepeats = 1)
        {
            if (slot.Itemstack?.Block is BlockTun)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }

            base.tryEatBegin(slot, byEntity, ref handling, eatSound, eatSoundRepeats);
        }

        protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
        {
            if (slot.Itemstack?.Block is BlockTun)
            {
                return false;
            }

            return base.tryEatStep(secondsUsed, slot, byEntity, spawnParticleStack);
        }

        protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (slot.Itemstack?.Block is BlockTun)
            {
                return;
            }

            base.tryEatStop(secondsUsed, slot, byEntity);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            BlockFacing facing = SuggestedHVOrientation(byPlayer, blockSel)[0];
            BlockPos originPos = blockSel.Position;

            world.Logger.Debug($"Attempting to place multiblock at {originPos}");

            // Place the block at the origin position
            if (!base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack))
            {
                world.Logger.Debug("Base placement failed.");
                return false; // If the base placement logic fails, return false
            }

            // Set up the block entity at the origin
            if (world.BlockAccessor.GetBlockEntity(originPos) is BlockEntityTun blockEntityTun)
            {
                blockEntityTun.MeshAngle = facing.HorizontalAngleIndex * 90f;
                blockEntityTun.MarkDirty(true, null);
                world.Logger.Debug($"Multiblock placed successfully at {originPos} with rotation {blockEntityTun.MeshAngle}");
            }
            else
            {
                world.Logger.Debug("Block entity setup failed.");
            }

            return true;
        }

        private Vec3i RotateOffset(Vec3i offset, BlockFacing facing)
        {
            switch (facing.HorizontalAngleIndex)
            {
                case 1: // 90 degrees
                    return new Vec3i(-offset.Z, offset.Y, offset.X);
                case 2: // 180 degrees
                    return new Vec3i(-offset.X, offset.Y, -offset.Z);
                case 3: // 270 degrees
                    return new Vec3i(offset.Z, offset.Y, -offset.X);
                default: // 0 degrees
                    return offset;
            }
        }

    }
}
