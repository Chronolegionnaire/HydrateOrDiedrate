using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
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

        private bool isShiftHeld;
        private bool shiftActionFired;

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
            _waterInteractionHandler = new WaterInteractionHandler(api, LoadedConfig);
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

            api.Event.RegisterGameTickListener(CheckPlayerInteraction, 100);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _clientApi = api;
            _thirstHud = new HudElementThirstBar(api);
            api.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            api.Event.RegisterGameTickListener(_thirstHud.OnFlashStatbar, 1000);
            api.Gui.RegisterDialog(_thirstHud);
        }

        private void OnPlayerJoin(IServerPlayer byPlayer)
        {
            var thirstBehavior = byPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
            {
                thirstBehavior = new EntityBehaviorThirst(byPlayer.Entity, LoadedConfig);
                byPlayer.Entity.AddBehavior(thirstBehavior);
            }

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

        private void CheckPlayerInteraction(float dt)
        {
            foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
            {
                if (player.Entity.Controls.Sneak && player.Entity.Controls.RightMouseDown)
                {
                    if (!isShiftHeld)
                    {
                        isShiftHeld = true;
                        HandleWaterInteraction(player);
                    }
                }
                else
                {
                    isShiftHeld = false;
                }
            }
        }

        private void HandleWaterInteraction(IServerPlayer player)
        {
            var blockSel = player.CurrentBlockSelection;
            if (blockSel != null)
            {
                var blockPosAbove = blockSel.Position.UpCopy();
                var block = player.Entity.World.BlockAccessor.GetBlock(blockPosAbove, BlockLayersAccess.Fluid);

                if (block.LiquidCode == "water" || block.LiquidCode == "saltwater" || block.LiquidCode == "boilingwater")
                {
                    _serverApi.Logger.Debug("Liquid block detected at {0}.", blockPosAbove);
                    _waterInteractionHandler.HandleWaterInteraction(player, block);
                }
                else
                {
                    _serverApi.Logger.Debug("No liquid block detected at position {0}.", blockPosAbove);
                }
            }
            else
            {
                _serverApi.Logger.Debug("No highlighted block found.");
            }
        }

    }
}
