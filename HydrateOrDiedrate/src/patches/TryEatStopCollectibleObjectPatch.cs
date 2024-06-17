using HarmonyLib;
using HydrateOrDiedrate;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

[HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
public class TryEatStopCollectibleObjectPatch
{
    static void Postfix(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        var api = byEntity?.World?.Api;
        if (api?.Side == EnumAppSide.Server)
        {
            if (slot?.Itemstack == null)
            {
                return;
            }

            string itemCode = slot.Itemstack.Collectible.Code?.ToString() ?? "Unknown Item";
            float hydration = HydrationManager.GetHydration(api, itemCode);

            if (hydration != 0 && byEntity is EntityPlayer player)
            {
                var handler = new WaterInteractionHandler(api, HydrateOrDiedrateModSystem.LoadedConfig);
                var playerByUid = api.World.PlayerByUid(player.PlayerUID);
                if (playerByUid != null)
                {
                    handler.ModifyThirst(playerByUid, hydration);
                }
                else
                {
                    api.Logger.Error("TryEatStopCollectibleObjectPatch: Player by UID not found for {0}", player.PlayerUID);
                }
            }
        }
    }
}