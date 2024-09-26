using HydrateOrDiedrate;
using Vintagestory.API.Common;

public static class HydrationCalculator
{
    public static float GetTotalHydration(IWorldAccessor world, ItemStack[] contentStacks)
    {
        float totalHydration = 0f;

        foreach (ItemStack contentStack in contentStacks)
        {
            if (contentStack != null)
            {
                string itemCode = contentStack.Collectible.Code.ToString();
                float hydrationValue = HydrationManager.GetHydration(world.Api, itemCode);

                // Ignore stack size and only use 1 of each item for hydration calculation
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