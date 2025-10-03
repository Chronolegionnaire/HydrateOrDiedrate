using HydrateOrDiedrate.Wells.Aquifer.ModData;
using HydrateOrDiedrate.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Wells.Aquifer;

public static partial class AquiferManager
{
    public static readonly int CurrentAquiferDataVersion = 2;
    
    public const string WaterCountsModDataKey = "game:waterCounts";
    public const string AquiferModDataKey = "aquiferData";
    public const string NeedsSmoothingModDataKey = "aqNeedsSmoothing";
    public const string WellspringModDataKey = "wellspringData";

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
    private static WellspringData GetOrCreateWellspringData(IWorldChunk chunk)
    {
        if (chunk is null || chunk.Disposed) return null;

        if (!chunk.LiveModData.TryGetValue(WellspringModDataKey, out var obj) || obj is not WellspringData data)
        {
            data = new WellspringData();
            chunk.LiveModData[WellspringModDataKey] = data;
        }
        return data;
    }

    private static WellspringData TryGetWellspringData(IWorldChunk chunk)
    {
        if (chunk is null || chunk.Disposed) return null;
        return chunk.LiveModData.TryGetValue(WellspringModDataKey, out var obj) && obj is WellspringData data
            ? data
            : null;
    }
    internal static void Unload()
    {
        WaterKindById = null;
    }

    public static void ClearAquiferData(IWorldAccessor world, FastVec3i pos) =>  world.BlockAccessor.GetChunk(pos.X, pos.Y, pos.Z)?.RemoveModdata(AquiferModDataKey);

    public static EWaterKind GetWaterKind(Block block)
    {
        if(WaterKindById is not null && (uint)block.Id < (uint)WaterKindById.Length) return WaterKindById[block.Id];
        return block.Code?.Path switch
        {
            string path when path == "water" || path.StartsWithFast("water-") => EWaterKind.Normal,
            string path when path == "lakeice" || path.StartsWithFast("lakeice") => EWaterKind.Ice,
            string path when path == "saltwater" || path.StartsWithFast("saltwater-") => EWaterKind.Salt,
            string path when path.StartsWithFast("boilingwater") => EWaterKind.Boiling,
            _ => EWaterKind.None
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EWaterKind GetWaterKindById(int blockId) => (uint)blockId < (uint)WaterKindById.Length ? WaterKindById[blockId] : EWaterKind.None;

    public static AquiferChunkData GetAquiferChunkData(IWorldAccessor world, FastVec3i chunkPos, ILogger logger = null)
        => GetAquiferChunkData(world.BlockAccessor.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z), logger);

    public static AquiferChunkData GetAquiferChunkData(IWorldAccessor world, BlockPos blockPos, ILogger logger = null)
        => GetAquiferChunkData(world.BlockAccessor.GetChunkAtBlockPos(blockPos), logger);

    public static AquiferChunkData GetAquiferChunkData(IWorldChunk chunk, ILogger logger = null)
    {
        if(chunk is null) return null;
        try
        {
            if(!chunk.LiveModData.TryGetValue(AquiferModDataKey, out var liveData) || liveData is not AquiferChunkData result)
            {
                result = chunk.GetModdata<AquiferChunkData>(AquiferModDataKey);
                if(result is not null)
                {
                    chunk.LiveModData[AquiferModDataKey] = result;
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            logger?.Warning("AquiferManager: failed to deserialize {type}: {exception}", nameof(AquiferChunkData), ex);
            chunk.RemoveModdata(AquiferModDataKey);

            return null;
        }
    }

    public static WaterCounts GetWaterCounts(IMapChunk chunk, ILogger logger = null)
    {
        if(chunk is null) return null;
        try
        {
            return chunk.GetModdata<WaterCounts>(WaterCountsModDataKey, null);
        }
        catch (Exception ex)
        {
            logger?.Warning("AquiferManager: failed to deserialize {type} data: {exception}", nameof(WaterCounts), ex);
            chunk.RemoveModdata(WaterCountsModDataKey);

            return null;
        }
    }

    public static bool SetAquiferRating(IWorldAccessor world, BlockPos blockPos, int rating)
    {
        if (world.Side != EnumAppSide.Server && rating < 0 || rating > 100) return false;
        
        IWorldChunk chunk = world.BlockAccessor.GetChunkAtBlockPos(blockPos);
        if (chunk is null) return false;

        var chunkData = GetAquiferChunkData(chunk) ?? new AquiferChunkData
        {
            Version = CurrentAquiferDataVersion
        };

        chunkData.Data = new AquiferData
        {
            AquiferRatingRaw = chunkData.Data?.AquiferRatingRaw ?? rating,
            AquiferRatingSmoothed = rating,
            IsSalty = false
        };

        chunk.LiveModData[AquiferModDataKey] = chunkData;
        chunk.SetModdata(AquiferModDataKey, chunkData);
        chunk.MarkModified();
        return true;
    }

    public static string GetAquiferDirectionHint(IWorldAccessor world, BlockPos blockPos) => GetAquiferDirectionHint(world, new FastVec3i(blockPos.X / GlobalConstants.ChunkSize, blockPos.Y / GlobalConstants.ChunkSize, blockPos.Z / GlobalConstants.ChunkSize));
    public static string GetAquiferDirectionHint(IWorldAccessor world, FastVec3i chunkPos)
    {
        var centerData = GetAquiferChunkData(world, chunkPos);
        int currentRating = centerData.Data.AquiferRating;

        int radius = ModConfig.Instance.GroundWater.ProspectingRadius;
        int bestRating = currentRating;
        FastVec3i bestChunk = chunkPos.Clone();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    AquiferData checkAquiferData = GetAquiferChunkData(world, new FastVec3i(chunkPos.X + dx, chunkPos.Y + dy, chunkPos.Z + dz), world.Logger)?.Data;
                    if (checkAquiferData is null || checkAquiferData.AquiferRating <= bestRating) continue;

                    bestRating = checkAquiferData.AquiferRating;
                    bestChunk = new FastVec3i(chunkPos.X + dx, chunkPos.Y + dy, chunkPos.Z + dz);
                }
            }
        }

        if (bestRating > currentRating)
        {
            int dxDir = bestChunk.X - chunkPos.X;
            int dyDir = bestChunk.Y - chunkPos.Y;
            int dzDir = bestChunk.Z - chunkPos.Z;
            string directionHint = GetDirectionHint(dxDir, dyDir, dzDir);
            if(string.IsNullOrEmpty(directionHint)) return Lang.Get("hydrateordiedrate:aquifer-direction-here");
            
            return Lang.Get("hydrateordiedrate:aquifer-direction", directionHint.ToLower());
        }
        return string.Empty;
    }

    private static string GetDirectionHint(int dx, int dy, int dz)
    {
        string horizontal = string.Empty;
        string verticalHor = string.Empty;

        if (dz < 0) verticalHor = Lang.Get("game:facing-north");
        else if (dz > 0) verticalHor = Lang.Get("game:facing-south");

        if (dx > 0) horizontal = Lang.Get("game:facing-east");
        else if (dx < 0) horizontal = Lang.Get("game:facing-west");

        string horizontalPart;
        if (!string.IsNullOrEmpty(verticalHor) && !string.IsNullOrEmpty(horizontal))
        {
            horizontalPart = $"{verticalHor}-{horizontal}";
        }
        else horizontalPart = !string.IsNullOrEmpty(verticalHor) ? verticalHor : horizontal;

        string verticalDepth = string.Empty;
        if (dy > 0) verticalDepth = Lang.Get("hydrateordiedrate:direction-above");
        else if (dy < 0) verticalDepth = Lang.Get("hydrateordiedrate:direction-below");
        
        if (!string.IsNullOrEmpty(horizontalPart) && !string.IsNullOrEmpty(verticalDepth)) return $"{horizontalPart} {Lang.Get("hydrateordiedrate:direction-and")} {verticalDepth}";
        else if (!string.IsNullOrEmpty(horizontalPart)) return horizontalPart;
        else if (!string.IsNullOrEmpty(verticalDepth)) return verticalDepth;
        
        return string.Empty;
    }

    public static List<WellspringInfo> GetWellspringsInChunk(IWorldChunk chunk)
    {
        if (chunk is null || chunk.Disposed) return new();
        return GetOrCreateWellspringData(chunk).Wellsprings;
    }


    public static void RemoveWellSpringFromChunk(IWorldAccessor world, BlockPos blockPos)
    {
        var chunk = world.BlockAccessor.GetChunkAtBlockPos(blockPos);
        var data = TryGetWellspringData(chunk);
        data?.Wellsprings?.RemoveAll(ws => ws.Position.Equals(blockPos));
    }

    public static void AddWellSpringToChunk(IWorldAccessor world, BlockPos blockPos)
    {
        var chunk = world.BlockAccessor.GetChunkAtBlockPos(blockPos);
        var data = GetOrCreateWellspringData(chunk);
        if (data is null) return;

        if (data.Wellsprings.Exists(ws => ws.Position.Equals(blockPos))) return;
        data.Wellsprings.Add(new WellspringInfo { Position = blockPos });
    }
}
