using System;
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
        static void Prefix(IWorldAccessor world, BlockPos pos, out BlockPos __state)
        {
            __state = pos.Copy();
        }
        static void Postfix(ref PropickReading __result, IWorldAccessor world, BlockPos pos, BlockPos __state)
        {
            if (world.Side != EnumAppSide.Server) return;
            if (HydrateOrDiedrateModSystem.AquiferManager == null) return;
            BlockPos originalPos = __state;
            int chunkX = originalPos.X / GlobalConstants.ChunkSize;
            int chunkY = originalPos.Y / GlobalConstants.ChunkSize;
            int chunkZ = originalPos.Z / GlobalConstants.ChunkSize;
            ChunkPos3D chunkCoord = new ChunkPos3D(chunkX, chunkY, chunkZ);
            Console.WriteLine("GenProbeResults=" + chunkCoord.X + "," + chunkCoord.Y + "," + chunkCoord.Z);
            
            var aquiferData = HydrateOrDiedrateModSystem.AquiferManager.GetAquiferData(chunkCoord);
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