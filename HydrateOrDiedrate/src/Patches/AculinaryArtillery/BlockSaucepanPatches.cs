using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Patches.AculinaryArtillery;

[HarmonyPatchCategory("HydrateOrDiedrate.ACulinaryArtillery")]
[HarmonyPatch]
internal static class BlockSaucepanPatches
{
    [HarmonyPatch("ACulinaryArtillery.BlockSaucepan", "GetMeltingDuration")]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> FixMeltingDurationForBoilingWater(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);

        matcher.MatchEndForward(
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CombustibleProperties), nameof(CombustibleProperties.MeltingDuration)))
        );

        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Dup)
        );

        matcher.InsertAfter(
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BlockSaucepanPatches), nameof(AdjustedMeltingDuration)))
        );

        return matcher.InstructionEnumeration();
    }

    public static float AdjustedMeltingDuration(CombustibleProperties combustibleProperties, float originalDuration)
    {
        var code = combustibleProperties.SmeltedStack?.Code;
        if(code?.Domain != "hydrateordiedrate" || !code.Path.StartsWith("waterportion")) return originalDuration;

        return originalDuration * 10;
    }
}
