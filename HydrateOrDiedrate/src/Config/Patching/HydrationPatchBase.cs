using Newtonsoft.Json;
using System.Collections.Generic;

namespace HydrateOrDiedrate.Config.Patching;

public abstract class HydrationPatchBase : PatchBase
{
    [JsonProperty(Attributes.Hydration)]
    public override float Value { get; set; }


    [JsonProperty(Attributes.HydrationByType)]
    public override Dictionary<string, float> ValueByType { get; set; }
}
