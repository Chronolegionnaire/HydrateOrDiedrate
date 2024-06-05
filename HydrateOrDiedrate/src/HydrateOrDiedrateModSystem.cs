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
        private float _maxThirst = 10f;
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
            EntityBehaviorThirst.SetInitialThirst(byPlayer, _maxThirst);
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            EntityBehaviorThirst.ResetThirstOnRespawn(byPlayer, _maxThirst);
        }

        private void OnServerTick(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, _maxThirst);
            }
        }
    }
}
