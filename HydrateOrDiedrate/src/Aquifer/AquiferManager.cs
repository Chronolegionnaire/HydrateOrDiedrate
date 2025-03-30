using System;
using System.Collections.Generic;
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
        private readonly Random random = new Random();
        private const int CurrentAquiferDataVersion = 1;
        private bool isEnabled;
        private bool runGamePhaseReached;
        private bool isInitialized;
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
            isEnabled = true;
            runGamePhaseReached = false;
            isInitialized = false;

            serverAPI.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            serverAPI.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
            serverAPI.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGamePhaseEntered);
        }

        private void OnRunGamePhaseEntered() => runGamePhaseReached = true;

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
                ProcessChunk(pos3D, chunk);
            }
        }

        private void ProcessChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            if (chunk == null || !IsChunkValid(chunk))
            {
                return;
            }

            chunk.Unpack();
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

        private Block GetBlock(int blockId)
        {
            return serverAPI.World.GetBlock(blockId);
        }

        private AquiferData CalculateAquiferData(IWorldChunk chunk, ChunkPos3D pos)
        {
            try
            {
                var config = HydrateOrDiedrateModSystem.LoadedConfig;
                int chunkSize = GlobalConstants.ChunkSize;
                int normalWaterBlockCount = chunk.GetModdata<int>("game:water", 0);
                int saltWaterBlockCount = chunk.GetModdata<int>("game:saltwater", 0);
                int boilingWaterBlockCount = chunk.GetModdata<int>("game:boilingwater", 0);
                if (normalWaterBlockCount == 0 && saltWaterBlockCount == 0 && boilingWaterBlockCount == 0)
                {
                    int step = config.AquiferStep;
                    int totalBlocks = chunk.Data.Length;
                    int iterY = chunkSize;
                    int iterX = (int)Math.Ceiling((double)chunkSize / step);
                    int iterZ = (int)Math.Ceiling((double)chunkSize / step);
                    int totalIterations = iterY * iterX * iterZ;

                    for (int i = 0; i < totalIterations; i++)
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

                        Block block = GetBlock(blockId);
                        if (block?.Code?.Path == null) continue;

                        if (block.Code.Path.Contains("boilingwater"))
                        {
                            boilingWaterBlockCount++;
                        }
                        else if (block.Code.Path.Contains("water"))
                        {
                            normalWaterBlockCount++;
                            if (block.Code.Path.Contains("saltwater"))
                            {
                                saltWaterBlockCount++;
                            }
                        }
                    }
                    chunk.SetModdata("game:water", normalWaterBlockCount);
                    chunk.SetModdata("game:saltwater", saltWaterBlockCount);
                    chunk.SetModdata("game:boilingwater", boilingWaterBlockCount);
                }

                double waterBlockMultiplier = config.AquiferWaterBlockMultiplier;
                double saltWaterMultiplier = config.AquiferSaltWaterMultiplier;
                int boilingWaterMultiplier = config.AquiferBoilingWaterMultiplier;
                double randomMultiplierChance = config.AquiferRandomMultiplierChance;
                int worldHeight = serverAPI.WorldManager.MapSizeY;
                int seaLevel = (int)Math.Round(0.4296875 * worldHeight);
                int chunkCenterY = (pos.Y * GlobalConstants.ChunkSize) + (GlobalConstants.ChunkSize / 2);

                double weightedNormal = CalculateDiminishingReturns(normalWaterBlockCount, 300.0, 1.0, 0.99) * waterBlockMultiplier;
                double weightedSalt = CalculateDiminishingReturns(saltWaterBlockCount, 300.0, 1.0, 0.99) * saltWaterMultiplier;
                double weightedBoiling = CalculateDiminishingReturns(boilingWaterBlockCount, 1000.0, 10.0, 0.5) * boilingWaterMultiplier;
                var climate = serverAPI.World.BlockAccessor.GetClimateAt(
                    new BlockPos(pos.X * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2, pos.Y * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2, pos.Z * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2),
                    EnumGetClimateMode.WorldGenValues);
                float rainfall = climate?.Rainfall ?? 0f;

                double totalWeighted = (weightedNormal + weightedSalt + weightedBoiling) * (0.75 + rainfall);
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
                if (random.NextDouble() < randomMultiplierChance * depthMultiplier)
                {
                    int baseAdd = random.Next(1, 11);
                    rating += baseAdd;
                    double rndMul = random.NextDouble() * (10.0 - 1.1) + 1.1;
                    rating = (int)(rating * rndMul);
                    rating = Math.Clamp(rating, 0, 100);
                }

                bool salty = saltWaterBlockCount > normalWaterBlockCount * 0.5;
                return new AquiferData { AquiferRating = rating, IsSalty = salty };
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Error("CalculateAquiferData exception: " + ex);
                return new AquiferData { AquiferRating = 0, IsSalty = false };
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
        private bool ShouldSimulateEmptyChunk(ChunkPos3D pos)
        {
            int chunkSize = GlobalConstants.ChunkSize;
            if (pos.Y < 0) return true;
            int sampleX = pos.X * chunkSize + chunkSize / 2;
            int sampleZ = pos.Z * chunkSize + chunkSize / 2;
            BlockPos samplePos = new BlockPos(sampleX, 0, sampleZ);
            int terrainHeight = serverAPI.World.BlockAccessor.GetTerrainMapheightAt(samplePos);
            int terrainChunkY = terrainHeight / chunkSize;
            return (terrainChunkY < pos.Y);
        }
        private void SmoothWithNeighbors(IWorldChunk chunk, ChunkPos3D pos)
        {
            if (!isEnabled) return;

            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            if (data == null)
            {
                return;
            }

            double centralChunkRating = data.PreSmoothedData.AquiferRating;
            int saltyNeighbors = 0;
            int totalValidNeighbors = 0;
            int highestNeighborRating = 0;

            foreach (var offset in NeighborOffsets)
            {
                var npos = new ChunkPos3D(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);
                AquiferData neighborAq = null;
                IWorldChunk neighborChunk = GetChunkAt(npos);
                if (neighborChunk != null && IsChunkValid(neighborChunk))
                {
                    var neighborData = neighborChunk.GetModdata<AquiferChunkData>("aquiferData", null);
                    if (neighborData != null)
                    {
                        neighborAq = neighborData.SmoothedData ?? neighborData.PreSmoothedData;
                    }
                }
                else if (ShouldSimulateEmptyChunk(npos))
                {
                    neighborAq = new AquiferData { AquiferRating = 0, IsSalty = false };
                }

                if (neighborAq != null)
                {
                    if (neighborAq.IsSalty)
                        saltyNeighbors++;
                    totalValidNeighbors++;
                    highestNeighborRating = Math.Max(highestNeighborRating, neighborAq.AquiferRating);
                }
            }

            data.ProcessedNeighborCount = totalValidNeighbors;
            data.NeedsReprocess = totalValidNeighbors < 26;

            if (totalValidNeighbors == 0)
            {
                return;
            }

            if (highestNeighborRating > centralChunkRating)
            {
                double reductionFactor = 0.2 + (random.NextDouble() * 0.3);
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
            if (data.NeedsReprocess)
            {
                TriggerNeighborReprocess(pos);
            }
        }

        private IWorldChunk GetChunkAt(ChunkPos3D pos)
        {
            return serverAPI.World.BlockAccessor.GetChunk(pos.X, pos.Y, pos.Z);
        }

        private void TriggerNeighborReprocess(ChunkPos3D pos)
        {
            int reprocessedCount = 0;
            foreach (var offset in NeighborOffsets)
            {
                var neighborPos = new ChunkPos3D(pos.X + offset.X, pos.Y + offset.Y, pos.Z + offset.Z);
                IWorldChunk neighborChunk = GetChunkAt(neighborPos);
                if (neighborChunk != null && IsChunkValid(neighborChunk))
                {
                    ProcessChunk(neighborPos, neighborChunk);
                    reprocessedCount++;
                }
                else if (ShouldSimulateEmptyChunk(neighborPos))
                {
                    reprocessedCount++;
                }
            }
        }

        public AquiferData GetAquiferData(ChunkPos3D pos)
        {
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk == null) return null;
            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            return data?.SmoothedData;
        }
        public void ClearAquiferData(ChunkPos3D pos)
        {
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk != null)
            {
                chunk.RemoveModdata("aquiferData");
            }
        }
        
        public bool SetAquiferRating(ChunkPos3D pos, int rating)
        {
            if (rating < 0 || rating > 100) return false;
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk == null) return false;
            var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null) ?? new AquiferChunkData { Version = CurrentAquiferDataVersion };
            data.PreSmoothedData = new AquiferData { AquiferRating = rating, IsSalty = false };
            data.SmoothedData = new AquiferData { AquiferRating = rating, IsSalty = false };
            chunk.SetModdata("aquiferData", data);
            return true;
        }

        public void RegisterWellspring(BlockPos pos)
        {
            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            IWorldChunk chunk = serverAPI.World.BlockAccessor.GetChunk(chunkX, chunkY, chunkZ);
            if (chunk == null || chunk.Disposed) return;

            var wellsData = chunk.GetModdata<WellspringData>("wellspringData", null) 
                              ?? new WellspringData { Wellsprings = new List<WellspringInfo>() };
            wellsData.Wellsprings.Add(new WellspringInfo { Position = pos });
            chunk.SetModdata("wellspringData", wellsData);
        }

        public void UnregisterWellspring(BlockPos pos)
        {
            int chunkX = pos.X / GlobalConstants.ChunkSize;
            int chunkY = pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = pos.Z / GlobalConstants.ChunkSize;
            IWorldChunk chunk = serverAPI.World.BlockAccessor.GetChunk(chunkX, chunkY, chunkZ);
            if (chunk == null || chunk.Disposed) return;

            var wellsData = chunk.GetModdata<WellspringData>("wellspringData", null);
            if (wellsData == null) return;
            wellsData.Wellsprings.RemoveAll(ws => ws.Position.Equals(pos));
            chunk.SetModdata("wellspringData", wellsData);
        }

        public List<WellspringInfo> GetWellspringsInChunk(ChunkPos3D pos)
        {
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk == null) return new List<WellspringInfo>();

            var wellsData = chunk.GetModdata<WellspringData>("wellspringData", null);
            if (wellsData == null || wellsData.Wellsprings == null)
            {
                return new List<WellspringInfo>();
            }
            return new List<WellspringInfo>(wellsData.Wellsprings);
        }

        public void AddWellspringToChunk(ChunkPos3D pos, BlockPos blockPos)
        {
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk == null) return;

            var wellsData = chunk.GetModdata<WellspringData>("wellspringData", null);
            if (wellsData == null)
            {
                wellsData = new WellspringData { Wellsprings = new List<WellspringInfo>() };
            }
            else if (wellsData.Wellsprings == null)
            {
                wellsData.Wellsprings = new List<WellspringInfo>();
            }
            if (!wellsData.Wellsprings.Any(ws => ws.Position.Equals(blockPos)))
            {
                wellsData.Wellsprings.Add(new WellspringInfo { Position = blockPos });
                chunk.SetModdata("wellspringData", wellsData);
            }
        }


        private bool IsChunkValid(IWorldChunk chunk)
        {
            return chunk != null && !chunk.Disposed && chunk.Data != null && chunk.Data.Length > 0;
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
            [ProtoMember(3)]
            public int Version { get; set; }
            [ProtoMember(4)]
            public int ProcessedNeighborCount { get; set; }
            [ProtoMember(5)]
            public bool NeedsReprocess { get; set; }
        }

        [ProtoContract]
        public class WellspringData
        {
            [ProtoMember(1)]
            public List<WellspringInfo> Wellsprings { get; set; }
        }

        [ProtoContract]
        public class WellspringInfo
        {
            [ProtoMember(1)]
            public BlockPos Position { get; set; }
        }
    }
}
