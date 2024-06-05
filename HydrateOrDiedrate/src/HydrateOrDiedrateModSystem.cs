using Vintagestory.API.Common;
using Vintagestory.API.Server;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Gui;
using Vintagestory.API.Client;

namespace HydrateOrDiedrate
{
    public class HydrateOrDiedrateModSystem : ModSystem
    {
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        private HudElementThirstBar _thirstHud;

        public override void StartServerSide(ICoreServerAPI api)
        {
            _serverApi = api;
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.PlayerRespawn += OnPlayerRespawn;
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

        private void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (!byPlayer.Entity.WatchedAttributes.HasAttribute("currentThirst"))
            {
                EntityBehaviorThirst.SetInitialThirst(byPlayer, 10f); // Default max thirst if not set
            }
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            EntityBehaviorThirst.ResetThirstOnRespawn(byPlayer, 10f); // Default max thirst if not set
        }

        private void OnServerTick(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, 10f); // Default max thirst if not set
            }
        }
    }
}