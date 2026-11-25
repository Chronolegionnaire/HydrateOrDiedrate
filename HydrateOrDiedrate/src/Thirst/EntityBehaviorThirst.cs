using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Hot_Weather;
using InsanityLib.Generators.Attributes;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate;

[AutoRegistryName("HoD:thirst")]
public partial class EntityBehaviorThirst : EntityBehavior
{
    public EntityBehaviorThirst(Entity entity) : base(entity)
    {
        entity.Stats.Register(HoDStats.ThirstRate, EnumStatBlendType.FlatMultiply);
    }

    private StoryStructuresSpawnConditions storySys;

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);

        //HACK: manually updating blend types as workaround for https://github.com/anegostudios/VintageStory-Issues/issues/10278
        entity.WatchedAttributes.RegisterModifiedListener("stats", () =>
        {
            entity.Stats[HoDStats.ThirstRate].BlendType = EnumStatBlendType.FlatMultiply;
        });

        storySys = entity.Api.ModLoader.GetModSystem<StoryStructuresSpawnConditions>(false);
        InitThirstAttributes();
        UpdateMovementPenalty();
    }

    private void UpdateMovementPenalty()
    {
        float currentThirst = CurrentHydration;
        var threshold = ModConfig.Instance.Thirst.MovementSpeedPenaltyThreshold;
        if(threshold < 0 || currentThirst > threshold)
        {
            MovementPenalty = 0;
        }
        else if(currentThirst <= 0)
        {
            MovementPenalty = ModConfig.Instance.Thirst.MaxMovementSpeedPenalty;
        }
        else if(threshold > 0)
        {
            MovementPenalty = ModConfig.Instance.Thirst.MaxMovementSpeedPenalty * (1 - (currentThirst / threshold));
        }
        else MovementPenalty = 0;
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
        if (damageSource.Source == EnumDamageSource.Revive && damageSource.Type == EnumDamageType.Heal)
        {
            if (ModConfig.Instance.Thirst.AntiThirstScumming)
            {
                if (CurrentHydration <= 0f)
                {
                    CurrentHydration = 0.1f * MaxHydration;
                }
            }
            else
            {
                CurrentHydration = ModConfig.Instance.Thirst.ThirstPercentageOnRespawn * MaxHydration;
                NutritionDeficitAmount = 0;
            }
        }
    }

    private void ApplyThirstDamage(float thirstDamage)
    {
        if(thirstDamage <= 0) return;
        entity.ReceiveDamage(new DamageSource
        {
            Source = EnumDamageSource.Internal,
            Type = EnumDamageType.Injury
        }, thirstDamage);
    }

    public void HandleConsumption(float totalHydration, float totalIntox, bool isBoiling = false)
    {
        var hydLossDelay = HydrationManager.CalculateHydLossDelay(totalHydration, totalIntox);
        ModifyThirst(totalHydration, hydLossDelay);

        //Nutrient Penalty is handled by satiety path
        if (isBoiling)
        {
            var boilingDamage = ModConfig.Instance.Thirst.BoilingWaterDamage;
            if(boilingDamage > 0)
            {
                //TODO boiling water blocks don't give boiling water so this doesn't really trigger -_-
                entity.ReceiveDamage(new DamageSource
                {
                    Source = EnumDamageSource.Internal,
                    Type = EnumDamageType.Heat
                }, boilingDamage);
            }
        }
    }

    public void ModifyThirst(float amount, float hydLossDelay = 0)
    {
        CurrentHydration += amount.GuardFinite();

        int hydrationLossDelay = HydrationLossDelay;
        if (hydLossDelay > hydrationLossDelay)
        {
            HydrationLossDelay = (int)Math.Floor(hydLossDelay);
        }
    }
    
    public const float thirstInterval = 10f; // seconds
    private float thirstDelta;

    public const float thirstDamageInterval = 10f; // seconds
    private float thirstDamageDelta;

    public const float fastInterval = 2f; // seconds
    private float fastDelta;

    private long lastMoveMs;
    
    private float hydLossCarry;

    public override void OnGameTick(float deltaTime)
    {
        if (entity.World.Side != EnumAppSide.Server || !ModConfig.Instance.Thirst.Enabled || !entity.Alive || entity is not EntityPlayer playerEntity) return;
        if (playerEntity.Player?.WorldData.CurrentGameMode != EnumGameMode.Survival) return;

        fastDelta += deltaTime;
        if (fastDelta >= fastInterval)
        {
            var controls = playerEntity.Controls;
            if (controls.TriesToMove || controls.Jump || controls.LeftMouseDown || controls.RightMouseDown)
            {
                lastMoveMs = entity.World.ElapsedMilliseconds;
            }


            UpdateHydrationLossDelay(fastDelta);
            fastDelta = 0f;
        }

        if(HydrationLossDelay > 0) return;

        thirstDelta += deltaTime;
        if (thirstDelta >= thirstInterval)
        {
            HandleAccumulatedThirst(playerEntity, thirstDelta);
            thirstDelta = 0;
        }

        if (CurrentHydration > 0) return;

        thirstDamageDelta += deltaTime;
        if (thirstDamageDelta >= thirstDamageInterval)
        {
            ApplyThirstDamage(ModConfig.Instance.Thirst.ThirstDamage);
            thirstDamageDelta -= thirstDamageInterval;
        }
    }

    private const float DefaultSpeedOfTime = 60f;
    private const float DefaultCalendarSpeedMul = 0.5f;

    public float CalculateMultiplierPerGameSecond()
    {
        float currentSpeedOfTime = entity.World.Calendar.SpeedOfTime.GuardFinite(DefaultSpeedOfTime);
        float currentCalendarSpeedMul = entity.World.Calendar.CalendarSpeedMul.GuardFinite(DefaultCalendarSpeedMul);

        float multiplierPerGameSec = Util.GuardFinite((currentSpeedOfTime / DefaultSpeedOfTime) * (currentCalendarSpeedMul / DefaultCalendarSpeedMul), 1f);

        return multiplierPerGameSec;
    }

    private void UpdateHydrationLossDelay(float elapsedSeconds)
    {
        int currentDelay = HydrationLossDelay;
        if (currentDelay <= 0) return;

        hydLossCarry += CalculateMultiplierPerGameSecond() * elapsedSeconds;

        int whole = (int)hydLossCarry;
        if (whole > 0)
        {
            HydrationLossDelay = Math.Max(0, currentDelay - whole);
            hydLossCarry -= whole;
        }
    }

    private void HandleAccumulatedThirst(EntityAgent agent, float deltaTime)
    {
        UpdateStats(agent);
        ModifyThirst(-ThirstRate * CalculateMultiplierPerGameSecond() * deltaTime * 0.1f);
    }

    private void UpdateStats(EntityAgent agent)
    {
        var config = ModConfig.Instance.Thirst;
        var stat = agent.Stats[HoDStats.ThirstRate];

        if (agent.Controls.Sprint)
        {
            stat.Set("sprint", config.SprintThirstMultiplier);
        }
        else stat.Remove("sprint");

        if (storySys?.GetStoryStructureAt(entity.Pos.AsBlockPos) is not null)
        {
            stat.Set("story", config.ThirstRateAtStoryLocations);
        }
        else stat.Remove("story");

        if (entity.World.ElapsedMilliseconds - lastMoveMs > 3000L)
        {
            stat.Set("idle", config.IdleThirstModifier);
        }
        else stat.Remove("idle");

        agent.GetBehavior<EntityBehaviorBodyTemperatureHot>()?.UpdateStats();
    }

}
