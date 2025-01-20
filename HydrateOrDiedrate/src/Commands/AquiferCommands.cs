using Vintagestory.API.Common;
using Vintagestory.API.Server;
using static HydrateOrDiedrate.HydrateOrDiedrateModSystem;

namespace HydrateOrDiedrate.Commands
{
    public static class AquiferCommands
    {
        public static void Register(ICoreServerAPI api)
        {
            api.ChatCommands.Create("clearaquiferdata")
                .WithDescription("Clears all saved aquifer data")
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(static args =>
                {
                    if(HydrateOrDiedrateGlobals.AquiferManager == null) return TextCommandResult.Error("Aquifer system is not initialized.");

                    HydrateOrDiedrateGlobals.AquiferManager.ClearAquiferData();
                    return TextCommandResult.Success("Aquifer data has been cleared successfully.");
                });
        }

    }
}
