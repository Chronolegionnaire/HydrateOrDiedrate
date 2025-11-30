using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather;

public static class CustomItemWearableExtensions
{
    public static float GetCooling(ItemSlot inslot, ICoreAPI api)
    {
        if (inslot.Empty) return 0f;
        EnsureConditionExists(inslot, api);

        ItemStack itemStack = inslot.Itemstack;

        float maxCooling = CoolingManager.GetMaxCooling(itemStack).GuardFinite();
        if (maxCooling < 0f)
        {
            return Util.GuardFinite(maxCooling);
        }
        float condition = itemStack.Attributes.GetFloat("condition", 1f).GuardFinite(1f);
        float factor = GameMath.Clamp(condition * 2f, 0f, 1f);

        return Util.GuardFinite(maxCooling * factor);
    }

    //TODO look at this method
    private static void EnsureConditionExists(ItemSlot slot, ICoreAPI api)
    {
        if (slot is DummySlot) return;

        if (slot.Itemstack.Attributes.HasAttribute("condition"))
        {
            float condition = slot.Itemstack.Attributes.GetFloat("condition", 1f);
            if (float.IsNaN(condition))
            {
                slot.Itemstack.Attributes.SetFloat("condition", 1f);
                slot.MarkDirty();
            }
            return;
        }

        if (api.Side == EnumAppSide.Server)
        {
            JsonObject itemAttributes = slot.Itemstack.ItemAttributes;

            float maxCooling = itemAttributes?[Attributes.Cooling]?.AsFloat(0f) ?? 0f;
            float maxWarmth = itemAttributes?["warmth"]?.AsFloat(0f) ?? 0f;

            bool shouldAssignCondition = !(IsZeroNaNOrNull(maxCooling) && IsZeroNaNOrNull(maxWarmth));

            if (shouldAssignCondition)
            {
                float newCondition = (slot is ItemSlotTrade)
                    ? (float)api.World.Rand.NextDouble() * 0.25f + 0.75f
                    : (float)api.World.Rand.NextDouble() * 0.4f;

                slot.Itemstack.Attributes.SetFloat("condition", newCondition);
                slot.MarkDirty();
            }
        }
    }

    private static bool IsZeroNaNOrNull(float value) => value == 0f || float.IsNaN(value);
}
