using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessGridRecipe(
        IServerWorldAccessor world,
        RecipeListInfo recipeListInfo,
        object recipe,
        List<object> newRecipes)
    {
        if (recipe is not GridRecipe gridRecipe) return;
        var contentCode = gridRecipe.Attributes?.Token["liquidContainerProps"]?["requiresContent"]?["code"]?.Value<string>();
        if (!string.IsNullOrEmpty(contentCode))
        {
            foreach (var (fromCode, toCodes) in ConversionMappings)
            {
                if (!MatchCodeString(contentCode, fromCode)) continue;

                foreach (var toCode in toCodes)
                {
                    if (!gridRecipe.TryClone(out var newRecipe)) continue;

                    newRecipe.Name = newRecipe.Name?.Clone();
                    ModifyRecipeName(newRecipe.Name, toCode);
                    ReplaceRequiresContentCode(newRecipe.Attributes.Token, toCode);
                    newRecipes.Add(newRecipe);
                }
            }

            return;
        }
        var resolved = gridRecipe.ResolvedIngredients;
        if (resolved == null || resolved.Length == 0) return;

        Span<bool> matches = stackalloc bool[resolved.Length];

        foreach (var (fromCode, toCodes) in ConversionMappings)
        {
            bool hasMatch = false;

            for (int i = 0; i < resolved.Length; i++)
            {
                if (resolved[i] is not CraftingRecipeIngredient ingredient)
                {
                    matches[i] = false;
                    continue;
                }

                if (MatchCode(ingredient.Code, fromCode))
                {
                    matches[i] = true;
                    hasMatch = true;
                }
                else
                {
                    matches[i] = false;
                }
            }

            if (!hasMatch) continue;

            foreach (var toCode in toCodes)
            {
                if (!gridRecipe.TryClone(out var newRecipe)) continue;

                newRecipe.Name = newRecipe.Name?.Clone();
                ModifyRecipeName(newRecipe.Name, toCode);

                var newResolved = newRecipe.ResolvedIngredients;
                if (newResolved == null || newResolved.Length != resolved.Length) continue;

                for (int i = 0; i < newResolved.Length; i++)
                {
                    if (!matches[i]) continue;
                    if (newResolved[i] is not CraftingRecipeIngredient ingredient) continue;

                    ReplaceCode(world, ingredient, toCode);
                    if (!ingredient.Resolve(world, SourceForLogging, newRecipe))
                    {
                        newResolved = null;
                        break;
                    }
                }

                if (newResolved == null) continue;

                newRecipe.ResolvedIngredients = newResolved;
                newRecipes.Add(newRecipe);
            }
        }
    }

    private static void ReplaceCode(IWorldAccessor world, CraftingRecipeIngredient ingredient, AssetLocation toCode)
    {
        ingredient.Code = toCode;
    }

    public static void ReplaceRequiresContentCode(JToken token, AssetLocation toCode)
    {
        token["liquidContainerProps"]["requiresContent"]["code"] = string.Intern(toCode.ToString());
    }
}