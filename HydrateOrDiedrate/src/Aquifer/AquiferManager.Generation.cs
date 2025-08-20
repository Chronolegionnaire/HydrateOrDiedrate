using HydrateOrDiedrate.Aquifer.ModData;
using HydrateOrDiedrate.Config;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace HydrateOrDiedrate.Aquifer;

public static partial class AquiferManager
{
    private static void HandleChunkColumnLoaded(Vec2i chunkColumnCoord, IWorldChunk[] chunks)
    {
        var world = HydrateOrDiedrateModSystem._serverApi.World;
        var mapChunk = world.BlockAccessor.GetMapChunk(chunkColumnCoord);
        var waterCounts = GetWaterCounts(mapChunk, world.Logger);

        if(waterCounts is null)
        {
            waterCounts = GenerateWaterCounts(chunks);
            mapChunk.SetModdata(WaterCountsModDataKey, waterCounts);
        }
        
        bool needsSmoothing = false;
        for (int i = 0; i < chunks.Length; i++)
        {
            var data = chunks[i].GetModdata<AquiferChunkData>(AquiferModDataKey);
            if(data is null || data.Version < CurrentAquiferDataVersion)
            {
                GenerateAquiferChunkDataColumn(chunkColumnCoord, chunks, waterCounts);
                needsSmoothing = true;
                break;
            }
            else if(data.Data.AquiferRatingSmoothed is null) needsSmoothing = true;
        }

        if(needsSmoothing) QueueForCrossChunkSmoothing(chunkColumnCoord);
    }

    private static void GenerateAquiferChunkDataColumn(Vec2i chunkColumnCoord, IWorldChunk[] chunks, WaterCounts waterCounts)
    {
        var data = new AquiferChunkData[chunks.Length];

        for(var i = 0; i < chunks.Length; i++)
        {
            data[i] = new AquiferChunkData
            {
                Data = GenerateAquiferData(chunkColumnCoord, i, waterCounts),
                Version = CurrentAquiferDataVersion
            };
        }

        SmoothChunkColumnData(data);

        for (var i = 0; i < chunks.Length; i++)
        {
            //Save the generated data to the chunk
            chunks[i].LiveModData[AquiferModDataKey] = data[i];
        }
    }

    private static AquiferData GenerateAquiferData(Vec2i chunkColumnCoord, int chunkCoordY, WaterCounts waterCounts)
    {
        var config = ModConfig.Instance.GroundWater;
        var world = HydrateOrDiedrateModSystem._serverApi.World;
        
        long seed = world.Seed;
        int chunkSeed = GameMath.MurmurHash3(
            chunkColumnCoord.X ^ (int)(seed >> 40),
            chunkColumnCoord.Y ^ (int)(seed >> 20),
            chunkCoordY ^ (int)(seed)
        );
        LCGRandom rand = new(chunkSeed);
        double chance = rand.NextDouble();

        int totalWaterCount = waterCounts.GetTotalWaterBlockCount();
        int worldHeight = world.BlockAccessor.MapSizeY;
        
        int seaLevel = world.SeaLevel;
        int chunkCenterY = chunkCoordY * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2;

        double depthMul = 1.0;
        if (chunkCenterY < seaLevel)
        {
            double normalizedDepth = (seaLevel - chunkCenterY) / (double)(seaLevel - 1);
            depthMul = 1 + normalizedDepth * config.AquiferDepthMultiplierScale;
        }

        bool randomChance = chance < config.AquiferRandomMultiplierChance * depthMul;

        if (totalWaterCount < config.AquiferMinimumWaterBlockThreshold && !randomChance)
        {
            return new AquiferData
            {
                AquiferRatingRaw = 0,
                IsSalty = false
            };
        }

        //TODO extract anything that is not related to Y coordinate to a seperate method to avoid duplicate calculations
        double wNormal = CalculateDiminishingReturns(waterCounts.NormalWaterBlockCount, 300, 1.0, 0.99) * config.AquiferWaterBlockMultiplier;
        double wSalt = CalculateDiminishingReturns(waterCounts.SaltWaterBlockCount, 300, 1.0, 0.99) * config.AquiferSaltWaterMultiplier;
        double wBoiling = CalculateDiminishingReturns(waterCounts.BoilingWaterBlockCount, 1000, 10.0, 0.50) * config.AquiferBoilingWaterMultiplier;

        var blockPos = new BlockPos(
            chunkColumnCoord.X * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2,
            chunkColumnCoord.Y * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2,
            chunkCenterY
        );

        float rainfall = world.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.WorldGenValues)?.Rainfall.GuardFinite() ?? 0f;

        double baseMul = 0.75 + rainfall;
        double nsWeighted = (wNormal + wSalt + wBoiling) * baseMul;

        int nsRating = NormalizeAquiferRating(nsWeighted, worldHeight);
        int rating = Math.Clamp(nsRating, 0, 100);

        if (randomChance)
        {
            int add = 1 + rand.NextInt(10);
            rating += add;

            double rndScale = rand.NextDouble() * (10.0 - 1.1) + 1.1;
            rating = (int)(rating * rndScale);
        }
        
        if (chunkCenterY > seaLevel) rating = Math.Min(rating, config.AquiferRatingCeilingAboveSeaLevel);

        return new AquiferData
        {
            AquiferRatingRaw = rating,
            IsSalty = waterCounts.SaltWaterBlockCount > waterCounts.NormalWaterBlockCount * 0.5
        };
    }

    private static WaterCounts GenerateWaterCounts(IWorldChunk[] chunks)
    {
        var waterCounts = new WaterCounts();

        foreach (var chunk in chunks)
        {
            chunk.Unpack();
            if (chunk.Data is not ChunkData chunkData || chunkData.fluidsLayer is null) continue;
            var fluidsLayer = chunkData.fluidsLayer;

            fluidsLayer.readWriteLock.AcquireReadLock();
            try
            {
                for (var i = 0; i < chunkData.Length; i++)
                {
                    waterCounts.Increment(GetWaterKindById(fluidsLayer.Get(i)));
                }
            }
            finally
            {
                fluidsLayer.readWriteLock.ReleaseReadLock();
            }
        }

        return waterCounts;
    }

    private static double CalculateDiminishingReturns(int n, double S, double m, double d)
    {
        int k = (int)Math.Floor((S/m - 1)/d) + 1;
        int x = Math.Min(n, k);
        double H = 1.0;
        for(int i = 1; i <= x; i++) H += 1.0/i;
        double sum = (S/d)*(H - 1);
        if (n > k) sum += (n - k)*m;
        return sum; 
    }

    private static int NormalizeAquiferRating(double weightedWaterBlocks, int worldHeight)
    {
        double baselineMax = 3000;
        double minWeightedValue = 0;
        double maxWeightedValue = CalculateDiminishingReturns((int)(baselineMax * (worldHeight / 256.0)), 500.0, 4.0, 0.80);
        if (maxWeightedValue == minWeightedValue) return 0;
        int normalizedRating = (int)((weightedWaterBlocks - minWeightedValue) / (maxWeightedValue - minWeightedValue) * 100);
        return Math.Clamp(normalizedRating, 0, 100);
    }
}
