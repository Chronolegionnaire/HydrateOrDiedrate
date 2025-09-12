using System;
using System.Reflection;
using HarmonyLib;
using HydrateOrDiedrate.wellwater;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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
            var block = api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block?.Code == null || block.Code.Path == null || !block.Code.Path.StartsWith("wellwater"))
            {
                return true;
            }
            if (block.Attributes == null) return true;
            var waterTightContainerProps = block.Attributes["waterTightContainerProps"];
            if (waterTightContainerProps == null || !waterTightContainerProps.Exists) return true;
            var whenFilledStack = waterTightContainerProps["whenFilled"]?["stack"];
            if (whenFilledStack == null || !whenFilledStack.Exists) return true;
            string itemCode = whenFilledStack["code"]?.AsString();
            if (string.IsNullOrEmpty(itemCode)) return true;
            var item = api.World.GetItem(new AssetLocation(itemCode));
            if (item == null) return true;

            var spring = FindOwningSpringFor(api.World, pos);
            if (spring == null)
            {
                __result = false;
                return false;
            }
            float currentLitres = __instance.GetCurrentLitres(itemslot.Itemstack);
            float containerCapacity = __instance.CapacityLitres - currentLitres;
            if (containerCapacity <= 0.0001f)
            {
                __result = false;
                return false;
            }
            int requestedLiters = Math.Max(0, (int)Math.Floor(containerCapacity));
            if (requestedLiters <= 0)
            {
                __result = false;
                return false;
            }
            int takenLiters = spring.TryExtractLitersAt(pos, requestedLiters);
            if (takenLiters <= 0)
            {
                __result = false;
                return false;
            }
            float transferLitres = takenLiters;
            var contentStack = new ItemStack(item)
            {
                StackSize = (int)(transferLitres * 1000)
            };
            int moved = __instance.SplitStackAndPerformAction((Entity)byEntity, itemslot, (ItemStack singleItem) =>
            {
                float before = __instance.GetCurrentLitres(singleItem);
                int filled = __instance.TryPutLiquid(singleItem, contentStack, transferLitres);
                float after = __instance.GetCurrentLitres(singleItem);
                return (Math.Abs(after - before) >= 0.001f) ? filled : 0;
            });

            if (moved > 0)
            {
                __instance.DoLiquidMovedEffects(byEntity as IPlayer, contentStack, moved, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
                __result = true;
            }
            else
            {
                __result = false;
            }
            return false;
        }
        private static BlockEntityWellSpring FindOwningSpringFor(IWorldAccessor world, BlockPos pos)
        {
            var p = pos.DownCopy();
            const int maxDepth = 512;
            for (int i = 0; i < maxDepth && p.Y >= 0; i++, p.Y--)
            {
                var be = world.BlockAccessor.GetBlockEntity(p);
                if (be is BlockEntityWellSpring spring) return spring;
            }
            return null;
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
            if (world.Side != EnumAppSide.Server) return true;
            if (entityItem.Swimming && world.Rand.NextDouble() < 0.03)
            {
                BlockPos pos = entityItem.SidedPos.AsBlockPos;
                Block block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);

                if (block?.Attributes != null && block.Code?.Path?.StartsWith("wellwater") == true)
                {
                    var waterTightContainerProps = block.Attributes["waterTightContainerProps"];
                    var whenFilledStack = waterTightContainerProps?["whenFilled"]?["stack"];
                    string itemCode = whenFilledStack?["code"]?.AsString();

                    if (!string.IsNullOrEmpty(itemCode))
                    {
                        var item = world.GetItem(new AssetLocation(itemCode));
                        if (item != null)
                        {
                            var spring = FindOwningSpringFor(world, pos);
                            if (spring != null)
                            {
                                float currentLitres = __instance.GetCurrentLitres(entityItem.Itemstack);
                                float containerCapacity = __instance.CapacityLitres - currentLitres;

                                if (containerCapacity > 0.0001f)
                                {
                                    int requestedLiters = Math.Max(0, (int)Math.Floor(containerCapacity));
                                    int takenLiters = spring.TryExtractLitersAt(pos, requestedLiters);

                                    if (takenLiters > 0)
                                    {
                                        float transferLitres = takenLiters;
                                        var contentStack = new ItemStack(item)
                                        {
                                            StackSize = (int)(transferLitres * 100)
                                        };
                                        var dummySlot = new DummySlot(entityItem.Itemstack);
                                        int moved = __instance.SplitStackAndPerformAction(entityItem, dummySlot, (ItemStack singleItem) =>
                                        {
                                            float before = __instance.GetCurrentLitres(singleItem);
                                            int filled = __instance.TryPutLiquid(singleItem, contentStack, transferLitres);
                                            float after = __instance.GetCurrentLitres(singleItem);
                                            return (Math.Abs(after - before) >= 0.001f) ? filled : 0;
                                        });

                                        if (moved > 0)
                                        {
                                            __instance.DoLiquidMovedEffects(null, contentStack, moved, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
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
        private static BlockEntityWellSpring FindOwningSpringFor(IWorldAccessor world, BlockPos pos)
        {
            var p = pos.DownCopy();
            const int maxDepth = 512;
            for (int i = 0; i < maxDepth && p.Y >= 0; i++, p.Y--)
            {
                var be = world.BlockAccessor.GetBlockEntity(p);
                if (be is BlockEntityWellSpring spring) return spring;
            }
            return null;
        }
    }
}
