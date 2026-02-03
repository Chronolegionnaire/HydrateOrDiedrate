// BodyTempGradientPatch.cs
// Single-file drop-in: transpiler replaces EntityBehaviorBodyTemperature.tempChange calculation
// with a custom gradient that breaks at 27°C and nudges toward 36.5°C.
// Harmony 2.3.6 compatible (no MatchForward, no setting CodeMatcher.Pos).

#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.HotWeather.Patches
{
    [HarmonyPatch(typeof(EntityBehaviorBodyTemperature), nameof(EntityBehaviorBodyTemperature.OnGameTick))]
    public static class BodyTempGradientPatch
    {
        private const float TargetBodyTemp = 36.5f;
        private const float BreakpointAmbient = 27f;

        private const float EnvGainScale = 1f / 6f;
        private const float EnvClamp = 6f;
        private const float HomeostasisRate = 0.08f;
        private const float HomeostasisClamp = 0.25f;
        private const float EnclosedRoomBias = 0.25f;

        private const int LOC_WINDSPEED = 8;
        private const int LOC_RAINEXPOSED = 9;
        private const int LOC_HERETEMPERATURE = 13;

        private static readonly MethodInfo MI_Compute =
            AccessTools.Method(typeof(BodyTempGradientPatch), nameof(ComputeTempChange))
            ?? throw new MissingMethodException(nameof(BodyTempGradientPatch), nameof(ComputeTempChange));

        private static readonly FieldInfo FI_TempChange =
            AccessTools.Field(typeof(EntityBehaviorBodyTemperature), "tempChange")
            ?? throw new MissingFieldException(typeof(EntityBehaviorBodyTemperature).FullName, "tempChange");
        private static readonly AccessTools.FieldRef<EntityBehaviorBodyTemperature, bool> FR_InEnclosedRoom =
            AccessTools.FieldRefAccess<EntityBehaviorBodyTemperature, bool>("inEnclosedRoom");

        private static readonly AccessTools.FieldRef<EntityBehaviorBodyTemperature, float> FR_NearHeatSourceStrength =
            AccessTools.FieldRefAccess<EntityBehaviorBodyTemperature, float>("nearHeatSourceStrength");

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var finder = new CodeMatcher(codes, generator)
                .Start()
                .MatchStartForward(new CodeMatch(ci =>
                    ci.opcode == OpCodes.Stfld &&
                    ci.operand is FieldInfo fi &&
                    fi.Name == "tempChange"))
                .ThrowIfInvalid("BodyTempGradientPatch: Could not find stfld tempChange");

            int endPos = finder.Pos;
            finder
                .SearchBackwards(ci => ci.IsStloc())
                .ThrowIfInvalid("BodyTempGradientPatch: Could not locate start of tempChange block");

            int startPos = finder.Pos + 1;

            if (startPos < 0 || startPos > endPos || endPos >= codes.Count)
                throw new InvalidOperationException($"BodyTempGradientPatch: computed invalid range start={startPos} end={endPos} count={codes.Count}");
            int removeCount = endPos - startPos + 1;
            codes.RemoveRange(startPos, removeCount);
            var injected = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_S, (byte)LOC_WINDSPEED),
                new CodeInstruction(OpCodes.Ldloc_S, (byte)LOC_HERETEMPERATURE),
                new CodeInstruction(OpCodes.Ldloc_S, (byte)LOC_RAINEXPOSED),
                new CodeInstruction(OpCodes.Call, MI_Compute),
                new CodeInstruction(OpCodes.Stfld, FI_TempChange),
            };

            codes.InsertRange(startPos, injected);

            return codes;
        }

        public static float ComputeTempChange(
            EntityBehaviorBodyTemperature self,
            Vec3d windspeed,
            float hereTemperature,
            bool rainExposed
        )
        {
            float envDelta = hereTemperature - BreakpointAmbient;
            float envEffect = GameMath.Clamp(envDelta * EnvGainScale, -EnvClamp, EnvClamp);
            float windCooling = 0f;
            if (!FR_InEnclosedRoom(self))
            {
                double w = Math.Max((windspeed.Length() - 0.15) * 2.0, 0.0);
                windCooling = (float)(-w);
            }
            float heat = FR_NearHeatSourceStrength(self);
            float coolingBonus = GetCoolingBonus(self);
            float coolingEffect = (hereTemperature > BreakpointAmbient) ? -coolingBonus : 0f;
            float rainEffect = 0f;
            float bodyDelta = TargetBodyTemp - self.CurBodyTemperature;
            float homeostasis = GameMath.Clamp(bodyDelta * HomeostasisRate, -HomeostasisClamp, HomeostasisClamp);
            float roomBias = FR_InEnclosedRoom(self) ? EnclosedRoomBias : 0f;

            float tempChange =
                envEffect
                + windCooling
                + heat
                + coolingEffect
                + rainEffect
                + roomBias
                + homeostasis;

            return tempChange;
        }

        private static float GetCoolingBonus(EntityBehaviorBodyTemperature self)
        {
            // If you store cooling somewhere (recommended: watched attr), read it here:
            // return self.entity?.WatchedAttributes?.GetFloat("coolingBonus", 0f) ?? 0f;
            return 0f;
        }
    }
}
