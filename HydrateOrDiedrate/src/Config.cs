using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Configuration
{
    public class Config : IModConfig
    {
        public float MaxThirst { get; set; } = 10.0f;
        public float ThirstDamage { get; set; } = 1.0f;
        public float MaxMovementSpeedPenalty { get; set; } = 0.03f; // Maximum penalty to subtract from movement speed
        public float ThirstDecayRate { get; set; } = 0.01f;
        public float SprintThirstMultiplier { get; set; } = 1.25f;
        public float MovementSpeedPenaltyThreshold { get; set; } = 4.0f;

        // New config options
        public float RegularWaterThirstDecrease { get; set; } = 10.0f;
        public float SaltWaterThirstIncrease { get; set; } = 5.0f;
        public float BoilingWaterDamage { get; set; } = 5.0f;
        public bool EnableSaltWaterThirstIncrease { get; set; } = true;
        public bool EnableBoilingWaterDamage { get; set; } = true;

        public Config() { }

        public Config(ICoreAPI api, Config previousConfig = null)
        {
            MaxThirst = previousConfig?.MaxThirst ?? 10.0f;
            ThirstDamage = previousConfig?.ThirstDamage ?? 1.0f;
            MaxMovementSpeedPenalty = previousConfig?.MaxMovementSpeedPenalty ?? 0.03f;
            ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 0.01f;
            SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.25f;
            MovementSpeedPenaltyThreshold = previousConfig?.MovementSpeedPenaltyThreshold ?? 4.0f;
            RegularWaterThirstDecrease = previousConfig?.RegularWaterThirstDecrease ?? 10.0f;
            SaltWaterThirstIncrease = previousConfig?.SaltWaterThirstIncrease ?? 5.0f;
            BoilingWaterDamage = previousConfig?.BoilingWaterDamage ?? 5.0f;
            EnableSaltWaterThirstIncrease = previousConfig?.EnableSaltWaterThirstIncrease ?? true;
            EnableBoilingWaterDamage = previousConfig?.EnableBoilingWaterDamage ?? true;
        }
    }
}