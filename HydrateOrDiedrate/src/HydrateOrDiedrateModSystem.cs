using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.encumbrance;
using HydrateOrDiedrate.Hot_Weather;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.XSkill;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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
    public static Config.Config LoadedConfig;
    private WaterInteractionHandler _waterInteractionHandler;
    private Harmony harmony;
    private ConfigLibCompatibility _configLibCompatibility;
    private XLibSkills xLibSkills;

    private RainHarvesterManager rainHarvesterManager;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        LoadConfig(api);
        ItemHydrationConfigLoader.GenerateDefaultHydrationConfig(api);
        BlockHydrationConfigLoader.GenerateDefaultBlockHydrationConfig(api);
        CoolingConfigLoader.GenerateDefaultCoolingConfig(api);
        harmony = new Harmony("com.chronolegionnaire.hydrateordiedrate");
        harmony.PatchAll();
    }

    private void LoadConfig(ICoreAPI api)
    {
        LoadedConfig = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json") ?? new Config.Config(api);
        ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", LoadedConfig);
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
            List<JObject> blockHydrationPatches = BlockHydrationConfigLoader.LoadBlockHydrationConfig(api);
            BlockHydrationManager.ApplyBlockHydrationPatches(blockHydrationPatches);
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

        if (LoadedConfig.EnableThirstMechanics)
        {
            api.RegisterEntityBehaviorClass("thirst", typeof(EntityBehaviorThirst));
        }

        _waterInteractionHandler = new WaterInteractionHandler(api, LoadedConfig);
    
        if (api.Side == EnumAppSide.Client)
        {
            _configLibCompatibility = new ConfigLibCompatibility((ICoreClientAPI)api);
        }
        if (api.ModLoader.IsModEnabled("xlib"))
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
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        _serverApi = api;
        rainHarvesterManager = new RainHarvesterManager(_serverApi);

        api.Event.PlayerJoin += OnPlayerJoinOrNowPlaying;
        api.Event.PlayerNowPlaying += OnPlayerJoinOrNowPlaying;
        api.Event.PlayerRespawn += OnPlayerRespawn;
        api.Event.RegisterGameTickListener(OnServerTick, 1000);
        api.Event.RegisterGameTickListener(_waterInteractionHandler.CheckPlayerInteraction, 100);

        if (LoadedConfig.EnableThirstMechanics)
        {
            ThirstCommands.Register(api, LoadedConfig);
        }
    }

    public RainHarvesterManager GetRainHarvesterManager()
    {
        return rainHarvesterManager;
    }

    private long customHudListenerId;

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;

        if (LoadedConfig.EnableThirstMechanics)
        {
            customHudListenerId = api.Event.RegisterGameTickListener(CheckAndInitializeCustomHud, 20);
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
            liquidEncumbranceBehavior = new EntityBehaviorLiquidEncumbrance(entity);
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
                    var gameMode = player.WorldData.CurrentGameMode;
                    if (gameMode != EnumGameMode.Creative && gameMode != EnumGameMode.Spectator && gameMode != EnumGameMode.Guest)
                    {
                        EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, LoadedConfig);
                    }
                }
            }
        }
    }

    public static bool XSkillActive(ICoreAPI api)
    {
        return api.ModLoader.IsModEnabled("xskills");
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