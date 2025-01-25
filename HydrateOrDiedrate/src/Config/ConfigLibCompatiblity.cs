using ConfigLib;
using ImGuiNET;
using Vintagestory.API.Client;
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
        private const string settingHydrationLossDelayMultiplier = "hydrateordiedrate:Config.Setting.HydrationLossDelayMultiplier";
        private const string settingWaterSatiety = "hydrateordiedrate:Config.Setting.WaterSatiety";
        private const string settingSaltWaterSatiety = "hydrateordiedrate:Config.Setting.SaltWaterSatiety";
        private const string settingBoilingWaterSatiety = "hydrateordiedrate:Config.Setting.BoilingWaterSatiety";
        private const string settingRainWaterSatiety = "hydrateordiedrate:Config.Setting.RainWaterSatiety";
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
        private const string settingDisableDrunkSway = "hydrateirduedrate:Config.Settings.DisableDrunkSway";
        
        // Well Settings

        private const string settingWellSpringOutputMultiplier =
            "hydrateordiedrate:Config.Settings.WellSpringOutputMultiplier";
        private const string settingWellwaterDepthMaxBase = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxBase";
        private const string settingWellwaterDepthMaxClay = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxClay";
        private const string settingWellwaterDepthMaxStone = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxStone";
        private const string settingAquiferRandomMultiplierChance = "hydrateordiedrate:Config.Settings.AquiferRandomMultiplierChance";
        private const string settingAquiferStep = "hydrateordiedrate:Config.Settings.AquiferStep";
        private const string settingAquiferWaterBlockMultiplier = "hydrateordiedrate:Config.Settings.AquiferWaterBlockMultiplier";
        private const string settingAquiferSaltWaterMultiplier = "hydrateordiedrate:Config.Settings.AquiferSaltWaterMultiplier";
        private const string settingAquiferBoilingWaterMultiplier = "hydrateordiedrate:Config.Settings.AquiferBoilingWaterMultiplier";

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

            float hydrationLossDelayMultiplier = config.HydrationLossDelayMultiplier;
            ImGui.DragFloat(Lang.Get(settingHydrationLossDelayMultiplier) + $"##hydrationLossDelayMultiplier-{id}", ref hydrationLossDelayMultiplier, 0.01f, 0.0f, 1.0f);
            config.HydrationLossDelayMultiplier = hydrationLossDelayMultiplier;

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
        }
    }
}
