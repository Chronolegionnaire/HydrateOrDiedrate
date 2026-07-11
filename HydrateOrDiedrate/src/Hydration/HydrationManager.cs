using HydrateOrDiedrate.Keg;
using HydrateOrDiedrate.Wells;
using HydrateOrDiedrate.Wells.WellWater;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
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
    
    //TODO interface/provider
    public static float GetHydration(this ItemStack itemStack)
    {
        var collectible = itemStack?.Collectible;
        if (collectible?.Code is null) return 0f;

        if (CustomHydrationEvaluators.TryGetValue(collectible.Code, out var evaluator)) return evaluator(itemStack);

        return collectible.Attributes?[Attributes.Hydration].AsFloat() ?? 0f;
    }

    //TODO There should be no methods for blocks other then GetLiquidFromBlock

    public static float GetBlockHydration(ICoreAPI api, Block block)
    {
        try
        {
            var liquidStack = block.GetLiquidFromBlock(api);
            if(liquidStack is null) return 0f;

            return GetHydration(liquidStack);
        }
        catch (Exception ex)
        {
            api.Logger.Error("[hydrateordiedrate] Failed to fetch hydration for {0}: {1}", block, ex);
        }

        return 0f;
    }

    #nullable enable

    public static ILiquidSource? GetLiquidSourceForDrinking(this Block block, IWorldAccessor world, BlockPos pos)
    {
        if (block.HasBehavior<BlockBehaviorWellWaterFinite>())
        {
            return WellBlockUtils.FindGoverningSpring(world.Api, block, pos);
        }

        var liquidSource = block.GetInterface<ILiquidSource>(world, pos);
        if(liquidSource is not null)
        {
            if(block is BlockLiquidContainerBase liquidContainer)
            {
                if(!liquidContainer.CanDrinkFrom) return null;
                if(block is BlockBarrel && world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBarrel barrelEntity && barrelEntity.Sealed) return null;
            }
            return liquidSource;
        }

        return null;
    }

    public static ItemStack? GetLiquidForDrinking(this Block block, IWorldAccessor world, BlockPos pos)
    {
        var liquidSource = GetLiquidSourceForDrinking(block, world, pos);
        if(liquidSource is not null) return liquidSource.GetContent(pos);

        return block.GetWhenFilledStack(world);
    }

    public static ItemStack? GetWhenFilledStack(this Block block, IWorldAccessor world)
    {
        if (block.Attributes is null) return null;

        var token = block.Attributes["waterTightContainerProps"];
        if(!token.Exists) return null;

        var props = token.AsObject<WaterTightContainableProps>(null, block.Code.Domain);

        var liquidItem = props?.WhenFilled?.Stack;
        if (liquidItem is not null && liquidItem.Resolve(world, nameof(GetLiquidForDrinking))) return liquidItem.ResolvedItemstack;

        return null;
    }

    [Obsolete("Use GetLiquidForDrinking instead")]
    public static ItemStack? GetLiquidFromBlock(this Block block, ICoreAPI api)
    {
        if (block.Attributes?.Token?["waterTightContainerProps"] is not JObject containerToken) return null;

        if (containerToken.ToObject<WaterTightContainableProps>() is { } props)
        {
            var liquidItem = props.WhenFilled.Stack;
            if (liquidItem.Resolve(api.World, nameof(GetBlockHydration))) return liquidItem.ResolvedItemstack;
        }
        return null;
    }
    #nullable disable

    //TODO interface/provider
    public static bool IsBoiling(ICoreAPI api, CollectibleObject collectible) => collectible.Attributes?.Token.Value<bool>(Attributes.IsBoiling) ?? false;

    public static int GetHealing(ICoreAPI api, CollectibleObject collectible)
    {
        var resultFromProps = GetProps(api.World, collectible)?.NutritionPropsPerLitre?.Health;
        if(resultFromProps is not null) return (int) (resultFromProps.Value < 0 ? resultFromProps.Value : 0);
        return collectible.Attributes?.Token.Value<int>(Attributes.Healing) ?? 0;
    }

    [Obsolete("Use GetNutritionDeficit instead")] 
    public static int GetHungerReduction(ICoreAPI api, CollectibleObject collectible) => GetNutritionDeficit(api.World, collectible);
    
    public static int GetNutritionDeficit(IWorldAccessor world, CollectibleObject collectible)
    {
        var resultFromProps = GetProps(world, collectible)?.NutritionPropsPerLitre?.Satiety;
        if(resultFromProps is not null) return (int) (resultFromProps.Value < 0 ? -resultFromProps.Value : 0);
        return collectible.Attributes?.Token.Value<int>(Attributes.NutritionDeficit) ?? 0;
    }

    public static WaterTightContainableProps GetProps(IWorldAccessor world, CollectibleObject collectible)
    {
        var token = collectible.Attributes?["waterTightContainerProps"];
        if(token is not { Exists: true }) return null;

        var result = token.AsObject<WaterTightContainableProps>(null, collectible.Code.Domain);

        if(result is null) return null;
        if(collectible is Block && result.WhenFilled.Stack is JsonItemStack itemStack && itemStack.Resolve(world, nameof(HydrationManager)))
        {
            var resultingItemProps = GetProps(world, itemStack.ResolvedItemstack.Collectible);
            if(resultingItemProps is not null) return resultingItemProps;
        }

        return result;
    }
}