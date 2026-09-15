using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class ItemTransitionConfig
{
    [Range(0, float.PositiveInfinity)]
    [DefaultValue(150f)]
    public float FreshHours { get; set; } = 150f;

    [Range(0.001f, float.PositiveInfinity)]
    [DefaultValue(36f)]
    public float TransitionHours { get; set; } = 36f;
}
