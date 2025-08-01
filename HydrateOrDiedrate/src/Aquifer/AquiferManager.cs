using HydrateOrDiedrate.Config;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

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
        public static AquiferManager Instance { get; private set; }
        private readonly WaterKind[] kindById;
        public enum WaterKind : byte { None, Normal, Ice, Salt, Boiling }
        public const string WaterCountsKey = "game:waterCounts";
        private readonly ICoreServerAPI serverAPI;
        private const int CurrentAquiferDataVersion = 2;
        private bool isEnabled;
        private bool runGamePhaseReached;
        private bool isInitialized;
        private static readonly ChunkPos3D[] NeighborOffsets = GenerateNeighborOffsets();
        private const string NeedsSmoothingKey = "aqNeedsSmoothing";
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
            var allBlocks = serverAPI.World.Blocks;
            var maxId  = allBlocks.Max(b => b.BlockId);
            kindById   = new WaterKind[maxId + 1];

            foreach (var b in allBlocks)
            {
                if (b.Code.Domain != "game") continue;
                var p = b.Code.Path;

                WaterKind kind =
                    (p == "water"      || p.StartsWithFast("water-"))       ? WaterKind.Normal  :
                    (p == "lakeice"    || p.StartsWithFast("lakeice"))      ? WaterKind.Ice     :
                    (p == "saltwater"  || p.StartsWithFast("saltwater-"))   ? WaterKind.Salt    :
                    (p.StartsWithFast("boilingwater"))                      ? WaterKind.Boiling :
                    WaterKind.None;

                if (kind != WaterKind.None) kindById[b.BlockId] = kind;
            }
            serverAPI.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            serverAPI.Event.ChunkColumnLoaded += OnChunkColumnLoaded;
            serverAPI.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGamePhaseEntered);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WaterKind GetWaterKind(int id) =>
            (uint)id < (uint)kindById.Length ? kindById[id] : WaterKind.None;
        private void OnRunGamePhaseEntered() => runGamePhaseReached = true;

        private void OnPlayerNowPlaying(IPlayer player)
        {
            if (isInitialized) return;
            isInitialized = true;
        }

        private void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!isEnabled) return;

            for (int y = 0; y < chunks.Length; y++)
            {
                var chunk = chunks[y];
                if (chunk == null) continue;

                var pos3D = new ChunkPos3D(chunkCoord.X, y, chunkCoord.Y);
                
                Task.Run(() =>
                {
                    if (chunk.GetModdata<byte?>(NeedsSmoothingKey, null) == 1)
                    {
                        if (NeighborOffsets.All(o =>
                                GetChunkAt(new ChunkPos3D(pos3D.X + o.X, pos3D.Y + o.Y, pos3D.Z + o.Z)) != null
                            ))
                        {
                            ReSmoothChunk(pos3D, chunk);
                        }
                    }
                    else
                    {
                        ProcessChunk(pos3D, chunk);
                    }
                });
            }
        }

        private void ProcessChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            if (chunk == null || !IsChunkValid(chunk)) return;

            chunk.Unpack();

            AquiferChunkData data = null;
            try
            {
                data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Warning($"AquiferManager: could not deserialize aquiferData for chunk {pos}: {ex}");
                chunk.RemoveModdata("aquiferData");
            }
            if (data == null || data.Version < CurrentAquiferDataVersion)
            {
                AquiferData result = CalculateAquiferData(chunk, pos);

                data = new AquiferChunkData
                {
                    Data    = result,
                    Version = CurrentAquiferDataVersion
                };

                chunk.SetModdata("aquiferData", data);
            }
        }
        private void ReSmoothChunk(ChunkPos3D pos, IWorldChunk chunk)
        {
            AquiferData existing = null;
            try
            {
                existing = chunk.GetModdata<AquiferChunkData>("aquiferData", null)?.Data;
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Warning(
                    $"AquiferManager: could not deserialize aquiferData while smoothing chunk {pos}: {ex}");
                chunk.RemoveModdata("aquiferData");
            }
            if (existing == null)
            {
                ProcessChunk(pos, chunk);
                return;
            }

            double tot  = existing.AquiferRating;
            double totW = 1.0;

            foreach (var off in NeighborOffsets)
            {
                var npos  = new ChunkPos3D(pos.X + off.X, pos.Y + off.Y, pos.Z + off.Z);
                var ndata = GetAquiferData(npos);
                if (ndata == null) return;

                double w = WeightForOffset(off);
                tot  += ndata.AquiferRating * w;
                totW += w;
            }

            existing.AquiferRating = (int)Math.Clamp(tot / totW, 0, 100);

            chunk.SetModdata("aquiferData", new AquiferChunkData {
                Data    = existing,
                Version = CurrentAquiferDataVersion
            });
            chunk.RemoveModdata(NeedsSmoothingKey);
        }

        public AquiferData CalculateAquiferData(IWorldChunk worldChunk, ChunkPos3D pos)
        {
            var config = ModConfig.Instance.GroundWater;
            long seed = serverAPI.World.Seed;
            int chunkSeed = GameMath.MurmurHash3(
                pos.X ^ (int)(seed >> 40),
                pos.Y ^ (int)(seed >> 20),
                pos.Z ^ (int)(seed)
            );
            LCGRandom rand = new LCGRandom(chunkSeed);
            double chance = rand.NextDouble();

            IMapChunk mapChunk = (worldChunk as ServerChunk)?.MapChunk as IMapChunk;
            if (mapChunk == null)
                mapChunk = serverAPI.WorldManager.GetMapChunk(pos.X, pos.Z) as IMapChunk;

            WaterCounts counts = null;
            try
            {
                counts = mapChunk?.GetModdata<WaterCounts>(WaterCountsKey, null);
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Warning($"AquiferManager: could not deserialize WaterCounts for chunk {pos}: {ex}");
                mapChunk?.RemoveModdata(WaterCountsKey);
            }
            int normalCount = counts?.NormalWaterBlockCount ?? 0;
            int saltCount = counts?.SaltWaterBlockCount ?? 0;
            int boilCount = counts?.BoilingWaterBlockCount ?? 0;

            if (counts == null &&
                worldChunk is ServerChunk scanSrv &&
                scanSrv.Data is ChunkData chunkData &&
                chunkData.fluidsLayer != null)
            {
                chunkData.fluidsLayer.readWriteLock.AcquireReadLock();
                try
                {
                    for (int i = 0; i < chunkData.Length; i++)
                    {
                        WaterKind kind = GetWaterKind(chunkData.fluidsLayer.Get(i));

                        switch (kind)
                        {
                            case WaterKind.Normal:
                            case WaterKind.Ice:
                                normalCount++;
                                break;

                            case WaterKind.Salt:
                                saltCount++;
                                break;

                            case WaterKind.Boiling:
                                boilCount++;
                                break;
                        }
                    }
                }
                finally
                {
                    chunkData.fluidsLayer.readWriteLock.ReleaseReadLock();
                }

                counts = new WaterCounts
                {
                    NormalWaterBlockCount = normalCount,
                    SaltWaterBlockCount   = saltCount,
                    BoilingWaterBlockCount= boilCount
                };

                mapChunk?.SetModdata(WaterCountsKey, counts);
            }

            int totalWaterCount = normalCount + saltCount + boilCount;
            int worldHeight = serverAPI.WorldManager.MapSizeY;
            int seaLevel = (int)Math.Round(0.4296875 * worldHeight);
            int chunkCenterY = pos.Y * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2;

            double depthMul = 1.0;
            if (chunkCenterY < seaLevel)
            {
                double normalizedDepth = (seaLevel - chunkCenterY) / (double)(seaLevel - 1);
                depthMul = 1 + normalizedDepth * config.AquiferDepthMultiplierScale;
            }

            bool randomChance = serverAPI.Side == EnumAppSide.Server &&
                                chance < config.AquiferRandomMultiplierChance * depthMul;

            if (totalWaterCount < config.AquiferMinimumWaterBlockThreshold && !randomChance)
            {
                return new AquiferData { AquiferRating = 0, IsSalty = false };
            }

            double wNormal = CalculateDiminishingReturns(normalCount, 300, 1.0, 0.99) *
                             config.AquiferWaterBlockMultiplier;
            double wSalt = CalculateDiminishingReturns(saltCount, 300, 1.0, 0.99) *
                           config.AquiferSaltWaterMultiplier;
            double wBoiling = CalculateDiminishingReturns(boilCount, 1000, 10.0, 0.50) *
                              config.AquiferBoilingWaterMultiplier;

            var blockPos = new BlockPos(
                pos.X * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2,
                pos.Y * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2,
                pos.Z * GlobalConstants.ChunkSize + GlobalConstants.ChunkSize / 2
            );

            float rainfall = serverAPI.World.BlockAccessor
                .GetClimateAt(blockPos, EnumGetClimateMode.WorldGenValues)?.Rainfall ?? 0f;

            double baseMul = 0.75 + rainfall;
            double nsWeighted = (wNormal + wSalt) * baseMul;
            double bWeighted = wBoiling * baseMul;

            int nsRating = NormalizeAquiferRating(nsWeighted);
            int bRating = NormalizeAquiferRating(bWeighted);
            int rating = Math.Clamp(nsRating + bRating, 0, 100);

            if (randomChance)
            {
                int add = 1 + rand.NextInt(10);
                rating += add;

                double rndScale = rand.NextDouble() * (10.0 - 1.1) + 1.1;
                rating = (int)(rating * rndScale);
            }
            if (chunkCenterY > seaLevel)
            {
                rating = Math.Min(rating, config.AquiferRatingCeilingAboveSeaLevel);
            }
            bool allNeighboursPresent = true;
            double tot  = rating;
            double totW = 1.0;

            foreach (var off in NeighborOffsets)
            {
                var npos  = new ChunkPos3D(pos.X + off.X, pos.Y + off.Y, pos.Z + off.Z);
                var ndata = GetAquiferData(npos);
                if (ndata == null)
                {
                    allNeighboursPresent = false;
                    continue;
                }
                double w = 1.0 / Math.Sqrt(off.X * off.X + off.Y * off.Y + off.Z * off.Z);
                tot  += ndata.AquiferRating * w;
                totW += w;
            }

            int smoothedRating = (int)Math.Clamp(tot / totW, 0, 100);
            bool salty = saltCount > normalCount * 0.5;

            var result = new AquiferData { AquiferRating = smoothedRating, IsSalty = salty };
            worldChunk.SetModdata("aquiferData",
                new AquiferChunkData { Data = result, Version = CurrentAquiferDataVersion });

            if (!allNeighboursPresent)
                worldChunk.SetModdata(NeedsSmoothingKey, (byte)1);
            else
                worldChunk.RemoveModdata(NeedsSmoothingKey);

            return result;
        }
        private static double WeightForOffset(ChunkPos3D o)
        {
            double dist = Math.Sqrt(o.X * o.X + o.Y * o.Y + o.Z * o.Z);
            return dist <= 0.0001 ? 1.0 : 1.0 / dist;
        }
        private double CalculateDiminishingReturns(int n, double S, double m, double d)
        {
            int k = (int)Math.Floor((S/m - 1)/d) + 1;
            int x = Math.Min(n, k);
            double H = 1.0;
            for(int i = 1; i <= x; i++) H += 1.0/i;
            double sum = (S/d)*(H - 1);
            if (n > k) sum += (n - k)*m;
            return sum; 
        }

        private int NormalizeAquiferRating(double weightedWaterBlocks)
        {
            double baselineMax = 3000;
            double minWeightedValue = 0;
            int worldHeight = serverAPI.WorldManager.MapSizeY;
            double maxWeightedValue = CalculateDiminishingReturns((int)(baselineMax * (worldHeight / 256.0)), 500.0, 4.0, 0.80);
            if (maxWeightedValue == minWeightedValue) return 0;
            int normalizedRating = (int)((weightedWaterBlocks - minWeightedValue) / (maxWeightedValue - minWeightedValue) * 100);
            return Math.Clamp(normalizedRating, 0, 100);
        }

        private IWorldChunk GetChunkAt(ChunkPos3D pos)
        {
            return serverAPI.World.BlockAccessor.GetChunk(pos.X, pos.Y, pos.Z);
        }

        public AquiferData GetAquiferData(ChunkPos3D pos)
        {
            IWorldChunk chunk = GetChunkAt(pos);
            if (chunk == null) return null;
            try
            {
                var data = chunk.GetModdata<AquiferChunkData>("aquiferData", null);
                return data?.Data;
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Warning($"AquiferManager: could not deserialize aquiferData for chunk {pos}: {ex}");
                chunk.RemoveModdata("aquiferData");
                return null;
            }
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
            AquiferChunkData data;
            try
            {
                data = chunk.GetModdata<AquiferChunkData>("aquiferData", null) ?? new AquiferChunkData
                    { Version = CurrentAquiferDataVersion };
            }
            catch (Exception ex)
            {
                serverAPI.Logger.Warning(
                    $"AquiferManager: could not deserialize aquiferData while setting rating for chunk {pos}: {ex}");
                data = new AquiferChunkData { Version = CurrentAquiferDataVersion };
            }
            data.Data = new AquiferData { AquiferRating = rating, IsSalty = false };
            chunk.SetModdata("aquiferData", data);
            return true;
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
        public class WaterCounts
        {
            [ProtoMember(1)] public int NormalWaterBlockCount;
            [ProtoMember(2)] public int SaltWaterBlockCount;
            [ProtoMember(3)] public int BoilingWaterBlockCount;
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
            public AquiferData Data { get; set; }
            [ProtoMember(3)]
            public int Version { get; set; }
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
