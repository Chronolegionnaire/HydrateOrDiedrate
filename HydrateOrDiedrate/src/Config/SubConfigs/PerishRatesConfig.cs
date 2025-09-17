using ProtoBuf;
using System.ComponentModel;

namespace HydrateOrDiedrate.Config.SubConfigs;

public class PerishRatesConfig
{
    /// <summary>
    /// Wether or not water should perish over time.
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;
}
