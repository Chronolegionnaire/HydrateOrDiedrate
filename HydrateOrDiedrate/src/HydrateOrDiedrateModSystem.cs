using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using HydrateOrDiedrate.Gui;

namespace HydrateOrDiedrate
{
    public class HydrateOrDiedrateModSystem : ModSystem
    {
        private ICoreServerAPI _serverApi;
        private ICoreClientAPI _clientApi;
        private HudElementThirstBar _thirstHud;
        public static Config LoadedConfig;
        private WaterInteractionHandler _waterInteractionHandler;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            LoadConfig(api);
        }

        private void LoadConfig(ICoreAPI api)
        {
            LoadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json") ?? new Config(api);
            ModConfig.WriteConfig(api, "HydrateOrDiedrateConfig.json", LoadedConfig);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            LoadedConfig = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
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
            api.Gui.RegisterDialog(_thirstHud);
        }

        private void OnPlayerJoinOrNowPlaying(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>() ?? new EntityBehaviorThirst(byPlayer.Entity, LoadedConfig);
            byPlayer.Entity.AddBehavior(thirstBehavior);

            if (!byPlayer.Entity.WatchedAttributes.HasAttribute("currentThirst"))
            {
                thirstBehavior.SetInitialThirst();
            }
            else
            {
                thirstBehavior.LoadThirst();
            }
        }

        private void OnPlayerRespawn(IServerPlayer byPlayer)
        {
            byPlayer.Entity.GetBehavior<EntityBehaviorThirst>()?.ResetThirstOnRespawn();
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
            if (!(args.Caller.Player is IServerPlayer player)) return TextCommandResult.Error("Player not found.");
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            if (!float.TryParse(args[0].ToString(), out float thirstValue)) return TextCommandResult.Error("Invalid thirst value. Please enter a valid number.");

            thirstBehavior.CurrentThirst = thirstValue;
            thirstBehavior.UpdateThirstAttributes();

            return TextCommandResult.Success($"Thirst set to {thirstValue}.");
        }
    }
}