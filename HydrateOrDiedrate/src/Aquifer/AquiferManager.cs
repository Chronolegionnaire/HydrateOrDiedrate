using HydrateOrDiedrate.Aquifer.ModData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Aquifer;

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
        return true;
    }

    public static List<WellspringInfo> GetWellspringsInChunk(IWorldChunk chunk)
    {
        if (chunk is null || chunk.Disposed) return [];

        var wellsData = chunk.GetModdata<WellspringData>("wellspringData", null);
        if (wellsData is null || wellsData.Wellsprings is null) return [];

        return wellsData.Wellsprings;
    }

    //TODO This should probably not be stored in ModData as it is not persisent
    public static void RemoveWellSpringFromChunk(IWorldAccessor world, BlockPos blockPos)
    {
        IWorldChunk chunk = world.BlockAccessor.GetChunkAtBlockPos(blockPos);
        if (chunk is null || chunk.Disposed) return;
        
        var wellsData = chunk.GetModdata<WellspringData>(WellspringModDataKey, null);
        if (wellsData is null) return;

        wellsData.Wellsprings.RemoveAll(ws => ws.Position.Equals(blockPos));
        chunk.SetModdata(WellspringModDataKey, wellsData);
    }

    public static void AddWellSpringToChunk(IWorldAccessor world, BlockPos blockPos)
    {
        IWorldChunk chunk = world.BlockAccessor.GetChunkAtBlockPos(blockPos);
        if (chunk is null || chunk.Disposed) return;

        var wellsData = chunk.GetModdata<WellspringData>(WellspringModDataKey, null) ?? new();
        
        wellsData.Wellsprings ??= [];
        if (wellsData.Wellsprings.Exists(ws => ws.Position.Equals(blockPos))) return;

        wellsData.Wellsprings.Add(new WellspringInfo { Position = blockPos });
        chunk.SetModdata(WellspringModDataKey, wellsData);
    }
}
