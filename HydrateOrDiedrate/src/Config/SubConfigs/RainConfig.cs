using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class RainConfig
{
    /// <summary>
    /// Whether rain gathering should be enabled
    /// (Allows containers to collect rainwater automatically)
    /// </summary>
    [DefaultValue(true)]
    public bool EnableRainGathering { get; set; } = true;

    /// <summary>
    /// Scales how much water is gathered per tick of rain
    /// </summary>
    [Range(0.1d, double.PositiveInfinity)]
    [DefaultValue(1.0d)]
    public float RainMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Toggles per‑tick updates for rain particles
    /// (performance trade‑off)
    /// </summary>
    [DefaultValue(false)]
    public bool EnableParticleTicking { get; set; } = false;
}
