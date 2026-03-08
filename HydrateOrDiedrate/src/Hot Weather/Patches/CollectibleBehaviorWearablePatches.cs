using HarmonyLib;
using HydrateOrDiedrate.Hot_Weather;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    public static class CollectibleBehaviorWearablePatches
    {
        public const string FONT_CLOSE_TAG = "</font>";
        [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetHeldItemInfo))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeTemperatureStats(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            var getMaxWarmth = AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMaxWarmth));
            var helperWarmthOrCooling = AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(GetMaxWarmthOrCoolingMarker));
            var appendInfo = AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(AppendInfo));
            matcher.MatchStartForward(CodeMatch.Calls(getMaxWarmth));
            if (!matcher.IsValid) return instructions;
            matcher.SetInstruction(new CodeInstruction(OpCodes.Call, helperWarmthOrCooling));
            matcher.Start();
            matcher.MatchEndForward(
                CodeMatch.Calls(AccessTools.Method(typeof(ColorUtil), nameof(ColorUtil.Int2Hex))),
                new CodeMatch(ci => ci.IsStloc() && ci.operand is LocalBuilder lb && lb.LocalIndex == 11)
            );
            if (!matcher.IsValid) return instructions;
            matcher.Start();
            matcher.MatchEndForward(CodeMatch.LoadsConstant("clothing-maxwarmth"));
            if (!matcher.IsValid) return instructions;

            matcher.MatchEndForward(new CodeMatch(OpCodes.Pop));
            if (!matcher.IsValid) return instructions;
            var skipTargetPos = matcher.Pos;
            var skipLabel = generator.DefineLabel();
            matcher.Instruction.labels.Add(skipLabel);
            matcher.Start();
            matcher.MatchEndForward(
                CodeMatch.Calls(AccessTools.Method(typeof(ColorUtil), nameof(ColorUtil.Int2Hex))),
                new CodeMatch(ci => ci.IsStloc() && ci.operand is LocalBuilder lb && lb.LocalIndex == 11)
            );
            if (!matcher.IsValid) return instructions;
            matcher.Insert(
                CodeInstruction.LoadLocal(10),           // warmth
                CodeInstruction.LoadArgument(0),         // this (CollectibleBehaviorWearable)
                CodeInstruction.LoadArgument(1),         // inSlot
                CodeInstruction.LoadArgument(2),         // dsc
                CodeInstruction.LoadLocal(9),            // condStr
                CodeInstruction.LoadLocal(11),           // color
                new CodeInstruction(OpCodes.Call, appendInfo),
                new CodeInstruction(OpCodes.Br, skipLabel)
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
            var itemStack = inSlot?.Itemstack;
            if (itemStack == null) return;
            float maxWarmth = wearableBh.GetMaxWarmth(inSlot);
            float maxCooling = CoolingManager.GetMaxCooling(itemStack);
            float coolingNow = wearableBh.GetCooling(inSlot);

            var warmthFormat = Lang.GetUnformatted("+{0:0.#}°C");
            var coolingFormat = Lang.GetUnformatted("hydrateordiedrate:itemwearable-temperature-format");
            if (string.IsNullOrEmpty(coolingFormat))
            {
                coolingFormat = "{0:0.#}°C";
            }

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
                dsc.AppendLine();
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
        [HarmonyPatch(typeof(CollectibleBehaviorWearable), "ensureConditionExists")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeEnsureConditionExists(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            var getMaxWarmth = AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMaxWarmth));
            var helper = AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(GetMaxWarmthOrCoolingMarker));
            matcher.MatchStartForward(CodeMatch.Calls(getMaxWarmth));
            while (matcher.IsValid)
            {
                matcher.SetInstruction(new CodeInstruction(OpCodes.Call, helper));
                matcher.MatchStartForward(CodeMatch.Calls(getMaxWarmth));
            }

            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMergableQuantity))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ChangeMergableQuantity(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            var getMaxWarmth = AccessTools.Method(typeof(CollectibleBehaviorWearable), nameof(CollectibleBehaviorWearable.GetMaxWarmth));
            var helper = AccessTools.Method(typeof(CollectibleBehaviorWearablePatches), nameof(GetMaxWarmthOrCoolingMarker));

            matcher.MatchStartForward(CodeMatch.Calls(getMaxWarmth));
            if (matcher.IsValid)
            {
                matcher.SetInstruction(new CodeInstruction(OpCodes.Call, helper));
            }

            return matcher.InstructionEnumeration();
        }
        public static float GetMaxWarmthOrCoolingMarker(CollectibleBehaviorWearable self, ItemSlot slot)
        {
            float maxWarmth = self.GetMaxWarmth(slot);
            if (maxWarmth != 0f) return maxWarmth;

            ItemStack stack = slot?.Itemstack;
            if (stack?.ItemAttributes == null) return 0f;
            float maxCooling = stack.ItemAttributes[Attributes.Cooling].AsFloat(0f);
            return maxCooling != 0f ? 1f : 0f;
        }
    }
}
