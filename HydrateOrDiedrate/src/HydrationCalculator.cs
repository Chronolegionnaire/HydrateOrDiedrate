using Vintagestory.API.Common;

namespace HydrateOrDiedrate;

public static class HydrationCalculator
{
    public static float GetTotalHydration(IWorldAccessor world, ItemStack[] contentStacks)
    {
        float totalHydration = 0f;

        foreach (ItemStack contentStack in contentStacks)
        {
            if (contentStack != null)
            {
                float hydrationValue = HydrationManager.GetHydration(contentStack);
                totalHydration += hydrationValue;
            }
        }
        return totalHydration;
    }

    private static bool IsLiquidPortion(ItemStack itemStack)
    {
        return itemStack?.Collectible?.GetType()?.Name == "ItemLiquidPortion";
    }
}