using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using HydrateOrDiedrate;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server; // Add this for IServerPlayer

namespace HydrateOrDiedrate.EntityBehavior
{
    public class EntityBehaviorThirst : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private float currentThirst;
        private ThirstConfig _config;
        private float customThirstRate;
        private int customThirstTicks;

        private bool penaltyApplied = false;
        private float defaultWalkSpeed = -1f;

        public float CurrentThirst
        {
            get => currentThirst;
            set
            {
                currentThirst = GameMath.Clamp(value, 0, _config.MaxThirst);
                entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
                entity.WatchedAttributes.MarkPathDirty("currentThirst");
            }
        }

        public EntityBehaviorThirst(Entity entity) : base(entity)
        {
            _config = entity.Api.ModLoader.GetModSystem<HydrateOrDiedrateModSystem>().GetConfig();
            LoadThirst(); // Load thirst values on initialization
        }

        public void LoadThirst()
        {
            currentThirst = entity.WatchedAttributes.GetFloat("currentThirst", _config.MaxThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", _config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive) return;

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

            if (customThirstTicks > 0)
            {
                thirstDecayRate = customThirstRate;
                customThirstTicks--;
            }

            CurrentThirst -= thirstDecayRate * deltaTime;
        }

        private void ApplyThirstEffects()
        {
            if (CurrentThirst <= 0)
            {
                ApplyDamage();
                ApplyMovementSpeedPenalty();
            }
            else
            {
                RemoveMovementSpeedPenalty();
            }
        }

        public void SetInitialThirst(ThirstConfig config)
        {
            _config = config;
            CurrentThirst = _config.MaxThirst;

            // Ensure attributes exist and are updated:
            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", _config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        public void ResetThirstOnRespawn(ThirstConfig config)
        {
            _config = config;
            CurrentThirst = 0.5f * _config.MaxThirst;

            // Ensure attributes exist and are updated:
            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", _config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        private void ApplyDamage()
        {
            entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Hunger
            }, _config.ThirstDamage);
        }

        private void ApplyMovementSpeedPenalty()
        {
            if (!penaltyApplied)
            {
                if (defaultWalkSpeed < 0)
                {
                    defaultWalkSpeed = entity.Stats.GetBlended("walkspeed");
                }

                float currentSpeed = entity.Stats.GetBlended("walkspeed");
                entity.Stats.Set("walkspeed", "global", currentSpeed * _config.MovementSpeedPenalty, true);
                penaltyApplied = true;
            }
        }

        private void RemoveMovementSpeedPenalty()
        {
            if (penaltyApplied)
            {
                entity.Stats.Set("walkspeed", "global", defaultWalkSpeed, true);
                penaltyApplied = false;
            }
        }

        private void UpdateThirstAttributes()
        {
            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
        }

        public void ApplyCustomThirstRate(float rate, int ticks)
        {
            customThirstRate = rate;
            customThirstTicks = ticks;
        }

        // We'll use the server tick event for updates
        public static void UpdateThirstOnServerTick(IServerPlayer player, float deltaTime, ThirstConfig config)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                // Delegate to the instance method:
                thirstBehavior.HandleThirstDecay(deltaTime);
                thirstBehavior.ApplyThirstEffects();
                thirstBehavior.UpdateThirstAttributes();
            }
        }

        public override string PropertyName()
        {
            return "thirst";
        }
    }
}
