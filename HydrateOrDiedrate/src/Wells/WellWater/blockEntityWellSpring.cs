using HydrateOrDiedrate.Wells.Aquifer;
using HydrateOrDiedrate.Config;
using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater;

public class BlockEntityWellSpring : BlockEntity
{
    private const int updateIntervalMs = 500;
    private double accumulatedWater = 0.0;
    private double LastInGameDay = -1.0;

    private bool canPlaceToConfiguredLevel; //TODO not read?
    
    private string cachedRingMaterial;
    private int partialValidatedHeight;
    private const double AquiferRatingToLitersOutputRatio = 0.5;
    
    public string LastWaterType { get; private set; }
    public double LastDailyLiters { get; private set; }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if(api.Side != EnumAppSide.Server) return;

        AquiferManager.AddWellSpringToChunk(api.World, Pos);

        RegisterGameTickListener(OnServerTick, updateIntervalMs);
        RegisterGameTickListener(OnPeriodicShaftCheck, 30000);
        OnPeriodicShaftCheck(0);
    }
    public override void OnBlockRemoved()
    {
        AquiferManager.RemoveWellSpringFromChunk(Api.World, Pos);
        base.OnBlockRemoved();
    }

    private bool HandleShallowWell(double elapsedDays)
    {
        if(!IsSurroundedBySoil(Api.World.BlockAccessor, Pos)) return false;

        var (nearbySalty, nearbyFresh) = CheckForNearbyGameWater(Api.World.BlockAccessor, Pos);
        if(!nearbyFresh && !nearbySalty) return true;
        
        LastWaterType = GetWaterType(nearbyFresh && !nearbySalty, "muddy");
        LastDailyLiters = ModConfig.Instance.GroundWater.ShallowWellLitersPerDay;
        accumulatedWater += LastDailyLiters * elapsedDays * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        if (accumulatedWater >= 1.0)
        {
            var wholeLiters = (int)accumulatedWater;
            AddOrPlaceWater(wholeLiters, nearbyFresh && !nearbySalty, "muddy");
            accumulatedWater -= wholeLiters;
        }

        return true;
    }

    private void OnServerTick(float dt)
    {
        double currentInGameDays = Api.World.Calendar.TotalDays;
        if (LastInGameDay < 0)
        {
            LastInGameDay = currentInGameDays;
            return;
        }

        double elapsedDays = currentInGameDays - LastInGameDay;
        if (elapsedDays <= 0.05) return; //Only check 20 times a day
        LastInGameDay = currentInGameDays;

        if (HandleShallowWell(elapsedDays)) return;

        var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);

        if(AquiferManager.GetAquiferChunkData(chunk, Api.Logger)?.Data is not { AquiferRating: not 0 } aquiferData) return;
        if(AquiferManager.GetWellspringsInChunk(chunk) is not { Count: not 0} wellsprings) return;

        double remainingRating = (double)aquiferData.AquiferRating / wellsprings.Count;
        var thisSpring = wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos));
        if (thisSpring is null) return;

        LastWaterType = GetWaterType(!aquiferData.IsSalty);
        LastDailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio) * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        accumulatedWater += LastDailyLiters * elapsedDays;
        if (accumulatedWater >= 1.0)
        {
            int wholeLiters = (int)Math.Floor(accumulatedWater);
            accumulatedWater -= wholeLiters;
            AddOrPlaceWater(wholeLiters, !aquiferData.IsSalty);
        }
        MarkDirty(true);
    }

    private void OnPeriodicShaftCheck(float dt)
    {
        cachedRingMaterial = CheckBaseRingMaterial(Api.World.BlockAccessor, Pos);
        int validatedLevels = CheckColumnForMaterial(Api.World.BlockAccessor, Pos, cachedRingMaterial);
        partialValidatedHeight = validatedLevels;
        
        canPlaceToConfiguredLevel = cachedRingMaterial switch
        {
            "brick" when validatedLevels >= ModConfig.Instance.GroundWater.WellwaterDepthMaxClay => true,
            "stonebrick" when validatedLevels >= ModConfig.Instance.GroundWater.WellwaterDepthMaxStone => true,
            _ => false,
        };

        MarkDirty(true);
    }

    /// <returns>The amount of liters that where leftover after placement</returns>
    private int AddOrPlaceWater(int litersToAdd, bool isFresh = true, string pollution = "clean")
    {
        var ba = Api.World.BlockAccessor;
        
        var baseWaterCode = $"wellwater-{(isFresh ? "fresh" : "salt")}-{pollution}";
        int maxVolume = GetMaxVolumeForWaterType(LastWaterType);

        int maxDepth = DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
        int leftoverLiters = litersToAdd;
        
        var currentPos = Pos.Copy();
        for (int i = 0; i < maxDepth && leftoverLiters > 0; i++)
        {
            currentPos.Up();

            if (!WellBlockUtils.SolidAllows(ba.GetSolid(currentPos))) break;

            Block fluidAt = ba.GetFluid(currentPos);
            if ((fluidAt.Code?.Path.StartsWith(baseWaterCode)) != true) continue;

            if (ba.GetBlockEntity(currentPos) is not BlockEntityWellWaterData existingBE) continue;

            int availableCapacity = maxVolume - existingBE.Volume;
            if (availableCapacity <= 0) continue;

            int addedVolume = Math.Min(availableCapacity, leftoverLiters);
            existingBE.Volume += addedVolume;
            existingBE.MarkDirty();
            leftoverLiters -= addedVolume;
        }
        
        var waterBlock = Api.World.GetBlock(new AssetLocation("hydrateordiedrate", $"{baseWaterCode}-natural-still-1"));
        if(waterBlock is null) return leftoverLiters;

        currentPos.Set(Pos);
        for (int i = 0; i < maxDepth && leftoverLiters > 0; i++) //TODO this can probably just be a single loop
        {
            currentPos.Up();
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

    private static string CheckBaseRingMaterial(IBlockAccessor blockAccessor, BlockPos pos)
    {
        //TODO do more efficient walk
        Block[] neighbors =
        [
            blockAccessor.GetBlock(pos.NorthCopy()),
            blockAccessor.GetBlock(pos.EastCopy()),
            blockAccessor.GetBlock(pos.SouthCopy()),
            blockAccessor.GetBlock(pos.WestCopy())
        ];

        bool allBrick = Array.TrueForAll(neighbors, b => b?.Code?.Domain == "game" && b.Code.Path.StartsWith("brick"));
        if (allBrick) return "brick";

        bool allStone = Array.TrueForAll(neighbors, b => b?.Code?.Domain == "game" && b.Code.Path.StartsWith("stonebrick"));
        if (allStone) return "stonebrick";

        return "none";
    }

    private static int CheckColumnForMaterial(IBlockAccessor blockAccessor, BlockPos basePos, string ringMaterial)
    {
        int maxCheck = ringMaterial switch
        {
            "brick" => ModConfig.Instance.GroundWater.WellwaterDepthMaxClay,
            "stonebrick" => ModConfig.Instance.GroundWater.WellwaterDepthMaxStone,
            _ => 0
        };
        if(maxCheck == 0) return 0;

        int validatedHeight = 0;
        for (int level = 1; level <= maxCheck; level++)
        {
            BlockPos checkPos = basePos.UpCopy(level);
            Block[] neighbors =
            [
                blockAccessor.GetBlock(checkPos.NorthCopy()),
                blockAccessor.GetBlock(checkPos.EastCopy()),
                blockAccessor.GetBlock(checkPos.SouthCopy()),
                blockAccessor.GetBlock(checkPos.WestCopy())
            ];
            if (ringMaterial == "brick")
            {
                bool allBrick = neighbors.All(b =>
                    b?.Code?.Domain == "game" && b.Code.Path.StartsWith("brick")
                );
                if (!allBrick)
                {
                    break;
                }
                validatedHeight++;
            }
            else if (ringMaterial == "stonebrick")
            {
                bool allStone = neighbors.All(b =>
                    b?.Code?.Domain == "game" && b.Code.Path.StartsWith("stonebrick")
                );
                if (!allStone)
                {
                    break;
                }

                validatedHeight++;
            }
        }
        return validatedHeight;
    }

    private static bool IsSurroundedBySoil(IBlockAccessor blockAccessor, BlockPos pos)
    {
        //TODO perf
        Block[] blocks = new Block[]
        {
            blockAccessor.GetBlock(pos.NorthCopy()),
            blockAccessor.GetBlock(pos.EastCopy()),
            blockAccessor.GetBlock(pos.SouthCopy()),
            blockAccessor.GetBlock(pos.WestCopy())
        };

        return blocks.Any(b =>
            b?.Code?.Path.StartsWith("soil-") == true ||
            b?.Code?.Path.StartsWith("sand-") == true ||
            b?.Code?.Path.StartsWith("gravel-") == true
        );
    }

    private static bool IsValidPlacement(IBlockAccessor blockAccessor, BlockPos pos, string baseWaterCode)
    {
        Block fluidAtPos = blockAccessor.GetFluid(pos);
        if (fluidAtPos.BlockId != 0 && fluidAtPos.Code?.Path.StartsWith(baseWaterCode) != true) return false;
        
        bool isAir = fluidAtPos.Id == 0;
        bool isSpreading = fluidAtPos.Variant["createdBy"] == "spreading";
        bool isNatural = fluidAtPos.Variant["createdBy"] == "natural";

        return (isAir || isSpreading) && !isNatural && SidesAreSolid(blockAccessor, pos);
    }

    private static bool SidesAreSolid(IBlockAccessor blockAccessor, BlockPos pos)
    {
        var currentPos = pos.NorthCopy();
        Block block = blockAccessor.GetBlock(currentPos);
        if(block is null || !block.SideSolid[BlockFacing.SOUTH.Index]) return false;

        currentPos.Set(pos);
        currentPos.East();
        block  = blockAccessor.GetBlock(currentPos);
        if(block is null || !block.SideSolid[BlockFacing.WEST.Index]) return false;

        currentPos.Set(pos);
        currentPos.South();
        block  = blockAccessor.GetBlock(currentPos);
        if(block is null || !block.SideSolid[BlockFacing.NORTH.Index]) return false;

        currentPos.Set(pos);
        currentPos.West();
        block  = blockAccessor.GetBlock(currentPos);
        if(block is null || !block.SideSolid[BlockFacing.EAST.Index]) return false;

        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        accumulatedWater     = tree.GetDouble("accumulatedWater", 0.0);
        LastDailyLiters      = tree.GetDouble("lastDailyLiters", LastDailyLiters);
        cachedRingMaterial   = tree.GetString("cachedRingMaterial", cachedRingMaterial);
        partialValidatedHeight = tree.GetInt("partialValidatedHeight", partialValidatedHeight);
        LastWaterType        = tree.GetString("lastWaterType", LastWaterType);
        LastInGameDay       = tree.GetDouble("lastInGameTime", LastInGameDay);
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
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        string description = Lang.Get("hydrateordiedrate:block-wellspring-description");
        dsc.AppendLine(description);
    }

    private static (bool nearbySalty, bool nearbyFresh) CheckForNearbyGameWater(IBlockAccessor blockAccessor, BlockPos centerPos)
    {
        bool saltyFound = false;
        bool freshFound = false;

        var checkPos = centerPos.Copy();
        int cx = centerPos.X, cy = centerPos.Y, cz = centerPos.Z;

        for (int dx = -3; dx <= 2; dx++)
        for (int dy = -3; dy <= 2; dy++)
        for (int dz = -3; dz <= 2; dz++)
        {
            if (dx == 0 && dy == 0 && dz == 0) continue;

            checkPos.Set(cx + dx, cy + dy, cz + dz);

            Block checkBlock = blockAccessor.GetBlock(checkPos, BlockLayersAccess.Fluid);
            if(checkBlock?.Code?.Domain != "game") continue;

            if (checkBlock.Code.Path.StartsWith("saltwater-"))
            {
                saltyFound = true;
            }
            else if (checkBlock.Code.Path.StartsWith("water-") || checkBlock.Code.Path.StartsWith("boilingwater-"))
            {
                freshFound = true;
            }

            if (saltyFound && freshFound) break;
        }

        return (saltyFound, freshFound);
    }

    public static string GetWaterType(bool isFresh, string pollution = "clean") => $"{(isFresh ? "fresh" : "salt")}-well-{pollution}";

    public static int GetMaxVolumeForWaterType(string waterType) => waterType.Contains("muddy") ? 9 : 70;

    public int GetMaxTotalVolume() => GetRetentionDepth() * GetMaxVolumeForWaterType(LastWaterType);

    public int GetRetentionDepth() => DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
}