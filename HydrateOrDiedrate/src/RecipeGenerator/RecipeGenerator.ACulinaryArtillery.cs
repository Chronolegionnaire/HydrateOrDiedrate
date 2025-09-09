using ACulinaryArtillery;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessACADoughRecipe(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe, List<object> newRecipes)
    {
        if(recipe is not DoughRecipe cookingRecipe || cookingRecipe.Ingredients is null) return;

        bool matchesFound = false;
        foreach ((var fromCode, var toCodes) in ConversionMappings)
        for (int i = 0; i < cookingRecipe.Ingredients.Length; i++)
        {
            if(cookingRecipe.Ingredients[i] is not DoughIngredient ingredient) continue;

            foreach(var stack in ingredient.Inputs)
            {
                if(!MatchCode(stack.Code, fromCode)) continue;

                ingredient.Inputs =
                [
                    ..ingredient.Inputs,
                    ..toCodes.Select(code => new CraftingRecipeIngredient
                    {
                        Code = code,
                        Quantity = stack.Quantity,
                        Type = EnumItemClass.Item
                    })
                ];

                matchesFound = true;
                break;
            }

        }

        if(!matchesFound) return;
        cookingRecipe.Resolve(world, SourceForLogging);
    }

    internal static void ProcessACASimmerRecipe(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe, List<object> newRecipes)
    {
        if(recipe is not SimmerRecipe cookingRecipe || cookingRecipe.Ingredients is null) return;

        Span<bool> matches = stackalloc bool[cookingRecipe.Ingredients.Length];
        foreach((var fromCode, var toCodes) in ConversionMappings)
        {
            bool hasMatch = false;
            for (int i = 0; i < cookingRecipe.Ingredients.Length; i++)
            {
                if(cookingRecipe.Ingredients[i] is not CraftingRecipeIngredient ingredient) continue;
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
                var newRecipe = cookingRecipe.Clone();
                newRecipe.Name = newRecipe.Name.Clone();
                ModifyRecipeName(newRecipe.Name, toCode);
                ReplaceCodes(world, newRecipe.Ingredients, matches, toCode);
                if(!newRecipe.Resolve(world, SourceForLogging)) continue;
                newRecipes.Add(newRecipe);
            }
        }
    }
}
