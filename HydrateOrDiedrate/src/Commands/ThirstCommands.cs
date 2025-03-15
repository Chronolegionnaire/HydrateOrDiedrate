using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Commands
{
    public static class ThirstCommands
    {
        public static void Register(ICoreServerAPI api, HydrateOrDiedrate.Config.Config loadedConfig)
        {
            if (!loadedConfig.EnableThirstMechanics) return;

            api.ChatCommands
                .Create("setthirst")
                .WithDescription(Lang.Get("hydrateordiedrate:cmd.setthirst.desc"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Float("thirstValue"))
                .HandleWith((args) => OnSetThirstCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("setnutriDef")
                .WithDescription(Lang.Get("hydrateordiedrate:cmd.setnutridef.desc"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Float("nutriDeficitValue"))
                .HandleWith((args) => OnSetNutriDefCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("sethydlossdelay")
                .WithDescription(Lang.Get("hydrateordiedrate:cmd.sethydlossdelay.desc"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"), api.ChatCommands.Parsers.Int("hydLossDelayValue"))
                .HandleWith((args) => OnSetHydLossDelayCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("getthirst")
                .WithDescription(Lang.Get("hydrateordiedrate:cmd.getthirst.desc"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
                .HandleWith((args) => OnGetThirstCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("getnutriDef")
                .WithDescription(Lang.Get("hydrateordiedrate:cmd.getnutridef.desc"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.OptionalWord("playerName"))
                .HandleWith((args) => OnGetNutriDefCommand(api, loadedConfig, args));

            api.ChatCommands
                .Create("gethydlossdelay")
                .WithDescription(Lang.Get("hydrateordiedrate:cmd.gethydlossdelay.desc"))
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
                    return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.playernotfound", playerName));
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
                return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.thirstnotfound"));

            thirstBehavior.CurrentThirst = thirstValue;

            return TextCommandResult.Success(Lang.Get("hydrateordiedrate:cmd.thirstset", thirstValue, targetPlayer.PlayerName));
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
                    return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.playernotfound", playerName));
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
                return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.thirstnotfound"));

            thirstBehavior.HungerReductionAmount = nutriDeficitValue;

            return TextCommandResult.Success(Lang.Get("hydrateordiedrate:cmd.nutridefset", nutriDeficitValue, targetPlayer.PlayerName));
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
                    return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.playernotfound", playerName));
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
                return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.thirstnotfound"));

            thirstBehavior.HydrationLossDelay = hydLossDelayValue;

            return TextCommandResult.Success(Lang.Get("hydrateordiedrate:cmd.hydlossdelayset", hydLossDelayValue, targetPlayer.PlayerName));
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
                    return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.playernotfound", playerName));
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
                return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.thirstnotfound"));

            float thirstValue = thirstBehavior.CurrentThirst;

            return TextCommandResult.Success(Lang.Get("hydrateordiedrate:cmd.getthirst", thirstValue, targetPlayer.PlayerName));
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
                    return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.playernotfound", playerName));
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
                return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.thirstnotfound"));

            float nutriDeficitValue = thirstBehavior.HungerReductionAmount;

            return TextCommandResult.Success(Lang.Get("hydrateordiedrate:cmd.getnutridef", nutriDeficitValue, targetPlayer.PlayerName));
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
                    return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.playernotfound", playerName));
                }
            }

            var thirstBehavior = targetPlayer.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
                return TextCommandResult.Error(Lang.Get("hydrateordiedrate:cmd.thirstnotfound"));

            int hydLossDelayValue = thirstBehavior.HydrationLossDelay;

            return TextCommandResult.Success(Lang.Get("hydrateordiedrate:cmd.gethydlossdelay", hydLossDelayValue, targetPlayer.PlayerName));
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
