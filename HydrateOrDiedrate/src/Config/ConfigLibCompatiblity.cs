using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace HydrateOrDiedrate.Config
{
    public class ConfigLibCompatibility
    {
        private const string settingEnableThirstMechanics = "hydrateordiedrate:Config.Setting.EnableThirstMechanics";
        private const string settingHarshHeat = "hydrateordiedrate:Config.Setting.HarshHeat";
        private const string settingEnableBoilingWaterDamage = "hydrateordiedrate:Config.Setting.EnableBoilingWaterDamage";
        private const string settingEnableLiquidEncumbrance = "hydrateordiedrate:Config.Setting.EnableLiquidEncumbrance";
        private const string settingMaxThirst = "hydrateordiedrate:Config.Setting.MaxThirst";
        private const string settingThirstDamage = "hydrateordiedrate:Config.Setting.ThirstDamage";
        private const string settingThirstDecayRate = "hydrateordiedrate:Config.Setting.ThirstDecayRate";
        private const string settingThirstIncreasePerDegreeMultiplier = "hydrateordiedrate:Config.Setting.ThirstIncreasePerDegreeMultiplier";
        private const string settingThirstDecayRateMax = "hydrateordiedrate:Config.Setting.ThirstDecayRateMax";
        private const string settingHydrationLossDelayMultiplier = "hydrateordiedrate:Config.Setting.HydrationLossDelayMultiplier";
        private const string settingMaxMovementSpeedPenalty = "hydrateordiedrate:Config.Setting.MaxMovementSpeedPenalty";
        private const string settingMovementSpeedPenaltyThreshold = "hydrateordiedrate:Config.Setting.MovementSpeedPenaltyThreshold";
        private const string settingTemperatureThreshold = "hydrateordiedrate:Config.Setting.TemperatureThreshold";
        private const string settingHarshHeatExponentialGainMultiplier = "hydrateordiedrate:Config.Setting.HarshHeatExponentialGainMultiplier";
        private const string settingBoilingWaterDamage = "hydrateordiedrate:Config.Setting.BoilingWaterDamage";
        private const string settingUnequippedSlotCooling = "hydrateordiedrate:Config.Setting.UnequippedSlotCooling";
        private const string settingWetnessCoolingFactor = "hydrateordiedrate:Config.Setting.WetnessCoolingFactor";
        private const string settingShelterCoolingFactor = "hydrateordiedrate:Config.Setting.ShelterCoolingFactor";
        private const string settingSunlightCoolingFactor = "hydrateordiedrate:Config.Setting.SunlightCoolingFactor";
        private const string settingDiurnalVariationAmplitude = "hydrateordiedrate:Config.Setting.DiurnalVariationAmplitude";
        private const string settingRefrigerationCooling = "hydrateordiedrate:Config.Setting.RefrigerationCooling";
        private const string settingSprintThirstMultiplier = "hydrateordiedrate:Config.Setting.SprintThirstMultiplier";
        private const string settingEncumbranceLimit = "hydrateordiedrate:Config.Setting.EncumbranceLimit";
        private const string settingLiquidEncumbranceMovementSpeedDebuff = "hydrateordiedrate:Config.Setting.LiquidEncumbranceMovementSpeedDebuff";

        private const string settingDromedaryMultiplierPerLevel = "hydrateordiedrate:Config.Setting.DromedaryMultiplierPerLevel";
        private const string settingEquatidianCoolingMultipliers = "hydrateordiedrate:Config.Setting.EquatidianCoolingMultipliers";
        
        private const string settingEnableRainGathering = "hydrateordiedrate:Config.Setting.EnableRainGathering";
        private const string settingRainMultiplier = "hydrateordiedrate:Config.Setting.RainMultiplier";

        public ConfigLibCompatibility(ICoreClientAPI api)
        {
            if (!api.ModLoader.IsModEnabled("configlib"))
            {
                return;
            }

            Init(api);
        }

        private void Init(ICoreClientAPI api)
        {
            if (!api.ModLoader.IsModEnabled("configlib"))
            {
                return;
            }

            api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig("hydrateordiedrate", (id, buttons) => EditConfig(id, buttons, api));
        }

        private void EditConfig(string id, ControlButtons buttons, ICoreClientAPI api)
        {
            if (buttons.Save) ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", HydrateOrDiedrateModSystem.LoadedConfig);
            if (buttons.Defaults) HydrateOrDiedrateModSystem.LoadedConfig = new Config();
            Edit(api, HydrateOrDiedrateModSystem.LoadedConfig, id);
        }

        private void Edit(ICoreClientAPI api, Config config, string id)
        {
            ImGui.TextWrapped("HydrateOrDiedrate Settings");

            bool enableThirstMechanics = config.EnableThirstMechanics;
            ImGui.Checkbox(Lang.Get(settingEnableThirstMechanics) + $"##enableThirstMechanics-{id}", ref enableThirstMechanics);
            config.EnableThirstMechanics = enableThirstMechanics;

            bool harshHeat = config.HarshHeat;
            ImGui.Checkbox(Lang.Get(settingHarshHeat) + $"##harshHeat-{id}", ref harshHeat);
            config.HarshHeat = harshHeat;

            bool enableBoilingWaterDamage = config.EnableBoilingWaterDamage;
            ImGui.Checkbox(Lang.Get(settingEnableBoilingWaterDamage) + $"##enableBoilingWaterDamage-{id}", ref enableBoilingWaterDamage);
            config.EnableBoilingWaterDamage = enableBoilingWaterDamage;

            bool enableLiquidEncumbrance = config.EnableLiquidEncumbrance;
            ImGui.Checkbox(Lang.Get(settingEnableLiquidEncumbrance) + $"##enableLiquidEncumbrance-{id}", ref enableLiquidEncumbrance);
            config.EnableLiquidEncumbrance = enableLiquidEncumbrance;

            ImGui.SeparatorText("Thirst Settings");

            float maxThirst = config.MaxThirst;
            ImGui.DragFloat(Lang.Get(settingMaxThirst) + $"##maxThirst-{id}", ref maxThirst, 100.0f, 100.0f, 10000.0f);
            config.MaxThirst = maxThirst;

            float thirstDamage = config.ThirstDamage;
            ImGui.DragFloat(Lang.Get(settingThirstDamage) + $"##thirstDamage-{id}", ref thirstDamage, 0.1f, 0.0f, 20.0f);
            config.ThirstDamage = thirstDamage;

            float thirstDecayRate = config.ThirstDecayRate;
            ImGui.DragFloat(Lang.Get(settingThirstDecayRate) + $"##thirstDecayRate-{id}", ref thirstDecayRate, 1.0f, 0.0f, 100.0f);
            config.ThirstDecayRate = thirstDecayRate;

            float thirstIncreasePerDegreeMultiplier = config.ThirstIncreasePerDegreeMultiplier;
            ImGui.DragFloat(Lang.Get(settingThirstIncreasePerDegreeMultiplier) + $"##thirstIncreasePerDegreeMultiplier-{id}", ref thirstIncreasePerDegreeMultiplier, 0.1f, 0.0f, 10.0f);
            config.ThirstIncreasePerDegreeMultiplier = thirstIncreasePerDegreeMultiplier;

            float thirstDecayRateMax = config.ThirstDecayRateMax;
            ImGui.DragFloat(Lang.Get(settingThirstDecayRateMax) + $"##thirstDecayRateMax-{id}", ref thirstDecayRateMax, 0.1f, 0.0f, 50.0f);
            config.ThirstDecayRateMax = thirstDecayRateMax;

            float hydrationLossDelayMultiplier = config.HydrationLossDelayMultiplier;
            ImGui.DragFloat(Lang.Get(settingHydrationLossDelayMultiplier) + $"##hydrationLossDelayMultiplier-{id}", ref hydrationLossDelayMultiplier, 0.01f, 0.0f, 1.0f);
            config.HydrationLossDelayMultiplier = hydrationLossDelayMultiplier;

            float maxMovementSpeedPenalty = config.MaxMovementSpeedPenalty;
            ImGui.DragFloat(Lang.Get(settingMaxMovementSpeedPenalty) + $"##maxMovementSpeedPenalty-{id}", ref maxMovementSpeedPenalty, 0.01f, 0.0f, 1.0f);
            config.MaxMovementSpeedPenalty = maxMovementSpeedPenalty;

            float movementSpeedPenaltyThreshold = config.MovementSpeedPenaltyThreshold;
            ImGui.DragFloat(Lang.Get(settingMovementSpeedPenaltyThreshold) + $"##movementSpeedPenaltyThreshold-{id}", ref movementSpeedPenaltyThreshold, 10.0f, 0.0f, 1500.0f);
            config.MovementSpeedPenaltyThreshold = movementSpeedPenaltyThreshold;

            float temperatureThreshold = config.TemperatureThreshold;
            ImGui.DragFloat(Lang.Get(settingTemperatureThreshold) + $"##temperatureThreshold-{id}", ref temperatureThreshold, 1.0f, 0.0f, 50.0f);
            config.TemperatureThreshold = temperatureThreshold;

            float harshHeatExponentialGainMultiplier = config.HarshHeatExponentialGainMultiplier;
            ImGui.DragFloat(Lang.Get(settingHarshHeatExponentialGainMultiplier) + $"##harshHeatExponentialGainMultiplier-{id}", ref harshHeatExponentialGainMultiplier, 0.01f, 0.0f, 1.0f);
            config.HarshHeatExponentialGainMultiplier = harshHeatExponentialGainMultiplier;

            float boilingWaterDamage = config.BoilingWaterDamage;
            ImGui.DragFloat(Lang.Get(settingBoilingWaterDamage) + $"##boilingWaterDamage-{id}", ref boilingWaterDamage, 0.1f, 0.0f, 25.0f);
            config.BoilingWaterDamage = boilingWaterDamage;

            float unequippedSlotCooling = config.UnequippedSlotCooling;
            ImGui.DragFloat(Lang.Get(settingUnequippedSlotCooling) + $"##unequippedSlotCooling-{id}", ref unequippedSlotCooling, 0.1f, 0.0f, 5.0f);
            config.UnequippedSlotCooling = unequippedSlotCooling;

            float wetnessCoolingFactor = config.WetnessCoolingFactor;
            ImGui.DragFloat(Lang.Get(settingWetnessCoolingFactor) + $"##wetnessCoolingFactor-{id}", ref wetnessCoolingFactor, 0.1f, 0.0f, 5.0f);
            config.WetnessCoolingFactor = wetnessCoolingFactor;

            float shelterCoolingFactor = config.ShelterCoolingFactor;
            ImGui.DragFloat(Lang.Get(settingShelterCoolingFactor) + $"##shelterCoolingFactor-{id}", ref shelterCoolingFactor, 0.1f, 0.0f, 5.0f);
            config.ShelterCoolingFactor = shelterCoolingFactor;

            float sunlightCoolingFactor = config.SunlightCoolingFactor;
            ImGui.DragFloat(Lang.Get(settingSunlightCoolingFactor) + $"##sunlightCoolingFactor-{id}", ref sunlightCoolingFactor, 0.1f, 0.0f, 5.0f);
            config.SunlightCoolingFactor = sunlightCoolingFactor;

            float diurnalVariationAmplitude = config.DiurnalVariationAmplitude;
            ImGui.DragFloat(Lang.Get(settingDiurnalVariationAmplitude) + $"##diurnalVariationAmplitude-{id}", ref diurnalVariationAmplitude, 1.0f, 0.0f, 50.0f);
            config.DiurnalVariationAmplitude = diurnalVariationAmplitude;
            
            float refrigerationCooling = config.RefrigerationCooling;
            ImGui.DragFloat(Lang.Get(settingRefrigerationCooling) + $"##refrigerationCooling-{id}", ref refrigerationCooling, 0.1f, 0.0f, 5.0f);
            config.RefrigerationCooling = refrigerationCooling;

            float sprintThirstMultiplier = config.SprintThirstMultiplier;
            ImGui.DragFloat(Lang.Get(settingSprintThirstMultiplier) + $"##sprintThirstMultiplier-{id}", ref sprintThirstMultiplier, 0.1f, 0.0f, 5.0f);
            config.SprintThirstMultiplier = sprintThirstMultiplier;

            float encumbranceLimit = config.EncumbranceLimit;
            ImGui.DragFloat(Lang.Get(settingEncumbranceLimit) + $"##encumbranceLimit-{id}", ref encumbranceLimit, 0.1f, 0.0f, 10.0f);
            config.EncumbranceLimit = encumbranceLimit;

            float liquidEncumbranceMovementSpeedDebuff = config.LiquidEncumbranceMovementSpeedDebuff;
            ImGui.DragFloat(Lang.Get(settingLiquidEncumbranceMovementSpeedDebuff) + $"##liquidEncumbranceMovementSpeedDebuff-{id}", ref liquidEncumbranceMovementSpeedDebuff, 0.01f, 0.0f, 1.0f);
            config.LiquidEncumbranceMovementSpeedDebuff = liquidEncumbranceMovementSpeedDebuff;

            ImGui.SeparatorText("XLib Skills Settings");

            float dromedaryMultiplierPerLevel = config.DromedaryMultiplierPerLevel;
            ImGui.DragFloat(Lang.Get(settingDromedaryMultiplierPerLevel) + $"##dromedaryMultiplierPerLevel-{id}", ref dromedaryMultiplierPerLevel, 0.01f, 0.1f, 3.0f);
            config.DromedaryMultiplierPerLevel = dromedaryMultiplierPerLevel;

            ImGui.TextWrapped(Lang.Get(settingEquatidianCoolingMultipliers) + $"##equatidianCoolingMultipliers-{id}");
            float equatidianLevel1 = config.EquatidianCoolingMultipliers[0];
            ImGui.DragFloat($"Level 1##equatidianLevel1-{id}", ref equatidianLevel1, 0.01f, 1.0f, 5.0f);
            config.EquatidianCoolingMultipliers[0] = equatidianLevel1;

            float equatidianLevel2 = config.EquatidianCoolingMultipliers[1];
            ImGui.DragFloat($"Level 2##equatidianLevel2-{id}", ref equatidianLevel2, 0.01f, 1.0f, 5.0f);
            config.EquatidianCoolingMultipliers[1] = equatidianLevel2;

            float equatidianLevel3 = config.EquatidianCoolingMultipliers[2];
            ImGui.DragFloat($"Level 3##equatidianLevel3-{id}", ref equatidianLevel3, 0.01f, 1.0f, 5.0f);
            config.EquatidianCoolingMultipliers[2] = equatidianLevel3;
            
            ImGui.SeparatorText("Rain Gathering Settings");
            
            bool enableRainGathering = config.EnableRainGathering;
            ImGui.Checkbox(Lang.Get(settingEnableRainGathering) + $"##enableRainGathering-{id}", ref enableRainGathering);
            config.EnableRainGathering = enableRainGathering;

            float rainMultiplier = config.RainMultiplier;
            ImGui.DragFloat(Lang.Get(settingRainMultiplier) + $"##rainMultiplier-{id}", ref rainMultiplier, 0.1f, 0.1f, 10.0f);
            config.RainMultiplier = rainMultiplier;
        }
    }
}
