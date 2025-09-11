using HydrateOrDiedrate.Aquifer;
using HydrateOrDiedrate.Config;
using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.wellwater;

public class BlockEntityWellSpring : BlockEntity
{
    private const int updateIntervalMs = 500;
    private double accumulatedWater = 0.0;
    private double LastInGameDay = -1.0;

    private string cachedRingMaterial;
    private bool canPlaceToConfiguredLevel; //TODO not read?
    private int partialValidatedHeight;
    private const double AquiferRatingToLitersOutputRatio = 0.5;
    
    private string lastWaterType;
    private double lastDailyLiters;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if(api.Side != EnumAppSide.Server) return;

        AquiferManager.AddWellSpringToChunk(api.World, Pos);

        RegisterGameTickListener(OnServerTick, updateIntervalMs);
        RegisterGameTickListener(OnPeriodicShaftCheck, 30000);
        OnPeriodicShaftCheck(0);
    }
    
    private Block GetFluid(BlockPos p) => Api.World.BlockAccessor.GetBlock(p, BlockLayersAccess.Fluid);
    
    private Block GetSolid(BlockPos p) => Api.World.BlockAccessor.GetBlock(p, BlockLayersAccess.Solid);
    
    private void  SetFluid(int blockId, BlockPos p) => Api.World.BlockAccessor.SetBlock(blockId, p, BlockLayersAccess.Fluid);

    private bool IsSolidBlocking(Block solidBlock)
        => solidBlock != null
           && solidBlock.Code != null
           && solidBlock.Code.Path != "air"
           && solidBlock.Replaceable < 500;
    
    public override void OnBlockRemoved()
    {
        AquiferManager.RemoveWellSpringFromChunk(Api.World, Pos);
        base.OnBlockRemoved();
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
        if (elapsedDays <= 0) return;
        LastInGameDay = currentInGameDays;

        var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);
        bool isMuddy = IsSurroundedBySoil(Api.World.BlockAccessor, Pos);

        var aquiferData = AquiferManager.GetAquiferChunkData(chunk, Api.Logger)?.Data;
        if (aquiferData is null || (aquiferData.AquiferRating == 0 && !isMuddy)) return;

        var wellsprings = AquiferManager.GetWellspringsInChunk(chunk);
        if (wellsprings.Count == 0) return;
        
        var (isSalty, isFresh) = CheckForNearbyGameWater(Api.World.BlockAccessor, Pos);
        if (isMuddy)
        {
            if (isFresh || isSalty)
            {
                accumulatedWater += 0.001 * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
                if (accumulatedWater >= 1.0)
                {
                    int wholeLiters = (int)Math.Floor(accumulatedWater);
                    accumulatedWater -= wholeLiters;
                    string waterType = isSalty ? "muddysalt" : "muddy";
                    AddOrPlaceWater(waterType, wholeLiters);
                    accumulatedWater = 0;
                }
            }
            
            return;
        }
        double remainingRating = (double)aquiferData.AquiferRating / wellsprings.Count;
        var thisSpring = wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos));
        if (thisSpring is null) return;

        double dailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio) * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        lastDailyLiters = dailyLiters;
        MarkDirty(true);
        double litersThisTick = dailyLiters * elapsedDays;
        accumulatedWater += litersThisTick;
        if (accumulatedWater >= 1.0)
        {
            int wholeLiters = (int)Math.Floor(accumulatedWater);
            accumulatedWater -= wholeLiters;
            string waterType = aquiferData.IsSalty ? "salt" : "fresh";
            lastWaterType = waterType;
            MarkDirty(true);
            AddOrPlaceWater(waterType, wholeLiters);
        }
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

    private void AddOrPlaceWater(string waterType, int litersToAdd)
    {
        var blockAccessor = Api.World.BlockAccessor;
        bool isMuddy = IsSurroundedBySoil(blockAccessor, Pos);

        if (waterType == "muddy" || waterType == "muddysalt")
        {
            BlockPos firstPos = Pos.UpCopy(1);
            Block solidAtFirst = GetSolid(firstPos);
            if (IsSolidBlocking(solidAtFirst))
            {
                accumulatedWater = 0.0;
                return;
            }
            Block fluidAtFirst = GetFluid(firstPos);
            if (fluidAtFirst?.Code?.Path.StartsWith($"wellwater{waterType}") == true)
            {
                var existingBE = blockAccessor.GetBlockEntity<BlockEntityWellWaterData>(firstPos);
                if (existingBE != null)
                {
                    int maxVolume = 9;
                    if (existingBE.Volume >= maxVolume)
                    {
                        accumulatedWater = 0.0;
                        return;
                    }
                    else
                    {
                        int addedVolume = Math.Min(maxVolume - existingBE.Volume, litersToAdd);
                        existingBE.Volume += addedVolume;
                        accumulatedWater = 0.0;
                        return;
                    }
                }
            }
            bool skipPlacementCheck = isMuddy;
            string fluidPath = fluidAtFirst?.Code?.Path;
            bool isAir = fluidPath == "air";
            bool isSpreading = fluidPath?.StartsWith($"wellwater{waterType}-spreading-") == true;
            bool isNatural = fluidPath?.StartsWith($"wellwater{waterType}-natural-") == true;

            if ((isAir || isSpreading) && !isNatural &&
                (skipPlacementCheck || IsValidPlacement(blockAccessor, firstPos)))
            {
                string blockCode = $"hydrateordiedrate:wellwater{waterType}-natural-still-1";
                Block waterBlock = Api.World.GetBlock(new AssetLocation(blockCode));
                if (waterBlock != null)
                {
                    SetFluid(waterBlock.BlockId, firstPos);
                    blockAccessor.TriggerNeighbourBlockUpdate(firstPos);
                    var newBE = blockAccessor.GetBlockEntity<BlockEntityWellWaterData>(firstPos);
                    if (newBE != null)
                    {
                        int maxVolume = 9;
                        int volumeToSet = Math.Min(litersToAdd, maxVolume);
                        newBE.Volume = volumeToSet;
                    }
                }
            }
            accumulatedWater = 0.0;
            return;
        }
        int maxDepth = DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
        int leftoverLiters = litersToAdd;
        for (int i = 0; i < maxDepth && leftoverLiters > 0; i++)
        {
            BlockPos currentPos = Pos.UpCopy(i + 1);

            if (IsSolidBlocking(GetSolid(currentPos))) break;
            Block fluidAt = GetFluid(currentPos);
            if (fluidAt?.Code?.Path.StartsWith($"wellwater{waterType}") == true)
            {
                var existingBE = blockAccessor.GetBlockEntity<BlockEntityWellWaterData>(currentPos);
                if (existingBE != null)
                {
                    int maxVolume = isMuddy ? 9 : 70;
                    int availableCapacity = maxVolume - existingBE.Volume;
                    if (availableCapacity > 0)
                    {
                        int addedVolume = Math.Min(availableCapacity, leftoverLiters);
                        existingBE.Volume += addedVolume;
                        leftoverLiters -= addedVolume;
                    }
                }
            }
        }

        for (int i = 0; i < maxDepth && leftoverLiters > 0; i++)
        {
            BlockPos currentPos = Pos.UpCopy(i + 1);
            if (IsSolidBlocking(GetSolid(currentPos))) break;

            bool skipPlacementCheck = isMuddy;
            Block fluidAt = GetFluid(currentPos);
            string fluidPath = fluidAt?.Code?.Path;
            bool isAir = fluidPath == "air";
            bool isSpreading = fluidPath?.StartsWith($"wellwater{waterType}-spreading-") == true;
            bool isNatural = fluidPath?.StartsWith($"wellwater{waterType}-natural-") == true;

            if ((isAir || isSpreading) && !isNatural &&
                (skipPlacementCheck || IsValidPlacement(blockAccessor, currentPos)))
            {
                string blockCode = $"hydrateordiedrate:wellwater{waterType}-natural-still-1";
                Block waterBlock = Api.World.GetBlock(new AssetLocation(blockCode));
                if (waterBlock != null)
                {
                    SetFluid(waterBlock.BlockId, currentPos);
                    blockAccessor.TriggerNeighbourBlockUpdate(currentPos);
                    var newBE = blockAccessor.GetBlockEntity<BlockEntityWellWaterData>(currentPos);
                    if (newBE != null)
                    {
                        int maxVolume = isMuddy ? 9 : 70;
                        int volumeToSet = Math.Min(leftoverLiters, maxVolume);
                        newBE.Volume = volumeToSet;
                        leftoverLiters -= volumeToSet;
                    }
                }
            }
        }

        if (leftoverLiters > 0)
        {
            accumulatedWater = 0.0;
        }
    }

    private bool IsBlockingBlock(Block block) => block?.Code is not null && block.Code.Path != "air"  && !block.Code.Path.Contains("wellwater");
    
    private int DetermineMaxDepthBasedOnCached(string ringMat, int validatedLevels)
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

    private string CheckBaseRingMaterial(IBlockAccessor blockAccessor, BlockPos pos)
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

    private int CheckColumnForMaterial(IBlockAccessor blockAccessor, BlockPos basePos, string ringMaterial)
    {
        if (ringMaterial == "none")
        {
            return 0;
        }
        int maxCheck;
        if (ringMaterial == "brick")
        {
            maxCheck = ModConfig.Instance.GroundWater.WellwaterDepthMaxClay;
        }
        else if (ringMaterial == "stonebrick")
        {
            maxCheck = ModConfig.Instance.GroundWater.WellwaterDepthMaxStone;
        }
        else
        {
            return 0;
        }
        int validatedHeight = 0;
        for (int level = 1; level <= maxCheck; level++)
        {
            BlockPos checkPos = basePos.UpCopy(level);
            Block[] neighbors = new Block[]
            {
                blockAccessor.GetBlock(checkPos.NorthCopy()),
                blockAccessor.GetBlock(checkPos.EastCopy()),
                blockAccessor.GetBlock(checkPos.SouthCopy()),
                blockAccessor.GetBlock(checkPos.WestCopy())
            };
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

    private bool IsSurroundedBySoil(IBlockAccessor blockAccessor, BlockPos pos)
    {
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
    private bool IsValidPlacement(IBlockAccessor blockAccessor, BlockPos pos)
    {
        Block northBlock = blockAccessor.GetBlock(pos.NorthCopy());
        Block eastBlock  = blockAccessor.GetBlock(pos.EastCopy());
        Block southBlock = blockAccessor.GetBlock(pos.SouthCopy());
        Block westBlock  = blockAccessor.GetBlock(pos.WestCopy());

        bool northSolid = northBlock != null && northBlock.SideSolid[BlockFacing.SOUTH.Index];
        bool eastSolid  = eastBlock  != null && eastBlock.SideSolid[BlockFacing.WEST.Index];
        bool southSolid = southBlock != null && southBlock.SideSolid[BlockFacing.NORTH.Index];
        bool westSolid  = westBlock  != null && westBlock.SideSolid[BlockFacing.EAST.Index];

        return northSolid && eastSolid && southSolid && westSolid;
    }
    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        accumulatedWater     = tree.GetDouble("accumulatedWater", 0.0);
        lastDailyLiters      = tree.GetDouble("lastDailyLiters", lastDailyLiters);
        cachedRingMaterial   = tree.GetString("cachedRingMaterial", cachedRingMaterial);
        partialValidatedHeight = tree.GetInt("partialValidatedHeight", partialValidatedHeight);
        lastWaterType        = tree.GetString("lastWaterType", lastWaterType);
        LastInGameDay       = tree.GetDouble("lastInGameTime", LastInGameDay);
    }
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetDouble("accumulatedWater", accumulatedWater);
        tree.SetDouble("lastDailyLiters", lastDailyLiters);
        tree.SetString("cachedRingMaterial", cachedRingMaterial);
        tree.SetString("lastWaterType", lastWaterType ?? "");
        tree.SetInt("partialValidatedHeight", partialValidatedHeight);
        tree.SetDouble("lastInGameTime", LastInGameDay);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        string description = Lang.Get("hydrateordiedrate:block-wellspring-description");
        dsc.AppendLine(description);
    }

    private (bool isSalty, bool isFresh) CheckForNearbyGameWater(IBlockAccessor blockAccessor, BlockPos centerPos)
    {
        bool saltyFound = false;
        bool freshFound = false;
        for (int dx = -3; dx <= 2; dx++)
        for (int dy = -3; dy <= 2; dy++)
        for (int dz = -3; dz <= 2; dz++)
        {
            if (dx == 0 && dy == 0 && dz == 0) continue;

            BlockPos checkPos = centerPos.AddCopy(dx, dy, dz);

            Block checkBlock = Api.World.BlockAccessor.GetBlock(checkPos, BlockLayersAccess.Fluid);

            if (checkBlock?.Code?.Domain == "game")
            {
                if (checkBlock.Code.Path.StartsWith("saltwater-"))
                    saltyFound = true;
                else if (checkBlock.Code.Path.StartsWith("water-") || checkBlock.Code.Path.StartsWith("boilingwater-"))
                    freshFound = true;
            }

            if (saltyFound && freshFound) return (true, true);
        }
        return (saltyFound, freshFound);
    }

    public string GetWaterType() => lastWaterType;

    public double GetCurrentOutputRate() => lastDailyLiters;

    public int GetRetentionDepth() => DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
}