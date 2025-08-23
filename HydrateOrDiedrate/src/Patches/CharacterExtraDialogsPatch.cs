using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Hot_Weather;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch(typeof(CharacterExtraDialogs))]
public static class CharacterExtraDialogsPatch
{
    private static Dictionary<object, CachedUIElements> cachedUIElements = new Dictionary<object, CachedUIElements>();

    private class CachedUIElements
    {
        public GuiComposer composer;
        public GuiElementDynamicText thirstDynamicText;
        public GuiElementDynamicText thirstRateDynamicText;
        public GuiElementDynamicText nutritionDeficitDynamicText;
        public GuiElementDynamicText currentCoolingDynamicText;
        public GuiElementDynamicText hydrationDelayDynamicText;
    }

    private static bool ShouldSkipPatch() => !ModConfig.Instance.Thirst.Enabled; //TODO use patch category instead

    [HarmonyPatch("ComposeStatsGui")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AddExtraUICall(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);
        
        codeMatcher.MatchStartForward(
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GuiComposer), "Compose", new Type[] { typeof(bool) }))
        );

        codeMatcher.Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CharacterExtraDialogs), "capi")),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(CharacterExtraDialogs), "Composers")),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterExtraDialogsPatch), nameof(ExtraUI)))
        );

        return codeMatcher.InstructionEnumeration();
    }

    private static void ExtraUI(CharacterExtraDialogs __instance, ICoreClientAPI capi, GuiDialog.DlgComposers composers)
    {
        if (ShouldSkipPatch() || composers is null || !composers.ContainsKey("playerstats")) return;

        GuiComposer composer = composers["playerstats"];
        if (composer.GetDynamicText("hydrateordiedrate_thirst") is not null) return;

        ElementBounds baseBounds = composer.GetDynamicText("rangedweaponchargespeed")?.Bounds ?? ElementBounds.Fixed(20, 350, 140, 20);

        const float rightColumnHorizontalOffset = 165.0f;
        const float hydrationVerticalOffset = -10.0f;
        const float hydrationXOffset = -165.0f;
        
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
                Lang.Get("hydrateordiedrate:characterextradialogs-hydration"),
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

        const float thirstRateVerticalOffset = -10f;
        const float thirstRateXOffset = 0.0f;
        
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
                Lang.Get("hydrateordiedrate:characterextradialogs-thirstrate"),
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

        const float nutritionDeficitVerticalOffset = -10.0f;
        const float nutritionDeficitXOffset = 0.0f;

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
                Lang.Get("hydrateordiedrate:characterextradialogs-nutritiondeficit"),
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

        const float currentCoolingVerticalOffset = -10.0f;
        const float currentCoolingXOffset = 0.0f;
        
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
                Lang.Get("hydrateordiedrate:characterextradialogs-currentcooling"),
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
        
        const float hydrationDelayVerticalOffset = -10.0f;
        
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
                Lang.Get("hydrateordiedrate:characterextradialogs-hydrationdelay"),
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
        
        cachedUIElements[__instance] = cached; //TODO check if this is propperly disposed
        UpdateDynamicTexts(capi, cached);
    }

    [HarmonyPatch("UpdateStats")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void UpdateStats_Postfix(CharacterExtraDialogs __instance, ICoreClientAPI ___capi, GuiDialogCharacterBase ___dlg)
    {
        if (ShouldSkipPatch() || !___dlg.IsOpened()) return;

        if (!cachedUIElements.TryGetValue(__instance, out var cached))
        {
            var compo = ___dlg.Composers["playerstats"];
            cached = new CachedUIElements
            {
                composer = compo,
                thirstDynamicText = compo.GetDynamicText("hydrateordiedrate_thirst"),
                thirstRateDynamicText = compo.GetDynamicText("hydrateordiedrate_thirstrate"),
                nutritionDeficitDynamicText = compo.GetDynamicText("nutritionDeficit"),
                currentCoolingDynamicText = compo.GetDynamicText("currentCoolingHot")
            };
            cachedUIElements[__instance] = cached;
        }
        
        UpdateDynamicTexts(___capi, cached);
    }

    private static void UpdateDynamicTexts(ICoreClientAPI capi, CachedUIElements cached)
    {
        var entity = capi.World.Player.Entity;
        var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();

        if(thirstBehavior is not null)
        {
            cached.thirstDynamicText?.SetNewText($"{(int)thirstBehavior.CurrentThirst} / {(int)thirstBehavior.MaxThirst}", false, false, false);

            float thirstRatePercentage = Math.Max(0, (thirstBehavior.ThirstRate / ModConfig.Instance.Thirst.ThirstDecayRate) * 100);
            cached.thirstRateDynamicText?.SetNewText($"{(int)thirstRatePercentage}%", false, false, false);

            cached.nutritionDeficitDynamicText?.SetNewText($"{thirstBehavior.HungerReductionAmount}", false, false, false);
        }

        var tempBehavior = entity.GetBehavior<EntityBehaviorBodyTemperatureHot>();
        
        if(tempBehavior is not null)
        {
            cached.currentCoolingDynamicText?.SetNewText($"{tempBehavior.Cooling:0.##}", false, false, false);
        }

        if(thirstBehavior is not null)
        {
            TimeSpan delayTime = TimeSpan.FromSeconds(thirstBehavior.HydrationLossDelay);
            cached.hydrationDelayDynamicText?.SetNewText(delayTime.ToString(@"hh\:mm\:ss"), false, false, false);
        }
    }
}
