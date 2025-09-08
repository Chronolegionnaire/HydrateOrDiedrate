using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessCookingRecipe(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe, List<object> newRecipes)
    {
        if(recipe is not CookingRecipe cookingRecipe || cookingRecipe.Ingredients is null || (cookingRecipe.Code is not null && cookingRecipe.Code.StartsWith("hydrateordiedrate:"))) return;

        bool matchesFound = false;
        foreach ((var fromCode, var toCodes) in ConversionMappings.Union(DeadlyConversionMappings))
        for (int i = 0; i < cookingRecipe.Ingredients.Length; i++)
        {
            if(cookingRecipe.Ingredients[i] is not CookingRecipeIngredient ingredient) continue;

            foreach(var stack in ingredient.ValidStacks)
            {
                if(!MatchCode(stack.Code, fromCode)) continue;

                ingredient.ValidStacks =
                [
                    ..ingredient.ValidStacks,
                    ..toCodes.Select(code => new CookingRecipeStack
                    {
                        Code = code,
                        StackSize = stack.StackSize,
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
}
