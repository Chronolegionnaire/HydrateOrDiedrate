using HydrateOrDiedrate.Config.SubConfigs;
using InsanityLib.Generators.Attributes;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Config;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class ModConfig
{
    public const string ConfigPath = "HydrateOrDiedrateConfig.json";

    [AutoConfig(ConfigPath, ServerSync = true)]
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
    /// The configuration for pump mechanics
    /// </summary>
    public PumpConfig Pump { get; set; } = new();
    
    /// <summary>
    /// The configuration for world gen settings
    /// </summary>
    public WorldGenConfig WorldGen { get; set; } = new();

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
    [DefaultValue(false)]
    public bool DisableDrunkSway { get; set; } = false;

    // TODO See if there is a good way to re-implement this auto fill functionality
    public void LoadLiveLiquidPortionData(ICoreAPI api)
    {
        foreach(var item in api.World.Items.OfType<ItemLiquidPortion>().Where(item => item.Code.Path.StartsWith("water")))
        {
            if (!Satiety.ItemSatietyMapping.ContainsKey(item.Code))
            {
                var satiety = item.Attributes?.Token["waterTightContainerProps"]?["nutritionPropsPerLitre"]?.Value<float>("satiety");
                if(satiety is null || satiety > 0) continue;
                
                Satiety.ItemSatietyMapping[item.Code] = satiety.Value;
            }

            var perish = item.TransitionableProps?.FirstOrDefault(static item => item.Type == EnumTransitionType.Perish);
            if(perish is not null)
            {
                PerishRates.TransitionConfig[item.Code] = new ItemTransitionConfig
                {
                    FreshHours = perish.FreshHours.avg,
                    TransitionHours = perish.TransitionHours.avg,
                };
            }
        }
    }
}
