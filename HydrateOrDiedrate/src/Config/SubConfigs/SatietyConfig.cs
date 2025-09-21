using System.Collections.Generic;
using System.ComponentModel;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.SubConfigs;

/// <summary>
/// Config for modifiers on the satiety lost from drinking various types of water.
/// NOTE: the modifiers are additive and the end result will not go above 0.
/// </summary>
public class SatietyConfig
{
    public Dictionary<AssetLocation, float> ItemSatietyMapping { get; set; } = [];
}
