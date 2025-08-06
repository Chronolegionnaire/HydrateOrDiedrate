using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Hot_Weather;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace HydrateOrDiedrate.XSkill;

public static class XLibSkills
{
    internal static void Initialize(ICoreAPI api)
    {
        XLeveling xleveling = api.ModLoader.GetModSystem<XLeveling>();
        if (xleveling is null) return;

        Skill skill = xleveling.GetSkill("survival", false);
        if (skill is null) return;

        if (ModConfig.Instance.Thirst.Enabled)
        {
            int[] dromedaryValues = [0];
            Ability dromedaryAbility = new Ability("dromedary", "hydrateordiedrate:ability-dromedary", "hydrateordiedrate:abilitydesc-dromedary", 1, 3, dromedaryValues, false);
            dromedaryAbility.OnPlayerAbilityTierChanged += OnDromedary;
            skill.AddAbility(dromedaryAbility);
        }

        //TODO these values should come from Xlevel config not ours
        //TODO these values should be filled so we can use them in the ability description
        int[] equatidianValues = [0];
        Ability equatidianAbility = new Ability("equatidian", "hydrateordiedrate:ability-equatidian", "hydrateordiedrate:abilitydesc-equatidian", 1, 3, equatidianValues, false);
        equatidianAbility.OnPlayerAbilityTierChanged += OnEquatidian;
        skill.AddAbility(equatidianAbility);
    }

    private static void OnDromedary(PlayerAbility playerAbility, int oldTier)
    {
        var entity = playerAbility.PlayerSkill.PlayerSkillSet.Player?.Entity;
        if(entity is null) return;
        
        var behavior = entity.GetBehavior<EntityBehaviorThirst>();
        if (behavior == null) return;

        if (playerAbility.Tier < 1)
        {
            float defaultMaxThirst = ModConfig.Instance.Thirst.MaxThirst;
            behavior.CurrentThirst = behavior.CurrentThirst / behavior.MaxThirst * defaultMaxThirst;
            behavior.MaxThirst = defaultMaxThirst;
            entity.WatchedAttributes.SetBool("dromedaryActive", false);
            return;
        }
        
        entity.WatchedAttributes.SetBool("dromedaryActive", true); // TODO

        float baseMultiplier = ModConfig.Instance.XLib.DromedaryMultiplierPerLevel;
        float multiplier = 1 + baseMultiplier + (baseMultiplier * (playerAbility.Tier - 1));
        float newMaxThirst = ModConfig.Instance.Thirst.MaxThirst * multiplier;
        behavior.CurrentThirst = behavior.CurrentThirst / behavior.MaxThirst * newMaxThirst;
        behavior.MaxThirst = newMaxThirst;
    }

    private static void OnEquatidian(PlayerAbility playerAbility, int oldTier)
    {
        EntityBehaviorBodyTemperatureHot behavior = playerAbility.PlayerSkill.PlayerSkillSet.Player?.Entity?.GetBehavior<EntityBehaviorBodyTemperatureHot>();
        if (behavior is null) return;

        var config = ModConfig.Instance.XLib.EquatidianCoolingMultipliers;
        
        if (playerAbility.Tier < 1)
        {
            behavior.EquatidianAbilityCoolingMultiplier = 1f;
        }
        else if (playerAbility.Tier > config.Length)
        {
            behavior.EquatidianAbilityCoolingMultiplier = config.Length > 0 ? config[^1] : 1f;
        }
        else
        {
            behavior.EquatidianAbilityCoolingMultiplier = config[playerAbility.Tier - 1];
        }
    }
}
