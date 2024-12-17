using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

public class AquiferSystem
{
    private readonly ICoreServerAPI serverAPI;
    private readonly ConcurrentDictionary<Vec2i, AquiferChunkData> aquiferDataCache;

    private bool isEnabled;

    public AquiferSystem(ICoreServerAPI api)
    {
        serverAPI = api;
        aquiferDataCache = new ConcurrentDictionary<Vec2i, AquiferChunkData>();
        isEnabled = true;
        serverAPI.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
        serverAPI.Event.SaveGameLoaded += OnSaveGameLoaded;
        serverAPI.Event.GameWorldSave += OnSaveGameSaving;
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }

    private void OnSaveGameLoaded()
    {
        if (!isEnabled) return;
        var loadedData = serverAPI.WorldManager.SaveGame.GetData<Dictionary<Vec2i, AquiferChunkData>>("aquiferData");
        if (loadedData != null)
        {
            aquiferDataCache.Clear();
            foreach (var entry in loadedData)
            {
                aquiferDataCache[entry.Key] = entry.Value;
            }
        }
    }

    private void OnSaveGameSaving()
    {
        if (!isEnabled) return;
        serverAPI.WorldManager.SaveGame.StoreData("aquiferData", aquiferDataCache);
    }

    private async void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
    {
        if (!isEnabled) return;

        if (!aquiferDataCache.ContainsKey(chunkCoord))
        {
            var preSmoothedData = await Task.Run(() => CalculateAquiferData(chunks, chunkCoord));

            aquiferDataCache[chunkCoord] = new AquiferChunkData
            {
                PreSmoothedData = preSmoothedData,
                SmoothedData = preSmoothedData
            };
        }
        await Task.Run(() => SmoothWithNeighbors(chunkCoord));
    }
    public class LocalCounts
    {
        public int NormalCount { get; set; }
        public int SaltCount { get; set; }
        public int BoilingCount { get; set; }
    }

    private AquiferData CalculateAquiferData(IWorldChunk[] chunks, Vec2i chunkCoord)
    {
        int normalWaterBlockCount = 0;
        int saltWaterBlockCount = 0;
        int boilingWaterBlockCount = 0;
        int chunkSize = serverAPI.World.BlockAccessor.ChunkSize;

        const int step = 4;
        const double waterBlockMultiplier = 50.0;
        const double saltWaterMultiplier = 5.0;
        const int boilingWaterMultiplier = 1500;
        const double randomMultiplierChance = 0.01;

        int worldSeed = serverAPI.WorldManager.SaveGame.Seed;
        int chunkSeed = worldSeed ^ (chunkCoord.X * 73856093) ^ (chunkCoord.Y * 19349663);
        Random random = new Random(chunkSeed);

        object lockObj = new object();
        
        Parallel.For(0, chunks.Length, 
            () => new LocalCounts(),
            (chunkIndex, state, localCounts) =>
            {
                var chunk = chunks[chunkIndex];
                if (chunk?.Blocks == null) return localCounts;

                for (int y = 0; y < chunkSize; y += step)
                {
                    for (int x = 0; x < chunkSize; x += step)
                    {
                        for (int z = 0; z < chunkSize; z += step)
                        {
                            int index = (y * chunkSize + z) * chunkSize + x;
                            if (index < 0 || index >= chunk.Blocks.Length) continue;

                            int blockId = chunk.Blocks[index];
                            Block block = serverAPI.World.GetBlock(blockId);
                            if (block == null) continue;

                            if (block.Code.Path.Contains("boilingwater"))
                                localCounts.BoilingCount++;
                            else if (block.Code.Path.Contains("water"))
                            {
                                localCounts.NormalCount++;
                                if (block.Code.Path.Contains("saltwater"))
                                    localCounts.SaltCount++;
                            }
                        }
                    }
                }

                return localCounts;
            },
            localCounts =>
            {
                lock (lockObj)
                {
                    normalWaterBlockCount += localCounts.NormalCount;
                    saltWaterBlockCount += localCounts.SaltCount;
                    boilingWaterBlockCount += localCounts.BoilingCount;
                }
            });
        int adjustedWaterBlockCount = normalWaterBlockCount + (int)(saltWaterBlockCount * saltWaterMultiplier) +
                                      (boilingWaterBlockCount * boilingWaterMultiplier);

        int maxBlocks = chunkSize * chunkSize * chunkSize / (step * step * step);
        double weightedWaterBlocks = adjustedWaterBlockCount * waterBlockMultiplier;
        int aquiferRating = maxBlocks > 0 ? (int)((weightedWaterBlocks / maxBlocks) * 100) : 0;

        aquiferRating = Math.Clamp(aquiferRating, 0, 100);

        if (random.NextDouble() < randomMultiplierChance)
        {
            int baseRandomAddition = random.Next(1, 11);
            aquiferRating += baseRandomAddition;

            double randomMultiplier = random.NextDouble() * (10.0 - 1.1) + 1.1;
            aquiferRating = (int)(aquiferRating * randomMultiplier);

            aquiferRating = Math.Max(aquiferRating, 1);
            aquiferRating = Math.Clamp(aquiferRating, 0, 100);
        }

        bool isSalty = adjustedWaterBlockCount > 0 && (saltWaterBlockCount > adjustedWaterBlockCount * 0.5);

        return new AquiferData
        {
            AquiferRating = aquiferRating,
            IsSalty = isSalty
        };
    }

    private void SmoothWithNeighbors(Vec2i chunkCoord)
    {
        if (!isEnabled) return;

        if (!aquiferDataCache.TryGetValue(chunkCoord, out var chunkData)) return;

        int radius = 4;
        double centralChunkRating = chunkData.PreSmoothedData.AquiferRating;
        double totalWeightedRating = 0;
        double totalWeight = 0;
        bool anySalty = chunkData.PreSmoothedData.IsSalty;

        double centralWeight = 2.0 + Math.Sqrt(centralChunkRating) / 10.0;
        totalWeightedRating += centralChunkRating * centralWeight;
        totalWeight += centralWeight;
        var neighborCoords = new List<Vec2i>();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    neighborCoords.Add(new Vec2i(chunkCoord.X + dx, chunkCoord.Y + dy));
                }
            }
        }

        object lockObj = new object();

        Parallel.ForEach(neighborCoords, neighborCoord =>
        {
            if (aquiferDataCache.TryGetValue(neighborCoord, out var neighborChunkData))
            {
                var neighborRating = neighborChunkData.PreSmoothedData.AquiferRating;
                bool isNeighborSalty = neighborChunkData.PreSmoothedData.IsSalty;

                double dist = Math.Sqrt(Math.Pow(neighborCoord.X - chunkCoord.X, 2) +
                                        Math.Pow(neighborCoord.Y - chunkCoord.Y, 2));
                double neighborWeight = (1.0 / (1.0 + dist)) * (1.0 + Math.Sqrt(neighborRating) / 10.0);

                lock (lockObj)
                {
                    totalWeightedRating += neighborRating * neighborWeight;
                    totalWeight += neighborWeight;

                    if (isNeighborSalty) anySalty = true;
                }
            }
        });

        int smoothedRating = (int)Math.Round(totalWeightedRating / totalWeight);
        smoothedRating = Math.Max(smoothedRating, (int)(centralChunkRating * 0.5));

        chunkData.SmoothedData = new AquiferData
        {
            AquiferRating = smoothedRating,
            IsSalty = anySalty
        };
    }

    public AquiferData GetAquiferData(Vec2i chunkCoord)
    {
        if (!isEnabled) return null;

        return aquiferDataCache.TryGetValue(chunkCoord, out var chunkData) ? chunkData.SmoothedData : null;
    }

    private IWorldChunk[] GetChunksForColumn(Vec2i chunkCoord)
    {
        int chunkHeight = serverAPI.World.BlockAccessor.MapSizeY / serverAPI.World.BlockAccessor.ChunkSize;
        IWorldChunk[] chunks = new IWorldChunk[chunkHeight];

        for (int i = 0; i < chunkHeight; i++)
        {
            chunks[i] = serverAPI.World.BlockAccessor.GetChunk(chunkCoord.X, i, chunkCoord.Y);
        }

        return chunks;
    }
    [ProtoContract]
    public class AquiferData
    {
        [ProtoMember(1)]
        public int AquiferRating { get; set; }

        [ProtoMember(2)]
        public bool IsSalty { get; set; }
    }
    [ProtoContract]
    public class AquiferChunkData
    {
        [ProtoMember(1)]
        public AquiferData PreSmoothedData { get; set; }

        [ProtoMember(2)]
        public AquiferData SmoothedData { get; set; }
    }
    public void ClearAquiferData()
    {
        if (!isEnabled) return;
        aquiferDataCache.Clear();
        serverAPI.WorldManager.SaveGame.StoreData("aquiferData", null);
        serverAPI.Logger.Notification("Aquifer data has been cleared.");
    }
}
