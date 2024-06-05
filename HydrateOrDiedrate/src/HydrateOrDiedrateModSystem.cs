using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate
{
    public class HydrateOrDiedrateModSystem : ModSystem
    {
        private ICoreServerAPI _serverApi;
        private float _maxThirst = 50f;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            _serverApi = api;

            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.RegisterGameTickListener(OnServerTick, 3000); // Every 3 seconds
            api.Event.PlayerRespawn += OnPlayerRespawn;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            // Create an instance of the HudElementThirstBar
            var thirstHud = new Gui.HudElementThirstBar(api); 

            // Register the thirstHud element
            api.Event.RegisterGameTickListener(thirstHud.OnGameTick, 20);
            api.Event.RegisterGameTickListener(thirstHud.OnFlashStatbar, 1000);
            api.Gui.RegisterDialog(thirstHud);
        }

        private void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (!byPlayer.Entity.WatchedAttributes.HasAttribute("currentThirst"))
            {
                byPlayer.Entity.WatchedAttributes.SetFloat("currentThirst", _maxThirst);
            }

            byPlayer.Entity.WatchedAttributes.SetFloat("maxThirst", _maxThirst);
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            byPlayer.Entity.WatchedAttributes.SetFloat("currentThirst", 0.5f);
            byPlayer.Entity.WatchedAttributes.SetFloat("maxThirst", _maxThirst);
        }

        private void OnServerTick(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                float currentThirst = player.Entity.WatchedAttributes.GetFloat("currentThirst", _maxThirst);

                currentThirst -= 0.05f * dt; // Adjust thirst drain rate
                currentThirst = GameMath.Clamp(currentThirst, 0f, _maxThirst);

                if (currentThirst <= 0)
                {
                    player.Entity.Stats.Set("walkspeed", "global", 0.5f, true); // Slow player

                    player.Entity.ReceiveDamage(new DamageSource()
                    {
                        Source = EnumDamageSource.Internal,
                        Type = EnumDamageType.Hunger // Use hunger damage type
                    }, player.Entity.Stats.GetBlended("health") * 0.05f * dt);
                }
                else
                {
                    player.Entity.Stats.Set("walkspeed", "global", 1f, true); // Reset speed
                }

                player.Entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
                player.Entity.Stats.Set("thirst", "", currentThirst, true); // Update thirst
            }
        }
    }
}
