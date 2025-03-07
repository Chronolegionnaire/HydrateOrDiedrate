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
    public struct ChunkPos3D
    {
        public int X;
        public int Y;
        public int Z;

        public ChunkPos3D(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }
    }

    public class AquiferManager
    {
        private readonly ICoreServerAPI serverAPI;
        private readonly ConcurrentQueue<(ChunkPos3D pos, IWorldChunk chunk)> calculationQueue;
        private readonly ConcurrentDictionary<ChunkPos3D, IWorldChunk> loadedChunks;
        private readonly ConcurrentDictionary<ChunkPos3D, (int attempts, IWorldChunk chunk)> failedChunks;
        private readonly ConcurrentDictionary<ChunkPos3D, List<WellspringInfo>> wellspringRegistry;
        private const int CurrentAquiferDataVersion = 1;
        private bool isEnabled;
        private bool runGamePhaseReached;
        private bool isInitialized;
        private const int MaxFailedAttempts = 5;

        public AquiferManager(ICoreServerAPI api)
        {
            serverAPI = api;
            calculationQueue = new ConcurrentQueue<(ChunkPos3D pos, IWorldChunk chunk)>();
            loadedChunks = new ConcurrentDictionary<ChunkPos3D, IWorldChunk>();
            failedChunks = new ConcurrentDictionary<ChunkPos3D, (int attempts, IWorldChunk chunk)>();
            wellspringRegistry = new ConcurrentDictionary<ChunkPos3D, List<WellspringInfo>>();
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
                            serverAPI.Logger.Error($"Error in ProcessQueuedCalculations: {t.Exception}");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!isEnabled) return;
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                if (chunk == null) continue;
                var pos3D = new ChunkPos3D(chunkCoord.X, i, chunkCoord.Y);
                loadedChunks[pos3D] = chunk;
                calculationQueue.Enqueue((pos3D, chunk));
            }
            AttemptReprocessingOfFailedChunks();
        }

        private async Task ProcessQueuedCalculations()
        {
            var tasks = new List<Task>();
            while (calculationQueue.TryDequeue(out var item))
            {
                if (IsChunkValid(item.chunk))
                {
                    tasks.Add(ProcessChunk(item.pos, item.chunk));
                }
                else
                {
                    RegisterFailedChunk(item.pos, item.chunk);
                }
            }
            await Task.WhenAll(tasks);
        }
        private async Task ProcessChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            if (chunk == null || !IsChunkValid(chunk)) return;
            chunk.Unpack();
            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);

            if (data == null || data.Version < CurrentAquiferDataVersion)
            {
                var pre = await Task.Run(() => CalculateAquiferData(chunk, pos));
                data = new AquiferChunkData
                {
                    PreSmoothedData = pre,
                    SmoothedData = null,
                    Version = CurrentAquiferDataVersion
                };
                chunk.SetModdata("aquiferData", data);
            }

            if (data.SmoothedData == null)
            {
                await Task.Run(() => SmoothWithNeighbors(chunk, pos));
            }
        }

        private bool IsChunkValid(IWorldChunk chunk)
        {
            if (chunk == null) return false;
            if (chunk.Disposed) return false;
            if (chunk.Data == null || chunk.Data.Length == 0) return false;
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
            }, 5000);
            serverAPI.Event.RegisterGameTickListener(dt => { RetryFailedChunks(); }, 5000);
        }

        private void RetryFailedChunks()
        {
            foreach (var kvp in failedChunks)
            {
                var pos = kvp.Key;
                var (attempts, chunk) = kvp.Value;

                if (attempts >= 10)
                {
                    failedChunks.TryRemove(pos, out _);
                    continue;
                }

                if (IsChunkValid(chunk))
                {
                    if (failedChunks.TryRemove(pos, out _))
                    {
                        calculationQueue.Enqueue((pos, chunk));
                    }
                }
                else
                {
                    failedChunks[pos] = (attempts + 1, chunk);
                }
            }
        }

        private void RegisterFailedChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            failedChunks.AddOrUpdate(
                pos,
                (1, chunk),
                (key, existingValue) =>
                {
                    int attempts = existingValue.attempts + 1;
                    return (attempts, chunk);
                });
        }

        private void AttemptReprocessingOfFailedChunks()
        {
            foreach (var kvp in failedChunks)
            {
                if (IsChunkValid(kvp.Value.chunk))
                {
                    if (failedChunks.TryRemove(kvp.Key, out var value))
                    {
                        calculationQueue.Enqueue((kvp.Key, value.chunk));
                    }
                }
            }
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

        private AquiferData CalculateAquiferData(IWorldChunk chunk, ChunkPos3D pos)
        {
            try
            {
                var config = HydrateOrDiedrateModSystem.LoadedConfig;

                int normalWaterBlockCount = 0;
                int saltWaterBlockCount = 0;
                int boilingWaterBlockCount = 0;
                int chunkSize = GlobalConstants.ChunkSize;
                int step = config.AquiferStep;
                double waterBlockMultiplier = config.AquiferWaterBlockMultiplier;
                double saltWaterMultiplier = config.AquiferSaltWaterMultiplier;
                int boilingWaterMultiplier = config.AquiferBoilingWaterMultiplier;
                double randomMultiplierChance = config.AquiferRandomMultiplierChance;

                int worldSeed = serverAPI.WorldManager.SaveGame.Seed;
                int chunkSeed = worldSeed ^ (pos.X * 73856093) ^ (pos.Z * 19349663) ^ (pos.Y * 83492791);
                Random random = new Random(chunkSeed);
                object lockObj = new object();
                BlockPos center = new BlockPos(
                    pos.X * chunkSize + chunkSize / 2,
                    pos.Y * chunkSize + chunkSize / 2,
                    pos.Z * chunkSize + chunkSize / 2
                );
                ClimateCondition climate =
                    serverAPI.World.BlockAccessor.GetClimateAt(center, EnumGetClimateMode.WorldGenValues);
                float rainMul = 0.75f + (climate?.Rainfall ?? 0f);
                int worldHeight = serverAPI.WorldManager.MapSizeY;
                int seaLevel = (int)Math.Round(0.4296875 * worldHeight);
                int chunkCenterY = center.Y;

                int blockCount = chunk.Data.Length;

                Parallel.For(0, chunkSize,
                    () => new LocalCounts(),
                    (y, state, localCounts) =>
                    {
                        for (int x = 0; x < chunkSize; x += step)
                        {
                            for (int z = 0; z < chunkSize; z += step)
                            {
                                int idx = (y * chunkSize + z) * chunkSize + x;
                                if (idx < 0 || idx >= blockCount) continue;
                                int blockId = chunk.Data[idx];
                                if (blockId < 0) continue;

                                Block block = serverAPI.World.GetBlock(blockId);
                                if (block?.Code?.Path == null) continue;

                                if (block.Code.Path.Contains("boilingwater"))
                                {
                                    localCounts.BoilingCount++;
                                }
                                else if (block.Code.Path.Contains("water"))
                                {
                                    localCounts.NormalCount++;
                                    if (block.Code.Path.Contains("saltwater"))
                                    {
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
                double weightedBoiling =
                    GetDiminishing(boilingWaterBlockCount, 1000.0, 10.0, 0.5) * boilingWaterMultiplier;
                double totalWeighted = (weightedNormal + weightedSalt + weightedBoiling) * rainMul;

                int rating = NormalizeAquiferRating(totalWeighted);
                if (chunkCenterY > seaLevel)
                {
                    rating = Math.Min(rating, config.AquiferRatingCeilingAboveSeaLevel);
                }
                double depthMultiplier = 1.0;
                if (chunkCenterY < seaLevel)
                {
                    double depthDifference = seaLevel - chunkCenterY;
                    double normalizedDepth = depthDifference / (double)seaLevel;
                    depthMultiplier = 1 + normalizedDepth * config.AquiferDepthMultiplierScale;
                }
                if (random.NextDouble() < randomMultiplierChance * depthMultiplier)
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

        private void SmoothWithNeighbors(IWorldChunk chunk, ChunkPos3D pos)
        {
            if (!isEnabled) return;

            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null)
            {
                RegisterFailedChunk(pos, chunk);
                return;
            }
            double centralChunkRating = data.PreSmoothedData.AquiferRating;
            int radius = 1;
            var neighborPositions = new List<ChunkPos3D>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        neighborPositions.Add(new ChunkPos3D(pos.X + dx, pos.Y + dy, pos.Z + dz));
                    }
                }
            }

            int worldSeed = serverAPI.WorldManager.SaveGame.Seed;
            int chunkSeed = worldSeed ^ (pos.X * 73856093) ^ (pos.Z * 19349663) ^ (pos.Y * 83492791);
            Random random = new Random(chunkSeed);
            int highestNeighborRating = 0;
            int saltyNeighbors = 0;
            int totalValidNeighbors = 0;
            foreach (var npos in neighborPositions)
            {
                var neighborChunk = GetChunkAt(npos);
                if (neighborChunk == null || !IsChunkValid(neighborChunk))
                {
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
                    continue;
                }

                if (neighborAqData == null)
                {
                    continue;
                }

                var neighborAq = neighborAqData.SmoothedData ?? neighborAqData.PreSmoothedData;
                int neighborRating = neighborAq.AquiferRating;
                bool isNeighborSalty = neighborAq.IsSalty;

                highestNeighborRating = Math.Max(highestNeighborRating, neighborRating);
                if (isNeighborSalty) saltyNeighbors++;
                totalValidNeighbors++;
            }
            if (totalValidNeighbors == 0)
            {
                RegisterFailedChunk(pos, chunk);
                return;
            }
            if (highestNeighborRating > centralChunkRating)
            {
                double reductionFactor = 0.2 + (random.NextDouble() * 0.3);
                centralChunkRating = highestNeighborRating - (highestNeighborRating * reductionFactor);
            }

            bool finalIsSalty = saltyNeighbors > (totalValidNeighbors / 2.0);

            data.SmoothedData = new AquiferData
            {
                AquiferRating = (int)Math.Clamp(centralChunkRating, 0, 100),
                IsSalty = finalIsSalty
            };

            chunk.SetModdata("aquiferData", data);
            if (totalValidNeighbors < neighborPositions.Count)
            {
                RegisterFailedChunk(pos, chunk);
            }
        }

        private IWorldChunk GetChunkAt(ChunkPos3D pos)
        {
            if (loadedChunks.TryGetValue(pos, out var chunk))
            {
                return chunk;
            }
            var column = serverAPI.World.BlockAccessor.GetChunk(pos.X, 0, pos.Z);
            if (column == null || column.Disposed) return null;
            return null;
        }
        public AquiferData GetAquiferData(ChunkPos3D pos)
        {
            if (!isEnabled) return null;
            var chunk = GetChunkAt(pos);
            if (chunk == null) return null;
            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null) return null;
            return data.SmoothedData;
        }

        public void ClearAquiferData()
        {
            if (!isEnabled) return;
            foreach (var kvp in loadedChunks)
            {
                var chunk = kvp.Value;
                if (chunk != null) chunk.RemoveModdata("aquiferData");
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
            [ProtoMember(3)] public int Version { get; set; }
        }

        public class LocalCounts
        {
            public int NormalCount { get; set; }
            public int SaltCount { get; set; }
            public int BoilingCount { get; set; }
        }

        public void RegisterWellspring(BlockPos pos, double depthFactor)
        {
            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            var c = new ChunkPos3D(chunkX, chunkY, chunkZ);
            var info = new WellspringInfo { Position = pos, DepthFactor = 1 };
            wellspringRegistry.AddOrUpdate(c, _ => new List<WellspringInfo> { info }, (_, list) =>
            {
                lock (list) list.Add(info);
                return list;
            });
        }

        public void UnregisterWellspring(BlockPos pos)
        {
            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            var c = new ChunkPos3D(chunkX, chunkY, chunkZ);
            if (wellspringRegistry.TryGetValue(c, out var list))
            {
                lock (list) list.RemoveAll(ws => ws.Position.Equals(pos));
            }
        }

        public List<WellspringInfo> GetWellspringsInChunk(ChunkPos3D pos)
        {
            if (wellspringRegistry.TryGetValue(pos, out var list))
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