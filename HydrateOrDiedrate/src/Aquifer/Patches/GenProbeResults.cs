using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(ItemProspectingPick), "GenProbeResults")]
    public static class GenProbeResultsPatch
    {
        static void Postfix(ref PropickReading __result, IWorldAccessor world, BlockPos pos)
        {
            if (world.Side != EnumAppSide.Server) return;
            if (HydrateOrDiedrateModSystem.AquiferManager == null) return;
            int cx = pos.X / GlobalConstants.ChunkSize;
            int cy = pos.Y / GlobalConstants.ChunkSize;
            int cz = pos.Z / GlobalConstants.ChunkSize;
            var chunkPos = new ChunkPos3D(cx, cy, cz);
            var aquiferData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(chunkPos);
            if (aquiferData == null) return;
            __result.OreReadings["$aquifer$"] = new OreReading()
            {
                DepositCode = aquiferData.IsSalty ? "salty" : "fresh",
                PartsPerThousand = aquiferData.AquiferRating,
                TotalFactor = 0.0000001
            };
        }
    }
}