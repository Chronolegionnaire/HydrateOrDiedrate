using HydrateOrDiedrate.Aquifer.ModData;
using HydrateOrDiedrate.Config;
using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

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
            serverAPi.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        }

        if(WaterKindById is null) return;
        var blocksToScan = api.World.Blocks.Where(b => b.Code?.Domain == "game");
        var maxId = blocksToScan.Max(b => b.BlockId);
        WaterKindById = new EWaterKind[maxId + 1];

        foreach (var b in blocksToScan)
        {
            var kind = GetWaterKind(b);
            if (kind != EWaterKind.None) WaterKindById[b.BlockId] = kind;
        }
    }

    internal static void Unload()
    {
        WaterKindById = null;
    }

    private static void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        for (int y = 0; y < chunks.Length; y++)
        {
            var chunk = chunks[y];
            if (chunk is null) continue;

            var chunkPos = new FastVec3i(chunkCoord.X, y, chunkCoord.Y);
            
            Task.Run(() => HandleChunkLoaded(chunkPos, chunk));
        }
    }

    private static void HandleChunkLoaded(FastVec3i chunkPos, IWorldChunk chunk)
    {
        if(!chunk.IsChunkValid()) return;

        var aquiferData = GetAquiferChunkData(chunk);
        if(aquiferData is null || aquiferData.Version < CurrentAquiferDataVersion)
        {
            GenerateAquiferData(chunkPos, chunk);
        }
        else if (chunk.GetModdata<byte?>(NeedsSmoothingModDataKey, null) == 1)
        {
            LateSmoothAquiferData(chunkPos, chunk, aquiferData);
        }
    }

    private static AquiferChunkData GenerateAquiferData(FastVec3i chunkPos, IWorldChunk chunk)
    {
        AquiferData data = CalculateAquiferData(chunk, chunkPos);
        if(data is null) return null;

        var result = new AquiferChunkData
        {
            Data = data,
            Version = CurrentAquiferDataVersion
        };

        chunk.SetModdata("aquiferData", result);
        return result;
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

        data.AquiferRating = (int)Math.Clamp(tot / totW, 0, 100);
        return true;
    }

    private static void LateSmoothAquiferData(FastVec3i chunkPos, IWorldChunk chunk, AquiferChunkData chunkData)
    {
        if(!TrySmoothAquiferData(chunkPos, chunkData.Data)) return;

        chunk.SetModdata(AquiferModDataKey, chunkData);
        chunk.RemoveModdata(NeedsSmoothingModDataKey);
    }

    private static WaterCounts GenerateWaterCounts(IMapChunk mapChunk, ILogger logger = null)
    {
        if(mapChunk is not ServerChunk serverChunk) return null;
        serverChunk.Unpack(); // Ensure the chunk is unpacked before accessing its data
        
        if(serverChunk.Data is not ChunkData chunkData || chunkData.fluidsLayer is null) return null;

        var waterCounts = new WaterCounts();

        chunkData.fluidsLayer.readWriteLock.AcquireReadLock();
        try
        {
            for (int i = 0; i < chunkData.Length; i++)
            {
                switch (GetWaterKindById(chunkData.fluidsLayer.Get(i)))
                {
                    case EWaterKind.Normal:
                    case EWaterKind.Ice:
                        waterCounts.NormalWaterBlockCount++;
                        break;

                    case EWaterKind.Salt:
                        waterCounts.SaltWaterBlockCount++;
                        break;

                    case EWaterKind.Boiling:
                        waterCounts.BoilingWaterBlockCount++;
                        break;
                }
            }
        }
        catch(Exception ex)
        {
            logger?.Warning("AquiferManager: exception occured while generating water counts: {exception}", ex);
            return null;
        }
        finally
        {
            chunkData.fluidsLayer.readWriteLock.ReleaseReadLock();
        }

        mapChunk.SetModdata(WaterCountsModDataKey, waterCounts);
        return waterCounts;
    }

    private static AquiferData CalculateAquiferData(IWorldChunk worldChunk, FastVec3i chunkPos)
    {
        var config = ModConfig.Instance.GroundWater;
        var world = HydrateOrDiedrateModSystem._serverApi.World;
        
        long seed = world.Seed;
        int chunkSeed = GameMath.MurmurHash3(
            chunkPos.X ^ (int)(seed >> 40),
            chunkPos.Y ^ (int)(seed >> 20),
            chunkPos.Z ^ (int)(seed)
        );
        LCGRandom rand = new(chunkSeed);
        double chance = rand.NextDouble();

        var mapChunk = worldChunk.MapChunk ?? world.BlockAccessor.GetMapChunk(chunkPos.X, chunkPos.Z);
        if(mapChunk is null) return null;

        WaterCounts counts = GetWaterCounts(mapChunk, world.Logger) ?? GenerateWaterCounts(mapChunk, world.Logger);

        int totalWaterCount = counts.GetTotalWaterBlockCount();
        int worldHeight = world.BlockAccessor.MapSizeY;
        
        var test = world.SeaLevel; //TODO TEST this
        
        int seaLevel = (int)Math.Round(0.4296875 * worldHeight);
        int chunkCenterY = chunkPos.Y * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2;

        double depthMul = 1.0;
        if (chunkCenterY < seaLevel)
        {
            double normalizedDepth = (seaLevel - chunkCenterY) / (double)(seaLevel - 1);
            depthMul = 1 + normalizedDepth * config.AquiferDepthMultiplierScale;
        }

        bool randomChance = chance < config.AquiferRandomMultiplierChance * depthMul;

        if (totalWaterCount < config.AquiferMinimumWaterBlockThreshold && !randomChance)
        {
            return new AquiferData { AquiferRating = 0, IsSalty = false };
        }

        double wNormal = CalculateDiminishingReturns(counts.NormalWaterBlockCount, 300, 1.0, 0.99) * config.AquiferWaterBlockMultiplier;
        double wSalt = CalculateDiminishingReturns(counts.SaltWaterBlockCount, 300, 1.0, 0.99) * config.AquiferSaltWaterMultiplier;
        double wBoiling = CalculateDiminishingReturns(counts.BoilingWaterBlockCount, 1000, 10.0, 0.50) * config.AquiferBoilingWaterMultiplier;

        var blockPos = new BlockPos(
            chunkPos.X * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2,
            chunkPos.Y * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2,
            chunkPos.Z * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2
        );

        float rainfall = world.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.WorldGenValues)?.Rainfall.GuardFinite() ?? 0f;

        double baseMul = 0.75 + rainfall;
        double nsWeighted = (wNormal + wSalt) * baseMul;
        double bWeighted = wBoiling * baseMul;

        int nsRating = NormalizeAquiferRating(nsWeighted, worldHeight);
        int bRating = NormalizeAquiferRating(bWeighted, worldHeight);
        int rating = Math.Clamp(nsRating + bRating, 0, 100);

        if (randomChance)
        {
            int add = 1 + rand.NextInt(10);
            rating += add;

            double rndScale = rand.NextDouble() * (10.0 - 1.1) + 1.1;
            rating = (int)(rating * rndScale);
        }
        
        if (chunkCenterY > seaLevel) rating = Math.Min(rating, config.AquiferRatingCeilingAboveSeaLevel);

        var result = new AquiferData
        {
            AquiferRating = rating,
            IsSalty = counts.SaltWaterBlockCount > counts.NormalWaterBlockCount * 0.5
        };

        if (TrySmoothAquiferData(chunkPos, result))
        {
            worldChunk.RemoveModdata(NeedsSmoothingModDataKey);
        }
        else worldChunk.GetModdata<byte>(NeedsSmoothingModDataKey, 1);

        return result;
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
