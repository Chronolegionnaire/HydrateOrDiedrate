using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg
{
    public class BlockTun : BlockLiquidContainerBase
    {
        private float tunCapacityLitres;
        private long updateListenerId;

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
            tunCapacityLitres = HydrateOrDiedrateModSystem.LoadedConfig.TunCapacityLitres;
        }

        private void RegisterConfigUpdateListener(ICoreAPI api)
        {
            updateListenerId = api.Event.RegisterGameTickListener(dt =>
            {
                float newTunCapacity = HydrateOrDiedrateModSystem.LoadedConfig.TunCapacityLitres;
                if (newTunCapacity != tunCapacityLitres)
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
    }
}
