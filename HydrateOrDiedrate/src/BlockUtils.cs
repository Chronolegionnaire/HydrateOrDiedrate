using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate;

public static class BlockUtils
{
    public static string[] SoilPathPrefixes { get; set; } = [ "soil-", "sand-", "gravel-" ];

    public static bool IsSoil(this Block block) => block?.Code is not null && SoilPathPrefixes.Any(block.Code.Path.StartsWith);

    public static string[] StonePathPrefixes { get; set; } = [ "rock-" ];

    public static bool IsStone(this Block block) => block?.Code is not null && StonePathPrefixes.Any(block.Code.Path.StartsWith);

    public static (bool nearbySalty, bool nearbyFresh) CheckForNearbyGameWater(this IBlockAccessor blockAccessor, BlockPos centerPos)
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
            if (checkBlock?.Code?.Domain != "game") continue;

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

    public static Block FindMostLikelyOriginBlockFromNeighbors(this IWorldAccessor world, BlockPos blockPos)
    {
        Dictionary<int, int> ApearanceCount = [];

        var ba = world.BlockAccessor;
        var pos = blockPos.Copy();
        var sidesToCheck = BlockFacing.ALLFACES;
        for(int i = 0; i < sidesToCheck.Length; i++)
        {
            sidesToCheck[i].IterateThruFacingOffsets(pos);
            var block = ba.GetBlock(pos);
            if(!block.IsSoil() && !block.IsStone()) continue;

            ApearanceCount.TryGetValue(block.Id, out var count);
            ApearanceCount[block.Id] = count + 1;
        }

        var mostLikelyBlockId = ApearanceCount.OrderByDescending(static pair => pair.Value).Select(static pair => pair.Key).FirstOrDefault();
        return mostLikelyBlockId == 0 ? null : world.GetBlock(mostLikelyBlockId);
    }

    public static bool IsContainedBySolids(this IBlockAccessor blockAccessor, BlockPos blockPos, BlockFacing[] sidesToCheck)
    {
        var pos = blockPos.Copy();
        for(int i = 0; i < sidesToCheck.Length; i++)
        {
            var sideToCheck = sidesToCheck[i];
            sideToCheck.IterateThruFacingOffsets(pos);
            var neighborBlock = blockAccessor.GetBlock(pos);
            if(neighborBlock is null || !neighborBlock.SideSolid[sideToCheck.Opposite.Index]) return false;
        }

        return true;
    }

    public static bool IsNextToSoil(this IBlockAccessor blockAccessor, BlockPos blockPos)
    {
        var sidesToCheck = BlockFacing.HORIZONTALS;
        
        var pos = blockPos.Copy();
        for(int i = 0; i < sidesToCheck.Length; i++)
        {
            var sideToCheck = sidesToCheck[i];
            sideToCheck.IterateThruFacingOffsets(pos);
            var neighborBlock = blockAccessor.GetBlock(pos);

            if(neighborBlock.IsSoil()) return true;
        }

        return false;
    }
}
