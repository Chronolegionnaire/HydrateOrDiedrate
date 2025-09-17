using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching.PatchTypes;

public class BlockHydrationPatch : HydrationPatchBase
{
    [JsonProperty("BlockCode")]
    public override string Code { get; set; }

    [JsonProperty(Attributes.IsBoiling)]
    public bool IsBoiling { get; set; }

    [JsonProperty(Attributes.HungerReduction)]
    public int HungerReduction { get; set; }

    [JsonProperty(Attributes.Healing)]
    public int Healing { get; set; }

    public override void Apply(CollectibleObject collectible, float value)
    {
        collectible.Attributes.Token["hydration"] = value;
        collectible.Attributes.Token[Attributes.IsBoiling] = IsBoiling;
        collectible.Attributes.Token[Attributes.HungerReduction] = HungerReduction;
        collectible.Attributes.Token[Attributes.Healing] = Healing;
    }

    public static PatchCollection<BlockHydrationPatch> GenerateDefaultPatchCollection() => new()
    {
        Priority = 5,
        Patches = []
    };
}
