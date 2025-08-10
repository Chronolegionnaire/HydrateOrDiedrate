using HarmonyLib;
using HydrateOrDiedrate.Aquifer;
using HydrateOrDiedrate.Config;
using System;
using System.Threading.Tasks;
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
            if (world.Api.Side != EnumAppSide.Server || ModConfig.Instance.GroundWater.AquiferDataOnProspectingNodeMode) return;

            const string flagKey = "PrintProbeResultsFlag";
            if (splr.Entity?.WatchedAttributes.GetBool(flagKey, false) == true) return;

            splr.Entity?.WatchedAttributes.SetBool(flagKey, true);
            var currentAquiferData = AquiferManager.GetAquiferChunkData(world, pos, world.Logger);

            double worldHeight = world.BlockAccessor.MapSizeY;
            double posY = pos.Y;

            world.RegisterCallback(_ => { splr.Entity?.WatchedAttributes.SetBool(flagKey, false); }, 500);
            
            if (currentAquiferData == null)
            {
                SendMessageToPlayer(world, splr, Lang.Get("hydrateordiedrate:aquifer-no-data"));
                return;
            }

            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            int currentRating = currentAquiferData.Data.AquiferRating;

            string aquiferInfo = currentRating == 0
                ? Lang.Get("hydrateordiedrate:aquifer-none")
                : GetAquiferDescription(currentAquiferData.Data.IsSalty, currentRating, worldHeight, posY);


            int radius = ModConfig.Instance.GroundWater.ProspectingRadius;
            int bestRating = currentRating;
            FastVec3i bestChunk = new(chunkX, chunkY, chunkZ);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        
                        FastVec3i checkChunk = new(chunkX + dx, chunkY + dy, chunkZ + dz);
                        var checkAquiferData = AquiferManager.GetAquiferChunkData(world, checkChunk, world.Logger)?.Data;
                        if (checkAquiferData is not null && checkAquiferData.AquiferRating > bestRating)
                        {
                            bestRating = checkAquiferData.AquiferRating;
                            bestChunk = checkChunk;
                        }
                    }
                }
            }

            if (bestRating > currentRating)
            {
                int dxDir = bestChunk.X - chunkX;
                int dyDir = bestChunk.Y - chunkY;
                int dzDir = bestChunk.Z - chunkZ;
                string directionHint = GetDirectionHint(dxDir, dyDir, dzDir);
                aquiferInfo += Lang.Get("hydrateordiedrate:aquifer-direction", directionHint);
            }
            
            SendMessageToPlayer(world, splr, aquiferInfo);
        }

        private static string GetAquiferDescription(bool isSalty, int rating, double worldHeight, double posY)
        {
            string aquiferType = isSalty ? Lang.Get("hydrateordiedrate:aquifer-salt") : Lang.Get("hydrateordiedrate:aquifer-fresh");
            return rating switch
            {
                <= 10 => Lang.Get("hydrateordiedrate:aquifer-none-detected"),
                <= 15 => Lang.Get("hydrateordiedrate:aquifer-very-poor", aquiferType),
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
            if (world.Api.Side != EnumAppSide.Server || !ModConfig.Instance.GroundWater.AquiferDataOnProspectingNodeMode) return;

            if (byEntity is not EntityPlayer entityPlayer) return;
            if (world.PlayerByUid(entityPlayer.PlayerUID) is not IServerPlayer serverPlayer) return;

            const string flagKey = "AquiferDataOnlyFlag";
            if (serverPlayer.Entity?.WatchedAttributes.GetBool(flagKey, false) == true) return;

            serverPlayer.Entity?.WatchedAttributes.SetBool(flagKey, true);
            BlockPos pos = blockSel.Position;

            var currentAquiferData = AquiferManager.GetAquiferChunkData(world, pos, world.Logger)?.Data;

            double worldHeight = world.BlockAccessor.MapSizeY;
            double posY = pos.Y;

            world.RegisterCallback(_ => { serverPlayer.Entity?.WatchedAttributes.SetBool(flagKey, false); }, 500);

            if (currentAquiferData == null)
            {
                SendMessageToPlayer(world, serverPlayer, Lang.Get("hydrateordiedrate:aquifer-no-data"));
                return;
            }
            
            int chunkX = pos.X / GlobalConstants.ChunkSize; //TODO extract this duplicate code to a utility method
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;

            int currentRating = currentAquiferData.AquiferRating;
            string aquiferInfo = currentRating == 0
                ? Lang.Get("hydrateordiedrate:aquifer-none")
                : GetAquiferDescription(currentAquiferData.IsSalty, currentRating, worldHeight, posY);

            int configRadius = ModConfig.Instance.GroundWater.ProspectingRadius;
            int bestRating = currentRating;
            FastVec3i bestChunk = new(chunkX, chunkY, chunkZ);
            for (int dx = -configRadius; dx <= configRadius; dx++)
            {
                for (int dy = -configRadius; dy <= configRadius; dy++)
                {
                    for (int dz = -configRadius; dz <= configRadius; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        
                        FastVec3i checkChunk = new(chunkX + dx, chunkY + dy, chunkZ + dz);
                        var checkAquiferData = AquiferManager.GetAquiferChunkData(world, checkChunk, world.Logger)?.Data;
                        if (checkAquiferData is not null && checkAquiferData.AquiferRating > bestRating)
                        {
                            bestRating = checkAquiferData.AquiferRating;
                            bestChunk = checkChunk;
                        }
                    }
                }
            }

            if (bestRating > currentRating)
            {
                int dxDir = bestChunk.X - chunkX;
                int dyDir = bestChunk.Y - chunkY;
                int dzDir = bestChunk.Z - chunkZ;
                string directionHint = GetDirectionHint(dxDir, dyDir, dzDir);
                aquiferInfo += Lang.Get("hydrateordiedrate:aquifer-direction", directionHint);
            }
            
            SendMessageToPlayer(world, serverPlayer, aquiferInfo);
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

        private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message) => world.Api.Event.EnqueueMainThreadTask(
            () => splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null),
            "SendAquiferMessage"
        );
    }
}
