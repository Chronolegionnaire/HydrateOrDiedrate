using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static HydrateOrDiedrate.HydrateOrDiedrateModSystem;

namespace HydrateOrDiedrate.Commands
{
    public static class AquiferCommands
    {
        public static void Register(ICoreServerAPI api)
        {
            api.ChatCommands.Create("clearaquiferdata")
                .WithDescription(Lang.Get("hydrateordiedrate:aquifer-command-description"))
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(static args =>
                {
                    if (HydrateOrDiedrateModSystem.AquiferManager == null)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:aquifer-command-not-initialized"));
                    }
                    HydrateOrDiedrateModSystem.AquiferManager.ClearAquiferData();
                    return TextCommandResult.Success(Lang.Get("hydrateordiedrate:aquifer-command-cleared"));
                });
            api.ChatCommands.Create("setaquifer")
                .WithDescription(Lang.Get("hydrateordiedrate:setaquifer-description"))
                .RequiresPrivilege("controlserver")
                .WithArgs(api.ChatCommands.Parsers.Int("rating"))
                .HandleWith((args) =>
                {
                    int rating = (int)args[0];
                    rating = Math.Clamp(rating, 0, 100);

                    if (HydrateOrDiedrateModSystem.AquiferManager == null)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:aquifer-command-not-initialized"));
                    }
                    IPlayer player = args.Caller.Player;
                    if (player == null)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:command-only-for-players"));
                    }

                    BlockPos pos = player.Entity.ServerPos.AsBlockPos;
                    int chunkX = pos.X / GlobalConstants.ChunkSize;
                    int chunkY = pos.Y / GlobalConstants.ChunkSize;
                    int chunkZ = pos.Z / GlobalConstants.ChunkSize;
                    var chunkPos = new ChunkPos3D(chunkX, chunkY, chunkZ);

                    bool success = HydrateOrDiedrateModSystem.AquiferManager.SetAquiferRating(chunkPos, rating);
                    if (!success)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:setaquifer-failed"));
                    }
                    return TextCommandResult.Success(Lang.Get("hydrateordiedrate:setaquifer-success", rating));
                });
            api.ChatCommands.Create("getaquifer")
                .WithDescription(Lang.Get("hydrateordiedrate:getaquifer-description"))
                .HandleWith(static args =>
                {
                    if (args.Caller is not IPlayer player)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:command-only-for-players"));
                    }

                    if (HydrateOrDiedrateModSystem.AquiferManager == null)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:aquifer-command-not-initialized"));
                    }

                    BlockPos pos = player.Entity.ServerPos.AsBlockPos;
                    int chunkX = pos.X / GlobalConstants.ChunkSize;
                    int chunkY = pos.Y / GlobalConstants.ChunkSize;
                    int chunkZ = pos.Z / GlobalConstants.ChunkSize;
                    var chunkPos = new ChunkPos3D(chunkX, chunkY, chunkZ);

                    var aqData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(chunkPos);
                    if (aqData == null)
                    {
                        return TextCommandResult.Error(Lang.Get("hydrateordiedrate:getaquifer-not-found"));
                    }

                    return TextCommandResult.Success(Lang.Get("hydrateordiedrate:getaquifer-success", aqData.AquiferRating, aqData.IsSalty));
                });
        }
    }
}
