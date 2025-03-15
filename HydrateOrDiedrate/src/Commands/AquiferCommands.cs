using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
        }
    }
}