using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather
{
    public static class CustomItemWearableExtensions
    {
        public static float GetCooling(ItemSlot inslot, ICoreAPI api)
        {
            EnsureConditionExists(inslot, api);

            ItemStack itemStack = inslot.Itemstack;
            float maxCooling = CoolingManager.GetCooling(itemStack);

            if (float.IsNaN(maxCooling))
            {
                maxCooling = 0;
            }

            float condition = itemStack.Attributes.GetFloat("condition", 1f);
            float cooling = Math.Min(maxCooling, condition * 2f * maxCooling);

            return float.IsNaN(cooling) ? 0 : cooling;
        }

        private static void EnsureConditionExists(ItemSlot slot, ICoreAPI api)
        {
            if (slot is DummySlot) return;

            if (slot.Itemstack.Attributes.HasAttribute("condition"))
            {
                return;
            }

            if (api.Side == EnumAppSide.Server)
            {
                JsonObject itemAttributes = slot.Itemstack.ItemAttributes;

                float maxCooling = itemAttributes?[CoolingManager.CoolingAttributeKey]?.AsFloat(0f) ?? 0f; // Updated to use the key from CoolingManager
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

        private static bool IsZeroNaNOrNull(float value)
        {
            return value == 0f || float.IsNaN(value);
        }
    }
}
