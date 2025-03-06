using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Commands
{
    public static class ThirstCommands
    {
        public static void Register(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig)
        {
            if(!loadedConfig.EnableThirstMechanics) return;

            api.ChatCommands
                .Create("setthirst")
                .WithDescription("Sets the player's thirst level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Float("thirstValue"))
                .HandleWith((args) => OnSetThirstCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("setnutriDef")
                .WithDescription("Sets the player's nutrition deficit (hunger reduction) level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Float("nutriDeficitValue"))
                .HandleWith((args) => OnSetNutriDefCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("sethydlossdelay")
                .WithDescription("Sets the player's hydration loss delay.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Int("hydLossDelayValue"))
                .HandleWith((args) => OnSetHydLossDelayCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("getthirst")
                .WithDescription("Gets the player's thirst level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
                .HandleWith((args) => OnGetThirstCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("getnutriDef")
                .WithDescription("Gets the player's nutrition deficit (hunger reduction) level.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
                .HandleWith((args) => OnGetNutriDefCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("gethydlossdelay")
                .WithDescription("Gets the player's hydration loss delay.")
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
                .HandleWith((args) => OnGetHydLossDelayCommand(api, loadedConfig, args));
        }

        private static TextCommandResult OnSetThirstCommand(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;
            float thirstValue = (float)args[1];

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            thirstBehavior.CurrentThirst = thirstValue;

            return TextCommandResult.Success($"Thirst set to {thirstValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static TextCommandResult OnSetNutriDefCommand(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;
            float nutriDeficitValue = (float)args[1];

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            thirstBehavior.HungerReductionAmount = nutriDeficitValue;

            return TextCommandResult.Success($"Nutrition deficit set to {nutriDeficitValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static TextCommandResult OnSetHydLossDelayCommand(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;
            int hydLossDelayValue = (int)args[1];

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            thirstBehavior.HydrationLossDelay = hydLossDelayValue;

            return TextCommandResult.Success($"Hydration loss delay set to {hydLossDelayValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static TextCommandResult OnGetThirstCommand(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            float thirstValue = thirstBehavior.CurrentThirst;

            return TextCommandResult.Success($"Value for thirst is {thirstValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static TextCommandResult OnGetNutriDefCommand(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            float nutriDeficitValue = thirstBehavior.HungerReductionAmount;

            return TextCommandResult.Success($"Nutrition deficit is {nutriDeficitValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static TextCommandResult OnGetHydLossDelayCommand(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig, TextCommandCallingArgs args)
        {
            string playerName = args[0] as string;

            IServerPlayer targetPlayer;

            if (string.IsNullOrEmpty(playerName))
            {
                targetPlayer = args.Caller.Player as IServerPlayer;
            }
            else
            {
                targetPlayer = GetPlayerByName(api, playerName);
                if (targetPlayer == null)
                {
                    return TextCommandResult.Error($"Player '{playerName}' not found.");
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null) return TextCommandResult.Error("Thirst behavior not found.");

            int hydLossDelayValue = thirstBehavior.HydrationLossDelay;

            return TextCommandResult.Success($"Hydration loss delay is {hydLossDelayValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static IServerPlayer GetPlayerByName(ICoreServerAPI api, string playerName)
        {
            foreach (IServerPlayer player in api.World.AllOnlinePlayers)
            {
                if (player.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }
    }
}
