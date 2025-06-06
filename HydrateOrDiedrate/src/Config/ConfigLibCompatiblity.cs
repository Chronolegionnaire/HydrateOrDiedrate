﻿using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HydrateOrDiedrate.Config
{
    public class ConfigLibCompatibility
    {
        // Thirst Settings
        private const string settingEnableThirstMechanics = "hydrateordiedrate:Config.Setting.EnableThirstMechanics";
        private const string settingMaxThirst = "hydrateordiedrate:Config.Setting.MaxThirst";
        private const string settingThirstDamage = "hydrateordiedrate:Config.Setting.ThirstDamage";
        private const string settingThirstDecayRate = "hydrateordiedrate:Config.Setting.ThirstDecayRate";
        private const string settingThirstDecayRateMax = "hydrateordiedrate:Config.Setting.ThirstDecayRateMax";
        private const string settingHydrationLossDelayMultiplierNormalized = "hydrateordiedrate:Config.Setting.HydrationLossDelayMultiplier";
        private const string settingWaterSatiety = "hydrateordiedrate:Config.Setting.WaterSatiety";
        private const string settingSaltWaterSatiety = "hydrateordiedrate:Config.Setting.SaltWaterSatiety";
        private const string settingBoilingWaterSatiety = "hydrateordiedrate:Config.Setting.BoilingWaterSatiety";
        private const string settingRainWaterSatiety = "hydrateordiedrate:Config.Setting.RainWaterSatiety";
        private const string settingBoiledWaterSatiety = "hydrateordiedrate:Config.Setting.BoiledWaterSatiety";
        private const string settingBoiledRainWaterSatiety = "hydrateordiedrate:Config.Setting.BoiledRainWaterSatiety";
        private const string settingDistilledWaterSatiety = "hydrateordiedrate:Config.Setting.DistilledWaterSatiety";
        private const string settingSprintThirstMultiplier = "hydrateordiedrate:Config.Setting.SprintThirstMultiplier";
        private const string settingEnableBoilingWaterDamage = "hydrateordiedrate:Config.Setting.EnableBoilingWaterDamage";
        private const string settingBoilingWaterDamage = "hydrateordiedrate:Config.Setting.BoilingWaterDamage";

        // Movement Speed Penalty Settings
        private const string settingMaxMovementSpeedPenalty = "hydrateordiedrate:Config.Setting.MaxMovementSpeedPenalty";
        private const string settingMovementSpeedPenaltyThreshold = "hydrateordiedrate:Config.Setting.MovementSpeedPenaltyThreshold";

        // Liquid Encumbrance Settings
        private const string settingEnableLiquidEncumbrance = "hydrateordiedrate:Config.Setting.EnableLiquidEncumbrance";
        private const string settingEncumbranceLimit = "hydrateordiedrate:Config.Setting.EncumbranceLimit";
        private const string settingLiquidEncumbranceMovementSpeedDebuff = "hydrateordiedrate:Config.Setting.LiquidEncumbranceMovementSpeedDebuff";

        // Temperature and Heat Settings
        private const string settingHarshHeat = "hydrateordiedrate:Config.Setting.HarshHeat";
        private const string settingTemperatureThreshold = "hydrateordiedrate:Config.Setting.TemperatureThreshold";
        private const string settingThirstIncreasePerDegreeMultiplier = "hydrateordiedrate:Config.Setting.ThirstIncreasePerDegreeMultiplier";
        private const string settingHarshHeatExponentialGainMultiplier = "hydrateordiedrate:Config.Setting.HarshHeatExponentialGainMultiplier";

        // Cooling Factors
        private const string settingUnequippedSlotCooling = "hydrateordiedrate:Config.Setting.UnequippedSlotCooling";
        private const string settingWetnessCoolingFactor = "hydrateordiedrate:Config.Setting.WetnessCoolingFactor";
        private const string settingShelterCoolingFactor = "hydrateordiedrate:Config.Setting.ShelterCoolingFactor";
        private const string settingSunlightCoolingFactor = "hydrateordiedrate:Config.Setting.SunlightCoolingFactor";
        private const string settingDiurnalVariationAmplitude = "hydrateordiedrate:Config.Setting.DiurnalVariationAmplitude";
        private const string settingRefrigerationCooling = "hydrateordiedrate:Config.Setting.RefrigerationCooling";

        // XLib Skills Settings
        private const string settingDromedaryMultiplierPerLevel = "hydrateordiedrate:Config.Setting.DromedaryMultiplierPerLevel";
        private const string settingEquatidianCoolingMultipliers = "hydrateordiedrate:Config.Setting.EquatidianCoolingMultipliers";

        // Rain Gathering Settings
        private const string settingEnableRainGathering = "hydrateordiedrate:Config.Setting.EnableRainGathering";
        private const string settingRainMultiplier = "hydrateordiedrate:Config.Setting.RainMultiplier";
        private const string settingEnableParticleTicking = "hydrateordiedrate:Config.Setting.EnableParticleTicking";

        // Keg Settings
        private const string settingKegCapacityLitres = "hydrateordiedrate:Config.Setting.KegCapacityLitres";
        private const string settingSpoilRateUntapped = "hydrateordiedrate:Config.Setting.SpoilRateUntapped";
        private const string settingSpoilRateTapped = "hydrateordiedrate:Config.Setting.SpoilRateTapped";
        private const string settingKegIronHoopDropChance = "hydrateordiedrate:Config.Setting.KegIronHoopDropChance";
        private const string settingKegTapDropChance = "hydrateordiedrate:Config.Setting.KegTapDropChance";

        // Tun Settings
        private const string settingTunCapacityLitres = "hydrateordiedrate:Config.Setting.TunCapacityLitres";
        private const string settingTunSpoilRateMultiplier = "hydrateordiedrate:Config.Setting.TunSpoilRateMultiplier";
        
        // Misc Settings
        private const string settingDisableDrunkSway = "hydrateordiedrate:Config.Setting.DisableDrunkSway";
        
        // Well Settings

        private const string settingWellSpringOutputMultiplier =
            "hydrateordiedrate:Config.Setting.WellSpringOutputMultiplier";
        private const string settingWellwaterDepthMaxBase = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxBase";
        private const string settingWellwaterDepthMaxClay = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxClay";
        private const string settingWellwaterDepthMaxStone = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxStone";
        private const string settingAquiferRandomMultiplierChance = "hydrateordiedrate:Config.Setting.AquiferRandomMultiplierChance";
        private const string settingAquiferStep = "hydrateordiedrate:Config.Setting.AquiferStep";
        private const string settingAquiferWaterBlockMultiplier = "hydrateordiedrate:Config.Setting.AquiferWaterBlockMultiplier";
        private const string settingAquiferSaltWaterMultiplier = "hydrateordiedrate:Config.Setting.AquiferSaltWaterMultiplier";
        private const string settingAquiferBoilingWaterMultiplier = "hydrateordiedrate:Config.Setting.AquiferBoilingWaterMultiplier";
        private const string settingWellWaterFreshSatiety = "hydrateordiedrate:Config.Setting.WellWaterFreshSatiety";
        private const string settingWellWaterSaltSatiety = "hydrateordiedrate:Config.Setting.WellWaterSaltSatiety";
        private const string settingWellWaterMuddySatiety = "hydrateordiedrate:Config.Setting.WellWaterMuddySatiety";
        private const string settingWellWaterTaintedSatiety = "hydrateordiedrate:Config.Setting.WellWaterTaintedSatiety";
        private const string settingWellWaterPoisonedSatiety = "hydrateordiedrate:Config.Setting.WellWaterPoisonedSatiety";
        private const string settingWellWaterMuddySaltSatiety = "hydrateordiedrate:Config.Setting.WellWaterMuddySaltSatiety";
        private const string settingWellWaterTaintedSaltSatiety = "hydrateordiedrate:Config.Setting.WellWaterTaintedSaltSatiety";
        private const string settingWellWaterPoisonedSaltSatiety = "hydrateordiedrate:Config.Setting.WellWaterPoisonedSaltSatiety";
        private const string settingProspectingRadius = "hydrateordiedrate:Config.Setting.ProspectingRadius";

        // Liquid Perish Rate Settings (new)
        private const string settingRainWaterFreshHours = "hydrateordiedrate:Config.Setting.RainWaterFreshHours";
        private const string settingRainWaterTransitionHours = "hydrateordiedrate:Config.Setting.RainWaterTransitionHours";
        private const string settingBoiledWaterFreshHours = "hydrateordiedrate:Config.Setting.BoiledWaterFreshHours";
        private const string settingBoiledWaterTransitionHours = "hydrateordiedrate:Config.Setting.BoiledWaterTransitionHours";
        private const string settingBoiledRainWaterFreshHours = "hydrateordiedrate:Config.Setting.BoiledRainWaterFreshHours";
        private const string settingBoiledRainWaterTransitionHours = "hydrateordiedrate:Config.Setting.BoiledRainWaterTransitionHours";
        private const string settingDistilledWaterFreshHours = "hydrateordiedrate:Config.Setting.DistilledWaterFreshHours";
        private const string settingDistilledWaterTransitionHours = "hydrateordiedrate:Config.Setting.DistilledWaterTransitionHours";
        private const string settingWellWaterFreshFreshHours = "hydrateordiedrate:Config.Setting.WellWaterFreshFreshHours";
        private const string settingWellWaterFreshTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterFreshTransitionHours";
        private const string settingWellWaterSaltFreshHours = "hydrateordiedrate:Config.Setting.WellWaterSaltFreshHours";
        private const string settingWellWaterSaltTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterSaltTransitionHours";
        private const string settingWellWaterMuddyFreshHours = "hydrateordiedrate:Config.Setting.WellWaterMuddyFreshHours";
        private const string settingWellWaterMuddyTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterMuddyTransitionHours";
        private const string settingWellWaterTaintedFreshHours = "hydrateordiedrate:Config.Setting.WellWaterTaintedFreshHours";
        private const string settingWellWaterTaintedTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterTaintedTransitionHours";
        private const string settingWellWaterPoisonedFreshHours = "hydrateordiedrate:Config.Setting.WellWaterPoisonedFreshHours";
        private const string settingWellWaterPoisonedTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterPoisonedTransitionHours";
        private const string settingWellWaterMuddySaltFreshHours = "hydrateordiedrate:Config.Setting.WellWaterMuddySaltFreshHours";
        private const string settingWellWaterMuddySaltTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterMuddySaltTransitionHours";
        private const string settingWellWaterTaintedSaltFreshHours = "hydrateordiedrate:Config.Setting.WellWaterTaintedSaltFreshHours";
        private const string settingWellWaterTaintedSaltTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterTaintedSaltTransitionHours";
        private const string settingWellWaterPoisonedSaltFreshHours = "hydrateordiedrate:Config.Setting.WellWaterPoisonedSaltFreshHours";
        private const string settingWellWaterPoisonedSaltTransitionHours = "hydrateordiedrate:Config.Setting.WellWaterPoisonedSaltTransitionHours";
        
        private const string settingWinchLowerSpeed = "hydrateordiedrate:Config.Setting.WinchLowerSpeed";
        private const string settingWinchRaiseSpeed = "hydrateordiedrate:Config.Setting.WinchRaiseSpeed";
        
        private const string settingKegDropWithLiquid = "hydrateordiedrate:Config.Setting.KegDropWithLiquid";
        private const string settingTunDropWithLiquid = "hydrateordiedrate:Config.Setting.TunDropWithLiquid";
        
        private const string settingSprintToDrink = "hydrateordiedrate:Config.Setting.SprintToDrink";

        private const string settingAquiferRatingCeilingAboveSeaLevel = "hydrateordiedrate:Config.Setting.settingAquiferRatingCeilingAboveSeaLevel";
        private const string settingAquiferDepthMultiplierScale = "hydrateordiedrate:Config.Setting.settingAquiferDepthMultiplierScale";
        
        private const string settingWaterPerish = "hydrateordiedrate:Config.Setting.WaterPerish";
        private const string settingAquiferDataOnProspectingNodeMode = "hydrateordiedrate:Config.Setting.AquiferDataOnProspectingNodeMode";
        private const string settingShowAquiferProspectingDataOnMap = "hydrateordiedrate:Config.Setting.ShowAquiferProspectingDataOnMap";
        private const string settingWinchOutputInfo = "hydrateordiedrate:Config.Setting.WinchOutputInfo";
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
            if (buttons.Save)
            {
                ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", HydrateOrDiedrateModSystem.LoadedConfig);
            }
            if (buttons.Reload)
            {
                Config reloadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
                if (reloadedConfig != null)
                {
                    HydrateOrDiedrateModSystem.LoadedConfig = reloadedConfig;
                    api.Logger.Notification("Config reloaded from file.");
                }
                else
                {
                    api.Logger.Warning("Failed to reload config from file.");
                }
            }
            if (buttons.Restore)
            {
                Config restoredConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
                if (restoredConfig != null)
                {
                    HydrateOrDiedrateModSystem.LoadedConfig = restoredConfig;
                    api.Logger.Notification("Config restored from file.");
                }
                else
                {
                    api.Logger.Warning("No saved config found to restore.");
                }
            }
            if (buttons.Defaults)
            {
                if (api.Side == EnumAppSide.Server)
                {
                    HydrateOrDiedrateModSystem.LoadedConfig = new Config();
                    ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", HydrateOrDiedrateModSystem.LoadedConfig);
                }
                else if (api.Side == EnumAppSide.Client)
                {
                    Config savedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
                    if (savedConfig != null)
                    {
                        HydrateOrDiedrateModSystem.LoadedConfig = savedConfig;
                    }
                    else
                    {
                        api.Logger.Warning(
                            "No saved config found when reloading defaults; using current config values.");
                    }
                }
            }
            Edit(api, HydrateOrDiedrateModSystem.LoadedConfig, id);
        }
        private void Edit(ICoreClientAPI api, Config config, string id)
        {
            ImGui.TextWrapped("HydrateOrDiedrate Settings");

            // Thirst Settings
            ImGui.SeparatorText("Thirst Settings");

            bool enableThirstMechanics = config.EnableThirstMechanics;
            ImGui.Checkbox(Lang.Get(settingEnableThirstMechanics) + $"##enableThirstMechanics-{id}", ref enableThirstMechanics);
            config.EnableThirstMechanics = enableThirstMechanics;

            float maxThirst = config.MaxThirst;
            ImGui.DragFloat(Lang.Get(settingMaxThirst) + $"##maxThirst-{id}", ref maxThirst, 100.0f, 100.0f, 10000.0f);
            config.MaxThirst = maxThirst;

            float thirstDamage = config.ThirstDamage;
            ImGui.DragFloat(Lang.Get(settingThirstDamage) + $"##thirstDamage-{id}", ref thirstDamage, 0.1f, 0.0f, 20.0f);
            config.ThirstDamage = thirstDamage;

            float thirstDecayRate = config.ThirstDecayRate;
            ImGui.DragFloat(Lang.Get(settingThirstDecayRate) + $"##thirstDecayRate-{id}", ref thirstDecayRate, 1.0f, 0.0f, 100.0f);
            config.ThirstDecayRate = thirstDecayRate;

            float thirstDecayRateMax = config.ThirstDecayRateMax;
            ImGui.DragFloat(Lang.Get(settingThirstDecayRateMax) + $"##thirstDecayRateMax-{id}", ref thirstDecayRateMax, 0.1f, 0.0f, 50.0f);
            config.ThirstDecayRateMax = thirstDecayRateMax;

            float hydrationLossDelayMultiplierNormalized = config.HydrationLossDelayMultiplierNormalized;
            ImGui.DragFloat(Lang.Get(settingHydrationLossDelayMultiplierNormalized) + $"##hydrationLossDelayMultiplierNormalized-{id}", ref hydrationLossDelayMultiplierNormalized, 0.1f, 0.0f, 10.0f);
            config.HydrationLossDelayMultiplierNormalized = hydrationLossDelayMultiplierNormalized;

            float waterSatiety = config.WaterSatiety;
            ImGui.DragFloat(Lang.Get(settingWaterSatiety) + $"##waterSatiety-{id}", ref waterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WaterSatiety = waterSatiety;

            float saltWaterSatiety = config.SaltWaterSatiety;
            ImGui.DragFloat(Lang.Get(settingSaltWaterSatiety) + $"##saltWaterSatiety-{id}", ref saltWaterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.SaltWaterSatiety = saltWaterSatiety;

            float boilingWaterSatiety = config.BoilingWaterSatiety;
            ImGui.DragFloat(Lang.Get(settingBoilingWaterSatiety) + $"##boilingWaterSatiety-{id}", ref boilingWaterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.BoilingWaterSatiety = boilingWaterSatiety;

            float rainWaterSatiety = config.RainWaterSatiety;
            ImGui.DragFloat(Lang.Get(settingRainWaterSatiety) + $"##rainWaterSatiety-{id}", ref rainWaterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.RainWaterSatiety = rainWaterSatiety;

            float distilledWaterSatiety = config.DistilledWaterSatiety;
            ImGui.DragFloat(Lang.Get(settingDistilledWaterSatiety) + $"##distilledWaterSatiety-{id}", ref distilledWaterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.DistilledWaterSatiety = distilledWaterSatiety;
            
            float boiledWaterSatiety = config.BoiledWaterSatiety;
            ImGui.DragFloat(Lang.Get(settingBoiledWaterSatiety) + $"##boiledWaterSatiety-{id}", ref boiledWaterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.BoiledWaterSatiety = boiledWaterSatiety;
            
            float boiledRainWaterSatiety = config.BoiledRainWaterSatiety;
            ImGui.DragFloat(Lang.Get(settingBoiledRainWaterSatiety) + $"##boiledRainWaterSatiety-{id}", ref boiledRainWaterSatiety, 1.0f, -1000.0f, 1000.0f);
            config.BoiledRainWaterSatiety = boiledRainWaterSatiety;

            float sprintThirstMultiplier = config.SprintThirstMultiplier;
            ImGui.DragFloat(Lang.Get(settingSprintThirstMultiplier) + $"##sprintThirstMultiplier-{id}", ref sprintThirstMultiplier, 0.1f, 0.0f, 5.0f);
            config.SprintThirstMultiplier = sprintThirstMultiplier;

            bool enableBoilingWaterDamage = config.EnableBoilingWaterDamage;
            ImGui.Checkbox(Lang.Get(settingEnableBoilingWaterDamage) + $"##enableBoilingWaterDamage-{id}", ref enableBoilingWaterDamage);
            config.EnableBoilingWaterDamage = enableBoilingWaterDamage;

            float boilingWaterDamage = config.BoilingWaterDamage;
            ImGui.DragFloat(Lang.Get(settingBoilingWaterDamage) + $"##boilingWaterDamage-{id}", ref boilingWaterDamage, 0.1f, 0.0f, 25.0f);
            config.BoilingWaterDamage = boilingWaterDamage;


            // Movement Speed Penalty Settings
            ImGui.SeparatorText("Movement Speed Penalty Settings");

            float maxMovementSpeedPenalty = config.MaxMovementSpeedPenalty;
            ImGui.DragFloat(Lang.Get(settingMaxMovementSpeedPenalty) + $"##maxMovementSpeedPenalty-{id}", ref maxMovementSpeedPenalty, 0.01f, 0.0f, 1.0f);
            config.MaxMovementSpeedPenalty = maxMovementSpeedPenalty;

            float movementSpeedPenaltyThreshold = config.MovementSpeedPenaltyThreshold;
            ImGui.DragFloat(Lang.Get(settingMovementSpeedPenaltyThreshold) + $"##movementSpeedPenaltyThreshold-{id}", ref movementSpeedPenaltyThreshold, 10.0f, 0.0f, 1500.0f);
            config.MovementSpeedPenaltyThreshold = movementSpeedPenaltyThreshold;


            // Liquid Encumbrance Settings
            ImGui.SeparatorText("Liquid Encumbrance Settings");

            bool enableLiquidEncumbrance = config.EnableLiquidEncumbrance;
            ImGui.Checkbox(Lang.Get(settingEnableLiquidEncumbrance) + $"##enableLiquidEncumbrance-{id}", ref enableLiquidEncumbrance);
            config.EnableLiquidEncumbrance = enableLiquidEncumbrance;

            float encumbranceLimit = config.EncumbranceLimit;
            ImGui.DragFloat(Lang.Get(settingEncumbranceLimit) + $"##encumbranceLimit-{id}", ref encumbranceLimit, 0.1f, 0.0f, 10.0f);
            config.EncumbranceLimit = encumbranceLimit;

            float liquidEncumbranceMovementSpeedDebuff = config.LiquidEncumbranceMovementSpeedDebuff;
            ImGui.DragFloat(Lang.Get(settingLiquidEncumbranceMovementSpeedDebuff) + $"##liquidEncumbranceMovementSpeedDebuff-{id}", ref liquidEncumbranceMovementSpeedDebuff, 0.01f, 0.0f, 1.0f);
            config.LiquidEncumbranceMovementSpeedDebuff = liquidEncumbranceMovementSpeedDebuff;


            // Temperature and Heat Settings
            ImGui.SeparatorText("Temperature and Heat Settings");

            bool harshHeat = config.HarshHeat;
            ImGui.Checkbox(Lang.Get(settingHarshHeat) + $"##harshHeat-{id}", ref harshHeat);
            config.HarshHeat = harshHeat;

            float temperatureThreshold = config.TemperatureThreshold;
            ImGui.DragFloat(Lang.Get(settingTemperatureThreshold) + $"##temperatureThreshold-{id}", ref temperatureThreshold, 1.0f, 0.0f, 50.0f);
            config.TemperatureThreshold = temperatureThreshold;

            float thirstIncreasePerDegreeMultiplier = config.ThirstIncreasePerDegreeMultiplier;
            ImGui.DragFloat(Lang.Get(settingThirstIncreasePerDegreeMultiplier) + $"##thirstIncreasePerDegreeMultiplier-{id}", ref thirstIncreasePerDegreeMultiplier, 0.1f, 0.0f, 10.0f);
            config.ThirstIncreasePerDegreeMultiplier = thirstIncreasePerDegreeMultiplier;

            float harshHeatExponentialGainMultiplier = config.HarshHeatExponentialGainMultiplier;
            ImGui.DragFloat(Lang.Get(settingHarshHeatExponentialGainMultiplier) + $"##harshHeatExponentialGainMultiplier-{id}", ref harshHeatExponentialGainMultiplier, 0.01f, 0.0f, 1.0f);
            config.HarshHeatExponentialGainMultiplier = harshHeatExponentialGainMultiplier;


            // Cooling Factors
            ImGui.SeparatorText("Cooling Factors");

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
            ImGui.DragFloat(Lang.Get(settingRefrigerationCooling) + $"##refrigerationCooling-{id}", ref refrigerationCooling, 0.1f, 0.0f, 50.0f);
            config.RefrigerationCooling = refrigerationCooling;


            // XLib Skills Settings
            ImGui.SeparatorText("XLib Skills Settings");

            float dromedaryMultiplierPerLevel = config.DromedaryMultiplierPerLevel;
            ImGui.DragFloat(Lang.Get(settingDromedaryMultiplierPerLevel) + $"##dromedaryMultiplierPerLevel-{id}", ref dromedaryMultiplierPerLevel, 0.01f, 0.1f, 3.0f);
            config.DromedaryMultiplierPerLevel = dromedaryMultiplierPerLevel;

            ImGui.TextWrapped(Lang.Get(settingEquatidianCoolingMultipliers));
            float equatidianLevel1 = config.EquatidianCoolingMultipliers[0];
            ImGui.DragFloat($"Level 1##equatidianLevel1-{id}", ref equatidianLevel1, 0.01f, 1.0f, 5.0f);
            config.EquatidianCoolingMultipliers[0] = equatidianLevel1;

            float equatidianLevel2 = config.EquatidianCoolingMultipliers[1];
            ImGui.DragFloat($"Level 2##equatidianLevel2-{id}", ref equatidianLevel2, 0.01f, 1.0f, 5.0f);
            config.EquatidianCoolingMultipliers[1] = equatidianLevel2;

            float equatidianLevel3 = config.EquatidianCoolingMultipliers[2];
            ImGui.DragFloat($"Level 3##equatidianLevel3-{id}", ref equatidianLevel3, 0.01f, 1.0f, 5.0f);
            config.EquatidianCoolingMultipliers[2] = equatidianLevel3;


            // Rain Gathering Settings
            ImGui.SeparatorText("Rain Gathering Settings");

            bool enableRainGathering = config.EnableRainGathering;
            ImGui.Checkbox(Lang.Get(settingEnableRainGathering) + $"##enableRainGathering-{id}", ref enableRainGathering);
            config.EnableRainGathering = enableRainGathering;

            float rainMultiplier = config.RainMultiplier;
            ImGui.DragFloat(Lang.Get(settingRainMultiplier) + $"##rainMultiplier-{id}", ref rainMultiplier, 0.1f, 0.1f, 10.0f);
            config.RainMultiplier = rainMultiplier;
            
            bool enableParticleTicking = config.EnableParticleTicking;
            ImGui.Checkbox(Lang.Get(settingEnableParticleTicking) + $"##enableParticleTicking-{id}", ref enableParticleTicking);
            config.EnableParticleTicking = enableParticleTicking;
            
            // Keg Settings
            ImGui.SeparatorText("Keg Settings");

            float kegCapacityLitres = config.KegCapacityLitres;
            ImGui.DragFloat(Lang.Get(settingKegCapacityLitres) + $"##kegCapacityLitres-{id}", ref kegCapacityLitres, 1.0f, 10.0f, 500.0f);
            config.KegCapacityLitres = kegCapacityLitres;

            float spoilRateUntapped = config.SpoilRateUntapped;
            ImGui.DragFloat(Lang.Get(settingSpoilRateUntapped) + $"##spoilRateUntapped-{id}", ref spoilRateUntapped, 0.01f, 0.1f, 1.0f);
            config.SpoilRateUntapped = spoilRateUntapped;

            float spoilRateTapped = config.SpoilRateTapped;
            ImGui.DragFloat(Lang.Get(settingSpoilRateTapped) + $"##spoilRateTapped-{id}", ref spoilRateTapped, 0.01f, 0.1f, 1.0f);
            config.SpoilRateTapped = spoilRateTapped;

            float kegIronHoopDropChance = config.KegIronHoopDropChance;
            ImGui.DragFloat(Lang.Get(settingKegIronHoopDropChance) + $"##kegIronHoopDropChance-{id}", ref kegIronHoopDropChance, 0.01f, 0.0f, 1.0f);
            config.KegIronHoopDropChance = kegIronHoopDropChance;

            float kegTapDropChance = config.KegTapDropChance;
            ImGui.DragFloat(Lang.Get(settingKegTapDropChance) + $"##kegTapDropChance-{id}", ref kegTapDropChance, 0.01f, 0.0f, 1.0f);
            config.KegTapDropChance = kegTapDropChance;


            // Tun Settings
            ImGui.SeparatorText("Tun Settings");

            float tunCapacityLitres = config.TunCapacityLitres;
            ImGui.DragFloat(Lang.Get(settingTunCapacityLitres) + $"##tunCapacityLitres-{id}", ref tunCapacityLitres, 1.0f, 10.0f, 2000.0f);
            config.TunCapacityLitres = tunCapacityLitres;

            float tunSpoilRateMultiplier = config.TunSpoilRateMultiplier;
            ImGui.DragFloat(Lang.Get(settingTunSpoilRateMultiplier) + $"##tunSpoilRateMultiplier-{id}", ref tunSpoilRateMultiplier, 0.01f, 0.1f, 5.0f);
            config.TunSpoilRateMultiplier = tunSpoilRateMultiplier;
            
            // Misc Settings
            ImGui.SeparatorText("Misc Settings");
            
            bool disableDrunkSway = config.DisableDrunkSway;
            ImGui.Checkbox(Lang.Get(settingDisableDrunkSway) + $"##disableDrunkSway-{id}", ref disableDrunkSway);
            config.DisableDrunkSway = disableDrunkSway;
            
            // Well Settings
            ImGui.SeparatorText("Well Settings");

            // WellSpringOutputMultiplier
            float wellSpringOutputMultiplier = config.WellSpringOutputMultiplier;
            ImGui.DragFloat(Lang.Get(settingWellSpringOutputMultiplier) + $"##wellSpringOutputMultiplier-{id}", ref wellSpringOutputMultiplier, 0.1f, 0.1f, 10.0f);
            config.WellSpringOutputMultiplier = wellSpringOutputMultiplier;
            
            // RandomMultiplierChance
            float aquiferRandomMultiplierChance = (float)config.AquiferRandomMultiplierChance;
            ImGui.DragFloat(Lang.Get(settingAquiferRandomMultiplierChance) + $"##aquiferRandomMultiplierChance-{id}", ref aquiferRandomMultiplierChance, 0.001f, 0.0f, 1.0f);
            config.AquiferRandomMultiplierChance = aquiferRandomMultiplierChance;

            // Wellwater Depth Max Base
            int wellwaterDepthMaxBase = config.WellwaterDepthMaxBase;
            ImGui.DragInt(Lang.Get(settingWellwaterDepthMaxBase) + $"##wellwaterDepthMaxBase-{id}", ref wellwaterDepthMaxBase, 1, 1, 20);
            config.WellwaterDepthMaxBase = wellwaterDepthMaxBase;

            // Wellwater Depth Max Clay
            int wellwaterDepthMaxClay = config.WellwaterDepthMaxClay;
            ImGui.DragInt(Lang.Get(settingWellwaterDepthMaxClay) + $"##wellwaterDepthMaxClay-{id}", ref wellwaterDepthMaxClay, 1, 1, 20);
            config.WellwaterDepthMaxClay = wellwaterDepthMaxClay;

            // Wellwater Depth Max Stone
            int wellwaterDepthMaxStone = config.WellwaterDepthMaxStone;
            ImGui.DragInt(Lang.Get(settingWellwaterDepthMaxStone) + $"##wellwaterDepthMaxStone-{id}", ref wellwaterDepthMaxStone, 1, 1, 20);
            config.WellwaterDepthMaxStone = wellwaterDepthMaxStone;

            
            // Step
            int aquiferStep = config.AquiferStep;
            ImGui.DragInt(Lang.Get(settingAquiferStep) + $"##aquiferStep-{id}", ref aquiferStep, 1, 1, 32);
            config.AquiferStep = aquiferStep;

            // WaterBlockMultiplier
            float aquiferWaterBlockMultiplier = (float)config.AquiferWaterBlockMultiplier;
            ImGui.DragFloat(Lang.Get(settingAquiferWaterBlockMultiplier) + $"##aquiferWaterBlockMultiplier-{id}", ref aquiferWaterBlockMultiplier, 0.1f, 0.1f, 10.0f);
            config.AquiferWaterBlockMultiplier = aquiferWaterBlockMultiplier;

            // SaltWaterMultiplier
            float aquiferSaltWaterMultiplier = (float)config.AquiferSaltWaterMultiplier;
            ImGui.DragFloat(Lang.Get(settingAquiferSaltWaterMultiplier) + $"##aquiferSaltWaterMultiplier-{id}", ref aquiferSaltWaterMultiplier, 0.1f, 0.1f, 10.0f);
            config.AquiferSaltWaterMultiplier = aquiferSaltWaterMultiplier;

            // BoilingWaterMultiplier
            int aquiferBoilingWaterMultiplier = config.AquiferBoilingWaterMultiplier;
            ImGui.DragInt(Lang.Get(settingAquiferBoilingWaterMultiplier) + $"##aquiferBoilingWaterMultiplier-{id}", ref aquiferBoilingWaterMultiplier, 1, 1, 500);
            config.AquiferBoilingWaterMultiplier = aquiferBoilingWaterMultiplier;
            
            float wellWaterFreshSatiety = config.WellWaterFreshSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterFreshSatiety) + $"##wellWaterFreshSatiety-{id}", ref wellWaterFreshSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterFreshSatiety = wellWaterFreshSatiety;

            float wellWaterSaltSatiety = config.WellWaterSaltSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterSaltSatiety) + $"##wellWaterSaltSatiety-{id}", ref wellWaterSaltSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterSaltSatiety = wellWaterSaltSatiety;

            float wellWaterMuddySatiety = config.WellWaterMuddySatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterMuddySatiety) + $"##wellWaterMuddySatiety-{id}", ref wellWaterMuddySatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterMuddySatiety = wellWaterMuddySatiety;

            float wellWaterTaintedSatiety = config.WellWaterTaintedSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSatiety) + $"##wellWaterTaintedSatiety-{id}", ref wellWaterTaintedSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterTaintedSatiety = wellWaterTaintedSatiety;

            float wellWaterPoisonedSatiety = config.WellWaterPoisonedSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSatiety) + $"##wellWaterPoisonedSatiety-{id}", ref wellWaterPoisonedSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterPoisonedSatiety = wellWaterPoisonedSatiety;

            float wellWaterMuddySaltSatiety = config.WellWaterMuddySaltSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterMuddySaltSatiety) + $"##wellWaterMuddySaltSatiety-{id}", ref wellWaterMuddySaltSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterMuddySaltSatiety = wellWaterMuddySaltSatiety;

            float wellWaterTaintedSaltSatiety = config.WellWaterTaintedSaltSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSaltSatiety) + $"##wellWaterTaintedSaltSatiety-{id}", ref wellWaterTaintedSaltSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterTaintedSaltSatiety = wellWaterTaintedSaltSatiety;

            float wellWaterPoisonedSaltSatiety = config.WellWaterPoisonedSaltSatiety;
            ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSaltSatiety) + $"##wellWaterPoisonedSaltSatiety-{id}", ref wellWaterPoisonedSaltSatiety, 1.0f, -1000.0f, 1000.0f);
            config.WellWaterPoisonedSaltSatiety = wellWaterPoisonedSaltSatiety;
            
            int prospectingRadius = config.ProspectingRadius;
            ImGui.DragInt(Lang.Get(settingProspectingRadius) + $"##prospectingRadius-{id}", ref prospectingRadius, 1, 1, 10);
            config.ProspectingRadius = prospectingRadius;
            
            ImGui.SeparatorText("Liquid Perish Rates");

            float rainWaterFreshHours = config.RainWaterFreshHours;
            ImGui.DragFloat(Lang.Get(settingRainWaterFreshHours) + $"##rainWaterFreshHours-{id}", ref rainWaterFreshHours, 1.0f, 0.0f, 1000.0f);
            config.RainWaterFreshHours = rainWaterFreshHours;

            float rainWaterTransitionHours = config.RainWaterTransitionHours;
            ImGui.DragFloat(Lang.Get(settingRainWaterTransitionHours) + $"##rainWaterTransitionHours-{id}", ref rainWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.RainWaterTransitionHours = rainWaterTransitionHours;

            float boiledWaterFreshHours = config.BoiledWaterFreshHours;
            ImGui.DragFloat(Lang.Get(settingBoiledWaterFreshHours) + $"##boiledWaterFreshHours-{id}", ref boiledWaterFreshHours, 1.0f, 0.0f, 1000.0f);
            config.BoiledWaterFreshHours = boiledWaterFreshHours;

            float boiledWaterTransitionHours = config.BoiledWaterTransitionHours;
            ImGui.DragFloat(Lang.Get(settingBoiledWaterTransitionHours) + $"##boiledWaterTransitionHours-{id}", ref boiledWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.BoiledWaterTransitionHours = boiledWaterTransitionHours;

            float boiledRainWaterFreshHours = config.BoiledRainWaterFreshHours;
            ImGui.DragFloat(Lang.Get(settingBoiledRainWaterFreshHours) + $"##boiledRainWaterFreshHours-{id}", ref boiledRainWaterFreshHours, 1.0f, 0.0f, 1000.0f);
            config.BoiledRainWaterFreshHours = boiledRainWaterFreshHours;

            float boiledRainWaterTransitionHours = config.BoiledRainWaterTransitionHours;
            ImGui.DragFloat(Lang.Get(settingBoiledRainWaterTransitionHours) + $"##boiledRainWaterTransitionHours-{id}", ref boiledRainWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.BoiledRainWaterTransitionHours = boiledRainWaterTransitionHours;

            float distilledWaterFreshHours = config.DistilledWaterFreshHours;
            ImGui.DragFloat(Lang.Get(settingDistilledWaterFreshHours) + $"##distilledWaterFreshHours-{id}", ref distilledWaterFreshHours, 1.0f, 0.0f, 1000.0f);
            config.DistilledWaterFreshHours = distilledWaterFreshHours;

            float distilledWaterTransitionHours = config.DistilledWaterTransitionHours;
            ImGui.DragFloat(Lang.Get(settingDistilledWaterTransitionHours) + $"##distilledWaterTransitionHours-{id}", ref distilledWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.DistilledWaterTransitionHours = distilledWaterTransitionHours;

            float wellWaterFreshFreshHours = config.WellWaterFreshFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterFreshFreshHours) + $"##wellWaterFreshFreshHours-{id}", ref wellWaterFreshFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterFreshFreshHours = wellWaterFreshFreshHours;

            float wellWaterFreshTransitionHours = config.WellWaterFreshTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterFreshTransitionHours) + $"##wellWaterFreshTransitionHours-{id}", ref wellWaterFreshTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterFreshTransitionHours = wellWaterFreshTransitionHours;

            float wellWaterSaltFreshHours = config.WellWaterSaltFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterSaltFreshHours) + $"##wellWaterSaltFreshHours-{id}", ref wellWaterSaltFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterSaltFreshHours = wellWaterSaltFreshHours;

            float wellWaterSaltTransitionHours = config.WellWaterSaltTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterSaltTransitionHours) + $"##wellWaterSaltTransitionHours-{id}", ref wellWaterSaltTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterSaltTransitionHours = wellWaterSaltTransitionHours;

            float wellWaterMuddyFreshHours = config.WellWaterMuddyFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterMuddyFreshHours) + $"##wellWaterMuddyFreshHours-{id}", ref wellWaterMuddyFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterMuddyFreshHours = wellWaterMuddyFreshHours;

            float wellWaterMuddyTransitionHours = config.WellWaterMuddyTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterMuddyTransitionHours) + $"##wellWaterMuddyTransitionHours-{id}", ref wellWaterMuddyTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterMuddyTransitionHours = wellWaterMuddyTransitionHours;

            float wellWaterTaintedFreshHours = config.WellWaterTaintedFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterTaintedFreshHours) + $"##wellWaterTaintedFreshHours-{id}", ref wellWaterTaintedFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterTaintedFreshHours = wellWaterTaintedFreshHours;

            float wellWaterTaintedTransitionHours = config.WellWaterTaintedTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterTaintedTransitionHours) + $"##wellWaterTaintedTransitionHours-{id}", ref wellWaterTaintedTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterTaintedTransitionHours = wellWaterTaintedTransitionHours;

            float wellWaterPoisonedFreshHours = config.WellWaterPoisonedFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedFreshHours) + $"##wellWaterPoisonedFreshHours-{id}", ref wellWaterPoisonedFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterPoisonedFreshHours = wellWaterPoisonedFreshHours;

            float wellWaterPoisonedTransitionHours = config.WellWaterPoisonedTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedTransitionHours) + $"##wellWaterPoisonedTransitionHours-{id}", ref wellWaterPoisonedTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterPoisonedTransitionHours = wellWaterPoisonedTransitionHours;

            float wellWaterMuddySaltFreshHours = config.WellWaterMuddySaltFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterMuddySaltFreshHours) + $"##wellWaterMuddySaltFreshHours-{id}", ref wellWaterMuddySaltFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterMuddySaltFreshHours = wellWaterMuddySaltFreshHours;

            float wellWaterMuddySaltTransitionHours = config.WellWaterMuddySaltTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterMuddySaltTransitionHours) + $"##wellWaterMuddySaltTransitionHours-{id}", ref wellWaterMuddySaltTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterMuddySaltTransitionHours = wellWaterMuddySaltTransitionHours;

            float wellWaterTaintedSaltFreshHours = config.WellWaterTaintedSaltFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSaltFreshHours) + $"##wellWaterTaintedSaltFreshHours-{id}", ref wellWaterTaintedSaltFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterTaintedSaltFreshHours = wellWaterTaintedSaltFreshHours;

            float wellWaterTaintedSaltTransitionHours = config.WellWaterTaintedSaltTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSaltTransitionHours) + $"##wellWaterTaintedSaltTransitionHours-{id}", ref wellWaterTaintedSaltTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterTaintedSaltTransitionHours = wellWaterTaintedSaltTransitionHours;

            float wellWaterPoisonedSaltFreshHours = config.WellWaterPoisonedSaltFreshHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSaltFreshHours) + $"##wellWaterPoisonedSaltFreshHours-{id}", ref wellWaterPoisonedSaltFreshHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterPoisonedSaltFreshHours = wellWaterPoisonedSaltFreshHours;

            float wellWaterPoisonedSaltTransitionHours = config.WellWaterPoisonedSaltTransitionHours;
            ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSaltTransitionHours) + $"##wellWaterPoisonedSaltTransitionHours-{id}", ref wellWaterPoisonedSaltTransitionHours, 1.0f, 0.0f, 1000.0f);
            config.WellWaterPoisonedSaltTransitionHours = wellWaterPoisonedSaltTransitionHours;
            
            ImGui.SeparatorText("Winch Speed");
            
            float winchLowerSpeed = config.WinchLowerSpeed;
            ImGui.DragFloat(Lang.Get(settingWinchLowerSpeed) + $"##winchLowerSpeed-{id}", ref winchLowerSpeed, 0.01f, 0.0f, 5.0f);
            config.WinchLowerSpeed = winchLowerSpeed;

            float winchRaiseSpeed = config.WinchRaiseSpeed;
            ImGui.DragFloat(Lang.Get(settingWinchRaiseSpeed) + $"##winchRaiseSpeed-{id}", ref winchRaiseSpeed, 0.01f, 0.0f, 5.0f);
            config.WinchRaiseSpeed = winchRaiseSpeed;
            
            bool kegDropWithLiquid = config.KegDropWithLiquid;
            ImGui.Checkbox(Lang.Get(settingKegDropWithLiquid) + $"##kegDropWithLiquid-{id}", ref kegDropWithLiquid);
            config.KegDropWithLiquid = kegDropWithLiquid;
            
            bool tunDropWithLiquid = config.TunDropWithLiquid;
            ImGui.Checkbox(Lang.Get(settingTunDropWithLiquid) + $"##tunDropWithLiquid-{id}", ref tunDropWithLiquid);
            config.TunDropWithLiquid = tunDropWithLiquid;
            
            bool sprintToDrink = config.SprintToDrink;
            ImGui.Checkbox(Lang.Get(settingSprintToDrink) + $"##sprintToDrink-{id}", ref sprintToDrink);
            config.SprintToDrink = sprintToDrink;
            
            int aquiferRatingCeilingAboveSeaLevel = config.AquiferRatingCeilingAboveSeaLevel;
            ImGui.DragInt(Lang.Get(settingAquiferRatingCeilingAboveSeaLevel) + $"##aquiferRatingCeilingAboveSeaLevel-{id}", ref aquiferRatingCeilingAboveSeaLevel, 1, 0, 100);
            config.AquiferRatingCeilingAboveSeaLevel = aquiferRatingCeilingAboveSeaLevel;

            float aquiferDepthMultiplierScale = config.AquiferDepthMultiplierScale;
            ImGui.DragFloat(Lang.Get(settingAquiferDepthMultiplierScale) + $"##aquiferDepthMultiplierScale-{id}", ref aquiferDepthMultiplierScale, 0.01f, 0.5f, 5.0f);
            config.AquiferDepthMultiplierScale = aquiferDepthMultiplierScale;
            
            bool waterPerish = config.WaterPerish;
            ImGui.Checkbox(Lang.Get(settingWaterPerish) + $"##waterPerish-{id}", ref waterPerish);
            config.WaterPerish = waterPerish;
            
            bool aquiferDataOnProspectingNodeMode = config.AquiferDataOnProspectingNodeMode;
            ImGui.Checkbox(Lang.Get(settingAquiferDataOnProspectingNodeMode) + $"##aquiferDataOnProspectingNodeMode-{id}", ref aquiferDataOnProspectingNodeMode);
            config.AquiferDataOnProspectingNodeMode = aquiferDataOnProspectingNodeMode;
            
            bool showAquiferProspectingDataOnMap = config.ShowAquiferProspectingDataOnMap;
            ImGui.Checkbox(Lang.Get(settingShowAquiferProspectingDataOnMap) + $"##showAquiferProspectingDataOnMap-{id}", ref showAquiferProspectingDataOnMap);
            config.ShowAquiferProspectingDataOnMap = showAquiferProspectingDataOnMap;
                
            bool winchOutputInfo = config.WinchOutputInfo;
            ImGui.Checkbox(Lang.Get(settingWinchOutputInfo) + $"##winchOutputInfo-{id}", ref winchOutputInfo);
            config.WinchOutputInfo = winchOutputInfo;
        }
    }
}
