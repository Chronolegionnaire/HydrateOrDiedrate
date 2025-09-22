using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessGridRecipe(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe, List<object> newRecipes)
    {
        if(recipe is not GridRecipe gridRecipe) return;

        var contentCode = gridRecipe.Attributes?.Token["liquidContainerProps"]?["requiresContent"]?["code"].Value<string>();
        if (!string.IsNullOrEmpty(contentCode))
        {
            foreach (var (fromCode, toCodes) in ConversionMappings)
            {
                if (!MatchCodeString(contentCode, fromCode)) continue;

                foreach(var toCode in toCodes)
                {
                    if(!gridRecipe.TryClone(out var newRecipe)) continue;
                    newRecipe.Name = newRecipe.Name?.Clone();
                    ModifyRecipeName(newRecipe.Name, toCode);
                    ReplaceRequiresContentCode(newRecipe.Attributes.Token, toCode);
                    newRecipe.ResolveIngredients(world);
                    if(!newRecipe.ResolveIngredients(world)) continue;
                    newRecipes.Add(newRecipe);
                }
            }

            return;
        }

        Span<bool> matches = stackalloc bool[gridRecipe.resolvedIngredients.Length];
        foreach((var fromCode, var toCodes) in ConversionMappings)
        {
            bool hasMatch = false;
            for (int i = 0; i < gridRecipe.resolvedIngredients.Length; i++)
            {
                if(gridRecipe.resolvedIngredients[i] is not GridRecipeIngredient ingredient) continue;
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
                if(!gridRecipe.TryClone(out var newRecipe)) continue;
                newRecipe.Name = newRecipe.Name?.Clone();
                ModifyRecipeName(newRecipe.Name, toCode);
                ReplaceCodes(world, newRecipe.resolvedIngredients, matches, toCode);
                if(!newRecipe.ResolveIngredients(world)) continue;
                newRecipes.Add(newRecipe);
            }
        }
    }

    public static void ReplaceRequiresContentCode(JToken token, AssetLocation toCode) => token["liquidContainerProps"]["requiresContent"]["code"] = string.Intern(toCode.ToString());
}
