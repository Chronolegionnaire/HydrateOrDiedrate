using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using HydrateOrDiedrate.Configuration;

namespace HydrateOrDiedrate.EntityBehavior
{
public class EntityBehaviorBodyTemperatureHot : Vintagestory.API.Common.Entities.EntityBehavior
{
private readonly Config _config;
private float _currentCooling;

    public EntityBehaviorBodyTemperatureHot(Entity entity) : base(entity)
    {
        _config = new Config();
        _currentCooling = 0;
        LoadCooling();
    }

    public EntityBehaviorBodyTemperatureHot(Entity entity, Config config) : base(entity)
    {
        _config = config;
        _currentCooling = 0;
        LoadCooling();
    }

    public float CurrentCooling
    {
        get => _currentCooling;
        set
        {
            _currentCooling = GameMath.Clamp(value, 0, float.MaxValue);
            entity.WatchedAttributes.SetFloat("currentCoolingHot", _currentCooling);
            entity.WatchedAttributes.MarkPathDirty("currentCoolingHot");
        }
    }

    public override void OnGameTick(float deltaTime)
    {
        if (!entity.Alive || !_config.HarshHeat) return;

        UpdateCoolingFactor();
    }

    private void UpdateCoolingFactor()
    {
        float coolingFactor = 0f;
        var entityAgent = entity as EntityAgent;
        if (entityAgent == null || entityAgent.GearInventory == null) return;

        foreach (var slot in entityAgent.GearInventory)
        {
            if (slot?.Itemstack == null) continue;

            var cooling = CustomItemWearableExtensions.GetCooling(slot, entity.World.Api);
            if (cooling != null)
            {
                coolingFactor += cooling;
            }
        }

        if (entity.WatchedAttributes.GetFloat("wetness", 0f) > 0)
        {
            coolingFactor *= 1.5f;
        }

        CurrentCooling = coolingFactor;
    }

    public void LoadCooling()
    {
        _currentCooling = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0);
    }

    public override string PropertyName() => "bodytemperaturehot";
}
}