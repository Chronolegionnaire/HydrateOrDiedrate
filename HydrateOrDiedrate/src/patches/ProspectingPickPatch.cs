using System.Threading.Tasks;
using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
public static class PrintProbeResultsPatch
{
    static void Postfix(ItemProspectingPick __instance, IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot, BlockPos pos)
    {
        if (world.Api.Side != EnumAppSide.Server || HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferSystem == null)
        {
            return;
        }

        string flagKey = "PrintProbeResultsFlag";
        if (splr.Entity?.WatchedAttributes.GetBool(flagKey, false) == true)
        {
            return;
        }

        splr.Entity?.WatchedAttributes.SetBool(flagKey, true);
        
        Task.Run(() =>
        {
            Vec2i chunkCoord = new Vec2i(pos.X / world.BlockAccessor.ChunkSize, pos.Z / world.BlockAccessor.ChunkSize);
            var aquiferData = HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferSystem.GetAquiferData(chunkCoord);

            if (aquiferData == null)
            {
                SendMessageToPlayer(world, splr, "No aquifer data available for this region.");
            }
            else
            {
                string aquiferInfo = aquiferData.AquiferRating == 0
                    ? "There is no aquifer here."
                    : GetAquiferDescription(aquiferData.IsSalty, aquiferData.AquiferRating);
                SendMessageToPlayer(world, splr, aquiferInfo);
            }

            // Reset the flag after a short delay
            world.RegisterCallback(_ =>
            {
                splr.Entity?.WatchedAttributes.SetBool(flagKey, false);
            }, 500);
        });
    }

    private static string GetAquiferDescription(bool isSalty, int rating)
    {
        if (rating == 0)
            return "No aquifer detected.";

        string aquiferType = isSalty ? "salt" : "fresh";

        if (rating <= 20)
            return $"Very poor {aquiferType} water aquifer detected.";
        else if (rating <= 40)
            return $"Poor {aquiferType} water aquifer detected.";
        else if (rating <= 60)
            return $"Light {aquiferType} water aquifer detected.";
        else if (rating <= 80)
            return $"Moderate {aquiferType} water aquifer detected.";
        else
            return $"Heavy {aquiferType} water aquifer detected.";
    }

    private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message)
    {
        world.Api.Event.EnqueueMainThreadTask(() =>
        {
            splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null);
        }, "SendAquiferMessage");
    }
}
