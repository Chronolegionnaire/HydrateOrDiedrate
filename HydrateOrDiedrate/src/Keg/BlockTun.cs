using HydrateOrDiedrate.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg
{
    public class BlockTun : BlockLiquidContainerBase
    {
        private float tunCapacityLitres;
        private long updateListenerId;
        private bool tunDropWithLiquid;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            LoadConfigValues();

            if (api.Side == EnumAppSide.Client)
            {
                RegisterConfigUpdateListener(api);
            }
        }

        private void LoadConfigValues()
        {
            tunCapacityLitres = ModConfig.Instance.Containers.TunCapacityLitres;
            tunDropWithLiquid = ModConfig.Instance.Containers.TunDropWithLiquid;
        }

        private void RegisterConfigUpdateListener(ICoreAPI api)
        {
            updateListenerId = api.Event.RegisterGameTickListener(dt =>
            {
                float newTunCapacity = ModConfig.Instance.Containers.TunCapacityLitres;
                bool newTunDropWithLiquid = ModConfig.Instance.Containers.TunDropWithLiquid;
                if (newTunCapacity != tunCapacityLitres || newTunDropWithLiquid != tunDropWithLiquid)
                {
                    LoadConfigValues();
                    api.Event.UnregisterGameTickListener(updateListenerId);
                }

            }, 5000);
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
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            var blockEntityTun = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityTun;
            bool containsRot = blockEntityTun?.GetContent()?.Collectible?.Code?.ToString() == "game:rot";

            if (tunDropWithLiquid && !containsRot)
            {
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                return;
            }

            bool preventDefault = false;
            foreach (var behavior in this.BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault)
                    preventDefault = true;
                if (handled == EnumHandling.PreventSubsequent)
                    return;
            }

            if (preventDefault)
                return;

            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack dropStack = new ItemStack(this);

                if (blockEntityTun != null && !containsRot)
                {
                    var contentStack = blockEntityTun.GetContent();
                    if (contentStack != null)
                    {
                        dropStack.Attributes.SetItemstack("tunContent", contentStack);
                    }
                }

                world.SpawnItemEntity(dropStack, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                world.PlaySoundAt(this.Sounds.GetBreakSound(byPlayer), pos, 0.0, byPlayer, true, 32f, 1f);
            }

            var entity = world.BlockAccessor.GetBlockEntity(pos);
            entity?.OnBlockBroken(null);
            world.BlockAccessor.SetBlock(0, pos);
        }
    }
}
