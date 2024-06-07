using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Configuration
{
    public class Config : IModConfig
    {
        public float MaxThirst { get; set; } = 10.0f;
        public float ThirstDamage { get; set; } = 1.0f;
        public float MovementSpeedPenalty { get; set; } = 0.75f;
        public float ThirstDecayRate { get; set; } = 0.01f;
        public float SprintThirstMultiplier { get; set; } = 1.25f;
        public float SprintMovementSpeedPenaltyThreshold { get; set; } = 4.0f;

        public Config() { }

        public Config(ICoreAPI api, Config previousConfig = null)
        {
            MaxThirst = previousConfig?.MaxThirst ?? 10.0f;
            ThirstDamage = previousConfig?.ThirstDamage ?? 1.0f;
            MovementSpeedPenalty = previousConfig?.MovementSpeedPenalty ?? 0.75f;
            ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 0.01f;
            SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.25f;
            SprintMovementSpeedPenaltyThreshold = previousConfig?.SprintMovementSpeedPenaltyThreshold ?? 4.0f;
        }
    }
}