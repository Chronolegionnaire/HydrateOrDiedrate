using HarmonyLib;
using HydrateOrDiedrate.Hot_Weather;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    public static class CollectibleBehaviorWearablePatches
    {
        public const string FONT_CLOSE_TAG = "</font>";

        // --- GetHeldItemInfo -------------------------------------------------

        [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetHeldItemInfo))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeTemperatureStats(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            // We want to:
            // 1) Make sure "condition exists" logic also triggers when Cooling exists (like you did before)
            // 2) Replace the vanilla "Condition + Warmth" line + "clothing-maxwarmth" line with your custom output

            AppendTheCoolingChecks(matcher);

            // Find the part where vanilla prints "+{0:0.#}°C" (warmth display) and the "clothing-maxwarmth" line after it.
            // We'll remove that block and replace with our AppendInfo call.

            // Anchor on the warmth format token
            matcher.MatchEndForward(CodeMatch.LoadsConstant("+{0:0.#}°C"));
            if (!matcher.IsValid) return instructions; // fail-safe

            // Back up to the warmth threshold compare (0.05) to remove the full vanilla warmth display block.
            matcher.MatchEndBackwards(CodeMatch.LoadsConstant(0.05d));
            int removalStart = matcher.Pos - 1;

            // Now go forward to the maxwarmth line append (Lang.Get("clothing-maxwarmth", ...))
            matcher.MatchEndForward(CodeMatch.LoadsConstant("clothing-maxwarmth"));
            matcher.MatchEndForward(new CodeMatch(OpCodes.Pop)); // after AppendLine(...); pop
            int removalEnd = matcher.Pos;

            matcher.RemoveInstructionsInRange(removalStart, removalEnd);

            // Return to removalStart insertion point
            matcher.Advance(removalStart - matcher.Pos);

            // Insert: AppendInfo(warmth, this, inSlot, dsc, condStr, color)
            // Locals per your IL dump:
            //  - warmth: 10
            //  - condStr: 9
            //  - color: 11
            matcher.Insert(
                CodeInstruction.LoadLocal(10),           // warmth
                CodeInstruction.LoadArgument(0),         // CollectibleBehaviorWearable (this)
                CodeInstruction.LoadArgument(1),         // inSlot
                CodeInstruction.LoadArgument(2),         // dsc
                CodeInstruction.LoadLocal(9),            // condStr
                CodeInstruction.LoadLocal(11),           // color
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(AppendInfo)))
            );

            return matcher.InstructionEnumeration();
        }

        public static void AppendInfo(
            float warmth,
            CollectibleBehaviorWearable wearableBh,
            ItemSlot inSlot,
            StringBuilder dsc,
            string condStr,
            string color
        )
        {
            var itemStack = inSlot.Itemstack;
            if (itemStack == null) return;

            // max warmth (1.22 uses behavior GetMaxWarmth)
            float maxWarmth = wearableBh.GetMaxWarmth(inSlot);

            // max cooling (your mod’s stat)
            float maxCooling = CoolingManager.GetMaxCooling(itemStack);

            // current cooling should scale with condition (same scheme as warmth)
            float coolingNow = wearableBh.GetCooling(inSlot);

            var warmthFormat = Lang.GetUnformatted("+{0:0.#}°C");
            var coolingFormat = Lang.GetUnformatted("hydrateordiedrate:itemwearable-temperature-format");

            if (inSlot is not ItemSlotCreative)
            {
                dsc.AppendFormat("<font color=\"{0}\">", color);
                dsc.AppendLine(condStr);
                dsc.Append(FONT_CLOSE_TAG);

                dsc.Append(Lang.Get("hydrateordiedrate:itemwearable-warmth"));
                dsc.Append(": ");
                dsc.Append("<font color=\"#ff8444\">");
                dsc.AppendFormat(warmthFormat, warmth);
                dsc.Append(FONT_CLOSE_TAG);

                dsc.Append(", ");

                dsc.Append(Lang.Get("hydrateordiedrate:itemwearable-cooling"));
                dsc.Append(": ");
                dsc.Append("<font color=\"#84dfff\">");
                dsc.AppendFormat(coolingFormat, coolingNow);
                dsc.AppendLine(FONT_CLOSE_TAG);
            }

            dsc.Append("<font size=13>");
            dsc.Append(Lang.Get("hydrateordiedrate:itemwearable-maxwarmth"));
            dsc.Append(": ");
            dsc.AppendFormat(warmthFormat, maxWarmth);

            dsc.Append(" | ");

            dsc.Append(Lang.Get("hydrateordiedrate:itemwearable-maxcooling"));
            dsc.Append(": ");
            dsc.AppendFormat(coolingFormat, maxCooling);
            dsc.AppendLine(FONT_CLOSE_TAG);
        }

        // --- ensureConditionExists / GetMergableQuantity ---------------------

        [HarmonyPatch(typeof(CollectibleBehaviorWearable), "ensureConditionExists")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeEnsureConditionExists(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            AppendTheCoolingChecks(matcher);
            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMergableQuantity))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeMergableQuantity(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            // In 1.22 base code:
            //   if (this.GetMaxWarmth(new DummySlot(sinkStack)) == 0f) { handling = PassThrough; return 0; }
            //
            // We want that check to treat "cooling gear" as valid too.
            //
            // Easiest robust change: replace the GetMaxWarmth(...) call with our helper that returns
            // "effective max warmth" OR 1 when cooling exists (so it won't be == 0).

            var getMaxWarmth = AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMaxWarmth));
            var helper = AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(GetMaxWarmthOrCoolingMarker));

            matcher.MatchStartForward(CodeMatch.Calls(getMaxWarmth));
            if (matcher.IsValid)
            {
                matcher.SetInstruction(new CodeInstruction(OpCodes.Call, helper));
            }

            return matcher.InstructionEnumeration();
        }

        // Called instead of CollectibleBehaviorWearable.GetMaxWarmth(slot)
        // If warmth is 0 but cooling exists, return 1 (any non-zero marker) so vanilla "== 0" checks treat it as wearable.
        public static float GetMaxWarmthOrCoolingMarker(CollectibleBehaviorWearable self, ItemSlot slot)
        {
            float maxWarmth = self.GetMaxWarmth(slot);
            if (maxWarmth != 0f) return maxWarmth;

            ItemStack stack = slot?.Itemstack;
            if (stack?.ItemAttributes == null) return 0f;

            float maxCooling = stack.ItemAttributes[Attributes.Cooling].AsFloat(0f);
            return maxCooling != 0f ? 1f : 0f;
        }

        // --- shared IL injection (warmth exists -> warmth OR cooling exists) --

        public static void AppendTheCoolingChecks(CodeMatcher matcher)
        {
            matcher.DeclareLocal(typeof(JsonObject), out var attributes);
            var jsonObjectIndexer = AccessTools.IndexerGetter(typeof(JsonObject), parameters: new[] { typeof(string) });
            var jsonObjectExists = AccessTools.PropertyGetter(typeof(JsonObject), nameof(JsonObject.Exists));

            // Find the first “warmth attribute exists” check pattern:
            matcher.MatchStartForward(
                CodeMatch.LoadsConstant("warmth"),
                CodeMatch.Calls(jsonObjectIndexer),
                CodeMatch.Calls(jsonObjectExists)
            );
            if (!matcher.IsValid) return;

            // Cache attributes JsonObject (dup + stloc)
            var originalEntry = matcher.Instruction.labels;
            matcher.Instruction.labels = new List<Label>();

            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup) { labels = originalEntry },
                CodeInstruction.StoreLocal(attributes.LocalIndex)
            );

            matcher.Advance(3);

            // Turn:
            //   (warmthExists)
            // into:
            //   (warmthExists || coolingExists)
            matcher.InsertAndAdvance(
                CodeInstruction.LoadLocal(attributes.LocalIndex),
                new CodeInstruction(OpCodes.Ldstr, Attributes.Cooling),
                new CodeInstruction(OpCodes.Call, jsonObjectIndexer),
                new CodeInstruction(OpCodes.Callvirt, jsonObjectExists),
                new CodeInstruction(OpCodes.Or)
            );

            // There are two common compiler shapes for the next branch.
            // We’ll try both like your 1.21 version.

            matcher.MatchStartForward(CodeMatch.LoadsConstant("warmth"));
            var backupPos = matcher.Pos;

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ceq),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ceq),
                CodeMatch.Branches()
            );

            if (matcher.IsValid)
            {
                matcher.Insert(
                    CodeInstruction.LoadArgument(1), // slot
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ItemSlot), nameof(ItemSlot.Itemstack))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(OrCoolingExists)))
                );
                return;
            }

            // Fallback “inverted” structure
            matcher.Start().Advance(backupPos);
            matcher.MatchEndForward(
                CodeMatch.Calls(AccessTools.Method(typeof(JsonObject), nameof(JsonObject.AsFloat))),
                CodeMatch.LoadsConstant(0f),
                new CodeMatch(OpCodes.Ceq),
                CodeMatch.Branches()
            );

            if (!matcher.IsValid) return;

            matcher.Insert(
                CodeInstruction.LoadArgument(1), // slot
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ItemSlot), nameof(ItemSlot.Itemstack))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(AndCoolingMissing)))
            );
        }

        public static bool AndCoolingMissing(bool warmthMissing, ItemStack itemStack)
            => warmthMissing && itemStack?.ItemAttributes?[Attributes.Cooling].AsFloat(0f) == 0f;

        public static bool OrCoolingExists(bool warmthExists, ItemStack itemStack)
            => warmthExists || itemStack?.ItemAttributes?[Attributes.Cooling].AsFloat(0f) != 0f;
    }
}
