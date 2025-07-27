using HydrateOrDiedrate.Config;
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
        
        private float thirstDelta;
        private float thirstDamageDelta;
        private float hydrationTickDelta;
        private long lastMoveMs;

        public float CurrentThirst
        {
            get => entity.WatchedAttributes.TryGetFloat("currentThirst") ?? MaxThirst;
            set
            {
                float safeValue = float.IsNaN(value) ? MaxThirst : value;
                safeValue = GameMath.Clamp(safeValue, 0, MaxThirst);
                entity.WatchedAttributes.SetFloat("currentThirst", safeValue);
            }
        }

        public float MaxThirst
        {
            get => entity.WatchedAttributes.TryGetFloat("maxThirst") ?? 
                   (float.IsNaN(ModConfig.Instance.Thirst.MaxThirst) ? 0f : ModConfig.Instance.Thirst.MaxThirst); //TODO: Config should not be validated here! this should be done when config is loaded or modified
            set => entity.WatchedAttributes.SetFloat("maxThirst", float.IsNaN(value) ? 0f : Math.Max(value, 0));
        }

        public float ThirstRate
        {
            get => entity.WatchedAttributes.GetFloat("thirstRate", 
                   float.IsNaN(ModConfig.Instance.Thirst.ThirstDecayRate) ? 0f : ModConfig.Instance.Thirst.ThirstDecayRate);
            set => entity.WatchedAttributes.SetFloat("thirstRate", float.IsNaN(value) ? 0f : Math.Max(value, 0));
        }

        private float _movementPenalty;
        public float MovementPenalty
        {
            get => _movementPenalty;
            set
            {
                float safeValue = float.IsNaN(value) ? 0f : value;
                safeValue = GameMath.Clamp(safeValue, 0, ModConfig.Instance.Thirst.MaxMovementSpeedPenalty);
                if (_movementPenalty != safeValue)
                {
                    _movementPenalty = safeValue;
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
            set => entity.WatchedAttributes.SetFloat("hungerReductionAmount", float.IsNaN(value) ? 0f : Math.Max((float)Math.Ceiling(value), 0f));
        }

        public EntityBehaviorThirst(Entity entity) : base(entity)
        {
            entity.WatchedAttributes.RegisterModifiedListener("currentThirst", OnCurrentThirstModified);
            if (!entity.WatchedAttributes.HasAttribute("maxThirst"))
            {
                MaxThirst = ModConfig.Instance.Thirst.MaxThirst;
            }
            if (!ModConfig.Instance.Thirst.Enabled || !entity.WatchedAttributes.HasAttribute("currentThirst"))
            {
                CurrentThirst = MaxThirst;
            }
        }

        private void OnCurrentThirstModified()
        {
            float currentThirst = CurrentThirst;
            if (float.IsNaN(currentThirst))
            {
                currentThirst = MaxThirst;
            }
            
            if (currentThirst <= 0)
            {
                MovementPenalty = ModConfig.Instance.Thirst.MaxMovementSpeedPenalty;
            }
            else if (currentThirst < ModConfig.Instance.Thirst.MovementSpeedPenaltyThreshold)
            {
                float threshold = ModConfig.Instance.Thirst.MovementSpeedPenaltyThreshold;
                if (threshold <= 0)
                {
                    MovementPenalty = ModConfig.Instance.Thirst.MaxMovementSpeedPenalty; //TODO: This doesn't make sense, shouldn't this be 0 instead?
                }
                else
                {
                    MovementPenalty = ModConfig.Instance.Thirst.MaxMovementSpeedPenalty * (1 - (currentThirst / threshold));
                }
            }
            else
            {
                MovementPenalty = 0;
            }
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!ModConfig.Instance.Thirst.Enabled || !entity.Alive || entity is not EntityPlayer playerEntity) return;

            if (playerEntity.Player?.WorldData?.CurrentGameMode is EnumGameMode.Creative or EnumGameMode.Spectator or EnumGameMode.Guest) return;
            if (float.IsNaN(deltaTime) || deltaTime < 0) 
            {
                deltaTime = 0f;
            }
            
            hydrationTickDelta += deltaTime;
            if (hydrationTickDelta >= 1f)
            {
                UpdateHydrationLossDelay(hydrationTickDelta);
                hydrationTickDelta = 0f;
            }

            thirstDelta += deltaTime;
            if (thirstDelta > 10)
            {
                HandleAccumulatedThirstDecay(thirstDelta);
                thirstDelta = 0;
            }

            if (CurrentThirst <= 0)
            {
                thirstDamageDelta += deltaTime;
                if (thirstDamageDelta > 10)
                {
                    ApplyThirstDamage(ModConfig.Instance.Thirst.ThirstDamage);
                    thirstDamageDelta -= 10f;
                }
            }
        }

        private void UpdateHydrationLossDelay(float elapsedSeconds)
        {
            int currentDelay = HydrationLossDelay;
            float currentSpeedOfTime = entity.Api?.World?.Calendar?.SpeedOfTime ?? DefaultSpeedOfTime;
            if (float.IsNaN(currentSpeedOfTime))
            {
                currentSpeedOfTime = DefaultSpeedOfTime;
            }
            
            float currentCalendarSpeedMul = entity.Api?.World?.Calendar?.CalendarSpeedMul ?? DefaultCalendarSpeedMul;
            if (float.IsNaN(currentCalendarSpeedMul))
            {
                currentCalendarSpeedMul = DefaultCalendarSpeedMul;
            }
            
            float multiplierPerGameSec = (currentSpeedOfTime / DefaultSpeedOfTime) *
                                         (currentCalendarSpeedMul / DefaultCalendarSpeedMul);
            if (float.IsNaN(multiplierPerGameSec) || multiplierPerGameSec <= 0)
            {
                multiplierPerGameSec = 1f;
            }
            int decrement = Math.Max(1, (int)Math.Floor(multiplierPerGameSec * elapsedSeconds));
            HydrationLossDelay = Math.Max(0, currentDelay - decrement);
        }

        private void HandleAccumulatedThirstDecay(float deltaTime)
        {
            var config = ModConfig.Instance.Thirst;
            float thirstDecayRate = config.ThirstDecayRate;
            if (float.IsNaN(thirstDecayRate))
            {
                thirstDecayRate = 0f;
            }
            int hydrationLossDelay = HydrationLossDelay;
            float currentSpeedOfTime = entity.Api?.World?.Calendar?.SpeedOfTime ?? DefaultSpeedOfTime;
            if (float.IsNaN(currentSpeedOfTime))
            {
                currentSpeedOfTime = DefaultSpeedOfTime;
            }
            float currentCalendarSpeedMul = entity.Api?.World?.Calendar?.CalendarSpeedMul ?? DefaultCalendarSpeedMul;
            if (float.IsNaN(currentCalendarSpeedMul))
            {
                currentCalendarSpeedMul = DefaultCalendarSpeedMul;
            }
            float multiplierPerGameSec = (currentSpeedOfTime / DefaultSpeedOfTime) *
                                         (currentCalendarSpeedMul / DefaultCalendarSpeedMul);
            if (float.IsNaN(multiplierPerGameSec) || multiplierPerGameSec <= 0)
            {
                multiplierPerGameSec = 1f;
            }

            if (hydrationLossDelay > 0)
            {
                return;
            }

            if (entity is EntityPlayer player)
            {
                var controls = player.Controls;
                if (controls != null)
                {
                    if (controls.Sprint)
                    {
                        float sprintMultiplier = config.SprintThirstMultiplier;
                        if (float.IsNaN(sprintMultiplier))
                        {
                            sprintMultiplier = 1f;
                        }
                        thirstDecayRate *= sprintMultiplier;
                    }
                    if (controls.TriesToMove || controls.Jump || controls.LeftMouseDown || controls.RightMouseDown)
                    {
                        lastMoveMs = entity.World.ElapsedMilliseconds;
                    }
                }
            }

            if (ModConfig.Instance.HeatAndCooling.HarshHeat)
            {
                var climate = entity.World.BlockAccessor.GetClimateAt(entity.ServerPos.AsBlockPos, EnumGetClimateMode.NowValues);
                if (climate != null && !float.IsNaN(climate.Temperature))
                {
                    if (climate.Temperature > ModConfig.Instance.HeatAndCooling.TemperatureThreshold)
                    {
                        float temperatureDifference = climate.Temperature - ModConfig.Instance.HeatAndCooling.TemperatureThreshold;
                        if (float.IsNaN(temperatureDifference))
                        {
                            temperatureDifference = 0f;
                        }

                        float expArgument = ModConfig.Instance.HeatAndCooling.HarshHeatExponentialGainMultiplier * temperatureDifference;
                        if (float.IsNaN(expArgument))
                        {
                            expArgument = 0f;
                        }
                        double expValue = Math.Exp(expArgument);
                        if (double.IsInfinity(expValue))
                        {
                            expValue = double.MaxValue;
                        }
                        float temperatureFactor = ModConfig.Instance.HeatAndCooling.ThirstIncreasePerDegreeMultiplier * (float)expValue;
                        if (float.IsNaN(temperatureFactor) || float.IsInfinity(temperatureFactor))
                        {
                            temperatureFactor = 0f;
                        }

                        thirstDecayRate += temperatureFactor;

                        float coolingFactor = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f);
                        if (float.IsNaN(coolingFactor))
                        {
                            coolingFactor = 0f;
                        }

                        double expCoolingDouble = Math.Exp(-0.5f * temperatureDifference);
                        if (double.IsInfinity(expCoolingDouble))
                        {
                            expCoolingDouble = double.MaxValue;
                        }
                        float expCooling = (float)expCoolingDouble;
                        if (float.IsNaN(expCooling) || float.IsInfinity(expCooling))
                        {
                            expCooling = 1f;
                        }

                        float coolingEffect = coolingFactor * (1f / (1f + expCooling));
                        if (float.IsNaN(coolingEffect))
                        {
                            coolingEffect = 0f;
                        }

                        thirstDecayRate -= Math.Min(coolingEffect, thirstDecayRate - config.ThirstDecayRate);
                    }
                }
            }

            float maxThirstDecay = config.ThirstDecayRate * config.ThirstDecayRateMax;
            if (float.IsNaN(maxThirstDecay))
            {
                maxThirstDecay = config.ThirstDecayRate;
            }
            thirstDecayRate = Math.Min(thirstDecayRate, maxThirstDecay);
            if (float.IsNaN(thirstDecayRate))
            {
                thirstDecayRate = config.ThirstDecayRate;
            }

            if (currentSpeedOfTime > DefaultSpeedOfTime || currentCalendarSpeedMul > DefaultCalendarSpeedMul)
            {
                thirstDecayRate *= multiplierPerGameSec;
                if (float.IsNaN(thirstDecayRate))
                {
                    thirstDecayRate = config.ThirstDecayRate;
                }
            }

            bool isIdle = entity.World.ElapsedMilliseconds - lastMoveMs > 3000L;
            if (isIdle)
            {
                thirstDecayRate /= 4f;
                if (float.IsNaN(thirstDecayRate))
                {
                    thirstDecayRate = config.ThirstDecayRate;
                }
            }
            float deltaThirst = -thirstDecayRate * deltaTime * 0.1f;
            if (float.IsNaN(deltaThirst))
            {
                deltaThirst = 0f;
            }
            ModifyThirst(deltaThirst);
            ThirstRate = float.IsNaN(thirstDecayRate) ? 0f : thirstDecayRate;
        }

        public void OnRespawn()
        {
            CurrentThirst = 0.5f * MaxThirst; //TODO: should be configureable
            HungerReductionAmount = 0;
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

        private void UpdateWalkSpeed() => entity.Stats.Set("walkspeed", "thirstPenalty", -MovementPenalty, true);

        public void ModifyThirst(float amount, float hydLossDelay = 0)
        {
            float newThirst = CurrentThirst + amount;
            if (float.IsNaN(newThirst))
            {
                newThirst = CurrentThirst;
            }
            CurrentThirst = newThirst;

            int hydrationLossDelay = HydrationLossDelay;
            if (hydLossDelay > hydrationLossDelay)
            {
                HydrationLossDelay = (int)Math.Floor(hydLossDelay);
            }
        }

        public override string PropertyName() => "thirst";
    }
}
