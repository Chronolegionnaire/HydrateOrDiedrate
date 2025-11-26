using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class WorldGenConfig
{
    /// <summary>
    /// Multiplier for vanilla pond generation attempts.
    /// 1.0 = vanilla, 0 = no ponds, 2 = roughly double ponds, etc.
    /// </summary>
    [Range(0d, 10d)]
    [DefaultValue(1f)]
    public float PondChance { get; set; } = 1f;
}