using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch(typeof(DrunkPerceptionEffect), "OnBeforeGameRender")]
public static class DrunkPerceptionEffectOnBeforeGameRenderPatch
{
    public static bool Prefix(float dt, DrunkPerceptionEffect __instance)
    {
        var capiField = AccessTools.Field(typeof(DrunkPerceptionEffect), "capi");
        var capi = (ICoreClientAPI)capiField.GetValue(__instance);
        if (HydrateOrDiedrateModSystem.LoadedConfig.DisableDrunkSway)
        {
            var targetIntensityField = AccessTools.Field(typeof(DrunkPerceptionEffect), "targetIntensity");
            targetIntensityField.SetValue(__instance, 0);
            capi.Render.ShaderUniforms.PerceptionEffectIntensity = 0;
            __instance.Intensity = 0;
            return false;
        }
        return true;
    }
}



[HarmonyPatch(typeof(DrunkPerceptionEffect), "ApplyToFpHand")]
public static class DrunkPerceptionEffectApplyToFpHandPatch
{
    public static bool Prefix(Matrixf modelMat, DrunkPerceptionEffect __instance)
    {
        if (HydrateOrDiedrateModSystem.LoadedConfig.DisableDrunkSway)
        {
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(DrunkPerceptionEffect), "ApplyToTpPlayer")]
public static class DrunkPerceptionEffectApplyToTpPlayerPatch
{
    public static bool Prefix(EntityPlayer entityPlr, float[] modelMatrix, DrunkPerceptionEffect __instance, float? playerIntensity = null)
    {
        if (HydrateOrDiedrateModSystem.LoadedConfig.DisableDrunkSway)
        {
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(DrunkPerceptionEffect), "NowActive")]
public static class DrunkPerceptionEffectNowActivePatch
{
    public static bool Prefix(float intensity, DrunkPerceptionEffect __instance)
    {
        if (HydrateOrDiedrateModSystem.LoadedConfig.DisableDrunkSway)
        {
            var capiField = AccessTools.Field(typeof(DrunkPerceptionEffect), "capi");
            var capi = (ICoreClientAPI)capiField.GetValue(__instance);
            __instance.Intensity = 0;
            capi.Render.ShaderUniforms.PerceptionEffectIntensity = 0;

            return false;
        }
        return true;
    }
}
