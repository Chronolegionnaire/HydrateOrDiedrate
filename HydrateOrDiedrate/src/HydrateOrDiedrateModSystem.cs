using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.encumbrance;
using HydrateOrDiedrate.Hot_Weather;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Keg;
using HydrateOrDiedrate.src.Commands;
using HydrateOrDiedrate.src.Config.Sync;
using HydrateOrDiedrate.Tun;
using HydrateOrDiedrate.wellwater;
using HydrateOrDiedrate.Wellwater;
using HydrateOrDiedrate.XSkill;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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

    private IClientNetworkChannel clientChannel;
    private IServerNetworkChannel serverChannel;

    private long customHudListenerId;
    private AquiferManager _aquiferManager;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        LoadConfig(api);
        ItemHydrationConfigLoader.GenerateDefaultHydrationConfig();
        BlockHydrationConfigLoader.GenerateDefaultBlockHydrationConfig();
        CoolingConfigLoader.GenerateDefaultCoolingConfig();
        harmony = new Harmony("com.chronolegionnaire.hydrateordiedrate");
        harmony.PatchAll();
    }

    private void LoadConfig(ICoreAPI api)
    {
        LoadedConfig = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");
        if (LoadedConfig == null)
        {
            LoadedConfig = new Config.Config();
            ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", LoadedConfig);
        }
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        ApplyWaterSatietyPatches(api);
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
            if (block is BlockLiquidContainerBase || block is BlockGroundStorage)
            {
                if (block is BlockKeg)
                {
                    return;
                }
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
    public void ApplyKegTunConfigPatches(ICoreAPI api)
    {
        ApplyKegPatch(api, "hydrateordiedrate:blocktypes/keg.json", LoadedConfig.KegCapacityLitres, LoadedConfig.SpoilRateTapped, LoadedConfig.KegIronHoopDropChance, LoadedConfig.KegTapDropChance);
        ApplyKegPatch(api, "hydrateordiedrate:blocktypes/kegtapped.json", LoadedConfig.KegCapacityLitres, LoadedConfig.SpoilRateUntapped, LoadedConfig.KegIronHoopDropChance, LoadedConfig.KegTapDropChance);
        ApplyTunPatch(api, "hydrateordiedrate:blocktypes/tun.json", LoadedConfig.TunCapacityLitres, LoadedConfig.TunSpoilRateMultiplier);
    }

    private void ApplyKegPatch(ICoreAPI api, string jsonFilePath, float kegCapacityLitres, float spoilRate, float ironHoopDropChance, float kegTapDropChance)
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
    patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegcapacitypatch"), patchKegCapacity, ref applied, ref notFound, ref errorCount);
    patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegspoilratepatch"), patchSpoilRate, ref applied, ref notFound, ref errorCount);
    patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicironhooppatch"), patchIronHoop, ref applied, ref notFound, ref errorCount);
    patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegtappatch"), patchKegTap, ref applied, ref notFound, ref errorCount);
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
        LoadedConfig = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");

        api.RegisterEntityBehaviorClass("bodytemperaturehot", typeof(EntityBehaviorBodyTemperatureHot));
        api.RegisterEntityBehaviorClass("liquidencumbrance", typeof(EntityBehaviorLiquidEncumbrance));
        if (!api.ModLoader.IsModEnabled("carryon"))
        {
            RegisterEmptyCarryBehaviors(api);
        }
        api.RegisterBlockClass("BlockKeg", typeof(BlockKeg));
        api.RegisterBlockEntityClass("BlockEntityKeg", typeof(BlockEntityKeg));
        api.RegisterItemClass("ItemKegTap", typeof(ItemKegTap));
        api.RegisterItemClass("DigWellToolMode", typeof(DigWellToolMode));
        api.RegisterBlockClass("BlockTun", typeof(BlockTun));
        api.RegisterBlockEntityClass("BlockEntityTun", typeof(BlockEntityTun));
        api.RegisterBlockEntityClass("BlockEntityWellWaterData", typeof(BlockEntityWellWaterData));
        api.RegisterBlockBehaviorClass("BlockBehaviorWellWaterFinite", typeof(BlockBehaviorWellWaterFinite));
        api.RegisterBlockClass("BlockWellSpring", typeof(blockWellSpring));
        api.RegisterBlockEntityClass("BlockEntityWellSpring", typeof(BlockEntityWellSpring));
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

        foreach (var block in api.World.Blocks)
        {
            if (block is BlockLiquidContainerBase || block is BlockGroundStorage)
            {
                AddBehaviorToBlock(block, api);
            }
        }
        if (api.Side == EnumAppSide.Server)
        {
            HydrateOrDiedrateGlobals.InitializeAquiferSystem(api as ICoreServerAPI);
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

    private void RegisterEmptyCarryBehaviors(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("Carryable", typeof(EmptyBlockBehavior));
        api.RegisterBlockBehaviorClass("CarryableInteract", typeof(EmptyBlockBehavior));
        //TODO carryon behaviors should just only be added when CarryOn is available
    }

    public class EmptyBlockBehavior : BlockBehavior
    {
        public EmptyBlockBehavior(Block block) : base(block)
        {
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }
    }
    
    private void InitializeServer(ICoreServerAPI api)
    {
        _serverApi = api;

        serverChannel = api.Network.RegisterChannel("hydrateordiedrate")
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<ConfigSyncPacket>()
            .RegisterMessageType<ConfigSyncRequestPacket>()
            .SetMessageHandler<ConfigSyncRequestPacket>(OnConfigSyncRequestReceived);
        
        _waterInteractionHandler.Initialize(serverChannel);
        
        rainHarvesterManager = new RainHarvesterManager(_serverApi, LoadedConfig);
        
        api.Event.PlayerJoin += OnPlayerJoinOrNowPlaying;
        api.Event.PlayerNowPlaying += OnPlayerJoinOrNowPlaying;
        api.Event.PlayerRespawn += OnPlayerRespawn;
        
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

    private void OnConfigSyncRequestReceived(IServerPlayer fromPlayer, ConfigSyncRequestPacket packet)
    {
        var configSyncPacket = new ConfigSyncPacket
        {
            ServerConfig = LoadedConfig,
            HydrationPatches = HydrationManager.GetLastAppliedPatches().ConvertAll(patch => patch.ToString()),
            BlockHydrationPatches = BlockHydrationManager.GetLastAppliedPatches().ConvertAll(patch => patch.ToString()),
            CoolingPatches = CoolingManager.GetLastAppliedPatches().ConvertAll(patch => patch.ToString())
        };

        serverChannel.SendPacket(configSyncPacket, fromPlayer);
    }

    private void OnConfigSyncReceived(ConfigSyncPacket packet)
    {
        LoadedConfig = new Config.Config
        {
            MaxThirst = packet.ServerConfig.MaxThirst,
            ThirstDamage = packet.ServerConfig.ThirstDamage,
            ThirstDecayRate = packet.ServerConfig.ThirstDecayRate,
            ThirstIncreasePerDegreeMultiplier = packet.ServerConfig.ThirstIncreasePerDegreeMultiplier,
            ThirstDecayRateMax = packet.ServerConfig.ThirstDecayRateMax,
            HydrationLossDelayMultiplier = packet.ServerConfig.HydrationLossDelayMultiplier,
            EnableThirstMechanics = packet.ServerConfig.EnableThirstMechanics,
            WaterSatiety = packet.ServerConfig.WaterSatiety,
            SaltWaterSatiety = packet.ServerConfig.SaltWaterSatiety,
            BoilingWaterSatiety = packet.ServerConfig.BoilingWaterSatiety,
            RainWaterSatiety = packet.ServerConfig.RainWaterSatiety,
            DistilledWaterSatiety = packet.ServerConfig.DistilledWaterSatiety,
            BoiledWaterSatiety = packet.ServerConfig.BoiledWaterSatiety,
            MaxMovementSpeedPenalty = packet.ServerConfig.MaxMovementSpeedPenalty,
            MovementSpeedPenaltyThreshold = packet.ServerConfig.MovementSpeedPenaltyThreshold,
            HarshHeat = packet.ServerConfig.HarshHeat,
            TemperatureThreshold = packet.ServerConfig.TemperatureThreshold,
            HarshHeatExponentialGainMultiplier = packet.ServerConfig.HarshHeatExponentialGainMultiplier,
            BoilingWaterDamage = packet.ServerConfig.BoilingWaterDamage,
            EnableBoilingWaterDamage = packet.ServerConfig.EnableBoilingWaterDamage,
            UnequippedSlotCooling = packet.ServerConfig.UnequippedSlotCooling,
            WetnessCoolingFactor = packet.ServerConfig.WetnessCoolingFactor,
            ShelterCoolingFactor = packet.ServerConfig.ShelterCoolingFactor,
            SunlightCoolingFactor = packet.ServerConfig.SunlightCoolingFactor,
            DiurnalVariationAmplitude = packet.ServerConfig.DiurnalVariationAmplitude,
            RefrigerationCooling = packet.ServerConfig.RefrigerationCooling,
            SprintThirstMultiplier = packet.ServerConfig.SprintThirstMultiplier,
            EnableLiquidEncumbrance = packet.ServerConfig.EnableLiquidEncumbrance,
            EncumbranceLimit = packet.ServerConfig.EncumbranceLimit,
            LiquidEncumbranceMovementSpeedDebuff = packet.ServerConfig.LiquidEncumbranceMovementSpeedDebuff,
            DromedaryMultiplierPerLevel = packet.ServerConfig.DromedaryMultiplierPerLevel,
            EquatidianCoolingMultipliers = packet.ServerConfig.EquatidianCoolingMultipliers != null
                ? (float[])packet.ServerConfig.EquatidianCoolingMultipliers.Clone()
                : null,
            EnableRainGathering = packet.ServerConfig.EnableRainGathering,
            RainMultiplier = packet.ServerConfig.RainMultiplier,
            KegCapacityLitres = packet.ServerConfig.KegCapacityLitres,
            SpoilRateUntapped = packet.ServerConfig.SpoilRateUntapped,
            SpoilRateTapped = packet.ServerConfig.SpoilRateTapped,
            KegIronHoopDropChance = packet.ServerConfig.KegIronHoopDropChance,
            KegTapDropChance = packet.ServerConfig.KegTapDropChance,
            TunCapacityLitres = packet.ServerConfig.TunCapacityLitres,
            TunSpoilRateMultiplier = packet.ServerConfig.TunSpoilRateMultiplier,
            DisableDrunkSway = packet.ServerConfig.DisableDrunkSway
        };
        ReloadComponents();
        var currentHydrationPatches = HydrationManager.GetLastAppliedPatches() ?? new List<JObject>();
        var packetHydrationPatches = packet.HydrationPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        bool hydrationPatchesChanged = !ArePatchesEqual(packetHydrationPatches, currentHydrationPatches);
        var currentBlockHydrationPatches = BlockHydrationManager.GetLastAppliedPatches()?.ConvertAll(jsonObj => jsonObj.Token as JObject) ?? new List<JObject>();
        var packetBlockHydrationPatches = packet.BlockHydrationPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        bool blockHydrationPatchesChanged = !ArePatchesEqual(packetBlockHydrationPatches, currentBlockHydrationPatches);
        var currentCoolingPatches = CoolingManager.GetLastAppliedPatches() ?? new List<JObject>();
        var packetCoolingPatches = packet.CoolingPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        bool coolingPatchesChanged = !ArePatchesEqual(packetCoolingPatches, currentCoolingPatches);
        if (hydrationPatchesChanged)
        {
            HydrationManager.ApplyHydrationPatches(_clientApi, packetHydrationPatches);
        }
        if (blockHydrationPatchesChanged)
        {
            var convertedBlockHydrationPatches = packetBlockHydrationPatches
                .ConvertAll(jObj => new Vintagestory.API.Datastructures.JsonObject(jObj));
            BlockHydrationManager.ApplyBlockHydrationPatches(_clientApi, convertedBlockHydrationPatches, _clientApi.World.Blocks);
        }
        if (coolingPatchesChanged)
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

    private void OnPlayerJoinOrNowPlaying(IServerPlayer byPlayer)
    {
        var entity = byPlayer.Entity;
        if (entity == null) return;
        //TODO revamp (this shouldn't be registered per player and definitly not every time player joins/respawns)
        entity.WatchedAttributes.SetBool("isFullyInitialized", true);
        _serverApi.Event.RegisterGameTickListener(
            (dt) => _waterInteractionHandler.CheckShiftRightClickBeforeInteractionForPlayer(dt, byPlayer), 100
        );
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

        entity.WatchedAttributes.SetBool("isFullyInitialized", true);
        
        _serverApi.Event.RegisterGameTickListener(
            (dt) => _waterInteractionHandler.CheckShiftRightClickBeforeInteractionForPlayer(dt, byPlayer), 100
        );
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

    public override void Dispose()
    {
        harmony.UnpatchAll("com.chronolegionnaire.hydrateordiedrate");
        base.Dispose();
    }
}