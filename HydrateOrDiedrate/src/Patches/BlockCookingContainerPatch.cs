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
            var code = recipe?.CooksInto?.ResolvedItemstack?.Collectible?.Code;
            var recipeCode = recipe?.Code;

            if (!string.IsNullOrEmpty(recipeCode) &&
                recipeCode.StartsWith("hydrateordiedrate:"))
                return true;

            if (code is not null && code.Domain == "hydrateordiedrate") 
                return true;

            return false;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var isHoDMethod = AccessTools.Method(
                typeof(BlockCookingContainer_DoSmelt_TranspilerPatch),
                nameof(IsHoDRecipe)
            );

            if (isHoDMethod is null)
                return instructions;

            var matcher = new CodeMatcher(instructions, generator);
    
            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(code => code.opcode == OpCodes.Ldfld && code.operand is FieldInfo field && field.Name == "IsFood"),
                new CodeMatch(code => code.opcode == OpCodes.Brtrue || code.opcode == OpCodes.Brtrue_S)
            );

            if (matcher.IsInvalid)
            {
                return matcher.InstructionEnumeration();
            }
            var skipLabel = (Label)matcher.Instruction.operand;

            matcher.InsertAfter(
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, isHoDMethod),
                new CodeInstruction(OpCodes.Brtrue_S, skipLabel)
            );

            return matcher.InstructionEnumeration();
        }
    }
}