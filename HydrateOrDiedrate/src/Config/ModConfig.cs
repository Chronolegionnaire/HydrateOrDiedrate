using HydrateOrDiedrate.src.Config.SubConfigs;
using ProtoBuf;
using System.ComponentModel;

namespace HydrateOrDiedrate.Config;

public class ModConfig
{
    public const string ConfigPath = "HydrateOrDiedrateConfig.json";

    public static ModConfig Instance { get; internal set; }

    /// <summary>
    /// The configuration for thirst mechanics
    /// </summary>
    public ThirstConfig Thirst { get; set; } = new ThirstConfig();

    /// <summary>
    /// The configuration for satiety mechanics
    /// </summary>
    public SatietyConfig Satiety { get; set; } = new SatietyConfig();

    /// <summary>
    /// The configuration for the perish rates
    /// </summary>
    public PerishRatesConfig PerishRates { get; set; } = new PerishRatesConfig();

    /// <summary>
    /// The configuration for liquid encumbrance mechanics
    /// </summary>
    public LiquidEncumbranceConfig LiquidEncumbrance { get; set; } = new LiquidEncumbranceConfig();

    /// <summary>
    /// The configuration for heat and cooling mechanics
    /// </summary>
    public HeatAndCoolingConfig HeatAndCooling { get; set; } = new HeatAndCoolingConfig();

    /// <summary>
    /// The configuration for ground water sources
    /// </summary>
    public GroundWaterConfig GroundWater { get; set; }

    /// <summary>
    /// The configuration for rain mechanics
    /// </summary>
    public RainConfig Rain { get; set; } = new RainConfig();

    /// <summary>
    /// The configuration for the containers
    /// </summary>
    public ContainersConfig Containers { get; set; } = new ContainersConfig();

    /// <summary>
    /// The configuration for the XLib integration
    /// </summary>
    public XLibConfig XLib { get; set; } = new XLibConfig();

    /// <summary>
    /// Turns off the sway/distortion effect when the player is drunk
    /// </summary>
    [DefaultValue(true)]
    public bool DisableDrunkSway { get; set; } = true;

    /// <summary>
    /// Allows using the sprint key to initiate drinking instead of sneaking
    /// </summary>
    [DefaultValue(false)]
    public bool SprintToDrink { get; set; } = false;
}
