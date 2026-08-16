using HarmonyLib;
using HydrateOrDiedrate.Utility;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hydration.Patches;

[HarmonyPatch] //TODO thirst catagory
public static class BlockLiquidContainerPatches
{
    [HarmonyPatch(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.SplitStackAndPerformAction))]
    [HarmonyPrefix]
    public static bool StopStackSplitIfLockedDummySlot(Entity byEntity, ItemSlot slot, Func<ItemStack, int> action, ref int __result)
    {
        if (slot is not LockedDummySlot) return true;

        __result = 0;
        if (slot.Itemstack is not null)
        {
            __result = action(slot.Itemstack);
        }

        return false;
    }

    [HarmonyPatch(typeof(BlockLiquidContainerBase), "tryEatStop")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileDrinkLogic(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);

        matcher.MatchEndForward(
            new CodeMatch(OpCodes.Isinst, typeof(IServerWorldAccessor)),
            CodeMatch.StoresLocal()
        );
        var worldLocalIndex = matcher.Instruction.LocalIndex();

        matcher.MatchStartForward(
            CodeMatch.LoadsLocal(),
            CodeMatch.Calls(AccessTools.Method(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetContentProps), [typeof(ItemStack)])),
            CodeMatch.StoresLocal()
        );
        var itemStackLocalIndex = matcher.Instruction.LocalIndex();
        

        matcher.DeclareLocal(typeof(ItemStack), out var contentStack);
        matcher.DeclareLocal(typeof(float), out var hydration);
        matcher.DeclareLocal(typeof(bool), out var isBoiling);

        matcher.Advance(3);
        matcher.InsertAndAdvance(
            CodeInstruction.LoadArgument(0), //this
            CodeInstruction.LoadLocal(itemStackLocalIndex),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(BlockLiquidContainerBase), nameof(BlockLiquidContainerBase.GetContent), [typeof(ItemStack)])),
            CodeInstruction.StoreLocal(contentStack.LocalIndex),
            CodeInstruction.LoadLocal(contentStack.LocalIndex),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HydrationManager), nameof(HydrationManager.GetHydration))),
            CodeInstruction.StoreLocal(hydration.LocalIndex),
            CodeInstruction.LoadLocal(worldLocalIndex),
            CodeInstruction.LoadLocal(contentStack.LocalIndex),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HydrationManager), nameof(HydrationManager.IsBoiling))),
            CodeInstruction.StoreLocal(isBoiling.LocalIndex)
        );

        matcher.MatchStartForward(
            CodeMatch.LoadsLocal(),
            new CodeMatch(OpCodes.Dup),
            CodeMatch.LoadsField(AccessTools.Field(typeof(FoodNutritionProperties), nameof(FoodNutritionProperties.Satiety))),
            CodeMatch.LoadsLocal(),
            new CodeMatch(OpCodes.Mul)
        );
        var nutrientPropsLocalIndex = matcher.Instruction.LocalIndex();
        matcher.Advance(3);
        var litersLocalIndex = matcher.Instruction.LocalIndex();

        matcher.MatchEndForward(
            CodeMatch.Calls(AccessTools.Method(typeof(EntityAgent), nameof(EntityAgent.ReceiveSaturation)))
        );

        matcher.InsertAfterAndAdvance(
            CodeInstruction.LoadArgument(3), //entity
            CodeInstruction.LoadLocal(hydration.LocalIndex),
            CodeInstruction.LoadLocal(litersLocalIndex),
            CodeInstruction.LoadLocal(nutrientPropsLocalIndex),
            new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(FoodNutritionProperties), nameof(FoodNutritionProperties.Intoxication))),
            CodeInstruction.LoadLocal(isBoiling.LocalIndex),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BlockLiquidContainerPatches), nameof(HandleConsumption)))
        );

        //TODO Fix negative satiety scaling with rot

        return matcher.InstructionEnumeration();
    }

    private static void HandleConsumption(Entity entity, float hydration, float liters, float totalIntox, bool isBoiling)
    {
        if(entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return;

        thirstBehavior.HandleConsumption(hydration * liters, totalIntox, isBoiling);
    }
}
