using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Gui;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using HydrateOrDiedrate.Commands;

namespace HydrateOrDiedrate
{
    public class HydrateOrDiedrateModSystem : ModSystem
    {
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        private HudElementThirstBar _thirstHud;
        public static Config LoadedConfig;
        private WaterInteractionHandler _waterInteractionHandler;
        private Harmony harmony;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            LoadConfig(api);
            HydrationConfigLoader.GenerateDefaultHydrationConfig(api);
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
            LoadAndApplyHydrationPatches(api);
        }

        private void LoadAndApplyHydrationPatches(ICoreAPI api)
        {
            List<JObject> hydrationPatches = HydrationConfigLoader.LoadHydrationPatches(api);
            HydrationManager.ApplyHydrationPatches(api, hydrationPatches);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            LoadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
            api.RegisterEntityBehaviorClass("thirst", typeof(EntityBehaviorThirst));
            _waterInteractionHandler = new WaterInteractionHandler(api, LoadedConfig);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _serverApi = api;

            api.Event.PlayerJoin += OnPlayerJoinOrNowPlaying;
            api.Event.PlayerNowPlaying += OnPlayerJoinOrNowPlaying;
            api.Event.PlayerRespawn += OnPlayerRespawn;
            api.Event.RegisterGameTickListener(OnServerTick, 1000);
            api.Event.RegisterGameTickListener(_waterInteractionHandler.CheckPlayerInteraction, 100);

            ThirstCommands.Register(api, LoadedConfig);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _clientApi = api;
            _thirstHud = new HudElementThirstBar(api);
            api.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            api.Gui.RegisterDialog(_thirstHud);
        }

        private void OnPlayerJoinOrNowPlaying(IServerPlayer byPlayer)
        {
            var entity = byPlayer.Entity;
            EnsureThirstBehavior(entity);
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            var entity = byPlayer.Entity;
            var thirstBehavior = EnsureThirstBehavior(entity);
            thirstBehavior.ResetThirstOnRespawn();
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


        private void OnServerTick(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, LoadedConfig);
            }
        }

        public override void Dispose()
        {
            harmony.UnpatchAll("com.chronolegionnaire.hydrateordiedrate");
            base.Dispose();
        }
    }
}
