using HarmonyLib;
using HydrateOrDiedrate.Aquifer;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch(typeof(ItemProspectingPick), "GenProbeResults")]
public static class GenProbeResultsPatch
{
    static void Prefix(IWorldAccessor world, BlockPos pos, out BlockPos __state)
    {
        __state = pos.Copy();
    }

    static void Postfix(ref PropickReading __result, IWorldAccessor world, BlockPos pos, BlockPos __state)
    {
        if (world.Side != EnumAppSide.Server || !ModConfig.Instance.GroundWater.ShowAquiferProspectingDataOnMap) return;
        
        var aquiferData = AquiferManager.GetAquiferChunkData(world, __state, world.Logger);
        if (aquiferData == null) return;
        
        __result.OreReadings["$aquifer$"] = new OreReading()
        {
            DepositCode = aquiferData.Data.IsSalty ? "salty" : "fresh",
            PartsPerThousand = aquiferData.Data.AquiferRating,
            TotalFactor = 0.0000001
        };
    }
}