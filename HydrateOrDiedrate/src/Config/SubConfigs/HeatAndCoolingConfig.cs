using System.ComponentModel;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class HeatAndCoolingConfig
{
    /// <summary>
    /// Wether Harsh Heat mechanics are enabled.
    /// (accelerated dehydration in high‑temperature locations)
    /// </summary>
    [DefaultValue(true)]
    public bool HarshHeat { get; set; } = true;

    /// <summary>
    /// Ambient temperature above which thirst decay starts ramping up
    /// </summary>
    [DefaultValue(25.0d)]
    public float TemperatureThreshold { get; set; } = 25.0f;

    /// <summary>
    /// Additional thirst units per degree above the threshold each second
    /// </summary>
    [DefaultValue(5.0d)]
    public float ThirstIncreasePerDegreeMultiplier { get; set; } = 5f;

    /// <summary>
    /// Exponential scaling factor for thirst gain in extreme heat
    /// </summary>
    [DefaultValue(0.25d)]
    public float HarshHeatExponentialGainMultiplier { get; set; } = 0.25f;

    /// <summary>
    /// Cooling bonus for each empty armor/clothing slot
    /// </summary>
    [DefaultValue(1.0d)]
    public float UnequippedSlotCooling { get; set; } = 1.0f;

    /// <summary>
    /// Cooling effect of being wet
    /// </summary>
    [DefaultValue(1.5d)]
    public float WetnessCoolingBonus { get; set; } = 8f;

    /// <summary>
    /// Cooling bonus when in a room
    /// </summary>
    [DefaultValue(1.5d)]
    public float RoomCoolingBonus { get; set; } = 8f;
    
    /// <summary>
    /// How much cooling being in shade provides
    /// </summary>
    [DefaultValue(6d)]
    public float ShadeCoolingBonus { get; set; } = 8f;
    
    /// <summary>
    /// How many degrees of effective temperature are reduced per 1 point of Cooling.
    /// This directly shifts the temperature used by the harsh-heat equation,
    /// allowing cooling to matter across the entire heat range.
    /// </summary>
    [DefaultValue(0.5d)]
    public float CoolingTempOffsetPerPoint { get; set; } = 0.5f;
    
    /// <summary>
    /// Additional thirst multiplier per point of negative cooling (cooling < 0).
    /// For example, 0.1 means each point of negative cooling increases thirst rate by +10%.
    /// </summary>
    [DefaultValue(0.1d)]
    public float NegativeCoolingThirstLinearPerPoint { get; set; } = 0.1f;

    /// <summary>
    /// Maximum allowed multiplier applied from negative cooling.
    /// Prevents extreme cooling values from causing runaway thirst rates.
    /// </summary>
    [DefaultValue(3.0d)]
    public float NegativeCoolingThirstMaxMultiplier { get; set; } = 5.0f;
    
    /// <summary>
    /// Sunlight level (0-22). Below this, we start giving a cooling bonus.
    /// </summary>
    [DefaultValue(16)]
    public int LowSunlightThreshold { get; set; } = 16;

    /// <summary>
    /// Max extra cooling bonus when sunlight level is 0.
    /// This is a flat additive cooling value (like gear cooling).
    /// </summary>
    [DefaultValue(8.0d)]
    public float LowSunlightCoolingBonus { get; set; } = 8f;
}
