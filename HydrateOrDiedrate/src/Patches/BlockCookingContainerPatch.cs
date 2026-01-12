using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(BlockCookingContainer), "DoSmelt")]
    public static class BlockCookingContainer_DoSmelt_TranspilerPatch
    {
        private static bool IsHoDRecipe(CookingRecipe recipe)
        {
            var code = recipe?.CooksInto?.ResolvedItemstack?.Collectible?.Code?.ToString();
            var recipeCode = recipe?.Code;

            if (!string.IsNullOrEmpty(recipeCode) &&
                recipeCode.StartsWith("hydrateordiedrate:"))
                return true;

            if (!string.IsNullOrEmpty(code) &&
                code.StartsWith("hydrateordiedrate:"))
                return true;

            return false;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var list = new List<CodeInstruction>(instructions);

            var isHoDMethod = AccessTools.Method(
                typeof(BlockCookingContainer_DoSmelt_TranspilerPatch),
                nameof(IsHoDRecipe)
            );

            if (isHoDMethod == null)
                return list;

            for (int i = 0; i < list.Count - 2; i++)
            {
                if (list[i].opcode != OpCodes.Ldloc_1)
                    continue;

                if (list[i + 1].opcode != OpCodes.Ldfld)
                    continue;

                if (list[i + 1].operand is not FieldInfo fi || fi.Name != "IsFood")
                    continue;

                if (list[i + 2].opcode != OpCodes.Brtrue_S &&
                    list[i + 2].opcode != OpCodes.Brtrue)
                    continue;

                var skipLabel = (Label)list[i + 2].operand;

                int insertIndex = i + 3;

                var newInstructions = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Call, isHoDMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, skipLabel)
                };

                list.InsertRange(insertIndex, newInstructions);
                break;
            }

            return list;
        }
    }
}