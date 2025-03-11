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
                HydrateOrDiedrateModSystem.AquiferManager == null)
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
                var currentAquiferData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(chunkCoord);

                double worldHeight = world.BlockAccessor.MapSizeY;
                double posY = pos.Y;
                
                if (currentAquiferData == null)
                {
                    SendMessageToPlayer(world, splr, "No aquifer data available for this region.");
                }
                else
                {
                    int currentRating = currentAquiferData.AquiferRating;
                    string aquiferInfo = currentRating == 0
                        ? "There is no aquifer here."
                        : GetAquiferDescription(currentAquiferData.IsSalty, currentRating, worldHeight, posY);

                    int radius = HydrateOrDiedrateModSystem.LoadedConfig.ProspectingRadius;
                    int bestRating = currentRating;
                    ChunkPos3D bestChunk = chunkCoord;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            for (int dz = -radius; dz <= radius; dz++)
                            {
                                // Skip the current chunk.
                                if (dx == 0 && dy == 0 && dz == 0) continue;
                                
                                ChunkPos3D checkChunk = new ChunkPos3D(chunkX + dx, chunkY + dy, chunkZ + dz);
                                var checkAquiferData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(checkChunk);
                                if (checkAquiferData != null && checkAquiferData.AquiferRating > bestRating)
                                {
                                    bestRating = checkAquiferData.AquiferRating;
                                    bestChunk = checkChunk;
                                }
                            }
                        }
                    }
                    if (bestRating > currentRating)
                    {
                        int dxDir = bestChunk.X - chunkCoord.X;
                        int dyDir = bestChunk.Y - chunkCoord.Y;
                        int dzDir = bestChunk.Z - chunkCoord.Z;
                        string directionHint = GetDirectionHint(dxDir, dyDir, dzDir);
                        aquiferInfo += $" The aquifer seems to get stronger to the {directionHint}.";
                    }
                    
                    SendMessageToPlayer(world, splr, aquiferInfo);
                }

                world.RegisterCallback(_ => { splr.Entity?.WatchedAttributes.SetBool(flagKey, false); }, 500);
            });
        }

        private static string GetAquiferDescription(bool isSalty, int rating, double worldHeight, double posY)
        {
            string aquiferType = isSalty ? "salt" : "fresh";
            return rating switch
            {
                <= 0 => "No aquifer detected.",
                <= 10 => $"Very poor {aquiferType} water aquifer detected.",
                <= 20 => $"Poor {aquiferType} water aquifer detected.",
                <= 40 => $"Light {aquiferType} water aquifer detected.",
                <= 60 => $"Moderate {aquiferType} water aquifer detected.",
                _ => $"Heavy {aquiferType} water aquifer detected."
            };
        }
        private static string GetDirectionHint(int dx, int dy, int dz)
        {
            string horizontal = "";
            string verticalHor = "";
            
            if (dz < 0) verticalHor = "north";
            else if (dz > 0) verticalHor = "south";

            if (dx > 0) horizontal = "east";
            else if (dx < 0) horizontal = "west";

            string horizontalPart = "";
            if (!string.IsNullOrEmpty(verticalHor) && !string.IsNullOrEmpty(horizontal))
                horizontalPart = verticalHor + "-" + horizontal;
            else
                horizontalPart = !string.IsNullOrEmpty(verticalHor) ? verticalHor : horizontal;

            string verticalDepth = "";
            if (dy > 0) verticalDepth = "above";
            else if (dy < 0) verticalDepth = "below";
            if (!string.IsNullOrEmpty(horizontalPart) && !string.IsNullOrEmpty(verticalDepth))
                return horizontalPart + " and " + verticalDepth;
            else if (!string.IsNullOrEmpty(horizontalPart))
                return horizontalPart;
            else if (!string.IsNullOrEmpty(verticalDepth))
                return verticalDepth;
            else
                return "here";
        }

        private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message)
        {
            world.Api.Event.EnqueueMainThreadTask(
                () => { splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null); },
                "SendAquiferMessage");
        }
    }
}
