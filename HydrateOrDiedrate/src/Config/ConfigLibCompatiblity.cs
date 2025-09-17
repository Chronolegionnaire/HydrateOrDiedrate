using ConfigLib;
using ImGuiNET;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HydrateOrDiedrate.Config;

public class ConfigLibCompatibility
{
    // Thirst Settings
    private const string settingEnableThirstMechanics = "hydrateordiedrate:Config.Setting.EnableThirstMechanics";
    private const string settingMaxThirst = "hydrateordiedrate:Config.Setting.MaxThirst";
    private const string settingThirstDamage = "hydrateordiedrate:Config.Setting.ThirstDamage";
    private const string settingThirstDecayRate = "hydrateordiedrate:Config.Setting.ThirstDecayRate";
    private const string settingThirstDecayRateMax = "hydrateordiedrate:Config.Setting.ThirstDecayRateMax";
    private const string settingHydrationLossDelayMultiplierNormalized = "hydrateordiedrate:Config.Setting.HydrationLossDelayMultiplierNormalized";
    private const string settingWaterSatiety = "hydrateordiedrate:Config.Setting.WaterSatiety";
    private const string settingSaltWaterSatiety = "hydrateordiedrate:Config.Setting.SaltWaterSatiety";
    private const string settingBoilingWaterSatiety = "hydrateordiedrate:Config.Setting.BoilingWaterSatiety";
    private const string settingRainWaterSatiety = "hydrateordiedrate:Config.Setting.RainWaterSatiety";
    private const string settingBoiledWaterSatiety = "hydrateordiedrate:Config.Setting.BoiledWaterSatiety";
    private const string settingBoiledRainWaterSatiety = "hydrateordiedrate:Config.Setting.BoiledRainWaterSatiety";
    private const string settingDistilledWaterSatiety = "hydrateordiedrate:Config.Setting.DistilledWaterSatiety";
    private const string settingSprintThirstMultiplier = "hydrateordiedrate:Config.Setting.SprintThirstMultiplier";
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

    private const string settingWellSpringOutputMultiplier ="hydrateordiedrate:Config.Setting.WellSpringOutputMultiplier";
    private const string settingWellwaterDepthMaxBase = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxBase";
    private const string settingWellwaterDepthMaxClay = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxClay";
    private const string settingWellwaterDepthMaxStone = "hydrateordiedrate:Config.Setting.WellwaterDepthMaxStone";
    private const string settingAquiferRandomMultiplierChance = "hydrateordiedrate:Config.Setting.AquiferRandomMultiplierChance";
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
    private const string settingAquiferMinimumWaterBlockThreshold = "hydrateordiedrate:Config.Setting.settingAquiferMinimumWaterBlockThreshold";
    
    private const string settingWaterPerish = "hydrateordiedrate:Config.Setting.WaterPerish";
    private const string settingAquiferDataOnProspectingNodeMode = "hydrateordiedrate:Config.Setting.AquiferDataOnProspectingNodeMode";
    private const string settingShowAquiferProspectingDataOnMap = "hydrateordiedrate:Config.Setting.ShowAquiferProspectingDataOnMap";
    private const string settingWinchOutputInfo = "hydrateordiedrate:Config.Setting.WinchOutputInfo";

    private ConfigLibCompatibility()
    {
    }

    /// <summary>
    /// A copy of the ModConfig since we don't want to mutate the live config before we are sure everything is valid
    /// </summary>
    public ModConfig EditInstance { get; private set; }

    private static ModConfig LoadFromDisk(ICoreAPI api)
    {
        try
        {
            return api.LoadModConfig<ModConfig>(ModConfig.ConfigPath) ?? new ModConfig();
        }
        catch(Exception ex)
        {
            api.Logger.Error(ex);
            return new ModConfig();
        }
    }

    internal static void Init(ICoreClientAPI api)
    {
        var container = new ConfigLibCompatibility();
        api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig("hydrateordiedrate", (id, buttons) =>
        {
            container.EditConfig(id, buttons, api);
            
            return new ControlButtons
            {
                Save = true,
                Restore = true,
                Defaults = true,
                Reload = api.IsSinglePlayer //There currently isn't any logic for re-sending configs to server and connected clients
            };
        });
    }

    private void EditConfig(string id, ControlButtons buttons, ICoreClientAPI api)
    {
        //Ensure we have a copy of the config ready (late initialized because we only need this if the editor was opened)
        EditInstance ??= ModConfig.Instance.JsonCopy();
        
        Edit(EditInstance, id);

        if (buttons.Save) ConfigManager.SaveModConfig(api, EditInstance);
        else if (buttons.Restore) EditInstance = LoadFromDisk(api);
        else if (buttons.Defaults) EditInstance = new ModConfig();
        else if (buttons.Reload) //Reload is for hot reloading config values
        {
            if (api.IsSinglePlayer)
            {
                ModConfig.Instance = EditInstance;
                EditInstance = null;
                ConfigManager.StoreModConfigToWorldConfig(api);
            }
            else
            {
                //TODO: maybe support reloading (at least part of) the config
            }
            
        }
    }

    private static void Edit(ModConfig config, string id)
    {
        var thirstConfig = config.Thirst;
        var satietyConfig = config.Satiety;
        var perishRateConfig = config.PerishRates;
        var liquidEncumbranceConfig = config.LiquidEncumbrance;
        var heatAndCoolingConfig = config.HeatAndCooling;
        var xlibConfig = config.XLib;
        var rainConfig = config.Rain;
        var containersConfig = config.Containers;
        var groundWaterConfig = config.GroundWater;
        ImGui.TextWrapped("HydrateOrDiedrate Settings");

        // Thirst Settings
        ImGui.SeparatorText("Thirst Settings");

        bool enableThirstMechanics = thirstConfig.Enabled;
        ImGui.Checkbox(Lang.Get(settingEnableThirstMechanics) + $"##enableThirstMechanics-{id}", ref enableThirstMechanics);
        thirstConfig.Enabled = enableThirstMechanics;

        float maxThirst = thirstConfig.MaxThirst;
        ImGui.DragFloat(Lang.Get(settingMaxThirst) + $"##maxThirst-{id}", ref maxThirst, 100.0f, 100.0f, 10000.0f);
        thirstConfig.MaxThirst = maxThirst;

        float thirstDamage = thirstConfig.ThirstDamage;
        ImGui.DragFloat(Lang.Get(settingThirstDamage) + $"##thirstDamage-{id}", ref thirstDamage, 0.1f, 0.0f, 20.0f);
        thirstConfig.ThirstDamage = thirstDamage;

        float thirstDecayRate = thirstConfig.ThirstDecayRate;
        ImGui.DragFloat(Lang.Get(settingThirstDecayRate) + $"##thirstDecayRate-{id}", ref thirstDecayRate, 1.0f, 0.0f, 100.0f);
        thirstConfig.ThirstDecayRate = thirstDecayRate;

        float thirstDecayRateMax = thirstConfig.ThirstDecayRateMax;
        ImGui.DragFloat(Lang.Get(settingThirstDecayRateMax) + $"##thirstDecayRateMax-{id}", ref thirstDecayRateMax, 0.1f, 0.0f, 50.0f);
        thirstConfig.ThirstDecayRateMax = thirstDecayRateMax;

        float hydrationLossDelayMultiplierNormalized = thirstConfig.HydrationLossDelayMultiplierNormalized;
        ImGui.DragFloat(Lang.Get(settingHydrationLossDelayMultiplierNormalized) + $"##hydrationLossDelayMultiplierNormalized-{id}", ref hydrationLossDelayMultiplierNormalized, 0.1f, 0.0f, 10.0f);
        thirstConfig.HydrationLossDelayMultiplierNormalized = hydrationLossDelayMultiplierNormalized;

        //TODO rework config
        //float waterSatiety = satietyConfig.WaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingWaterSatiety) + $"##waterSatiety-{id}", ref waterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WaterSatiety = waterSatiety;
        //
        //float saltWaterSatiety = satietyConfig.SaltWaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingSaltWaterSatiety) + $"##saltWaterSatiety-{id}", ref saltWaterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.SaltWaterSatiety = saltWaterSatiety;
        //
        //float boilingWaterSatiety = satietyConfig.BoilingWaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingBoilingWaterSatiety) + $"##boilingWaterSatiety-{id}", ref boilingWaterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.BoilingWaterSatiety = boilingWaterSatiety;
        //
        //float rainWaterSatiety = satietyConfig.RainWaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingRainWaterSatiety) + $"##rainWaterSatiety-{id}", ref rainWaterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.RainWaterSatiety = rainWaterSatiety;
        //
        //float distilledWaterSatiety = satietyConfig.DistilledWaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingDistilledWaterSatiety) + $"##distilledWaterSatiety-{id}", ref distilledWaterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.DistilledWaterSatiety = distilledWaterSatiety;
        //
        //float boiledWaterSatiety = satietyConfig.BoiledWaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingBoiledWaterSatiety) + $"##boiledWaterSatiety-{id}", ref boiledWaterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.BoiledWaterSatiety = boiledWaterSatiety;
        //
        //float boiledRainWaterSatiety = satietyConfig.BoiledRainWaterSatiety;
        //ImGui.DragFloat(Lang.Get(settingBoiledRainWaterSatiety) + $"##boiledRainWaterSatiety-{id}", ref boiledRainWaterSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.BoiledRainWaterSatiety = boiledRainWaterSatiety;

        float sprintThirstMultiplier = thirstConfig.SprintThirstMultiplier;
        ImGui.DragFloat(Lang.Get(settingSprintThirstMultiplier) + $"##sprintThirstMultiplier-{id}", ref sprintThirstMultiplier, 0.1f, 0.0f, 5.0f);
        thirstConfig.SprintThirstMultiplier = sprintThirstMultiplier;

        float boilingWaterDamage = thirstConfig.BoilingWaterDamage;
        ImGui.DragFloat(Lang.Get(settingBoilingWaterDamage) + $"##boilingWaterDamage-{id}", ref boilingWaterDamage, 0.1f, 0.0f, 25.0f);
        thirstConfig.BoilingWaterDamage = boilingWaterDamage;


        // Movement Speed Penalty Settings
        ImGui.SeparatorText("Movement Speed Penalty Settings");

        float maxMovementSpeedPenalty = thirstConfig.MaxMovementSpeedPenalty;
        ImGui.DragFloat(Lang.Get(settingMaxMovementSpeedPenalty) + $"##maxMovementSpeedPenalty-{id}", ref maxMovementSpeedPenalty, 0.01f, 0.0f, 1.0f);
        thirstConfig.MaxMovementSpeedPenalty = maxMovementSpeedPenalty;

        float movementSpeedPenaltyThreshold = thirstConfig.MovementSpeedPenaltyThreshold;
        ImGui.DragFloat(Lang.Get(settingMovementSpeedPenaltyThreshold) + $"##movementSpeedPenaltyThreshold-{id}", ref movementSpeedPenaltyThreshold, 10.0f, 0.0f, 1500.0f);
        thirstConfig.MovementSpeedPenaltyThreshold = movementSpeedPenaltyThreshold;


        // Liquid Encumbrance Settings
        ImGui.SeparatorText("Liquid Encumbrance Settings");

        bool enableLiquidEncumbrance = liquidEncumbranceConfig.Enabled;
        ImGui.Checkbox(Lang.Get(settingEnableLiquidEncumbrance) + $"##enableLiquidEncumbrance-{id}", ref enableLiquidEncumbrance);
        liquidEncumbranceConfig.Enabled = enableLiquidEncumbrance;

        float encumbranceLimit = liquidEncumbranceConfig.EncumbranceLimit;
        ImGui.DragFloat(Lang.Get(settingEncumbranceLimit) + $"##encumbranceLimit-{id}", ref encumbranceLimit, 0.1f, 0.0f, 10.0f);
        liquidEncumbranceConfig.EncumbranceLimit = encumbranceLimit;

        float liquidEncumbranceMovementSpeedDebuff = liquidEncumbranceConfig.EncumbranceMovementSpeedDebuff;
        ImGui.DragFloat(Lang.Get(settingLiquidEncumbranceMovementSpeedDebuff) + $"##liquidEncumbranceMovementSpeedDebuff-{id}", ref liquidEncumbranceMovementSpeedDebuff, 0.01f, 0.0f, 1.0f);
        liquidEncumbranceConfig.EncumbranceMovementSpeedDebuff = liquidEncumbranceMovementSpeedDebuff;


        // Temperature and Heat Settings
        ImGui.SeparatorText("Temperature and Heat Settings");

        bool harshHeat = heatAndCoolingConfig.HarshHeat;
        ImGui.Checkbox(Lang.Get(settingHarshHeat) + $"##harshHeat-{id}", ref harshHeat);
        heatAndCoolingConfig.HarshHeat = harshHeat;

        float temperatureThreshold = heatAndCoolingConfig.TemperatureThreshold;
        ImGui.DragFloat(Lang.Get(settingTemperatureThreshold) + $"##temperatureThreshold-{id}", ref temperatureThreshold, 1.0f, 0.0f, 50.0f);
        heatAndCoolingConfig.TemperatureThreshold = temperatureThreshold;

        float thirstIncreasePerDegreeMultiplier = heatAndCoolingConfig.ThirstIncreasePerDegreeMultiplier;
        ImGui.DragFloat(Lang.Get(settingThirstIncreasePerDegreeMultiplier) + $"##thirstIncreasePerDegreeMultiplier-{id}", ref thirstIncreasePerDegreeMultiplier, 0.1f, 0.0f, 10.0f);
        heatAndCoolingConfig.ThirstIncreasePerDegreeMultiplier = thirstIncreasePerDegreeMultiplier;

        float harshHeatExponentialGainMultiplier = heatAndCoolingConfig.HarshHeatExponentialGainMultiplier;
        ImGui.DragFloat(Lang.Get(settingHarshHeatExponentialGainMultiplier) + $"##harshHeatExponentialGainMultiplier-{id}", ref harshHeatExponentialGainMultiplier, 0.01f, 0.0f, 1.0f);
        heatAndCoolingConfig.HarshHeatExponentialGainMultiplier = harshHeatExponentialGainMultiplier;


        // Cooling Factors
        ImGui.SeparatorText("Cooling Factors");

        float unequippedSlotCooling = heatAndCoolingConfig.UnequippedSlotCooling;
        ImGui.DragFloat(Lang.Get(settingUnequippedSlotCooling) + $"##unequippedSlotCooling-{id}", ref unequippedSlotCooling, 0.1f, 0.0f, 5.0f);
        heatAndCoolingConfig.UnequippedSlotCooling = unequippedSlotCooling;

        float wetnessCoolingFactor = heatAndCoolingConfig.WetnessCoolingFactor;
        ImGui.DragFloat(Lang.Get(settingWetnessCoolingFactor) + $"##wetnessCoolingFactor-{id}", ref wetnessCoolingFactor, 0.1f, 0.0f, 5.0f);
        heatAndCoolingConfig.WetnessCoolingFactor = wetnessCoolingFactor;

        float shelterCoolingFactor = heatAndCoolingConfig.ShelterCoolingFactor;
        ImGui.DragFloat(Lang.Get(settingShelterCoolingFactor) + $"##shelterCoolingFactor-{id}", ref shelterCoolingFactor, 0.1f, 0.0f, 5.0f);
        heatAndCoolingConfig.ShelterCoolingFactor = shelterCoolingFactor;

        float sunlightCoolingFactor = heatAndCoolingConfig.SunlightCoolingFactor;
        ImGui.DragFloat(Lang.Get(settingSunlightCoolingFactor) + $"##sunlightCoolingFactor-{id}", ref sunlightCoolingFactor, 0.1f, 0.0f, 5.0f);
        heatAndCoolingConfig.SunlightCoolingFactor = sunlightCoolingFactor;

        float diurnalVariationAmplitude = heatAndCoolingConfig.DiurnalVariationAmplitude;
        ImGui.DragFloat(Lang.Get(settingDiurnalVariationAmplitude) + $"##diurnalVariationAmplitude-{id}", ref diurnalVariationAmplitude, 1.0f, 0.0f, 50.0f);
        heatAndCoolingConfig.DiurnalVariationAmplitude = diurnalVariationAmplitude;

        float refrigerationCooling = heatAndCoolingConfig.RefrigerationCooling;
        ImGui.DragFloat(Lang.Get(settingRefrigerationCooling) + $"##refrigerationCooling-{id}", ref refrigerationCooling, 0.1f, 0.0f, 50.0f);
        heatAndCoolingConfig.RefrigerationCooling = refrigerationCooling;


        // XLib Skills Settings
        ImGui.SeparatorText("XLib Skills Settings");

        float dromedaryMultiplierPerLevel = xlibConfig.DromedaryMultiplierPerLevel;
        ImGui.DragFloat(Lang.Get(settingDromedaryMultiplierPerLevel) + $"##dromedaryMultiplierPerLevel-{id}", ref dromedaryMultiplierPerLevel, 0.01f, 0.1f, 3.0f);
        xlibConfig.DromedaryMultiplierPerLevel = dromedaryMultiplierPerLevel;

        ImGui.TextWrapped(Lang.Get(settingEquatidianCoolingMultipliers));
        float equatidianLevel1 = xlibConfig.EquatidianCoolingMultipliers[0];
        ImGui.DragFloat($"Level 1##equatidianLevel1-{id}", ref equatidianLevel1, 0.01f, 1.0f, 5.0f);
        xlibConfig.EquatidianCoolingMultipliers[0] = equatidianLevel1;

        float equatidianLevel2 = xlibConfig.EquatidianCoolingMultipliers[1];
        ImGui.DragFloat($"Level 2##equatidianLevel2-{id}", ref equatidianLevel2, 0.01f, 1.0f, 5.0f);
        xlibConfig.EquatidianCoolingMultipliers[1] = equatidianLevel2;

        float equatidianLevel3 = xlibConfig.EquatidianCoolingMultipliers[2];
        ImGui.DragFloat($"Level 3##equatidianLevel3-{id}", ref equatidianLevel3, 0.01f, 1.0f, 5.0f);
        xlibConfig.EquatidianCoolingMultipliers[2] = equatidianLevel3;


        // Rain Gathering Settings
        ImGui.SeparatorText("Rain Gathering Settings");
        
        bool enableRainGathering = rainConfig.EnableRainGathering;
        ImGui.Checkbox(Lang.Get(settingEnableRainGathering) + $"##enableRainGathering-{id}", ref enableRainGathering);
        rainConfig.EnableRainGathering = enableRainGathering;

        float rainMultiplier = rainConfig.RainMultiplier;
        ImGui.DragFloat(Lang.Get(settingRainMultiplier) + $"##rainMultiplier-{id}", ref rainMultiplier, 0.1f, 0.1f, 10.0f);
        rainConfig.RainMultiplier = rainMultiplier;
        
        bool enableParticleTicking = rainConfig.EnableParticleTicking;
        ImGui.Checkbox(Lang.Get(settingEnableParticleTicking) + $"##enableParticleTicking-{id}", ref enableParticleTicking);
        rainConfig.EnableParticleTicking = enableParticleTicking;
        
        // Keg Settings
        ImGui.SeparatorText("Keg Settings");

        float kegCapacityLitres = containersConfig.KegCapacityLitres;
        ImGui.DragFloat(Lang.Get(settingKegCapacityLitres) + $"##kegCapacityLitres-{id}", ref kegCapacityLitres, 1.0f, 10.0f, 500.0f);
        containersConfig.KegCapacityLitres = kegCapacityLitres;

        float spoilRateUntapped = containersConfig.SpoilRateUntapped;
        ImGui.DragFloat(Lang.Get(settingSpoilRateUntapped) + $"##spoilRateUntapped-{id}", ref spoilRateUntapped, 0.01f, 0.1f, 1.0f);
        containersConfig.SpoilRateUntapped = spoilRateUntapped;

        float spoilRateTapped = containersConfig.SpoilRateTapped;
        ImGui.DragFloat(Lang.Get(settingSpoilRateTapped) + $"##spoilRateTapped-{id}", ref spoilRateTapped, 0.01f, 0.1f, 1.0f);
        containersConfig.SpoilRateTapped = spoilRateTapped;

        float kegIronHoopDropChance = containersConfig.KegIronHoopDropChance;
        ImGui.DragFloat(Lang.Get(settingKegIronHoopDropChance) + $"##kegIronHoopDropChance-{id}", ref kegIronHoopDropChance, 0.01f, 0.0f, 1.0f);
        containersConfig.KegIronHoopDropChance = kegIronHoopDropChance;

        float kegTapDropChance = containersConfig.KegTapDropChance;
        ImGui.DragFloat(Lang.Get(settingKegTapDropChance) + $"##kegTapDropChance-{id}", ref kegTapDropChance, 0.01f, 0.0f, 1.0f);
        containersConfig.KegTapDropChance = kegTapDropChance;


        // Tun Settings
        ImGui.SeparatorText("Tun Settings");

        float tunCapacityLitres = containersConfig.TunCapacityLitres;
        ImGui.DragFloat(Lang.Get(settingTunCapacityLitres) + $"##tunCapacityLitres-{id}", ref tunCapacityLitres, 1.0f, 10.0f, 2000.0f);
        containersConfig.TunCapacityLitres = tunCapacityLitres;

        float tunSpoilRateMultiplier = containersConfig.TunSpoilRateMultiplier;
        ImGui.DragFloat(Lang.Get(settingTunSpoilRateMultiplier) + $"##tunSpoilRateMultiplier-{id}", ref tunSpoilRateMultiplier, 0.01f, 0.1f, 5.0f);
        containersConfig.TunSpoilRateMultiplier = tunSpoilRateMultiplier;
        
        // Misc Settings
        ImGui.SeparatorText("Misc Settings");
        
        bool disableDrunkSway = config.DisableDrunkSway;
        ImGui.Checkbox(Lang.Get(settingDisableDrunkSway) + $"##disableDrunkSway-{id}", ref disableDrunkSway);
        config.DisableDrunkSway = disableDrunkSway;
        
        // Well Settings
        ImGui.SeparatorText("Well Settings");

        // WellSpringOutputMultiplier
        float wellSpringOutputMultiplier = groundWaterConfig.WellSpringOutputMultiplier;
        ImGui.DragFloat(Lang.Get(settingWellSpringOutputMultiplier) + $"##wellSpringOutputMultiplier-{id}", ref wellSpringOutputMultiplier, 0.1f, 0.1f, 10.0f);
        groundWaterConfig.WellSpringOutputMultiplier = wellSpringOutputMultiplier;
        
        // RandomMultiplierChance
        float aquiferRandomMultiplierChance = (float)groundWaterConfig.AquiferRandomMultiplierChance;
        ImGui.DragFloat(Lang.Get(settingAquiferRandomMultiplierChance) + $"##aquiferRandomMultiplierChance-{id}", ref aquiferRandomMultiplierChance, 0.001f, 0.0f, 1.0f);
        groundWaterConfig.AquiferRandomMultiplierChance = aquiferRandomMultiplierChance;

        // Wellwater Depth Max Base
        int wellwaterDepthMaxBase = groundWaterConfig.WellwaterDepthMaxBase;
        ImGui.DragInt(Lang.Get(settingWellwaterDepthMaxBase) + $"##wellwaterDepthMaxBase-{id}", ref wellwaterDepthMaxBase, 1, 1, 20);
        groundWaterConfig.WellwaterDepthMaxBase = wellwaterDepthMaxBase;

        // Wellwater Depth Max Clay
        int wellwaterDepthMaxClay = groundWaterConfig.WellwaterDepthMaxClay;
        ImGui.DragInt(Lang.Get(settingWellwaterDepthMaxClay) + $"##wellwaterDepthMaxClay-{id}", ref wellwaterDepthMaxClay, 1, 1, 20);
        groundWaterConfig.WellwaterDepthMaxClay = wellwaterDepthMaxClay;

        // Wellwater Depth Max Stone
        int wellwaterDepthMaxStone = groundWaterConfig.WellwaterDepthMaxStone;
        ImGui.DragInt(Lang.Get(settingWellwaterDepthMaxStone) + $"##wellwaterDepthMaxStone-{id}", ref wellwaterDepthMaxStone, 1, 1, 20);
        groundWaterConfig.WellwaterDepthMaxStone = wellwaterDepthMaxStone;
        
        // WaterBlockMultiplier
        float aquiferWaterBlockMultiplier = (float)groundWaterConfig.AquiferWaterBlockMultiplier;
        ImGui.DragFloat(Lang.Get(settingAquiferWaterBlockMultiplier) + $"##aquiferWaterBlockMultiplier-{id}", ref aquiferWaterBlockMultiplier, 0.1f, 0.1f, 10.0f);
        groundWaterConfig.AquiferWaterBlockMultiplier = aquiferWaterBlockMultiplier;

        // SaltWaterMultiplier
        float aquiferSaltWaterMultiplier = (float)groundWaterConfig.AquiferSaltWaterMultiplier;
        ImGui.DragFloat(Lang.Get(settingAquiferSaltWaterMultiplier) + $"##aquiferSaltWaterMultiplier-{id}", ref aquiferSaltWaterMultiplier, 0.1f, 0.1f, 10.0f);
        groundWaterConfig.AquiferSaltWaterMultiplier = aquiferSaltWaterMultiplier;

        // BoilingWaterMultiplier
        int aquiferBoilingWaterMultiplier = groundWaterConfig.AquiferBoilingWaterMultiplier;
        ImGui.DragInt(Lang.Get(settingAquiferBoilingWaterMultiplier) + $"##aquiferBoilingWaterMultiplier-{id}", ref aquiferBoilingWaterMultiplier, 1, 1, 500);
        groundWaterConfig.AquiferBoilingWaterMultiplier = aquiferBoilingWaterMultiplier;
        
        //float wellWaterFreshSatiety = satietyConfig.WellWaterFreshSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterFreshSatiety) + $"##wellWaterFreshSatiety-{id}", ref wellWaterFreshSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterFreshSatiety = wellWaterFreshSatiety;
        //
        //float wellWaterSaltSatiety = satietyConfig.WellWaterSaltSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterSaltSatiety) + $"##wellWaterSaltSatiety-{id}", ref wellWaterSaltSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterSaltSatiety = wellWaterSaltSatiety;
        //
        //float wellWaterMuddySatiety = satietyConfig.WellWaterMuddySatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterMuddySatiety) + $"##wellWaterMuddySatiety-{id}", ref wellWaterMuddySatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterMuddySatiety = wellWaterMuddySatiety;
        //
        //float wellWaterTaintedSatiety = satietyConfig.WellWaterTaintedSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSatiety) + $"##wellWaterTaintedSatiety-{id}", ref wellWaterTaintedSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterTaintedSatiety = wellWaterTaintedSatiety;
        //
        //float wellWaterPoisonedSatiety = satietyConfig.WellWaterPoisonedSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSatiety) + $"##wellWaterPoisonedSatiety-{id}", ref wellWaterPoisonedSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterPoisonedSatiety = wellWaterPoisonedSatiety;
        //
        //float wellWaterMuddySaltSatiety = satietyConfig.WellWaterMuddySaltSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterMuddySaltSatiety) + $"##wellWaterMuddySaltSatiety-{id}", ref wellWaterMuddySaltSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterMuddySaltSatiety = wellWaterMuddySaltSatiety;
        //
        //float wellWaterTaintedSaltSatiety = satietyConfig.WellWaterTaintedSaltSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSaltSatiety) + $"##wellWaterTaintedSaltSatiety-{id}", ref wellWaterTaintedSaltSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterTaintedSaltSatiety = wellWaterTaintedSaltSatiety;
        //
        //float wellWaterPoisonedSaltSatiety = satietyConfig.WellWaterPoisonedSaltSatiety;
        //ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSaltSatiety) + $"##wellWaterPoisonedSaltSatiety-{id}", ref wellWaterPoisonedSaltSatiety, 1.0f, -1000.0f, 1000.0f);
        //satietyConfig.WellWaterPoisonedSaltSatiety = wellWaterPoisonedSaltSatiety;
        
        int prospectingRadius = groundWaterConfig.ProspectingRadius;
        ImGui.DragInt(Lang.Get(settingProspectingRadius) + $"##prospectingRadius-{id}", ref prospectingRadius, 1, 1, 10);
        groundWaterConfig.ProspectingRadius = prospectingRadius;
        
        ImGui.SeparatorText("Liquid Perish Rates");

        float rainWaterFreshHours = perishRateConfig.RainWaterFreshHours;
        ImGui.DragFloat(Lang.Get(settingRainWaterFreshHours) + $"##rainWaterFreshHours-{id}", ref rainWaterFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.RainWaterFreshHours = rainWaterFreshHours;

        float rainWaterTransitionHours = perishRateConfig.RainWaterTransitionHours;
        ImGui.DragFloat(Lang.Get(settingRainWaterTransitionHours) + $"##rainWaterTransitionHours-{id}", ref rainWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.RainWaterTransitionHours = rainWaterTransitionHours;

        float boiledWaterFreshHours = perishRateConfig.BoiledWaterFreshHours;
        ImGui.DragFloat(Lang.Get(settingBoiledWaterFreshHours) + $"##boiledWaterFreshHours-{id}", ref boiledWaterFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.BoiledWaterFreshHours = boiledWaterFreshHours;

        float boiledWaterTransitionHours = perishRateConfig.BoiledWaterTransitionHours;
        ImGui.DragFloat(Lang.Get(settingBoiledWaterTransitionHours) + $"##boiledWaterTransitionHours-{id}", ref boiledWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.BoiledWaterTransitionHours = boiledWaterTransitionHours;

        float boiledRainWaterFreshHours = perishRateConfig.BoiledRainWaterFreshHours;
        ImGui.DragFloat(Lang.Get(settingBoiledRainWaterFreshHours) + $"##boiledRainWaterFreshHours-{id}", ref boiledRainWaterFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.BoiledRainWaterFreshHours = boiledRainWaterFreshHours;

        float boiledRainWaterTransitionHours = perishRateConfig.BoiledRainWaterTransitionHours;
        ImGui.DragFloat(Lang.Get(settingBoiledRainWaterTransitionHours) + $"##boiledRainWaterTransitionHours-{id}", ref boiledRainWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.BoiledRainWaterTransitionHours = boiledRainWaterTransitionHours;

        float distilledWaterFreshHours = perishRateConfig.DistilledWaterFreshHours;
        ImGui.DragFloat(Lang.Get(settingDistilledWaterFreshHours) + $"##distilledWaterFreshHours-{id}", ref distilledWaterFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.DistilledWaterFreshHours = distilledWaterFreshHours;

        float distilledWaterTransitionHours = perishRateConfig.DistilledWaterTransitionHours;
        ImGui.DragFloat(Lang.Get(settingDistilledWaterTransitionHours) + $"##distilledWaterTransitionHours-{id}", ref distilledWaterTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.DistilledWaterTransitionHours = distilledWaterTransitionHours;

        float wellWaterFreshFreshHours = perishRateConfig.WellWaterFreshFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterFreshFreshHours) + $"##wellWaterFreshFreshHours-{id}", ref wellWaterFreshFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterFreshFreshHours = wellWaterFreshFreshHours;

        float wellWaterFreshTransitionHours = perishRateConfig.WellWaterFreshTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterFreshTransitionHours) + $"##wellWaterFreshTransitionHours-{id}", ref wellWaterFreshTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterFreshTransitionHours = wellWaterFreshTransitionHours;

        float wellWaterSaltFreshHours = perishRateConfig.WellWaterSaltFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterSaltFreshHours) + $"##wellWaterSaltFreshHours-{id}", ref wellWaterSaltFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterSaltFreshHours = wellWaterSaltFreshHours;

        float wellWaterSaltTransitionHours = perishRateConfig.WellWaterSaltTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterSaltTransitionHours) + $"##wellWaterSaltTransitionHours-{id}", ref wellWaterSaltTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterSaltTransitionHours = wellWaterSaltTransitionHours;

        float wellWaterMuddyFreshHours = perishRateConfig.WellWaterMuddyFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterMuddyFreshHours) + $"##wellWaterMuddyFreshHours-{id}", ref wellWaterMuddyFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterMuddyFreshHours = wellWaterMuddyFreshHours;

        float wellWaterMuddyTransitionHours = perishRateConfig.WellWaterMuddyTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterMuddyTransitionHours) + $"##wellWaterMuddyTransitionHours-{id}", ref wellWaterMuddyTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterMuddyTransitionHours = wellWaterMuddyTransitionHours;

        float wellWaterTaintedFreshHours = perishRateConfig.WellWaterTaintedFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterTaintedFreshHours) + $"##wellWaterTaintedFreshHours-{id}", ref wellWaterTaintedFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterTaintedFreshHours = wellWaterTaintedFreshHours;

        float wellWaterTaintedTransitionHours = perishRateConfig.WellWaterTaintedTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterTaintedTransitionHours) + $"##wellWaterTaintedTransitionHours-{id}", ref wellWaterTaintedTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterTaintedTransitionHours = wellWaterTaintedTransitionHours;

        float wellWaterPoisonedFreshHours = perishRateConfig.WellWaterPoisonedFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedFreshHours) + $"##wellWaterPoisonedFreshHours-{id}", ref wellWaterPoisonedFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterPoisonedFreshHours = wellWaterPoisonedFreshHours;

        float wellWaterPoisonedTransitionHours = perishRateConfig.WellWaterPoisonedTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedTransitionHours) + $"##wellWaterPoisonedTransitionHours-{id}", ref wellWaterPoisonedTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterPoisonedTransitionHours = wellWaterPoisonedTransitionHours;

        float wellWaterMuddySaltFreshHours = perishRateConfig.WellWaterMuddySaltFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterMuddySaltFreshHours) + $"##wellWaterMuddySaltFreshHours-{id}", ref wellWaterMuddySaltFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterMuddySaltFreshHours = wellWaterMuddySaltFreshHours;

        float wellWaterMuddySaltTransitionHours = perishRateConfig.WellWaterMuddySaltTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterMuddySaltTransitionHours) + $"##wellWaterMuddySaltTransitionHours-{id}", ref wellWaterMuddySaltTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterMuddySaltTransitionHours = wellWaterMuddySaltTransitionHours;

        float wellWaterTaintedSaltFreshHours = perishRateConfig.WellWaterTaintedSaltFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSaltFreshHours) + $"##wellWaterTaintedSaltFreshHours-{id}", ref wellWaterTaintedSaltFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterTaintedSaltFreshHours = wellWaterTaintedSaltFreshHours;

        float wellWaterTaintedSaltTransitionHours = perishRateConfig.WellWaterTaintedSaltTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterTaintedSaltTransitionHours) + $"##wellWaterTaintedSaltTransitionHours-{id}", ref wellWaterTaintedSaltTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterTaintedSaltTransitionHours = wellWaterTaintedSaltTransitionHours;

        float wellWaterPoisonedSaltFreshHours = perishRateConfig.WellWaterPoisonedSaltFreshHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSaltFreshHours) + $"##wellWaterPoisonedSaltFreshHours-{id}", ref wellWaterPoisonedSaltFreshHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterPoisonedSaltFreshHours = wellWaterPoisonedSaltFreshHours;

        float wellWaterPoisonedSaltTransitionHours = perishRateConfig.WellWaterPoisonedSaltTransitionHours;
        ImGui.DragFloat(Lang.Get(settingWellWaterPoisonedSaltTransitionHours) + $"##wellWaterPoisonedSaltTransitionHours-{id}", ref wellWaterPoisonedSaltTransitionHours, 1.0f, 0.0f, 1000.0f);
        perishRateConfig.WellWaterPoisonedSaltTransitionHours = wellWaterPoisonedSaltTransitionHours;
        
        ImGui.SeparatorText("Winch Speed");
        float winchLowerSpeed = groundWaterConfig.WinchLowerSpeed;
        ImGui.DragFloat(Lang.Get(settingWinchLowerSpeed) + $"##winchLowerSpeed-{id}", ref winchLowerSpeed, 0.01f, 0.0f, 5.0f);
        groundWaterConfig.WinchLowerSpeed = winchLowerSpeed;

        float winchRaiseSpeed = groundWaterConfig.WinchRaiseSpeed;
        ImGui.DragFloat(Lang.Get(settingWinchRaiseSpeed) + $"##winchRaiseSpeed-{id}", ref winchRaiseSpeed, 0.01f, 0.0f, 5.0f);
        groundWaterConfig.WinchRaiseSpeed = winchRaiseSpeed;
        
        bool kegDropWithLiquid = containersConfig.KegDropWithLiquid;
        ImGui.Checkbox(Lang.Get(settingKegDropWithLiquid) + $"##kegDropWithLiquid-{id}", ref kegDropWithLiquid);
        containersConfig.KegDropWithLiquid = kegDropWithLiquid;
        
        bool tunDropWithLiquid = containersConfig.TunDropWithLiquid;
        ImGui.Checkbox(Lang.Get(settingTunDropWithLiquid) + $"##tunDropWithLiquid-{id}", ref tunDropWithLiquid);
        containersConfig.TunDropWithLiquid = tunDropWithLiquid;
        
        bool sprintToDrink = config.SprintToDrink;
        ImGui.Checkbox(Lang.Get(settingSprintToDrink) + $"##sprintToDrink-{id}", ref sprintToDrink);
        config.SprintToDrink = sprintToDrink;
        
        int aquiferRatingCeilingAboveSeaLevel = groundWaterConfig.AquiferRatingCeilingAboveSeaLevel;
        ImGui.DragInt(Lang.Get(settingAquiferRatingCeilingAboveSeaLevel) + $"##aquiferRatingCeilingAboveSeaLevel-{id}", ref aquiferRatingCeilingAboveSeaLevel, 1, 0, 100);
        groundWaterConfig.AquiferRatingCeilingAboveSeaLevel = aquiferRatingCeilingAboveSeaLevel;

        float aquiferDepthMultiplierScale = groundWaterConfig.AquiferDepthMultiplierScale;
        ImGui.DragFloat(Lang.Get(settingAquiferDepthMultiplierScale) + $"##aquiferDepthMultiplierScale-{id}", ref aquiferDepthMultiplierScale, 0.01f, 0.5f, 5.0f);
        groundWaterConfig.AquiferDepthMultiplierScale = aquiferDepthMultiplierScale;
        
        int aquiferMinimumWaterBlockThreshold = groundWaterConfig.AquiferMinimumWaterBlockThreshold;
        ImGui.DragInt(Lang.Get(settingAquiferMinimumWaterBlockThreshold) + $"##aquiferMinimumWaterBlockThreshold-{id}", ref aquiferMinimumWaterBlockThreshold, 1, 0, 100);
        groundWaterConfig.AquiferMinimumWaterBlockThreshold = aquiferMinimumWaterBlockThreshold;
        
        bool waterPerish = perishRateConfig.Enabled;
        ImGui.Checkbox(Lang.Get(settingWaterPerish) + $"##waterPerish-{id}", ref waterPerish);
        perishRateConfig.Enabled = waterPerish;
        
        bool aquiferDataOnProspectingNodeMode = groundWaterConfig.AquiferDataOnProspectingNodeMode;
        ImGui.Checkbox(Lang.Get(settingAquiferDataOnProspectingNodeMode) + $"##aquiferDataOnProspectingNodeMode-{id}", ref aquiferDataOnProspectingNodeMode);
        groundWaterConfig.AquiferDataOnProspectingNodeMode = aquiferDataOnProspectingNodeMode;
        
        bool showAquiferProspectingDataOnMap = groundWaterConfig.ShowAquiferProspectingDataOnMap;
        ImGui.Checkbox(Lang.Get(settingShowAquiferProspectingDataOnMap) + $"##showAquiferProspectingDataOnMap-{id}", ref showAquiferProspectingDataOnMap);
        groundWaterConfig.ShowAquiferProspectingDataOnMap = showAquiferProspectingDataOnMap;
            
        bool winchOutputInfo = groundWaterConfig.WinchOutputInfo;
        ImGui.Checkbox(Lang.Get(settingWinchOutputInfo) + $"##winchOutputInfo-{id}", ref winchOutputInfo);
        groundWaterConfig.WinchOutputInfo = winchOutputInfo;
    }
}
