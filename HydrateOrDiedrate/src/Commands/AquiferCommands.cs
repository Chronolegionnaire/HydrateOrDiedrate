using HydrateOrDiedrate.Wells.Aquifer;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Commands
{
    public static class AquiferCommands
    {
        public static void Register(ICoreServerAPI api)
        {
            api.ChatCommands.Create("setaquifer")
                .WithDescription(Lang.Get("hydrateordiedrate:setaquifer-description"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.Int("rating"))
                .HandleWith(static (args) =>
                {
                    //TODO remove "hydrateordiedrate:aquifer-command-not-initialized"
                    int rating = (int)args[0];
                    rating = Math.Clamp(rating, 0, 100);

                    IPlayer player = args.Caller.Player;
                    if (player == null) return TextCommandResult.Error(Lang.Get("hydrateordiedrate:command-only-for-players"));

                    if (!AquiferManager.SetAquiferRating(HydrateOrDiedrateModSystem._serverApi.World, player.Entity.ServerPos.AsBlockPos, rating))
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:setaquifer-failed"));
                    }
                    return TextCommandResult.Success(Lang.Get("hydrateordiedrate:setaquifer-success", rating)); //TODO this does not represent the actual rating set, but the one requested
                });

            api.ChatCommands.Create("getaquifer")
                .WithDescription(Lang.Get("hydrateordiedrate:getaquifer-description"))
                .RequiresPrivilege("controlserver")
                .HandleWith(static args =>
                {
                    IPlayer player = args.Caller.Player;
                    if (player == null) return TextCommandResult.Error(Lang.Get("hydrateordiedrate:command-only-for-players"));

                    var world = HydrateOrDiedrateModSystem._serverApi.World;
                    var aqData = AquiferManager.GetAquiferChunkData(HydrateOrDiedrateModSystem._serverApi.World, player.Entity.ServerPos.AsBlockPos, world.Logger);
                    if (aqData == null)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:getaquifer-not-found"));
                    }

                    return TextCommandResult.Success(Lang.Get("hydrateordiedrate:getaquifer-success", aqData.Data.AquiferRating, aqData.Data.IsSalty));
                });
        }
    }
}
