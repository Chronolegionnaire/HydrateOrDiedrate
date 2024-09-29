using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg
{
    public class ItemKegTap : ItemChisel
    {
        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return null;
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!IsHammerInOffHand(byEntity))
            {
                ICoreClientAPI coreClientAPI = this.api as ICoreClientAPI;
                coreClientAPI?.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (byPlayer == null) return;
            if (!IsHammerInOffHand(byEntity))
            {
                ICoreClientAPI coreClientAPI = this.api as ICoreClientAPI;
                coreClientAPI?.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }
        private bool IsHammerInOffHand(Entity entity)
        {
            if (entity is EntityAgent entityAgent)
            {
                ItemSlot offhandSlot = entityAgent.LeftHandItemSlot;
                if (offhandSlot != null && offhandSlot.Itemstack?.Collectible?.Tool == EnumTool.Hammer)
                {
                    return true;
                }
            }
            return false;
        }
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            if (IsHammerInOffHand(forEntity))
            {
                return "use";
            }

            return null;
        }
        public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            if (IsHammerInOffHand(byEntity))
            {
                return "hit";
            }

            return null;
        }
    }
}
