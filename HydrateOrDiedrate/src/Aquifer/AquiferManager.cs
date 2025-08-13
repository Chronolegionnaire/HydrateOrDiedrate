using HydrateOrDiedrate.Aquifer.ModData;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Aquifer;

public static partial class AquiferManager
{
    private static readonly FastVec3i[] NeighborChunkOffsets = GenerateNeighborChunkOffsets();
    
    private static FastVec3i[] GenerateNeighborChunkOffsets()
    {
        var offsets = new FastVec3i[26];
        int index = 0;
    
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;
                    offsets[index++] = new FastVec3i(dx, dy, dz);
                }
            }
        }

        return offsets;
    }

    private static readonly double[] NeighborChunkOffsetWeights = GenerateNeighborChunkOffsetWeights();
    private static double[] GenerateNeighborChunkOffsetWeights()
    {
        var offsets = new double[26];
        int index = 0;
    
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    offsets[index++] = dist <= 0.0001 ? 1.0 : 1.0 / dist;
                }
            }
        }

        return offsets;
    }

    private static EWaterKind[] WaterKindById;

    internal static void Initialize(ICoreAPI api)
    {
        if(api is ICoreServerAPI serverAPi)
        {
            serverAPi.Event.ChunkColumnLoaded += HandleChunkColumnLoaded;
        }

        if(WaterKindById is not null) return;
        var blocksToScan = api.World.Blocks.Where(b => b.Code?.Domain == "game");
        var maxId = blocksToScan.Max(b => b.BlockId);
        var waterKindById = new EWaterKind[maxId + 1];

        foreach (var b in blocksToScan)
        {
            var kind = GetWaterKind(b);
            if (kind != EWaterKind.None) waterKindById[b.BlockId] = kind;
        }
        WaterKindById = waterKindById;
    }

    internal static void Unload()
    {
        WaterKindById = null;
    }

    private static bool TrySmoothAquiferData(FastVec3i chunkPos, AquiferData data)
    {
        double tot  = data.AquiferRating;
        double totW = 1.0;
        var world = HydrateOrDiedrateModSystem._serverApi.World;
        
        //TODO this is not consistent as chunk load order is not guaranteed so we might end up smoothing chunks in different states
        for (int i = 0; i < NeighborChunkOffsets.Length; i++)
        {
            FastVec3i chunkOffset = NeighborChunkOffsets[i];
            
            var neighborChunk = world.BlockAccessor.GetChunk(chunkPos.X + chunkOffset.X, chunkPos.Y + chunkOffset.Y, chunkPos.Z + chunkOffset.Z);
            if(neighborChunk is null) return false; // neighbor chunk not loaded

            var neighborChunkData = GetAquiferChunkData(neighborChunk, world.Logger);
            if(neighborChunkData is null || neighborChunkData.Version < CurrentAquiferDataVersion) return false; // neighbor chunk data not generated or outdated

            var chunkWeight = NeighborChunkOffsetWeights[i];

            tot  += neighborChunkData.Data.AquiferRating * chunkWeight;
            totW += chunkWeight;
        }


        data.AquiferRatingRaw = (int)Math.Clamp(tot / totW, 0, 100);
        return true;
    }

    private static void LateSmoothAquiferData(FastVec3i chunkPos, IWorldChunk chunk, AquiferChunkData chunkData)
    {
        if(!TrySmoothAquiferData(chunkPos, chunkData.Data)) return;

        chunk.SetModdata(AquiferModDataKey, chunkData);
        chunk.RemoveModdata(NeedsSmoothingModDataKey);
    }

}
