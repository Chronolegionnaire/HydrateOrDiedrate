using HarmonyLib;
using HydrateOrDiedrate.Hot_Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    public static class ItemWearablePatches
    {
        public const string FONT_CLOSE_TAG = "</font>";

        [HarmonyPatch(typeof(ItemWearable), "GetHeldItemInfo")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeTemperatureStats(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            AppendTheCoolingChecks(matcher);

            matcher.MatchEndForward(CodeMatch.Calls(AccessTools.Method(typeof(ItemWearable), "ensureConditionExists")));

            matcher.MatchEndBackwards(CodeMatch.Branches());
            var jump = (Label)matcher.Operand;

            matcher.MatchEndForward(CodeMatch.LoadsConstant("+{0:0.#}°C"));
            matcher.MatchEndBackwards(CodeMatch.LoadsConstant(0.05d));

            var removalStart = matcher.Pos - 1;

            matcher.MatchEndForward(CodeMatch.LoadsConstant("clothing-maxwarmth"));
            matcher.MatchEndForward(new CodeMatch(OpCodes.Pop));
            var removalEnd = matcher.Pos;

            //Remove original code
            matcher.RemoveInstructionsInRange(removalStart, removalEnd);

            //Return to start point
            matcher.Advance(removalStart - matcher.Pos);

            //New current warmth/cooling text
            matcher.Insert(
                CodeInstruction.LoadArgument(0), //ItemWearable (this)
                CodeInstruction.LoadArgument(1), //inSlot
                CodeInstruction.LoadArgument(2), //dsc

                CodeInstruction.LoadLocal(6), //condStr
                CodeInstruction.LoadLocal(8), //color

                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemWearablePatches), nameof(AppendInfo)))
            );

            matcher.MatchStartBackwards(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_1),
                CodeMatch.Calls(AccessTools.Method(typeof(ItemWearable), nameof(ItemWearable.GetWarmth)))
            );

            matcher.Instruction.labels.Add(jump);

            return matcher.InstructionEnumeration();
        }

        public static void AppendInfo(float warmth, ItemWearable itemWearable, ItemSlot inSlot, StringBuilder dsc, string condStr, string color)
        {
            var itemStack = inSlot.Itemstack;

            var maxCooling = CoolingManager.GetMaxCooling(itemStack);
            var maxWarmth = itemStack.ItemAttributes?["warmth"].AsFloat(0) ?? 0;

            var warmthFormat = Lang.GetUnformatted("+{0:0.#}°C");
            var coolingFormat = Lang.GetUnformatted("hydrateordiedrate:itemwearable-temperature-format");

            if(inSlot is not ItemSlotCreative)
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
                dsc.AppendFormat(coolingFormat, itemWearable.GetCooling(inSlot));
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

        public static void AppendTheCoolingChecks(CodeMatcher matcher)
        {
            matcher.DeclareLocal(typeof(JsonObject), out var attributes);
            var jsonObjectIndexer = AccessTools.IndexerGetter(typeof(JsonObject), parameters: [typeof(string)]);
            var jsonObjectExists = AccessTools.PropertyGetter(typeof(JsonObject), nameof(JsonObject.Exists));
            
            //Find exists check
            matcher.MatchStartForward(
                CodeMatch.LoadsConstant("warmth"),
                CodeMatch.Calls(jsonObjectIndexer),
                CodeMatch.Calls(jsonObjectExists)
            );

            //Cache attributes
            var originalEntry = matcher.Instruction.labels;
            matcher.Instruction.labels = [];
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup) { labels = originalEntry },
                CodeInstruction.StoreLocal(attributes.LocalIndex)
            );
            matcher.Advance(3);

            //Append or if cooling attribute exists check
            matcher.InsertAndAdvance(
                CodeInstruction.LoadLocal(attributes.LocalIndex),
                new CodeInstruction(OpCodes.Ldstr, Attributes.Cooling),
                new CodeInstruction(OpCodes.Call, AccessTools.IndexerGetter(typeof(JsonObject), parameters: [typeof(string)])),
                new CodeInstruction(OpCodes.Callvirt, jsonObjectExists),
                new CodeInstruction(OpCodes.Or)
            );

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
                    CodeInstruction.LoadArgument(1), //slot
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(ItemSlot), nameof(ItemSlot.Itemstack))),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemWearablePatches), nameof(OrCoolingExists)))
                );
                return;
            }

            //Fallback inverted structure
            matcher.Start().Advance(backupPos);
            matcher.MatchEndForward(
                CodeMatch.Calls(AccessTools.Method(typeof(JsonObject), nameof(JsonObject.AsFloat))),
                CodeMatch.LoadsConstant(0f),
                new CodeMatch(OpCodes.Ceq),
                CodeMatch.Branches()
            );

            matcher.Insert(
                CodeInstruction.LoadArgument(1), //sinkstack
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemWearablePatches), nameof(AndCoolingMissing)))
            );
        }

        [HarmonyPatch(typeof(ItemWearable), "ensureConditionExists")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeEnsureConditionExists(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            AppendTheCoolingChecks(matcher);

            return matcher.InstructionEnumeration();
        }
        
        [HarmonyPatch(typeof(ItemWearable), "GetMergableQuantity")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeMergableQuantity(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);
            AppendTheCoolingChecks(matcher);
            return matcher.InstructionEnumeration();
        }

        public static bool AndCoolingMissing(bool warmthMissing, ItemStack itemStack) => warmthMissing && itemStack.ItemAttributes[Attributes.Cooling].AsFloat(0f) == 0f;
        public static bool OrCoolingExists(bool warmthExists, ItemStack itemStack) => warmthExists || itemStack.ItemAttributes[Attributes.Cooling].AsFloat(0f) != 0f;
    }
}