using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using HardcoreWater.ModBlockEntity;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(BlockEntityAqueduct))]
    [HarmonyPatch("IsValidWaterSource", new System.Type[] { typeof(BlockPos), typeof(int) })]
    public static class Patch_AqueductIsValidWaterSource
    {
        static bool Prefix(BlockEntityAqueduct __instance, BlockPos blockPos, int minLevel, ref bool __result)
        {
            var api = __instance.Api;
            if (api == null)
            {
                return true;
            }
            var blockAccessor = api.World.BlockAccessor;
            var block = blockAccessor.GetBlock(blockPos, BlockLayersAccess.Fluid);
            if (block == null)
            {
                return true;
            }
            if (block.Code?.Domain == "game")
            {
                return true;
            }
            if (block.Code?.Domain == "hydrateordiedrate" && block.Code.Path.StartsWith("wellwater"))
            {
                var tokens = block.Code.Path.Split('-');
                if (tokens.Length >= 2)
                {
                    string createdBy = tokens[1]; // Check the second part of the code path (index 1)
                    int liquidLevel = block.LiquidLevel;
                    if (createdBy == "natural" && liquidLevel > 1)
                    {
                        __result = true;
                        return false;
                    }
                }
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BlockEntityAqueduct))]
    [HarmonyPatch("onServerTick1s")]
    public static class Patch_AqueductOnServerTick1s
    {
        static bool Prefix(BlockEntityAqueduct __instance, float dt)
        {
            var api = __instance.Api;
            if (api == null)
            {
                return false;
            }

            var blockAccessor = api.World.BlockAccessor;
            if (__instance.WaterSourcePos != null)
            {
                var sourceBlock = blockAccessor.GetBlock(__instance.WaterSourcePos, BlockLayersAccess.Fluid);
                if (sourceBlock != null)
                {
                    var domain = sourceBlock.Code?.Domain;
                    if (domain == "game")
                    {
                        // Skip postfix and keep original method behavior
                        return true;
                    }
                    else if (domain == "hydrateordiedrate")
                    {
                        // Skip original method and go to postfix
                        return false;
                    }
                }
            }

            return true; // Keep original method behavior if no condition matches
        }

        static void Postfix(BlockEntityAqueduct __instance, float dt)
        {
            var api = __instance.Api;
            if (api == null || __instance.WaterSourcePos == null)
            {
                return;
            }

            var blockAccessor = api.World.BlockAccessor;
            var sourceBlock = blockAccessor.GetBlock(__instance.WaterSourcePos, BlockLayersAccess.Fluid);
            if (sourceBlock != null && sourceBlock.Code?.Domain == "hydrateordiedrate")
            {
                var tokens = sourceBlock.Code.Path.Split('-');
                if (tokens.Length >= 4)
                {
                    string liquidLevel = tokens[3];
                    if (liquidLevel == "1")
                    {
                        return;
                    }
                    if (liquidLevel == "7")
                    {
                        liquidLevel = "6";
                    }
                    string baseCode = tokens[0];
                    string createdBy = "spreading";
                    string still = tokens[2];

                    string newCode = $"hydrateordiedrate:{baseCode}-{createdBy}-{still}-{liquidLevel}";
                    var newWaterBlock = api.World.GetBlock(new AssetLocation(newCode));

                    if (newWaterBlock != null)
                    {
                        blockAccessor.SetBlock(newWaterBlock.BlockId, __instance.Pos, BlockLayersAccess.Fluid);
                        blockAccessor.TriggerNeighbourBlockUpdate(__instance.Pos);
                        __instance.MarkDirty(true);
                    }
                }
            }
        }
    }
}
