using System.Collections.Generic;
using HarmonyLib;
using HydrateOrDiedrate;
using HydrateOrDiedrate.Commands;
using HydrateOrDiedrate.Compatibility;
using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Gui;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

public class HydrateOrDiedrateModSystem : ModSystem
{
    private ICoreServerAPI _serverApi;
    private ICoreClientAPI _clientApi;
    private HudElementThirstBar _thirstHud;
    public static Config LoadedConfig;
    private WaterInteractionHandler _waterInteractionHandler;
    private Harmony harmony;
    private ConfigLibCompatibility _configLibCompatibility;
    private XLibSkills xLibSkills;

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
        LoadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json") ?? new Config(api);
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
        LoadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");

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

        // Initialize XLibSkills if XLib is installed
        if (api.ModLoader.IsModEnabled("xlib"))
        {
            xLibSkills = new XLibSkills();
            xLibSkills.Initialize(api);
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        _serverApi = api;

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

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;

        if (LoadedConfig.EnableThirstMechanics)
        {
            _thirstHud = new HudElementThirstBar(api);
            api.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            api.Gui.RegisterDialog(_thirstHud);
        }
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

    public override void Dispose()
    {
        harmony.UnpatchAll("com.chronolegionnaire.hydrateordiedrate");
        base.Dispose();
    }
}
