using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate
{
    public class AquiferManager
    {
        private readonly ICoreServerAPI serverAPI;
        private readonly ConcurrentQueue<(Vec2i chunkCoord, IWorldChunk[] chunks)> calculationQueue;
        private readonly ConcurrentDictionary<Vec2i, List<WellspringInfo>> wellspringRegistry;
        private readonly ConcurrentDictionary<Vec2i, (int attempts, IWorldChunk[] chunks)> failedChunks;
        private const int MaxFailedAttempts = 5;
        private bool isEnabled;
        private bool runGamePhaseReached;
        private bool isInitialized;

        public AquiferManager(ICoreServerAPI api)
        {
            serverAPI = api;
            calculationQueue = new ConcurrentQueue<(Vec2i chunkCoord, IWorldChunk[] chunks)>();
            wellspringRegistry = new ConcurrentDictionary<Vec2i, List<WellspringInfo>>();
            failedChunks = new ConcurrentDictionary<Vec2i, (int attempts, IWorldChunk[] chunks)>();
            isEnabled = true;
            runGamePhaseReached = false;
            isInitialized = false;
            serverAPI.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            serverAPI.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
            serverAPI.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGamePhaseEntered);
            SetupRetryTimer();
        }

        private void OnRunGamePhaseEntered()
        {
            runGamePhaseReached = true;
        }

        private void OnPlayerNowPlaying(IPlayer player)
        {
            if (isInitialized) return;
            isInitialized = true;
            if (runGamePhaseReached)
            {
                _ = Task.Run(() => ProcessQueuedCalculations())
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            serverAPI.Logger.Error($"Error in ProcessQueuedCalculationsAsync: {t.Exception}");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }


        private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!isEnabled) return;
            calculationQueue.Enqueue((chunkCoord, chunks));
            AttemptReprocessingOfFailedChunks();
        }

        private async Task ProcessQueuedCalculations()
        {
            var tasks = new List<Task>();
            while (calculationQueue.TryDequeue(out var chunkData))
            {
                if (AreChunksValid(chunkData.chunks))
                {
                    tasks.Add(ProcessChunkColumn(chunkData.chunkCoord, chunkData.chunks));
                }
                else
                {
                    RegisterFailedChunk(chunkData.chunkCoord, chunkData.chunks);
                }
            }
            await Task.WhenAll(tasks);
        }

        private async Task ProcessChunkColumn(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (chunks == null || chunks.Length == 0) return;
            if (!AreChunksValid(chunks))
            {
                RegisterFailedChunk(chunkCoord, chunks);
                return;
            }
            var mainChunk = chunks[0];
            if (mainChunk == null) return;
            mainChunk.Unpack();
            var data = mainChunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null)
            {
                var pre = await Task.Run(() => CalculateAquiferData(chunks, chunkCoord));
                data = new AquiferChunkData
                {
                    PreSmoothedData = pre,
                    SmoothedData = null
                };
                mainChunk.SetModdata("aquiferData", data);
            }
            if (data.SmoothedData == null)
            {
                await Task.Run(() => SmoothWithNeighbors(mainChunk, chunkCoord));
            }
        }


        private bool AreChunksValid(IWorldChunk[] chunks)
        {
            foreach (var chunk in chunks)
            {
                if (chunk == null || chunk.Disposed || chunk.Data == null || chunk.Data.Length == 0) return false;
            }
            return true;
        }

        private void SetupRetryTimer()
        {
            serverAPI.Event.RegisterGameTickListener(dt =>
            {
                if (calculationQueue.Count > 0 && isInitialized && runGamePhaseReached)
                {
                    _ = ProcessQueuedCalculations(); 
                }
                AttemptReprocessingOfFailedChunks();
            }, 5000);
        }
        private void AttemptReprocessingOfFailedChunks()
        {
            foreach (var kvp in failedChunks)
            {
                if (AreChunksValid(kvp.Value.chunks))
                {
                    if (failedChunks.TryRemove(kvp.Key, out var value))
                    {
                        calculationQueue.Enqueue((kvp.Key, value.chunks));
                    }
                }
            }
        }


        private void RegisterFailedChunk(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            failedChunks.AddOrUpdate(
                chunkCoord,
                (1, chunks),
                (key, existingValue) =>
                {
                    int attempts = existingValue.attempts + 1;
                    if (attempts < MaxFailedAttempts)
                    {
                        return (attempts, chunks);
                    }
                    else
                    {
                        failedChunks.TryRemove(key, out _);
                        return existingValue;
                    }
                });
        }

        private double CalculateDiminishingReturns(int blockCount, double startValue, double minValue, double decayRate)
        {
            double total = 0;
            for (int i = 1; i <= blockCount; i++)
            {
                total += Math.Max(minValue, startValue / (1 + decayRate * (i - 1)));
            }
            return total;
        }

        private int NormalizeAquiferRating(double weightedWaterBlocks)
        {
            double baselineMax = 3000;
            double minWeightedValue = 0;
            int worldHeight = serverAPI.WorldManager.MapSizeY;
            double maxWeightedValue = CalculateDiminishingReturns(
                (int)(baselineMax * (worldHeight / 256.0)),
                startValue: 500.0,
                minValue: 4.0,
                decayRate: 0.80
            );
            if (maxWeightedValue == minWeightedValue) return 0;
            int normalizedRating = (int)((weightedWaterBlocks - minWeightedValue) / (maxWeightedValue - minWeightedValue) * 100);
            return Math.Clamp(normalizedRating, 0, 100);
        }

        private AquiferData CalculateAquiferData(IWorldChunk[] chunks, Vec2i chunkCoord)
        {
            try
            {
                int normalWaterBlockCount = 0;
                int saltWaterBlockCount = 0;
                int boilingWaterBlockCount = 0;
                int chunkSize = GlobalConstants.ChunkSize;
                const int step = 4;
                const double waterBlockMultiplier = 4.0;
                const double saltWaterMultiplier = 4.0;
                const int boilingWaterMultiplier = 100;
                const double randomMultiplierChance = 0.07;
                int worldSeed = serverAPI.WorldManager.SaveGame.Seed;
                int chunkSeed = worldSeed ^ (chunkCoord.X * 73856093) ^ (chunkCoord.Y * 19349663);
                Random random = new Random(chunkSeed);
                object lockObj = new object();
                BlockPos center = new BlockPos(chunkCoord.X * chunkSize + chunkSize / 2, 0, chunkCoord.Y * chunkSize + chunkSize / 2);
                ClimateCondition climate = serverAPI.World.BlockAccessor.GetClimateAt(center, EnumGetClimateMode.WorldGenValues);
                float rainMul = 0.75f + (climate?.Rainfall ?? 0f);
                Parallel.For(0, chunks.Length,
                    () => new LocalCounts(),
                    (index, state, localCounts) =>
                    {
                        var chunk = chunks[index];
                        if (chunk == null || chunk.Disposed || chunk.Data == null || chunk.Data.Length == 0) return localCounts;
                        var chunkData = chunk.Data;
                        int blockCount = chunkData.Length;
                        for (int y = 0; y < chunkSize; y += step)
                        {
                            for (int x = 0; x < chunkSize; x += step)
                            {
                                for (int z = 0; z < chunkSize; z += step)
                                {
                                    int idx = (y * chunkSize + z) * chunkSize + x;
                                    if (idx < 0 || idx >= blockCount) continue;
                                    int blockId = chunkData[idx];
                                    if (blockId < 0) continue;
                                    Block block = serverAPI.World.GetBlock(blockId);
                                    if (block?.Code?.Path == null) continue;
                                    if (block.Code.Path.Contains("boilingwater")) localCounts.BoilingCount++;
                                    else if (block.Code.Path.Contains("water"))
                                    {
                                        localCounts.NormalCount++;
                                        if (block.Code.Path.Contains("saltwater")) localCounts.SaltCount++;
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
                    }
                );
                double GetDiminishing(int count, double startV, double minV, double decayR)
                {
                    double t = 0;
                    for (int i = 1; i <= count; i++)
                    {
                        t += Math.Max(minV, startV / (1 + decayR * (i - 1)));
                    }
                    return t;
                }
                double weightedNormal = GetDiminishing(normalWaterBlockCount, 300.0, 1.0, 0.99) * waterBlockMultiplier;
                double weightedSalt = GetDiminishing(saltWaterBlockCount, 300.0, 1.0, 0.99) * saltWaterMultiplier;
                double weightedBoiling = GetDiminishing(boilingWaterBlockCount, 1000.0, 10.0, 0.5) * boilingWaterMultiplier;
                double totalWeighted = (weightedNormal + weightedSalt + weightedBoiling) * rainMul;
                int rating = NormalizeAquiferRating(totalWeighted);
                if (random.NextDouble() < randomMultiplierChance)
                {
                    int baseAdd = random.Next(1, 11);
                    rating += baseAdd;
                    double rndMul = random.NextDouble() * (10.0 - 1.1) + 1.1;
                    rating = (int)(rating * rndMul);
                    rating = Math.Clamp(rating, 0, 100);
                }
                bool salty = saltWaterBlockCount > normalWaterBlockCount * 0.5;
                return new AquiferData
                {
                    AquiferRating = rating,
                    IsSalty = salty
                };
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Error("CalculateAquiferData exception: " + ex);
                return new AquiferData { AquiferRating = 0, IsSalty = false };
            }
        }

        private void SmoothWithNeighbors(IWorldChunk mainChunk, Vec2i chunkCoord)
        {
            if (!isEnabled) return;

            if (mainChunk?.GetModdata<AquiferChunkData>("aquiferData", null) == null)
            {
                RegisterFailedChunk(chunkCoord, new[] { mainChunk });
                return;
            }
            var data = mainChunk?.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null)
            {
                RegisterFailedChunk(chunkCoord, new[] { mainChunk });
            }
            if (data == null || data.SmoothedData != null) return;
            double centralChunkRating = data.PreSmoothedData.AquiferRating;
            int radius = 2;
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
            int worldSeed = serverAPI.WorldManager.SaveGame.Seed;
            int chunkSeed = worldSeed ^ (chunkCoord.X * 73856093) ^ (chunkCoord.Y * 19349663);
            Random random = new Random(chunkSeed);
            bool hasInvalidNeighbors = false;
            int highestNeighborRating = 0;
            int saltyNeighbors = 0;
            int totalNeighbors = 0;
            foreach (var ncoord in neighborCoords)
            {
                var neighborChunk = serverAPI.World.BlockAccessor.GetChunk(ncoord.X, 0, ncoord.Y);
                if (neighborChunk == null || neighborChunk.Disposed)
                {
                    hasInvalidNeighbors = true;
                    RegisterFailedChunk(ncoord,
                        neighborChunk == null ? Array.Empty<IWorldChunk>() : new[] { neighborChunk });
                    continue;
                }
                neighborChunk.Unpack();
                AquiferChunkData neighborAqData = null;
                try
                {
                    neighborAqData = neighborChunk.GetModdata<AquiferChunkData>("aquiferData", null);
                }
                catch (Exception ex)
                {
                    RegisterFailedChunk(ncoord, new[] { neighborChunk });
                    hasInvalidNeighbors = true;
                    continue;
                }

                if (neighborAqData == null)
                {
                    hasInvalidNeighbors = true;
                    RegisterFailedChunk(ncoord, new[] { neighborChunk });
                    continue;
                }
                var neighborAq = neighborAqData.SmoothedData ?? neighborAqData.PreSmoothedData;
                int neighborRating = neighborAq.AquiferRating;
                bool isNeighborSalty = neighborAq.IsSalty;

                highestNeighborRating = Math.Max(highestNeighborRating, neighborRating);

                if (isNeighborSalty) saltyNeighbors++;
                totalNeighbors++;
            }

            if (hasInvalidNeighbors)
            {
                RegisterFailedChunk(chunkCoord, new[] { mainChunk });
                return;
            }
            if (highestNeighborRating < centralChunkRating)
            {
                double reductionFactor = 0.1 + (random.NextDouble() * 0.1);
                centralChunkRating -= centralChunkRating * reductionFactor;
            }
            bool finalIsSalty = saltyNeighbors > (totalNeighbors / 2.0);

            data.SmoothedData = new AquiferData
            {
                AquiferRating = (int)Math.Clamp(centralChunkRating, 0, 100),
                IsSalty = finalIsSalty
            };

            mainChunk.SetModdata("aquiferData", data);
        }

        public AquiferData GetAquiferData(Vec2i chunkCoord)
        {
            if (!isEnabled) return null;
            var chunk = serverAPI.World.BlockAccessor.GetChunk(chunkCoord.X, 0, chunkCoord.Y);
            if (chunk == null) return null;
            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null) return null;
            return data.SmoothedData;
        }

        public void ClearAquiferData()
        {
            if (!isEnabled) return;
            int cX = serverAPI.WorldManager.MapSizeX / GlobalConstants.ChunkSize;
            int cZ = serverAPI.WorldManager.MapSizeZ / GlobalConstants.ChunkSize;
            for (int x = 0; x < cX; x++)
            {
                for (int z = 0; z < cZ; z++)
                {
                    var c = serverAPI.World.BlockAccessor.GetChunk(x, 0, z);
                    if (c != null) c.RemoveModdata("aquiferData");
                }
            }
        }

        [ProtoContract]
        public class AquiferData
        {
            [ProtoMember(1)] public int AquiferRating { get; set; }
            [ProtoMember(2)] public bool IsSalty { get; set; }
        }

        [ProtoContract]
        public class AquiferChunkData
        {
            [ProtoMember(1)] public AquiferData PreSmoothedData { get; set; }
            [ProtoMember(2)] public AquiferData SmoothedData { get; set; }
        }

        public class LocalCounts
        {
            public int NormalCount { get; set; }
            public int SaltCount { get; set; }
            public int BoilingCount { get; set; }
        }

        public void RegisterWellspring(BlockPos pos, double depthFactor)
        {
            Vec2i c = new Vec2i(pos.X / GlobalConstants.ChunkSize, pos.Z / GlobalConstants.ChunkSize);
            var info = new WellspringInfo { Position = pos, DepthFactor = depthFactor };
            wellspringRegistry.AddOrUpdate(c, _ => new List<WellspringInfo> { info }, (_, list) =>
            {
                lock (list) list.Add(info);
                return list;
            });
        }

        public void UnregisterWellspring(BlockPos pos)
        {
            Vec2i c = new Vec2i(pos.X / GlobalConstants.ChunkSize, pos.Z / GlobalConstants.ChunkSize);
            if (wellspringRegistry.TryGetValue(c, out var list))
            {
                lock (list) list.RemoveAll(ws => ws.Position.Equals(pos));
            }
        }

        public List<WellspringInfo> GetWellspringsInChunk(Vec2i chunkCoord)
        {
            if (wellspringRegistry.TryGetValue(chunkCoord, out var list))
            {
                lock (list) return new List<WellspringInfo>(list);
            }
            return new List<WellspringInfo>();
        }

        public class WellspringInfo
        {
            public BlockPos Position { get; set; }
            public double DepthFactor { get; set; }
        }
    }
}