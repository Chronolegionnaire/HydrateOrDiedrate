using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate
{
    public struct ChunkPos3D : IEquatable<ChunkPos3D>
    {
        public int X, Y, Z;
        public ChunkPos3D(int x, int y, int z) => (X, Y, Z) = (x, y, z);
        public bool Equals(ChunkPos3D other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is ChunkPos3D other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    }

    public class AquiferManager
    {
        private readonly ICoreServerAPI serverAPI;

        private readonly Channel<(ChunkPos3D pos, IWorldChunk chunk)> calculationChannel;
        private readonly ConcurrentDictionary<ChunkPos3D, ChunkStatus> chunkStatuses;
        private class FailedChunkInfo
        {
            public int Attempts;
            public IWorldChunk Chunk;
            public DateTime NextRetryTime;
        }
        private class ChunkStatus
        {
            public IWorldChunk Chunk { get; set; }
            public FailedChunkInfo FailedInfo { get; set; }
            public bool IsUnpacked { get; set; }
        }

        private readonly ConcurrentDictionary<ChunkPos3D, List<WellspringInfo>> wellspringRegistry;
        private readonly ConcurrentDictionary<int, Block> blockCache = new();
        private readonly ConcurrentDictionary<ChunkPos3D, float> climateCache = new();
        private readonly ThreadLocal<Random> threadLocalRandom = new(() => new Random());
        private const int CurrentAquiferDataVersion = 1;
        private bool isEnabled;
        private bool runGamePhaseReached;
        private bool isInitialized;
        private const int MaxFailedAttempts = 10;
        private readonly int TimerIntervalMs = 5000;
        private static readonly ChunkPos3D[] NeighborOffsets = GenerateNeighborOffsets();
        private static ChunkPos3D[] GenerateNeighborOffsets()
        {
            var list = new List<ChunkPos3D>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        list.Add(new ChunkPos3D(dx, dy, dz));
                    }
                }
            }
            return list.ToArray();
        }

        public AquiferManager(ICoreServerAPI api)
        {
            serverAPI = api;
            calculationChannel = Channel.CreateBounded<(ChunkPos3D, IWorldChunk)>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });
            chunkStatuses = new ConcurrentDictionary<ChunkPos3D, ChunkStatus>();
            wellspringRegistry = new ConcurrentDictionary<ChunkPos3D, List<WellspringInfo>>();
            isEnabled = true;
            runGamePhaseReached = false;
            isInitialized = false;

            serverAPI.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            serverAPI.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
            serverAPI.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGamePhaseEntered);
            SetupMergedTimer();
            StartBackgroundProcessor();
        }

        private void OnRunGamePhaseEntered()
        {
            runGamePhaseReached = true;
        }

        private void OnPlayerNowPlaying(IPlayer player)
        {
            if (isInitialized) return;
            isInitialized = true;
        }

        private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!isEnabled) return;
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                if (chunk == null) continue;
                var pos3D = new ChunkPos3D(chunkCoord.X, i, chunkCoord.Y);
                var status = chunkStatuses.GetOrAdd(pos3D, _ => new ChunkStatus());
                status.Chunk = chunk;
                EnqueueChunkProcessing(pos3D, chunk);
            }
            AttemptReprocessingOfFailedChunks();
        }

        private void StartBackgroundProcessor()
        {
            Task.Factory.StartNew(async () =>
            {
                await foreach (var (pos, chunk) in calculationChannel.Reader.ReadAllAsync())
                {
                    if (IsChunkValid(chunk))
                    {
                        await ProcessChunk(pos, chunk);
                    }
                    else
                    {
                        RegisterFailedChunk(pos, chunk);
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void EnqueueChunkProcessing(ChunkPos3D pos, IWorldChunk chunk)
        {
            calculationChannel.Writer.TryWrite((pos, chunk));
        }

        private void SetupMergedTimer()
        {
            serverAPI.Event.RegisterGameTickListener(dt =>
            {
                RetryFailedChunks();
            }, TimerIntervalMs);
        }

        private void RetryFailedChunks()
        {
            DateTime now = DateTime.UtcNow;
            foreach (var kvp in chunkStatuses)
            {
                var pos = kvp.Key;
                var status = kvp.Value;
                if (status.FailedInfo == null)
                    continue;
                if (status.FailedInfo.Attempts >= MaxFailedAttempts)
                {
                    status.FailedInfo = null;
                    continue;
                }
                if (now >= status.FailedInfo.NextRetryTime && IsChunkValid(status.FailedInfo.Chunk))
                {
                    EnqueueChunkProcessing(pos, status.FailedInfo.Chunk);
                    status.FailedInfo = null;
                }
            }
        }

        private void RegisterFailedChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            var status = chunkStatuses.GetOrAdd(pos, _ => new ChunkStatus());
            if (status.FailedInfo == null)
            {
                status.FailedInfo = new FailedChunkInfo
                {
                    Attempts = 1,
                    Chunk = chunk,
                    NextRetryTime = DateTime.UtcNow.AddMilliseconds(TimerIntervalMs)
                };
            }
            else
            {
                status.FailedInfo.Attempts++;
                status.FailedInfo.NextRetryTime = DateTime.UtcNow.AddMilliseconds(TimerIntervalMs * Math.Pow(2, status.FailedInfo.Attempts));
                status.FailedInfo.Chunk = chunk;
            }
        }

        private void AttemptReprocessingOfFailedChunks()
        {
            foreach (var kvp in chunkStatuses)
            {
                var pos = kvp.Key;
                var status = kvp.Value;
                if (status.FailedInfo != null && IsChunkValid(status.FailedInfo.Chunk))
                {
                    EnqueueChunkProcessing(pos, status.FailedInfo.Chunk);
                    status.FailedInfo = null;
                }
            }
        }

        private bool IsChunkValid(IWorldChunk chunk)
        {
            return chunk != null && !chunk.Disposed && chunk.Data != null && chunk.Data.Length > 0;
        }

        private Block GetCachedBlock(int blockId)
            => blockCache.GetOrAdd(blockId, id => serverAPI.World.GetBlock(id));
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
        
        private async Task ProcessChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            if (chunk == null || !IsChunkValid(chunk)) return;

            var status = chunkStatuses.GetOrAdd(pos, _ => new ChunkStatus { Chunk = chunk });
            if (!status.IsUnpacked)
            {
                chunk.Unpack();
                status.IsUnpacked = true;
            }

            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null || data.Version < CurrentAquiferDataVersion)
            {
                var pre = CalculateAquiferData(chunk, pos);
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
                SmoothWithNeighbors(chunk, pos);
            }
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

                int worldHeight = serverAPI.WorldManager.MapSizeY;
                int seaLevel = (int)Math.Round(0.4296875 * worldHeight);
                int chunkCenterY = (pos.Y * chunkSize) + (chunkSize / 2);

                int totalBlocks = chunk.Data.Length;

                int iterY = chunkSize;
                int iterX = (int)Math.Ceiling((double)chunkSize / step);
                int iterZ = (int)Math.Ceiling((double)chunkSize / step);
                int totalIterations = iterY * iterX * iterZ;

                Parallel.ForEach(Partitioner.Create(0, totalIterations), range =>
                {
                    var localCounts = new LocalCounts();
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        int y = i / (iterX * iterZ);
                        int rem = i % (iterX * iterZ);
                        int xIndex = rem / iterZ;
                        int zIndex = rem % iterZ;
                        int x = xIndex * step;
                        int z = zIndex * step;

                        int idx = (y * chunkSize + z) * chunkSize + x;
                        if (idx < 0 || idx >= totalBlocks) continue;
                        int blockId = chunk.Data[idx];
                        if (blockId < 0) continue;

                        Block block = GetCachedBlock(blockId);
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
                    Interlocked.Add(ref normalWaterBlockCount, localCounts.NormalCount);
                    Interlocked.Add(ref saltWaterBlockCount, localCounts.SaltCount);
                    Interlocked.Add(ref boilingWaterBlockCount, localCounts.BoilingCount);
                });

                double weightedNormal = CalculateDiminishingReturns(normalWaterBlockCount, 300.0, 1.0, 0.99) * waterBlockMultiplier;
                double weightedSalt = CalculateDiminishingReturns(saltWaterBlockCount, 300.0, 1.0, 0.99) * saltWaterMultiplier;
                double weightedBoiling = CalculateDiminishingReturns(boilingWaterBlockCount, 1000.0, 10.0, 0.5) * boilingWaterMultiplier;
                double totalWeighted = (weightedNormal + weightedSalt + weightedBoiling) *
                    (0.75 + (GetCachedClimate(pos, chunkSize)?.Rainfall ?? 0f));

                int rating = NormalizeAquiferRating(totalWeighted);
                if (chunkCenterY > seaLevel)
                {
                    rating = Math.Min(rating, config.AquiferRatingCeilingAboveSeaLevel);
                }
                double depthMultiplier = 1.0;
                if (chunkCenterY < seaLevel)
                {
                    double normalizedDepth = (seaLevel - chunkCenterY) / (double)(seaLevel - 1);
                    depthMultiplier = 1 + normalizedDepth * config.AquiferDepthMultiplierScale;
                }
                var random = threadLocalRandom.Value;
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

        private ClimateData GetCachedClimate(ChunkPos3D pos, int chunkSize)
        {
            var center = new BlockPos(
                pos.X * chunkSize + chunkSize / 2,
                pos.Y * chunkSize + chunkSize / 2,
                pos.Z * chunkSize + chunkSize / 2
            );
            if (climateCache.TryGetValue(pos, out float rainfall))
            {
                return new ClimateData { Rainfall = rainfall };
            }
            else
            {
                var climate = serverAPI.World.BlockAccessor.GetClimateAt(center, EnumGetClimateMode.WorldGenValues);
                float rain = climate?.Rainfall ?? 0f;
                climateCache[pos] = rain;
                return new ClimateData { Rainfall = rain };
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
            int saltyNeighbors = 0;
            int totalValidNeighbors = 0;
            int highestNeighborRating = 0;
            foreach (var offset in NeighborOffsets)
            {
                var npos = new ChunkPos3D(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);
                var neighborChunk = GetChunkAt(npos);
                if (neighborChunk == null || !IsChunkValid(neighborChunk))
                {
                    continue;
                }
                var neighborStatus = chunkStatuses.GetOrAdd(npos, _ => new ChunkStatus());
                if (!neighborStatus.IsUnpacked)
                {
                    neighborChunk.Unpack();
                    neighborStatus.IsUnpacked = true;
                }
                AquiferChunkData neighborAqData = null;
                try
                {
                    neighborAqData = neighborChunk.GetModdata<AquiferChunkData>("aquiferData", null);
                }
                catch
                {
                    continue;
                }
                if (neighborAqData == null) continue;

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

            var rnd = threadLocalRandom.Value;
            if (highestNeighborRating > centralChunkRating)
            {
                double reductionFactor = 0.2 + (rnd.NextDouble() * 0.3);
                centralChunkRating = highestNeighborRating - (highestNeighborRating * reductionFactor);
            }

            bool finalIsSalty = saltyNeighbors > (totalValidNeighbors / 2.0);
            var config = HydrateOrDiedrateModSystem.LoadedConfig;
            int chunkSize = GlobalConstants.ChunkSize;
            BlockPos center = new BlockPos(
                pos.X * chunkSize + chunkSize / 2,
                pos.Y * chunkSize + chunkSize / 2,
                pos.Z * chunkSize + chunkSize / 2
            );
            int worldHeight = serverAPI.WorldManager.MapSizeY;
            int seaLevel = (int)Math.Round(0.4296875 * worldHeight);
            if (center.Y > seaLevel)
            {
                centralChunkRating = Math.Min(centralChunkRating, config.AquiferRatingCeilingAboveSeaLevel);
            }

            data.SmoothedData = new AquiferData
            {
                AquiferRating = (int)Math.Clamp(centralChunkRating, 0, 100),
                IsSalty = finalIsSalty
            };

            chunk.SetModdata("aquiferData", data);
            if (totalValidNeighbors < NeighborOffsets.Length)
            {
                RegisterFailedChunk(pos, chunk);
            }
        }

        private IWorldChunk GetChunkAt(ChunkPos3D pos)
        {
            if (chunkStatuses.TryGetValue(pos, out var status))
            {
                return status.Chunk;
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
            foreach (var kvp in chunkStatuses)
            {
                var chunk = kvp.Value.Chunk;
                if (chunk != null) chunk.RemoveModdata("aquiferData");
            }
        }
        
        public bool SetAquiferRating(ChunkPos3D pos, int rating)
        {
            if (rating < 0 || rating > 100)
            {
                return false;
            }
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk == null)
            {
                return false;
            }
            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null)
            {
                data = new AquiferChunkData
                {
                    PreSmoothedData = new AquiferData { AquiferRating = rating, IsSalty = false },
                    SmoothedData = new AquiferData { AquiferRating = rating, IsSalty = false },
                    Version = CurrentAquiferDataVersion
                };
            }
            else
            {
                if (data.SmoothedData == null)
                {
                    data.SmoothedData = new AquiferData();
                }
                data.SmoothedData.AquiferRating = rating;

                if (data.PreSmoothedData == null)
                {
                    data.PreSmoothedData = new AquiferData();
                }
                data.PreSmoothedData.AquiferRating = rating;
            }
            chunk.SetModdata("aquiferData", data);
            return true;
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
            var info = new WellspringInfo { Position = pos, DepthFactor = depthFactor };
            wellspringRegistry.AddOrUpdate(c, _ => new List<WellspringInfo> { info }, (_, list) =>
            {
                lock (list)
                {
                    list.Add(info);
                }
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
        private class ClimateData
        {
            public float Rainfall { get; set; }
        }
    }
}
