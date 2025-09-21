using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class PerishRatesConfig
{
    /// <summary>
    /// Wether or not water should perish over time.
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    public Dictionary<AssetLocation, ItemTransitionConfig> TransitionConfig { get; set; } = [];
}
