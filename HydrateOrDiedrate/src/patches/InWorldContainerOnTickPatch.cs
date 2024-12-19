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
            var inventoryClassName = __instance.Inventory?.ClassName;
            
            if (inventoryClassName == "tun" || inventoryClassName == "keg")
            {
                return false;
            }

            return true;
        }
    }
}