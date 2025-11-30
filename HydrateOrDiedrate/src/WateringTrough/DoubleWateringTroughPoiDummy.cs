using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough
{
    public class DoubleWateringTroughPoiDummy : IAnimalFoodSource, IAnimalDrinkSource, IPointOfInterest
    {
        private BlockEntityWateringTrough be;

        public DoubleWateringTroughPoiDummy(BlockEntityWateringTrough be) => this.be = be;

        public Vec3d Position { get; set; }

        public string Type => this.be.Type;

        public float ConsumeOnePortion(Entity entity) => this.be.ConsumeOnePortion(entity);
        
        public float ConsumeOneLiquidPortion(Entity entity) => this.be.ConsumeOneLiquidPortion(entity);
        
        public bool IsSuitableFor(Entity entity, CreatureDiet diet)
        {
            return this.be.IsSuitableFor(entity, diet);
        }
    }
}
