using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using HydrateOrDiedrate.Configuration;

namespace HydrateOrDiedrate.EntityBehavior
{
    public class EntityBehaviorThirst : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private float _currentThirst;
        private float _customThirstRate;
        private int _customThirstTicks;
        private readonly Config _config;
        private float _currentPenaltyAmount;
        private int _thirstTickCounter;
        private bool _isPenaltyApplied;

        public float CurrentThirst
        {
            get => _currentThirst;
            set
            {
                _currentThirst = GameMath.Clamp(value, 0, _config.MaxThirst);
                entity.WatchedAttributes.SetFloat("currentThirst", _currentThirst);
                entity.WatchedAttributes.MarkPathDirty("currentThirst");
            }
        }

        public float MaxThirst => _config.MaxThirst;

        public EntityBehaviorThirst(Entity entity) : base(entity)
        {
            _config = new Config();
            LoadThirst();
            _thirstTickCounter = 0;
            _isPenaltyApplied = false;
        }

        public EntityBehaviorThirst(Entity entity, Config config) : base(entity)
        {
            _config = config;
            LoadThirst();
            _thirstTickCounter = 0;
            _isPenaltyApplied = false;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive) return;

            var player = entity as EntityPlayer;
            if (player?.Player?.WorldData?.CurrentGameMode is EnumGameMode.Creative or EnumGameMode.Spectator)
            {
                return;
            }

            HandleThirstDecay(deltaTime);
            ApplyThirstEffects();
        }

        private void HandleThirstDecay(float deltaTime)
        {
            float thirstDecayRate = _customThirstTicks > 0 ? _customThirstRate : _config.ThirstDecayRate;

            if (entity is EntityPlayer player && player.Controls?.Sprint == true)
            {
                thirstDecayRate *= _config.SprintThirstMultiplier;
            }

            if (_config.HarshHeat)
            {
                var climate = entity.World.BlockAccessor.GetClimateAt(entity.ServerPos.AsBlockPos, EnumGetClimateMode.NowValues);
                if (climate.Temperature > _config.TemperatureThreshold)
                {
                    thirstDecayRate += (climate.Temperature - _config.TemperatureThreshold) * _config.ThirstIncreasePerDegree;

                    float coolingFactor = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f) * _config.CoolingMultiplier;
                    thirstDecayRate -= Math.Min(coolingFactor, thirstDecayRate - _config.ThirstDecayRate);
                }
            }

            if (_customThirstTicks > 0) _customThirstTicks--;

            CurrentThirst -= thirstDecayRate * deltaTime;
            UpdateThirstRate(thirstDecayRate);
        }

        private void ApplyThirstEffects()
        {
            _thirstTickCounter++;

            if (_thirstTickCounter >= 250)
            {
                if (CurrentThirst <= 0)
                {
                    ApplyDamage();
                    ApplyMovementSpeedPenalty(_config.MaxMovementSpeedPenalty);
                }
                else if (CurrentThirst < _config.MovementSpeedPenaltyThreshold)
                {
                    float penaltyAmount = _config.MaxMovementSpeedPenalty * (1 - (CurrentThirst / _config.MovementSpeedPenaltyThreshold));
                    ApplyMovementSpeedPenalty(penaltyAmount);
                }
                else
                {
                    RemoveMovementSpeedPenalty();
                }

                _thirstTickCounter = 0;
            }
        }

        public void SetInitialThirst()
        {
            CurrentThirst = _config.MaxThirst;
            UpdateThirstAttributes();
            RemoveMovementSpeedPenalty();
        }

        public void ResetThirstOnRespawn()
        {
            CurrentThirst = 0.5f * _config.MaxThirst;
            UpdateThirstAttributes();
            RemoveMovementSpeedPenalty();
        }

        private void ApplyDamage()
        {
            entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Injury
            }, _config.ThirstDamage);
        }

        private void ApplyMovementSpeedPenalty(float penaltyAmount)
        {
            if (_currentPenaltyAmount == penaltyAmount && _isPenaltyApplied) return;

            _currentPenaltyAmount = penaltyAmount;
            UpdateWalkSpeed();
            _isPenaltyApplied = true;
        }

        private void RemoveMovementSpeedPenalty()
        {
            if (_currentPenaltyAmount == 0f && !_isPenaltyApplied) return;

            _currentPenaltyAmount = 0f;
            UpdateWalkSpeed();
            _isPenaltyApplied = false;
        }

        private void UpdateWalkSpeed()
        {
            entity.Stats.Set("walkspeed", "thirstPenalty", -_currentPenaltyAmount, true);
        }

        public void ApplyCustomThirstRate(float rate, int ticks)
        {
            _customThirstRate = rate;
            _customThirstTicks = ticks;
        }

        public void LoadThirst()
        {
            _currentThirst = entity.WatchedAttributes.GetFloat("currentThirst", _config.MaxThirst);
            UpdateThirstAttributes();
        }

        public void UpdateThirstAttributes()
        {
            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", _config.MaxThirst);
            entity.WatchedAttributes.SetFloat("normalThirstRate", _config.ThirstDecayRate);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
            entity.WatchedAttributes.MarkPathDirty("normalThirstRate");
        }

        public static void UpdateThirstOnServerTick(IServerPlayer player, float deltaTime, Config config)
        {
            player.Entity.GetBehavior<EntityBehaviorThirst>()?.OnGameTick(deltaTime);
        }

        public void UpdateThirstRate(float rate)
        {
            entity.WatchedAttributes.SetFloat("thirstRate", rate);
            entity.WatchedAttributes.MarkPathDirty("thirstRate");
        }

        public override string PropertyName() => "thirst";
    }
}
