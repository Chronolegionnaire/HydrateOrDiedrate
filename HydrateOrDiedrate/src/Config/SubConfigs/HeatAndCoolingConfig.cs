using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
    /// Ambient temperature above which thirst rate starts ramping up
    /// </summary>
    [DefaultValue(25.0d)]
    public float TemperatureThreshold { get; set; } = 25.0f;

    
    /// <summary>
    /// Percentage with which thirst increases based on temperature above threshold.
    /// increase =  tempAboveThreashold * HarshHeatThirstIncrease * (HarshHeatExponentialFactor ^ tempAboveThreashold)
    /// </summary>
    [DisplayFormat( DataFormatString = "P")]
    [DefaultValue(0.5d)]
    public float HarshHeatThirstIncrease { get; set; } = 0.05f;

    /// <summary>
    /// Exponential scaling factor for thirst gain in extreme heat
    /// </summary>
    [DefaultValue(1.15d)]
    public float HarshHeatExponentialFactor { get; set; } = 1.15f;

    /// <summary>
    /// Cooling bonus for on empty armor/clothing slots
    /// Note: this bonus only applies to some of the slots (slots like the necklace do not get this bonus)
    /// </summary>
    [DefaultValue(1.0d)]
    public float UnequippedSlotCooling { get; set; } = 1.0f;

    /// <summary>
    /// Cooling effect of being wet
    /// </summary>
    [DefaultValue(8d)]
    public float WetnessCoolingBonus { get; set; } = 8f;

    /// <summary>
    /// Cooling bonus when in a room
    /// </summary>
    [DefaultValue(8d)]
    public float RoomCoolingBonus { get; set; } = 8f;
    
    /// <summary>
    /// How much cooling being in shade provides
    /// </summary>
    [DefaultValue(8d)]
    public float ShadeCoolingBonus { get; set; } = 8f;
    
    /// <summary>
    /// How many degrees of effective temperature are reduced per 1 point of Cooling.
    /// This directly shifts the temperature used by the harsh-heat equation,
    /// allowing cooling to matter across the entire heat range.
    /// </summary>
    [DefaultValue(0.5d)]
    public float CoolingTempOffsetPerPoint { get; set; } = 0.5f;
    
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
