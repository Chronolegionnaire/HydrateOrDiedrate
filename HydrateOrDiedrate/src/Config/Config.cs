﻿using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Configuration
{
    public class Config : IModConfig
    {
        public float MaxThirst { get; set; } = 1500.0f;
        public float ThirstDamage { get; set; } = 1f;
        public float MaxMovementSpeedPenalty { get; set; } = 0.3f; 
        public float ThirstDecayRate { get; set; } = 10f;
        public float SprintThirstMultiplier { get; set; } = 1.5f;
        public float MovementSpeedPenaltyThreshold { get; set; } = 600.0f;
        public float RegularWaterThirstDecrease { get; set; } = 100.0f;
        public float SaltWaterThirstIncrease { get; set; } = 200f;
        public float BoilingWaterDamage { get; set; } = 5.0f;
        public bool EnableSaltWaterThirstIncrease { get; set; } = true;
        public bool EnableBoilingWaterDamage { get; set; } = true;
        public float SourceBlockHungerDecrease { get; set; } = 100.0f; 
        public float TemperatureThreshold { get; set; } = 27.0f; 
        public float ThirstIncreasePerDegreeMultiplier { get; set; } = 5f;
        public bool HarshHeat { get; set; } = true;
        public float HydrationLossDelayMultiplier { get; set; } = 2f;

        public Config() { }

        public Config(ICoreAPI api, Config previousConfig = null)
        {
            MaxThirst = previousConfig?.MaxThirst ?? 1500.0f;
            ThirstDamage = previousConfig?.ThirstDamage ?? 1f;
            MaxMovementSpeedPenalty = previousConfig?.MaxMovementSpeedPenalty ?? 0.3f;
            ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 10f;
            SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.5f;
            MovementSpeedPenaltyThreshold = previousConfig?.MovementSpeedPenaltyThreshold ?? 600.0f;
            RegularWaterThirstDecrease = previousConfig?.RegularWaterThirstDecrease ?? 100.0f;
            SaltWaterThirstIncrease = previousConfig?.SaltWaterThirstIncrease ?? 200f;
            BoilingWaterDamage = previousConfig?.BoilingWaterDamage ?? 5.0f;
            EnableSaltWaterThirstIncrease = previousConfig?.EnableSaltWaterThirstIncrease ?? true;
            EnableBoilingWaterDamage = previousConfig?.EnableBoilingWaterDamage ?? true;
            SourceBlockHungerDecrease = previousConfig?.SourceBlockHungerDecrease ?? 100.0f;
            TemperatureThreshold = previousConfig?.TemperatureThreshold ?? 27.0f;
            ThirstIncreasePerDegreeMultiplier = previousConfig?.ThirstIncreasePerDegreeMultiplier ?? 5f;
            HarshHeat = previousConfig?.HarshHeat ?? true;
            HydrationLossDelayMultiplier = previousConfig?.HydrationLossDelayMultiplier ?? 10f;
        }
    }
}