using HarmonyLib;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Hot_Weather;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch(typeof(CharacterExtraDialogs))]
public static class CharacterExtraDialogsPatch
{
    internal static CachedUIElements cachedElements;

    internal class CachedUIElements
    {
        public GuiComposer composer;
        public GuiElementDynamicText thirstDynamicText;
        public GuiElementDynamicText thirstRateDynamicText;
        public GuiElementHoverText thirstRateHoverText;
        public GuiElementDynamicText nutritionDeficitDynamicText;
        public GuiElementDynamicText coolingDynamicText;
        public GuiElementHoverText coolingHoverText;
        public GuiElementDynamicText hydrationDelayDynamicText;
    }

    private static bool ShouldSkipPatch() => !ModConfig.Instance.Thirst.Enabled; //TODO use patch category instead

    [HarmonyPatch("ComposeStatsGui")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> AddExtraUICall(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);
        
        codeMatcher.MatchStartForward(
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GuiComposer), "Compose", [typeof(bool)]))
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
    
    //TODO maybe create some shared library code for stuff like this
    //TODO support different types of stats and blend/calculation method
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

        var statBounds = thirstRateRightBounds.FlatCopy();
        statBounds.fixedHeight = 20;
        statBounds.fixedWidth /= 3;
        composer.AddAutoSizeHoverText(BuildThirstRateStatsString(capi.World.Player.Entity), CairoFont.WhiteDetailText(), 300, statBounds, "hydrateordiedrate_thirstrate_hover");

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
        const float hydrationDelayVerticalOffset = -10.0f;

        ElementBounds hydrationDelayLeftBounds = nutritionDeficitLeftBounds.BelowCopy(hydrationDelayVerticalOffset)
            .WithFixedPosition(
                nutritionDeficitLeftBounds.fixedX,
                nutritionDeficitLeftBounds.fixedY + nutritionDeficitLeftBounds.fixedHeight + hydrationDelayVerticalOffset
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
        const float coolingVerticalOffset = -10.0f;
        ElementBounds coolingLeftBounds = hydrationDelayLeftBounds.BelowCopy(coolingVerticalOffset)
            .WithFixedPosition(
                hydrationDelayLeftBounds.fixedX,
                hydrationDelayLeftBounds.fixedY + hydrationDelayLeftBounds.fixedHeight + coolingVerticalOffset
            );
        ElementBounds coolingRightBounds = coolingLeftBounds.FlatCopy()
            .WithFixedPosition(
                coolingLeftBounds.fixedX + rightColumnHorizontalOffset,
                coolingLeftBounds.fixedY
            );
        
        var coolingLabelFont = CairoFont.WhiteDetailText().Clone();
        coolingLabelFont.UnscaledFontsize *= 0.95f;

        composer
            .AddStaticText(
                Lang.Get("hydrateordiedrate:itemwearable-cooling"),
                coolingLabelFont,
                coolingLeftBounds,
                "coolingStaticText"
            )
            .AddDynamicText(
                "{0:+0.#;-0.#}°C",
                CairoFont.WhiteDetailText(),
                coolingRightBounds,
                "cooling"
            );

        statBounds = coolingRightBounds.FlatCopy();
        statBounds.fixedHeight = 20;
        statBounds.fixedWidth /= 3;
        composer.AddAutoSizeHoverText(BuildCoolingStatsString(capi.World.Player.Entity), CairoFont.WhiteDetailText(), 300, statBounds, "hydrateordiedrate_cooling_hover");
        CachedUIElements cached = CacheFromComposer(composer);

        cachedElements = cached;
        UpdateDynamicTexts(capi, cached);
    }

    private static CachedUIElements CacheFromComposer(GuiComposer composer)
    {
        return new()
        {
            composer = composer,
            thirstDynamicText = composer.GetDynamicText("hydrateordiedrate_thirst"),
            thirstRateDynamicText = composer.GetDynamicText("hydrateordiedrate_thirstrate"),
            thirstRateHoverText = composer.GetHoverText("hydrateordiedrate_thirstrate_hover"),
            nutritionDeficitDynamicText = composer.GetDynamicText("nutritionDeficit"),
            coolingDynamicText = composer.GetDynamicText("cooling"),
            coolingHoverText = composer.GetHoverText("hydrateordiedrate_cooling_hover"),
            hydrationDelayDynamicText = composer.GetDynamicText("hydrateordiedrate_delay"),
        };
    }

    [HarmonyPatch("UpdateStats")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Low)]
    public static void UpdateStats_Postfix(CharacterExtraDialogs __instance, ICoreClientAPI ___capi, GuiDialogCharacterBase ___dlg)
    {
        if (ShouldSkipPatch() || !___dlg.IsOpened()) return;

        var cached = cachedElements ??= CacheFromComposer(___dlg.Composers["playerstats"]);
        
        UpdateDynamicTexts(___capi, cached);
    }

    private static void UpdateDynamicTexts(ICoreClientAPI capi, CachedUIElements cached)
    {
        var entity = capi.World.Player.Entity;
        var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();

        if (thirstBehavior is not null)
        {
            cached.thirstDynamicText?.SetNewText(
                $"{(int)thirstBehavior.CurrentHydration} / {(int)thirstBehavior.MaxHydration}",
                false, false, false
            );

            float thirstRatePercentage = Math.Max(
                0,
                (thirstBehavior.ThirstRate / ModConfig.Instance.Thirst.ThirstRate) * 100
            );
            cached.thirstRateDynamicText?.SetNewText($"{(int)thirstRatePercentage}%", false, false, false);

            cached.thirstRateHoverText?.SetNewText(BuildThirstRateStatsString(entity));
            cached.coolingHoverText?.SetNewText(BuildCoolingStatsString(entity));

            cached.nutritionDeficitDynamicText?.SetNewText(
                $"{thirstBehavior.NutritionDeficitAmount}",
                false, false, false
            );
        }

        if(entity.GetBehavior<EntityBehaviorBodyTemperatureHot>() is { } bodyTemperatureHotBehavior)
        {
            cached.coolingDynamicText.SetNewText(string.Format("{0:+0.#;-0.#}°C", bodyTemperatureHotBehavior.Cooling * ModConfig.Instance.HeatAndCooling.CoolingTempOffsetPerPoint));
        }

        if (thirstBehavior is not null)
        {
            var seconds = Math.Max(0, thirstBehavior.HydrationLossDelay);
            TimeSpan delayTime = TimeSpan.FromSeconds(seconds);

            cached.hydrationDelayDynamicText?.SetNewText(
                delayTime.ToString(@"hh\:mm\:ss"),
                false, false, false
            );
        }
    }

    public static string BuildThirstRateStatsString(EntityAgent agent)
	{
		StringBuilder sb = new();

		var stats = agent.Stats[HoDStats.ThirstRate];

        sb.AppendFormat("{0}: 100%", Lang.Get("hydrateordiedrate:statkey-base")).AppendLine();

        foreach ((var key, var value) in stats.ValuesByKey)
        {
            if (Math.Abs(value.Value - 1) < float.Epsilon) continue;

            sb.AppendFormat("{0}: x{1:P0}", Lang.Get($"hydrateordiedrate:statkey-{key}"), value.Value).AppendLine();
        }

        return sb.ToString();
	}

    public static string BuildCoolingStatsString(EntityAgent agent)
	{
		StringBuilder sb = new();
        var offsetPerPoint = ModConfig.Instance.HeatAndCooling.CoolingTempOffsetPerPoint;
		var coolingStat = agent.Stats[HoDStats.Cooling];

        foreach ((var key, var value) in coolingStat.ValuesByKey)
        {
            if (Math.Abs(value.Value) < float.Epsilon) continue;

            sb.AppendFormat("{0}: {1:+0.#;-0.#}°C ", Lang.Get($"hydrateordiedrate:statkey-{key}"), value.Value * offsetPerPoint).AppendLine();
        }

        var coolingEfficiencyStat = agent.Stats[HoDStats.CoolingEfficiency];
        bool HasAppendedPrefix = false;
        foreach ((var key, var value) in coolingEfficiencyStat.ValuesByKey)
        {
            if (key == "base" || Math.Abs(value.Value) < float.Epsilon) continue;
            if (!HasAppendedPrefix)
            {
                sb.Append(Lang.Get("hydrateordiedrate:cooling-effciency"));
                sb.AppendLine(":");
                HasAppendedPrefix = true;
            }

            sb.AppendFormat("{0}: {1:+0%;-0%;0%}", Lang.Get($"hydrateordiedrate:statkey-{key}"), value.Value).AppendLine();
        }
        
        return sb.ToString();
	}
}
