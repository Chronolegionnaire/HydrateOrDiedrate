using System;
using System.Linq;
using System.Reflection;
using HardcoreWater.ModBlockEntity;
using HarmonyLib;
using HydrateOrDiedrate.wellwater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Patches.Harmony;

[HarmonyPatch]
[HarmonyPatchCategory("hardcorewater")]
public static class AqueductPatches
{
    [HarmonyPatch("HardcoreWater.ModBlockEntity.BlockEntityAqueduct", "IsValidWaterSource")]
    [HarmonyPrefix]
    public static bool PatchIsValidWaterSource(BlockEntity __instance, BlockPos blockPos, ref bool __result)
    {
        var fluidBlock = __instance.Api.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.Fluid);
        if(fluidBlock is null || fluidBlock.Code?.Domain != "hydrateordiedrate") return true;

        if (fluidBlock.Code.Path.StartsWith("wellwater"))
        {
            __result = (fluidBlock.Variant["createdBy"] == "natural") && fluidBlock.LiquidLevel > 1;

            return false;
        }

        return true;
    }

    [HarmonyPatch("HardcoreWater.ModBlockEntity.BlockEntityAqueduct", "onServerTick1s")]
    [HarmonyPrefix]
    public static bool ServerTickPrefix(object __instance, out Block __state)
    {
        __state = null;
        if(__instance is not BlockEntityAqueduct aqueduct) return true;
        var blockAccessor = aqueduct.Api.World.BlockAccessor;

        var liquidBlock = blockAccessor.GetBlock(aqueduct.Pos, BlockLayersAccess.Fluid);

        var blockBehavior = liquidBlock.GetBehavior<BlockBehaviorWellWaterFinite>();
        if (blockBehavior is not null && blockBehavior.FindNaturalSourceInLiquidChain(blockAccessor, aqueduct.Pos) is null)
        {
            blockAccessor.SetBlock(0, aqueduct.Pos, BlockLayersAccess.Fluid);
            blockAccessor.TriggerNeighbourBlockUpdate(aqueduct.Pos);
            aqueduct.MarkDirty(true);
            return false;
        }

        if (aqueduct.WaterSourcePos is not null)
        {
            var block = blockAccessor.GetBlock(aqueduct.WaterSourcePos, BlockLayersAccess.Fluid);
            if(block.Code?.Domain == "hydrateordiedrate")
            {
                __state = block;
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch("HardcoreWater.ModBlockEntity.BlockEntityAqueduct", "onServerTick1s")]
    [HarmonyPrefix]
    public static void ServerTickPostFix(object __instance, Block __state)
    {
        if (__instance is not BlockEntityAqueduct aqueduct || __state is null) return;
        var blockAccessor = aqueduct.Api.World.BlockAccessor;

        var liquidLevel = __state.LiquidLevel;
        if (liquidLevel == 0) return;
        else if (liquidLevel == 7) liquidLevel--;

        var tokens = __state.Code.Path.Split('-');
        if (tokens.Length < 4) return;

        var newWaterBlock = blockAccessor.GetBlock(new AssetLocation("hydrateordiedrate", $"{tokens[0]}-spreading-{tokens[2]}-{liquidLevel}"));
        if (newWaterBlock is null) return;
        
        blockAccessor.SetBlock(newWaterBlock.BlockId, aqueduct.Pos, BlockLayersAccess.Fluid);
        blockAccessor.TriggerNeighbourBlockUpdate(aqueduct.Pos);
        aqueduct.MarkDirty(true);
    }
}
