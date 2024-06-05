using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.EntityBehavior
{
    public class EntityBehaviorThirst : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private float currentThirst;
        private float maxThirst = 10f;
        private float thirstDepletionRate = 0.01f;

        public float CurrentThirst
        {
            get => currentThirst;
            set
            {
                currentThirst = GameMath.Clamp(value, 0, maxThirst);
                entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
                entity.WatchedAttributes.MarkPathDirty("currentThirst");
            }
        }

        public float MaxThirst
        {
            get => maxThirst;
            set
            {
                maxThirst = value;
                entity.WatchedAttributes.SetFloat("maxThirst", maxThirst);
                entity.WatchedAttributes.MarkPathDirty("maxThirst");
            }
        }

        public EntityBehaviorThirst(Entity entity) : base(entity)
        {
            InitializeThirst();
        }

        private void InitializeThirst()
        {
            currentThirst = entity.WatchedAttributes.GetFloat("currentThirst", maxThirst);
            maxThirst = entity.WatchedAttributes.GetFloat("maxThirst", maxThirst);
            entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", maxThirst);

            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive) return;

            currentThirst -= thirstDepletionRate * deltaTime;
            currentThirst = GameMath.Clamp(currentThirst, 0, maxThirst);

            if (currentThirst <= 0)
            {
                entity.ReceiveDamage(new DamageSource()
                {
                    Source = EnumDamageSource.Internal,
                    Type = EnumDamageType.Hunger
                }, 1f);
            }

            entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
            entity.WatchedAttributes.SetFloat("maxThirst", maxThirst);

            entity.WatchedAttributes.MarkPathDirty("currentThirst");
            entity.WatchedAttributes.MarkPathDirty("maxThirst");
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            InitializeThirst();
        }

        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
            InitializeThirst();
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            InitializeThirst();
        }

        public override string PropertyName()
        {
            return "thirst";
        }

        public static void SetInitialThirst(IServerPlayer byPlayer, float maxThirst)
        {
            byPlayer.Entity.WatchedAttributes.SetFloat("currentThirst", maxThirst);
            byPlayer.Entity.WatchedAttributes.SetFloat("maxThirst", maxThirst);
        }

        public static void ResetThirstOnRespawn(IServerPlayer byPlayer, float maxThirst)
        {
            byPlayer.Entity.WatchedAttributes.SetFloat("currentThirst", 0.5f);
            byPlayer.Entity.WatchedAttributes.SetFloat("maxThirst", maxThirst);
        }

        public static void UpdateThirstOnServerTick(IServerPlayer player, float deltaTime, float maxThirst)
        {
            float currentThirst = player.Entity.WatchedAttributes.GetFloat("currentThirst", maxThirst);

            currentThirst -= 0.05f * deltaTime; // Adjust thirst drain rate
            currentThirst = GameMath.Clamp(currentThirst, 0f, maxThirst);

            if (currentThirst <= 0)
            {
                player.Entity.Stats.Set("walkspeed", "global", 0.5f, true); // Slow player

                player.Entity.ReceiveDamage(new DamageSource()
                {
                    Source = EnumDamageSource.Internal,
                    Type = EnumDamageType.Hunger // Use hunger damage type
                }, player.Entity.Stats.GetBlended("health") * 0.05f * deltaTime);
            }
            else
            {
                player.Entity.Stats.Set("walkspeed", "global", 1f, true); // Reset speed
            }

            player.Entity.WatchedAttributes.SetFloat("currentThirst", currentThirst);
            player.Entity.WatchedAttributes.SetFloat("maxThirst", maxThirst);
            player.Entity.WatchedAttributes.MarkPathDirty("currentThirst");
            player.Entity.WatchedAttributes.MarkPathDirty("maxThirst");
            player.Entity.Stats.Set("thirst", "", currentThirst, true); // Update thirst
        }
    }
}
