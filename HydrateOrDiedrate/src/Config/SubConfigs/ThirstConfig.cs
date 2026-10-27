using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class ThirstConfig 
{
    /// <summary>
    /// Master switch for the entire thirst/dehydration system
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The upper bound of the player’s hydration bar
    /// </summary>
    [Range(100d, 10000d)]
    [DefaultValue(1500f)]
    public float MaxHydration { get; set; } = 1500f;

    [Obsolete("Use MaxHydration instead")]
    [JsonIgnore]
    [Browsable(false)]
    public float MaxThirst
    {
        get => MaxHydration;
        set => MaxHydration = value;
    }

    /// <summary>
    /// Base amount of hydration lost every thirst tick under normal conditions
    /// (this will be multiplied with other factors to decide the final amount of hydration lost)
    /// </summary>
    [Category("Thirst Rate")]
    [Range(0.1d, double.PositiveInfinity)]
    [DefaultValue(10f)]
    public float ThirstRate { get; set; } = 10f;

    [Obsolete("Use ThirstRate instead")]
    [JsonIgnore]
    [Browsable(false)]
    public float ThirstDecayRate 
    { 
        get => ThirstRate;
        set => ThirstRate = value; 
    }

    /// <summary>
    /// The maximum allowed multiplier on ThirstRate.
    /// (So the thirst rate after applying all multipliers will be capped at ThirstRate * MaxThirstRateMultiplier)
    /// </summary>
    [Category("Thirst Rate")]
    [Range(1d, double.PositiveInfinity)]
    [DefaultValue(5f)]
    [DisplayFormat(DataFormatString = "P")]
    public float MaxThirstRateMultiplier { get; set; } = 5f;

    [Obsolete("Use ThirstRateMax instead")]
    [JsonIgnore]
    [Browsable(false)]
    public float ThirstDecayRateMax 
    {
        get => MaxThirstRateMultiplier;
        set => MaxThirstRateMultiplier = value; 
    }

    /// <summary>
    /// Scales the thirst reduction delay you get from drinking drinks
    /// Note: other parts of the equation are not configurable yet and final delay is capped at 600 seconds.
    /// </summary>
    [Category("Thirst Rate")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(1d)]
    public float HydrationLossDelayMultiplierNormalized { get; set; } = 1f; //TODO: the other parts of this equation should really be configurable as well.

    /// <summary>
    /// Multiplier for thirst rate when the player is sprinting.
    /// </summary>
    [Category("Thirst Rate")]
    [Range(1d, double.PositiveInfinity)]
    [DefaultValue(1.5d)]
    public float SprintThirstMultiplier { get; set; } = 1.5f;
    
    /// <summary>
    /// Multiplier for thirst rate when the player is idle.
    /// </summary>
    [Category("Thirst Rate")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0.01d, 1d)]
    [DefaultValue(0.25f)]
    public float IdleThirstModifier { get; set; } = 0.25f;

    /// <summary>
    /// Damage dealt to the player each interval once fully dehydrated
    /// (set to 0 to disable thirst damage entirely)
    /// </summary>
    [Category("Damage")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(1f)]
    public float ThirstDamage { get; set; } = 1f;

    /// <summary>
    /// How much damage a player takes from drinking boiling water.
    /// (set to 0 to disable boiling water damage entirely)
    /// </summary>
    [Category("Damage")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(5.0d)]
    public float BoilingWaterDamage { get; set; } = 5.0f;

    /// <summary>
    /// The maximum speed penalty a player can get from thirst.
    /// </summary>
    [Category("Penalty")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.3f)]
    public float MaxMovementSpeedPenalty { get; set; } = 0.3f;

    /// <summary>
    /// From which thirst level the player starts getting a movement speed penalty.
    /// Set to -1 to disable the movement speed penalty entirely.
    /// (penalty will increase gradually as hydration drops even further)
    /// </summary>
    [Category("Penalty")]
    [Range(-1d, 10000d)] //TODO: should probably be a percentage of MaxThirst so you can't just forget about and have it always be active because you lowered max thirst
    [DefaultValue(600f)]
    public float MovementSpeedPenaltyThreshold { get; set; } = 600f;

    /// <summary>
    /// Percentage of the thirst gauge that will be filled after the player respawns.
    /// </summary>
    [Category("Penalty")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.5f)]
    public float ThirstPercentageOnRespawn { get; set; } = 0.5f;
    
    /// <summary>
    /// How much thirst is effected in story locations.
    /// </summary>
    [Category("Story")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 2d)]
    [DefaultValue(1.00f)]
    public float ThirstRateAtStoryLocations { get; set; } = 1.00f;
    
    /// <summary>
    /// Multiplier on nutrition penalty from drinking water, set this to 0 to disable it entirely.
    /// </summary>
    [Category("Penalty")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 10d)]
    [DefaultValue(3.00f)]
    public float NutritionDeficitMultiplier { get; set; } = 3.00f;

    /// <summary>
    /// Toggle for turning on anti thirst scumming. If on players will spawn with the same thirst as when they died,
    /// unless it is 0 in which case they will spawn with 0.005% satiety.
    /// </summary>
    [DefaultValue(false)]
    public bool AntiThirstScumming { get; set; } = false;
}
