using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching.PatchTypes;

public class CoolingPatch : PatchBase
{
    public override string Code { get; set; }

    [JsonProperty(Attributes.Cooling)]
    public override float? Value { get; set; }

    [JsonProperty("CoolingByType")]
    public override Dictionary<string, float> ValueByType { get; set; }

    public override void Apply(CollectibleObject collectible, float value)
    {
        var token = collectible.Attributes.Token;
        if (OverwriteExisting || token[Attributes.Cooling] is null) token[Attributes.Cooling] = value;
    }
}
