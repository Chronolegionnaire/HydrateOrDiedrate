using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.XSkill;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate;

public partial class EntityBehaviorThirst
{
    public override string PropertyName() => "thirst";

    // seperate tree for thrist attributes to prevent too many paths from being marked dirty, which would result in full resyncs all the time
    private ITreeAttribute ThirstTree => entity.WatchedAttributes.GetTreeAttribute(thirstTreePath);
    public const string thirstTreePath = "thirst";

    [Obsolete("Use MaxHydration instead")]
    public float MaxThirst { get => MaxHydration;  set => MaxHydration = value; }
    public float MaxHydration
    {
        get => ThirstTree.TryGetFloat("maxHydration") ?? ModConfig.Instance.Thirst.MaxHydration;
        set
        {
            var currentMaxThirst = MaxHydration;
            if (!float.IsFinite(value) || value == currentMaxThirst) return;

            ThirstTree.SetFloat("maxHydration", value);
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);

            CurrentHydration = (CurrentHydration / currentMaxThirst) * value; // adjust current thirst relative to the new max thirst
        }
    }

    [Obsolete("Use CurrentHydration instead")]
    public float CurrentThirst { get => CurrentHydration; set =>  CurrentHydration = value; }
    public float CurrentHydration
    {
        get => ThirstTree.TryGetFloat("currentHydration") ?? MaxHydration;
        set
        {
            var maxThirst = MaxHydration;
            var safeValue = GameMath.Clamp(value.GuardFinite(maxThirst), 0, maxThirst);
            if (safeValue == CurrentHydration) return;

            ThirstTree.SetFloat("currentHydration", safeValue);
            UpdateMovementPenalty();
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);
        }
    }

    public float ThirstRate
    {
        get
        {
            var defaultThirstRate = ModConfig.Instance.Thirst.ThirstRate;
            
            return GameMath.Clamp(
                defaultThirstRate * entity.Stats.GetBlended(HoDStats.ThirstRate),
                1,
                defaultThirstRate * ModConfig.Instance.Thirst.MaxThirstRateMultiplier
            ).GuardFinite(defaultThirstRate);
        }

        [Obsolete("ThirstRate now uses stats and is no longer settable directly")]
        set { /*Obsolete*/ }
    }

    [Obsolete("Use NutritionDeficitAmount instead")] public float HungerReductionAmount { get => NutritionDeficitAmount; set => NutritionDeficitAmount = value; }
    public float NutritionDeficitAmount
    {
        get => ThirstTree.GetFloat("nutritionDeficitAmount");
        set
        {
            var safeValue = Math.Max((float)Math.Ceiling(value.GuardFinite()), 0f);
            if(safeValue == NutritionDeficitAmount) return;
            
            ThirstTree.SetFloat("nutritionDeficitAmount", safeValue);
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);
        }
    }

    public int HydrationLossDelay
    {
        get => ThirstTree.GetInt("hydrationLossDelay");
        set
        {
            if(value == HydrationLossDelay) return;
            ThirstTree.SetInt("hydrationLossDelay", value);
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);
        }
    }

    private float movementPenalty;
    public float MovementPenalty
    {
        get => movementPenalty;
        set
        {

            float safeValue = GameMath.Clamp(value.GuardFinite(), 0, ModConfig.Instance.Thirst.MaxMovementSpeedPenalty);
            if (movementPenalty == safeValue) return;

            movementPenalty = safeValue;
            entity.Stats.Set("walkspeed", "thirstPenalty", -safeValue, false);
        }
    }

    public void MapLegacyData()
    {
        var attr = entity.WatchedAttributes.GetTreeAttribute(thirstTreePath);
        
        if(attr.HasAttribute("maxThirst"))
        {
            MaxHydration = attr.GetFloat("maxThirst");
            attr.RemoveAttribute("maxThirst");
        }
        if(attr.HasAttribute("currentThirst"))
        {
            CurrentHydration = attr.GetFloat("currentThirst");
            attr.RemoveAttribute("currentThirst");
        }
        attr.RemoveAttribute("thirstRate");
    }

    private void InitThirstAttributes()
    {
        if(entity.WatchedAttributes.GetTreeAttribute(thirstTreePath) is null)
        {
            entity.WatchedAttributes.SetAttribute(thirstTreePath, new TreeAttribute());
        }
        MapLegacyData();

        if(!XLibSkills.Enabled) RecalculateMaxThirst();
    }

    public void RecalculateMaxThirst()
    {
        if(entity.Api.Side != EnumAppSide.Server) return;
        var newMaxThirst = ModConfig.Instance.Thirst.MaxHydration;

        if(XLibSkills.Enabled) newMaxThirst *= XLibSkills.GetDromedaryModifier(entity.Api, (entity as EntityPlayer)?.Player);

        MaxHydration = newMaxThirst;
    }
}
