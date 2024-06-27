using HydrateOrDiedrate.Configuration;
using Vintagestory.API.Common;

public class Config : IModConfig
{
    public float MaxThirst { get; set; } = 1500.0f;
    public float ThirstDamage { get; set; } = 1f;
    public float MaxMovementSpeedPenalty { get; set; } = 0.3f; 
    public float ThirstDecayRate { get; set; } = 10f;
    public float SprintThirstMultiplier { get; set; } = 1.5f;
    public float MovementSpeedPenaltyThreshold { get; set; } = 600.0f;
    public float BoilingWaterDamage { get; set; } = 5.0f;
    public bool EnableBoilingWaterDamage { get; set; } = true;
    public float TemperatureThreshold { get; set; } = 27.0f; 
    public float ThirstIncreasePerDegreeMultiplier { get; set; } = 5f;
    public bool HarshHeat { get; set; } = true;
    public float HydrationLossDelayMultiplier { get; set; } = 0.05f;
    public bool EnableLiquidEncumbrance { get; set; } = true;
    public float EncumbranceLimit { get; set; } = 4.0f; 
    public float LiquidEncumbranceMovementSpeedDebuff { get; set; } = 0.4f;
    public bool EnableThirstMechanics { get; set; } = true;
    public float ThirstDecayRateMax { get; set; } = 5.0f;

    public Config() { }

    public Config(ICoreAPI api, Config previousConfig = null)
    {
        MaxThirst = previousConfig?.MaxThirst ?? 1500.0f;
        ThirstDamage = previousConfig?.ThirstDamage ?? 1f;
        MaxMovementSpeedPenalty = previousConfig?.MaxMovementSpeedPenalty ?? 0.3f;
        ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 10f;
        SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.5f;
        MovementSpeedPenaltyThreshold = previousConfig?.MovementSpeedPenaltyThreshold ?? 600.0f;
        BoilingWaterDamage = previousConfig?.BoilingWaterDamage ?? 5.0f;
        EnableBoilingWaterDamage = previousConfig?.EnableBoilingWaterDamage ?? true;
        TemperatureThreshold = previousConfig?.TemperatureThreshold ?? 27.0f;
        ThirstIncreasePerDegreeMultiplier = previousConfig?.ThirstIncreasePerDegreeMultiplier ?? 5f;
        HarshHeat = previousConfig?.HarshHeat ?? true;
        HydrationLossDelayMultiplier = previousConfig?.HydrationLossDelayMultiplier ?? 0.05f;
        EnableLiquidEncumbrance = previousConfig?.EnableLiquidEncumbrance ?? true;
        EncumbranceLimit = previousConfig?.EncumbranceLimit ?? 4.0f;
        LiquidEncumbranceMovementSpeedDebuff = previousConfig?.LiquidEncumbranceMovementSpeedDebuff ?? 0.4f;
        EnableThirstMechanics = previousConfig?.EnableThirstMechanics ?? true;
        ThirstDecayRateMax = previousConfig?.ThirstDecayRateMax ?? 5.0f;
    }
}
