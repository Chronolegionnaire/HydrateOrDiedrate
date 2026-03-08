using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather
{
    public static class CustomWearableBehaviorExtensions
    {
        private static readonly Action<CollectibleBehaviorWearable, ItemSlot, bool> EnsureConditionExistsDelegate =
            AccessTools.MethodDelegate<Action<CollectibleBehaviorWearable, ItemSlot, bool>>(
                AccessTools.Method(typeof(CollectibleBehaviorWearable), "ensureConditionExists")
            );
        // TODO change this to use UnsafeAccessorAttribute at some point (better for performance and far more readable)
        public static CollectibleBehaviorWearable GetWearableBehavior(this ItemSlot slot)
        {
            var stack = slot?.Itemstack;
            if (stack?.Collectible == null) return null;

            var wearable = stack.Collectible.GetCollectibleInterface<IWearable>();
            return wearable as CollectibleBehaviorWearable;
        }

        public static void EnsureConditionExists(this CollectibleBehaviorWearable wearableBh, ItemSlot slot, bool markdirty = true)
        {
            if (wearableBh == null) return;
            EnsureConditionExistsDelegate(wearableBh, slot, markdirty);
        }

        public static float GetCooling(this CollectibleBehaviorWearable wearableBh, ItemSlot inslot)
        {
            if (wearableBh == null || inslot == null || inslot.Empty) return 0f;

            wearableBh.EnsureConditionExists(inslot);

            ItemStack itemStack = inslot.Itemstack;
            float maxCooling = CoolingManager.GetMaxCooling(itemStack);
            if (maxCooling < 0f) return maxCooling;

            float condition = itemStack.Attributes.GetFloat("condition", 1f).GuardFinite(1f);
            float factor = GameMath.Clamp(condition * 2f, 0f, 1f);

            return Util.GuardFinite(maxCooling * factor);
        }
    }
}
