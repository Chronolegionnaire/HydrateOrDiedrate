using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough
{
    public interface IAnimalDrinkSource : IPointOfInterest
    {
        float ConsumeOneLiquidPortion(Entity entity);
    }
}
