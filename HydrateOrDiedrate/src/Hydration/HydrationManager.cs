using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate;

public static class HydrationManager
{
    private static readonly Dictionary<AssetLocation, Vintagestory.API.Common.Func<ItemStack, float>> CustomHydrationEvaluators = [];

    public static void RegisterHydrationEvaluator(AssetLocation code, Vintagestory.API.Common.Func<ItemStack, float> evaluator)
    {

        if (code is null) throw new ArgumentNullException(nameof(code));
        else if (code.IsWildCard) throw new ArgumentException("Code may not be a wildcard", nameof(code));

        CustomHydrationEvaluators[code] = evaluator ?? throw new ArgumentNullException(nameof(code));
    }

    public static float GetHydration(ItemStack itemStack)
    {
        var collectible = itemStack?.Collectible;
        if (collectible?.Code is null) return 0f;

        if (CustomHydrationEvaluators.TryGetValue(collectible.Code, out var evaluator)) return evaluator(itemStack);

        return collectible.Attributes?.Token.Value<int>(Attributes.Hydration) ?? 0f;
    }

    public static float GetBlockHydration(ICoreAPI api, Block block)
    {
        try
        {
            var token = block?.Attributes?.Token;
            if (token is null) return 0f;

            if (token["waterTightContainerProps"] is JObject containerToken)
            {
                var props = containerToken.ToObject<WaterTightContainableProps>();
                if (props is not null)
                {
                    var liquidItem = props.WhenFilled.Stack;
                    if (liquidItem.Resolve(api.World, nameof(GetBlockHydration))) return GetHydration(liquidItem.ResolvedItemstack);
                }
            }

            return token.Value<float>(Attributes.Hydration);
        }
        catch (Exception ex)
        {
            api.Logger.Error("[hydrateordiedrate] Failed to fetch hydration for {0}: {1}", block, ex);
        }

        return 0f;
    }

    //TODO
    public static bool IsBoiling(ICoreAPI api, CollectibleObject collectible) => collectible.Attributes?.Token.Value<bool>(Attributes.IsBoiling) ?? false;

    public static int GetHealing(ICoreAPI api, CollectibleObject collectible)
    {
        var resultFromProps = GetProps(api, collectible)?.NutritionPropsPerLitre?.Health;
        if(resultFromProps is not null) return (int) (resultFromProps.Value < 0 ? resultFromProps.Value : 0);
        return collectible.Attributes?.Token.Value<int>(Attributes.Healing) ?? 0;
    }

    public static int GetHungerReduction(ICoreAPI api, CollectibleObject collectible)
    {
        var resultFromProps = GetProps(api, collectible)?.NutritionPropsPerLitre?.Satiety;
        if(resultFromProps is not null) return (int) (resultFromProps.Value < 0 ? -resultFromProps.Value : 0);
        return collectible.Attributes?.Token.Value<int>(Attributes.HungerReduction) ?? 0;
    }

    private static WaterTightContainableProps GetProps(ICoreAPI api, CollectibleObject collectible)
    {
        if(collectible.Attributes?.Token is not JObject token || token["waterTightContainerProps"] is not JObject propsToken) return null;
        var result = propsToken.ToObject<WaterTightContainableProps>();
        if(result is null) return null;
        if(collectible is Block && result.WhenFilled.Stack is JsonItemStack itemStack && itemStack.Resolve(api.World, nameof(HydrationManager)))
        {
            var resultingItemProps = GetProps(api, itemStack.ResolvedItemstack.Collectible);
            if(resultingItemProps is not null) return resultingItemProps;
        }

        return result;
    }
}