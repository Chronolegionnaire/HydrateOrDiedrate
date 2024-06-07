using Vintagestory.API.Common;
using Vintagestory.API.Server;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Gui;
using Vintagestory.API.Client;
using HydrateOrDiedrate.Configuration;

namespace HydrateOrDiedrate
{
    public class HydrateOrDiedrateModSystem : ModSystem
    {
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        private HudElementThirstBar _thirstHud;
        public static Config LoadedConfig;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            try
            {
                Config loaded;
                if ((loaded = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json")) == null)
                {
                    LoadedConfig = new Config(api);
                    ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", LoadedConfig);
                }
                else
                {
                    LoadedConfig = loaded;
                }
            }
            catch
            {
                LoadedConfig = new Config(api);
                ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", LoadedConfig);
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            LoadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _serverApi = api;

            api.Event.PlayerJoin += OnPlayerJoin;
            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.PlayerRespawn += OnPlayerRespawn;
            api.Event.RegisterGameTickListener(OnServerTick, 1000);

            api.ChatCommands
                .Create("setthirst")
                .WithDescription("Sets the player's thirst level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.Float("thirstValue"))
                .HandleWith(OnSetThirstCommand);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _clientApi = api;
            _thirstHud = new HudElementThirstBar(api);
            api.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            api.Event.RegisterGameTickListener(_thirstHud.OnFlashStatbar, 1000);
            api.Gui.RegisterDialog(_thirstHud);

            api.ChatCommands
                .Create("setthirst")
                .WithDescription("Sets the player's thirst level.")
                .HandleWith(OnSetThirstCommand);
        }

        private void OnPlayerJoin(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
            {
                thirstBehavior = new EntityBehaviorThirst(byPlayer.Entity, LoadedConfig);
                byPlayer.Entity.AddBehavior(thirstBehavior);
            }

            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative) return;

            if (!byPlayer.Entity.WatchedAttributes.HasAttribute("currentThirst"))
            {
                thirstBehavior.SetInitialThirst(); // Initialize thirst for new players
            }
        }

        private void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
            {
                thirstBehavior = new EntityBehaviorThirst(byPlayer.Entity, LoadedConfig);
                byPlayer.Entity.AddBehavior(thirstBehavior);
            }

            thirstBehavior.LoadThirst(); // Load the saved thirst value for returning players
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.ResetThirstOnRespawn(byPlayer.Entity);
            }
        }

        private void OnServerTick(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                EntityBehaviorThirst.UpdateThirstOnServerTick(player, dt, LoadedConfig);
            }
        }

        private TextCommandResult OnSetThirstCommand(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player as IServerPlayer;
            if (player == null) return TextCommandResult.Error("Player not found.");

            var entity = player.Entity;
            if (entity == null) return TextCommandResult.Error("Player entity not found.");

            var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            if (args.ArgCount < 1) return TextCommandResult.Error("Missing thirst value.");

            if (!float.TryParse(args[0].ToString(), out float thirstValue))
            {
                return TextCommandResult.Error("Invalid thirst value. Please enter a valid number.");
            }

            thirstBehavior.CurrentThirst = thirstValue;
            thirstBehavior.UpdateThirstAttributes();

            return TextCommandResult.Success($"Thirst set to {thirstValue}.");
        }
    }
}
