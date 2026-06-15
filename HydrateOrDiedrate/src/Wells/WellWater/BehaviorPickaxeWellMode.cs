using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using XSkills;

namespace HydrateOrDiedrate.Wells.WellWater;

public class BehaviorPickaxeWellMode(CollectibleObject collObj) : BehaviorShovelWellMode(collObj)
{
    protected override SkillItem[] GenerateToolModes(ICoreAPI api)
    {
        var capi = api as ICoreClientAPI;
        return ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "HoD:DigWellToolModesPickaxe", () =>
        {
            var wellSpringMode = new SkillItem
            {
                Code = new AssetLocation("hydrateordiedrate", "digwellspring"),
                Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digwellspring")
            }.WithIcon(capi, capi?.Gui.LoadSvgWithPadding(new AssetLocation("hydrateordiedrate:textures/icons/well.svg"), 48, 48, 5, ColorUtil.WhiteArgb));

            if(api.ModLoader.Mods.Any(mod => mod.Info.ModID.StartsWith("xskill")))
            {
                SkillItem[] result = [..CreateXSkillsToolModes(api), wellSpringMode];

                api.ObjectCache["pickaxeToolModes"] = result;

                return result;
            }

            return [
                new SkillItem
                {
                    Code = new AssetLocation("hydrateordiedrate", "digmode"),
                    Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digmode")
                }.WithIcon(capi, capi?.Gui.LoadSvgWithPadding(new AssetLocation("game:textures/icons/rocks.svg"), 48, 48, 5, ColorUtil.WhiteArgb)),
                wellSpringMode
            ];
        });
    }

    private static SkillItem[] CreateXSkillsToolModes(ICoreAPI api)
    {
        try
        {
            return PickaxeBehavior.CreateToolModes(api);
        }
        catch(Exception ex)
        {
            api.Logger.Error(ex);
            return [];
        }
    }

    public override bool CanMakeWellSpring(Block block)
    {
        var path = block?.Code?.Path;
        if(string.IsNullOrEmpty(path)) return false;

        return path.StartsWith("rock") || path.StartsWith("crackedrock");
    }
}
