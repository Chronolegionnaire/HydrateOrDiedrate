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
    public static void Prefix(BlockPos pos, out BlockPos __state) => __state = pos.Copy(); // Make a copy to avoid the mutation that happens in the original method

    //TODO figure out why filtering on this just hides everything
    public static void Postfix(ref PropickReading __result, IWorldAccessor world, BlockPos __state)
    {
        if (world.Side != EnumAppSide.Server || !ModConfig.Instance.GroundWater.ShowAquiferProspectingDataOnMap) return;
        
        var aquiferData = AquiferManager.GetAquiferChunkData(world, __state, world.Logger);
        if (aquiferData == null) return;
        
        __result.OreReadings["$aquifer$"] = aquiferData.Data;
    }
}