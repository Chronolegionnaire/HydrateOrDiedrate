using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Wells.WellWater;

public class BehaviorShovelWellMode(CollectibleObject collObj) : CollectibleBehavior(collObj)
{
    private SkillItem[] toolModes = [];

    protected int WellSpringToolMode = -1;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        toolModes = GenerateToolModes(api);
        WellSpringToolMode = toolModes.IndexOf(item => item.Code is { Domain: "hydrateordiedrate", Path: "digwellspring" });
    }

    protected virtual SkillItem[] GenerateToolModes(ICoreAPI api)
    {
        var capi = api as ICoreClientAPI;
        return ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "HoD:DigWellToolModes", () => [
            new SkillItem
            {
                Code = new AssetLocation("hydrateordiedrate", "digmode"),
                Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digmode")
            }.WithIcon(capi, capi?.Gui.LoadSvgWithPadding(new AssetLocation("game:textures/icons/rocks.svg"), 48, 48, 5, ColorUtil.WhiteArgb)),
            new SkillItem
            {
                Code = new AssetLocation("hydrateordiedrate", "digwellspring"),
                Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digwellspring")
            }.WithIcon(capi, capi?.Gui.LoadSvgWithPadding(new AssetLocation("hydrateordiedrate:textures/icons/well.svg"), 48, 48, 5, ColorUtil.WhiteArgb))
        ]);
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => toolModes;

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection) => slot.Itemstack.Attributes.GetInt("toolMode");

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        slot.MarkDirty();
    }
    
    public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier, ref EnumHandling bhHandling)
    {
        if (blockSel is null || byEntity is null) return false;

        if (itemslot.Itemstack.Attributes.GetInt("toolMode") == WellSpringToolMode)
        {
            blockSel.Block ??= world.BlockAccessor.GetBlock(blockSel.Position);
            if (CanMakeWellSpring(blockSel.Block))
            {
                bhHandling = EnumHandling.PreventDefault;
                SpawnWellSpring(world, blockSel);
                return true;
            }
        }

        return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
    }

    protected static void SpawnWellSpring(IWorldAccessor world, BlockSelection blockSel)
    {
        Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate", "wellspring"));

        world.Api.Event.EnqueueMainThreadTask(() =>
        {
            world.BlockAccessor.SetBlock(wellSpringBlock.Id, blockSel.Position);
            if(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityWellSpring wellSpring)
            {
                wellSpring.OriginBlock = blockSel.Block;
            }
        }, "HoD:spawn-wellspring");
    }

    public virtual bool CanMakeWellSpring(Block block)
    {
        var path = block?.Code?.Path;
        if(string.IsNullOrEmpty(path)) return false;

        return path.StartsWith("soil-") || path.StartsWith("gravel-") || path.StartsWith("sand-");
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        if(ObjectCacheUtil.Delete(api, "HoD:DigWellToolModes"))
        {
            foreach (SkillItem mode in toolModes)
            {
                mode.Dispose();
            }
        }
        base.OnUnloaded(api);
    }
}
