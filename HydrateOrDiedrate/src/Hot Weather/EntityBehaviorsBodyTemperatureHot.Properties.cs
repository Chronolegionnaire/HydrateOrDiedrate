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
    public float CoolingMultiplier => 1f; //TODO

    [Obsolete("this makes use of stats now")]
    public float GearCooling
    {
        get => entity.Stats[HoDStats.Cooling].ValuesByKey.TryGetValue("equipment")?.Value ?? 0;
        
        [Obsolete("GearCooling now uses stats and is no longer settable directly")]
        set { /*Obsolete*/ }
    }

    public float Cooling
    {
        get
        {
            var cooling = entity.Stats.GetBlended(HoDStats.Cooling);
            var mult = entity.Stats.GetBlended(HoDStats.CoolingMul);

            if(XLibSkills.Enabled && entity is EntityPlayer player)
            {
                mult += XLibSkills.GetEquatidianModifier(entity.Api, player.Player); 
                //TODO this used to multiplied with the earlier mult, decide if that should still happen
                //TODO have the XSkill just set the stat rather then checking it ourselves
            }
            return GameMath.Clamp(ApplyBidirectionalMultiplier(cooling, mult), -100f, 100f).GuardFinite();
        }

        [Obsolete("Cooling now uses stats and is no longer settable directly")]
        set { /*Obsolete*/ }
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
