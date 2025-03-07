using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(CharacterExtraDialogs), "ComposeStatsGui")]
    public static class CharacterExtraDialogs_ComposeStatsGui_Transpiler
    {
        private static readonly MethodInfo ComposeMethod = AccessTools.Method(
            typeof(GuiComposer), "Compose", new Type[] { typeof(bool) });
        private static readonly MethodInfo InjectExtraUiMethod = AccessTools.Method(
            typeof(CharacterExtraDialogs_ComposeStatsGui_Transpiler), nameof(InjectExtraUi));
        private static FieldInfo cachedCapiField;
        private static PropertyInfo cachedComposersProperty;
        private static MethodInfo cachedIsOpenedMethod;
        private static Dictionary<object, CachedUIElements> cachedUIElements = new Dictionary<object, CachedUIElements>();

        private class CachedUIElements
        {
            public GuiComposer composer;
            public dynamic thirstDynamicText;
            public dynamic thirstRateDynamicText;
            public dynamic nutritionDeficitDynamicText;
            public dynamic currentCoolingDynamicText;
            public dynamic hydrationDelayDynamicText;
        }

        private static bool ShouldSkipPatch()
        {
            return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool injected = false;
            foreach (var instruction in instructions)
            {
                if (!injected &&
                    instruction.opcode == OpCodes.Callvirt &&
                    instruction.operand is MethodInfo method &&
                    method == ComposeMethod)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, InjectExtraUiMethod);
                    injected = true;
                }
                yield return instruction;
            }
        }

        private static void InjectExtraUi(object __instance)
        {
            if (ShouldSkipPatch()) return;
            Type type = __instance.GetType();
            FieldInfo capiField = AccessTools.Field(type, "capi");
            ICoreClientAPI capi = (ICoreClientAPI)capiField.GetValue(__instance);
            PropertyInfo composersProp = AccessTools.Property(type, "Composers");
            GuiDialog.DlgComposers composers = (GuiDialog.DlgComposers)composersProp.GetValue(__instance);
            if (composers == null || !composers.ContainsKey("playerstats")) return;
            GuiComposer composer = composers["playerstats"];
            if (composer.GetDynamicText("hydrateordiedrate_thirst") != null) return;
            ElementBounds baseBounds = null;
            var lastElement = composer.GetDynamicText("rangedweaponchargespeed");
            if (lastElement != null)
            {
                baseBounds = lastElement.Bounds;
            }
            if (baseBounds == null)
            {
                baseBounds = ElementBounds.Fixed(20, 350, 140, 20);
            }
            float rightColumnHorizontalOffset = 165.0f;
            float hydrationVerticalOffset = -10.0f;
            float hydrationXOffset = -165.0f;
            ElementBounds hydrationLeftBounds = baseBounds.BelowCopy(hydrationVerticalOffset)
                .WithFixedPosition(
                    baseBounds.fixedX + hydrationXOffset,
                    baseBounds.fixedY + baseBounds.fixedHeight + hydrationVerticalOffset
                );
            ElementBounds hydrationRightBounds = hydrationLeftBounds.FlatCopy()
                .WithFixedPosition(
                    hydrationLeftBounds.fixedX + rightColumnHorizontalOffset,
                    hydrationLeftBounds.fixedY
                );

            composer
                .AddStaticText(
                    Lang.Get("Hydration"),
                    CairoFont.WhiteDetailText(),
                    hydrationLeftBounds,
                    "hydrateordiedrate_thirstStaticText"
                )
                .AddDynamicText(
                    "0 / 1500",
                    CairoFont.WhiteDetailText(),
                    hydrationRightBounds,
                    "hydrateordiedrate_thirst"
                );
            float thirstRateVerticalOffset = -10f;
            float thirstRateXOffset = 0.0f;
            ElementBounds thirstRateLeftBounds = hydrationLeftBounds.BelowCopy(thirstRateVerticalOffset)
                .WithFixedPosition(
                    hydrationLeftBounds.fixedX + thirstRateXOffset,
                    hydrationLeftBounds.fixedY + hydrationLeftBounds.fixedHeight + thirstRateVerticalOffset
                );
            ElementBounds thirstRateRightBounds = thirstRateLeftBounds.FlatCopy()
                .WithFixedPosition(
                    thirstRateLeftBounds.fixedX + rightColumnHorizontalOffset,
                    thirstRateLeftBounds.fixedY
                );
            composer
                .AddStaticText(
                    Lang.Get("Thirst Rate"),
                    CairoFont.WhiteDetailText(),
                    thirstRateLeftBounds,
                    "hydrateordiedrate_thirstRateStaticText"
                )
                .AddDynamicText(
                    "0%",
                    CairoFont.WhiteDetailText(),
                    thirstRateRightBounds,
                    "hydrateordiedrate_thirstrate"
                );
            float nutritionDeficitVerticalOffset = -10.0f;
            float nutritionDeficitXOffset = 0.0f;
            ElementBounds nutritionDeficitLeftBounds = thirstRateLeftBounds.BelowCopy(nutritionDeficitVerticalOffset)
                .WithFixedPosition(
                    thirstRateLeftBounds.fixedX + nutritionDeficitXOffset,
                    thirstRateLeftBounds.fixedY + thirstRateLeftBounds.fixedHeight + nutritionDeficitVerticalOffset
                );
            ElementBounds nutritionDeficitRightBounds = nutritionDeficitLeftBounds.FlatCopy()
                .WithFixedPosition(
                    nutritionDeficitLeftBounds.fixedX + rightColumnHorizontalOffset,
                    nutritionDeficitLeftBounds.fixedY
                );
            composer
                .AddStaticText(
                    Lang.Get("Nutrition Deficit"),
                    CairoFont.WhiteDetailText(),
                    nutritionDeficitLeftBounds,
                    "nutritionDeficitStaticText"
                )
                .AddDynamicText(
                    "0",
                    CairoFont.WhiteDetailText(),
                    nutritionDeficitRightBounds,
                    "nutritionDeficit"
                );
            float currentCoolingVerticalOffset = -10.0f;
            float currentCoolingXOffset = 0.0f;
            ElementBounds currentCoolingLeftBounds = nutritionDeficitLeftBounds.BelowCopy(currentCoolingVerticalOffset)
                .WithFixedPosition(
                    nutritionDeficitLeftBounds.fixedX + currentCoolingXOffset,
                    nutritionDeficitLeftBounds.fixedY + nutritionDeficitLeftBounds.fixedHeight + currentCoolingVerticalOffset
                );
            ElementBounds currentCoolingRightBounds = currentCoolingLeftBounds.FlatCopy()
                .WithFixedPosition(
                    currentCoolingLeftBounds.fixedX + rightColumnHorizontalOffset,
                    currentCoolingLeftBounds.fixedY
                );
            composer
                .AddStaticText(
                    Lang.Get("Current Cooling"),
                    CairoFont.WhiteDetailText(),
                    currentCoolingLeftBounds,
                    "currentCoolingHotStaticText"
                )
                .AddDynamicText(
                    "0",
                    CairoFont.WhiteDetailText(),
                    currentCoolingRightBounds,
                    "currentCoolingHot"
                );
            float hydrationDelayVerticalOffset = -10.0f;
            ElementBounds hydrationDelayLeftBounds = currentCoolingLeftBounds.BelowCopy(hydrationDelayVerticalOffset)
                .WithFixedPosition(
                    currentCoolingLeftBounds.fixedX,
                    currentCoolingLeftBounds.fixedY + currentCoolingLeftBounds.fixedHeight + hydrationDelayVerticalOffset
                );
            ElementBounds hydrationDelayRightBounds = hydrationDelayLeftBounds.FlatCopy()
                .WithFixedPosition(
                    hydrationDelayLeftBounds.fixedX + rightColumnHorizontalOffset,
                    hydrationDelayLeftBounds.fixedY
                );
            composer
                .AddStaticText(
                    Lang.Get("Hydration Delay"),
                    CairoFont.WhiteDetailText(),
                    hydrationDelayLeftBounds,
                    "hydrateordiedrate_delayStaticText"
                )
                .AddDynamicText(
                    "00:00:00",
                    CairoFont.WhiteDetailText(),
                    hydrationDelayRightBounds,
                    "hydrateordiedrate_delay"
                );
            CachedUIElements cached = new CachedUIElements
            {
                composer = composer,
                thirstDynamicText = composer.GetDynamicText("hydrateordiedrate_thirst"),
                thirstRateDynamicText = composer.GetDynamicText("hydrateordiedrate_thirstrate"),
                nutritionDeficitDynamicText = composer.GetDynamicText("nutritionDeficit"),
                currentCoolingDynamicText = composer.GetDynamicText("currentCoolingHot"),
                hydrationDelayDynamicText = composer.GetDynamicText("hydrateordiedrate_delay")
            };
            cachedUIElements[__instance] = cached;
            UpdateDynamicTexts(__instance, cached);
        }
        private static void UpdateDynamicTexts(object __instance, CachedUIElements cached)
        {
            Type type = __instance.GetType();
            FieldInfo capiField = cachedCapiField ?? AccessTools.Field(type, "capi");
            if (cachedCapiField == null) cachedCapiField = capiField;
            ICoreClientAPI capi = (ICoreClientAPI)capiField.GetValue(__instance);

            var entity = capi.World.Player.Entity;

            float currentThirst = entity.WatchedAttributes.GetFloat("currentThirst", 0f);
            float maxThirst = entity.WatchedAttributes.GetFloat("maxThirst", 1500f);
            if (cached.thirstDynamicText != null)
            {
                cached.thirstDynamicText.SetNewText($"{(int)currentThirst} / {(int)maxThirst}", false, false, false);
            }
            float currentThirstRate = entity.WatchedAttributes.GetFloat("thirstRate", 0.01f);
            float normalThirstRate = HydrateOrDiedrateModSystem.LoadedConfig.ThirstDecayRate;
            float currentSpeedOfTime = capi.World.Calendar?.SpeedOfTime ?? 60f;
            float currentCalendarSpeedMul = capi.World.Calendar?.CalendarSpeedMul ?? 0.5f;
            float multiplierPerGameSec = (currentSpeedOfTime / 60f) * (currentCalendarSpeedMul / 0.5f);
            float normalizedThirstRate = currentThirstRate / multiplierPerGameSec;
            float thirstRatePercentage = (normalizedThirstRate / normalThirstRate) * 100;
            thirstRatePercentage = Math.Max(0, thirstRatePercentage);
            if (cached.thirstRateDynamicText != null)
            {
                cached.thirstRateDynamicText.SetNewText($"{(int)thirstRatePercentage}%", false, false, false);
            }
            float hungerReductionAmount = entity.WatchedAttributes.GetFloat("hungerReductionAmount", 0f);
            if (cached.nutritionDeficitDynamicText != null)
            {
                cached.nutritionDeficitDynamicText.SetNewText($"{hungerReductionAmount}", false, false, false);
            }
            float rawCoolingValue = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f);
            if (cached.currentCoolingDynamicText != null)
            {
                cached.currentCoolingDynamicText.SetNewText($"{rawCoolingValue:0.##}", false, false, false);
            }
            int hydrationDelay = entity.WatchedAttributes.GetInt("hydrationLossDelay");
            TimeSpan delayTime = TimeSpan.FromSeconds(hydrationDelay);
            string formattedDelay = $"{delayTime.Hours:D2}:{delayTime.Minutes:D2}:{delayTime.Seconds:D2}";
            if (cached.hydrationDelayDynamicText != null)
            {
                cached.hydrationDelayDynamicText.SetNewText(formattedDelay, false, false, false);
            }
        }

        [HarmonyPatch("UpdateStats")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        public static void UpdateStats_Postfix(object __instance)
        {
            if (ShouldSkipPatch()) return;
            Type type = __instance.GetType();
            if (cachedCapiField == null)
            {
                cachedCapiField = type.GetField("capi", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            ICoreClientAPI capi = (ICoreClientAPI)cachedCapiField.GetValue(__instance);

            if (cachedComposersProperty == null)
            {
                cachedComposersProperty = type.GetProperty("Composers", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            var composers = cachedComposersProperty.GetValue(__instance) as GuiDialog.DlgComposers;
            if (composers == null) return;

            if (cachedIsOpenedMethod == null)
            {
                cachedIsOpenedMethod = type.GetMethod("IsOpened", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            bool isOpened = (bool)cachedIsOpenedMethod.Invoke(__instance, null);
            if (!isOpened) return;

            var compo = composers["playerstats"];
            if (compo == null) return;

            if (!cachedUIElements.TryGetValue(__instance, out var cached))
            {
                cached = new CachedUIElements();
                cached.composer = compo;
                cached.thirstDynamicText = compo.GetDynamicText("hydrateordiedrate_thirst");
                cached.thirstRateDynamicText = compo.GetDynamicText("hydrateordiedrate_thirstrate");
                cached.nutritionDeficitDynamicText = compo.GetDynamicText("nutritionDeficit");
                cached.currentCoolingDynamicText = compo.GetDynamicText("currentCoolingHot");
                cachedUIElements[__instance] = cached;
            }
            
            UpdateDynamicTexts(__instance, cached);
        }
    }
}
