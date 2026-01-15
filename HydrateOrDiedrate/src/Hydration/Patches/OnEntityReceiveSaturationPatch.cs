using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
namespace HydrateOrDiedrate.src.Hydration.Patches;



[HarmonyPatch]
public static class EntityBehaviorHungerPatch
{

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(EntityBehaviorHunger), nameof(EntityBehaviorHunger.OnEntityReceiveSaturation));
        
        var method = AccessTools.Method("SmoothDigestion.Behaviors.EntityBehaviorSDHunger:OnEntityReceiveSaturation");
        if(method is not null) yield return method;
    }
    
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var matcher = new CodeMatcher(instructions, generator);

        var negativeNutritionSkip = new Label();
        matcher.MatchEndForward(
            CodeMatch.Calls(AccessTools.PropertySetter(typeof(EntityBehaviorHunger), nameof(EntityBehaviorHunger.Saturation)))
        );

        matcher.InsertAfterAndAdvance(
            CodeInstruction.LoadArgument(0), // this
            CodeInstruction.LoadArgument(1, true), //saturation
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EntityBehaviorHungerPatch), nameof(ApplyDeficit))),
            new CodeInstruction(OpCodes.Brtrue, negativeNutritionSkip)
        );

        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0), // this
            new CodeMatch(code => code.operand is MethodBase method && method.Name == nameof(EntityBehaviorHunger.UpdateNutrientHealthBoost))
        );
        matcher.Labels.Add(negativeNutritionSkip);
        
        return matcher.InstructionEnumeration();
    }

    /// <summary>
    /// Applies deficit
    /// </summary>
    /// <returns>true if saturation is negative and should skip normal logic</returns>
    public static bool ApplyDeficit(EntityBehavior behavior, ref float saturation)
    {
        var thirstBehavior = behavior.entity.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior is null) return false;

        if (saturation < 0)
        {
            float deficitMul = ModConfig.Instance.Thirst.NutritionDeficitMultiplier;
            if (!float.IsFinite(deficitMul) || deficitMul <= 0f)
            {
                deficitMul = 1f;
            }
            
            thirstBehavior.NutritionDeficitAmount += Math.Abs(saturation) * deficitMul;
            return true;
        }

        var nutritionDeficitAmount = thirstBehavior.NutritionDeficitAmount;
        thirstBehavior.NutritionDeficitAmount = Math.Max(0, nutritionDeficitAmount - saturation);

        saturation = Math.Max(0, saturation - nutritionDeficitAmount);

        return false;
    }
}