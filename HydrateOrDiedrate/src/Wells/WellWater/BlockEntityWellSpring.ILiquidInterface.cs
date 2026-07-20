using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Wells.WellWater;

public partial class BlockEntityWellSpring : ILiquidSource
{
    public bool AllowHeldLiquidTransfer => false;

    public float CapacityLitres => IsShallow ? 9 : WellShaftHeight * LitersPerFullBlock;

    public float TransferSizeLitres => 1f;

    #region ItemStack
    public ItemStack GetContent(ItemStack containerStack) => null;

    public WaterTightContainableProps GetContentProps(ItemStack containerStack) => null;

    public float GetCurrentLitres(ItemStack containerStack) => 0f;

    public bool IsFull(ItemStack containerStack) => false;

    public ItemStack TryTakeContent(ItemStack containerStack, int quantity) => null;

    #endregion ItemStack

    private bool TryGetWellInteractionHeight(BlockPos pos, out int height)
    {
        height = pos.Y - Pos.Y;
        return height >= 0 && pos.X == Pos.X && pos.Z == Pos.Z;
    }

    public ItemStack GetContent(BlockPos pos)
    {
        if(!TryGetWellInteractionHeight(pos, out var height)) return null;

        var itemCount = (int)(itemsPerLiter * GetLitersAtAndAbove(height));
        if(itemCount <= 0 ) return null;
        return new ItemStack(WaterItem, itemCount);
    }

    public WaterTightContainableProps GetContentProps(BlockPos pos) => HydrationManager.GetProps(Api.World, WaterItem);

    public float GetCurrentLitres(BlockPos pos)
    {
        if(!TryGetWellInteractionHeight(pos, out var height)) return 0;
        return GetLitersAtAndAbove(height);
    }

    private float GetLitersAtAndAbove(int height)
    {
        if(height <= 1) return TotalLiters;
        var blocksMissing = height - 1;
        return Math.Clamp(TotalLiters - (blocksMissing * LitersPerFullBlock), 0, CapacityLitres);
    }

    public bool IsFull(BlockPos pos) => TotalLiters >= CapacityLitres;

    public ItemStack TryTakeContentLiters(BlockPos pos, float liters)
    {
         if(!TryGetWellInteractionHeight(pos, out var height)) return null;
         
        var litersAvailable = GetLitersAtAndAbove(height);
        var canTake = Math.Clamp(liters, 0, litersAvailable);
        if (canTake <= 0) return null;

        var taken = -TryChangeVolume(-canTake);


        var itemCount = (int)(itemsPerLiter * taken);
        if(itemCount <= 0 ) return null;

        return new ItemStack(WaterItem, itemCount);
    }

    public ItemStack TryTakeContent(BlockPos pos, int quantity)
    {
        if(quantity <= 0) return null;
        return TryTakeContentLiters(pos, quantity / itemsPerLiter);
    }
}
