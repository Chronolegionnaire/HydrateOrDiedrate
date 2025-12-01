using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HydrateOrDiedrate.Config;
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
        public GuiElementDynamicText coolingDynamicText;
        public GuiElementDynamicText hydrationDelayDynamicText;
        public GuiElementRichtext coolingBonusesRichText;
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
                Lang.Get("hydrateordiedrate:characterextradialogs-cooling"),
                coolingLabelFont,
                coolingLeftBounds,
                "coolingStaticText"
            )
            .AddDynamicText(
                "0 | 0",
                CairoFont.WhiteDetailText(),
                coolingRightBounds,
                "cooling"
            );

        const float bonusesVerticalOffset = -16.0f;

        ElementBounds bonusesBounds = coolingLeftBounds.BelowCopy(bonusesVerticalOffset)
            .WithFixedPosition(
                coolingLeftBounds.fixedX + 10, 
                coolingLeftBounds.fixedY + 1 + coolingLeftBounds.fixedHeight + bonusesVerticalOffset
            )
            .WithFixedWidth(160.0)
            .WithFixedHeight(26.0);

        composer.AddRichtext(
            "",
            CairoFont.WhiteDetailText(),
            bonusesBounds,
            "hydrateordiedrate_coolingBonuses"
        );

        
        CachedUIElements cached = new CachedUIElements
        {
            composer = composer,
            thirstDynamicText = composer.GetDynamicText("hydrateordiedrate_thirst"),
            thirstRateDynamicText = composer.GetDynamicText("hydrateordiedrate_thirstrate"),
            nutritionDeficitDynamicText = composer.GetDynamicText("nutritionDeficit"),
            coolingDynamicText = composer.GetDynamicText("cooling"),
            hydrationDelayDynamicText = composer.GetDynamicText("hydrateordiedrate_delay"),
            coolingBonusesRichText = composer.GetRichtext("hydrateordiedrate_coolingBonuses")
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
                coolingDynamicText = compo.GetDynamicText("cooling"),
                hydrationDelayDynamicText = compo.GetDynamicText("hydrateordiedrate_delay"),
                coolingBonusesRichText = compo.GetRichtext("hydrateordiedrate_coolingBonuses")
            };
            cachedUIElements[__instance] = cached;
        }
        
        UpdateDynamicTexts(___capi, cached);
    }

    private static void UpdateDynamicTexts(ICoreClientAPI capi, CachedUIElements cached)
    {
        var entity = capi.World.Player.Entity;
        var thirstBehavior = entity.GetBehavior<EntityBehaviorThirst>();

        if (thirstBehavior is not null)
        {
            cached.thirstDynamicText?.SetNewText(
                $"{(int)thirstBehavior.CurrentThirst} / {(int)thirstBehavior.MaxThirst}",
                false, false, false
            );

            float thirstRatePercentage = Math.Max(
                0,
                (thirstBehavior.ThirstRate / ModConfig.Instance.Thirst.ThirstDecayRate) * 100
            );
            cached.thirstRateDynamicText?.SetNewText($"{(int)thirstRatePercentage}%", false, false, false);

            cached.nutritionDeficitDynamicText?.SetNewText(
                $"{thirstBehavior.NutritionDeficitAmount}",
                false, false, false
            );
        }

        var hodCooling = entity.WatchedAttributes.GetTreeAttribute("hodCooling");
        if (hodCooling != null)
        {
            float gearCooling = hodCooling.GetFloat("gearCooling", 0f);
            float totalCooling = hodCooling.GetFloat("totalCooling", 0f);
            cached.coolingDynamicText?.SetNewText(
                $"{gearCooling:0.##} | {totalCooling:0.##}",
                false, false, false
            );

            int wetBonus    = hodCooling.GetInt("wetBonus", 0);
            int roomBonus   = hodCooling.GetInt("roomBonus", 0);
            int lowSunBonus = hodCooling.GetInt("lowSunBonus", 0);
            int shadeBonus  = hodCooling.GetInt("shadeBonus", 0);
            var line1Parts = new List<string>();
            if (wetBonus != 0)
                line1Parts.Add(Lang.Get("hydrateordiedrate:coolingbonus-wet"));
            if (roomBonus != 0)
                line1Parts.Add(Lang.Get("hydrateordiedrate:coolingbonus-room"));
            var line2Parts = new List<string>();
            if (lowSunBonus != 0)
                line2Parts.Add(Lang.Get("hydrateordiedrate:coolingbonus-lowsun"));
            if (shadeBonus != 0)
                line2Parts.Add(Lang.Get("hydrateordiedrate:coolingbonus-shade"));
            var lines = new List<string>();
            if (line1Parts.Count > 0)
                lines.Add(string.Join(" ", line1Parts));
            if (line2Parts.Count > 0)
                lines.Add(string.Join(" ", line2Parts));

            string vtml = "";
            if (lines.Count > 0)
            {
                string text = string.Join("<br>", lines);
                vtml = $"<font color=\"#1097b4\" size=\"9\">{text}</font>";
            }

            cached.coolingBonusesRichText?.SetNewText(
                vtml,
                CairoFont.WhiteDetailText()
            );
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
}
