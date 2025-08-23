using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches.Harmony;

[HarmonyPatch(typeof(BlockCookingContainer), "DoSmelt")]
public static class BlockCookingContainerPatch
{
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