using HarmonyLib;
using Vintagestory.GameContent;
using System.Collections.Generic;
using Vintagestory.API.Config;

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
                <= 0 => Lang.Get("hydrateordiedrate:noaquiferdetected"),
                <= 10 => Lang.Get("hydrateordiedrate:verypoor"),
                <= 20 => Lang.Get("hydrateordiedrate:poor"),
                <= 40 => Lang.Get("hydrateordiedrate:light"),
                <= 60 => Lang.Get("hydrateordiedrate:moderate"),
                _ => Lang.Get("hydrateordiedrate:heavy")
            };

            string aquiferType = isSalty
                ? Lang.Get("hydrateordiedrate:saltwater")
                : Lang.Get("hydrateordiedrate:freshwater");
            if (descriptor == Lang.Get("hydrateordiedrate:noaquiferdetected"))
            {
                __result += $"\n{Lang.Get("hydrateordiedrate:aquiferinfo")} {descriptor}";
            }
            else
            {
                __result += $"\n{Lang.Get("hydrateordiedrate:aquiferinfo")} {descriptor}, {aquiferType}";
            }
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
