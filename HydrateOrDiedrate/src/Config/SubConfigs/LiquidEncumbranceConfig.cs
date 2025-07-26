using ProtoBuf;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class LiquidEncumbranceConfig
{
    /// <summary>
    /// Wether or not the liquid encumbrance is enabled.
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// How many litres of water a player can carry before they become encumbered.
    /// </summary>
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(4.0d)]
    public float EncumbranceLimit { get; set; } = 4.0f;

    /// <summary>
    /// How large the penalty for being encumbered is.
    /// </summary>
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.4d)]
    public float EncumbranceMovementSpeedDebuff { get; set; } = 0.4f;
}
