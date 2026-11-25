using HydrateOrDiedrate.XSkill;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Hot_Weather;

public partial class EntityBehaviorBodyTemperatureHot : EntityBehavior
{

    [Obsolete("this makes use of stats now")]
    public bool HasWetnessCoolingBonus => entity.Stats[HoDStats.Cooling].ValuesByKey.TryGetValue("wetness") is { Value: > 0 };
    
    [Obsolete("this makes use of stats now")]
    public bool HasRoomCoolingBonus => entity.Stats[HoDStats.Cooling].ValuesByKey.TryGetValue("room") is { Value: > 0 };
    
    [Obsolete("this makes use of stats now")]
    public bool HasLowSunlightCoolingBonus => entity.Stats[HoDStats.Cooling].ValuesByKey.TryGetValue("lowSunlight") is { Value: > 0 };

    [Obsolete("this makes use of stats now")]
    public bool HasShadeCoolingBonus => entity.Stats[HoDStats.Cooling].ValuesByKey.TryGetValue("shade") is { Value: > 0 };

    [Obsolete("this makes use of stats now")]
    public float CoolingMultiplier => entity.Stats[HoDStats.CoolingMul].GetBlended();

    [Obsolete("this makes use of stats now")]
    public float GearCooling
    {
        get => entity.Stats[HoDStats.Cooling].ValuesByKey.TryGetValue("gear")?.Value ?? 0;
        
        [Obsolete("GearCooling now uses stats and is no longer settable directly")]
        set { /*Obsolete*/ }
    }

    public float Cooling
    {
        get
        {
            var cooling = entity.Stats.GetBlended(HoDStats.Cooling);
            var mult = entity.Stats.GetBlended(HoDStats.CoolingMul);

            if(cooling > 0)
            {
                cooling = cooling * mult;
            }
            else if(cooling < 0)
            {
                cooling /= mult;
            }

            return GameMath.Clamp(cooling, -100f, 100f).GuardFinite();
        }

        [Obsolete("Cooling now uses stats and is no longer settable directly")]
        set { /*Obsolete*/ }
    }

        //TODO
    private static float ApplyBidirectionalMultiplier(float value, float factor)
    {
        if (factor <= 0f || Math.Abs(value) < float.Epsilon) return value;
        if (value > 0f) return value * factor;
        return value / factor;
    }

    private void MapLegacyData()
    {
        var attr = entity.WatchedAttributes;
        attr.RemoveAttribute("hodCooling");
        var bodyTempTree = attr.GetTreeAttribute("bodyTemp");
        if(bodyTempTree is not null)
        {
            bodyTempTree.RemoveAttribute("cooling");
            bodyTempTree.RemoveAttribute("gearCooling");
        }
    }

    [Obsolete("This is now fetched when relevant, this method does nothing anymore")]
    public void RecalculateCoolingMultiplier(){}
}
