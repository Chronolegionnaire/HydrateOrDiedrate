using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Wells.Aquifer.ModData;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch(typeof(OreMapComponent))]
public static class OreMapComponentColorPatch
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[]
    {
        typeof(int),
        typeof(PropickReading),
        typeof(OreMapLayer),
        typeof(ICoreClientAPI),
        typeof(string)
    })]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Ctor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        var highestReadingGetter = AccessTools.PropertyGetter(typeof(PropickReading), nameof(PropickReading.HighestReading));
        var oreReadingsField = AccessTools.Field(typeof(PropickReading), nameof(PropickReading.OreReadings));
        var dictItemGetter = AccessTools.PropertyGetter(typeof(Dictionary<string, OreReading>), "Item");
        var totalFactorField = AccessTools.Field(typeof(OreReading), nameof(OreReading.TotalFactor));
        var helperHighest = AccessTools.Method(typeof(OreMapComponentColorPatch), nameof(GetHighestReadingIgnoringAquifer));
        var helperFiltered = AccessTools.Method(typeof(OreMapComponentColorPatch), nameof(GetFilteredReadingFactorIgnoringAquifer));
        for (int i = 0; i < code.Count; i++)
        {
            if (i > 0 &&
                code[i].Calls(highestReadingGetter) &&
                code[i - 1].opcode == OpCodes.Ldarg_2)
            {
                code[i] = new CodeInstruction(OpCodes.Call, helperHighest);
                continue;
            }
            if (i + 4 < code.Count &&
                code[i].IsLdarg(2) &&
                code[i + 1].LoadsField(oreReadingsField) &&
                code[i + 2].IsLdarg(5) &&
                code[i + 3].Calls(dictItemGetter) &&
                code[i + 4].LoadsField(totalFactorField))
            {
                var first = code[i];
                var arg = code[i + 2];

                code[i] = new CodeInstruction(OpCodes.Ldarg_2)
                {
                    labels = first.labels,
                    blocks = first.blocks
                };

                code[i + 1] = CodeInstruction.LoadArgument(5);

                code[i + 2] = new CodeInstruction(OpCodes.Call, helperFiltered)
                {
                    labels = arg.labels,
                    blocks = arg.blocks
                };

                code[i + 3] = new CodeInstruction(OpCodes.Nop);
                code[i + 4] = new CodeInstruction(OpCodes.Nop);
            }
        }

        return code;
    }

    public static double GetHighestReadingIgnoringAquifer(PropickReading reading)
    {
        if (reading?.OreReadings == null || reading.OreReadings.Count == 0) return 0;

        double max = 0;
        foreach (var kv in reading.OreReadings)
        {
            if (kv.Key == AquiferData.OreReadingKey) continue;
            if (kv.Value == null) continue;

            if (kv.Value.TotalFactor > max)
            {
                max = kv.Value.TotalFactor;
            }
        }
        return max;
    }

    public static double GetFilteredReadingFactorIgnoringAquifer(PropickReading reading, string filterByOreCode)
    {
        if (reading?.OreReadings == null || string.IsNullOrEmpty(filterByOreCode)) return 0;
        if (filterByOreCode == AquiferData.OreReadingKey) return 0;

        return reading.OreReadings.TryGetValue(filterByOreCode, out var ore) && ore != null
            ? ore.TotalFactor
            : 0;
    }
}