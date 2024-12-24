using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    public static class Patch_AqueductIsValidWaterSource
    {
        static bool Prepare()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "HardcoreWater");
        }
        static MethodBase TargetMethod()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "HardcoreWater");
            if (assembly == null) return null;
            var aqueductType = assembly.GetType("HardcoreWater.ModBlockEntity.BlockEntityAqueduct");
            if (aqueductType == null) return null;
            return aqueductType.GetMethod(
                "IsValidWaterSource",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(BlockPos), typeof(int) },
                null
            );
        }
        [HarmonyPrefix]
        static bool Prefix(object __instance, BlockPos blockPos, int minLevel, ref bool __result)
        {
            dynamic aqueduct = __instance;
            var api = aqueduct.Api;
            if (api == null)
            {
                return true;
            }
            var blockAccessor = api.World.BlockAccessor;
            var block = blockAccessor.GetBlock(blockPos, BlockLayersAccess.Fluid);
            if (block == null) return true;
            if (block.Code?.Domain == "game")
            {
                return true;
            }

            if (block.Code?.Domain == "hydrateordiedrate" && block.Code.Path.StartsWith("wellwater"))
            {
                var tokens = block.Code.Path.Split('-');
                if (tokens.Length >= 2)
                {
                    string createdBy = tokens[1];
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
    [HarmonyPatch]
    public static class Patch_AqueductOnServerTick1s
    {
        static bool Prepare()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "HardcoreWater");
        }
        static MethodBase TargetMethod()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "HardcoreWater");
            if (assembly == null) return null;
            var aqueductType = assembly.GetType("HardcoreWater.ModBlockEntity.BlockEntityAqueduct");
            if (aqueductType == null) return null;
            return aqueductType.GetMethod(
                "onServerTick1s",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(float) },
                null
            );
        }

        [HarmonyPrefix]
        static bool Prefix(object __instance, float dt)
        {
            dynamic aqueduct = __instance;
            var api = aqueduct.Api;
            if (api == null) return false;
            var blockAccessor = api.World.BlockAccessor;
            var waterSourcePos = aqueduct.WaterSourcePos;
            if (waterSourcePos != null)
            {
                var sourceBlock = blockAccessor.GetBlock(waterSourcePos, BlockLayersAccess.Fluid);
                if (sourceBlock != null)
                {
                    var domain = sourceBlock.Code?.Domain;
                    if (domain == "game")
                    {
                        return true;
                    }
                    else if (domain == "hydrateordiedrate")
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        [HarmonyPostfix]
        static void Postfix(object __instance, float dt)
        {
            dynamic aqueduct = __instance;
            var api = aqueduct.Api;
            if (api == null) return;
            var waterSourcePos = aqueduct.WaterSourcePos;
            if (waterSourcePos == null) return;
            var blockAccessor = api.World.BlockAccessor;
            var sourceBlock = blockAccessor.GetBlock(waterSourcePos, BlockLayersAccess.Fluid);
            if (sourceBlock != null && sourceBlock.Code?.Domain == "hydrateordiedrate")
            {
                var tokens = sourceBlock.Code.Path.Split('-');
                if (tokens.Length >= 4)
                {
                    string liquidLevel = tokens[3];
                    if (liquidLevel == "1") return;
                    if (liquidLevel == "7") liquidLevel = "6";
                    string baseCode = tokens[0];
                    string createdBy = "spreading";
                    string still = tokens[2];
                    string newCode = $"hydrateordiedrate:{baseCode}-{createdBy}-{still}-{liquidLevel}";
                    var newWaterBlock = api.World.GetBlock(new AssetLocation(newCode));
                    if (newWaterBlock != null)
                    {
                        blockAccessor.SetBlock(newWaterBlock.BlockId, aqueduct.Pos, BlockLayersAccess.Fluid);
                        blockAccessor.TriggerNeighbourBlockUpdate(aqueduct.Pos);
                        aqueduct.MarkDirty(true);
                    }
                }
            }
        }
    }
}
