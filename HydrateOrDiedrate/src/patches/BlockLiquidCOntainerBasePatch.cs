using System;
using System.Reflection;
using HarmonyLib;
using HydrateOrDiedrate.wellwater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
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
    [HarmonyPatch(typeof(BlockLiquidContainerBase))]
    [HarmonyPatch("OnGroundIdle", new Type[] { typeof(EntityItem) })]
    public static class Patch_BlockLiquidContainerBase_OnGroundIdle
    {
        private static readonly FieldInfo ApiField = typeof(BlockLiquidContainerBase)
            .GetField("api", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Low)]
        public static bool Prefix(BlockLiquidContainerBase __instance, EntityItem entityItem)
        {
            ICoreAPI api = ApiField?.GetValue(__instance) as ICoreAPI;
            if (api == null) return true;
            IWorldAccessor world = entityItem.World;
            if (world.Side != EnumAppSide.Server)
            {
                return true;
            }
            if (entityItem.Swimming && world.Rand.NextDouble() < 0.03)
            {
                BlockPos pos = entityItem.SidedPos.AsBlockPos;
                Block block = world.BlockAccessor.GetBlock(pos);
                if (block != null && block.Attributes != null)
                {
                    var waterTightContainerProps = block.Attributes["waterTightContainerProps"];
                    if (waterTightContainerProps != null && waterTightContainerProps.Exists)
                    {
                        var whenFilledStack = waterTightContainerProps["whenFilled"]?["stack"];
                        if (whenFilledStack != null && whenFilledStack.Exists)
                        {
                            string itemCode = whenFilledStack["code"]?.AsString();
                            if (!string.IsNullOrEmpty(itemCode))
                            {
                                var item = world.GetItem(new AssetLocation(itemCode));
                                if (item != null)
                                {
                                    BlockEntityWellWaterData wellWaterData = null;
                                    var blockBehavior = block.GetBehavior<BlockBehaviorWellWaterFinite>();
                                    if (blockBehavior != null)
                                    {
                                        BlockPos naturalSourcePos = blockBehavior.FindNaturalSourceInLiquidChain(world.BlockAccessor, pos);
                                        if (naturalSourcePos != null)
                                        {
                                            var blockEntity = world.BlockAccessor.GetBlockEntity(naturalSourcePos);
                                            if (blockEntity is BlockEntityWellWaterData sourceData)
                                            {
                                                wellWaterData = sourceData;
                                            }
                                        }
                                    }
                                    if (wellWaterData == null)
                                    {
                                        var be = world.BlockAccessor.GetBlockEntity(pos);
                                        if (be is BlockEntityWellWaterData fallbackData)
                                        {
                                            wellWaterData = fallbackData;
                                        }
                                    }
                                    if (wellWaterData != null && wellWaterData.Volume > 0)
                                    {
                                        float availableLitres = wellWaterData.Volume;
                                        float currentLitres = __instance.GetCurrentLitres(entityItem.Itemstack);
                                        float containerCapacity = __instance.CapacityLitres - currentLitres;
                                        if (containerCapacity > 0)
                                        {
                                            float transferLitres = Math.Min(availableLitres, containerCapacity);
                                            int volumeToTake = (int)transferLitres;
                                            wellWaterData.Volume -= volumeToTake;
                                            float beforeLitres = __instance.GetCurrentLitres(entityItem.Itemstack);
                                            var contentStack = new ItemStack(item)
                                            {
                                                StackSize = (int)(transferLitres * 100)
                                            };
                                            __instance.TryPutLiquid(entityItem.Itemstack, contentStack, transferLitres);
                                            float afterLitres = __instance.GetCurrentLitres(entityItem.Itemstack);
                                            if (Math.Abs(afterLitres - beforeLitres) >= 0.001f)
                                            {
                                                __instance.DoLiquidMovedEffects(
                                                    null,
                                                    contentStack,
                                                    volumeToTake,
                                                    BlockLiquidContainerBase.EnumLiquidDirection.Fill
                                                );
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (entityItem.Swimming && world.Rand.NextDouble() < 0.01)
            {
                ItemStack[] stacks = __instance.GetContents(world, entityItem.Itemstack);
                if (MealMeshCache.ContentsRotten(stacks))
                {
                    for (int i = 0; i < stacks.Length; i++)
                    {
                        if (stacks[i] != null && stacks[i].StackSize > 0 && stacks[i].Collectible.Code.Path == "rot")
                        {
                            world.SpawnItemEntity(stacks[i], entityItem.ServerPos.XYZ, null);
                        }
                    }
                    __instance.SetContent(entityItem.Itemstack, null);
                }
            }
            return false;
        }
    }
}
