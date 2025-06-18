using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.Server;

namespace HydrateOrDiedrate;

//TODO this method can potentially be called multiple times for the same blockPos, so might be better to just remove this altogether and rely on `CalculateAquiferData`
[HarmonyPatch(typeof(BlockAccessorWorldGen), nameof(BlockAccessorWorldGen.SetFluidBlock))]
public static class BlockAccessorWorldGenPatch
{
    public static void Postfix(int blockId, BlockPos pos, BlockAccessorWorldGen __instance)
    {
        var block = __instance.GetBlock(blockId);
        if (block == null) return;

        if(block.Code.Domain != "game") return;
        var path = block.Code.Path;

        string key = null;
        if (path.StartsWith("water")) key = "game:water";
        else if (path.StartsWith("saltwater")) key = "game:saltwater";
        else if (path.StartsWith("boilingwater")) key = "game:boilingwater";

        if(key == null) return;

        var chunk = __instance.GetChunkAtBlockPos(pos);
        if(chunk == null) return;

        chunk.SetModdata(key, chunk.GetModdata(key, 0) + 1);
    }
}