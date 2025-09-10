using System.ComponentModel;

namespace HydrateOrDiedrate.Config.SubConfigs;

/// <summary>
/// Config for modifiers on the satiety lost from drinking various types of water.
/// NOTE: the modifiers are additive and the end result will not go above 0.
/// </summary>
public class SatietyConfig
{

    /// <summary>
    /// Modifier on the satiety of fresh water
    /// </summary>
    [DefaultValue(0d)]
    public float FreshWaterSatietyModifier { get; set; } = -100;
    
    /// <summary>
    /// Modifier on the satiety of salt water
    /// </summary>
    [DefaultValue(-200d)]
    public float SaltWaterSatietyModifier { get; set; } = -200;

    /// <summary>
    /// Modifier on the satiety of boiled water
    /// </summary>
    [DefaultValue(50d)]
    public float BoiledWaterSatietyModifier { get; set; } = 50;

    /// <summary>
    /// Modifier on the satiety of natural water
    /// </summary>
    [DefaultValue(0d)]
    public float NaturalWaterSatietyModifier { get; set; } = 0;
    
    /// <summary>
    /// Modifier on the satiety of well water
    /// </summary>
    [DefaultValue(100d)]
    public float WellWaterSatietyModifier { get; set; } = 100;

    /// <summary>
    /// Modifier on the satiety of rain water
    /// </summary>
    [DefaultValue(0d)]
    public float RainWaterSatietyModifier { get; set; } = 0;

    /// <summary>
    /// Modifier on the satiety of distilled water
    /// </summary>
    [DefaultValue(0d)]
    public float DistilledWaterSatietyModifier { get; set; } = 0;

    /// <summary>
    /// Modifier on the satiety of clean water
    /// </summary>
    [DefaultValue(0d)]
    public float CleanWaterSatietyModifier { get; set; } = 0;

    /// <summary>
    /// Modifier on the satiety of muddy water
    /// </summary>
    [DefaultValue(-200d)]
    public float MuddyWaterSatietyModifier { get; set; } = -200;
    
    /// <summary>
    /// Modifier on the satiety of tainted water
    /// </summary>
    [DefaultValue(-400d)]
    public float TaintedWaterSatietyModifier { get; set; } = -400;
    
    /// <summary>
    /// Modifier on the satiety of poisoned water
    /// </summary>
    [DefaultValue(0d)]
    public float PoisonedWaterSatietyModifier { get; set; } = 0;
}
