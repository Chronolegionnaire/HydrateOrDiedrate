using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather;

public static class CustomItemWearableExtensions
{
    private static readonly Action<ItemWearable, ItemSlot, bool> EnsureConditionExistsDelegate = AccessTools.MethodDelegate<Action<ItemWearable, ItemSlot, bool>>(
        AccessTools.Method(typeof(ItemWearable), "ensureConditionExists")
    );

    public static void EnsureConditionExists(this ItemWearable wearableItem, ItemSlot slot, bool markdirty = true) => EnsureConditionExistsDelegate(wearableItem, slot, markdirty);

    [Obsolete("Use the extension method instead")]
    public static float GetCooling(ItemSlot inslot, ICoreAPI api) => (inslot.Itemstack.Item as ItemWearable)?.GetCooling(inslot) ?? 0f;

    public static float GetCooling(this ItemWearable itemWearable, ItemSlot inslot)
    {
        if (inslot.Empty) return 0f;
        itemWearable.EnsureConditionExists(inslot);

        ItemStack itemStack = inslot.Itemstack;

        float maxCooling = CoolingManager.GetMaxCooling(itemStack);
        if (maxCooling < 0f) return maxCooling;

        float condition = itemStack.Attributes.GetFloat("condition", 1f).GuardFinite(1f);
        float factor = GameMath.Clamp(condition * 2f, 0f, 1f);

        return Util.GuardFinite(maxCooling * factor);
    }
}
