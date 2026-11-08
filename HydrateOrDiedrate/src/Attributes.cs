using Vintagestory.API.Common;

namespace HydrateOrDiedrate;

public static class Attributes
{
    public const string NutritionDeficit = "nutritionDeficit";
    public const string IsBoiling = "HoDisBoiling";
    public const string Healing = "health";
    public const string Cooling = "cooling";
    public const string Hydration = "hydration";

    /// <summary>
    /// Optional block attribute: true or false overrides whether the shovel or pickaxe in well spring mode can turn the block into a well spring
    /// </summary>
    public const string AllowWellSpring = "AllowWellSpring";
}
