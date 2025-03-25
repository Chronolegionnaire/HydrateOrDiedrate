using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HydrateOrDiedrate.Commands;
using HydrateOrDiedrate.Config;
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
    public static AquiferManager AquiferManager { get; private set; }
    private ConfigLibCompatibility _configLibCompatibility;
    private XLibSkills xLibSkills;

    private RainHarvesterManager rainHarvesterManager;
    private DrinkHudOverlayRenderer hudOverlayRenderer;

    public static IClientNetworkChannel clientChannel;
    private IServerNetworkChannel serverChannel;
    private long customHudListenerId;
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        ItemHydrationConfigLoader.GenerateDefaultHydrationConfig();
        BlockHydrationConfigLoader.GenerateDefaultBlockHydrationConfig();
        CoolingConfigLoader.GenerateDefaultCoolingConfig();
        harmony = new Harmony("com.chronolegionnaire.hydrateordiedrate");
        harmony.PatchAll();
        var initConfig = new InitConfig();
        initConfig.LoadConfig(api);
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        var waterPatches = new WaterPatches();
        waterPatches.PrepareWaterSatietyPatches(api);
        waterPatches.PrepareWellWaterSatietyPatches(api);
        waterPatches.PrepareWaterPerishPatches(api);
        waterPatches.PrepareWellWaterPerishPatches(api);
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

        if (api.ModLoader.IsModEnabled("xlib") || api.ModLoader.IsModEnabled("xlibpatch"))
        {
            xLibSkills = new XLibSkills();
            xLibSkills.Initialize(api);
        }

        api.RegisterBlockEntityBehaviorClass("RainHarvester", typeof(RegisterRainHarvester));

        if (api.ModLoader.IsModEnabled("BetterProspecting"))
        {
            BetterProspectingAquiferPatch.Apply(api);
        }
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _serverApi = api;
        base.StartServerSide(api);
        string configJson = JsonConvert.SerializeObject(LoadedConfig, Formatting.Indented);
        byte[] configBytes = System.Text.Encoding.UTF8.GetBytes(configJson);
        string base64Config = Convert.ToBase64String(configBytes);
        api.World.Config.SetString("HydrateOrDiedrateConfig", base64Config);
        serverChannel = api.Network.RegisterChannel("hydrateordiedrate")
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<WellSpringBlockPacket>()
            .SetMessageHandler<WellSpringBlockPacket>(WellSpringBlockPacketReceived);
        AquiferManager = new AquiferManager(api);
        _waterInteractionHandler.Initialize(serverChannel);
        rainHarvesterManager = new RainHarvesterManager(_serverApi, LoadedConfig);
        api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        api.Event.PlayerRespawn += OnPlayerRespawn;
        api.Event.RegisterGameTickListener(CheckPlayerInteraction, 100);

        ThirstCommands.Register(api, LoadedConfig);
        AquiferCommands.Register(api);
    }


    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;
        base.StartClientSide(api);

        clientChannel = api.Network.RegisterChannel("hydrateordiedrate")
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<WellSpringBlockPacket>()
            .SetMessageHandler<DrinkProgressPacket>(OnDrinkProgressReceived);

        string base64Config = api.World.Config.GetString("HydrateOrDiedrateConfig", "");
        if (!string.IsNullOrWhiteSpace(base64Config))
        {
            try
            {
                byte[] configBytes = Convert.FromBase64String(base64Config);
                string configJson = System.Text.Encoding.UTF8.GetString(configBytes);
                LoadedConfig = JsonConvert.DeserializeObject<Config.Config>(configJson);
            }
            catch (Exception ex)
            {
                api.Logger.Error("Failed to deserialize HydrateOrDiedrate config: " + ex);
                LoadedConfig = new Config.Config();
            }
        }
        else
        {
            api.Logger.Warning("HydrateOrDiedrate config not found in world config; using defaults.");
            LoadedConfig = new Config.Config();
        }
        hudOverlayRenderer = new DrinkHudOverlayRenderer(api);
        api.Event.RegisterRenderer(hudOverlayRenderer, EnumRenderStage.Ortho, "drinkoverlay");

        if (LoadedConfig.EnableThirstMechanics)
        {
            customHudListenerId = api.Event.RegisterGameTickListener(CheckAndInitializeCustomHud, 20);
        }

        _configLibCompatibility = new ConfigLibCompatibility(api);
    }
    public RainHarvesterManager GetRainHarvesterManager()
    {
        return rainHarvesterManager;
    }
    private void OnDrinkProgressReceived(DrinkProgressPacket msg)
    {
        if (hudOverlayRenderer == null) return;
        hudOverlayRenderer.ProcessDrinkProgress(msg.Progress, msg.IsDrinking, msg.IsDangerous);
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
            if(player.ConnectionState != EnumClientState.Playing) continue;
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
            bool dromedaryActive = entity.WatchedAttributes.GetBool("dromedaryActive", false);
            if (!dromedaryActive)
            {
                float defaultMaxThirst = HydrateOrDiedrateModSystem.LoadedConfig.MaxThirst;
                thirstBehavior.CurrentThirst = thirstBehavior.CurrentThirst / thirstBehavior.MaxThirst * defaultMaxThirst;
                thirstBehavior.MaxThirst = defaultMaxThirst;
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
    }
    public override void Dispose()
    {
        harmony.UnpatchAll("com.chronolegionnaire.hydrateordiedrate");
        base.Dispose();
    }
}