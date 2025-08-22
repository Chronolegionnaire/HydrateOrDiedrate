using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class AdvancedConfig
{
    //These are intentionally left out of the config lib configs, only want advanced users to touch these so they don't break things
    
    /// <summary>
    /// Increases mark dirty threshold for users experiencing rubber banding when pairing HoD with other mods that
    /// add more mark dirty calls.
    /// </summary>
    [Category("Advanced")]
    [DefaultValue(false)]
    public bool IncreaseMarkDirtyThreshold { get; set; } = false;
    
    /// <summary>The threshold value used by the Harmony patch. Higher numbers can adversely affect performance start at 10
    /// and keep increasing until rubber banding stops. Try to have set to lowest possible number (vanilla default is 10)
    /// to where you no longer experience rubber banding.</summary>
    [Category("Advanced")]
    [Range(10, 200)]
    [DefaultValue(20)]
    public int markdirtythreshold { get; set; } = 20;
}