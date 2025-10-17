using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Wells.Patches
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

            var fluid = api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (fluid == null || fluid.Attributes == null) return true;

            var waterTightContainerProps = fluid.Attributes["waterTightContainerProps"];
            if (waterTightContainerProps == null || !waterTightContainerProps.Exists) return true;

            var whenFilledStack = waterTightContainerProps["whenFilled"]?["stack"];
            if (whenFilledStack == null || !whenFilledStack.Exists) return true;

            string itemCode = whenFilledStack["code"]?.AsString();
            if (string.IsNullOrEmpty(itemCode)) return true;

            var item = api.World.GetItem(new AssetLocation(itemCode));
            if (item == null) return true;

            if (!WellBlockUtils.IsOurWellwater(fluid))
            {
                return true;
            }

            var spring = WellBlockUtils.FindGoverningSpring(api, fluid, pos);
            if (spring == null) return true;

            if (spring.totalLiters <= 0)
            {
                __result = false;
                return false;
            }

            float availableLitres = spring.totalLiters;
            float currentLitres = __instance.GetCurrentLitres(itemslot.Itemstack);
            float containerCapacity = __instance.CapacityLitres - currentLitres;

            if (containerCapacity <= 0f)
            {
                __result = false;
                return false;
            }

            float transferLitres = Math.Min(availableLitres, containerCapacity);
            int volumeToTake = (int)Math.Floor(transferLitres);
            if (volumeToTake <= 0)
            {
                __result = false;
                return false;
            }

            int delta = spring.TryChangeVolume(-volumeToTake);
            int extractedLiters = -delta;
            if (extractedLiters <= 0)
            {
                __result = false;
                return false;
            }

            var contentStack = new ItemStack(item)
            {
                StackSize = extractedLiters * 1000
            };

            int moved = __instance.SplitStackAndPerformAction((Entity)byEntity, itemslot, (ItemStack singleItem) =>
            {
                float beforeLitres = __instance.GetCurrentLitres(singleItem);
                int filled = __instance.TryPutLiquid(singleItem, contentStack, extractedLiters);
                float afterLitres = __instance.GetCurrentLitres(singleItem);
                return (Math.Abs(afterLitres - beforeLitres) >= 0.001f) ? filled : 0;
            });

            if (moved > 0)
            {
                IPlayer player = (byEntity as EntityPlayer)?.Player;
                if (player != null)
                {
                    __instance.DoLiquidMovedEffects(player, contentStack, moved, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
                }
                __result = true;
            }
            else
            {
                spring.TryChangeVolume(extractedLiters);
                __result = false;
            }

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

                if (!WellBlockUtils.IsOurWellwater(block))
                {
                    return true;
                }

                var spring = (block != null) ? WellBlockUtils.FindGoverningSpring(api, block, pos) : null;
                if (spring == null)
                {
                    return true;
                }

                if (block?.Attributes != null)
                {
                    var waterTightContainerProps = block.Attributes["waterTightContainerProps"];
                    var whenFilledStack = waterTightContainerProps?["whenFilled"]?["stack"];
                    string itemCode = whenFilledStack?["code"]?.AsString();

                    if (!string.IsNullOrEmpty(itemCode))
                    {
                        var item = world.GetItem(new AssetLocation(itemCode));
                        if (item != null)
                        {
                            if (spring.totalLiters > 0)
                            {
                                float availableLitres = spring.totalLiters;
                                float currentLitres = __instance.GetCurrentLitres(entityItem.Itemstack);
                                float containerCapacity = __instance.CapacityLitres - currentLitres;

                                if (containerCapacity > 0f)
                                {
                                    float transferLitres = Math.Min(availableLitres, containerCapacity);
                                    int volumeToTake = (int)Math.Floor(transferLitres);
                                    if (volumeToTake > 0)
                                    {
                                        int delta = spring.TryChangeVolume(-volumeToTake);
                                        int extractedLiters = -delta;

                                        if (extractedLiters > 0)
                                        {
                                            var contentStack = new ItemStack(item)
                                            {
                                                StackSize = extractedLiters * 1000
                                            };

                                            var dummySlot = new DummySlot(entityItem.Itemstack);
                                            int moved = __instance.SplitStackAndPerformAction(entityItem, dummySlot, (ItemStack singleItem) =>
                                            {
                                                float beforeLitres = __instance.GetCurrentLitres(singleItem);
                                                int filled = __instance.TryPutLiquid(singleItem, contentStack, extractedLiters);
                                                float afterLitres = __instance.GetCurrentLitres(singleItem);
                                                return (Math.Abs(afterLitres - beforeLitres) >= 0.001f) ? filled : 0;
                                            });

                                            if (moved > 0)
                                            {
                                                __instance.DoLiquidMovedEffects(null, contentStack, moved, BlockLiquidContainerBase.EnumLiquidDirection.Fill);
                                            }
                                            else
                                            {
                                                spring.TryChangeVolume(extractedLiters);
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
