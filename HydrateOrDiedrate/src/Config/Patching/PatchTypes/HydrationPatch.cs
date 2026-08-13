using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching.PatchTypes;

public class HydrationPatch : PatchBase
{
    public override string Code { get; set; }

    [JsonProperty(Attributes.Hydration)]
    public override float? Value { get; set; }

    public bool IsBoiling { get; set; }

    [JsonProperty("hydrationByType")]
    public override Dictionary<string, float> ValueByType { get; set; }

    public override void Apply(CollectibleObject collectible, float value)
    {
        var token = collectible.Attributes.Token;
        if (OverwriteExisting || token[Attributes.Hydration] is null) token[Attributes.Hydration] = value;
        if (OverwriteExisting || token[Attributes.IsBoiling] is null) token[Attributes.IsBoiling] = IsBoiling;
    }
}
