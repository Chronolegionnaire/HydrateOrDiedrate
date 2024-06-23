using System;
using HydrateOrDiedrate;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

public static class CustomItemWearableExtensions
{
    public static float GetCooling(ItemSlot inslot, ICoreAPI api)
    {
        EnsureConditionExists(inslot, api);

        string itemCode = inslot.Itemstack.Collectible.Code.ToString();
        float maxCooling = CoolingManager.GetCooling(api, itemCode);
        float condition = inslot.Itemstack.Attributes.GetFloat("condition", 1f);
        float cooling = Math.Min(maxCooling, condition * 2f * maxCooling);

        return cooling;
    }

    private static void EnsureConditionExists(ItemSlot slot, ICoreAPI api)
    {
        if (slot is DummySlot) return;
        if (!slot.Itemstack.Attributes.HasAttribute("condition") && api.Side == EnumAppSide.Server)
        {
            JsonObject itemAttributes = slot.Itemstack.ItemAttributes;
            if (itemAttributes != null && itemAttributes["cooling"].Exists)
            {
                if (slot is ItemSlotTrade)
                {
                    slot.Itemstack.Attributes.SetFloat("condition", (float)api.World.Rand.NextDouble() * 0.25f + 0.75f);
                }
                else
                {
                    slot.Itemstack.Attributes.SetFloat("condition", (float)api.World.Rand.NextDouble() * 0.4f);
                }
                slot.MarkDirty();
            }
        }
    }
}