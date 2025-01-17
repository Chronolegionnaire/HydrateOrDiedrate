using System;
using System.Reflection;
using HarmonyLib;
using HydrateOrDiedrate.wellwater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.HarmonyPatches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("TryFillFromBlock", new Type[] { typeof(ItemSlot), typeof(EntityAgent), typeof(BlockPos) })]
    public static class Patch_BlockLiquidContainerBase_TryFillFromBlock
    {
        private static readonly FieldInfo ApiField = typeof(BlockLiquidContainerBase)
            .GetField("api", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool Prefix(
            BlockLiquidContainerBase __instance,
            ItemSlot itemslot,
            EntityAgent byEntity,
            BlockPos pos,
            ref bool __result
        )
        {
            ICoreAPI api = ApiField?.GetValue(__instance) as ICoreAPI;
            if (api == null) return true;
            var block = api.World.BlockAccessor.GetBlock(pos);
            if (block == null || block.Attributes == null) return true;
            var waterTightContainerProps = block.Attributes["waterTightContainerProps"];
            if (waterTightContainerProps == null || !waterTightContainerProps.Exists) return true;
            var whenFilledStack = waterTightContainerProps["whenFilled"]?["stack"];
            if (whenFilledStack == null || !whenFilledStack.Exists) return true;
            string itemCode = whenFilledStack["code"]?.AsString();
            if (string.IsNullOrEmpty(itemCode)) return true;
            var item = api.World.GetItem(new AssetLocation(itemCode));
            if (item == null) return true;
            BlockEntityWellWaterData wellWaterData = null;
            var blockBehavior = block.GetBehavior<BlockBehaviorWellWaterFinite>();
            if (blockBehavior != null)
            {
                var naturalSourcePos = blockBehavior.FindNaturalSourceInLiquidChain(
                    api.World.BlockAccessor,
                    pos
                );
                if (naturalSourcePos != null)
                {
                    var blockEntity = api.World.BlockAccessor.GetBlockEntity(naturalSourcePos);
                    if (blockEntity is BlockEntityWellWaterData sourceData)
                    {
                        wellWaterData = sourceData;
                    }
                }
            }
            if (wellWaterData == null)
            {
                var be = api.World.BlockAccessor.GetBlockEntity(pos);
                if (be is BlockEntityWellWaterData fallbackData)
                {
                    wellWaterData = fallbackData;
                }
            }
            if (wellWaterData == null) return true;
            if (wellWaterData.Volume <= 0)
            {
                __result = false;
                return false;
            }
            float availableLitres = wellWaterData.Volume;
            float currentLitres = __instance.GetCurrentLitres(itemslot.Itemstack);
            float containerCapacity = __instance.CapacityLitres - currentLitres;
            if (containerCapacity <= 0)
            {
                __result = false;
                return false;
            }
            float transferLitres = Math.Min(availableLitres, containerCapacity);
            int volumeToTake = (int)transferLitres; 
            wellWaterData.Volume -= volumeToTake;
            float beforeLitres = __instance.GetCurrentLitres(itemslot.Itemstack);
            var contentStack = new ItemStack(item)
            {
                StackSize = (int)(transferLitres * 1000)
            };
            __instance.TryPutLiquid(itemslot.Itemstack, contentStack, transferLitres);
            float afterLitres = __instance.GetCurrentLitres(itemslot.Itemstack);
            if (Math.Abs(afterLitres - beforeLitres) < 0.001f)
            {
                __result = false;
                return false;
            }
            __instance.DoLiquidMovedEffects(
                byEntity as IPlayer, 
                contentStack, 
                volumeToTake, 
                BlockLiquidContainerBase.EnumLiquidDirection.Fill
            );
            __result = true;
            return false;
        }
    }
}
