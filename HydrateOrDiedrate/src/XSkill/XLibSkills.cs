using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Hot_Weather;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace HydrateOrDiedrate.XSkill;

public static class XLibSkills
{
    public static bool Enabled { get; internal set; }

    internal static void Initialize(ICoreAPI api)
    {
        XLeveling xleveling = api.ModLoader.GetModSystem<XLeveling>();
        if (xleveling is null) return;

        Skill skill = xleveling.GetSkill("survival", false);
        if (skill is null) return;

        if (ModConfig.Instance.Thirst.Enabled)
        {
            var dromedaryAbility = new StatAbility("dromedary", "HoD:maxHydration", "hydrateordiedrate:ability-dromedary", "hydrateordiedrate:abilitydesc-dromedary", 1, 3, [30, 60, 90], false);
            skill.AddAbility(dromedaryAbility);
        }

        //TODO this should be behind equivelant IF statement no?
        var equatidianAbility = new StatAbility("equatidian", HoDStats.CoolingMul, "hydrateordiedrate:ability-equatidian", "hydrateordiedrate:abilitydesc-equatidian", 1, 3, [25, 50, 100], false);
        
        skill.AddAbility(equatidianAbility);
    }
}
