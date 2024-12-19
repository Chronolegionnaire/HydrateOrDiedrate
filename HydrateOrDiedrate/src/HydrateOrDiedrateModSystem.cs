using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.encumbrance;
using HydrateOrDiedrate.Hot_Weather;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Keg;
using HydrateOrDiedrate.Tun;
using HydrateOrDiedrate.wellwater;
using HydrateOrDiedrate.XSkill;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
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
    private AquiferSystem aquiferSystem;

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

        ApplySatietyPatch(api, "game:itemtypes/liquid/waterportion.json", waterSatiety);
        ApplySatietyPatch(api, "game:itemtypes/liquid/saltwaterportion.json", saltWaterSatiety);
        ApplySatietyPatch(api, "game:itemtypes/liquid/boilingwaterportion.json", boilingWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/rainwaterportion.json", rainWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/distilledwaterportion.json", distilledWaterSatiety);
    }
    private void ApplySatietyPatch(ICoreAPI api, string jsonFilePath, float satietyValue)
    {
        JsonPatch patch = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre",
            Value = new JsonObject(JObject.FromObject(new
            {
                satiety = satietyValue,
                foodcategory = "NoNutrition"
            })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        int applied = 0;
        int notFound = 0;
        int errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicwaterpatch"), patch, ref applied, ref notFound, ref errorCount);
    }
    public void ApplyKegTunConfigPatches(ICoreAPI api)
    {
        ApplyKegPatch(api, "hydrateordiedrate:blocktypes/keg.json", LoadedConfig.KegCapacityLitres, LoadedConfig.SpoilRateTapped, LoadedConfig.KegIronHoopDropChance, LoadedConfig.KegTapDropChance);
        ApplyKegPatch(api, "hydrateordiedrate:blocktypes/kegtapped.json", LoadedConfig.KegCapacityLitres, LoadedConfig.SpoilRateUntapped, LoadedConfig.KegIronHoopDropChance, LoadedConfig.KegTapDropChance);
        ApplyTunPatch(api, "hydrateordiedrate:blocktypes/tun.json", LoadedConfig.TunCapacityLitres, LoadedConfig.TunSpoilRateMultiplier);
    }

    private void ApplyKegPatch(ICoreAPI api, string jsonFilePath, float capacityLitres, float spoilRate, float ironHoopDropChance, float kegTapDropChance)
    {
        JsonPatch patch = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes",
            Value = new JsonObject(JObject.FromObject(new
            {
                kegCapacityLitres = capacityLitres,
                spoilRate = spoilRate,
                ironHoopDropChance = ironHoopDropChance,
                kegTapDropChance = kegTapDropChance
            })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0;
        int notFound = 0;
        int errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamickegpatch"), patch, ref applied, ref notFound, ref errorCount);
    }
    private void ApplyTunPatch(ICoreAPI api, string jsonFilePath, float capacityLitres, float spoilRate)
    {
        JsonPatch patch = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes",
            Value = new JsonObject(JObject.FromObject(new
            {
                TunCapacityLitres = capacityLitres,
                spoilRate = spoilRate
            })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0;
        int notFound = 0;
        int errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamictunpatch"), patch, ref applied, ref notFound, ref errorCount);
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
            List<JObject> loadedPatches = BlockHydrationConfigLoader.LoadBlockHydrationConfig(api);
            BlockHydrationManager.ApplyBlockHydrationPatches(loadedPatches);
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
        
        api.RegisterBlockClass("BlockTun", typeof(BlockTun));
        api.RegisterBlockEntityClass("BlockEntityTun", typeof(BlockEntityTun));
        
        api.RegisterBlockClass("BlockwellWater", typeof(BlockWellWater));
        api.RegisterBlockClass("BlockwellWaterfall", typeof(BlockWellWaterfall));
        api.RegisterBlockClass("BlockwellWaterflowing", typeof(BlockWellWaterflowing));
        api.RegisterBlockBehaviorClass("BlockBehaviorWellWater", typeof(BlockBehaviorWellWater));
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
        public static AquiferSystem AquiferSystem { get; private set; }

        public static void InitializeAquiferSystem(ICoreServerAPI api)
        {
            AquiferSystem = new AquiferSystem(api);
        }
    }
    private void RegisterEmptyCarryBehaviors(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("Carryable", typeof(EmptyBlockBehavior));
        api.RegisterBlockBehaviorClass("CarryableInteract", typeof(EmptyBlockBehavior));
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
        api.Event.RegisterGameTickListener(OnServerTick, 1000);
        if (LoadedConfig.EnableThirstMechanics)
        {
            ThirstCommands.Register(api, LoadedConfig);
        }
        RegisterClearAquiferCommand(api);
    }
    private void RegisterClearAquiferCommand(ICoreServerAPI api)
    {
        api.RegisterCommand(
            "clearaquiferdata", 
            "Clears all saved aquifer data", 
            "", 
            (player, groupId, args) =>
            {
                if (aquiferSystem == null)
                {
                    api.SendMessage(player, groupId, "Aquifer system is not initialized.", EnumChatType.Notification);
                    return;
                }

                aquiferSystem.ClearAquiferData();
                api.SendMessage(player, groupId, "Aquifer data has been cleared successfully.", EnumChatType.Notification);
            }, 
            Privilege.controlserver 
        );
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
        _configLibCompatibility = new ConfigLibCompatibility((ICoreClientAPI)api);
    }

    [ProtoContract]
    public class ConfigSyncPacket
    {
        [ProtoMember(1)] public Config.Config ServerConfig { get; set; }

        [ProtoMember(2)] public List<string> HydrationPatches { get; set; }

        [ProtoMember(3)] public List<string> BlockHydrationPatches { get; set; }

        [ProtoMember(4)] public List<string> CoolingPatches { get; set; }
    }

    [ProtoContract]
    public class ConfigSyncRequestPacket
    {
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
            TunSpoilRateMultiplier = packet.ServerConfig.TunSpoilRateMultiplier
        };

        ReloadComponents();


        var currentHydrationPatches = HydrationManager.GetLastAppliedPatches() ?? new List<JObject>();
        var packetHydrationPatches = packet.HydrationPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
        bool hydrationPatchesChanged = !ArePatchesEqual(packetHydrationPatches, currentHydrationPatches);

        var currentBlockHydrationPatches = BlockHydrationManager.GetLastAppliedPatches() ?? new List<JObject>();
        var packetBlockHydrationPatches =
            packet.BlockHydrationPatches?.ConvertAll(JObject.Parse) ?? new List<JObject>();
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
            BlockHydrationManager.ApplyBlockHydrationPatches(packetBlockHydrationPatches);
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
        
        if (!HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics)
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

        EnsureBodyTemperatureHotBehavior(entity);
        EnsureLiquidEncumbranceBehavior(entity);

        if (LoadedConfig.EnableThirstMechanics)
        {
            EnsureThirstBehavior(entity);
        }
        
        entity.WatchedAttributes.SetBool("isFullyInitialized", true);
        
        _serverApi.Event.RegisterGameTickListener(
            (dt) => _waterInteractionHandler.CheckShiftRightClickBeforeInteractionForPlayer(dt, byPlayer), 100
        );
    }


    private void OnPlayerRespawn(IServerPlayer byPlayer)
    {
        var entity = byPlayer.Entity;
        if (entity == null) return;

        EnsureBodyTemperatureHotBehavior(entity);
        EnsureLiquidEncumbranceBehavior(entity);

        if (LoadedConfig.EnableThirstMechanics)
        {
            var thirstBehavior = EnsureThirstBehavior(entity);
            thirstBehavior.ResetThirstOnRespawn();
        }

        entity.WatchedAttributes.SetBool("isFullyInitialized", true);
        
        _serverApi.Event.RegisterGameTickListener(
            (dt) => _waterInteractionHandler.CheckShiftRightClickBeforeInteractionForPlayer(dt, byPlayer), 100
        );
    }

    private EntityBehaviorThirst EnsureThirstBehavior(Entity entity)
    {
        var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior == null)
        {
            thirstBehavior = new EntityBehaviorThirst(entity, LoadedConfig);
            entity.AddBehavior(thirstBehavior);
        }

        if (!entity.WatchedAttributes.HasAttribute("currentThirst"))
        {
            thirstBehavior.SetInitialThirst();
        }
        else
        {
            thirstBehavior.LoadThirst();
        }

        return thirstBehavior;
    }

    private void EnsureBodyTemperatureHotBehavior(Entity entity)
    {
        var bodyTemperatureHotBehavior = entity.GetBehavior<EntityBehaviorBodyTemperatureHot>();
        if (bodyTemperatureHotBehavior == null)
        {
            bodyTemperatureHotBehavior = new EntityBehaviorBodyTemperatureHot(entity, LoadedConfig);
            entity.AddBehavior(bodyTemperatureHotBehavior);
        }
    }

    private void EnsureLiquidEncumbranceBehavior(Entity entity)
    {
        var liquidEncumbranceBehavior = entity.GetBehavior<EntityBehaviorLiquidEncumbrance>();
        if (liquidEncumbranceBehavior == null)
        {
            liquidEncumbranceBehavior = new EntityBehaviorLiquidEncumbrance(entity, LoadedConfig);
            entity.AddBehavior(liquidEncumbranceBehavior);
        }
    }

    private void OnServerTick(float dt)
    {
        if (LoadedConfig.EnableThirstMechanics)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                if (player.Entity != null && player.Entity.Alive)
                {
                    if (!player.Entity.WatchedAttributes.GetBool("isFullyInitialized", false)) continue;

                    var gameMode = player.WorldData.CurrentGameMode;
                    if (gameMode != EnumGameMode.Creative && gameMode != EnumGameMode.Spectator && gameMode != EnumGameMode.Guest)
                    {
                        EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, LoadedConfig);
                    }
                }
            }
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

    public override void Dispose()
    {
        harmony.UnpatchAll("com.chronolegionnaire.hydrateordiedrate");
        base.Dispose();
    }
}