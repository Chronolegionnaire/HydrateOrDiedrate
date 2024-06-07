using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using HydrateOrDiedrate.Configuration;

namespace HydrateOrDiedrate.EntityBehavior
{
    public class EntityBehaviorThirst : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private float currentThirst;
        private float customThirstRate;
        private int customThirstTicks;
        private Config config;

        private bool penaltyApplied = false;
        private float defaultWalkSpeed = -1f;

        public float CurrentThirst
        {
            get => currentThirst;
            set
            {
                currentThirst = GameMath.Clamp(value, 0, config.MaxThirst);
                entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
                entity.WatchedAttributes.MarkPathDirty("currentThirst");
            }
        }

        public EntityBehaviorThirst(Entity entity, Config config) : base(entity)
        {
            this.config = config;
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
            float thirstDecayRate = config.ThirstDecayRate;

            var player = entity as EntityPlayer;
            if (player != null && player.Controls != null && player.Controls.Sprint)
            {
                thirstDecayRate *= config.SprintThirstMultiplier;
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
                ApplyMovementSpeedPenalty(config.MovementSpeedPenalty);
            }
            else if (CurrentThirst < config.SprintMovementSpeedPenaltyThreshold)
            {
                float penaltyMultiplier = 1f - (config.MovementSpeedPenalty * (config.SprintMovementSpeedPenaltyThreshold - CurrentThirst) / config.SprintMovementSpeedPenaltyThreshold);
                ApplyMovementSpeedPenalty(penaltyMultiplier);
            }
            else
            {
                RemoveMovementSpeedPenalty();
            }
        }

        public void SetInitialThirst()
        {
            CurrentThirst = config.MaxThirst;

            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        public void ResetThirstOnRespawn(Entity entity)
        {
            CurrentThirst = 0.5f * config.MaxThirst;

            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", config.MaxThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        private void ApplyDamage()
        {
            entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Hunger
            }, config.ThirstDamage);
        }

        private void ApplyMovementSpeedPenalty(float penaltyMultiplier)
        {
            if (defaultWalkSpeed < 0)
            {
                defaultWalkSpeed = entity.Stats.GetBlended("walkspeed");
            }

            entity.Stats.Set("walkspeed", "global", defaultWalkSpeed * penaltyMultiplier, true);
            penaltyApplied = true;
        }

        private void RemoveMovementSpeedPenalty()
        {
            if (penaltyApplied)
            {
                entity.Stats.Set("walkspeed", "global", defaultWalkSpeed, true);
                penaltyApplied = false;
            }
        }

        public void UpdateThirstAttributes()
        {
            entity.WatchedAttributes.SetFloat("currentThirst", CurrentThirst);
            entity.WatchedAttributes.MarkPathDirty("currentThirst");
        }

        public void ApplyCustomThirstRate(float rate, int ticks)
        {
            customThirstRate = rate;
            customThirstTicks = ticks;
        }

        public static void UpdateThirstOnServerTick(IServerPlayer player, float deltaTime, Config config)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
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
