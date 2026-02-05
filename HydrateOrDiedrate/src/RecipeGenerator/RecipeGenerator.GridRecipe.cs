using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.RecipeGenerator;

public static partial class RecipeGenerator
{
    internal static void ProcessGridRecipe(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe,
        List<object> newRecipes)
    {
        if (recipe is not GridRecipe gridRecipe) return;

        var contentCode = gridRecipe.Attributes?.Token["liquidContainerProps"]?["requiresContent"]?["code"]
            ?.Value<string>();
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

                    // 1.22+: Resolve() replaces ResolveIngredients()
                    if (!newRecipe.Resolve(world, SourceForLogging)) continue;

                    newRecipes.Add(newRecipe);
                }
            }

            return;
        }

        // 1.22+: use ResolvedIngredients (property), not resolvedIngredients (field)
        var resolved = gridRecipe.ResolvedIngredients;
        if (resolved == null || resolved.Length == 0) return;

        Span<bool> matches = stackalloc bool[resolved.Length];

        foreach (var (fromCode, toCodes) in ConversionMappings)
        {
            bool hasMatch = false;

            for (int i = 0; i < resolved.Length; i++)
            {
                // ResolvedIngredients is CraftingRecipeIngredient?[]
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
                else matches[i] = false;
            }

            if (!hasMatch) continue;

            foreach (var toCode in toCodes)
            {
                if (!gridRecipe.TryClone(out var newRecipe)) continue;

                newRecipe.Name = newRecipe.Name?.Clone();
                ModifyRecipeName(newRecipe.Name, toCode);

                // ReplaceCodes expects CraftingRecipeIngredient[]; adapt:
                var newResolved = newRecipe.ResolvedIngredients;
                if (newResolved == null || newResolved.Length != resolved.Length) continue;

                // Convert nullable array to non-nullable temp view for ReplaceCodes
                // (we only touch indices where matches[i] == true, and those must be non-null)
                var tmp = new CraftingRecipeIngredient[newResolved.Length];
                for (int i = 0; i < tmp.Length; i++)
                {
                    tmp[i] = newResolved[i] as CraftingRecipeIngredient;
                }

                ReplaceCodes(world, tmp, matches, toCode);

                // Copy back
                for (int i = 0; i < tmp.Length; i++)
                {
                    newResolved[i] = tmp[i];
                }

                newRecipe.ResolvedIngredients = newResolved;

                if (!newRecipe.Resolve(world, SourceForLogging)) continue;

                newRecipes.Add(newRecipe);
            }
        }
    }

    public static void ReplaceRequiresContentCode(JToken token, AssetLocation toCode) => token["liquidContainerProps"]["requiresContent"]["code"] = string.Intern(toCode.ToString());
}
