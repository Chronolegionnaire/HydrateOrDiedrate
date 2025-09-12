using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace HydrateOrDiedrate.RecipeGenerator;

public delegate void RecipeItemProcessor(IServerWorldAccessor world, RecipeListInfo recipeListInfo, object recipe, List<object> newRecipes);

public static partial class RecipeGenerator
{
    public const string SourceForLogging = "HoD recipe generator";

    internal static readonly Dictionary<AssetLocation, AssetLocation[]> DeadlyConversionMappings = new()
    {
        [new("game", "waterportion")] = [
            new("hydrateordiedrate", "waterportion-fresh-well-tainted"),
            new("hydrateordiedrate", "waterportion-fresh-well-poisoned")
        ]
    };

    internal static readonly Dictionary<AssetLocation, AssetLocation[]> ConversionMappings = new()
    {
        [new("game", "waterportion")] = [
            new("hydrateordiedrate", "waterportion-boiled-natural-clean"),
            new("hydrateordiedrate", "waterportion-boiled-rain-clean"),
            new("hydrateordiedrate", "waterportion-fresh-distilled-clean"),
            new("hydrateordiedrate", "waterportion-fresh-rain-clean"),
            new("hydrateordiedrate", "waterportion-fresh-well-clean"),
        ],

        [new("game", "saltwaterportion")] = [
            new("hydrateordiedrate", "waterportion-salt-well-clean"),
        ],
    };

    private static void FindAndAppendRecipeLists(object target, List<RecipeListInfo> result)
    {
        foreach(var property in target.GetType().GetProperties(AccessTools.all))
        {
            if (!property.CanRead || !property.Name.Contains("recipes", StringComparison.OrdinalIgnoreCase) || !typeof(IList).IsAssignableFrom(property.PropertyType)) continue;

            try
            {
                if(property.GetValue(target) is not IList recipeList) continue;

                result.Add(new RecipeListInfo(target, property, recipeList));
            }
            catch { /* ignore */ }
        }

        foreach(var field in target.GetType().GetFields(AccessTools.all))
        {
            if (!field.Name.Contains("recipes", StringComparison.OrdinalIgnoreCase) || !typeof(IList).IsAssignableFrom(field.FieldType)) continue;

            try
            {
                if(field.GetValue(target) is not IList recipeList) continue;

                result.Add(new RecipeListInfo(target, field, recipeList));
            }
            catch { /* ignore */ }
        }
    }

    private static IEnumerable<RecipeListInfo> FindRecipeLists(ICoreAPI api)
    {
        var result = new List<RecipeListInfo>();

        FindAndAppendRecipeLists(api.World, result);

        foreach (var modSystem in api.ModLoader.Systems)
        {
            FindAndAppendRecipeLists(modSystem, result);
        }

        // Deduplicate lists, preferring those whose target member can be set (i.e. not readonly)
        return result.GroupBy(list => list.RecipeList)
            .Select(duplicateLists => duplicateLists.OrderBy(info => info.TargetMember.CanSetValue()).First());
    }

    public static void GenerateVariants(ICoreServerAPI api, ILogger logger)
    {
        logger.Event("Starting HoD water variant recipe generation...");
        var recipeLists = FindRecipeLists(api);

        foreach(var recipeList in recipeLists)
        {
            try
            {

                var processor = GetRecipeListProcessor(recipeList);
                if(processor is null)
                {
                    logger.VerboseDebug("Skipping recipe list with element type {0}, as it is not supported for HoD water variant generation.", recipeList.ElementType);
                    continue;
                }

                recipeList.GenerateVariantsForHoDWater(api.World, processor, logger);
            }
            catch(Exception ex)
            {
                logger.Error("[{0}] Exception generating HoD water variant recipes for {1} ({2}): {3}", recipeList.Source, recipeList.TargetObject, recipeList.HostMemberName, ex);
            }
        }
        logger.Event("HoD water variant recipe generation completed.");
    }

    public static RecipeItemProcessor GetRecipeListProcessor(RecipeListInfo recipeListInfo)
    {
        var recipeType = recipeListInfo.ElementType;
        if (typeof(GridRecipe).IsAssignableFrom(recipeType)) return ProcessGridRecipe;
        if(typeof(CookingRecipe).IsAssignableFrom(recipeType)) return ProcessCookingRecipe;

        var baseRecipeInterface = recipeType.FindGenericInterfaceDefinition(typeof(IRecipeBase<>));
        if(baseRecipeInterface is not null)
        {
            return (RecipeItemProcessor) Delegate.CreateDelegate(
                typeof(RecipeItemProcessor),
                AccessTools.Method(typeof(RecipeGenerator), nameof(ProcessIRecipeBase)).MakeGenericMethod(baseRecipeInterface.GetGenericArguments()[0])
            );
        }

        if(recipeType.FullName == "ACulinaryArtillery.DoughRecipe") return ProcessACADoughRecipe;
        if(recipeType.FullName == "ACulinaryArtillery.SimmerRecipe") return ProcessACASimmerRecipe;

        return null;
    }

    public static bool MatchCode(AssetLocation code, AssetLocation match) => code is not null && code.Domain == match.Domain && code.Path == match.Path;

    public static bool MatchCodeString(ReadOnlySpan<char> code, AssetLocation match)
    {
        var path = code.ExtractSegment(':', out code);
        return path.SequenceEqual(match.Path) && ((code.IsEmpty && "game" == match.Domain) || code.SequenceEqual(match.Domain));
    }
    
    public static void ModifyRecipeName(AssetLocation name, AssetLocation toCode)
    {
        name.Domain = toCode.Domain;
        name.Path = $"-HoD-{name.Path}-{toCode.Path}";
    }

    public static void ReplaceCodes(IWorldAccessor world, CraftingRecipeIngredient[] ingredients, Span<bool> matches, AssetLocation toCode)
    {
        for(var  i = 0;  i < ingredients.Length; i++)
        {
            if (!matches[i]) continue;
            var ingredient = ingredients[i];
            ingredient.Code = toCode;
            ingredient.Resolve(world, SourceForLogging);
        }
    }
    public static void ReplaceCodesWithoutResolve(IWorldAccessor world, IRecipeIngredient[] ingredients, Span<bool> matches, AssetLocation toCode)
    {
        for(var  i = 0;  i < ingredients.Length; i++)
        {
            if (!matches[i]) continue;
            var ingredient = ingredients[i];
            ingredient.Code = toCode;
        }
    }
}
