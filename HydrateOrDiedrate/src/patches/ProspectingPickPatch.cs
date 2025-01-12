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
                Vec2i chunkCoord = new Vec2i(pos.X / GlobalConstants.ChunkSize, pos.Z / GlobalConstants.ChunkSize);
                var aquiferData =
                    HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferManager.GetAquiferData(chunkCoord);

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
            double waterLineY = Math.Round(0.4296875 * worldHeight);
            double depthFactor;
            depthFactor = (waterLineY - posY) / (waterLineY - 1);

            int effectiveRating = (int)Math.Round(rating * depthFactor);
            string aquiferType = isSalty ? "salt" : "fresh";
            if (effectiveRating <= 0)
                return $"No aquifer detected. Actual {rating}.";
            else if (effectiveRating <= 10)
                return $"Very poor {aquiferType} water aquifer detected. Actual {rating}.";
            else if (effectiveRating <= 20)
                return $"Poor {aquiferType} water aquifer detected. Actual {rating}.";
            else if (effectiveRating <= 40)
                return $"Light {aquiferType} water aquifer detected. Actual {rating}.";
            else if (effectiveRating <= 60)
                return $"Moderate {aquiferType} water aquifer detected. Actual {rating}.";
            else
                return $"Heavy {aquiferType} water aquifer detected. Actual {rating}.";
        }

        private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message)
        {
            world.Api.Event.EnqueueMainThreadTask(
                () => { splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null); },
                "SendAquiferMessage");
        }
    }
}
