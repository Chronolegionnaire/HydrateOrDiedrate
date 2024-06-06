using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Gui;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace HydrateOrDiedrate
{
    public class HydrateOrDiedrateModSystem : ModSystem
    {
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        private HudElementThirstBar _thirstHud;
        private ThirstConfig _config;

        public override void StartServerSide(ICoreServerAPI api)
        {
            _serverApi = api;
            LoadConfig(api);

            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.PlayerRespawn += OnPlayerRespawn; // Re-added event
            api.Event.RegisterGameTickListener(OnServerTick, 1000);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _clientApi = api;
            _thirstHud = new HudElementThirstBar(api);
            api.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            api.Event.RegisterGameTickListener(_thirstHud.OnFlashStatbar, 1000);
            api.Gui.RegisterDialog(_thirstHud);
        }

        private void LoadConfig(ICoreAPI api)
        {
            string configDirectory = Path.Combine(GamePaths.Config, "hydrateordiedrate");
            string configPath = Path.Combine(configDirectory, "config.json");

            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            if (!File.Exists(configPath))
            {
                _config = new ThirstConfig();
                api.StoreModConfig(_config, configPath);
            }
            else
            {
                _config = api.LoadModConfig<ThirstConfig>(configPath) ?? new ThirstConfig();
                api.StoreModConfig(_config, configPath);
            }
        }

        private void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
            {
                thirstBehavior = new EntityBehaviorThirst(byPlayer.Entity);
                byPlayer.Entity.AddBehavior(thirstBehavior);
            }

            thirstBehavior.LoadThirst(); // Ensure thirst values are loaded on login
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.ResetThirstOnRespawn(_config);
            }
        }

        private void OnServerTick(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, _config);
            }
        }

        public ThirstConfig GetConfig()
        {
            return _config;
        }
    }

    public class ThirstConfig
    {
        public float MaxThirst { get; set; } = 10f;
        public float ThirstDamage { get; set; } = 1f;
        public float MovementSpeedPenalty { get; set; } = 0.75f;
        public float ThirstDecayRate { get; set; } = 0.01f;
        public float SprintThirstMultiplier { get; set; } = 1.25f;
    }
}
