using System.ComponentModel;

namespace HydrateOrDiedrate.src.Config.SubConfigs;

public class SatietyConfig
{
    /// <summary>
    /// The amount of satiety a player gets from drinking normal water.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-100d)]
    public float WaterSatiety { get; set; } = -100f;
    
    /// <summary>
    /// The amount of satiety a player gets from drinking salt water.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-100d)]
    public float SaltWaterSatiety { get; set; } = -100f;

    /// <summary>
    /// The amount of satiety a player gets from drinking boiling water.
    /// </summary>
    [DefaultValue(0d)]
    public float BoilingWaterSatiety { get; set; } = 0f;

    /// <summary>
    /// The amount of satiety a player gets from drinking rain water.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-50d)]
    public float RainWaterSatiety { get; set; } = -50f;

    /// <summary>
    /// The amount of satiety a player gets from drinking distilled water.
    /// </summary>
    [DefaultValue(0d)]
    public float DistilledWaterSatiety { get; set; } = 0f;

    /// <summary>
    /// The amount of satiety a player gets from drinking boiled water.
    /// </summary>
    [DefaultValue(0d)]
    public float BoiledWaterSatiety { get; set; } = 0f;

    /// <summary>
    /// The amount of satiety a player gets from drinking boiled rain water.
    /// </summary>
    [DefaultValue(0d)]
    public float BoiledRainWaterSatiety { get; set; } = 0f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water.
    /// </summary>
    [DefaultValue(0d)]
    public float WellWaterFreshSatiety { get; set; } = 0f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is salty.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-100d)]
    public float WellWaterSaltSatiety { get; set; } = -100f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is muddy.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-75d)]
    public float WellWaterMuddySatiety { get; set; } = -75f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is tainted.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    public float WellWaterTaintedSatiety { get; set; } = -400f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is poisoned.
    /// </summary>
    [DefaultValue(0d)]
    public float WellWaterPoisonedSatiety { get; set; } = 0f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is muddy and salty.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-75d)]
    public float WellWaterMuddySaltSatiety { get; set; } = -75f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is tainted and salty.
    /// (this is normally a negative number, which results in nutrient deficiency)
    /// </summary>
    [DefaultValue(-400d)]
    public float WellWaterTaintedSaltSatiety { get; set; } = -400f;

    /// <summary>
    /// The amount of satiety a player gets from drinking well water that is poisoned and salty.
    /// </summary>
    [DefaultValue(0d)]
    public float WellWaterPoisonedSaltSatiety { get; set; } = 0f;
}
