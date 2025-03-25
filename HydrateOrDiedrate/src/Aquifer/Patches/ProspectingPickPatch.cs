using System;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
    public static class PrintProbeResultsPatch
    {
        static void Postfix(ItemProspectingPick __instance, IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot, BlockPos pos)
        {
            if (world.Api.Side != EnumAppSide.Server || HydrateOrDiedrateModSystem.AquiferManager == null)
            {
                return;
            }

            if (HydrateOrDiedrateModSystem.LoadedConfig != null && HydrateOrDiedrateModSystem.LoadedConfig.AquiferDataOnProspectingNodeMode)
            {
                return;
            }

            string flagKey = "PrintProbeResultsFlag";
            if (splr.Entity?.WatchedAttributes.GetBool(flagKey, false) == true)
            {
                return;
            }

            splr.Entity?.WatchedAttributes.SetBool(flagKey, true);
            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            ChunkPos3D chunkCoord = new ChunkPos3D(chunkX, chunkY, chunkZ);
            var currentAquiferData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(chunkCoord);

            double worldHeight = world.BlockAccessor.MapSizeY;
            double posY = pos.Y;

            if (currentAquiferData == null)
            {
                SendMessageToPlayer(world, splr, Lang.Get("hydrateordiedrate:aquifer-no-data"));
            }
            else
            {
                int currentRating = currentAquiferData.AquiferRating;
                string aquiferInfo = currentRating == 0
                    ? Lang.Get("hydrateordiedrate:aquifer-none")
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
                    aquiferInfo += Lang.Get("hydrateordiedrate:aquifer-direction", directionHint);
                }
                
                SendMessageToPlayer(world, splr, aquiferInfo);
            }
            world.RegisterCallback(_ => { splr.Entity?.WatchedAttributes.SetBool(flagKey, false); }, 500);
        }

        private static string GetAquiferDescription(bool isSalty, int rating, double worldHeight, double posY)
        {
            string aquiferType = isSalty ? Lang.Get("hydrateordiedrate:aquifer-salt") : Lang.Get("hydrateordiedrate:aquifer-fresh");
            return rating switch
            {
                <= 0 => Lang.Get("hydrateordiedrate:aquifer-none-detected"),
                <= 10 => Lang.Get("hydrateordiedrate:aquifer-very-poor", aquiferType),
                <= 20 => Lang.Get("hydrateordiedrate:aquifer-poor", aquiferType),
                <= 40 => Lang.Get("hydrateordiedrate:aquifer-light", aquiferType),
                <= 60 => Lang.Get("hydrateordiedrate:aquifer-moderate", aquiferType),
                _ => Lang.Get("hydrateordiedrate:aquifer-heavy", aquiferType)
            };
        }

        private static string GetDirectionHint(int dx, int dy, int dz)
        {
            string horizontal = "";
            string verticalHor = "";
            
            if (dz < 0) verticalHor = Lang.Get("hydrateordiedrate:direction-north");
            else if (dz > 0) verticalHor = Lang.Get("hydrateordiedrate:direction-south");

            if (dx > 0) horizontal = Lang.Get("hydrateordiedrate:direction-east");
            else if (dx < 0) horizontal = Lang.Get("hydrateordiedrate:direction-west");

            string horizontalPart = "";
            if (!string.IsNullOrEmpty(verticalHor) && !string.IsNullOrEmpty(horizontal))
                horizontalPart = verticalHor + "-" + horizontal;
            else
                horizontalPart = !string.IsNullOrEmpty(verticalHor) ? verticalHor : horizontal;

            string verticalDepth = "";
            if (dy > 0) verticalDepth = Lang.Get("hydrateordiedrate:direction-above");
            else if (dy < 0) verticalDepth = Lang.Get("hydrateordiedrate:direction-below");
            if (!string.IsNullOrEmpty(horizontalPart) && !string.IsNullOrEmpty(verticalDepth))
                return horizontalPart + " " + Lang.Get("hydrateordiedrate:direction-and") + " " + verticalDepth;
            else if (!string.IsNullOrEmpty(horizontalPart))
                return horizontalPart;
            else if (!string.IsNullOrEmpty(verticalDepth))
                return verticalDepth;
            else
                return Lang.Get("hydrateordiedrate:direction-here");
        }

        private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message)
        {
            world.Api.Event.EnqueueMainThreadTask(
                () => { splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null); },
                "SendAquiferMessage");
        }
    }

    [HarmonyPatch(typeof(ItemProspectingPick), "ProbeBlockNodeMode")]
    public static class ProbeBlockNodeMode_AquiferOnlyPatch
    {
        static void Postfix(ItemProspectingPick __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, int radius)
        {
            if (world.Api.Side != EnumAppSide.Server ||
                HydrateOrDiedrateModSystem.LoadedConfig == null ||
                !HydrateOrDiedrateModSystem.LoadedConfig.AquiferDataOnProspectingNodeMode)
            {
                return;
            }

            if (!(byEntity is EntityPlayer entityPlayer))
                return;
            IServerPlayer splr = world.PlayerByUid(entityPlayer.PlayerUID) as IServerPlayer;
            if (splr == null)
                return;
            string flagKey = "AquiferDataOnlyFlag";
            if (splr.Entity?.WatchedAttributes.GetBool(flagKey, false) == true)
            {
                return;
            }
            splr.Entity?.WatchedAttributes.SetBool(flagKey, true);
            BlockPos pos = blockSel.Position;
            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            ChunkPos3D chunkCoord = new ChunkPos3D(chunkX, chunkY, chunkZ);
            var currentAquiferData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(chunkCoord);

            double worldHeight = world.BlockAccessor.MapSizeY;
            double posY = pos.Y;

            if (currentAquiferData == null)
            {
                SendMessageToPlayer(world, splr, Lang.Get("hydrateordiedrate:aquifer-no-data"));
            }
            else
            {
                int currentRating = currentAquiferData.AquiferRating;
                string aquiferInfo = currentRating == 0
                    ? Lang.Get("hydrateordiedrate:aquifer-none")
                    : GetAquiferDescription(currentAquiferData.IsSalty, currentRating, worldHeight, posY);

                int configRadius = HydrateOrDiedrateModSystem.LoadedConfig.ProspectingRadius;
                int bestRating = currentRating;
                ChunkPos3D bestChunk = chunkCoord;
                for (int dx = -configRadius; dx <= configRadius; dx++)
                {
                    for (int dy = -configRadius; dy <= configRadius; dy++)
                    {
                        for (int dz = -configRadius; dz <= configRadius; dz++)
                        {
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
                    aquiferInfo += Lang.Get("hydrateordiedrate:aquifer-direction", directionHint);
                }
                
                SendMessageToPlayer(world, splr, aquiferInfo);
            }
            world.RegisterCallback(_ => { splr.Entity?.WatchedAttributes.SetBool(flagKey, false); }, 500);
        }

        private static string GetAquiferDescription(bool isSalty, int rating, double worldHeight, double posY)
        {
            string aquiferType = isSalty ? Lang.Get("hydrateordiedrate:aquifer-salt") : Lang.Get("hydrateordiedrate:aquifer-fresh");
            return rating switch
            {
                <= 0 => Lang.Get("hydrateordiedrate:aquifer-none-detected"),
                <= 10 => Lang.Get("hydrateordiedrate:aquifer-very-poor", aquiferType),
                <= 20 => Lang.Get("hydrateordiedrate:aquifer-poor", aquiferType),
                <= 40 => Lang.Get("hydrateordiedrate:aquifer-light", aquiferType),
                <= 60 => Lang.Get("hydrateordiedrate:aquifer-moderate", aquiferType),
                _ => Lang.Get("hydrateordiedrate:aquifer-heavy", aquiferType)
            };
        }

        private static string GetDirectionHint(int dx, int dy, int dz)
        {
            string horizontal = "";
            string verticalHor = "";
            
            if (dz < 0) verticalHor = Lang.Get("hydrateordiedrate:direction-north");
            else if (dz > 0) verticalHor = Lang.Get("hydrateordiedrate:direction-south");

            if (dx > 0) horizontal = Lang.Get("hydrateordiedrate:direction-east");
            else if (dx < 0) horizontal = Lang.Get("hydrateordiedrate:direction-west");

            string horizontalPart = (!string.IsNullOrEmpty(verticalHor) && !string.IsNullOrEmpty(horizontal))
                ? verticalHor + "-" + horizontal
                : (!string.IsNullOrEmpty(verticalHor) ? verticalHor : horizontal);

            string verticalDepth = "";
            if (dy > 0) verticalDepth = Lang.Get("hydrateordiedrate:direction-above");
            else if (dy < 0) verticalDepth = Lang.Get("hydrateordiedrate:direction-below");
            if (!string.IsNullOrEmpty(horizontalPart) && !string.IsNullOrEmpty(verticalDepth))
                return horizontalPart + " " + Lang.Get("hydrateordiedrate:direction-and") + " " + verticalDepth;
            else if (!string.IsNullOrEmpty(horizontalPart))
                return horizontalPart;
            else if (!string.IsNullOrEmpty(verticalDepth))
                return verticalDepth;
            else
                return Lang.Get("hydrateordiedrate:direction-here");
        }

        private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message)
        {
            world.Api.Event.EnqueueMainThreadTask(
                () => { splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null); },
                "SendAquiferMessage");
        }
    }
}
