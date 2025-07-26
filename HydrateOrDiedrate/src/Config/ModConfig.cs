using HydrateOrDiedrate.Config.SubConfigs;
using Newtonsoft.Json;
using System.ComponentModel;

namespace HydrateOrDiedrate.Config;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ModConfig
{
    public const string ConfigPath = "HydrateOrDiedrateConfig.json";

    public static ModConfig Instance { get; internal set; }

    /// <summary>
    /// The configuration for thirst mechanics
    /// </summary>
    public ThirstConfig Thirst { get; set; } = new();

    /// <summary>
    /// The configuration for satiety mechanics
    /// </summary>
    public SatietyConfig Satiety { get; set; } = new();

    /// <summary>
    /// The configuration for the perish rates
    /// </summary>
    public PerishRatesConfig PerishRates { get; set; } = new();

    /// <summary>
    /// The configuration for liquid encumbrance mechanics
    /// </summary>
    public LiquidEncumbranceConfig LiquidEncumbrance { get; set; } = new();

    /// <summary>
    /// The configuration for heat and cooling mechanics
    /// </summary>
    public HeatAndCoolingConfig HeatAndCooling { get; set; } = new();

    /// <summary>
    /// The configuration for ground water sources
    /// </summary>
    public GroundWaterConfig GroundWater { get; set; } = new();

    /// <summary>
    /// The configuration for rain mechanics
    /// </summary>
    public RainConfig Rain { get; set; } = new();

    /// <summary>
    /// The configuration for the containers
    /// </summary>
    public ContainersConfig Containers { get; set; } = new();

    /// <summary>
    /// The configuration for the XLib integration
    /// </summary>
    public XLibConfig XLib { get; set; } = new();

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
