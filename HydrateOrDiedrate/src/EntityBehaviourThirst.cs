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
        private Config _config;
        private float _currentPenaltyAmount = 0f;

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

        public EntityBehaviorThirst(Entity entity, Config config) : base(entity)
        {
            this._config = config;
            LoadThirst();
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive) return;

            var player = entity as EntityPlayer;
            if (player?.Player?.WorldData?.CurrentGameMode == EnumGameMode.Creative || player?.Player?.WorldData?.CurrentGameMode == EnumGameMode.Spectator)
            {
                return; // Don't update thirst if the player is in Creative or Spectator mode
            }

            HandleThirstDecay(deltaTime);
            ApplyThirstEffects();
            UpdateThirstAttributes();
        }

        private void HandleThirstDecay(float deltaTime)
        {
            float thirstDecayRate = _config.ThirstDecayRate;

            var player = entity as EntityPlayer;
            if (player != null && player.Controls != null && player.Controls.Sprint)
            {
                thirstDecayRate *= _config.SprintThirstMultiplier;
            }

            if (_customThirstTicks > 0)
            {
                thirstDecayRate = _customThirstRate;
                _customThirstTicks--;
            }

            CurrentThirst -= thirstDecayRate * deltaTime;
        }

        private void ApplyThirstEffects()
        {
            if (CurrentThirst <= 0)
            {
                ApplyDamage();
                ApplyMovementSpeedPenalty(_config.MaxMovementSpeedPenalty);
            }
            else if (CurrentThirst < _config.MovementSpeedPenaltyThreshold)
            {
                float penaltyAmount = _config.MaxMovementSpeedPenalty * (_config.MovementSpeedPenaltyThreshold - CurrentThirst) / _config.MovementSpeedPenaltyThreshold;
                ApplyMovementSpeedPenalty(penaltyAmount);
            }
            else
            {
                RemoveMovementSpeedPenalty();
            }
        }

        public void SetInitialThirst()
        {
            CurrentThirst = _config.MaxThirst;

            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", _config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");

            RemoveMovementSpeedPenalty(); // Ensure speed is reset on initialization
        }

        public void ResetThirstOnRespawn(Entity entity)
        {
            CurrentThirst = 0.5f * _config.MaxThirst;

            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", _config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");

            RemoveMovementSpeedPenalty(); // Ensure speed is reset on respawn
        }

        private void ApplyDamage()
        {
            entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Hunger
            }, _config.ThirstDamage);
        }

        private void ApplyMovementSpeedPenalty(float penaltyAmount)
        {
            if (_currentPenaltyAmount != penaltyAmount)
            {
                _currentPenaltyAmount = penaltyAmount;
                UpdateWalkSpeed();
            }
        }

        private void RemoveMovementSpeedPenalty()
        {
            if (_currentPenaltyAmount != 0f)
            {
                _currentPenaltyAmount = 0f;
                UpdateWalkSpeed();
            }
        }

        private void UpdateWalkSpeed()
        {
            entity.Stats.Set("walkspeed", "thirstPenalty", -_currentPenaltyAmount, true); // Apply the penalty as a blended stat part
        }

        public void UpdateThirstAttributes()
        {
            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
        }

        public void ApplyCustomThirstRate(float rate, int ticks)
        {
            _customThirstRate = rate;
            _customThirstTicks = ticks;
        }

        public void LoadThirst()
        {
            _currentThirst = entity.WatchedAttributes.GetFloat("currentThirst", _config.MaxThirst);
        }

        public static void UpdateThirstOnServerTick(IServerPlayer player, float deltaTime, Config config)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.OnGameTick(deltaTime);
            }
        }

        public override string PropertyName()
        {
            return "thirst";
        }
    }
}
