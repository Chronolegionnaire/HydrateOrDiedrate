using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessIRecipeBase(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe,
        List<object> newRecipes)
    {
        if (recipe is not IRecipeBase recipeBase) return;
        var recipeType = recipe.GetType();
        var ingrMember =
            (MemberInfo)recipeType.GetProperty("Ingredients", AccessTools.all) ??
            recipeType.GetField("Ingredients", AccessTools.all);

        if (ingrMember == null) return;

        IRecipeIngredient[] ingredients = ingrMember switch
        {
            PropertyInfo pi => pi.GetValue(recipe) as IRecipeIngredient[],
            FieldInfo fi => fi.GetValue(recipe) as IRecipeIngredient[],
            _ => null
        };

        if (ingredients == null || ingredients.Length == 0) return;

        Span<bool> matches = stackalloc bool[ingredients.Length];

        foreach ((var fromCode, var toCodes) in ConversionMappings)
        {
            bool hasMatch = false;

            for (int i = 0; i < ingredients.Length; i++)
            {
                if (ingredients[i] is not BarrelRecipeIngredient bri)
                {
                    matches[i] = false;
                    continue;
                }

                if (MatchCode(bri.Code, fromCode))
                {
                    matches[i] = true;
                    hasMatch = true;
                }
                else matches[i] = false;
            }

            if (!hasMatch) continue;

            foreach (var toCode in toCodes)
            {
                var cloned = recipeBase.CloneAsInterface();
                if (cloned == null) continue;
                var newRecipeObj = cloned;
                if (newRecipeObj.Name != null)
                {
                    newRecipeObj.Name = newRecipeObj.Name.Clone();
                    ModifyRecipeName(newRecipeObj.Name, toCode);
                }
                var newIngredients = ingrMember switch
                {
                    PropertyInfo pi => pi.GetValue(newRecipeObj) as IRecipeIngredient[],
                    FieldInfo fi => fi.GetValue(newRecipeObj) as IRecipeIngredient[],
                    _ => null
                };

                if (newIngredients == null || newIngredients.Length != ingredients.Length) continue;

                ReplaceCodesWithoutResolve(world, newIngredients, matches, toCode);
                if (!newRecipeObj.Resolve(world, SourceForLogging)) continue;

                newRecipes.Add(newRecipeObj);
            }
        }
    }
}
