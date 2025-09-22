using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Wells.Aquifer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater;

public class BlockEntityWellSpring : BlockEntity, ITexPositionSource
{
    private const int updateIntervalMs = 500;

    private Block originBlock;
    
    public Block OriginBlock
    {
        get => originBlock;
        set
        {
            if (originBlock == value) return;
            IsShallow = value.IsSoil();
            if (IsShallow && value is not null && value.Variant.TryGetValue("grasscoverage", out var variant) && variant != "none")
            {
                var withoutGrass = Api.World.GetBlock(value.CodeWithVariant("grasscoverage", "none"));
                if(withoutGrass is not null) value = withoutGrass;
            }

            originBlock = value;
            UpdateTextureSources();
            MarkDirty(true);
        }
    }

    private void UpdateTextureSources()
    {
        if(Api is not ICoreClientAPI capi) return;

        textureSources = OriginBlock is null ? null : [
            capi.Tesselator.GetTextureSource(OriginBlock, returnNullWhenMissing: true),
            capi.Tesselator.GetTextureSource(Block, returnNullWhenMissing: true)
        ];
    }
    
    private ITexPositionSource[] textureSources = [];
    public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;

    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            var unknown = ((ICoreClientAPI)Api).BlockTextureAtlas.UnknownTexturePosition;
            TextureAtlasPosition result;

            for (int i = 0; i < textureSources.Length; i++)
            {
                result = textureSources[i][textureCode];
                if(result is not null && result != unknown) return result;
                
                if(textureCode == "all")
                {
                    result = textureSources[i]["north"]; //HACK: base game annoyingly empties the 'all' identifier
                    if(result is not null && result != unknown) return result;
                }
            }
            
            for (int i = 0; i < textureSources.Length - 1; i++)
            {
                result = textureSources[i]["all"];
                if(result is not null&& result != unknown) return result;
            }

            return unknown;
        }
    }

    public bool IsShallow { get; private set; }

    private double LastInGameDay = -1.0;

    private string cachedRingMaterial;
    
    private int partialValidatedHeight;
    
    private const double AquiferRatingToLitersOutputRatio = 0.5;

    public string LastWaterType { get; private set; }
    
    public double LastDailyLiters { get; private set; }

    private double accumulatedWater = 0.0;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if ((int)LastInGameDay == -1) LastInGameDay = api.World.Calendar.TotalDays;
        
        UpdateTextureSources();
        if (api.Side != EnumAppSide.Server) return;
        
        AquiferManager.AddWellSpringToChunk(api.World, Pos);
        RegisterGameTickListener(OnServerTick, updateIntervalMs);
        RegisterGameTickListener(OnPeriodicShaftCheck, 30000);
        
        OriginBlock ??= api.World.FindMostLikelyOriginBlockFromNeighbors(Pos) ?? api.World.GetBlock(new AssetLocation("game", "rock-granite"));
        OnPeriodicShaftCheck(0);
        HandleWell(0);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if(base.OnTesselation(mesher, tessThreadTesselator)) return true;

        var meshData = GetMesh();
        if(meshData is null) return false;
        mesher.AddMeshData(meshData);
    
        return true;
    }

    public MeshData GetMesh()
    {
        if(Api is not ICoreClientAPI capi || OriginBlock is null) return null;
        if(!capi.ObjectCache.TryGetValue("wellspringmeshes", out var obj) || obj is not Dictionary<AssetLocation, MeshData> cachedMeshes)
        {
            capi.ObjectCache["wellspringmeshes"] = cachedMeshes = [];
        }
        if(cachedMeshes.TryGetValue(OriginBlock.Code, out var result)) return result;

        var shape = Shape.TryGet(Api, new AssetLocation("hydrateordiedrate","shapes/block/wellspring.json"));
        capi.Tesselator.TesselateShape("wellspirng", shape, out result, this, new Vec3f(Block.Shape.rotateX, Block.Shape.rotateY, Block.Shape.rotateZ), 0, 0, 0, null, null);
        cachedMeshes[OriginBlock.Code] = result;
        return result;
    }


    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        if(Api.Side == EnumAppSide.Server) AquiferManager.RemoveWellSpringFromChunk(Api.World, Pos);
    }

    private void HandleShallowWell(double elapsedDays)
    {
        var (nearbySalty, nearbyFresh) = Api.World.BlockAccessor.CheckForNearbyGameWater(Pos);
        if (!nearbyFresh && !nearbySalty) return;

        LastWaterType = GetWaterType(nearbyFresh && !nearbySalty, "muddy");
        LastDailyLiters = ModConfig.Instance.GroundWater.ShallowWellLitersPerDay;
        accumulatedWater += LastDailyLiters * elapsedDays * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        if (accumulatedWater < 1.0) return;

        var wholeLiters = (int)accumulatedWater;
        AddOrPlaceWater(wholeLiters, nearbyFresh && !nearbySalty, "muddy");
        accumulatedWater -= wholeLiters;
    }

    private void HandleAquiferWell(double elapsedDays)
    {
        var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);

        if (AquiferManager.GetAquiferChunkData(chunk, Api.Logger)?.Data is not { AquiferRating: not 0 } aquiferData) return;
        if (AquiferManager.GetWellspringsInChunk(chunk) is not { Count: not 0 } wellsprings) return;

        double remainingRating = (double)aquiferData.AquiferRating / wellsprings.Count;
        var thisSpring = wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos));
        if (thisSpring is null) return;

        LastWaterType = GetWaterType(!aquiferData.IsSalty);
        LastDailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio) * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        accumulatedWater += LastDailyLiters * elapsedDays;
        if (accumulatedWater < 1.0) return;

        int wholeLiters = (int)Math.Floor(accumulatedWater);
        accumulatedWater -= wholeLiters;
        AddOrPlaceWater(wholeLiters, !aquiferData.IsSalty);
    }

    private void HandleWell(double elapsedDays)
    {
        if(IsShallow) HandleShallowWell(elapsedDays);
        else HandleAquiferWell(elapsedDays);
        MarkDirty();
    }

    private void OnServerTick(float dt)
    {
        double currentInGameDays = Api.World.Calendar.TotalDays;

        double elapsedDays = currentInGameDays - LastInGameDay;
        if (elapsedDays <= 0.05) return; //Only check 20 times a day
        LastInGameDay = currentInGameDays;
        HandleWell(elapsedDays);
    }

    private void OnPeriodicShaftCheck(float dt)
    {
        cachedRingMaterial = CheckBaseRingMaterial(Api.World.BlockAccessor, Pos);
        partialValidatedHeight = CheckColumnForMaterial(Api.World.BlockAccessor, Pos, cachedRingMaterial);
        MarkDirty();
    }

    /// <returns>The amount of liters that where leftover after placement</returns>
    private int AddOrPlaceWater(int litersToAdd, bool isFresh = true, string pollution = "clean")
    {
        var ba = Api.World.BlockAccessor;

        var baseWaterCode = $"wellwater-{(isFresh ? "fresh" : "salt")}-{pollution}";
        int maxVolume = GetMaxVolumeForWaterType(LastWaterType);

        int maxDepth = pollution == "muddy" ? 1 : DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
        int leftoverLiters = litersToAdd;

        var currentPos = Pos.Copy();
        for (int i = 0; i < maxDepth && leftoverLiters > 0; i++)
        {
            currentPos.Up();

            if (!WellBlockUtils.SolidAllows(ba.GetSolid(currentPos))) break;

            Block fluidAt = ba.GetFluid(currentPos);
            if ((fluidAt.Code?.Path.StartsWith(baseWaterCode)) != true) break;

            if (ba.GetBlockEntity(currentPos) is not BlockEntityWellWaterData existingBE) continue;

            int availableCapacity = maxVolume - existingBE.Volume;
            if (availableCapacity <= 0) continue;

            int addedVolume = Math.Min(availableCapacity, leftoverLiters);
            existingBE.Volume += addedVolume;
            existingBE.MarkDirty();
            leftoverLiters -= addedVolume;
        }

        var waterBlock = Api.World.GetBlock(new AssetLocation("hydrateordiedrate", $"{baseWaterCode}-natural-still-1"));
        if (waterBlock is null) return leftoverLiters;

        currentPos.Set(Pos);
        for (int i = 0; i < maxDepth && leftoverLiters > 0; i++) //TODO this can probably just be a single loop
        {
            currentPos.Up();
            if(ba.GetBlockEntity(currentPos) is BlockEntityWellWaterData) continue;
            if (!WellBlockUtils.SolidAllows(ba.GetSolid(currentPos))) break;
            if (!IsValidPlacement(ba, currentPos, baseWaterCode)) break;

            ba.SetFluid(waterBlock.BlockId, currentPos);
            ba.TriggerNeighbourBlockUpdate(currentPos);

            if (ba.GetBlockEntity(currentPos) is not BlockEntityWellWaterData newBE) continue;

            int volumeToSet = Math.Min(leftoverLiters, maxVolume);
            newBE.Volume = volumeToSet;
            leftoverLiters -= volumeToSet;
        }

        return leftoverLiters;
    }

    private static int DetermineMaxDepthBasedOnCached(string ringMat, int validatedLevels)
    {
        int baseDepth = ModConfig.Instance.GroundWater.WellwaterDepthMaxBase;
        if (validatedLevels <= 0 || ringMat == "none") return baseDepth;
        if (ringMat == "brick")
        {
            int clayMax = ModConfig.Instance.GroundWater.WellwaterDepthMaxClay;
            int partialDepth = Math.Min(validatedLevels, clayMax);
            return Math.Max(baseDepth, partialDepth);
        }
        else if (ringMat == "stonebrick")
        {
            int stoneMax = ModConfig.Instance.GroundWater.WellwaterDepthMaxStone;
            int partialDepth = Math.Min(validatedLevels, stoneMax);
            return Math.Max(baseDepth, partialDepth);
        }
        return baseDepth;
    }

    public static string GetRingMaterial(Block block)
    {
        //TODO make a better mechanism for this
        if(block?.Code is null || block.Code.Domain != "game") return  "none";
        if(block.Code.Path.StartsWith("brick")) return "brick";
        if(block.Code.Path.StartsWith("stonebrick")) return "stonebrick";
        return "none";
    }

    private static string CheckBaseRingMaterial(IBlockAccessor blockAccessor, BlockPos blockPos)
    {
        string result = null;
        var pos = blockPos.Copy();
        
        var sidesToCheck = BlockFacing.HORIZONTALS;
        for (int i = 0; i < sidesToCheck.Length; i++)
        {
            sidesToCheck[i].IterateThruFacingOffsets(pos);
            var ringMaterial = GetRingMaterial(blockAccessor.GetBlock(blockPos));
            result ??= ringMaterial;
            if(ringMaterial == "none" || result != ringMaterial) return "none";
        }

        return result;
    }

    public static int MaxDepthForRingMaterial(string ringMaterial) => ringMaterial switch
    {
        "brick" => ModConfig.Instance.GroundWater.WellwaterDepthMaxClay,
        "stonebrick" => ModConfig.Instance.GroundWater.WellwaterDepthMaxStone,
        _ => 0
    };

    private static int CheckColumnForMaterial(IBlockAccessor blockAccessor, BlockPos basePos, string ringMaterial)
    {
        int maxCheck = MaxDepthForRingMaterial(ringMaterial);
        if (maxCheck == 0) return 0;

        var pos = basePos.Copy();
        while (pos.Y < basePos.Y + maxCheck)
        {
            pos.Y++;
            var ringMaterialAtPos = CheckBaseRingMaterial(blockAccessor, pos);
            if(ringMaterialAtPos != ringMaterial) return pos.Y - basePos.Y - 1;
        }

        return maxCheck;
    }

    private static bool IsValidPlacement(IBlockAccessor blockAccessor, BlockPos pos, string baseWaterCode)
    {
        Block fluidAtPos = blockAccessor.GetFluid(pos);
        if (fluidAtPos.BlockId != 0 && fluidAtPos.Code?.Path.StartsWith(baseWaterCode) != true) return false;

        return baseWaterCode.Contains("muddy") || blockAccessor.IsContainedBySolids(pos, BlockFacing.HORIZONTALS);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine(Lang.Get("hydrateordiedrate:block-wellspring-description"));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetDouble("accumulatedWater", accumulatedWater);
        tree.SetDouble("lastDailyLiters", LastDailyLiters);
        tree.SetString("cachedRingMaterial", cachedRingMaterial);
        tree.SetString("lastWaterType", LastWaterType ?? string.Empty);
        tree.SetInt("partialValidatedHeight", partialValidatedHeight);
        tree.SetDouble("lastInGameTime", LastInGameDay);
        if(OriginBlock is not null) tree.SetInt("OriginBlockId", OriginBlock.Id);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        accumulatedWater = tree.GetDouble("accumulatedWater", accumulatedWater);
        LastDailyLiters = tree.GetDouble("lastDailyLiters", LastDailyLiters);
        cachedRingMaterial = tree.GetString("cachedRingMaterial", cachedRingMaterial);
        partialValidatedHeight = tree.GetInt("partialValidatedHeight", partialValidatedHeight);
        LastWaterType = tree.GetString("lastWaterType", LastWaterType);
        LastInGameDay = tree.GetDouble("lastInGameTime", worldAccessForResolve.Calendar.TotalDays);

        var originBlockId = tree.TryGetInt("OriginBlockId");
        if (originBlockId.HasValue && originBlockId != OriginBlock?.Id) OriginBlock = worldAccessForResolve.GetBlock(originBlockId.Value);
    }

    public static string GetWaterType(bool isFresh, string pollution = "clean") => $"{(isFresh ? "fresh" : "salt")}-well-{pollution}";

    public static int GetMaxVolumeForWaterType(string waterType) => waterType.Contains("muddy") ? 9 : 70;

    public int GetMaxTotalVolume() => GetRetentionDepth() * GetMaxVolumeForWaterType(LastWaterType);

    public int GetRetentionDepth() => DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
}