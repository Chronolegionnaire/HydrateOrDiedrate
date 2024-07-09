﻿using System;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace HydrateOrDiedrate.Compatibility
{
    public class XLibSkills
    {
        private ICoreAPI _api;

        public void Initialize(ICoreAPI api)
        {
            if (!api.ModLoader.IsModEnabled("xlib"))
            {
                return;
            }

            _api = api;
            XLeveling xleveling = api.ModLoader.GetModSystem<XLeveling>();
            if (xleveling == null)
            {
                return;
            }

            Skill skill = xleveling.GetSkill("survival", false);
            if (skill == null)
            {
                return;
            }

            if (HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics)
            {
                int[] dromedaryValues = new int[] { 0 }; // Example values array, replace with appropriate values
                Ability dromedaryAbility = new Ability("dromedary", "hydrateordiedrate:ability-dromedary",
                    "hydrateordiedrate:abilitydesc-dromedary", 1, 3, dromedaryValues, false);
                dromedaryAbility.OnPlayerAbilityTierChanged += OnDromedary;
                skill.AddAbility(dromedaryAbility);
            }

            int[] equatidianValues = new int[] { 0 }; // Example values array, replace with appropriate values
            Ability equatidianAbility = new Ability("equatidian", "hydrateordiedrate:ability-equatidian",
                "hydrateordiedrate:abilitydesc-equatidian", 1, 3, equatidianValues, false);
            equatidianAbility.OnPlayerAbilityTierChanged += OnEquatidian;
            skill.AddAbility(equatidianAbility);
        }

        private void OnDromedary(PlayerAbility playerAbility, int oldTier)
        {
            if (!HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics)
            {
                playerAbility.SetTier(0);
                return;
            }

            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null)
            {
                return;
            }

            EntityPlayer entity = player.Entity;
            if (entity == null)
            {
                return;
            }

            EntityBehaviorThirst behavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (behavior == null)
            {
                return;
            }
            if (playerAbility.Tier < 1)
            {
                return;
            }
            float baseMultiplier = HydrateOrDiedrateModSystem.LoadedConfig.DromedaryMultiplierPerLevel;
            float multiplier = 1 + baseMultiplier + (baseMultiplier * (playerAbility.Tier - 1));
            float newMaxThirst = HydrateOrDiedrateModSystem.LoadedConfig.MaxThirst * multiplier;
            behavior.CurrentThirst = behavior.CurrentThirst / behavior.MaxThirst * newMaxThirst;
            behavior.MaxThirst = newMaxThirst;
            behavior.UpdateThirstAttributes();
        }

        private void OnEquatidian(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null)
            {
                return;
            }

            EntityPlayer entity = player.Entity;
            if (entity == null)
            {
                return;
            }
            EntityBehaviorBodyTemperatureHot behavior = entity.GetBehavior<EntityBehaviorBodyTemperatureHot>();
            if (behavior == null)
            {
                return;
            }
            if (playerAbility.Tier < 1)
            {
                return;
            }
            if (playerAbility.Tier > HydrateOrDiedrateModSystem.LoadedConfig.EquatidianCoolingMultipliers.Length)
            {
                return;
            }

            float coolingMultiplier =
                HydrateOrDiedrateModSystem.LoadedConfig.EquatidianCoolingMultipliers[playerAbility.Tier - 1];
            behavior.CoolingMultiplier = coolingMultiplier;
            behavior.UpdateCoolingFactor();
        }
    }
}
