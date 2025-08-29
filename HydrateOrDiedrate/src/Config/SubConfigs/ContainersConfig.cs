using ProtoBuf;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class ContainersConfig
{
    /// <summary>
    /// Storage capacity of a keg in litres
    /// </summary>
    [Category("Keg")]
    [Range(1d, 2000d)]
    [DefaultValue(100.0d)]
    public float KegCapacityLitres { get; set; } = 100.0f;

    /// <summary>
    /// Hourly spoilage rate when a keg is sealed
    /// </summary>
    [Category("Keg")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(0.15d)]
    public float SpoilRateUntapped { get; set; } = 0.15f;

    /// <summary>
    /// Hourly spoilage rate when a keg is open/tapped
    /// </summary>
    [Category("Keg")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(0.65d)]
    public float SpoilRateTapped { get; set; } = 0.65f;

    /// <summary>
    /// Chance of recovering the iron hoop when dismantling a keg 
    /// </summary>
    [Category("Keg")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.8d)]
    public float KegIronHoopDropChance { get; set; } = 0.8f;

    /// <summary>
    /// Chance of recovering the tap when dismantling
    /// </summary>
    [Category("Keg")]
    [DisplayFormat(DataFormatString = "P")]
    [Range(0d, 1d)]
    [DefaultValue(0.9d)]
    public float KegTapDropChance { get; set; } = 0.9f;

    /// <summary>
    /// If true, kegs drop with contents instead of spilling
    /// </summary>
    [Category("Keg")]
    [DefaultValue(true)]
    public bool KegDropWithLiquid { get; set; } = true;

    /// <summary>
    /// Capacity of a tun barrel in litres 
    /// </summary>
    [Category("Tun")]
    [Range(1d, 2000d)]
    [DefaultValue(950.0d)]
    public float TunCapacityLitres { get; set; } = 950f;
    
    /// <summary>
    /// Global spoilage multiplier for tuns
    /// </summary>
    [Category("Tun")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(1.0d)]
    public float TunSpoilRateMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// If true, tuns drop with contents instead of spilling 
    /// </summary>
    [Category("Tun")]
    [DefaultValue(false)]
    public bool TunDropWithLiquid { get; set; } = false;
}
