using ProtoBuf;
using System.ComponentModel;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class PerishRatesConfig
{
    /// <summary>
    /// Wether or not water should perish over time.
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    [DefaultValue(150d)]
    public float RainWaterFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float RainWaterTransitionHours { get; set; } = 36f;

    [DefaultValue(75d)]
    public float BoiledWaterFreshHours { get; set; } = 75f;

    [DefaultValue(18d)]
    public float BoiledWaterTransitionHours { get; set; } = 18f;

    [DefaultValue(75d)]
    public float BoiledRainWaterFreshHours { get; set; } = 75f;

    [DefaultValue(18d)]
    public float BoiledRainWaterTransitionHours { get; set; } = 18f;

    [DefaultValue(150d)]
    public float DistilledWaterFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float DistilledWaterTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterFreshFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterFreshTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterSaltFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterSaltTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterMuddyFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterMuddyTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterTaintedFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterTaintedTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterPoisonedFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterPoisonedTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterMuddySaltFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterMuddySaltTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterTaintedSaltFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterTaintedSaltTransitionHours { get; set; } = 36f;

    [DefaultValue(150d)]
    public float WellWaterPoisonedSaltFreshHours { get; set; } = 150f;

    [DefaultValue(36d)]
    public float WellWaterPoisonedSaltTransitionHours { get; set; } = 36f;
}
