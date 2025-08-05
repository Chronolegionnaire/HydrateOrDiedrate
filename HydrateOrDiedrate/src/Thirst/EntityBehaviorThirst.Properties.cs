using HydrateOrDiedrate.Config;
using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate;

public partial class EntityBehaviorThirst
{
    public override string PropertyName() => "thirst";

    // seperate tree for thrist attributes to prevent too many paths from being marked dirty, which would result in full resyncs all the time
    private ITreeAttribute ThirstTree => entity.WatchedAttributes.GetTreeAttribute(thirstTreePath);
    public const string thirstTreePath = "thirst";

    public float MaxThirst
    {
        get => ThirstTree.TryGetFloat("maxThirst") ?? ModConfig.Instance.Thirst.MaxThirst;
        set
        {
            if(value == MaxThirst) return;
            ThirstTree.SetFloat("maxThirst", value.GuardFinite(MaxThirst)); //TODO should we not do something with CurrentThirst here?
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);
        }
    }

    public float CurrentThirst
    {
        get => ThirstTree.TryGetFloat("currentThirst") ?? MaxThirst;
        set
        {
            if(value == CurrentThirst) return;
            ThirstTree.SetFloat("currentThirst", GameMath.Clamp(value.GuardFinite(MaxThirst), 0, MaxThirst));
            UpdateMovementPenalty();
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);
        }
    }

    public float ThirstRate
    {
        get => ThirstTree.TryGetFloat("thirstRate") ?? ModConfig.Instance.Thirst.ThirstDecayRate;
        set
        {
            if(value == ThirstRate) return;
            ThirstTree.SetFloat("thirstRate", value.GuardFinite());
            entity.WatchedAttributes.MarkPathDirty(thirstTreePath);
        }
    }

    public float HungerReductionAmount
    {
        get => ThirstTree.GetFloat("hungerReductionAmount");
        set
        {
            if(value == HungerReductionAmount) return;
            ThirstTree.SetFloat("hungerReductionAmount", Math.Max((float)Math.Ceiling(value.GuardFinite()), 0f));
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
        var attr = entity.WatchedAttributes;
        if(attr.HasAttribute("maxThirst"))
        {
            MaxThirst = attr.GetFloat("maxThirst");
            attr.RemoveAttribute("maxThirst");
        }
        if(attr.HasAttribute("currentThirst"))
        {
            CurrentThirst = attr.GetFloat("currentThirst");
            attr.RemoveAttribute("currentThirst");
        }
        if(attr.HasAttribute("thirstRate"))
        {
            ThirstRate = attr.GetFloat("thirstRate");
            attr.RemoveAttribute("thirstRate");
        }
        if(attr.HasAttribute("hungerReductionAmount"))
        {
            HungerReductionAmount = attr.GetFloat("hungerReductionAmount");
            attr.RemoveAttribute("hungerReductionAmount");
        }
        if(attr.HasAttribute("hydrationLossDelay"))
        {
            HydrationLossDelay = attr.GetInt("hydrationLossDelay");
            attr.RemoveAttribute("hydrationLossDelay");
        }
    }

    private void InitThirstAttributes()
    {
        if(entity.WatchedAttributes.GetTreeAttribute(thirstTreePath) is null)
        {
            entity.WatchedAttributes.SetAttribute(thirstTreePath, new TreeAttribute());
            MapLegacyData();
        }
    }
}
