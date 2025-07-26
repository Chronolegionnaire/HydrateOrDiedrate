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
    [DefaultValue(27.0d)]
    public float TemperatureThreshold { get; set; } = 27.0f;

    /// <summary>
    /// Additional thirst units per degree above the threshold each second
    /// </summary>
    [DefaultValue(5.0d)]
    public float ThirstIncreasePerDegreeMultiplier { get; set; } = 5f;

    /// <summary>
    /// Exponential scaling factor for thirst gain in extreme heat
    /// </summary>
    [DefaultValue(0.2d)]
    public float HarshHeatExponentialGainMultiplier { get; set; } = 0.2f;

    /// <summary>
    /// Cooling bonus for each empty armor/clothing slot
    /// </summary>
    [DefaultValue(1.0d)]
    public float UnequippedSlotCooling { get; set; } = 1.0f;

    /// <summary>
    /// Cooling effect of being wet
    /// </summary>
    [DefaultValue(1.5d)]
    public float WetnessCoolingFactor { get; set; } = 1.5f;

    /// <summary>
    /// Cooling bonus when in a room
    /// </summary>
    [DefaultValue(1.5d)]
    public float ShelterCoolingFactor { get; set; } = 1.5f;

    /// <summary>
    /// Cooling effect in shade
    /// </summary>
    [DefaultValue(1.0d)]
    public float SunlightCoolingFactor { get; set; } = 1.0f;

    /// <summary>
    /// Temperature swing between day and night in degrees Celsius
    /// </summary>
    [DefaultValue(18.0d)]
    public float DiurnalVariationAmplitude { get; set; } = 18f;

    /// <summary>
    /// Reduction provided by refrigeration blocks (if supported mod is installed) 
    /// </summary>
    [DefaultValue(20.0d)]
    public float RefrigerationCooling { get; set; } = 20.0f;
}
