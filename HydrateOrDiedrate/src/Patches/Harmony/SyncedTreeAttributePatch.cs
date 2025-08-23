using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Datastructures;
using System;
using System.Linq;

namespace HydrateOrDiedrate.Patches.Harmony;

[HarmonyPatch]
[HarmonyPatchCategory(HydrateOrDiedrateModSystem.PatchCategory_MarkDirtyThreshold)]
internal static class SyncedTreeAttributePatch
{
    private static int Threshold => ModConfig.Instance.Advanced.markdirtythreshold;

    [HarmonyPatch(typeof(SyncedTreeAttribute), "MarkPathDirty")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> MarkPathDirtyTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var instrList = instructions.ToList();

        var fieldAttrPathsDirty = AccessTools.Field(typeof(SyncedTreeAttribute), "attributePathsDirty") ?? throw new MissingFieldException(typeof(SyncedTreeAttribute).FullName, "attributePathsDirty");
        
        var hashSetCountGetter = AccessTools.PropertyGetter(typeof(HashSet<string>), "Count") ?? throw new MissingMethodException(typeof(HashSet<string>).FullName, "get_Count");
        
        var matcher = new CodeMatcher(instrList, generator)
            .MatchStartForward(
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Ldfld && ci.operand is FieldInfo fi && fi == fieldAttrPathsDirty),
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo mi && mi == hashSetCountGetter),
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Ldc_I4_S || ci.opcode == OpCodes.Ldc_I4)
            );

        if (!matcher.IsValid)
        {
            throw new InvalidOperationException(
                "HydrateOrDiedrate: Could not locate the IL pattern for MarkPathDirty transpiler. " +
                "The target method may have changed; please update the matcher.");
        }
        
        matcher.Advance(2);
        matcher.SetInstruction(new CodeInstruction(OpCodes.Ldc_I4, Threshold));

        return matcher.InstructionEnumeration();
    }
}
