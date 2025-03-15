using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.wellwater
{
    public class BlockEntityWellSpring : BlockEntity
    {
        private ICoreServerAPI sapi;
        private AquiferManager _aquiferManager;
        private int updateIntervalMs = 5000;
        private double accumulatedWater = 0.0;
        private const double MaxDailyOutput = 70.0;
        private const double MinimumDailyOutput = 1.0;
        private double lastInGameTime = -1.0;
        private double depthFactor = 1.0;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            sapi = api as ICoreServerAPI;
            if (sapi != null)
            {
                _aquiferManager = HydrateOrDiedrateModSystem.AquiferManager;
                if (_aquiferManager == null) return;
                _aquiferManager.RegisterWellspring(Pos, depthFactor);
                RegisterGameTickListener(OnTick, updateIntervalMs);
            }
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (_aquiferManager != null)
            {
                _aquiferManager.UnregisterWellspring(Pos);
            }
        }

        private void OnTick(float dt)
        {
            int chunkX = Pos.X / GlobalConstants.ChunkSize;
            int chunkY = Pos.Y / GlobalConstants.ChunkSize;
            int chunkZ = Pos.Z / GlobalConstants.ChunkSize;
            ChunkPos3D chunkCoord = new ChunkPos3D(chunkX, chunkY, chunkZ);
            var aquiferData = _aquiferManager.GetAquiferData(chunkCoord);
            if (aquiferData == null || aquiferData.AquiferRating == 0)
            {
                return;
            }

            var wellsprings = _aquiferManager.GetWellspringsInChunk(chunkCoord);
            if (wellsprings.Count == 0)
            {
                return;
            }

            var calendar = sapi.World.Calendar;
            double currentInGameTime = calendar.TotalDays;
            if (lastInGameTime < 0)
            {
                lastInGameTime = currentInGameTime;
                return;
            }

            double elapsedDays = currentInGameTime - lastInGameTime;
            if (elapsedDays <= 0)
            {
                return;
            }

            lastInGameTime = currentInGameTime;

            bool isMuddy = IsSurroundedBySoil(sapi.World.BlockAccessor, Pos);
            var nearbyWater = CheckForNearbyGameWater(sapi.World.BlockAccessor, Pos);

            if (isMuddy && (nearbyWater.isFresh || nearbyWater.isSalty))
            {
                accumulatedWater += 0.1;
                if (accumulatedWater >= 1.0)
                {
                    int wholeLiters = (int)Math.Floor(accumulatedWater);
                    accumulatedWater -= wholeLiters;
                    string waterType = nearbyWater.isSalty ? "muddysalt" : "muddy";
                    AddOrPlaceWater(waterType, wholeLiters);
                }

                return;
            }

            if (wellsprings.Count > 0)
            {
                double remainingRating = aquiferData.AquiferRating / wellsprings.Count;
                var thisSpring = wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos));
                if (thisSpring == null)
                {
                    return;
                }

                double dailyLiters = MaxDailyOutput * (remainingRating / 100.0) * (1.0 - 0.75 + (0.75 * depthFactor));
                dailyLiters *= (2f * HydrateOrDiedrateModSystem.LoadedConfig.WellSpringOutputMultiplier);

                if (dailyLiters < MinimumDailyOutput)
                {
                    dailyLiters = MinimumDailyOutput;
                }

                double litersThisTick = dailyLiters * elapsedDays;
                accumulatedWater += litersThisTick;
                if (accumulatedWater >= 1.0)
                {
                    int wholeLiters = (int)Math.Floor(accumulatedWater);
                    accumulatedWater -= wholeLiters;
                    string waterType = aquiferData.IsSalty ? "salt" : "fresh";
                    AddOrPlaceWater(waterType, wholeLiters);
                }
            }
        }

        private void AddOrPlaceWater(string waterType, int litersToAdd)
        {
            var blockAccessor = sapi.World.BlockAccessor;
            bool isMuddy = IsSurroundedBySoil(blockAccessor, Pos);
            int baseVertical = isMuddy ? 1 : DetermineBaseVertical(blockAccessor, Pos);
            int leftoverLiters = litersToAdd;
            for (int i = 0; i < baseVertical && leftoverLiters > 0; i++)
            {
                BlockPos currentPos = Pos.UpCopy(i + 1);
                Block currentBlock = blockAccessor.GetBlock(currentPos);

                if (currentBlock?.Code?.Path.StartsWith($"wellwater{waterType}") == true)
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
            for (int i = 0; i < baseVertical && leftoverLiters > 0; i++)
            {
                BlockPos currentPos = Pos.UpCopy(i + 1);
                Block currentBlock = blockAccessor.GetBlock(currentPos);
                bool skipPlacementCheck = isMuddy;
                string blockPath = currentBlock?.Code?.Path;
                bool isAir = blockPath == "air";
                bool isSpreading = blockPath?.StartsWith($"wellwater{waterType}-spreading-") == true;
                bool isNatural = blockPath?.StartsWith($"wellwater{waterType}-natural-") == true;
                if ((isAir || isSpreading) && !isNatural &&
                    (skipPlacementCheck || IsValidPlacement(blockAccessor, currentPos)))
                {
                    string blockCode = $"hydrateordiedrate:wellwater{waterType}-natural-still-1";
                    Block waterBlock = sapi.World.GetBlock(new AssetLocation(blockCode));
                    if (waterBlock != null)
                    {
                        blockAccessor.SetBlock(waterBlock.BlockId, currentPos);
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

        private int DetermineBaseVertical(IBlockAccessor blockAccessor, BlockPos pos)
        {
            string ringMat = CheckBaseRingMaterial(blockAccessor, pos);
            if (CheckColumnForMaterial(blockAccessor, pos, ringMat))
            {
                switch (ringMat)
                {
                    case "brick":
                        return HydrateOrDiedrateModSystem.LoadedConfig.WellwaterDepthMaxClay;
                    case "stonebrick":
                        return HydrateOrDiedrateModSystem.LoadedConfig.WellwaterDepthMaxStone;
                    default:
                        return HydrateOrDiedrateModSystem.LoadedConfig.WellwaterDepthMaxBase;
                }
            }
            return HydrateOrDiedrateModSystem.LoadedConfig.WellwaterDepthMaxBase;
        }

        private bool IsValidPlacement(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block[] neighbors = new Block[]
            {
                blockAccessor.GetBlock(pos.NorthCopy()),
                blockAccessor.GetBlock(pos.EastCopy()),
                blockAccessor.GetBlock(pos.SouthCopy()),
                blockAccessor.GetBlock(pos.WestCopy())
            };
            bool valid = neighbors.Any(block => block != null && block.SideSolid[BlockFacing.DOWN.Index]);
            return valid;
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
            bool surrounded = blocks.Any(b =>
                b?.Code?.Path.StartsWith("soil-") == true ||
                b?.Code?.Path.StartsWith("sand-") == true ||
                b?.Code?.Path.StartsWith("gravel-") == true
            );
            return surrounded;
        }
        private string CheckBaseRingMaterial(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block[] neighbors = new Block[]
            {
                blockAccessor.GetBlock(pos.NorthCopy()),
                blockAccessor.GetBlock(pos.EastCopy()),
                blockAccessor.GetBlock(pos.SouthCopy()),
                blockAccessor.GetBlock(pos.WestCopy())
            };
            bool allBrick = neighbors.All(b =>
                b?.Code?.Domain == "game" && b.Code.Path.StartsWith("brick")
            );
            if (allBrick)
            {
                return "brick";
            }
            bool allStone = neighbors.All(b =>
                b?.Code?.Domain == "game" && b.Code.Path.StartsWith("stonebrick")
            );
            if (allStone)
            {
                return "stonebrick";
            }
            return "none";
        }
        private bool CheckColumnForMaterial(IBlockAccessor blockAccessor, BlockPos basePos, string ringMaterial)
        {
            for (int level = 1; level <= 5; level++)
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
                        return false;
                    }
                }
                else if (ringMaterial == "stonebrick")
                {
                    bool allStone = neighbors.All(b =>
                        b?.Code?.Domain == "game" && b.Code.Path.StartsWith("stonebrick")
                    );
                    if (!allStone)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            accumulatedWater = tree.GetDouble("accumulatedWater", 0.0);
            if (worldForResolving.Api.Side == EnumAppSide.Server)
            {
                _aquiferManager = HydrateOrDiedrateModSystem.AquiferManager;
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("accumulatedWater", accumulatedWater);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            string description = Lang.Get("hydrateordiedrate:hydrateordiedrate:block-wellspring-description");
            dsc.AppendLine(description);
        }

        private (bool isSalty, bool isFresh) CheckForNearbyGameWater(IBlockAccessor blockAccessor, BlockPos centerPos)
        {
            bool saltyFound = false;
            bool freshFound = false;

            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;

                        BlockPos checkPos = new BlockPos(centerPos.X + dx, centerPos.Y + dy, centerPos.Z + dz);
                        Block checkBlock = blockAccessor.GetBlock(checkPos);

                        if (checkBlock?.Code?.Domain == "game")
                        {
                            if (checkBlock.Code.Path.StartsWith("saltwater-"))
                            {
                                saltyFound = true;
                            }
                            else if (checkBlock.Code.Path.StartsWith("water-") || checkBlock.Code.Path.StartsWith("boilingwater-"))
                            {
                                freshFound = true;
                            }
                        }

                        if (saltyFound && freshFound) return (true, true);
                    }
                }
            }
            return (saltyFound, freshFound);
        }
    }
}