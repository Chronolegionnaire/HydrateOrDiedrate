using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessIRecipeBase<T>(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe, List<object> newRecipes) where T : IRecipeBase<T>
    {
        if(recipe is not IRecipeBase<T> recipeBase || recipeBase.Ingredients is null) return;

        Span<bool> matches = stackalloc bool[recipeBase.Ingredients.Length];
        foreach((var fromCode, var toCodes) in ConversionMappings)
        {
            bool hasMatch = false;
            for (int i = 0; i < recipeBase.Ingredients.Length; i++)
            {
                if(recipeBase.Ingredients[i] is not BarrelRecipeIngredient ingredient) continue;
                if (MatchCode(ingredient.Code, fromCode))
                {
                    matches[i] = true;
                    hasMatch = true;
                }
                else matches[i] = false;

            }
            if(!hasMatch) continue;

            foreach(var toCode in toCodes)
            {
                if(!recipeBase.TryClone(out var newRecipe)) continue;
                newRecipe.Name = newRecipe.Name?.Clone();
                ModifyRecipeName(newRecipe.Name, toCode);
                ReplaceCodesWithoutResolve(world, newRecipe.Ingredients, matches, toCode);
                if(!newRecipe.Resolve(world, SourceForLogging)) continue;

                newRecipes.Add(newRecipe);
            }
        }
    }
}
