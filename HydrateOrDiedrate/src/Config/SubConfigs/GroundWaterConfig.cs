using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HydrateOrDiedrate.src.Config.SubConfigs;

public class GroundWaterConfig //TODO: this could potentially be split even further into WellConfig and AquiferConfig.
{
    /// <summary>
    /// Scales the flow rate of natural springs in wells
    /// </summary>
    [Category("Well")]
    [Range(0d, double.PositiveInfinity)]
    [DefaultValue(1.0d)]
    public float WellSpringOutputMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Max water retention depth (in blocks) for wells with normal shafts
    /// </summary>
    [Category("Well")]
    [DefaultValue(5)]
    public int WellwaterDepthMaxBase { get; set; } = 5;

    [Category("Well")]
    /// <summary>
    /// Max water retention depth (in blocks) for wells with clay shafts
    /// </summary>
    [DefaultValue(7)]
    public int WellwaterDepthMaxClay { get; set; } = 7;

    /// <summary>
    /// Max water retention depth (in blocks) for wells with stone shafts
    /// </summary>
    [Category("Well")]
    [DefaultValue(10)]
    public int WellwaterDepthMaxStone { get; set; } = 10;

    /// <summary>
    /// Multiplier for how fast it’s raised back up 
    /// </summary>
    [Category("Well")]
    [Range(0.05d, double.PositiveInfinity)]
    [DefaultValue(0.8d)]
    public float WinchRaiseSpeed { get; set; } = 0.8f;

    /// <summary>
    /// Multiplier for how fast the bucket lowers in a well
    /// </summary>
    [Category("Well")]
    [Range(0.05d, double.PositiveInfinity)]
    [DefaultValue(0.8d)]
    public float WinchLowerSpeed { get; set; } = 0.8f;

    /// <summary>
    /// Displays well output info in tooltips for winches
    /// </summary>
    [Category("Well")]
    [DefaultValue(true)]
    public bool WinchOutputInfo { get; set; } = true;

    /// <summary>
    /// Chance for a random output multiplier when generating an aquifer
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(0.02d)]
    public double AquiferRandomMultiplierChance { get; set; } = 0.02;

    /// <summary>
    /// Number of chunks to check around an aquifer chunk to smooth rating
    /// Higher will result in aquifers that span more chunks and may have preformance repercussions
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(4)]
    public int AquiferStep { get; set; } = 4;

    /// <summary>
    /// Water‑block multiplier for determining aquifer rating. Larger numbers will make aquifers stronger near regular water
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(4.0d)]
    public double AquiferWaterBlockMultiplier { get; set; } = 4.0;

    /// <summary>
    /// Salt‑water multiplier for determining aquifer rating
    /// Larger numbers will make aquifers stronger near salt-water
    /// (Will also make aquifers that are further from oceans saltier)
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(4.0d)]
    public double AquiferSaltWaterMultiplier { get; set; } = 4.0;

    /// <summary>
    /// Boiling water multiplier for determining aquifer rating
    /// Larger numbers will make aquifers stronger near boiling water
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(100)]
    public int AquiferBoilingWaterMultiplier { get; set; } = 100;

    /// <summary>
    /// Maximum aquifer quality rating above sea level
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(30)]
    public int AquiferRatingCeilingAboveSeaLevel { get; set; } = 30;

    /// <summary>
    /// Scale factor for depth’s effect on aquifer rating
    /// </summary>
    [Category("Aquifer")]
    [DefaultValue(1.0d)]
    public float AquiferDepthMultiplierScale { get; set; } = 1.0f;

    /// <summary>
    /// Block radius when prospecting within which aquifers are detected
    /// </summary>
    [Category("Prospecting")]
    [DefaultValue(3)]
    public int ProspectingRadius { get; set; } = 3;

    /// <summary>
    /// SHow aquifer prospecting on Node mode rather than density search
    /// </summary>
    [Category("Prospecting")]
    [DefaultValue(false)]
    public bool AquiferDataOnProspectingNodeMode { get; set; } = false;

    /// <summary>
    /// Toggle display of aquifer icons/overlays on the world map after prospecting
    /// </summary>
    [Category("Prospecting")]
    [DefaultValue(true)]
    public bool ShowAquiferProspectingDataOnMap { get; set; } = true;
}
