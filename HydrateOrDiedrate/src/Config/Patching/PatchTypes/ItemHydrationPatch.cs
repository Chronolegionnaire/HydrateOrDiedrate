using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config.Patching.PatchTypes;

public class ItemHydrationPatch : HydrationPatchBase
{
    [JsonProperty("ItemName")]
    public override string Code { get; set; }

    public override void Apply(CollectibleObject collectible, float value)
    {
        collectible.Attributes.Token[Attributes.Hydration] = value;
    }

    public static PatchCollection<ItemHydrationPatch> GenerateDefaultPatchCollection() => new()
    {
        Priority = 5,
        Patches =
        [
            new()
            {
                Code = "game:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 950,
                    ["game:juiceportion-cranberry"] = 900,
                    ["game:juiceportion-blueberry"] = 1000,
                    ["game:juiceportion-pinkapple"] = 1050,
                    ["game:juiceportion-lychee"] = 1250,
                    ["game:juiceportion-redcurrant"] = 1100,
                    ["game:juiceportion-breadfruit"] = 800,
                    ["game:juiceportion-pineapple"] = 1250,
                    ["game:juiceportion-blackcurrant"] = 1100,
                    ["game:juiceportion-saguaro"] = 1300,
                    ["game:juiceportion-whitecurrant"] = 1100,
                    ["game:juiceportion-redapple"] = 1200,
                    ["game:juiceportion-yellowapple"] = 1200,
                    ["game:juiceportion-apple"] = 1200,
                    ["game:juiceportion-cherry"] = 1100,
                    ["game:juiceportion-peach"] = 1250,
                    ["game:juiceportion-pear"] = 1250,
                    ["game:juiceportion-orange"] = 1300,
                    ["game:juiceportion-mango"] = 1250,
                    ["game:juiceportion-pomegranate"] = 1150,
                    ["game:juiceportion-redgrapes"] = 1250,
                    ["game:juiceportion-greengrapes"] = 1250
                }
            },
            new()
            {
                Code = "game:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40,
                    ["game:fruit-cranberry"] = 40,
                    ["game:fruit-blueberry"] = 40,
                    ["game:fruit-pinkapple"] = 30,
                    ["game:fruit-lychee"] = 30,
                    ["game:fruit-redcurrant"] = 40,
                    ["game:fruit-breadfruit"] = 5,
                    ["game:fruit-pineapple"] = 35,
                    ["game:fruit-blackcurrant"] = 40,
                    ["game:fruit-saguaro"] = 60,
                    ["game:fruit-whitecurrant"] = 40,
                    ["game:fruit-redapple"] = 30,
                    ["game:fruit-yellowapple"] = 30,
                    ["game:fruit-cherry"] = 40,
                    ["game:fruit-peach"] = 50,
                    ["game:fruit-pear"] = 35,
                    ["game:fruit-orange"] = 35,
                    ["game:fruit-mango"] = 50,
                    ["game:fruit-pomegranate"] = 40,
                    ["game:fruit-redgrapes"] = 40,
                    ["game:fruit-greengrapes"] = 40
                }
            },
            new()
            {
                Code = "game:bambooshoot",
                Value = 15
            },
            new()
            {
                Code = "game:bread-*",
                ValueByType = new()
                {
                    ["*"] = -50,
                    ["game:bread-spelt"] = -50,
                    ["game:bread-rye"] = -50,
                    ["game:bread-flax"] = -40,
                    ["game:bread-rice"] = -60,
                    ["game:bread-cassava"] = -50,
                    ["game:bread-amaranth"] = -40,
                    ["game:bread-sunflower"] = -50,
                    ["game:bread-spelt-partbaked"] = -40,
                    ["game:bread-rye-partbaked"] = -40,
                    ["game:bread-flax-partbaked"] = -35,
                    ["game:bread-rice-partbaked"] = -50,
                    ["game:bread-cassava-partbaked"] = -40,
                    ["game:bread-amaranth-partbaked"] = -35,
                    ["game:bread-sunflower-partbaked"] = -40,
                    ["game:bread-spelt-perfect"] = -50,
                    ["game:bread-rye-perfect"] = -50,
                    ["game:bread-flax-perfect"] = -40,
                    ["game:bread-rice-perfect"] = -60,
                    ["game:bread-cassava-perfect"] = -50,
                    ["game:bread-amaranth-perfect"] = -40,
                    ["game:bread-sunflower-perfect"] = -50,
                    ["game:bread-spelt-charred"] = -80,
                    ["game:bread-rye-charred"] = -100,
                    ["game:bread-flax-charred"] = -70,
                    ["game:bread-rice-charred"] = -80,
                    ["game:bread-cassava-charred"] = -80,
                    ["game:bread-amaranth-charred"] = -70,
                    ["game:bread-sunflower-charred"] = -80
                }
            },
            new()
            {
                Code = "game:bushmeat-*",
                ValueByType = new()
                {
                    ["*"] = -80,
                    ["game:bushmeat-cooked"] = -70,
                    ["game:bushmeat-cured"] = -90
                }
            },
            new()
            {
                Code = "game:butter",
                Value = -80
            },
            new()
            {
                Code = "game:cheese-*",
                ValueByType = new()
                {
                    ["*"] = -50,
                    ["game:cheese-blue-1slice"] = -60,
                    ["game:cheese-cheddar-1slice"] = -45
                }
            },
            new()
            {
                Code = "game:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5,
                    ["game:dough-spelt"] = 5,
                    ["game:dough-rye"] = 5,
                    ["game:dough-flax"] = 5,
                    ["game:dough-rice"] = 5,
                    ["game:dough-cassava"] = 5,
                    ["game:dough-amaranth"] = 5,
                    ["game:dough-sunflower"] = 5
                }
            },
            new()
            {
                Code = "game:fish-*",
                ValueByType = new()
                {
                    ["*"] = -5,
                    ["game:fish-raw"] = 15,
                    ["game:fish-cooked"] = -20,
                    ["game:fish-cured"] = -70,
                    ["game:fish-smoked"] = -80,
                    ["game:fish-cured-smoked"] = -120
                }
            },
            new()
            {
                Code = "game:grain-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "game:insect-*",
                ValueByType = new()
                {
                    ["*"] = 10
                }
            },
            new()
            {
                Code = "game:legume-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "game:pemmican-*",
                ValueByType = new()
                {
                    ["*"] = -150,
                    ["game:pemmican-raw-basic"] = -75,
                    ["game:pemmican-raw-salted"] = -120,
                    ["game:pemmican-dried-basic"] = -150,
                    ["game:pemmican-dried-salted"] = -200
                }
            },
            new()
            {
                Code = "game:pickledlegume-soybean",
                Value = 20
            },
            new()
            {
                Code = "game:pickledvegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30,
                    ["game:pickledvegetable-carrot"] = 30,
                    ["game:pickledvegetable-cabbage"] = 30,
                    ["game:pickledvegetable-onion"] = 10,
                    ["game:pickledvegetable-turnip"] = 15,
                    ["game:pickledvegetable-parsnip"] = 10,
                    ["game:pickledvegetable-pumpkin"] = 40,
                    ["game:pickledvegetable-bellpepper"] = 50,
                    ["game:pickledvegetable-olive"] = 10
                }
            },
            new()
            {
                Code = "game:poultry-*",
                ValueByType = new()
                {
                    ["*"] = -40,
                    ["game:poultry-cooked"] = -50,
                    ["game:poultry-cured"] = -70
                }
            },
            new()
            {
                Code = "game:redmeat-*",
                ValueByType = new()
                {
                    ["*"] = -50,
                    ["game:redmeat-cooked"] = -100,
                    ["game:redmeat-vintage"] = -150,
                    ["game:redmeat-cured"] = -150
                }
            },
            new()
            {
                Code = "game:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 20,
                    ["game:vegetable-carrot"] = 20,
                    ["game:vegetable-cabbage"] = 40,
                    ["game:vegetable-onion"] = 20,
                    ["game:vegetable-turnip"] = 15,
                    ["game:vegetable-parsnip"] = 15,
                    ["game:vegetable-cookedcattailroot"] = 100,
                    ["game:vegetable-pumpkin"] = 20,
                    ["game:vegetable-cassava"] = 200,
                    ["game:vegetable-cookedpapyrusroot"] = 100,
                    ["game:vegetable-bellpepper"] = 30,
                    ["game:vegetable-olive"] = 15
                }
            },
            new()
            {
                Code = "game:alcoholportion",
                Value = -40
            },
            new()
            {
                Code = "game:boilingwaterportion",
                Value = 600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-boiled-natural-clean",
                Value = 600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-boiled-rain-clean",
                Value = 800
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-fresh-distilled-clean",
                Value = 900
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-fresh-rain-clean",
                Value = 750
            },
            new()
            {
                Code = "game:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 800,
                    ["game:ciderportion-cranberry"] = 750,
                    ["game:ciderportion-blueberry"] = 800,
                    ["game:ciderportion-pinkapple"] = 800,
                    ["game:ciderportion-lychee"] = 850,
                    ["game:ciderportion-redcurrant"] = 800,
                    ["game:ciderportion-breadfruit"] = 550,
                    ["game:ciderportion-pineapple"] = 800,
                    ["game:ciderportion-blackcurrant"] = 750,
                    ["game:ciderportion-saguaro"] = 900,
                    ["game:ciderportion-whitecurrant"] = 850,
                    ["game:ciderportion-redapple"] = 800,
                    ["game:ciderportion-yellowapple"] = 800,
                    ["game:ciderportion-cherry"] = 700,
                    ["game:ciderportion-peach"] = 775,
                    ["game:ciderportion-pear"] = 825,
                    ["game:ciderportion-orange"] = 700,
                    ["game:ciderportion-mango"] = 825,
                    ["game:ciderportion-pomegranate"] = 725,
                    ["game:ciderportion-apple"] = 800,
                    ["game:ciderportion-mead"] = 700,
                    ["game:ciderportion-spelt"] = 750,
                    ["game:ciderportion-rice"] = 750,
                    ["game:ciderportion-rye"] = 750,
                    ["game:ciderportion-amaranth"] = 750,
                    ["game:ciderportion-cassava"] = 750,
                    ["game:ciderportion-redgrapes"] = 850,
                    ["game:ciderportion-greengrapes"] = 850
                }
            },
            new()
            {
                Code = "game:honeyportion",
                Value = -75
            },
            new()
            {
                Code = "game:jamhoneyportion",
                Value = -60
            },
            new()
            {
                Code = "game:saltwaterportion",
                Value = -600
            },
            new()
            {
                Code = "game:vinegarportion",
                Value = -50
            },
            new()
            {
                Code = "game:cottagecheeseportion",
                Value = 50
            },
            new()
            {
                Code = "game:milkportion",
                Value = 800
            },
            new()
            {
                Code = "game:waterportion",
                Value = 600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-fresh-well-clean",
                Value = 750
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-salt-well-clean",
                Value = -600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-fresh-well-muddy",
                Value = 600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-fresh-well-tainted",
                Value = 750
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-fresh-well-poisoned",
                Value = 750
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-salt-well-muddy",
                Value = -600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-salt-well-tainted",
                Value = -600
            },
            new()
            {
                Code = "hydrateordiedrate:waterportion-salt-well-poisoned",
                Value = -600
            },
            new()
            {
                Code = "game:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 650,
                    ["game:spiritportion-cranberry"] = 620,
                    ["game:spiritportion-blueberry"] = 660,
                    ["game:spiritportion-pinkapple"] = 680,
                    ["game:spiritportion-lychee"] = 740,
                    ["game:spiritportion-redcurrant"] = 650,
                    ["game:spiritportion-breadfruit"] = 530,
                    ["game:spiritportion-pineapple"] = 700,
                    ["game:spiritportion-blackcurrant"] = 640,
                    ["game:spiritportion-saguaro"] = 850,
                    ["game:spiritportion-whitecurrant"] = 670,
                    ["game:spiritportion-redapple"] = 700,
                    ["game:spiritportion-yellowapple"] = 700,
                    ["game:spiritportion-cherry"] = 630,
                    ["game:spiritportion-peach"] = 700,
                    ["game:spiritportion-pear"] = 750,
                    ["game:spiritportion-orange"] = 850,
                    ["game:spiritportion-mango"] = 750,
                    ["game:spiritportion-pomegranate"] = 690,
                    ["game:spiritportion-apple"] = 700,
                    ["game:spiritportion-mead"] = 500,
                    ["game:spiritportion-spelt"] = 525,
                    ["game:spiritportion-rice"] = 525,
                    ["game:spiritportion-rye"] = 525,
                    ["game:spiritportion-amaranth"] = 525,
                    ["game:spiritportion-cassava"] = 525,
                    ["game:spiritportion-redgrapes"] = 680,
                    ["game:spiritportion-greengrapes"] = 680
                }
            },
            new()
            {
                Code = "alchemy:potionportion-*",
                ValueByType = new()
                {
                    ["*"] = -200,
                    ["alchemy:potionportion-all-medium"] = -100,
                    ["alchemy:potionportion-all-strong"] = -300,
                    ["alchemy:potionportion-alltick-medium"] = -100,
                    ["alchemy:potionportion-alltick-strong"] = -300,
                    ["alchemy:potionportion-archer-medium"] = -100,
                    ["alchemy:potionportion-archer-strong"] = -300,
                    ["alchemy:potionportion-healingeffect-medium"] = -100,
                    ["alchemy:potionportion-healingeffect-strong"] = -300,
                    ["alchemy:potionportion-hungerenhance-medium"] = -100,
                    ["alchemy:potionportion-hungerenhance-strong"] = -300,
                    ["alchemy:potionportion-hungersupress-medium"] = -100,
                    ["alchemy:potionportion-hungersupress-strong"] = -300,
                    ["alchemy:potionportion-hunter-medium"] = -100,
                    ["alchemy:potionportion-hunter-strong"] = -300,
                    ["alchemy:potionportion-looter-medium"] = -100,
                    ["alchemy:potionportion-looter-strong"] = -300,
                    ["alchemy:potionportion-melee-medium"] = -100,
                    ["alchemy:potionportion-melee-strong"] = -300,
                    ["alchemy:potionportion-mining-medium"] = -100,
                    ["alchemy:potionportion-mining-strong"] = -300,
                    ["alchemy:potionportion-poison-medium"] = -100,
                    ["alchemy:potionportion-poison-strong"] = -300,
                    ["alchemy:potionportion-predator-medium"] = -100,
                    ["alchemy:potionportion-predator-strong"] = -300,
                    ["alchemy:potionportion-regen-medium"] = -100,
                    ["alchemy:potionportion-regen-strong"] = -300,
                    ["alchemy:potionportion-scentmask-medium"] = -100,
                    ["alchemy:potionportion-scentmask-strong"] = -300,
                    ["alchemy:potionportion-speed-medium"] = -100,
                    ["alchemy:potionportion-speed-strong"] = -300,
                    ["alchemy:potionportion-vitality-medium"] = -100,
                    ["alchemy:potionportion-vitality-strong"] = -300
                }
            },
            new()
            {
                Code = "alchemy:potionteaportion",
                Value = 300
            },
            new()
            {
                Code = "alchemy:utilitypotionportion-*",
                ValueByType = new()
                {
                    ["*"] = -200,
                    ["alchemy:utilitypotionportion-recall"] = -200,
                    ["alchemy:utilitypotionportion-glow"] = -200,
                    ["alchemy:utilitypotionportion-waterbreathe"] = -200,
                    ["alchemy:utilitypotionportion-nutrition"] = -200,
                    ["alchemy:utilitypotionportion-temporal"] = -200
                }
            },
            new()
            {
                Code = "butchering:bloodportion",
                Value = 50
            },
            new()
            {
                Code = "butchering:smoked-*",
                ValueByType = new()
                {
                    ["*"] = -50,
                    ["butchering:smoked-none-redmeat"] = -60,
                    ["butchering:smoked-cured-redmeat"] = -80,
                    ["butchering:smoked-none-bushmeat"] = -50,
                    ["butchering:smoked-cured-bushmeat"] = -70,
                    ["butchering:smoked-none-fish"] = -50,
                    ["butchering:smoked-cured-fish"] = -80,
                    ["butchering:smoked-none-primemeat"] = -50,
                    ["butchering:smoked-healing-primemeat"] = -80
                }
            },
            new()
            {
                Code = "butchering:sausage-*",
                ValueByType = new()
                {
                    ["*"] = -80,
                    ["butchering:sausage-bloodsausage-raw"] = -50,
                    ["butchering:sausage-bloodsausage-cooked"] = -80,
                    ["butchering:sausage-blackpudding-raw"] = -50,
                    ["butchering:sausage-blackpudding-cooked"] = -80
                }
            },
            new()
            {
                Code = "butchering:primemeat-*",
                ValueByType = new()
                {
                    ["*"] = -100,
                    ["butchering:primemeat-raw"] = -50,
                    ["butchering:primemeat-curedhealing"] = -80,
                    ["butchering:primemeat-cooked"] = -100,
                    ["butchering:primemeat-healing"] = -100
                }
            },
            new()
            {
                Code = "butchering:offal",
                Value = 3
            },
            new()
            {
                Code = "butchering:bloodbread-*",
                ValueByType = new()
                {
                    ["butchering:bloodbread-spelt-partbaked"] = -40,
                    ["butchering:bloodbread-spelt-perfect"] = -50,
                    ["butchering:bloodbread-spelt-charred"] = -80,

                    ["butchering:bloodbread-rye-partbaked"] = -40,
                    ["butchering:bloodbread-rye-perfect"] = -50,
                    ["butchering:bloodbread-rye-charred"] = -100,

                    ["butchering:bloodbread-flax-partbaked"] = -35,
                    ["butchering:bloodbread-flax-perfect"] = -40,
                    ["butchering:bloodbread-flax-charred"] = -70,

                    ["butchering:bloodbread-rice-partbaked"] = -50,
                    ["butchering:bloodbread-rice-perfect"] = -60,
                    ["butchering:bloodbread-rice-charred"] = -80,

                    ["butchering:bloodbread-cassava-partbaked"] = -40,
                    ["butchering:bloodbread-cassava-perfect"] = -50,
                    ["butchering:bloodbread-cassava-charred"] = -80,

                    ["butchering:bloodbread-amaranth-partbaked"] = -35,
                    ["butchering:bloodbread-amaranth-perfect"] = -40,
                    ["butchering:bloodbread-amaranth-charred"] = -70,

                    ["butchering:bloodbread-sunflower-partbaked"] = -40,
                    ["butchering:bloodbread-sunflower-perfect"] = -50,
                    ["butchering:bloodbread-sunflower-charred"] = -80
                }
            },
            new()
            {
                Code = "butchering:blooddough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "expandedfoods:birchsapportion",
                Value = 500
            },
            new()
            {
                Code = "expandedfoods:breadstarter",
                Value = -75
            },
            new()
            {
                Code = "expandedfoods:brothportion-*",
                ValueByType = new()
                {
                    ["*"] = 400,
                    ["expandedfoods:brothportion-bone"] = 400,
                    ["expandedfoods:brothportion-vegetable"] = 450,
                    ["expandedfoods:brothportion-meat"] = 425,
                    ["expandedfoods:brothportion-fish"] = 420
                }
            },
            new()
            {
                Code = "expandedfoods:clarifiedbrothportion-*",
                ValueByType = new()
                {
                    ["*"] = 600,
                    ["expandedfoods:clarifiedbrothportion-bone"] = 600,
                    ["expandedfoods:clarifiedbrothportion-vegetable"] = 650,
                    ["expandedfoods:clarifiedbrothportion-meat"] = 625,
                    ["expandedfoods:clarifiedbrothportion-fish"] = 620
                }
            },
            new()
            {
                Code = "expandedfoods:dressing-*",
                ValueByType = new()
                {
                    ["*"] = 50
                }
            },
            new()
            {
                Code = "expandedfoods:fishsauce",
                Value = 60
            },
            new()
            {
                Code = "expandedfoods:foodoilportion-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "expandedfoods:fruitsyrupportion-*",
                ValueByType = new()
                {
                    ["*"] = 100
                }
            },
            new()
            {
                Code = "expandedfoods:lard",
                Value = -5
            },
            new()
            {
                Code = "expandedfoods:maplesapportion",
                Value = 600
            },
            new()
            {
                Code = "expandedfoods:pasteurizedmilkportion",
                Value = 800
            },
            new()
            {
                Code = "expandedfoods:peanutliquid-*",
                ValueByType = new()
                {
                    ["*"] = -5,
                    ["expandedfoods:peanutliquid-paste"] = -5,
                    ["expandedfoods:peanutliquid-butter"] = -30,
                    ["expandedfoods:peanutliquid-sauce"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:potentspiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 470,
                    ["expandedfoods:potentspiritportion-apple"] = 470,
                    ["expandedfoods:potentspiritportion-mead"] = 480,
                    ["expandedfoods:potentspiritportion-spelt"] = 475,
                    ["expandedfoods:potentspiritportion-rice"] = 475,
                    ["expandedfoods:potentspiritportion-rye"] = 475,
                    ["expandedfoods:potentspiritportion-amaranth"] = 475,
                    ["expandedfoods:potentspiritportion-cassava"] = 475
                }
            },
            new()
            {
                Code = "expandedfoods:potentwineportion-*",
                ValueByType = new()
                {
                    ["*"] = 160,
                    ["expandedfoods:potentwineportion-apple"] = 160,
                    ["expandedfoods:potentwineportion-mead"] = 170,
                    ["expandedfoods:potentwineportion-spelt"] = 165,
                    ["expandedfoods:potentwineportion-rice"] = 165,
                    ["expandedfoods:potentwineportion-rye"] = 165,
                    ["expandedfoods:potentwineportion-amaranth"] = 165,
                    ["expandedfoods:potentwineportion-cassava"] = 165
                }
            },
            new()
            {
                Code = "expandedfoods:softresin",
                Value = -10
            },
            new()
            {
                Code = "expandedfoods:soulstormbrew-*",
                ValueByType = new()
                {
                    ["*"] = 100,
                    ["expandedfoods:soulstormbrew-slop"] = 100,
                    ["expandedfoods:soulstormbrew-refinedslop"] = 150,
                    ["expandedfoods:soulstormbrew-basic"] = 200
                }
            },
            new()
            {
                Code = "expandedfoods:soymilk-*",
                ValueByType = new()
                {
                    ["*"] = 600,
                    ["expandedfoods:soymilk-raw"] = 600,
                    ["expandedfoods:soymilk-edible"] = 700
                }
            },
            new()
            {
                Code = "expandedfoods:soysauce",
                Value = -20
            },
            new()
            {
                Code = "expandedfoods:strongspiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 450,
                    ["expandedfoods:strongspiritportion-apple"] = 450,
                    ["expandedfoods:strongspiritportion-mead"] = 460,
                    ["expandedfoods:strongspiritportion-spelt"] = 455,
                    ["expandedfoods:strongspiritportion-rice"] = 455,
                    ["expandedfoods:strongspiritportion-rye"] = 455,
                    ["expandedfoods:strongspiritportion-amaranth"] = 455,
                    ["expandedfoods:strongspiritportion-cassava"] = 455
                }
            },
            new()
            {
                Code = "expandedfoods:strongwineportion-*",
                ValueByType = new()
                {
                    ["*"] = 150,
                    ["expandedfoods:strongwineportion-apple"] = 450,
                    ["expandedfoods:strongwineportion-mead"] = 460,
                    ["expandedfoods:strongwineportion-spelt"] = 455,
                    ["expandedfoods:strongwineportion-rice"] = 455,
                    ["expandedfoods:strongwineportion-rye"] = 455,
                    ["expandedfoods:strongwineportion-amaranth"] = 455,
                    ["expandedfoods:strongwineportion-cassava"] = 455
                }
            },
            new()
            {
                Code = "expandedfoods:treesyrupportion-*",
                ValueByType = new()
                {
                    ["*"] = -20
                }
            },
            new()
            {
                Code = "expandedfoods:vegetablejuiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 150,
                    ["expandedfoods:vegetablejuiceportion-carrot"] = 200,
                    ["expandedfoods:vegetablejuiceportion-cabbage"] = 300,
                    ["expandedfoods:vegetablejuiceportion-onion"] = 150,
                    ["expandedfoods:vegetablejuiceportion-turnip"] = 150,
                    ["expandedfoods:vegetablejuiceportion-parsnip"] = 150,
                    ["expandedfoods:vegetablejuiceportion-pumpkin"] = 150,
                    ["expandedfoods:vegetablejuiceportion-cassava"] = 150,
                    ["expandedfoods:vegetablejuiceportion-bellpepper"] = 150
                }
            },
            new()
            {
                Code = "expandedfoods:yeastwaterportion",
                Value = 50
            },
            new()
            {
                Code = "expandedfoods:yogurt-*",
                ValueByType = new()
                {
                    ["*"] = 50
                }
            },
            new()
            {
                Code = "expandedfoods:acorncoffee",
                Value = 75
            },
            new()
            {
                Code = "expandedfoods:wildfruitsyrupportion-*",
                ValueByType = new()
                {
                    ["*"] = 50
                }
            },
            new()
            {
                Code = "expandedfoods:wildpotentspiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 450
                }
            },
            new()
            {
                Code = "expandedfoods:wildpotentwineportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "expandedfoods:wildstrongspiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 450
                }
            },
            new()
            {
                Code = "expandedfoods:wildstrongwineportion-*",
                ValueByType = new()
                {
                    ["*"] = 450
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreefruitsyrupportion-*",
                ValueByType = new()
                {
                    ["*"] = 50
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreepotentspiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 450
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreepotentwineportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreestrongspiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 450
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreestrongwineportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "expandedfoods:agedmeat-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:breadcrumbs-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:candiedfruit-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "expandedfoods:choppedmushroom-*",
                ValueByType = new()
                {
                    ["*"] = -5,
                    ["expandedfoods:choppedmushroom-earthball"] = -4,
                    ["expandedfoods:choppedmushroom-deathcap"] = -25,
                    ["expandedfoods:choppedmushroom-funeralbell"] = -20,
                    ["expandedfoods:choppedmushroom-elfinsaddle"] = -3,
                    ["expandedfoods:choppedmushroom-jackolantern"] = -3,
                    ["expandedfoods:choppedmushroom-devilbolete"] = -5,
                    ["expandedfoods:choppedmushroom-pinkbonnet"] = -10,
                    ["expandedfoods:choppedmushroom-flyagaric"] = -3,
                    ["expandedfoods:choppedmushroom-bitterbolete"] = -1,
                    ["expandedfoods:choppedmushroom-devilstooth"] = -1,
                    ["expandedfoods:choppedmushroom-golddropmilkcap"] = -1
                }
            },
            new()
            {
                Code = "expandedfoods:choppedvegetable-*",
                ValueByType = new()
                {
                    ["*"] = 5,
                    ["expandedfoods:choppedvegetable-cassavadried"] = 5,
                    ["expandedfoods:choppedvegetable-cabbage"] = 8,
                    ["expandedfoods:choppedvegetable-pumpkin"] = 7,
                    ["expandedfoods:choppedvegetable-pickledcarrot"] = 6,
                    ["expandedfoods:choppedvegetable-pickledonion"] = 6,
                    ["expandedfoods:choppedvegetable-pickledparsnip"] = 6,
                    ["expandedfoods:choppedvegetable-pickledturnip"] = 6,
                    ["expandedfoods:choppedvegetable-pickledpumpkin"] = 6,
                    ["expandedfoods:choppedvegetable-papyrusroot"] = 7,
                    ["expandedfoods:choppedvegetable-cattailroot"] = 100,
                    ["expandedfoods:choppedvegetable-bellpepper"] = 7
                }
            },
            new()
            {
                Code = "expandedfoods:cookedchoppedmushroom-*",
                ValueByType = new()
                {
                    ["*"] = -1
                }
            },
            new()
            {
                Code = "expandedfoods:cookedchoppedvegetable-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "expandedfoods:cookedmushroom-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:cookedveggie-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:dehydratedfruit-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:dryfruit-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:driedseaweed-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:fermentedfish-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:fishnugget-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:gelatin-*",
                ValueByType = new()
                {
                    ["*"] = 1
                }
            },
            new()
            {
                Code = "expandedfoods:gelatinfish-*",
                ValueByType = new()
                {
                    ["*"] = 1
                }
            },
            new()
            {
                Code = "expandedfoods:limeegg-*",
                ValueByType = new()
                {
                    ["*"] = 1
                }
            },
            new()
            {
                Code = "expandedfoods:meatnugget-*",
                ValueByType = new()
                {
                    ["*"] = -1
                }
            },
            new()
            {
                Code = "expandedfoods:peanut-*",
                ValueByType = new()
                {
                    ["*"] = -3
                }
            },
            new()
            {
                Code = "expandedfoods:soyprep-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "expandedfoods:acornbreadcrumbs-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:acornportion-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:acornpowdered-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:acorns-roasted-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:acornberrybread-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornbreadedball-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornbreadedfishnugget-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornbreadedmeatnugget-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornbreadedmushroom-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornbreadedvegetable-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acorndoughball-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:acorndumpling-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:acornhardtack-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:acornmuffin-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornpasta-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:acornstuffedpepper-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:saltedmeatnugget-*",
                ValueByType = new()
                {
                    ["*"] = -15
                }
            },
            new()
            {
                Code = "expandedfoods:berrybread-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:breadedball-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:breadedfishnugget-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:breadedmeatnugget-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:breadedmushroom-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:breadedvegetable-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:candy-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:dumpling-*",
                ValueByType = new()
                {
                    ["*"] = -3
                }
            },
            new()
            {
                Code = "expandedfoods:fruitbar-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:gozinaki-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:hardtack-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:muffin-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:pasta-*",
                ValueByType = new()
                {
                    ["*"] = -3
                }
            },
            new()
            {
                Code = "expandedfoods:pemmican-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:pemmicanfish-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:plaindoughball-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sausage-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sausagefish-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:stuffedpepper-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sushi-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "expandedfoods:sushiveg-*",
                ValueByType = new()
                {
                    ["*"] = -1
                }
            },
            new()
            {
                Code = "expandedfoods:trailmix-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:trailmixvegetarian-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:crabnugget-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:snakenugget-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "expandedfoods:pemmicancrab-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:pemmicansnake-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:sausagecrab-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sausagesnake-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:herbalcornbreadcrumbs-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:herbalbreadcrumbs-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:wildcandiedfruit-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "expandedfoods:wilddehydratedfruit-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:wilddryfruit-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:gozinakiherb-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:herbalacornbread-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:herbalacornhardtack-*",
                ValueByType = new()
                {
                    ["*"] = -25
                }
            },
            new()
            {
                Code = "expandedfoods:herbalabread-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:sausagecrabherb-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sausagefishherb-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sausageherb-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:sausagesnakeherb-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreecandiedfruit-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreedehydratedfruit-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "expandedfoods:wildtreedryfruit-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "floralzonescaperegion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40
                }
            },
            new()
            {
                Code = "floralzonescaperegion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 35
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:vegetable-nonpalm-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 50
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:flour-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:bread-*",
                ValueByType = new()
                {
                    ["*-partbaked"] = -40,
                    ["*-perfect"] = -50,
                    ["*-charred"] = -80
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 1100
                }
            },
            new()
            {
                Code = "floralzonescaribbeanregion:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 750
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:legume-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:grain-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:flour-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:bread-*",
                ValueByType = new()
                {
                    ["*-partbaked"] = -40,
                    ["*-perfect"] = -50,
                    ["*-charred"] = -80
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 750
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 1100
                }
            },
            new()
            {
                Code = "floralzonescentralaustralianregion:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:rawkudzu-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:flour-*",
                ValueByType = new()
                {
                    ["*"] = -8
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:bread-*",
                ValueByType = new()
                {
                    ["*-partbaked"] = -40,
                    ["*-perfect"] = -50,
                    ["*-charred"] = -80
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 1100
                }
            },
            new()
            {
                Code = "floralzoneseastasiaticregion:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 750
                }
            },
            new()
            {
                Code = "floralzonesneozeylandicregion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40
                }
            },
            new()
            {
                Code = "floralzonesneozeylandicregion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:pickledvegetable-*",
                ValueByType = new()
                {
                    ["*"] = 15
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:legume-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:grain-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:flour-*",
                ValueByType = new()
                {
                    ["*"] = -30
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:bread-*",
                ValueByType = new()
                {
                    ["*-partbaked"] = -40,
                    ["*-perfect"] = -50,
                    ["*-charred"] = -80
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 750
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 1100
                }
            },
            new()
            {
                Code = "floralzonescosmopolitanregion:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "floralzonesmediterraneanregion:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "floralzonesmediterraneanregion:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40
                }
            },
            new()
            {
                Code = "floralzonesmediterraneanregion:pickledvegetable-*",
                ValueByType = new()
                {
                    ["*"] = 15
                }
            },
            new()
            {
                Code = "floralzonesmediterraneanregion:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 750
                }
            },
            new()
            {
                Code = "floralzonesmediterraneanregion:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 1100
                }
            },
            new()
            {
                Code = "floralzonesmediterraneanregion:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "newworldcrops:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "newworldcrops:pickledlegume-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "newworldcrops:pickledlegrain-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "newworldcrops:legume-*",
                ValueByType = new()
                {
                    ["*"] = -3
                }
            },
            new()
            {
                Code = "newworldcrops:grain-*",
                ValueByType = new()
                {
                    ["*"] = -20
                }
            },
            new()
            {
                Code = "newworldcrops:flour-*",
                ValueByType = new()
                {
                    ["*"] = -8
                }
            },
            new()
            {
                Code = "newworldcrops:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "newworldcrops:bread-*",
                ValueByType = new()
                {
                    ["*-partbaked"] = -40,
                    ["*-perfect"] = -50,
                    ["*-charred"] = -80
                }
            },
            new()
            {
                Code = "warriordrink:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 400
                }
            },
            new()
            {
                Code = "warriordrink:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 300
                }
            },
            new()
            {
                Code = "wildcraftfruit:vegetable-*",
                ValueByType = new()
                {
                    ["*"] = 30
                }
            },
            new()
            {
                Code = "wildcraftfruit:nut-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "wildcraftfruit:fruit-*",
                ValueByType = new()
                {
                    ["*"] = 40,

                    ["wildcraftfruit:fruit-watermelon"] = 60,
                    ["wildcraftfruit:fruit-illawarra"] = 35,
                    ["wildcraftfruit:fruit-creepingpine"] = 30,
                    ["wildcraftfruit:fruit-pandanbits"] = 35,
                    ["wildcraftfruit:fruit-maritabits"] = 35,
                    ["wildcraftfruit:fruit-crowseye"] = 28,
                    ["wildcraftfruit:fruit-kakaha"] = 40,
                    ["wildcraftfruit:fruit-flaxlily"] = 35,
                    ["wildcraftfruit:fruit-engkala"] = 40,
                    ["wildcraftfruit:fruit-kawakawa"] = 35,
                    ["wildcraftfruit:fruit-gooseberry"] = 35,
                    ["wildcraftfruit:fruit-blackgrape"] = 40,
                    ["wildcraftfruit:fruit-redgrape"] = 40,
                    ["wildcraftfruit:fruit-whitegrape"] = 40,
                    ["wildcraftfruit:fruit-foxgrape"] = 38,
                    ["wildcraftfruit:fruit-virgingrape"] = 36,
                    ["wildcraftfruit:fruit-gardenstrawberry"] = 40,
                    ["wildcraftfruit:fruit-strawberry"] = 40,
                    ["wildcraftfruit:fruit-falsestrawberry"] = 30,
                    ["wildcraftfruit:fruit-raspberry"] = 40,
                    ["wildcraftfruit:fruit-blueraspberry"] = 40,
                    ["wildcraftfruit:fruit-brambleberry"] = 36,
                    ["wildcraftfruit:fruit-cloudberry"] = 35,
                    ["wildcraftfruit:fruit-knyazberry"] = 42,
                    ["wildcraftfruit:fruit-bushlawyer"] = 34,
                    ["wildcraftfruit:fruit-dogrose"] = 32,
                    ["wildcraftfruit:fruit-hawthorn"] = 32,
                    ["wildcraftfruit:fruit-rowanberry"] = 32,
                    ["wildcraftfruit:fruit-quince"] = 38,
                    ["wildcraftfruit:fruit-loquat"] = 36,
                    ["wildcraftfruit:fruit-pittedcherry"] = 40,
                    ["wildcraftfruit:fruit-apricot"] = 40,
                    ["wildcraftfruit:fruit-pittedapricot"] = 40,
                    ["wildcraftfruit:fruit-purpleplum"] = 40,
                    ["wildcraftfruit:fruit-cherryplum"] = 38,
                    ["wildcraftfruit:fruit-sallowthorn"] = 30,
                    ["wildcraftfruit:fruit-jujube"] = 35,
                    ["wildcraftfruit:fruit-fig"] = 38,
                    ["wildcraftfruit:fruit-falseorange"] = 40,
                    ["wildcraftfruit:fruit-pittedbreadfruit"] = 20,
                    ["wildcraftfruit:fruit-silvernettle"] = 28,
                    ["wildcraftfruit:fruit-cucumber"] = 50,
                    ["wildcraftfruit:fruit-muskmelon"] = 55,
                    ["wildcraftfruit:fruit-mirzamelon"] = 55,
                    ["wildcraftfruit:fruit-bryony"] = 28,
                    ["wildcraftfruit:fruit-passionfruit"] = 42,
                    ["wildcraftfruit:fruit-achacha"] = 38,
                    ["wildcraftfruit:fruit-spindle"] = 28,
                    ["wildcraftfruit:fruit-ugni"] = 34,
                    ["wildcraftfruit:fruit-midyimberry"] = 36,
                    ["wildcraftfruit:fruit-munthari"] = 34,
                    ["wildcraftfruit:fruit-guajava"] = 40,
                    ["wildcraftfruit:fruit-feijoa"] = 40,
                    ["wildcraftfruit:fruit-roseapple"] = 45,
                    ["wildcraftfruit:fruit-lillypillypink"] = 38,
                    ["wildcraftfruit:fruit-lillypillyblue"] = 38,
                    ["wildcraftfruit:fruit-lillypillywhite"] = 38,
                    ["wildcraftfruit:fruit-beachalmondwhole"] = 20,
                    ["wildcraftfruit:fruit-pittedbeachalmond"] = 20,
                    ["wildcraftfruit:fruit-bluetongue"] = 36,
                    ["wildcraftfruit:fruit-turkscap"] = 32,
                    ["wildcraftfruit:fruit-cocoa"] = 25,
                    ["wildcraftfruit:fruit-wolfberry"] = 32,
                    ["wildcraftfruit:fruit-caperberry"] = 30,
                    ["wildcraftfruit:fruit-citron"] = 35,
                    ["wildcraftfruit:fruit-lemon"] = 35,
                    ["wildcraftfruit:fruit-pomelo"] = 40,
                    ["wildcraftfruit:fruit-fingerlime"] = 35,
                    ["wildcraftfruit:fruit-kumquat"] = 30,
                    ["wildcraftfruit:fruit-lemonaspen"] = 35,
                    ["wildcraftfruit:fruit-cashewwhole"] = 15,
                    ["wildcraftfruit:fruit-cashewapple"] = 45,
                    ["wildcraftfruit:fruit-chinaberry"] = 20,
                    ["wildcraftfruit:fruit-pittedchinaberry"] = 20,
                    ["wildcraftfruit:fruit-redquandong"] = 38,
                    ["wildcraftfruit:fruit-rubysaltbush"] = 30,
                    ["wildcraftfruit:fruit-pokeberry"] = 20,
                    ["wildcraftfruit:fruit-bunchberry"] = 35,
                    ["wildcraftfruit:fruit-cheeseberry"] = 35,
                    ["wildcraftfruit:fruit-pineheath"] = 28,
                    ["wildcraftfruit:fruit-pricklyheath"] = 28,
                    ["wildcraftfruit:fruit-honeypots"] = 30,
                    ["wildcraftfruit:fruit-crowberry"] = 32,
                    ["wildcraftfruit:fruit-lingonberry"] = 35,
                    ["wildcraftfruit:fruit-huckleberry"] = 38,
                    ["wildcraftfruit:fruit-kiwi"] = 40,
                    ["wildcraftfruit:fruit-honeysuckle"] = 30,
                    ["wildcraftfruit:fruit-snowberry"] = 25,
                    ["wildcraftfruit:fruit-blackelder"] = 30,
                    ["wildcraftfruit:fruit-elderberry"] = 35,
                    ["wildcraftfruit:fruit-ivy"] = 20,
                    ["wildcraftfruit:fruit-blacknightshade"] = 22,
                    ["wildcraftfruit:fruit-blacknightshadeunripe"] = 18,
                    ["wildcraftfruit:fruit-bitternightshade"] = 18,
                    ["wildcraftfruit:fruit-naranjilla"] = 38,
                    ["wildcraftfruit:fruit-husktomato"] = 35,
                    ["wildcraftfruit:fruit-beautyberry"] = 34,
                    ["wildcraftfruit:fruit-numnum"] = 34,
                    ["wildcraftfruit:fruit-seamango"] = 38,
                    ["wildcraftfruit:fruit-coralbead"] = 20,
                    ["wildcraftfruit:fruit-pilo"] = 32,
                    ["wildcraftfruit:fruit-mingimingi"] = 32,
                    ["wildcraftfruit:fruit-fractureberry"] = 30
                }
            },
            new()
            {
                Code = "wildcraftfruit:flour-*",
                ValueByType = new()
                {
                    ["*"] = -8
                }
            },
            new()
            {
                Code = "wildcraftfruit:dough-*",
                ValueByType = new()
                {
                    ["*"] = 5
                }
            },
            new()
            {
                Code = "wildcraftfruit:bread-*",
                ValueByType = new()
                {
                    ["*"] = -50,
                    ["*-partbaked"] = -40,
                    ["*-perfect"] = -50,
                    ["*-charred"] = -80
                }
            },
            new()
            {
                Code = "wildcraftfruit:berrymush-*",
                ValueByType = new()
                {
                    ["*"] = 20
                }
            },
            new()
            {
                Code = "wildcraftfruit:spiritportion-*",
                ValueByType = new()
                {
                    ["*"] = 650,
                    ["wildcraftfruit:spiritportion-juniper"] = 680,
                    ["wildcraftfruit:spiritportion-avocado"] = 520,
                    ["wildcraftfruit:spiritportion-sumac"] = 720,
                    ["wildcraftfruit:spiritportion-illawarra"] = 650,
                    ["wildcraftfruit:spiritportion-creepingpine"] = 630,
                    ["wildcraftfruit:spiritportion-pandanbits"] = 660,
                    ["wildcraftfruit:spiritportion-maritabits"] = 660,
                    ["wildcraftfruit:spiritportion-crowseye"] = 630,
                    ["wildcraftfruit:spiritportion-kakaha"] = 660,
                    ["wildcraftfruit:spiritportion-flaxlily"] = 650,
                    ["wildcraftfruit:spiritportion-engkala"] = 670,
                    ["wildcraftfruit:spiritportion-kawakawa"] = 650,
                    ["wildcraftfruit:spiritportion-baneberry"] = 600,
                    ["wildcraftfruit:spiritportion-gooseberry"] = 660,
                    ["wildcraftfruit:spiritportion-blackgrape"] = 680,
                    ["wildcraftfruit:spiritportion-redgrape"] = 680,
                    ["wildcraftfruit:spiritportion-whitegrape"] = 680,
                    ["wildcraftfruit:spiritportion-foxgrape"] = 670,
                    ["wildcraftfruit:spiritportion-virgingrape"] = 670,
                    ["wildcraftfruit:spiritportion-strawberry"] = 650,
                    ["wildcraftfruit:spiritportion-gardenstrawberry"] = 650,
                    ["wildcraftfruit:spiritportion-falsestrawberry"] = 630,
                    ["wildcraftfruit:spiritportion-raspberry"] = 660,
                    ["wildcraftfruit:spiritportion-blueraspberry"] = 660,
                    ["wildcraftfruit:spiritportion-brambleberry"] = 650,
                    ["wildcraftfruit:spiritportion-dewberry"] = 640,
                    ["wildcraftfruit:spiritportion-cloudberry"] = 660,
                    ["wildcraftfruit:spiritportion-knyazberry"] = 690,
                    ["wildcraftfruit:spiritportion-bushlawyer"] = 640,
                    ["wildcraftfruit:spiritportion-dogrose"] = 640,
                    ["wildcraftfruit:spiritportion-hawthorn"] = 630,
                    ["wildcraftfruit:spiritportion-rowanberry"] = 630,
                    ["wildcraftfruit:spiritportion-sorb"] = 650,
                    ["wildcraftfruit:spiritportion-aronia"] = 630,
                    ["wildcraftfruit:spiritportion-crabapple"] = 700,
                    ["wildcraftfruit:spiritportion-sandpear"] = 750,
                    ["wildcraftfruit:spiritportion-quince"] = 690,
                    ["wildcraftfruit:spiritportion-loquat"] = 700,
                    ["wildcraftfruit:spiritportion-apricot"] = 700,
                    ["wildcraftfruit:spiritportion-sloe"] = 630,
                    ["wildcraftfruit:spiritportion-purpleplum"] = 700,
                    ["wildcraftfruit:spiritportion-greengage"] = 700,
                    ["wildcraftfruit:spiritportion-cherryplum"] = 690,
                    ["wildcraftfruit:spiritportion-sallowthorn"] = 620,
                    ["wildcraftfruit:spiritportion-jujube"] = 640,
                    ["wildcraftfruit:spiritportion-fig"] = 700,
                    ["wildcraftfruit:spiritportion-falseorange"] = 720,
                    ["wildcraftfruit:spiritportion-jackfruit"] = 660,
                    ["wildcraftfruit:spiritportion-cempedak"] = 660,
                    ["wildcraftfruit:spiritportion-silvernettle"] = 630,
                    ["wildcraftfruit:spiritportion-cucumber"] = 700,
                    ["wildcraftfruit:spiritportion-muskmelon"] = 800,
                    ["wildcraftfruit:spiritportion-honeydewmelon"] = 800,
                    ["wildcraftfruit:spiritportion-mirzamelon"] = 800,
                    ["wildcraftfruit:spiritportion-watermelon"] = 800,
                    ["wildcraftfruit:spiritportion-bryony"] = 600,
                    ["wildcraftfruit:spiritportion-passionfruit"] = 690,
                    ["wildcraftfruit:spiritportion-granadilla"] = 690,
                    ["wildcraftfruit:spiritportion-achacha"] = 690,
                    ["wildcraftfruit:spiritportion-spindle"] = 600,
                    ["wildcraftfruit:spiritportion-ugni"] = 650,
                    ["wildcraftfruit:spiritportion-midyimberry"] = 650,
                    ["wildcraftfruit:spiritportion-munthari"] = 650,
                    ["wildcraftfruit:spiritportion-guajava"] = 700,
                    ["wildcraftfruit:spiritportion-feijoa"] = 700,
                    ["wildcraftfruit:spiritportion-roseapple"] = 720,
                    ["wildcraftfruit:spiritportion-bluetongue"] = 660,
                    ["wildcraftfruit:spiritportion-turkscap"] = 630,
                    ["wildcraftfruit:spiritportion-cocoa"] = 650,
                    ["wildcraftfruit:spiritportion-wolfberry"] = 630,
                    ["wildcraftfruit:spiritportion-caperberry"] = 630,
                    ["wildcraftfruit:spiritportion-rambutan"] = 700,
                    ["wildcraftfruit:spiritportion-citron"] = 720,
                    ["wildcraftfruit:spiritportion-pomelo"] = 850,
                    ["wildcraftfruit:spiritportion-grapefruit"] = 850,
                    ["wildcraftfruit:spiritportion-lime"] = 750,
                    ["wildcraftfruit:spiritportion-fingerlime"] = 750,
                    ["wildcraftfruit:spiritportion-kumquat"] = 700,
                    ["wildcraftfruit:spiritportion-lemonaspen"] = 720,
                    ["wildcraftfruit:spiritportion-cashewapple"] = 800,
                    ["wildcraftfruit:spiritportion-wani"] = 750,
                    ["wildcraftfruit:spiritportion-redquandong"] = 690,
                    ["wildcraftfruit:spiritportion-rubysaltbush"] = 630,
                    ["wildcraftfruit:spiritportion-pokeberry"] = 600,
                    ["wildcraftfruit:spiritportion-bunchberry"] = 650,
                    ["wildcraftfruit:spiritportion-bearberry"] = 630,
                    ["wildcraftfruit:spiritportion-cheeseberry"] = 650,
                    ["wildcraftfruit:spiritportion-pineheath"] = 630,
                    ["wildcraftfruit:spiritportion-pricklyheath"] = 630,
                    ["wildcraftfruit:spiritportion-honeypots"] = 630,
                    ["wildcraftfruit:spiritportion-crowberry"] = 630,
                    ["wildcraftfruit:spiritportion-lingonberry"] = 650,
                    ["wildcraftfruit:spiritportion-huckleberry"] = 660,
                    ["wildcraftfruit:spiritportion-kiwi"] = 700,
                    ["wildcraftfruit:spiritportion-kolomikta"] = 690,
                    ["wildcraftfruit:spiritportion-woodbine"] = 650,
                    ["wildcraftfruit:spiritportion-honeysuckle"] = 600,
                    ["wildcraftfruit:spiritportion-snowberry"] = 600,
                    ["wildcraftfruit:spiritportion-blackelder"] = 670,
                    ["wildcraftfruit:spiritportion-elderberry"] = 670,
                    ["wildcraftfruit:spiritportion-ivy"] = 520,
                    ["wildcraftfruit:spiritportion-hairyappleberry"] = 660,
                    ["wildcraftfruit:spiritportion-purpleappleberry"] = 660,
                    ["wildcraftfruit:spiritportion-blacknightshade"] = 620,
                    ["wildcraftfruit:spiritportion-naranjilla"] = 750,
                    ["wildcraftfruit:spiritportion-belladonna"] = 500,
                    ["wildcraftfruit:spiritportion-husktomato"] = 660,
                    ["wildcraftfruit:spiritportion-beautyberry"] = 640,
                    ["wildcraftfruit:spiritportion-numnum"] = 650,
                    ["wildcraftfruit:spiritportion-seamango"] = 600,
                    ["wildcraftfruit:spiritportion-coralbead"] = 500,
                    ["wildcraftfruit:spiritportion-pilo"] = 650,
                    ["wildcraftfruit:spiritportion-mingimingi"] = 650,
                    ["wildcraftfruit:spiritportion-fractureberry"] = 640
                }
            },
            new()
        {
            Code = "wildcraftfruit:finespiritportion-*",
            ValueByType = new()
            {
                ["*"] = 850,
                ["wildcraftfruit:finespiritportion-juniper"] = 880,
                ["wildcraftfruit:finespiritportion-avocado"] = 720,
                ["wildcraftfruit:finespiritportion-sumac"] = 920,
                ["wildcraftfruit:finespiritportion-illawarra"] = 850,
                ["wildcraftfruit:finespiritportion-creepingpine"] = 830,
                ["wildcraftfruit:finespiritportion-pandanbits"] = 860,
                ["wildcraftfruit:finespiritportion-maritabits"] = 860,
                ["wildcraftfruit:finespiritportion-crowseye"] = 830,
                ["wildcraftfruit:finespiritportion-kakaha"] = 860,
                ["wildcraftfruit:finespiritportion-flaxlily"] = 850,
                ["wildcraftfruit:finespiritportion-engkala"] = 870,
                ["wildcraftfruit:finespiritportion-kawakawa"] = 850,
                ["wildcraftfruit:finespiritportion-baneberry"] = 800,
                ["wildcraftfruit:finespiritportion-gooseberry"] = 860,
                ["wildcraftfruit:finespiritportion-blackgrape"] = 880,
                ["wildcraftfruit:finespiritportion-redgrape"] = 880,
                ["wildcraftfruit:finespiritportion-whitegrape"] = 880,
                ["wildcraftfruit:finespiritportion-foxgrape"] = 870,
                ["wildcraftfruit:finespiritportion-virgingrape"] = 870,
                ["wildcraftfruit:finespiritportion-strawberry"] = 850,
                ["wildcraftfruit:finespiritportion-gardenstrawberry"] = 850,
                ["wildcraftfruit:finespiritportion-falsestrawberry"] = 830,
                ["wildcraftfruit:finespiritportion-raspberry"] = 860,
                ["wildcraftfruit:finespiritportion-blueraspberry"] = 860,
                ["wildcraftfruit:finespiritportion-brambleberry"] = 850,
                ["wildcraftfruit:finespiritportion-dewberry"] = 840,
                ["wildcraftfruit:finespiritportion-cloudberry"] = 860,
                ["wildcraftfruit:finespiritportion-knyazberry"] = 890,
                ["wildcraftfruit:finespiritportion-bushlawyer"] = 840,
                ["wildcraftfruit:finespiritportion-dogrose"] = 840,
                ["wildcraftfruit:finespiritportion-hawthorn"] = 830,
                ["wildcraftfruit:finespiritportion-rowanberry"] = 830,
                ["wildcraftfruit:finespiritportion-sorb"] = 850,
                ["wildcraftfruit:finespiritportion-aronia"] = 830,
                ["wildcraftfruit:finespiritportion-crabapple"] = 900,
                ["wildcraftfruit:finespiritportion-sandpear"] = 950,
                ["wildcraftfruit:finespiritportion-quince"] = 890,
                ["wildcraftfruit:finespiritportion-loquat"] = 900,
                ["wildcraftfruit:finespiritportion-apricot"] = 900,
                ["wildcraftfruit:finespiritportion-sloe"] = 830,
                ["wildcraftfruit:finespiritportion-purpleplum"] = 900,
                ["wildcraftfruit:finespiritportion-greengage"] = 900,
                ["wildcraftfruit:finespiritportion-cherryplum"] = 890,
                ["wildcraftfruit:finespiritportion-sallowthorn"] = 820,
                ["wildcraftfruit:finespiritportion-jujube"] = 840,
                ["wildcraftfruit:finespiritportion-fig"] = 900,
                ["wildcraftfruit:finespiritportion-falseorange"] = 920,
                ["wildcraftfruit:finespiritportion-jackfruit"] = 860,
                ["wildcraftfruit:finespiritportion-cempedak"] = 860,
                ["wildcraftfruit:finespiritportion-silvernettle"] = 830,
                ["wildcraftfruit:finespiritportion-cucumber"] = 900,
                ["wildcraftfruit:finespiritportion-muskmelon"] = 1000,
                ["wildcraftfruit:finespiritportion-honeydewmelon"] = 1000,
                ["wildcraftfruit:finespiritportion-mirzamelon"] = 1000,
                ["wildcraftfruit:finespiritportion-watermelon"] = 1000,
                ["wildcraftfruit:finespiritportion-bryony"] = 800,
                ["wildcraftfruit:finespiritportion-passionfruit"] = 890,
                ["wildcraftfruit:finespiritportion-granadilla"] = 890,
                ["wildcraftfruit:finespiritportion-achacha"] = 890,
                ["wildcraftfruit:finespiritportion-spindle"] = 800,
                ["wildcraftfruit:finespiritportion-ugni"] = 850,
                ["wildcraftfruit:finespiritportion-midyimberry"] = 850,
                ["wildcraftfruit:finespiritportion-munthari"] = 850,
                ["wildcraftfruit:finespiritportion-guajava"] = 900,
                ["wildcraftfruit:finespiritportion-feijoa"] = 900,
                ["wildcraftfruit:finespiritportion-roseapple"] = 920,
                ["wildcraftfruit:finespiritportion-bluetongue"] = 860,
                ["wildcraftfruit:finespiritportion-turkscap"] = 830,
                ["wildcraftfruit:finespiritportion-cocoa"] = 850,
                ["wildcraftfruit:finespiritportion-wolfberry"] = 830,
                ["wildcraftfruit:finespiritportion-caperberry"] = 830,
                ["wildcraftfruit:finespiritportion-rambutan"] = 900,
                ["wildcraftfruit:finespiritportion-citron"] = 920,
                ["wildcraftfruit:finespiritportion-pomelo"] = 1050,
                ["wildcraftfruit:finespiritportion-grapefruit"] = 1050,
                ["wildcraftfruit:finespiritportion-lime"] = 950,
                ["wildcraftfruit:finespiritportion-fingerlime"] = 950,
                ["wildcraftfruit:finespiritportion-kumquat"] = 900,
                ["wildcraftfruit:finespiritportion-lemonaspen"] = 920,
                ["wildcraftfruit:finespiritportion-cashewapple"] = 1000,
                ["wildcraftfruit:finespiritportion-wani"] = 950,
                ["wildcraftfruit:finespiritportion-redquandong"] = 890,
                ["wildcraftfruit:finespiritportion-rubysaltbush"] = 830,
                ["wildcraftfruit:finespiritportion-pokeberry"] = 800,
                ["wildcraftfruit:finespiritportion-bunchberry"] = 850,
                ["wildcraftfruit:finespiritportion-bearberry"] = 830,
                ["wildcraftfruit:finespiritportion-cheeseberry"] = 850,
                ["wildcraftfruit:finespiritportion-pineheath"] = 830,
                ["wildcraftfruit:finespiritportion-pricklyheath"] = 830,
                ["wildcraftfruit:finespiritportion-honeypots"] = 830,
                ["wildcraftfruit:finespiritportion-crowberry"] = 830,
                ["wildcraftfruit:finespiritportion-lingonberry"] = 850,
                ["wildcraftfruit:finespiritportion-huckleberry"] = 860,
                ["wildcraftfruit:finespiritportion-kiwi"] = 900,
                ["wildcraftfruit:finespiritportion-kolomikta"] = 890,
                ["wildcraftfruit:finespiritportion-woodbine"] = 850,
                ["wildcraftfruit:finespiritportion-honeysuckle"] = 800,
                ["wildcraftfruit:finespiritportion-snowberry"] = 800,
                ["wildcraftfruit:finespiritportion-blackelder"] = 870,
                ["wildcraftfruit:finespiritportion-elderberry"] = 870,
                ["wildcraftfruit:finespiritportion-ivy"] = 720,
                ["wildcraftfruit:finespiritportion-hairyappleberry"] = 860,
                ["wildcraftfruit:finespiritportion-purpleappleberry"] = 860,
                ["wildcraftfruit:finespiritportion-blacknightshade"] = 820,
                ["wildcraftfruit:finespiritportion-naranjilla"] = 950,
                ["wildcraftfruit:finespiritportion-belladonna"] = 700,
                ["wildcraftfruit:finespiritportion-husktomato"] = 860,
                ["wildcraftfruit:finespiritportion-beautyberry"] = 840,
                ["wildcraftfruit:finespiritportion-numnum"] = 850,
                ["wildcraftfruit:finespiritportion-seamango"] = 800,
                ["wildcraftfruit:finespiritportion-coralbead"] = 700,
                ["wildcraftfruit:finespiritportion-pilo"] = 850,
                ["wildcraftfruit:finespiritportion-mingimingi"] = 850,
                ["wildcraftfruit:finespiritportion-fractureberry"] = 840
            }
        },
            new()
            {
                Code = "wildcraftfruit:juiceportion-*",
                ValueByType = new()
                {
                    ["*"] = 900,
                    ["wildcraftfruit:juiceportion-juniper"] = 950,
                    ["wildcraftfruit:juiceportion-avocado"] = 850,
                    ["wildcraftfruit:juiceportion-sumac"] = 1100,
                    ["wildcraftfruit:juiceportion-illawarra"] = 1100,
                    ["wildcraftfruit:juiceportion-creepingpine"] = 1000,
                    ["wildcraftfruit:juiceportion-pandanbits"] = 1100,
                    ["wildcraftfruit:juiceportion-maritabits"] = 1100,
                    ["wildcraftfruit:juiceportion-crowseye"] = 1000,
                    ["wildcraftfruit:juiceportion-kakaha"] = 1050,
                    ["wildcraftfruit:juiceportion-flaxlily"] = 1050,
                    ["wildcraftfruit:juiceportion-engkala"] = 1100,
                    ["wildcraftfruit:juiceportion-kawakawa"] = 1050,
                    ["wildcraftfruit:juiceportion-baneberry"] = 900,
                    ["wildcraftfruit:juiceportion-gooseberry"] = 1100,
                    ["wildcraftfruit:juiceportion-blackgrape"] = 1250,
                    ["wildcraftfruit:juiceportion-redgrape"] = 1250,
                    ["wildcraftfruit:juiceportion-whitegrape"] = 1250,
                    ["wildcraftfruit:juiceportion-foxgrape"] = 1200,
                    ["wildcraftfruit:juiceportion-virgingrape"] = 1200,
                    ["wildcraftfruit:juiceportion-strawberry"] = 1100,
                    ["wildcraftfruit:juiceportion-gardenstrawberry"] = 1100,
                    ["wildcraftfruit:juiceportion-falsestrawberry"] = 1000,
                    ["wildcraftfruit:juiceportion-raspberry"] = 1100,
                    ["wildcraftfruit:juiceportion-blueraspberry"] = 1100,
                    ["wildcraftfruit:juiceportion-brambleberry"] = 1100,
                    ["wildcraftfruit:juiceportion-dewberry"] = 1050,
                    ["wildcraftfruit:juiceportion-cloudberry"] = 1100,
                    ["wildcraftfruit:juiceportion-knyazberry"] = 1150,
                    ["wildcraftfruit:juiceportion-bushlawyer"] = 1000,
                    ["wildcraftfruit:juiceportion-dogrose"] = 1050,
                    ["wildcraftfruit:juiceportion-hawthorn"] = 1000,
                    ["wildcraftfruit:juiceportion-rowanberry"] = 1000,
                    ["wildcraftfruit:juiceportion-sorb"] = 1050,
                    ["wildcraftfruit:juiceportion-aronia"] = 1000,
                    ["wildcraftfruit:juiceportion-crabapple"] = 1150,
                    ["wildcraftfruit:juiceportion-sandpear"] = 1250,
                    ["wildcraftfruit:juiceportion-quince"] = 1100,
                    ["wildcraftfruit:juiceportion-loquat"] = 1150,
                    ["wildcraftfruit:juiceportion-apricot"] = 1200,
                    ["wildcraftfruit:juiceportion-sloe"] = 1000,
                    ["wildcraftfruit:juiceportion-purpleplum"] = 1150,
                    ["wildcraftfruit:juiceportion-greengage"] = 1150,
                    ["wildcraftfruit:juiceportion-cherryplum"] = 1150,
                    ["wildcraftfruit:juiceportion-sallowthorn"] = 1000,
                    ["wildcraftfruit:juiceportion-jujube"] = 1000,
                    ["wildcraftfruit:juiceportion-fig"] = 1150,
                    ["wildcraftfruit:juiceportion-falseorange"] = 1200,
                    ["wildcraftfruit:juiceportion-jackfruit"] = 1050,
                    ["wildcraftfruit:juiceportion-cempedak"] = 1050,
                    ["wildcraftfruit:juiceportion-silvernettle"] = 1000,
                    ["wildcraftfruit:juiceportion-cucumber"] = 1250,
                    ["wildcraftfruit:juiceportion-muskmelon"] = 1300,
                    ["wildcraftfruit:juiceportion-honeydewmelon"] = 1300,
                    ["wildcraftfruit:juiceportion-mirzamelon"] = 1300,
                    ["wildcraftfruit:juiceportion-watermelon"] = 1300,
                    ["wildcraftfruit:juiceportion-bryony"] = 900,
                    ["wildcraftfruit:juiceportion-passionfruit"] = 1150,
                    ["wildcraftfruit:juiceportion-granadilla"] = 1150,
                    ["wildcraftfruit:juiceportion-achacha"] = 1150,
                    ["wildcraftfruit:juiceportion-spindle"] = 900,
                    ["wildcraftfruit:juiceportion-ugni"] = 1050,
                    ["wildcraftfruit:juiceportion-midyimberry"] = 1050,
                    ["wildcraftfruit:juiceportion-munthari"] = 1050,
                    ["wildcraftfruit:juiceportion-guajava"] = 1200,
                    ["wildcraftfruit:juiceportion-feijoa"] = 1200,
                    ["wildcraftfruit:juiceportion-roseapple"] = 1200,
                    ["wildcraftfruit:juiceportion-lillypillypink"] = 1100,
                    ["wildcraftfruit:juiceportion-bluetongue"] = 1100,
                    ["wildcraftfruit:juiceportion-turkscap"] = 1000,
                    ["wildcraftfruit:juiceportion-cocoa"] = 1050,
                    ["wildcraftfruit:juiceportion-wolfberry"] = 1000,
                    ["wildcraftfruit:juiceportion-caperberry"] = 1000,
                    ["wildcraftfruit:juiceportion-rambutan"] = 1250,
                    ["wildcraftfruit:juiceportion-citron"] = 1200,
                    ["wildcraftfruit:juiceportion-pomelo"] = 1300,
                    ["wildcraftfruit:juiceportion-grapefruit"] = 1300,
                    ["wildcraftfruit:juiceportion-lime"] = 1250,
                    ["wildcraftfruit:juiceportion-fingerlime"] = 1250,
                    ["wildcraftfruit:juiceportion-kumquat"] = 1150,
                    ["wildcraftfruit:juiceportion-lemonaspen"] = 1200,
                    ["wildcraftfruit:juiceportion-cashewapple"] = 1300,
                    ["wildcraftfruit:juiceportion-wani"] = 1250,
                    ["wildcraftfruit:juiceportion-redquandong"] = 1150,
                    ["wildcraftfruit:juiceportion-rubysaltbush"] = 1000,
                    ["wildcraftfruit:juiceportion-pokeberry"] = 900,
                    ["wildcraftfruit:juiceportion-bunchberry"] = 1050,
                    ["wildcraftfruit:juiceportion-bearberry"] = 1000,
                    ["wildcraftfruit:juiceportion-cheeseberry"] = 1050,
                    ["wildcraftfruit:juiceportion-pineheath"] = 1000,
                    ["wildcraftfruit:juiceportion-pricklyheath"] = 1000,
                    ["wildcraftfruit:juiceportion-honeypots"] = 1000,
                    ["wildcraftfruit:juiceportion-crowberry"] = 1000,
                    ["wildcraftfruit:juiceportion-lingonberry"] = 1050,
                    ["wildcraftfruit:juiceportion-huckleberry"] = 1100,
                    ["wildcraftfruit:juiceportion-kiwi"] = 1200,
                    ["wildcraftfruit:juiceportion-kolomikta"] = 1150,
                    ["wildcraftfruit:juiceportion-woodbine"] = 1050,
                    ["wildcraftfruit:juiceportion-honeysuckle"] = 900,
                    ["wildcraftfruit:juiceportion-snowberry"] = 900,
                    ["wildcraftfruit:juiceportion-blackelder"] = 1100,
                    ["wildcraftfruit:juiceportion-elderberry"] = 1100,
                    ["wildcraftfruit:juiceportion-ivy"] = 850,
                    ["wildcraftfruit:juiceportion-hairyappleberry"] = 1100,
                    ["wildcraftfruit:juiceportion-purpleappleberry"] = 1100,
                    ["wildcraftfruit:juiceportion-blacknightshade"] = 950,
                    ["wildcraftfruit:juiceportion-blacknightshadeunripe"] = 900,
                    ["wildcraftfruit:juiceportion-bitternightshade"] = 900,
                    ["wildcraftfruit:juiceportion-naranjilla"] = 1250,
                    ["wildcraftfruit:juiceportion-belladonna"] = 800,
                    ["wildcraftfruit:juiceportion-husktomato"] = 1100,
                    ["wildcraftfruit:juiceportion-beautyberry"] = 1000,
                    ["wildcraftfruit:juiceportion-numnum"] = 1050,
                    ["wildcraftfruit:juiceportion-seamango"] = 900,
                    ["wildcraftfruit:juiceportion-coralbead"] = 800,
                    ["wildcraftfruit:juiceportion-pilo"] = 1050,
                    ["wildcraftfruit:juiceportion-mingimingi"] = 1050,
                    ["wildcraftfruit:juiceportion-fractureberry"] = 1000
                }
            },
            new()
            {
                Code = "wildcraftfruit:ciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 800,
                    ["wildcraftfruit:ciderportion-juniper"] = 775,
                    ["wildcraftfruit:ciderportion-avocado"] = 650,
                    ["wildcraftfruit:ciderportion-sumac"] = 825,
                    ["wildcraftfruit:ciderportion-pomace"] = 300,
                    ["wildcraftfruit:ciderportion-illawarra"] = 800,
                    ["wildcraftfruit:ciderportion-creepingpine"] = 775,
                    ["wildcraftfruit:ciderportion-pandanbits"] = 800,
                    ["wildcraftfruit:ciderportion-maritabits"] = 800,
                    ["wildcraftfruit:ciderportion-crowseye"] = 775,
                    ["wildcraftfruit:ciderportion-kakaha"] = 800,
                    ["wildcraftfruit:ciderportion-flaxlily"] = 790,
                    ["wildcraftfruit:ciderportion-engkala"] = 825,
                    ["wildcraftfruit:ciderportion-kawakawa"] = 790,
                    ["wildcraftfruit:ciderportion-baneberry"] = 700,
                    ["wildcraftfruit:ciderportion-gooseberry"] = 800,
                    ["wildcraftfruit:ciderportion-blackgrape"] = 850,
                    ["wildcraftfruit:ciderportion-redgrape"] = 850,
                    ["wildcraftfruit:ciderportion-whitegrape"] = 850,
                    ["wildcraftfruit:ciderportion-foxgrape"] = 825,
                    ["wildcraftfruit:ciderportion-virgingrape"] = 825,
                    ["wildcraftfruit:ciderportion-strawberry"] = 800,
                    ["wildcraftfruit:ciderportion-gardenstrawberry"] = 800,
                    ["wildcraftfruit:ciderportion-falsestrawberry"] = 775,
                    ["wildcraftfruit:ciderportion-raspberry"] = 800,
                    ["wildcraftfruit:ciderportion-blueraspberry"] = 800,
                    ["wildcraftfruit:ciderportion-brambleberry"] = 800,
                    ["wildcraftfruit:ciderportion-dewberry"] = 775,
                    ["wildcraftfruit:ciderportion-cloudberry"] = 800,
                    ["wildcraftfruit:ciderportion-knyazberry"] = 825,
                    ["wildcraftfruit:ciderportion-bushlawyer"] = 775,
                    ["wildcraftfruit:ciderportion-dogrose"] = 775,
                    ["wildcraftfruit:ciderportion-hawthorn"] = 750,
                    ["wildcraftfruit:ciderportion-rowanberry"] = 750,
                    ["wildcraftfruit:ciderportion-sorb"] = 775,
                    ["wildcraftfruit:ciderportion-aronia"] = 750,
                    ["wildcraftfruit:ciderportion-crabapple"] = 800,
                    ["wildcraftfruit:ciderportion-sandpear"] = 825,
                    ["wildcraftfruit:ciderportion-quince"] = 800,
                    ["wildcraftfruit:ciderportion-loquat"] = 800,
                    ["wildcraftfruit:ciderportion-apricot"] = 800,
                    ["wildcraftfruit:ciderportion-sloe"] = 750,
                    ["wildcraftfruit:ciderportion-purpleplum"] = 800,
                    ["wildcraftfruit:ciderportion-greengage"] = 800,
                    ["wildcraftfruit:ciderportion-cherryplum"] = 800,
                    ["wildcraftfruit:ciderportion-sallowthorn"] = 750,
                    ["wildcraftfruit:ciderportion-jujube"] = 760,
                    ["wildcraftfruit:ciderportion-fig"] = 800,
                    ["wildcraftfruit:ciderportion-falseorange"] = 800,
                    ["wildcraftfruit:ciderportion-jackfruit"] = 775,
                    ["wildcraftfruit:ciderportion-cempedak"] = 775,
                    ["wildcraftfruit:ciderportion-silvernettle"] = 760,
                    ["wildcraftfruit:ciderportion-cucumber"] = 850,
                    ["wildcraftfruit:ciderportion-muskmelon"] = 900,
                    ["wildcraftfruit:ciderportion-honeydewmelon"] = 900,
                    ["wildcraftfruit:ciderportion-mirzamelon"] = 900,
                    ["wildcraftfruit:ciderportion-watermelon"] = 900,
                    ["wildcraftfruit:ciderportion-bryony"] = 700,
                    ["wildcraftfruit:ciderportion-passionfruit"] = 800,
                    ["wildcraftfruit:ciderportion-granadilla"] = 800,
                    ["wildcraftfruit:ciderportion-achacha"] = 800,
                    ["wildcraftfruit:ciderportion-spindle"] = 700,
                    ["wildcraftfruit:ciderportion-ugni"] = 775,
                    ["wildcraftfruit:ciderportion-midyimberry"] = 775,
                    ["wildcraftfruit:ciderportion-munthari"] = 775,
                    ["wildcraftfruit:ciderportion-guajava"] = 800,
                    ["wildcraftfruit:ciderportion-feijoa"] = 800,
                    ["wildcraftfruit:ciderportion-roseapple"] = 825,
                    ["wildcraftfruit:ciderportion-bluetongue"] = 800,
                    ["wildcraftfruit:ciderportion-turkscap"] = 760,
                    ["wildcraftfruit:ciderportion-cocoa"] = 775,
                    ["wildcraftfruit:ciderportion-wolfberry"] = 760,
                    ["wildcraftfruit:ciderportion-caperberry"] = 760,
                    ["wildcraftfruit:ciderportion-rambutan"] = 825,
                    ["wildcraftfruit:ciderportion-citron"] = 800,
                    ["wildcraftfruit:ciderportion-pomelo"] = 850,
                    ["wildcraftfruit:ciderportion-grapefruit"] = 850,
                    ["wildcraftfruit:ciderportion-lime"] = 825,
                    ["wildcraftfruit:ciderportion-fingerlime"] = 825,
                    ["wildcraftfruit:ciderportion-kumquat"] = 800,
                    ["wildcraftfruit:ciderportion-lemonaspen"] = 800,
                    ["wildcraftfruit:ciderportion-cashewapple"] = 900,
                    ["wildcraftfruit:ciderportion-wani"] = 825,
                    ["wildcraftfruit:ciderportion-redquandong"] = 800,
                    ["wildcraftfruit:ciderportion-rubysaltbush"] = 760,
                    ["wildcraftfruit:ciderportion-pokeberry"] = 700,
                    ["wildcraftfruit:ciderportion-bunchberry"] = 775,
                    ["wildcraftfruit:ciderportion-bearberry"] = 760,
                    ["wildcraftfruit:ciderportion-cheeseberry"] = 775,
                    ["wildcraftfruit:ciderportion-pineheath"] = 760,
                    ["wildcraftfruit:ciderportion-pricklyheath"] = 760,
                    ["wildcraftfruit:ciderportion-honeypots"] = 760,
                    ["wildcraftfruit:ciderportion-crowberry"] = 760,
                    ["wildcraftfruit:ciderportion-lingonberry"] = 780,
                    ["wildcraftfruit:ciderportion-huckleberry"] = 800,
                    ["wildcraftfruit:ciderportion-kiwi"] = 800,
                    ["wildcraftfruit:ciderportion-kolomikta"] = 790,
                    ["wildcraftfruit:ciderportion-woodbine"] = 775,
                    ["wildcraftfruit:ciderportion-honeysuckle"] = 700,
                    ["wildcraftfruit:ciderportion-snowberry"] = 700,
                    ["wildcraftfruit:ciderportion-blackelder"] = 800,
                    ["wildcraftfruit:ciderportion-elderberry"] = 800,
                    ["wildcraftfruit:ciderportion-ivy"] = 650,
                    ["wildcraftfruit:ciderportion-hairyappleberry"] = 800,
                    ["wildcraftfruit:ciderportion-purpleappleberry"] = 800,
                    ["wildcraftfruit:ciderportion-blacknightshade"] = 720,
                    ["wildcraftfruit:ciderportion-naranjilla"] = 825,
                    ["wildcraftfruit:ciderportion-belladonna"] = 550,
                    ["wildcraftfruit:ciderportion-husktomato"] = 800,
                    ["wildcraftfruit:ciderportion-beautyberry"] = 775,
                    ["wildcraftfruit:ciderportion-numnum"] = 775,
                    ["wildcraftfruit:ciderportion-seamango"] = 700,
                    ["wildcraftfruit:ciderportion-coralbead"] = 550,
                    ["wildcraftfruit:ciderportion-pilo"] = 775,
                    ["wildcraftfruit:ciderportion-mingimingi"] = 775,
                    ["wildcraftfruit:ciderportion-fractureberry"] = 775
                }
            },
            new()
            {
                Code = "wildcraftfruit:fineciderportion-*",
                ValueByType = new()
                {
                    ["*"] = 1000,
                    ["wildcraftfruit:fineciderportion-juniper"] = 975,
                    ["wildcraftfruit:fineciderportion-avocado"] = 850,
                    ["wildcraftfruit:fineciderportion-sumac"] = 1025,
                    ["wildcraftfruit:fineciderportion-pomace"] = 500,
                    ["wildcraftfruit:fineciderportion-illawarra"] = 1000,
                    ["wildcraftfruit:fineciderportion-creepingpine"] = 975,
                    ["wildcraftfruit:fineciderportion-pandanbits"] = 1000,
                    ["wildcraftfruit:fineciderportion-maritabits"] = 1000,
                    ["wildcraftfruit:fineciderportion-crowseye"] = 975,
                    ["wildcraftfruit:fineciderportion-kakaha"] = 1000,
                    ["wildcraftfruit:fineciderportion-flaxlily"] = 990,
                    ["wildcraftfruit:fineciderportion-engkala"] = 1025,
                    ["wildcraftfruit:fineciderportion-kawakawa"] = 990,
                    ["wildcraftfruit:fineciderportion-baneberry"] = 900,
                    ["wildcraftfruit:fineciderportion-gooseberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-blackgrape"] = 1050,
                    ["wildcraftfruit:fineciderportion-redgrape"] = 1050,
                    ["wildcraftfruit:fineciderportion-whitegrape"] = 1050,
                    ["wildcraftfruit:fineciderportion-foxgrape"] = 1025,
                    ["wildcraftfruit:fineciderportion-virgingrape"] = 1025,
                    ["wildcraftfruit:fineciderportion-strawberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-gardenstrawberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-falsestrawberry"] = 975,
                    ["wildcraftfruit:fineciderportion-raspberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-blueraspberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-brambleberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-dewberry"] = 975,
                    ["wildcraftfruit:fineciderportion-cloudberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-knyazberry"] = 1025,
                    ["wildcraftfruit:fineciderportion-bushlawyer"] = 975,
                    ["wildcraftfruit:fineciderportion-dogrose"] = 975,
                    ["wildcraftfruit:fineciderportion-hawthorn"] = 950,
                    ["wildcraftfruit:fineciderportion-rowanberry"] = 950,
                    ["wildcraftfruit:fineciderportion-sorb"] = 975,
                    ["wildcraftfruit:fineciderportion-aronia"] = 950,
                    ["wildcraftfruit:fineciderportion-crabapple"] = 1000,
                    ["wildcraftfruit:fineciderportion-sandpear"] = 1025,
                    ["wildcraftfruit:fineciderportion-quince"] = 1000,
                    ["wildcraftfruit:fineciderportion-loquat"] = 1000,
                    ["wildcraftfruit:fineciderportion-apricot"] = 1000,
                    ["wildcraftfruit:fineciderportion-sloe"] = 950,
                    ["wildcraftfruit:fineciderportion-purpleplum"] = 1000,
                    ["wildcraftfruit:fineciderportion-greengage"] = 1000,
                    ["wildcraftfruit:fineciderportion-cherryplum"] = 1000,
                    ["wildcraftfruit:fineciderportion-sallowthorn"] = 950,
                    ["wildcraftfruit:fineciderportion-jujube"] = 960,
                    ["wildcraftfruit:fineciderportion-fig"] = 1000,
                    ["wildcraftfruit:fineciderportion-falseorange"] = 1000,
                    ["wildcraftfruit:fineciderportion-jackfruit"] = 975,
                    ["wildcraftfruit:fineciderportion-cempedak"] = 975,
                    ["wildcraftfruit:fineciderportion-silvernettle"] = 960,
                    ["wildcraftfruit:fineciderportion-cucumber"] = 1050,
                    ["wildcraftfruit:fineciderportion-muskmelon"] = 1100,
                    ["wildcraftfruit:fineciderportion-honeydewmelon"] = 1100,
                    ["wildcraftfruit:fineciderportion-mirzamelon"] = 1100,
                    ["wildcraftfruit:fineciderportion-watermelon"] = 1100,
                    ["wildcraftfruit:fineciderportion-bryony"] = 900,
                    ["wildcraftfruit:fineciderportion-passionfruit"] = 1000,
                    ["wildcraftfruit:fineciderportion-granadilla"] = 1000,
                    ["wildcraftfruit:fineciderportion-achacha"] = 1000,
                    ["wildcraftfruit:fineciderportion-spindle"] = 900,
                    ["wildcraftfruit:fineciderportion-ugni"] = 975,
                    ["wildcraftfruit:fineciderportion-midyimberry"] = 975,
                    ["wildcraftfruit:fineciderportion-munthari"] = 975,
                    ["wildcraftfruit:fineciderportion-guajava"] = 1000,
                    ["wildcraftfruit:fineciderportion-feijoa"] = 1000,
                    ["wildcraftfruit:fineciderportion-roseapple"] = 1025,
                    ["wildcraftfruit:fineciderportion-bluetongue"] = 1000,
                    ["wildcraftfruit:fineciderportion-turkscap"] = 960,
                    ["wildcraftfruit:fineciderportion-cocoa"] = 975,
                    ["wildcraftfruit:fineciderportion-wolfberry"] = 960,
                    ["wildcraftfruit:fineciderportion-caperberry"] = 960,
                    ["wildcraftfruit:fineciderportion-rambutan"] = 1025,
                    ["wildcraftfruit:fineciderportion-citron"] = 1000,
                    ["wildcraftfruit:fineciderportion-pomelo"] = 1050,
                    ["wildcraftfruit:fineciderportion-grapefruit"] = 1050,
                    ["wildcraftfruit:fineciderportion-lime"] = 1025,
                    ["wildcraftfruit:fineciderportion-fingerlime"] = 1025,
                    ["wildcraftfruit:fineciderportion-kumquat"] = 1000,
                    ["wildcraftfruit:fineciderportion-lemonaspen"] = 1000,
                    ["wildcraftfruit:fineciderportion-cashewapple"] = 1100,
                    ["wildcraftfruit:fineciderportion-wani"] = 1025,
                    ["wildcraftfruit:fineciderportion-redquandong"] = 1000,
                    ["wildcraftfruit:fineciderportion-rubysaltbush"] = 960,
                    ["wildcraftfruit:fineciderportion-pokeberry"] = 900,
                    ["wildcraftfruit:fineciderportion-bunchberry"] = 975,
                    ["wildcraftfruit:fineciderportion-bearberry"] = 960,
                    ["wildcraftfruit:fineciderportion-cheeseberry"] = 975,
                    ["wildcraftfruit:fineciderportion-pineheath"] = 960,
                    ["wildcraftfruit:fineciderportion-pricklyheath"] = 960,
                    ["wildcraftfruit:fineciderportion-honeypots"] = 960,
                    ["wildcraftfruit:fineciderportion-crowberry"] = 960,
                    ["wildcraftfruit:fineciderportion-lingonberry"] = 980,
                    ["wildcraftfruit:fineciderportion-huckleberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-kiwi"] = 1000,
                    ["wildcraftfruit:fineciderportion-kolomikta"] = 990,
                    ["wildcraftfruit:fineciderportion-woodbine"] = 975,
                    ["wildcraftfruit:fineciderportion-honeysuckle"] = 900,
                    ["wildcraftfruit:fineciderportion-snowberry"] = 900,
                    ["wildcraftfruit:fineciderportion-blackelder"] = 1000,
                    ["wildcraftfruit:fineciderportion-elderberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-ivy"] = 850,
                    ["wildcraftfruit:fineciderportion-hairyappleberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-purpleappleberry"] = 1000,
                    ["wildcraftfruit:fineciderportion-blacknightshade"] = 920,
                    ["wildcraftfruit:fineciderportion-naranjilla"] = 1025,
                    ["wildcraftfruit:fineciderportion-belladonna"] = 750,
                    ["wildcraftfruit:fineciderportion-husktomato"] = 1000,
                    ["wildcraftfruit:fineciderportion-beautyberry"] = 975,
                    ["wildcraftfruit:fineciderportion-numnum"] = 975,
                    ["wildcraftfruit:fineciderportion-seamango"] = 900,
                    ["wildcraftfruit:fineciderportion-coralbead"] = 750,
                    ["wildcraftfruit:fineciderportion-pilo"] = 975,
                    ["wildcraftfruit:fineciderportion-mingimingi"] = 975,
                    ["wildcraftfruit:fineciderportion-fractureberry"] = 975
                }
            },
            new()
            {
                Code = "wildcraftherb:root-*",
                ValueByType = new()
                {
                    ["*"] = 2
                }
            },
            new()
            {
                Code = "wildcraftherb:herb-*",
                ValueByType = new()
                {
                    ["*"] = 4
                }
            },
            new()
            {
                Code = "wildcraftfruit:flower-*",
                ValueByType = new()
                {
                    ["*"] = 2
                }
            },
            new()
            {
                Code = "wildcraftfruit:pickledvegetable-*",
                ValueByType = new()
                {
                    ["*"] = 15
                }
            },
            new()
            {
                Code = "wildcraftfruit:sweetwaterportion",
                Value = 650
            },
            new()
            {
                Code = "wildcraftfruit:flowerwine-*",
                ValueByType = new()
                {
                    ["wildcraftfruit:flowerwine-currant"] = 900,
                    ["wildcraftfruit:flowerwine-rose"] = 875,
                    ["wildcraftfruit:flowerwine-passionflower"] = 850,
                    ["wildcraftfruit:flowerwine-hibiscus"] = 875,
                    ["wildcraftfruit:flowerwine-honeysuckle"] = 850,
                    ["wildcraftfruit:flowerwine-elder"] = 900
                }
            },
            new()
            {
                Code = "wildcraftfruit:lemonade-*",
                ValueByType = new()
                {
                    ["wildcraftfruit:lemonade-orange"] = 1500,
                    ["wildcraftfruit:lemonade-citron"] = 1480,
                    ["wildcraftfruit:lemonade-pomelo"] = 1550,
                    ["wildcraftfruit:lemonade-grapefruit"] = 1550,
                    ["wildcraftfruit:lemonade-lime"] = 1500,
                    ["wildcraftfruit:lemonade-kumquat"] = 1475,
                    ["wildcraftfruit:lemonade-fingerlime"] = 1475
                }
            },
            new()
            {
                Code = "wildcraftfruit:soymilkportion-*",
                ValueByType = new()
                {
                    ["*"] = 800
                }
            },
            new()
            {
                Code = "wildcraftfruit:chocomilkportion-*",
                ValueByType = new()
                {
                    ["*"] = 800
                }
            },
            new()
            {
                Code = "wildcraftfruit:tinctureportion-*",
                ValueByType = new()
                {
                    ["wildcraftfruit:tinctureportion-raspberry"] = -290,
                    ["wildcraftfruit:tinctureportion-elderberry"] = -300,
                    ["wildcraftfruit:tinctureportion-rosehip"] = -320,
                    ["wildcraftfruit:tinctureportion-orange"] = -290,
                    ["wildcraftfruit:tinctureportion-citron"] = -300,
                    ["wildcraftfruit:tinctureportion-lime"] = -305,
                    ["wildcraftfruit:tinctureportion-grapefruit"] = -315,
                    ["wildcraftfruit:tinctureportion-ivy"] = -380,
                    ["wildcraftfruit:tinctureportion-aronia"] = -330,
                    ["wildcraftfruit:tinctureportion-bearberry"] = -340,
                    ["wildcraftfruit:tinctureportion-passionflower"] = -300,
                    ["wildcraftfruit:tinctureportion-cloudberry"] = -280,
                    ["wildcraftfruit:tinctureportion-hawthorn"] = -310,
                    ["wildcraftfruit:tinctureportion-juniper"] = -320
                }
            },
            new()
            {
                Code = "acorns:flour-acorn-*",
                ValueByType = new()
                {
                    ["*"] = -8
                }
            },
            new()
            {
                Code = "acorns:acorn-meal-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "primitivesurvival:trussedrot-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "primitivesurvival:trussedmeat-*",
                ValueByType = new()
                {
                    ["*"] = -5
                }
            },
            new()
            {
                Code = "primitivesurvival:snakemeat-*",
                ValueByType = new()
                {
                    ["*"] = -2
                }
            },
            new()
            {
                Code = "primitivesurvival:smokedmeat-*",
                ValueByType = new()
                {
                    ["*"] = -6
                }
            },
            new()
            {
                Code = "primitivesurvival:jerky-redmeat-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "primitivesurvival:jerky-fish-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "primitivesurvival:jerky-bushmeat-*",
                ValueByType = new()
                {
                    ["*"] = -10
                }
            },
            new()
            {
                Code = "primitivesurvival:curedsmokedmeat-*",
                ValueByType = new()
                {
                    ["*"] = -12
                }
            },
            new()
            {
                Code = "primitivesurvival:crabmeat-*",
                ValueByType = new()
                {
                    ["*"] = -4
                }
            },
            new()
            {
                Code = "ancienttools:saltedmeat-*",
                ValueByType = new()
                {
                    ["*"] = -12
                }
            },
            new()
            {
                Code = "maketea:teaportion-*",
                ValueByType = new()
                {
                    ["*"] = 600
                }
            },
            new()
            {
                Code = "saltandsands:bivalvemeat-*",
                ValueByType = new()
                {
                    ["*"] = -5,
                    ["saltandsands:bivalvemeat-freshwatermussel-*"] = 20
                }
            }
        ]
    };
}
