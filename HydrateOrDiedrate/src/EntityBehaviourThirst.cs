using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate
{
    public class EntityBehaviorThirst : EntityBehavior
    {
        private const float DefaultSpeedOfTime = 60f;
        private const float DefaultCalendarSpeedMul = 0.5f;

        private float _customThirstRate;
        private int _customThirstTicks;
        
        private float thirstDelta;
        private float thirstDamageDelta;
        
        private long lastMoveMs;

        public float CurrentThirst
        {
            get => entity.WatchedAttributes.TryGetFloat("currentThirst") ?? MaxThirst;
            set => entity.WatchedAttributes.SetFloat("currentThirst", GameMath.Clamp(value, 0, MaxThirst));
        }

        public float MaxThirst
        {
            get => entity.WatchedAttributes.TryGetFloat("maxThirst") ?? HydrateOrDiedrateModSystem.LoadedConfig.MaxThirst;
            set => entity.WatchedAttributes.SetFloat("maxThirst", Math.Max(value, 0));
        }

        public float ThirstRate
        {
            get => entity.WatchedAttributes.GetFloat("thirstRate", HydrateOrDiedrateModSystem.LoadedConfig.ThirstDecayRate);
            set => entity.WatchedAttributes.SetFloat("thirstRate", Math.Max(value, 0));
        }

        private float _movementPenalty;
        public float MovementPenalty
        {
            get => _movementPenalty;
            set
            {
                value = GameMath.Clamp(value, 0, HydrateOrDiedrateModSystem.LoadedConfig.MaxMovementSpeedPenalty);
                if(_movementPenalty != value)
                {
                    _movementPenalty = value;
                    UpdateWalkSpeed();
                }
            }
        }

        public int HydrationLossDelay
        {
            get => entity.WatchedAttributes.GetInt("hydrationLossDelay");
            set => entity.WatchedAttributes.SetInt("hydrationLossDelay", value);
        }

        public float HungerReductionAmount
        {
            get => entity.WatchedAttributes.GetFloat("hungerReductionAmount");
            set => entity.WatchedAttributes.SetFloat("hungerReductionAmount", Math.Max((float)Math.Ceiling(value), 0f));
        }

        public EntityBehaviorThirst(Entity entity) : base(entity)
        {
            entity.WatchedAttributes.RegisterModifiedListener("currentThirst", OnCurrentThirstModified);
            
            //Ensuring these values are present in attributes for GUI purposes
            MaxThirst = HydrateOrDiedrateModSystem.LoadedConfig.MaxThirst;
            if(!HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics || entity.WatchedAttributes.TryGetFloat("currentThirst") == null) CurrentThirst = MaxThirst;
        }

        private void OnCurrentThirstModified()
        {
            var currentThirst = CurrentThirst;
            
            if (currentThirst <= 0)
            {
                MovementPenalty = HydrateOrDiedrateModSystem.LoadedConfig.MaxMovementSpeedPenalty;
            }
            else if (currentThirst < HydrateOrDiedrateModSystem.LoadedConfig.MovementSpeedPenaltyThreshold)
            {
                MovementPenalty = HydrateOrDiedrateModSystem.LoadedConfig.MaxMovementSpeedPenalty * (1 - (currentThirst / HydrateOrDiedrateModSystem.LoadedConfig.MovementSpeedPenaltyThreshold));
            }
            else MovementPenalty = 0;
        }

        public void Reset(Config.Config newConfig)
        {
            MaxThirst = HydrateOrDiedrateModSystem.LoadedConfig.MaxThirst;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!HydrateOrDiedrateModSystem.LoadedConfig.EnableThirstMechanics || !entity.Alive || entity is not EntityPlayer playerEntity) return;

            if (playerEntity.Player?.WorldData?.CurrentGameMode is EnumGameMode.Creative or EnumGameMode.Spectator or EnumGameMode.Guest) return;

            thirstDelta += deltaTime;
            if (thirstDelta > 10)
            {
                HandleAccumulatedThirstDecay(thirstDelta);
                thirstDelta = 0;
            }

            if(CurrentThirst <= 0)
            {
                thirstDamageDelta += deltaTime;

                if (thirstDamageDelta > 10)
                {
                    ApplyDamage();

                    thirstDamageDelta -= 10f;
                }
            }
        }

        private void HandleAccumulatedThirstDecay(float deltaTime)
        {
            float thirstDecayRate = _customThirstTicks > 0 ? _customThirstRate : HydrateOrDiedrateModSystem.LoadedConfig.ThirstDecayRate;
            int hydrationLossDelay = HydrationLossDelay;

            float currentSpeedOfTime = entity.Api.World.Calendar.SpeedOfTime;
            float currentCalendarSpeedMul = entity.Api.World.Calendar.CalendarSpeedMul;
            float multiplierPerGameSec = (currentSpeedOfTime / DefaultSpeedOfTime) *
                                         (currentCalendarSpeedMul / DefaultCalendarSpeedMul);

            if (hydrationLossDelay > 0)
            {
                HydrationLossDelay = hydrationLossDelay - Math.Max(1, (int)Math.Floor(multiplierPerGameSec * deltaTime));
                return;
            }

            if (entity is EntityPlayer player)
            {
                if (player.Controls?.Sprint == true)
                {
                    thirstDecayRate *= HydrateOrDiedrateModSystem.LoadedConfig.SprintThirstMultiplier;
                }

                if (player.Controls.TriesToMove || player.Controls.Jump || player.Controls.LeftMouseDown || player.Controls.RightMouseDown)
                {
                    lastMoveMs = entity.World.ElapsedMilliseconds;
                }
            }

            if (HydrateOrDiedrateModSystem.LoadedConfig.HarshHeat)
            {
                var climate = entity.World.BlockAccessor.GetClimateAt(entity.ServerPos.AsBlockPos, EnumGetClimateMode.NowValues);

                if (climate.Temperature > HydrateOrDiedrateModSystem.LoadedConfig.TemperatureThreshold)
                {
                    float temperatureDifference = climate.Temperature - HydrateOrDiedrateModSystem.LoadedConfig.TemperatureThreshold;
                    float temperatureFactor = HydrateOrDiedrateModSystem.LoadedConfig.ThirstIncreasePerDegreeMultiplier *
                                              (float)Math.Exp(HydrateOrDiedrateModSystem.LoadedConfig.HarshHeatExponentialGainMultiplier *
                                                              temperatureDifference);

                    thirstDecayRate += temperatureFactor;

                    float coolingFactor = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f);
                    float coolingEffect = coolingFactor * (1f / (1f + (float)Math.Exp(-0.5f * temperatureDifference)));
                    thirstDecayRate -= Math.Min(coolingEffect, thirstDecayRate - HydrateOrDiedrateModSystem.LoadedConfig.ThirstDecayRate);
                }
            }
            thirstDecayRate = Math.Min(thirstDecayRate, HydrateOrDiedrateModSystem.LoadedConfig.ThirstDecayRate * HydrateOrDiedrateModSystem.LoadedConfig.ThirstDecayRateMax);

            if (currentSpeedOfTime > DefaultSpeedOfTime || currentCalendarSpeedMul > DefaultCalendarSpeedMul)
            {
                thirstDecayRate *= multiplierPerGameSec;
            }

            bool isIdle = entity.World.ElapsedMilliseconds - lastMoveMs > 3000L;
            if (isIdle)
            {
                thirstDecayRate /= 4f;
            }

            if (_customThirstTicks > 0) _customThirstTicks--;
            ModifyThirst(-thirstDecayRate * deltaTime * 0.1f);
            
            ThirstRate = thirstDecayRate;
        }

        public void OnRespawn()
        {
            CurrentThirst = 0.5f * MaxThirst;
            HungerReductionAmount = 0;
        }

        private void ApplyDamage()
        {
            entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Injury
            }, HydrateOrDiedrateModSystem.LoadedConfig.ThirstDamage);
        }

        private void UpdateWalkSpeed() => entity.Stats.Set("walkspeed", "thirstPenalty", -MovementPenalty, true);

        public void ApplyCustomThirstRate(float rate, int ticks)
        {
            //TODO is this maybe used by other mods?
            _customThirstRate = rate;
            _customThirstTicks = ticks;
        }

        public void ModifyThirst(float amount, float hydLossDelay = 0)
        {
            CurrentThirst += amount;

            int hydrationLossDelay = HydrationLossDelay;

            if (hydLossDelay > hydrationLossDelay)
            {
                HydrationLossDelay = (int)Math.Floor(hydLossDelay);
            }
        }

        public override string PropertyName() => "thirst";
    }
}