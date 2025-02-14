using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HydrateOrDiedrate.Commands;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Config.Sync;
using HydrateOrDiedrate.encumbrance;
using HydrateOrDiedrate.Hot_Weather;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Keg;
using HydrateOrDiedrate.patches;
using HydrateOrDiedrate.wellwater;
using HydrateOrDiedrate.winch;
using HydrateOrDiedrate.XSkill;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace HydrateOrDiedrate;

public class HydrateOrDiedrateModSystem : ModSystem
{
    private ICoreServerAPI _serverApi;
    private ICoreClientAPI _clientApi;

    private HudElementThirstBar _thirstHud;
    private HudElementHungerReductionBar _hungerReductionHud;
    public static Config.Config LoadedConfig { get; set; }
    private WaterInteractionHandler _waterInteractionHandler;
    private Harmony harmony;

    private ConfigLibCompatibility _configLibCompatibility;
    private XLibSkills xLibSkills;

    private RainHarvesterManager rainHarvesterManager;
    private DrinkHudOverlayRenderer hudOverlayRenderer;

    public static IClientNetworkChannel clientChannel;
    private IServerNetworkChannel serverChannel;
    private string configFilename = "HydrateOrDiedrateConfig.json";
    private long customHudListenerId;
    private AquiferManager _aquiferManager;
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        ItemHydrationConfigLoader.GenerateDefaultHydrationConfig();
        BlockHydrationConfigLoader.GenerateDefaultBlockHydrationConfig();
        CoolingConfigLoader.GenerateDefaultCoolingConfig();
        harmony = new Harmony("com.chronolegionnaire.hydrateordiedrate");
        harmony.PatchAll();
    }

    private Config.Config LoadConfigFromFile(ICoreAPI api)
    {
        var jsonObj = api.LoadModConfig(configFilename);
        if (jsonObj == null)
        {
            return null;
        }
        var existingJson = JObject.Parse(jsonObj.Token.ToString());
        var configType = typeof(Config.Config);
        var properties = configType.GetProperties();
        var defaultConfig = new Config.Config();
        bool needsSave = false;
    
        foreach (var prop in properties)
        {
            string pascalCaseName = prop.Name;
            string camelCaseName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

            // Check both pascal and camel case
            var hasValue = false;
            JToken value = null;
        
            if (existingJson.ContainsKey(pascalCaseName))
            {
                value = existingJson[pascalCaseName];
                hasValue = value != null && value.Type != JTokenType.Null;
            }
            else if (existingJson.ContainsKey(camelCaseName))
            {
                value = existingJson[camelCaseName];
                hasValue = value != null && value.Type != JTokenType.Null;
            }

            if (!hasValue)
            {
                var defaultValue = prop.GetValue(defaultConfig);
                existingJson[pascalCaseName] = JToken.FromObject(defaultValue);
                needsSave = true;
            }
        }

        var settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Include
        };
    
        var config = JsonConvert.DeserializeObject<Config.Config>(existingJson.ToString(), settings);
    
        if (needsSave)
        {
            SaveConfig(api, config);
        }
    
        return config;
    }

    private void SaveConfig(ICoreAPI api, Config.Config config = null)
    {
        if (config == null)
        {
            config = LoadedConfig;
        }

        if (config == null)
        {
            return;
        }

        var jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
        };
        var configJson = JsonConvert.SerializeObject(config, jsonSettings);
        ModConfig.WriteConfig(api, configFilename, config);
    }
    private void LoadConfig(ICoreAPI api)
    {
        var savedConfig = LoadConfigFromFile(api);
        if (savedConfig == null)
        {
            LoadedConfig = new Config.Config();
            SaveConfig(api);
        }
        else
        {
            LoadedConfig = savedConfig;
        }
    }
    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        ApplyWaterSatietyPatches(api);
        ApplyWellWaterSatietyPatches(api);
        ApplyWaterPerishPatches(api);
        ApplyWellWaterPerishPatches(api);
        ApplyKegTunConfigPatches(api);
    }
    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        LoadAndApplyCoolingPatches(api);

        if (LoadedConfig.EnableThirstMechanics)
        {
            LoadAndApplyHydrationPatches(api);
            LoadAndApplyBlockHydrationPatches(api);
        }
        foreach (var block in api.World.Blocks)
        {
            if (block is BlockLiquidContainerTopOpened || block is BlockBarrel || block is BlockGroundStorage)
            {
                AddBehaviorToBlock(block, api);
            }
        }
    }
    public void ApplyWaterSatietyPatches(ICoreAPI api)
    {
        float waterSatiety = LoadedConfig.WaterSatiety;
        float saltWaterSatiety = LoadedConfig.SaltWaterSatiety;
        float boilingWaterSatiety = LoadedConfig.BoilingWaterSatiety;
        float rainWaterSatiety = LoadedConfig.RainWaterSatiety;
        float distilledWaterSatiety = LoadedConfig.DistilledWaterSatiety;
        float boiledWaterSatiety = LoadedConfig.BoiledWaterSatiety;
        float boiledRainWaterSatiety = LoadedConfig.BoiledRainWaterSatiety;

        ApplySatietyPatch(api, "game:itemtypes/liquid/waterportion.json", waterSatiety);
        ApplySatietyPatch(api, "game:itemtypes/liquid/saltwaterportion.json", saltWaterSatiety);
        ApplySatietyPatch(api, "game:itemtypes/liquid/boilingwaterportion.json", boilingWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/rainwaterportion.json", rainWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/distilledwaterportion.json", distilledWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledwaterportion.json", boiledWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledrainwaterportion.json", boiledRainWaterSatiety);
    }

    private void ApplySatietyPatch(ICoreAPI api, string jsonFilePath, float satietyValue)
    {
        JsonPatch ensureNutritionProps = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre",
            Value = new JsonObject(JToken.FromObject(new { })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchSatiety = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre/satiety",
            Value = new JsonObject(JToken.FromObject(satietyValue)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchFoodCategory = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre/foodcategory",
            Value = new JsonObject(JToken.FromObject("NoNutrition")),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:ensureNutritionProps"), ensureNutritionProps, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicsatietypatch"), patchSatiety, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicfoodcategorypatch"), patchFoodCategory, ref applied, ref notFound, ref errorCount);
    }

    public void ApplyWellWaterSatietyPatches(ICoreAPI api)
    {
        var wellWaterSatietyValues = new Dictionary<string, float>
        {
            { "fresh", LoadedConfig.WellWaterFreshSatiety },
            { "salt", LoadedConfig.WellWaterSaltSatiety },
            { "muddy", LoadedConfig.WellWaterMuddySatiety },
            { "tainted", LoadedConfig.WellWaterTaintedSatiety },
            { "poisoned", LoadedConfig.WellWaterPoisonedSatiety },
            { "muddysalt", LoadedConfig.WellWaterMuddySaltSatiety },
            { "taintedsalt", LoadedConfig.WellWaterTaintedSaltSatiety },
            { "poisonedsalt", LoadedConfig.WellWaterPoisonedSaltSatiety }
        };
        foreach (var kvp in wellWaterSatietyValues)
        {
            ApplyWellWaterSatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/wellwaterportion.json", kvp.Key, kvp.Value);
        }
    }

    private void ApplyWellWaterSatietyPatch(ICoreAPI api, string jsonFilePath, string waterType, float satietyValue)
    {
        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        JsonPatch ensureNutritionProps = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/attributesByType/*-{waterType}/waterTightContainerProps/nutritionPropsPerLitre",
            Value = new JsonObject(JToken.FromObject(new { })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchSatiety = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/attributesByType/*-{waterType}/waterTightContainerProps/nutritionPropsPerLitre/satiety",
            Value = new JsonObject(JToken.FromObject(satietyValue)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchFoodCategory = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/attributesByType/*-{waterType}/waterTightContainerProps/nutritionPropsPerLitre/foodcategory",
            Value = new JsonObject(JToken.FromObject("NoNutrition")),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwaterensure-{waterType}"), ensureNutritionProps, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwatersatiety-{waterType}"), patchSatiety, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwaterfoodcat-{waterType}"), patchFoodCategory, ref applied, ref notFound, ref errorCount);
    }
    public void ApplyWaterPerishPatches(ICoreAPI api)
    {
        float rainWaterFreshFreshHours = LoadedConfig.RainWaterFreshHours;
        float rainWaterFreshTransitionHours = LoadedConfig.RainWaterTransitionHours;
        float boiledWaterFreshFreshHours = LoadedConfig.BoiledWaterFreshHours;
        float boiledWaterFreshTransitionHours = LoadedConfig.BoiledWaterTransitionHours;
        float boiledRainWaterFreshFreshHours = LoadedConfig.BoiledRainWaterFreshHours;
        float boiledRainWaterFreshTransitionHours = LoadedConfig.BoiledRainWaterTransitionHours;
        float distilledWaterFreshFreshHours = LoadedConfig.DistilledWaterFreshHours;
        float distilledWaterFreshTransitionHours = LoadedConfig.DistilledWaterTransitionHours;
        
        ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/rainwaterportion.json", rainWaterFreshFreshHours, rainWaterFreshTransitionHours);
        ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/distilledwaterportion.json", distilledWaterFreshFreshHours, distilledWaterFreshTransitionHours);
        ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledwaterportion.json", boiledWaterFreshFreshHours, boiledWaterFreshTransitionHours);
        ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledrainwaterportion.json", boiledRainWaterFreshFreshHours, boiledRainWaterFreshTransitionHours);
    }

    private void ApplyPerishPatch(ICoreAPI api, string jsonFilePath, float freshHours, float transitionHours)
    {
        JsonPatch patchFreshHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/transitionableProps/0/freshHours",
            Value = new JsonObject(JToken.FromObject(new { avg = freshHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
    
        JsonPatch patchTransitionHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/transitionableProps/0/transitionHours",
            Value = new JsonObject(JToken.FromObject(new { avg = transitionHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicfreshhourspatch"), patchFreshHours, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamictransitionhourspatch"), patchTransitionHours, ref applied, ref notFound, ref errorCount);
    }

    public void ApplyWellWaterPerishPatches(ICoreAPI api)
    {
        var wellWaterPerishRateValues = new Dictionary<string, (float fresh, float transition)>
        {
            { "fresh", (LoadedConfig.WellWaterFreshFreshHours, LoadedConfig.WellWaterFreshTransitionHours) },
            { "salt", (LoadedConfig.WellWaterSaltFreshHours, LoadedConfig.WellWaterSaltTransitionHours) },
            { "muddy", (LoadedConfig.WellWaterMuddyFreshHours, LoadedConfig.WellWaterMuddyTransitionHours) },
            { "tainted", (LoadedConfig.WellWaterTaintedFreshHours, LoadedConfig.WellWaterTaintedTransitionHours) },
            { "poisoned", (LoadedConfig.WellWaterPoisonedFreshHours, LoadedConfig.WellWaterPoisonedTransitionHours) },
            { "muddysalt", (LoadedConfig.WellWaterMuddySaltFreshHours, LoadedConfig.WellWaterMuddySaltTransitionHours) },
            { "taintedsalt", (LoadedConfig.WellWaterTaintedSaltFreshHours, LoadedConfig.WellWaterTaintedSaltTransitionHours) },
            { "poisonedsalt", (LoadedConfig.WellWaterPoisonedSaltFreshHours, LoadedConfig.WellWaterPoisonedSaltTransitionHours) }
        };
        foreach (var kvp in wellWaterPerishRateValues)
        {
            ApplyWellWaterPerishRatePatch(api, "hydrateordiedrate:itemtypes/liquid/wellwaterportion.json", kvp.Key,
                kvp.Value.fresh, kvp.Value.transition);
        }
    }

    private void ApplyWellWaterPerishRatePatch(ICoreAPI api, string jsonFilePath, string waterType, float freshHours,
        float transitionHours)
    {
        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        JsonPatch patchFreshHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/transitionablePropsByType/*-{waterType}/0/freshHours",
            Value = new JsonObject(JToken.FromObject(new { avg = freshHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        JsonPatch patchTransitionHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/transitionablePropsByType/*-{waterType}/0/transitionHours",
            Value = new JsonObject(JToken.FromObject(new { avg = transitionHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwaterfreshperishrate-{waterType}"),
            patchFreshHours, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwatertransitionperishrate-{waterType}"),
            patchTransitionHours, ref applied, ref notFound, ref errorCount);
    }

    public void ApplyKegTunConfigPatches(ICoreAPI api)
    {
        ApplyKegPatch(api, "hydrateordiedrate:blocktypes/keg.json", LoadedConfig.KegCapacityLitres, LoadedConfig.SpoilRateTapped, LoadedConfig.KegIronHoopDropChance, LoadedConfig.KegTapDropChance);
        ApplyKegPatch(api, "hydrateordiedrate:blocktypes/kegtapped.json", LoadedConfig.KegCapacityLitres, LoadedConfig.SpoilRateUntapped, LoadedConfig.KegIronHoopDropChance, LoadedConfig.KegTapDropChance);
        ApplyTunPatch(api, "hydrateordiedrate:blocktypes/tun.json", LoadedConfig.TunCapacityLitres, LoadedConfig.TunSpoilRateMultiplier);
    }

    private void ApplyKegPatch(ICoreAPI api, string jsonFilePath, float kegCapacityLitres, float spoilRate,
        float ironHoopDropChance, float kegTapDropChance)
    {
        JsonPatch patchKegCapacity = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/kegCapacityLitres",
            Value = new JsonObject(JToken.FromObject(kegCapacityLitres)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        JsonPatch patchSpoilRate = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/spoilRate",
            Value = new JsonObject(JToken.FromObject(spoilRate)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        JsonPatch patchIronHoop = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/ironHoopDropChance",
            Value = new JsonObject(JToken.FromObject(ironHoopDropChance)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        JsonPatch patchKegTap = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/kegTapDropChance",
            Value = new JsonObject(JToken.FromObject(kegTapDropChance)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegcapacitypatch"), patchKegCapacity,
            ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegspoilratepatch"), patchSpoilRate,
            ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicironhooppatch"), patchIronHoop,
            ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegtappatch"), patchKegTap, ref applied,
            ref notFound, ref errorCount);
    }

    private void ApplyTunPatch(ICoreAPI api, string jsonFilePath, float capacityLitres, float spoilRate)
    {
        JsonPatch patchCapacity = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/tunCapacityLitres",
            Value = new JsonObject(JToken.FromObject(capacityLitres)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        JsonPatch patchSpoilRate = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/spoilRate",
            Value = new JsonObject(JToken.FromObject(spoilRate)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamictuncapacitypatch"), patchCapacity, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamictunspoilratepatch"), patchSpoilRate, ref applied, ref notFound, ref errorCount);
    }
    private void LoadAndApplyHydrationPatches(ICoreAPI api)
    {
        if (LoadedConfig.EnableThirstMechanics)
        {
            List<JObject> hydrationPatches = ItemHydrationConfigLoader.LoadHydrationPatches(api);
            HydrationManager.ApplyHydrationPatches(api, hydrationPatches);
        }
    }
    private void LoadAndApplyBlockHydrationPatches(ICoreAPI api)
    {
        if (LoadedConfig.EnableThirstMechanics)
        {
            List<JsonObject> loadedPatches = BlockHydrationConfigLoader.LoadBlockHydrationConfig(api)
                .ConvertAll(jObject => new JsonObject(jObject));
            BlockHydrationManager.ApplyBlockHydrationPatches(api, loadedPatches, api.World.Blocks);
        }
    }
    private void LoadAndApplyCoolingPatches(ICoreAPI api)
    {
        List<JObject> coolingPatches = CoolingConfigLoader.LoadCoolingPatches(api);
        CoolingManager.ApplyCoolingPatches(api, coolingPatches);
    }
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        LoadConfig(api);
        api.RegisterEntityBehaviorClass("bodytemperaturehot", typeof(EntityBehaviorBodyTemperatureHot));
        api.RegisterEntityBehaviorClass("liquidencumbrance", typeof(EntityBehaviorLiquidEncumbrance));
        api.RegisterBlockClass("BlockKeg", typeof(BlockKeg));
        api.RegisterBlockEntityClass("BlockEntityKeg", typeof(BlockEntityKeg));
        api.RegisterItemClass("ItemKegTap", typeof(ItemKegTap));
        api.RegisterCollectibleBehaviorClass("BehaviorPickaxeWellMode", typeof(BehaviorPickaxeWellMode));
        api.RegisterCollectibleBehaviorClass("BehaviorShovelWellMode", typeof(BehaviorShovelWellMode));
        api.RegisterBlockClass("BlockTun", typeof(BlockTun));
        api.RegisterBlockEntityClass("BlockEntityTun", typeof(BlockEntityTun));
        api.RegisterBlockEntityClass("BlockEntityWellWaterData", typeof(BlockEntityWellWaterData));
        api.RegisterBlockBehaviorClass("BlockBehaviorWellWaterFinite", typeof(BlockBehaviorWellWaterFinite));
        api.RegisterBlockClass("BlockWellSpring", typeof(BlockWellSpring));
        api.RegisterBlockEntityClass("BlockEntityWellSpring", typeof(BlockEntityWellSpring));
        api.RegisterBlockClass("BlockWinch", typeof(BlockWinch));
        api.RegisterBlockEntityClass("BlockEntityWinch", typeof(BlockEntityWinch));

        if (LoadedConfig.EnableThirstMechanics)
        {
            api.RegisterEntityBehaviorClass("thirst", typeof(EntityBehaviorThirst));
        }

        _waterInteractionHandler = new WaterInteractionHandler(api, LoadedConfig);
        
        if (api.Side == EnumAppSide.Server)
        {
            InitializeServer(api as ICoreServerAPI);
        }
        else if (api.Side == EnumAppSide.Client)
        {
            InitializeClient(api as ICoreClientAPI);
        }

        if (api.ModLoader.IsModEnabled("xlib") || api.ModLoader.IsModEnabled("xlibpatch"))
        {
            xLibSkills = new XLibSkills();
            xLibSkills.Initialize(api);
        }


        api.RegisterBlockEntityBehaviorClass("RainHarvester", typeof(RegisterRainHarvester));
        if (api.Side == EnumAppSide.Server)
        {
            HydrateOrDiedrateGlobals.InitializeAquiferSystem(api as ICoreServerAPI);
        }
        if (api.ModLoader.IsModEnabled("BetterProspecting"))
        {
            BetterProspectingAquiferPatch.Apply(api);
        }
    }
    public static class HydrateOrDiedrateGlobals
    {
        public static AquiferManager AquiferManager { get; private set; }

        public static void InitializeAquiferSystem(ICoreServerAPI api)
        {
            AquiferManager = new AquiferManager(api);
        }
    }
    
    private void InitializeServer(ICoreServerAPI api)
    {
        _serverApi = api;

        serverChannel = api.Network.RegisterChannel("hydrateordiedrate")
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<ConfigSyncPacket>()
            .RegisterMessageType<ConfigSyncRequestPacket>()
            .RegisterMessageType<WellSpringBlockPacket>()
            .SetMessageHandler<WellSpringBlockPacket>(WellSpringBlockPacketReceived)
            .SetMessageHandler<ConfigSyncRequestPacket>(OnConfigSyncRequestReceived);
        
        _waterInteractionHandler.Initialize(serverChannel);
        
        rainHarvesterManager = new RainHarvesterManager(_serverApi, LoadedConfig);
        api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        api.Event.PlayerRespawn += OnPlayerRespawn;
        api.Event.RegisterGameTickListener(CheckPlayerInteraction, 100);

        ThirstCommands.Register(api, LoadedConfig);
        AquiferCommands.Register(api);
    }

    private void InitializeClient(ICoreClientAPI api)
    {
        _clientApi = api;

        clientChannel = api.Network.RegisterChannel("hydrateordiedrate")
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<ConfigSyncPacket>()
            .RegisterMessageType<ConfigSyncRequestPacket>()
            .RegisterMessageType<WellSpringBlockPacket>()
            .SetMessageHandler<DrinkProgressPacket>(OnDrinkProgressReceived)
            .SetMessageHandler<ConfigSyncPacket>(OnConfigSyncReceived);
        

        api.Event.RegisterCallback((dt) =>
        {
            if (clientChannel.Connected)
            {
                clientChannel.SendPacket(new ConfigSyncRequestPacket());
            }
        }, 500);

        hudOverlayRenderer = new DrinkHudOverlayRenderer(api);
        api.Event.RegisterRenderer(hudOverlayRenderer, EnumRenderStage.Ortho, "drinkoverlay");

        if (LoadedConfig.EnableThirstMechanics)
        {
            customHudListenerId = api.Event.RegisterGameTickListener(CheckAndInitializeCustomHud, 20);
        }

        _configLibCompatibility = new ConfigLibCompatibility(api);
    }
    public override void StartClientSide(ICoreClientAPI capi)
    {
        base.StartClientSide(capi);
        harmony.PatchAll();
    }

    private void OnConfigSyncRequestReceived(IServerPlayer fromPlayer, ConfigSyncRequestPacket packet)
    {
        var configToSend = new Config.Config(_serverApi, LoadedConfig);
        var configSyncPacket = new ConfigSyncPacket
        {
            ServerConfig = configToSend,
        };
        serverChannel.SendPacket(configSyncPacket, fromPlayer);
    }

    private void OnConfigSyncReceived(ConfigSyncPacket packet)
    {
        if (_clientApi == null) return;
        LoadedConfig = packet.ServerConfig;
        ReloadComponents();
        SyncPatches(packet);
    }

    private void SyncPatches(ConfigSyncPacket packet)
    {
        var currentHydrationPatches = HydrationManager.GetLastAppliedPatches() ?? new List<JObject>();
        var packetHydrationPatches = packet.HydrationPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        if (!ArePatchesEqual(packetHydrationPatches, currentHydrationPatches))
        {
            HydrationManager.ApplyHydrationPatches(_clientApi, packetHydrationPatches);
        }
        var currentBlockHydrationPatches =
            BlockHydrationManager.GetLastAppliedPatches()?.ConvertAll(jsonObj => jsonObj.Token as JObject) ??
            new List<JObject>();
        var packetBlockHydrationPatches =
            packet.BlockHydrationPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        if (!ArePatchesEqual(packetBlockHydrationPatches, currentBlockHydrationPatches))
        {
            var convertedBlockHydrationPatches = packetBlockHydrationPatches.ConvertAll(jObj => new JsonObject(jObj));
            BlockHydrationManager.ApplyBlockHydrationPatches(_clientApi, convertedBlockHydrationPatches,
                _clientApi.World.Blocks);
        }
        var currentCoolingPatches = CoolingManager.GetLastAppliedPatches() ?? new List<JObject>();
        var packetCoolingPatches = packet.CoolingPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        if (!ArePatchesEqual(packetCoolingPatches, currentCoolingPatches))
        {
            CoolingManager.ApplyCoolingPatches(_clientApi, packetCoolingPatches);
        }
    }
    private bool ArePatchesEqual(List<JObject> patches1, List<JObject> patches2)
    {
        if (patches1.Count != patches2.Count) return false;
        for (int i = 0; i < patches1.Count; i++)
        {
            if (!JToken.DeepEquals(patches1[i], patches2[i]))
            {
                return false;
            }
        }
        return true;
    }
    
    private void ReloadComponents()
    {
        _waterInteractionHandler?.Reset(LoadedConfig);
        rainHarvesterManager?.Reset(LoadedConfig);

        if (_clientApi?.World == null)
        {
            _clientApi?.Event.RegisterCallback((dt) => ReloadComponents(), 1000);
            return;
        }

        foreach (var entity in _clientApi.World.LoadedEntities.Values)
        {
            var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();
            thirstBehavior?.Reset(LoadedConfig);

            var bodyTempBehavior = entity.GetBehavior<EntityBehaviorBodyTemperatureHot>();
            bodyTempBehavior?.Reset(LoadedConfig);

            var encumbranceBehavior = entity.GetBehavior<EntityBehaviorLiquidEncumbrance>();
            encumbranceBehavior?.Reset(LoadedConfig);
        }
        
        if (!LoadedConfig.EnableThirstMechanics)
        {
            _thirstHud?.TryClose();
            _thirstHud = null;
        }
        else if (_thirstHud == null)
        {
            _thirstHud = new HudElementThirstBar(_clientApi);
            _clientApi.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            _clientApi.Gui.RegisterDialog(_thirstHud);
        }
    }
    public RainHarvesterManager GetRainHarvesterManager()
    {
        return rainHarvesterManager;
    }
    private void OnDrinkProgressReceived(DrinkProgressPacket msg)
    {
        if (hudOverlayRenderer == null)
        {
            return;
        }

        hudOverlayRenderer.CircleVisible = msg.IsDrinking;

        if (!msg.IsDrinking || msg.Progress <= 0)
        {
            hudOverlayRenderer.CircleProgress = 0f;
            hudOverlayRenderer.CircleVisible = false;
        }
        else
        {
            hudOverlayRenderer.CircleProgress = msg.Progress;
            hudOverlayRenderer.IsDangerous = msg.IsDangerous;
        }
    }

    private void CheckAndInitializeCustomHud(float dt)
    {
        var vanillaHudStatbar = GetVanillaStatbarHud();

        if (vanillaHudStatbar != null && vanillaHudStatbar.IsOpened())
        {

            _thirstHud = new HudElementThirstBar(_clientApi);
            _clientApi.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            _clientApi.Gui.RegisterDialog(_thirstHud);

            _hungerReductionHud = new HudElementHungerReductionBar(_clientApi);
            _clientApi.Event.RegisterGameTickListener(_hungerReductionHud.OnGameTick, 1000);
            _clientApi.Gui.RegisterDialog(_hungerReductionHud);

            _clientApi.Event.UnregisterGameTickListener(customHudListenerId);
        }
    }

    private HudStatbar GetVanillaStatbarHud()
    {
        foreach (var hud in _clientApi.Gui.OpenedGuis)
        {
            if (hud is HudStatbar statbar)
            {
                return statbar;
            }
        }

        return null;
    }

    public void CheckPlayerInteraction(float dt)
    {
        foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
        {
            if(player.ConnectionState != EnumClientState.Playing) return;
            _waterInteractionHandler.CheckShiftRightClickBeforeInteractionForPlayer(dt, player);
        }
    }
    private void OnPlayerNowPlaying(IServerPlayer byPlayer)
    {
        var entity = byPlayer.Entity;
        if (entity == null) return;

        var bodyTemperatureHotBehavior = entity.GetBehavior<EntityBehaviorBodyTemperatureHot>();
        if (bodyTemperatureHotBehavior == null)
        {
            bodyTemperatureHotBehavior = new EntityBehaviorBodyTemperatureHot(entity, LoadedConfig);
            entity.AddBehavior(bodyTemperatureHotBehavior);
        }

        var liquidEncumbranceBehavior = entity.GetBehavior<EntityBehaviorLiquidEncumbrance>();
        if (liquidEncumbranceBehavior == null)
        {
            liquidEncumbranceBehavior = new EntityBehaviorLiquidEncumbrance(entity, LoadedConfig);
            entity.AddBehavior(liquidEncumbranceBehavior);
        }

        if (LoadedConfig.EnableThirstMechanics)
        {
            var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
            {
                thirstBehavior = new EntityBehaviorThirst(entity);
                entity.AddBehavior(thirstBehavior);
            }
            if (!entity.WatchedAttributes.HasAttribute("currentThirst"))
            {
                thirstBehavior.CurrentThirst = thirstBehavior.MaxThirst;
                thirstBehavior.MovementPenalty = 0f; 
            }
            if (thirstBehavior.HungerReductionAmount < 0)
            {
                thirstBehavior.HungerReductionAmount = 0;
            }
        }
    }
    private void OnPlayerRespawn(IServerPlayer byPlayer)
    {
        var entity = byPlayer.Entity;
        if (entity == null) return;

        if (LoadedConfig.EnableThirstMechanics)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            thirstBehavior.OnRespawn();
        }
    }

    private void AddBehaviorToBlock(Block block, ICoreAPI api)
    {
        if (block.BlockEntityBehaviors == null)
        {
            block.BlockEntityBehaviors = new BlockEntityBehaviorType[0];
        }

        if (block.BlockEntityBehaviors.All(b => b.Name != "RainHarvester"))
        {
            var behaviorsList = block.BlockEntityBehaviors.ToList();
            behaviorsList.Add(new BlockEntityBehaviorType()
            {
                Name = "RainHarvester",
                properties = null
            });
            block.BlockEntityBehaviors = behaviorsList.ToArray();
        }
    }
    private void WellSpringBlockPacketReceived(IServerPlayer sender, WellSpringBlockPacket packet)
    {
        if (_serverApi?.World == null) return;
        IBlockAccessor accessor = _serverApi.World.GetBlockAccessor(true, true, false);
        accessor.ExchangeBlock(packet.BlockId, packet.Position);
        accessor.SpawnBlockEntity("BlockEntityWellSpring", packet.Position, null);
        Console.WriteLine();
    }
    public override void Dispose()
    {
        harmony.UnpatchAll("com.chronolegionnaire.hydrateordiedrate");
        base.Dispose();
    }
}