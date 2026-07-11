using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching.PatchTypes;

public class HydrationPatch : PatchBase
{
    public override string Code { get; set; }

    ////Legacy mapping
    [Obsolete("use Code instead")] [JsonProperty(nameof(BlockCode))] private string BlockCode { set => Code = value; }
    [Obsolete("use Code instead")] [JsonProperty(nameof(ItemName))] private string ItemName { set => Code = value;}

    [JsonProperty(Attributes.Hydration)]
    public override float Value { get; set; }

    public bool IsBoiling { get; set; }

    [JsonProperty("hydrationByType")]
    public override Dictionary<string, float> ValueByType { get; set; }

    public override void Apply(CollectibleObject collectible, float value)
    {
        collectible.Attributes.Token[Attributes.Hydration] = value;
        collectible.Attributes.Token[Attributes.IsBoiling] = IsBoiling;
    }
}
