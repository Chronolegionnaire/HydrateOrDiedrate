using System;
using System.Reflection;
using Cairo;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(CharacterExtraDialogs))]
    public static class CharacterExtraDialogs_Patch
    {
        private static bool ShouldSkipPatch()
        {
            return !HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics;
        }

        [HarmonyPatch("ComposeStatsGui")]
        [HarmonyPrefix]
        public static bool ComposeStatsGui_Prefix(object __instance)
        {
            if (ShouldSkipPatch())
            {
                return true;
            }
            try
            {
                Type type = __instance.GetType();
                FieldInfo capiField = type.GetField("capi", BindingFlags.NonPublic | BindingFlags.Instance);
                ICoreClientAPI capi = (ICoreClientAPI)capiField.GetValue(__instance);
                PropertyInfo composersProperty = type.GetProperty("Composers", BindingFlags.NonPublic | BindingFlags.Instance);
                var composers = composersProperty.GetValue(__instance) as GuiDialog.DlgComposers;
                FieldInfo dlgField = type.GetField("dlg", BindingFlags.NonPublic | BindingFlags.Instance);
                var dlg = dlgField.GetValue(__instance);
                MethodInfo onTitleBarCloseMethod = dlg.GetType().GetMethod("OnTitleBarClose", BindingFlags.NonPublic | BindingFlags.Instance);

                var entity = capi.World.Player.Entity;

                ElementBounds leftDlgBounds = composers["playercharacter"].Bounds;
                ElementBounds bounds = composers["environment"].Bounds;
                ElementBounds leftColumnBounds = ElementBounds.Fixed(0.0, 25.0, 90.0, 20.0);
                ElementBounds rightColumnBounds = ElementBounds.Fixed(120.0, 30.0, 120.0, 8.0);
                ElementBounds leftColumnBoundsW = ElementBounds.Fixed(0.0, 0.0, 140.0, 20.0);
                ElementBounds rightColumnBoundsW = ElementBounds.Fixed(165.0, 0.0, 120.0, 20.0);

                double b = bounds.InnerHeight / (double)RuntimeEnv.GUIScale + 10.0;
                ElementBounds bgBounds = ElementBounds.Fixed(0.0, 0.0, 235.0, leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + b).WithFixedPadding(GuiStyle.ElementToDialogPadding);
                ElementBounds dialogBounds = bgBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0).WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset((leftDlgBounds.renderX + leftDlgBounds.OuterWidth + 10.0) / (double)RuntimeEnv.GUIScale, b / 2.0);

                float? health = null;
                float? maxhealth = null;
                float? saturation = null;
                float? maxsaturation = null;
                getHealthSat(entity, out health, out maxhealth, out saturation, out maxsaturation);

                float walkspeed = entity.Stats.GetBlended("walkspeed");
                float healingEffectivness = entity.Stats.GetBlended("healingeffectivness");
                float hungerRate = entity.Stats.GetBlended("hungerrate");
                float rangedWeaponAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
                float rangedWeaponSpeed = entity.Stats.GetBlended("rangedWeaponsSpeed");
                ITreeAttribute tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");
                float wetness = entity.WatchedAttributes.GetFloat("wetness", 0f);
                string wetnessString = getWetnessString(wetness);

                composers["playerstats"] = capi.Gui.CreateCompo("playerstats", dialogBounds)
                    .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
                    .AddDialogTitleBar(Lang.Get("Stats"), () =>
                    {
                        var tryCloseMethod = dlg.GetType().GetMethod("TryClose", BindingFlags.Public | BindingFlags.Instance);
                        if (tryCloseMethod != null)
                        {
                            tryCloseMethod.Invoke(dlg, null);
                        }
                        else
                        {
                            var isOpenedField = dlg.GetType().GetField("isOpened", BindingFlags.NonPublic | BindingFlags.Instance);
                            var callbackIdField = dlg.GetType().GetField("callbackId", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (isOpenedField != null && callbackIdField != null)
                            {
                                isOpenedField.SetValue(dlg, false);
                                long callbackId = (long)callbackIdField.GetValue(dlg);
                                capi.Event.UnregisterCallback(callbackId);
                            }
                        }
                    }, null, null)
                    .BeginChildElements(bgBounds);

                if (saturation != null)
                {
                    addNutritionSection(composers["playerstats"], ref leftColumnBounds, ref rightColumnBounds);
                    leftColumnBoundsW = leftColumnBoundsW.FixedUnder(leftColumnBounds, -5.0);
                }

                composers["playerstats"].AddStaticText(Lang.Get("Physical", Array.Empty<object>()), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), leftColumnBoundsW.WithFixedWidth(200.0).WithFixedOffset(0.0, 23.0), null).Execute(delegate
                {
                    leftColumnBoundsW = leftColumnBoundsW.FlatCopy();
                    leftColumnBoundsW.fixedY += 5.0;
                });

                addPhysicalStats(composers["playerstats"], entity, health, maxhealth, saturation, maxsaturation, tempTree, wetnessString, walkspeed, healingEffectivness, hungerRate, rangedWeaponAcc, rangedWeaponSpeed, ref leftColumnBoundsW, ref rightColumnBoundsW);
                
                if (saturation != null)
                {
                    var hydrationStaticBounds = leftColumnBoundsW.BelowCopy(fixedDeltaY: 0.0);
                    var hydrationBounds = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, hydrationStaticBounds.fixedY);

                    composers["playerstats"].AddStaticText(Lang.Get("Hydration"),
                        CairoFont.WhiteDetailText(), 
                        hydrationStaticBounds, 
                        "hydrateordiedrate_thirstStaticText")
                    .AddDynamicText("0 / 1500", 
                        CairoFont.WhiteDetailText(), 
                        hydrationBounds, 
                        "hydrateordiedrate_thirst");

                    leftColumnBoundsW = hydrationStaticBounds.BelowCopy(fixedDeltaY: 0.0);
                }

                if (saturation != null)
                {
                    var thirstRateStaticBounds = leftColumnBoundsW.BelowCopy(fixedDeltaY: -20.0); 
                    var thirstRateBounds = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, thirstRateStaticBounds.fixedY);

                    composers["playerstats"].AddStaticText(Lang.Get("Thirst Rate"), 
                        CairoFont.WhiteDetailText(), 
                        thirstRateStaticBounds, 
                        "hydrateordiedrate_thirstRateStaticText")
                    .AddDynamicText("0%", 
                        CairoFont.WhiteDetailText(), 
                        thirstRateBounds, 
                        "hydrateordiedrate_thirstrate");
                }
                var nutritionDeficitStaticBounds = leftColumnBoundsW.BelowCopy(fixedDeltaY: 1.0);
                var nutritionDeficitBounds = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, nutritionDeficitStaticBounds.fixedY);

                composers["playerstats"].AddStaticText(Lang.Get("Nutrition Deficit"), 
                        CairoFont.WhiteDetailText(), 
                        nutritionDeficitStaticBounds, 
                        "nutritionDeficitStaticText")
                    .AddDynamicText("0", 
                        CairoFont.WhiteDetailText(), 
                        nutritionDeficitBounds, 
                        "nutritionDeficit");
                
                composers["playerstats"].EndChildElements().Compose(true);
                MethodInfo updateStatBarsMethod = type.GetMethod("UpdateStatBars", BindingFlags.NonPublic | BindingFlags.Instance);
                updateStatBarsMethod.Invoke(__instance, null);

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        [HarmonyPatch("UpdateStats")]
        [HarmonyPostfix]
        public static void UpdateStats_Postfix(object __instance)
        {
            if (ShouldSkipPatch())
            {
                return;
            }
            Type type = __instance.GetType();
            FieldInfo capiField = type.GetField("capi", BindingFlags.NonPublic | BindingFlags.Instance);
            ICoreClientAPI capi = (ICoreClientAPI)capiField.GetValue(__instance);
            PropertyInfo composersProperty =
                type.GetProperty("Composers", BindingFlags.NonPublic | BindingFlags.Instance);
            var composers = composersProperty.GetValue(__instance) as GuiDialog.DlgComposers;
            MethodInfo isOpenedMethod = type.GetMethod("IsOpened", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isOpened = (bool)isOpenedMethod.Invoke(__instance, null);

            if (!isOpened)
            {
                return;
            }

            var entity = capi.World.Player.Entity;
            var compo = composers?["playerstats"];
            if (compo == null)
            {
                return;
            }

            float currentThirst = entity.WatchedAttributes.GetFloat("currentThirst", 0f);
            float maxThirst = entity.WatchedAttributes.GetFloat("maxThirst", 1500f);
            float thirstPenalty = entity.WatchedAttributes.GetFloat("thirstPenalty", 0f);
            float currentThirstRate = entity.WatchedAttributes.GetFloat("thirstRate", 0.01f);
            float normalThirstRate = entity.WatchedAttributes.GetFloat("normalThirstRate", 0.01f);
            float thirstRatePercentage = (currentThirstRate / normalThirstRate) * 100;
            thirstRatePercentage = Math.Max(0, thirstRatePercentage);
            var thirstDynamicText = compo.GetDynamicText("hydrateordiedrate_thirst");
            if (thirstDynamicText != null)
            {
                thirstDynamicText.SetNewText($"{(int)currentThirst} / {(int)maxThirst}", false, false, false);
            }

            var thirstRateDynamicText = compo.GetDynamicText("hydrateordiedrate_thirstrate");
            if (thirstRateDynamicText != null)
            {
                thirstRateDynamicText.SetNewText($"{(int)thirstRatePercentage}%", false, false, false);
            }
            float hungerReductionAmount = entity.WatchedAttributes.GetFloat("hungerReductionAmount", 0f);
            var nutritionDeficitDynamicText = compo.GetDynamicText("nutritionDeficit");
            if (nutritionDeficitDynamicText != null)
            {
                nutritionDeficitDynamicText.SetNewText($"{hungerReductionAmount}", false, false, false);
            }

            float walkSpeed = entity.Stats.GetBlended("walkspeed");
            float effectiveWalkSpeed = walkSpeed - thirstPenalty;
            var walkSpeedDynamicText = compo.GetDynamicText("walkspeed");
            if (walkSpeedDynamicText != null)
            {
                walkSpeedDynamicText.SetNewText($"{(int)Math.Round(effectiveWalkSpeed * 100)}%", false, false,
                    false);
            }
        }

        private static void getHealthSat(EntityPlayer entity, out float? health, out float? maxHealth, out float? saturation, out float? maxSaturation)
        {
            health = null;
            maxHealth = null;
            saturation = null;
            maxSaturation = null;
            ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
            if (healthTree != null)
            {
                health = healthTree.TryGetFloat("currenthealth");
                maxHealth = healthTree.TryGetFloat("maxhealth");
            }
            if (health != null)
            {
                health = new float?((float)Math.Round((double)health.Value, 1));
            }
            if (maxHealth != null)
            {
                maxHealth = new float?((float)Math.Round((double)maxHealth.Value, 1));
            }
            ITreeAttribute hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree != null)
            {
                saturation = hungerTree.TryGetFloat("currentsaturation");
                maxSaturation = hungerTree.TryGetFloat("maxsaturation");
            }
            if (saturation != null)
            {
                saturation = new float?((float)((int)saturation.Value));
            }
        }

        private static string getWetnessString(float wetness)
        {
            if ((double)wetness > 0.7)
            {
                return Lang.Get("wetness_soakingwet", Array.Empty<object>());
            }
            else if ((double)wetness > 0.4)
            {
                return Lang.Get("wetness_wet", Array.Empty<object>());
            }
            else if ((double)wetness > 0.1)
            {
                return Lang.Get("wetness_slightlywet", Array.Empty<object>());
            }
            return string.Empty;
        }

        private static void addNutritionSection(GuiComposer compo, ref ElementBounds leftColumnBounds, ref ElementBounds rightColumnBounds)
        {
            compo.AddStaticText(Lang.Get("playerinfo-nutrition", Array.Empty<object>()), CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), leftColumnBounds.WithFixedWidth(200.0), null)
                .AddStaticText(Lang.Get("playerinfo-nutrition-Freeza", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedWidth(90.0), null)
                .AddStaticText(Lang.Get("playerinfo-nutrition-Vegita", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddStaticText(Lang.Get("playerinfo-nutrition-Krillin", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddStaticText(Lang.Get("playerinfo-nutrition-Cell", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddStaticText(Lang.Get("playerinfo-nutrition-Dairy", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBounds = leftColumnBounds.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 16.0, 0.0, 0.0), GuiStyle.FoodBarColor, "fruitBar")
                .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0, 0.0, 0.0), GuiStyle.FoodBarColor, "vegetableBar")
                .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0, 0.0, 0.0), GuiStyle.FoodBarColor, "grainBar")
                .AddStatbar(rightColumnBounds = rightColumnBounds.BelowCopy(0.0, 12.0, 0.0, 0.0), GuiStyle.FoodBarColor, "proteinBar")
                .AddStatbar(rightColumnBounds.BelowCopy(0.0, 12.0, 0.0, 0.0), GuiStyle.FoodBarColor, "dairyBar");
        }

        private static void addPhysicalStats(GuiComposer compo, EntityPlayer entity, float? health, float? maxhealth, float? saturation, float? maxsaturation, ITreeAttribute tempTree, string wetnessString, float walkspeed, float healingEffectivness, float hungerRate, float rangedWeaponAcc, float rangedWeaponSpeed, ref ElementBounds leftColumnBoundsW, ref ElementBounds rightColumnBoundsW)
        {
            if (health != null)
            {
                GuiComposer composer = compo.AddStaticText(Lang.Get("Health Points", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null);
                float? num = health;
                string str = num.ToString();
                string str2 = " / ";
                num = maxhealth;
                composer.AddDynamicText(str + str2 + num.ToString(), CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY).WithFixedHeight(30.0), "health");
            }
            if (saturation != null)
            {
                compo.AddStaticText(Lang.Get("Satiety", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                    .AddDynamicText(((int)saturation.Value).ToString() + " / " + ((int)maxsaturation.Value).ToString(), CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "satiety");
            }
            if (tempTree != null)
            {
                compo.AddStaticText(Lang.Get("Body Temperature", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                    .AddRichtext((tempTree == null) ? "-" : getBodyTempText(tempTree), CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "bodytemp");
            }
            if (wetnessString.Length > 0)
            {
                compo.AddRichtext(wetnessString, CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null);
            }
            compo.AddStaticText(Lang.Get("Walk speed", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddDynamicText(((int)Math.Round((double)(100f * walkspeed))).ToString() + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "walkspeed")
                .AddStaticText(Lang.Get("Healing effectivness", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddDynamicText(((int)Math.Round((double)(100f * healingEffectivness))).ToString() + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "healeffectiveness");
            if (saturation != null)
            {
                compo.AddStaticText(Lang.Get("Hunger rate", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                    .AddDynamicText(((int)Math.Round((double)(100f * hungerRate))).ToString() + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "hungerrate");
            }
            compo.AddStaticText(Lang.Get("Ranged Accuracy", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddDynamicText(((int)Math.Round((double)(100f * rangedWeaponAcc))).ToString() + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW = rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "rangedweaponacc")
                .AddStaticText(Lang.Get("Ranged Charge Speed", Array.Empty<object>()), CairoFont.WhiteDetailText(), leftColumnBoundsW = leftColumnBoundsW.BelowCopy(0.0, 0.0, 0.0, 0.0), null)
                .AddDynamicText(((int)Math.Round((double)(100f * rangedWeaponSpeed))).ToString() + "%", CairoFont.WhiteDetailText(), rightColumnBoundsW.FlatCopy().WithFixedPosition(rightColumnBoundsW.fixedX, leftColumnBoundsW.fixedY), "rangedweaponchargespeed");
        }

        private static string getBodyTempText(ITreeAttribute tempTree)
        {
            float baseTemp = tempTree.GetFloat("bodytemp", 0f);
            if (baseTemp > 37f)
            {
                baseTemp = 37f + (baseTemp - 37f) / 10f;
            }
            return string.Format("{0:0.#}°C", baseTemp);
        }
    }
}
