using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg;

public class ItemKegTap : ItemChisel
{
    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => null;

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (byEntity is not EntityPlayer) return;
        handling = EnumHandHandling.PreventDefaultAction;

        if (api is ICoreClientAPI clientApi && !byEntity.HasToolInOffHand(EnumTool.Hammer))
        {
            clientApi.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
        }
    }
    
    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        if (byEntity is not EntityPlayer) return;
        handling = EnumHandHandling.PreventDefaultAction;

        if (api is ICoreClientAPI clientApi && !byEntity.HasToolInOffHand(EnumTool.Hammer))
        {
            clientApi.TriggerIngameError(this, "nohammer", Lang.Get("Requires a hammer in the off hand"));
        }
    }

    public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity) => forEntity.HasToolInOffHand(EnumTool.Hammer) ? "use" : null;

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity) => byEntity.HasToolInOffHand(EnumTool.Hammer) ? "hit" : null;
}
