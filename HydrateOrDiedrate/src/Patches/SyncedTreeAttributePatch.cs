using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Config;

namespace HydrateOrDiedrate.patches
{
    internal static class SyncedTreeAttributePatch
    {
        public static IEnumerable<CodeInstruction> MarkPathDirtyTranspiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            int value = ModConfig.Instance?.Advanced?.markdirtythreshold ?? 100;

            return new CodeMatcher(instructions, generator)
                .Advance(39)
                .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4, value))
                .InstructionEnumeration();
        }
    }
}