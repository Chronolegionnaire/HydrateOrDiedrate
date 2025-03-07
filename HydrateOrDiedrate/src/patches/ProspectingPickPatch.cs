using System;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
    public static class PrintProbeResultsPatch
    {
        static void Postfix(ItemProspectingPick __instance, IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot,
            BlockPos pos)
        {
            if (world.Api.Side != EnumAppSide.Server ||
                HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferManager == null)
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
                int chunkX = pos.X / GlobalConstants.ChunkSize;
                int chunkY = pos.Y / GlobalConstants.ChunkSize;
                int chunkZ = pos.Z / GlobalConstants.ChunkSize;
                ChunkPos3D chunkCoord = new ChunkPos3D(chunkX, chunkY, chunkZ);
                var aquiferData = HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferManager.GetAquiferData(chunkCoord);

                if (aquiferData == null)
                {
                    SendMessageToPlayer(world, splr, "No aquifer data available for this region.");
                }
                else
                {
                    double worldHeight = world.BlockAccessor.MapSizeY;
                    double posY = pos.Y;
                    string aquiferInfo = aquiferData.AquiferRating == 0
                        ? "There is no aquifer here."
                        : GetAquiferDescription(aquiferData.IsSalty, aquiferData.AquiferRating, worldHeight, posY);
                    SendMessageToPlayer(world, splr, aquiferInfo);
                }

                world.RegisterCallback(_ => { splr.Entity?.WatchedAttributes.SetBool(flagKey, false); }, 500);
            });
        }

        private static string GetAquiferDescription(bool isSalty, int rating, double worldHeight, double posY)
        {
            int effectiveRating = rating;

            if (HydrateOrDiedrateModSystem.LoadedConfig.AquiferDepthScaling)
            {
                effectiveRating = rating;
            }

            string aquiferType = isSalty ? "salt" : "fresh";

            return effectiveRating switch
            {
                <= 0 => "No aquifer detected.",
                <= 10 => $"Very poor {aquiferType} water aquifer detected.",
                <= 20 => $"Poor {aquiferType} water aquifer detected.",
                <= 40 => $"Light {aquiferType} water aquifer detected.",
                <= 60 => $"Moderate {aquiferType} water aquifer detected.",
                _ => $"Heavy {aquiferType} water aquifer detected."
            };
        }

        private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message)
        {
            world.Api.Event.EnqueueMainThreadTask(
                () => { splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null); },
                "SendAquiferMessage");
        }
    }
}
