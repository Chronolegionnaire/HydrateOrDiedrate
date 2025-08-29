using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches.Harmony;

[HarmonyPatch]
public static class BlockCookingContainerPatches
{

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var baseGameType = typeof(BlockCookingContainer);
        var baseGameMethod = AccessTools.Method(baseGameType, nameof(BlockCookingContainer.DoSmelt));
        yield return baseGameMethod;

        //Find all method overrides
        foreach(var inheritingClass in AccessTools.AllTypes().Where(baseGameType.IsAssignableFrom))
        {
            var method = AccessTools.GetDeclaredMethods(inheritingClass).Find(method => method.GetBaseDefinition() == baseGameMethod && method != baseGameMethod);
            if(method is not null) yield return method;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    public static bool Prefix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, BlockCookingContainer __instance)
    {
        ItemStack[] stacks = __instance.GetCookingStacks(cookingSlotsProvider, true);
        CookingRecipe recipe = __instance.GetMatchingCookingRecipe(world, stacks, out var quantityServings);

        if (recipe is null || (recipe.CooksInto?.ResolvedItemstack?.Collectible?.Code?.Domain) != "hydrateordiedrate") return true;

        ItemStack outstack = recipe.CooksInto.ResolvedItemstack.Clone();
        outstack.StackSize *= quantityServings;
        stacks = [outstack];
        
        ItemStack outputStack = new(__instance);

        outputStack.Collectible.SetTemperature(world, outputStack, BlockCookingContainer.GetIngredientsTemperature(world, stacks), true);
        TransitionableProperties cookedPerishProps = recipe.PerishableProps.Clone();
        cookedPerishProps.TransitionedStack.Resolve(world, "cooking container perished stack", true);


        CollectibleObject.CarryOverFreshness(world.Api, cookingSlotsProvider.Slots, stacks, cookedPerishProps);
        
        for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
        {
            cookingSlotsProvider.Slots[i].Itemstack = i == 0 ? stacks[0] : null;
        }

        inputSlot.Itemstack = outputStack;
        return false;
    }
}