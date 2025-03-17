using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches
{
    [HarmonyPatch]
    public static class ArtXskillsBlockCookingContainerPatch
    {
    static bool Prepare()
    {
        return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "ArtsXSkills");
    }

    static MethodBase TargetMethod()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ArtsXSkills");
        if (assembly == null) return null;

        var type = assembly.GetType("ArtsXSlills.ArtsXSlillsBlockCookingContainer");
        if (type == null) return null;

        return type.GetMethod("DoSmelt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    static bool Prefix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot,
        ItemSlot outputSlot, object __instance)
    {
        var instanceType = __instance.GetType();
        var getCookingStacksMethod = instanceType.GetMethod("GetCookingStacks",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getCookingStacksMethod == null) return true;

        var stacksObj = getCookingStacksMethod.Invoke(__instance, new object[] { cookingSlotsProvider, true });
        ItemStack[] stacks = stacksObj as ItemStack[];
        if (stacks == null) return true;
        var getMatchingCookingRecipeMethod = instanceType.GetMethod("GetMatchingCookingRecipe",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getMatchingCookingRecipeMethod == null) return true;

        var recipeObj = getMatchingCookingRecipeMethod.Invoke(__instance, new object[] { world, stacks });
        CookingRecipe recipe = recipeObj as CookingRecipe;
        if (recipe == null) return true;
        bool contains = false;
        if (recipe.CooksInto != null &&
            recipe.CooksInto.ResolvedItemstack != null &&
            recipe.CooksInto.ResolvedItemstack.Collectible != null &&
            recipe.CooksInto.ResolvedItemstack.Collectible.Code != null)
        {
            string codeString = recipe.CooksInto.ResolvedItemstack.Collectible.Code.ToString();
            contains = codeString.Contains("hydrateordiedrate:");
        }

        if (!contains)
        {
            return true;
        }

        int quantityServings = recipe.GetQuantityServings(stacks);
        ItemStack resolvedItemstack = recipe.CooksInto.ResolvedItemstack;
        ItemStack outstack = resolvedItemstack?.Clone();
        if (outstack != null)
        {
            outstack.StackSize *= quantityServings;
            stacks = new ItemStack[] { outstack };
        }

        var containerBlock = __instance as BlockCookingContainer;
        if (containerBlock == null) return true;
        ItemStack outputStack = new ItemStack(containerBlock, 1);
        float ingredientsTemp = BlockCookingContainer.GetIngredientsTemperature(world, stacks);
        outputStack.Collectible.SetTemperature(world, outputStack, ingredientsTemp, true);

        TransitionableProperties cookedPerishProps = recipe.PerishableProps.Clone();
        cookedPerishProps.TransitionedStack.Resolve(world, "cooking container perished stack", true);

        ICoreAPI api = world.Api;
        CollectibleObject.CarryOverFreshness(api, cookingSlotsProvider.Slots, stacks, cookedPerishProps);

        for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
        {
            cookingSlotsProvider.Slots[i].Itemstack = (i == 0) ? stacks[0] : null;
        }

        inputSlot.Itemstack = outputStack;
        return false;
    }
    }
}
