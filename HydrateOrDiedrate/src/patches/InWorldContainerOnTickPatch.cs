using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch(typeof(InWorldContainer), "OnTick")]
    public static class InWorldContainerOnTickPatch
    {
        static bool Prefix(InWorldContainer __instance)
        {
            // Get the inventory class name
            var inventoryClassName = __instance.Inventory?.ClassName;

            // Skip processing for custom tun and keg entities
            if (inventoryClassName == "tun" || inventoryClassName == "keg")
            {
                return false; // Skip original method
            }

            return true; // Continue with original method for other inventories
        }
    }
}