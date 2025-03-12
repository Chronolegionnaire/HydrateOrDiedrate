using HarmonyLib;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch(typeof(PropickReading), "ToHumanReadable")]
    public static class PropickReading_ToHumanReadable_Patch
    {
        public static bool AppendAquiferInfo = true;
        private static OreReading aquiferBackup;

        static void Prefix(PropickReading __instance)
        {
            if (__instance.OreReadings.ContainsKey("$aquifer$"))
            {
                aquiferBackup = __instance.OreReadings["$aquifer$"];
                __instance.OreReadings.Remove("$aquifer$");
            }
        }

        static void Postfix(PropickReading __instance, string languageCode, Dictionary<string, string> pageCodes, ref string __result)
        {
            if (aquiferBackup != null)
            {
                __instance.OreReadings["$aquifer$"] = aquiferBackup;
            }
            if (!AppendAquiferInfo || aquiferBackup == null) return;
            OreReading aquiferEntry = aquiferBackup;
            bool isSalty = (aquiferEntry.DepositCode == "salty");
            int rating = (int)aquiferEntry.PartsPerThousand;

            string descriptor = rating switch
            {
                <= 0 => "No aquifer detected.",
                <= 10 => "Very poor",
                <= 20 => "Poor",
                <= 40 => "Light",
                <= 60 => "Moderate",
                _ => "Heavy"
            };

            string aquiferType = isSalty ? "Salt" : "Fresh";
            string aquiferLine = descriptor == "No aquifer detected."
                ? "\n[Aquifer Info] " + descriptor
                : $"\n[Aquifer Info] {descriptor}, {aquiferType} Water";
            __result += aquiferLine;

            aquiferBackup = null;
        }
    }
    [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
    public static class PrintProbeResults_ChatPatch
    {
        static void Prefix() => PropickReading_ToHumanReadable_Patch.AppendAquiferInfo = false;
        static void Postfix() => PropickReading_ToHumanReadable_Patch.AppendAquiferInfo = true;
    }
}