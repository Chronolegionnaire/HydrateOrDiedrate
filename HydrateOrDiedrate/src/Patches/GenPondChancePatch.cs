using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Config;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    internal static class GenPondsPondChancePatch
    {
        static MethodBase TargetMethod()
        {
            var genPondsType = AccessTools.TypeByName("Vintagestory.ServerMods.GenPonds");
            return AccessTools.Method(genPondsType, "OnChunkColumnGen");
        }
        public static float AdjustMaxTries(float maxTries)
        {
            float mult = ModConfig.Instance.WorldGen.PondChance;
            if (mult < 0f) mult = 0f;
            return maxTries * mult;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var adjustMethod = AccessTools.Method(typeof(GenPondsPondChancePatch), nameof(AdjustMaxTries));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float f && f == 10f)
                {
                    int j = i + 1;
                    while (j < codes.Count && codes[j].opcode == OpCodes.Nop) j++;
                    if (j >= codes.Count || codes[j].opcode != OpCodes.Mul) continue;
                    int insertPos = j + 1;
                    codes.Insert(insertPos, new CodeInstruction(OpCodes.Call, adjustMethod));
                    break;
                }
            }
            return codes;
        }
    }
}
