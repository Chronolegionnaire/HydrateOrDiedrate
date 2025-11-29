using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching.PatchTypes;

public class BlockHydrationPatch : HydrationPatchBase
{
    [JsonProperty("BlockCode")]
    public override string Code { get; set; }

    [JsonProperty(Attributes.IsBoiling)]
    public bool IsBoiling { get; set; }

    [JsonProperty(Attributes.NutritionDeficit)]
    public int NutritionDeficit { get; set; }

    [JsonProperty(Attributes.Healing)]
    public int Healing { get; set; }

    public override void Apply(CollectibleObject collectible, float value)
    {
        collectible.Attributes.Token["hydration"] = value;
        collectible.Attributes.Token[Attributes.IsBoiling] = IsBoiling;
        collectible.Attributes.Token[Attributes.NutritionDeficit] = NutritionDeficit;
        collectible.Attributes.Token[Attributes.Healing] = Healing;
    }

    public static PatchCollection<BlockHydrationPatch> GenerateDefaultPatchCollection() => new()
    {
        Priority = 5,
        Patches = [
            new()
            {
                Code = "game:mushroom-*",
                ValueByType = new()
                {
                    ["*"] = 0,
                    ["game:mushroom-flyagaric-*"] = -25,
                    ["game:mushroom-earthball-*"] = -30,
                    ["game:mushroom-deathcap-*"] = -35,
                    ["game:mushroom-elfinsaddle-*"] = -30,
                    ["game:mushroom-jackolantern-*"] = -25,
                    ["game:mushroom-devilbolete-*"] = -80,
                    ["game:mushroom-bitterbolete-*"] = -15,
                    ["game:mushroom-devilstooth-*"] = -25,
                    ["game:mushroom-golddropmilkcap-*"] = -20,
                    ["game:mushroom-beardedtooth-*"] = 10,
                    ["game:mushroom-whiteoyster-*"] = 10,
                    ["game:mushroom-pinkoyster-*"] = 10,
                    ["game:mushroom-dryadsaddle-*"] = 10,
                    ["game:mushroom-tinderhoof-*"] = 10,
                    ["game:mushroom-chickenofthewoods-*"] = 10,
                    ["game:mushroom-reishi-*"] = 5,
                    ["game:mushroom-funeralbell-*"] = -20,
                    ["game:mushroom-livermushroom-*"] = 10,
                    ["game:mushroom-pinkbonnet-*"] = -25,
                    ["game:mushroom-shiitake-*"] = 5,
                    ["game:mushroom-deerear-*"] = 5,
                    ["game:mushroom-aniseedfunnel-*"] = -20,
                    ["game:mushroom-blewit-*"] = -15,
                    ["game:mushroom-destroyingangel-*"] = -40,
                    ["game:mushroom-falseearthstar-*"] = -25,
                    ["game:mushroom-greenearthtongue-*"] = -10,
                    ["game:mushroom-questionablestropharia-*"] = -20,
                    ["game:mushroom-stinkhorn-*"] = -15,
                    ["game:mushroom-sulfurtuft-*"] = -30,
                    ["game:mushroom-yellowstainer-*"] = -30,
                    ["game:mushroom-dyerspolypore-*"] = -10,
                    ["game:mushroom-flattoppedclubcoral-*"] = -10,
                    ["game:mushroom-northernreddye-*"] = -5,
                    ["game:mushroom-purplefairyclub-*"] = -5,
                    ["game:mushroom-redramaria-*"] = -5,
                    ["game:mushroom-yellowramaria-*"] = -5,
                    ["game:mushroom-aspenbolete-*"] = 5,
                    ["game:mushroom-birchbolete-*"] = 5,
                    ["game:mushroom-bluechanterelle-*"] = 10,
                    ["game:mushroom-candycap-*"] = 10,
                    ["game:mushroom-coccora-*"] = 10,
                    ["game:mushroom-desertshaggymane-*"] = 5,
                    ["game:mushroom-friedchickenmushroom-*"] = 10,
                    ["game:mushroom-hawkwing-*"] = 5,
                    ["game:mushroom-hedgehogmushroom-*"] = 10,
                    ["game:mushroom-honeymushroom-*"] = 5,
                    ["game:mushroom-matsutake-*"] = 10,
                    ["game:mushroom-parasol-*"] = 10,
                    ["game:mushroom-queenbolete-*"] = 10,
                    ["game:mushroom-shaggymane-*"] = 5,
                    ["game:mushroom-shrimpmushroom-*"] = 10,
                    ["game:mushroom-slipperyjack-*"] = 5,
                    ["game:mushroom-winterchanterelle-*"] = 10,
                    ["game:mushroom-artistsconk-*"] = -5,
                    ["game:mushroom-beefsteakfungus-*"] = 5,
                    ["game:mushroom-redbeltedconk-*"] = -5,
                    ["game:mushroom-turkeytail-*"] = -5,
                    ["game:mushroom-westernvarnishedconk-*"] = -5,
                    ["game:mushroom-fieldmushroom-*"] = 10,
                    ["game:mushroom-almondmushroom-*"] = 10,
                    ["game:mushroom-blacktrumpet-*"] = 10,
                    ["game:mushroom-chanterelle-*"] = 10,
                    ["game:mushroom-commonmorel-*"] = 10,
                    ["game:mushroom-greencrackedrussula-*"] = 10,
                    ["game:mushroom-indigomilkcap-*"] = 10,
                    ["game:mushroom-kingbolete-*"] = 10,
                    ["game:mushroom-lobster-*"] = 10,
                    ["game:mushroom-orangeoakbolete-*"] = 5,
                    ["game:mushroom-paddystraw-*"] = 10,
                    ["game:mushroom-puffball-*"] = 10,
                    ["game:mushroom-redwinecap-*"] = 10,
                    ["game:mushroom-saffronmilkcap-*"] = 10,
                    ["game:mushroom-violetwebcap-*"] = 5,
                    ["game:mushroom-witchhat-*"] = -20,
                }
            }
        ]
    };
}
