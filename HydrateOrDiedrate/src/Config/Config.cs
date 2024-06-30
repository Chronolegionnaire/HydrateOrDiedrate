using HydrateOrDiedrate.Configuration;
using Vintagestory.API.Common;

public class Config : IModConfig
{
    // Thirst Settings
    public float MaxThirst { get; set; } = 1500.0f;
    public float ThirstDamage { get; set; } = 1f;
    public float ThirstDecayRate { get; set; } = 10f;
    public float ThirstIncreasePerDegreeMultiplier { get; set; } = 5f;
    public float ThirstDecayRateMax { get; set; } = 5.0f;
    public float HydrationLossDelayMultiplier { get; set; } = 0.05f;
    public bool EnableThirstMechanics { get; set; } = true;

    // Movement Speed Penalty Settings
    public float MaxMovementSpeedPenalty { get; set; } = 0.3f;
    public float MovementSpeedPenaltyThreshold { get; set; } = 600.0f;

    // Temperature and Heat Settings
    public bool HarshHeat { get; set; } = true;
    public float TemperatureThreshold { get; set; } = 27.0f;
    public float HarshHeatExponentialGainMultiplier { get; set; } = 0.2f;
    public float BoilingWaterDamage { get; set; } = 5.0f;
    public bool EnableBoilingWaterDamage { get; set; } = true;

    // Cooling Factors
    public float UnequippedSlotCooling { get; set; } = 1.0f;
    public float WetnessCoolingFactor { get; set; } = 1.5f;
    public float ShelterCoolingFactor { get; set; } = 1.5f;
    public float SunlightCoolingFactor { get; set; } = 1.0f;
    public float DiurnalVariationAmplitude { get; set; } = 18f;
    public float RefrigerationCooling { get; set; } = 20.0f;

    // Other Settings
    public float SprintThirstMultiplier { get; set; } = 1.5f;
    public bool EnableLiquidEncumbrance { get; set; } = true;
    public float EncumbranceLimit { get; set; } = 4.0f;
    public float LiquidEncumbranceMovementSpeedDebuff { get; set; } = 0.4f;

    public Config() { }

    public Config(ICoreAPI api, Config previousConfig = null)
    {
        // Thirst Settings
        MaxThirst = previousConfig?.MaxThirst ?? 1500.0f;
        ThirstDamage = previousConfig?.ThirstDamage ?? 1f;
        ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 10f;
        ThirstIncreasePerDegreeMultiplier = previousConfig?.ThirstIncreasePerDegreeMultiplier ?? 5f;
        ThirstDecayRateMax = previousConfig?.ThirstDecayRateMax ?? 5.0f;
        HydrationLossDelayMultiplier = previousConfig?.HydrationLossDelayMultiplier ?? 0.05f;
        EnableThirstMechanics = previousConfig?.EnableThirstMechanics ?? true;

        // Movement Speed Penalty Settings
        MaxMovementSpeedPenalty = previousConfig?.MaxMovementSpeedPenalty ?? 0.3f;
        MovementSpeedPenaltyThreshold = previousConfig?.MovementSpeedPenaltyThreshold ?? 600.0f;

        // Temperature and Heat Settings
        HarshHeat = previousConfig?.HarshHeat ?? true;
        TemperatureThreshold = previousConfig?.TemperatureThreshold ?? 27.0f;
        HarshHeatExponentialGainMultiplier = previousConfig?.HarshHeatExponentialGainMultiplier ?? 0.2f;
        BoilingWaterDamage = previousConfig?.BoilingWaterDamage ?? 5.0f;
        EnableBoilingWaterDamage = previousConfig?.EnableBoilingWaterDamage ?? true;

        // Cooling Factors
        UnequippedSlotCooling = previousConfig?.UnequippedSlotCooling ?? 1.0f;
        WetnessCoolingFactor = previousConfig?.WetnessCoolingFactor ?? 1.5f;
        ShelterCoolingFactor = previousConfig?.ShelterCoolingFactor ?? 1.5f;
        SunlightCoolingFactor = previousConfig?.SunlightCoolingFactor ?? 1.0f;
        DiurnalVariationAmplitude = previousConfig?.DiurnalVariationAmplitude ?? 18f;
        RefrigerationCooling = previousConfig?.RefrigerationCooling ?? 20.0f;

        // Other Settings
        SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.5f;
        EnableLiquidEncumbrance = previousConfig?.EnableLiquidEncumbrance ?? true;
        EncumbranceLimit = previousConfig?.EncumbranceLimit ?? 4.0f;
        LiquidEncumbranceMovementSpeedDebuff = previousConfig?.LiquidEncumbranceMovementSpeedDebuff ?? 0.4f;
    }
}
