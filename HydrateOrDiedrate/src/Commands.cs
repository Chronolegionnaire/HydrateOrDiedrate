using System;
using HydrateOrDiedrate.Configuration;
using Vintagestory.API.Server;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate.Commands
{
    public static class ThirstCommands
    {
        public static void Register(ICoreServerAPI api, Config loadedConfig)
        {
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
        }

        private static TextCommandResult OnSetThirstCommand(ICoreServerAPI api, Config loadedConfig, TextCommandCallingArgs args)
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
            thirstBehavior.UpdateThirstAttributes();

            return TextCommandResult.Success($"Thirst set to {thirstValue} for player '{targetPlayer.PlayerName}'.");
        }

        private static TextCommandResult OnSetNutriDefCommand(ICoreServerAPI api, Config loadedConfig, TextCommandCallingArgs args)
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
