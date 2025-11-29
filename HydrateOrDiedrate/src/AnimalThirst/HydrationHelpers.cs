using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.AnimalThirst;

public static class HydrationHelpers
{
    public static float GetHydration(Entity entity)
    {
        var tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
        return tree?.GetFloat("hydration", 0f) ?? 0f;
    }

    public static void AddHydration(Entity entity, float amount)
    {
        var tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
        if (tree == null)
        {
            tree = new TreeAttribute();
            entity.WatchedAttributes.SetAttribute("hunger", tree);
        }

        float hyd = tree.GetFloat("hydration", 0f) + amount;
        hyd = GameMath.Clamp(hyd, 0f, 10f);

        tree.SetFloat("hydration", hyd);
        tree.SetDouble("lastDrinkHours", entity.World.Calendar.TotalHours);
        entity.WatchedAttributes.MarkPathDirty("hunger");
    }

    public static bool IsHydratedForBreeding(Entity entity, float minHydration, double maxHoursSinceDrink)
    {
        var tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
        if (tree == null) return false;

        float hydration = tree.GetFloat("hydration", 0f);
        double lastDrink = tree.GetDouble("lastDrinkHours", double.NegativeInfinity);
        double hoursNow = entity.World.Calendar.TotalHours;

        if (hydration < minHydration) return false;
        if (hoursNow - lastDrink > maxHoursSinceDrink) return false;

        return true;
    }
}
