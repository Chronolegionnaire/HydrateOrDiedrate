using HydrateOrDiedrate.Wells.Aquifer.ModData;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.Aquifer;

public static partial class AquiferManager
{
    public static readonly int SmoothingRadius = 1;

    private static void SmoothChunkColumnData(AquiferChunkData[] data)
    {
        // Keep a copy so smoothing uses original values, not progressively modified ones
        int[] originalRatings = data.Select(d => d.Data.AquiferRating).ToArray();
    
        for (int y = 0; y < data.Length; y++)
        {
            int below = Math.Max(y - 1, 0);
            int above = Math.Min(y + 1, data.Length - 1);
    
            // Weighted average: neighbors influence, but center weight is higher
            //Note: this is intentionally assigned to the raw rating, not the smoothed one (smoothed one is for cross chunk column smoothing, that cannot be done immediatly)
            data[y].Data.AquiferRatingRaw = Math.Clamp((originalRatings[below] + originalRatings[above] + originalRatings[y] * 2) / 4, 0, 100);
        }
    }

    private static bool TryGetWorldChunksForNearbyColumns(Vec2i vec2i, int chunkYCount, IWorldChunk[,,] chunkBuffer)
    {
        var world = HydrateOrDiedrateModSystem._serverApi.World;
        var provider = world.ChunkProvider;

        for (int chunkY = 0; chunkY < chunkYCount; chunkY++)
        {
            for (int chunkXOffset = -SmoothingRadius; chunkXOffset <= SmoothingRadius; chunkXOffset++)
            {
                int chunkX = vec2i.X + chunkXOffset;

                for (int chunkZOffset = -SmoothingRadius; chunkZOffset <= SmoothingRadius; chunkZOffset++)
                {
                    int chunkZ = vec2i.Y + chunkZOffset;

                    var chunk = provider.GetChunk(chunkX, chunkY, chunkZ);
                    if (chunk is null) return false;

                    chunkBuffer[chunkXOffset + SmoothingRadius, chunkY, chunkZOffset + SmoothingRadius] = chunk;
                }
            }
        }

        return true;
    }

    public static void TrySmoothChunk(int chunkX, int chunkZ)
    {
        var provider = HydrateOrDiedrateModSystem._serverApi.World.BlockAccessor;

        var size = SmoothingRadius * 2 + 1;
        var chunkYCount = provider.MapSizeY / GlobalConstants.ChunkSize;
        var chunkBuffer = new IWorldChunk[size, chunkYCount, size];

        TrySmoothCrossChunkData(new Vec2i(chunkX, chunkZ), chunkYCount, chunkBuffer);
    }

    internal static bool TrySmoothCrossChunkData(Vec2i vec2i, int chunkYCount, IWorldChunk[,,] chunkBuffer)
    {
        //TODO we could potentially also come up with a salt water smoothing method
        var world = HydrateOrDiedrateModSystem._serverApi.World;

        if(!TryGetWorldChunksForNearbyColumns(vec2i, chunkYCount, chunkBuffer)) return false;

        var aquiferData = new AquiferChunkData[chunkBuffer.GetLength(0), chunkBuffer.GetLength(1), chunkBuffer.GetLength(2)];

        var xLength = chunkBuffer.GetLength(0);
        var yLength = chunkBuffer.GetLength(1);
        var zLength = chunkBuffer.GetLength(2);

        for (int x = 0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    var data = GetAquiferChunkData(chunkBuffer[x, y, z], world.Logger);
                    if (data is null || data.Version < CurrentAquiferDataVersion) return false;
                    aquiferData[x, y, z] = data;
                }
            }
        }

        var xCenter = (xLength - 1) / 2;
        var zCenter = (zLength - 1) / 2;

        for (int y = 0; y < yLength; y++)
        {
            var totalRating = 0.0;
            var totalWeight = 0.0;
            var centerWeight = xLength * zLength;

            for (int x = 0; x < xLength; x++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    var weight = 1;
                    if(x == xCenter && zCenter == z) weight = centerWeight;
                    totalRating += aquiferData[x, y, z].Data.AquiferRatingRaw * weight;
                    totalWeight += weight;
                }
            }

            var centerChunkData = aquiferData[xCenter, y, zCenter];
            centerChunkData.Data.AquiferRatingSmoothed = (int)Math.Clamp(totalRating / totalWeight, 0, 100);
            chunkBuffer[xCenter, y, zCenter].LiveModData[AquiferModDataKey] = centerChunkData;
        }

        return true;
    }
}
