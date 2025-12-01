using HydrateOrDiedrate.XSkill;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Hot_Weather;

public partial class EntityBehaviorBodyTemperatureHot : EntityBehavior
{
    private ITreeAttribute TempTree => entity.WatchedAttributes.GetTreeAttribute(tempTreePath);
    public const string tempTreePath = "bodyTemp";
    
    public bool HasWetnessCoolingBonus    { get; private set; }
    public bool HasRoomCoolingBonus       { get; private set; }
    public bool HasLowSunlightCoolingBonus{ get; private set; }
    public bool HasShadeCoolingBonus      { get; private set; }

    public float CoolingMultiplier { get; internal set; } = 1f;
    
    private void SyncCoolingToWatchedAttributes()
    {
        var root = entity.WatchedAttributes;
        var hodCooling = root.GetTreeAttribute("hodCooling") as TreeAttribute;
        if (hodCooling == null)
        {
            hodCooling = new TreeAttribute();
            root["hodCooling"] = hodCooling;
        }
        hodCooling.SetFloat("gearCooling", GearCooling);
        hodCooling.SetFloat("totalCooling", Cooling);
        hodCooling.SetInt("wetBonus",      HasWetnessCoolingBonus     ? 1 : 0);
        hodCooling.SetInt("roomBonus",     HasRoomCoolingBonus        ? 1 : 0);
        hodCooling.SetInt("lowSunBonus",   HasLowSunlightCoolingBonus ? 1 : 0);
        hodCooling.SetInt("shadeBonus",    HasShadeCoolingBonus       ? 1 : 0);
        entity.WatchedAttributes.MarkPathDirty("hodCooling");
    }

    public float GearCooling
    {
        get => TempTree?.GetFloat("gearCooling") ?? 0f;
        set
        {
            var tree = TempTree;
            if (tree is null) return;

            var safeValue = GameMath.Clamp(value.GuardFinite(), -100f, 100f);
            if (safeValue == GearCooling) return;

            tree.SetFloat("gearCooling", safeValue);
            entity.WatchedAttributes.MarkPathDirty(tempTreePath);
        }
    }
    public float Cooling
    {
        get => TempTree?.GetFloat("cooling") ?? 0f;
        set
        {
            var tree = TempTree;
            if(tree is null) return;

            var safeValue = GameMath.Clamp(value.GuardFinite(), -100f, 100f);
            if(safeValue == Cooling) return;

            tree.SetFloat("cooling", safeValue);
            entity.WatchedAttributes.MarkPathDirty(tempTreePath);
        }
    }

    public void InitBodyHeatAttributes()
    {
        MapLegacyData();

        if(!XLibSkills.Enabled) RecalculateCoolingMultiplier();
    }

    private void MapLegacyData()
    {
        var attr = entity.WatchedAttributes;
        attr.RemoveAttribute("currentCoolingHot");
        attr.RemoveAttribute("adjustedCoolingHot");
    }

    public void RecalculateCoolingMultiplier()
    {
        var newCoolingModifier = 1f;
        if(XLibSkills.Enabled) newCoolingModifier *= XLibSkills.GetEquatidianModifier(entity.Api, (entity as EntityPlayer)?.Player);
        CoolingMultiplier = newCoolingModifier;
    }
}
