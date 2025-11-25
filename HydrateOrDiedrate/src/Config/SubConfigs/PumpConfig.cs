using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class PumpConfig
{
    /// <summary>
    /// Whether hand pump priming strokes should be enabled
    /// </summary>
    [DefaultValue(true)]
    public bool HandPumpEnablePriming { get; set; } = true;
    
    /// <summary>
    /// Whether hand pump priming strokes should be enabled
    /// </summary>
    [DefaultValue(true)]
    public bool HandPumpOutputInfo { get; set; } = true;

    /// <summary>
    /// How many pipe segments per priming stroke required on the hand pump.
    /// </summary>
    [DefaultValue(3)]
    public int HandPumpPrimingBlocksPerStroke  { get; set; } = 3;
}