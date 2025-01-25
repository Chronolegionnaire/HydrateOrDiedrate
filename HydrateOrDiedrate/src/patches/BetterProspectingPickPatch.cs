using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
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
            if (api.Side != EnumAppSide.Server)
            {
                return;
            }
            if (HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferManager == null)
            {
                return;
            }
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
            var harmony = new Harmony("com.yourname.hydrateordiedrate.betterprospecting");
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
            Vec2i chunkCoord = new Vec2i(pos.X / GlobalConstants.ChunkSize, pos.Z / GlobalConstants.ChunkSize);
            var aquiferData = HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferManager.GetAquiferData(chunkCoord);

            if (aquiferData == null)
            {
                SendMessageToPlayer(world, serverPlayer, "No aquifer data available for this region.");
            }
            else
            {
                double worldHeight = world.BlockAccessor.MapSizeY;
                double posY = pos.Y;
                string aquiferInfo = aquiferData.AquiferRating == 0
                    ? "There is no aquifer here."
                    : GetAquiferDescription(aquiferData.IsSalty, aquiferData.AquiferRating, worldHeight, posY);
                SendMessageToPlayer(world, serverPlayer, aquiferInfo);
            }
        }

        private static string GetAquiferDescription(bool isSalty, int rating, double worldHeight, double posY)
        {
            double waterLineY = Math.Round(0.4296875 * worldHeight);
            double depthFactor = (waterLineY - posY) / (waterLineY - 1);

            int effectiveRating = (int)Math.Round(rating * depthFactor);
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