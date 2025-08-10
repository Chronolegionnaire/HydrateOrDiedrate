using HarmonyLib;
using HydrateOrDiedrate.Aquifer;
using HydrateOrDiedrate.Config;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.patches
{
    public static class BetterProspectingAquiferPatch
    {
        public static void Apply(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            var betterProspectingAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Equals("BetterProspecting", StringComparison.InvariantCultureIgnoreCase));

            if (betterProspectingAssembly == null)
            {
                return;
            }
            var itemBetterProspectingType = betterProspectingAssembly.GetType("BetterProspecting.ItemBetterProspecting");
            if (itemBetterProspectingType == null)
            {
                return;
            }
            var targetMethod = itemBetterProspectingType.GetMethod("OnBlockBrokenWith",
                BindingFlags.Public | BindingFlags.Instance);

            if (targetMethod == null)
            {
                return;
            }
            var harmony = new Harmony("com.chronolegionnaire.hydrateordiedrate.betterprospecting");
            var postfix = new HarmonyMethod(typeof(BetterProspectingAquiferPatch).GetMethod(nameof(OnBlockBrokenWithPostfix), BindingFlags.NonPublic | BindingFlags.Static));
            var patchProcessor = new PatchProcessor(harmony, targetMethod);
            patchProcessor.AddPostfix(postfix);
            patchProcessor.Patch();
        }

        private static void OnBlockBrokenWithPostfix(object __instance, IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, bool __result)
        {
            if (!__result)
            {
                return;
            }
            int toolMode = itemslot.Itemstack.Attributes.GetInt("toolMode", -1);
            if (toolMode != 2)
            {
                return;
            }
            if (world.Api.Side != EnumAppSide.Server)
            {
                return;
            }
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (byPlayer == null)
            {
                return;
            }
            IServerPlayer serverPlayer = byPlayer as IServerPlayer;
            if (serverPlayer == null)
            {
                return;
            }
            BlockPos pos = blockSel.Position;
            
            var aquiferData = AquiferManager.GetAquiferChunkData(world, pos, world.Logger)?.Data;
            if (aquiferData == null)
            {
                SendMessageToPlayer(world, serverPlayer, Lang.Get("hydrateordiedrate:aquifer-no-data"));
                return;
            }

            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;

            int currentRating = aquiferData.AquiferRating;
            string aquiferInfo = currentRating == 0
                ? Lang.Get("hydrateordiedrate:aquifer-none")
                : GetAquiferDescription(aquiferData.IsSalty, currentRating, world.BlockAccessor.MapSizeY, pos.Y);
            
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
                <= 10 => Lang.Get("hydrateordiedrate:aquifer-none-detected"),
                <= 15 => Lang.Get("hydrateordiedrate:aquifer-very-poor", aquiferType),
                <= 20 => Lang.Get("hydrateordiedrate:aquifer-poor", aquiferType),
                <= 40 => Lang.Get("hydrateordiedrate:aquifer-light", aquiferType),
                <= 60 => Lang.Get("hydrateordiedrate:aquifer-moderate", aquiferType),
                _     => Lang.Get("hydrateordiedrate:aquifer-heavy", aquiferType)
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
                horizontalPart = verticalHor + " " + Lang.Get("hydrateordiedrate:direction-and") + " " + horizontal;
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
            splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null);
        }
    }
}
