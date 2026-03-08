using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Hot_Weather;

public static class CoolingManager
{
    public static float GetMaxCooling(ItemStack itemStack)
    {
        var itemAttrs = itemStack?.ItemAttributes;
        if (itemAttrs == null) return 0f;
        return itemAttrs[Attributes.Cooling].AsFloat(0f).GuardFinite();
    }
}