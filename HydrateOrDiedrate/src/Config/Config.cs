using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Config;

[ProtoContract]
    public class Config : IModConfig
    {
        // Thirst Settings
        [ProtoMember(1)]
        public float MaxThirst { get; set; } = 1500.0f;

        [ProtoMember(2)]
        public float ThirstDamage { get; set; } = 1f;

        [ProtoMember(3)]
        public float ThirstDecayRate { get; set; } = 10f;

        [ProtoMember(4)]
        public float ThirstIncreasePerDegreeMultiplier { get; set; } = 5f;

        [ProtoMember(5)]
        public float ThirstDecayRateMax { get; set; } = 5.0f;

        [ProtoMember(6)]
        public float HydrationLossDelayMultiplier { get; set; } = 0.05f;

        [ProtoMember(7)]
        public bool EnableThirstMechanics { get; set; } = true;

        [ProtoMember(8)]
        public float WaterSatiety { get; set; } = -100f;

        [ProtoMember(9)]
        public float SaltWaterSatiety { get; set; } = -100f;

        [ProtoMember(10)]
        public float BoilingWaterSatiety { get; set; } = 0f;

        [ProtoMember(11)]
        public float RainWaterSatiety { get; set; } = -50f;

        [ProtoMember(12)]
        public float DistilledWaterSatiety { get; set; } = 0f;

        // Movement Speed Penalty Settings
        [ProtoMember(13)]
        public float MaxMovementSpeedPenalty { get; set; } = 0.3f;

        [ProtoMember(14)]
        public float MovementSpeedPenaltyThreshold { get; set; } = 600.0f;

        // Temperature and Heat Settings
        [ProtoMember(15)]
        public bool HarshHeat { get; set; } = true;

        [ProtoMember(16)]
        public float TemperatureThreshold { get; set; } = 27.0f;

        [ProtoMember(17)]
        public float HarshHeatExponentialGainMultiplier { get; set; } = 0.2f;

        [ProtoMember(18)]
        public float BoilingWaterDamage { get; set; } = 5.0f;

        [ProtoMember(19)]
        public bool EnableBoilingWaterDamage { get; set; } = true;

        // Cooling Factors
        [ProtoMember(20)]
        public float UnequippedSlotCooling { get; set; } = 1.0f;

        [ProtoMember(21)]
        public float WetnessCoolingFactor { get; set; } = 1.5f;

        [ProtoMember(22)]
        public float ShelterCoolingFactor { get; set; } = 1.5f;

        [ProtoMember(23)]
        public float SunlightCoolingFactor { get; set; } = 1.0f;

        [ProtoMember(24)]
        public float DiurnalVariationAmplitude { get; set; } = 18f;

        [ProtoMember(25)]
        public float RefrigerationCooling { get; set; } = 20.0f;

        // Other Settings
        [ProtoMember(26)]
        public float SprintThirstMultiplier { get; set; } = 1.5f;

        [ProtoMember(27)]
        public bool EnableLiquidEncumbrance { get; set; } = true;

        [ProtoMember(28)]
        public float EncumbranceLimit { get; set; } = 4.0f;

        [ProtoMember(29)]
        public float LiquidEncumbranceMovementSpeedDebuff { get; set; } = 0.4f;

        // New Ability Settings
        [ProtoMember(30)]
        public float DromedaryMultiplierPerLevel { get; set; } = 0.3f;

        [ProtoMember(31)]
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public float[] EquatidianCoolingMultipliers { get; set; }

        // Rain Gathering Settings
        [ProtoMember(32)]
        public bool EnableRainGathering { get; set; } = true;

        [ProtoMember(33)]
        public float RainMultiplier { get; set; } = 1.0f;

        [ProtoMember(34)]
        public bool EnableParticleTicking { get; set; } = true;

        // Keg Settings
        [ProtoMember(35)]
        public float KegCapacityLitres { get; set; } = 100.0f;

        [ProtoMember(36)]
        public float SpoilRateUntapped { get; set; } = 0.15f;

        [ProtoMember(37)]
        public float SpoilRateTapped { get; set; } = 0.75f;

        [ProtoMember(38)]
        public float KegIronHoopDropChance { get; set; } = 0.8f;

        [ProtoMember(39)]
        public float KegTapDropChance { get; set; } = 0.9f;



    public Config() { }

    public Config(ICoreAPI api, Config previousConfig = null)
    {
        // Thirst Settings
        MaxThirst = previousConfig?.MaxThirst ?? 1500.0f;
        ThirstDamage = previousConfig?.ThirstDamage ?? 1f;
        ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 10f;
        ThirstIncreasePerDegreeMultiplier = previousConfig?.ThirstIncreasePerDegreeMultiplier ?? 5f;
        ThirstDecayRateMax = previousConfig?.ThirstDecayRateMax ?? 5.0f;
        HydrationLossDelayMultiplier = previousConfig?.HydrationLossDelayMultiplier ?? 0.05f;
        EnableThirstMechanics = previousConfig?.EnableThirstMechanics ?? true;
        WaterSatiety = previousConfig?.WaterSatiety ?? -100f;
        SaltWaterSatiety = previousConfig?.SaltWaterSatiety ?? -100f;
        BoilingWaterSatiety = previousConfig?.BoilingWaterSatiety ?? 0f;
        RainWaterSatiety = previousConfig?.RainWaterSatiety ?? -50f;
        DistilledWaterSatiety = previousConfig?.DistilledWaterSatiety ?? 0f;

        // Movement Speed Penalty Settings
        MaxMovementSpeedPenalty = previousConfig?.MaxMovementSpeedPenalty ?? 0.3f;
        MovementSpeedPenaltyThreshold = previousConfig?.MovementSpeedPenaltyThreshold ?? 600.0f;

        // Temperature and Heat Settings
        HarshHeat = previousConfig?.HarshHeat ?? true;
        TemperatureThreshold = previousConfig?.TemperatureThreshold ?? 27.0f;
        HarshHeatExponentialGainMultiplier = previousConfig?.HarshHeatExponentialGainMultiplier ?? 0.2f;
        BoilingWaterDamage = previousConfig?.BoilingWaterDamage ?? 5.0f;
        EnableBoilingWaterDamage = previousConfig?.EnableBoilingWaterDamage ?? true;

        // Cooling Factors
        UnequippedSlotCooling = previousConfig?.UnequippedSlotCooling ?? 1.0f;
        WetnessCoolingFactor = previousConfig?.WetnessCoolingFactor ?? 1.5f;
        ShelterCoolingFactor = previousConfig?.ShelterCoolingFactor ?? 1.5f;
        SunlightCoolingFactor = previousConfig?.SunlightCoolingFactor ?? 1.0f;
        DiurnalVariationAmplitude = previousConfig?.DiurnalVariationAmplitude ?? 18f;
        RefrigerationCooling = previousConfig?.RefrigerationCooling ?? 20.0f;

        // Other Settings
        SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.5f;
        EnableLiquidEncumbrance = previousConfig?.EnableLiquidEncumbrance ?? true;
        EncumbranceLimit = previousConfig?.EncumbranceLimit ?? 4.0f;
        LiquidEncumbranceMovementSpeedDebuff = previousConfig?.LiquidEncumbranceMovementSpeedDebuff ?? 0.4f;
        
        DromedaryMultiplierPerLevel = previousConfig?.DromedaryMultiplierPerLevel ?? 0.3f;
        EquatidianCoolingMultipliers = previousConfig?.EquatidianCoolingMultipliers ?? new float[] { 1.25f, 1.5f, 2.0f };
        
        // Rain Gathering Settings
        EnableRainGathering = previousConfig?.EnableRainGathering ?? true;
        RainMultiplier = previousConfig?.RainMultiplier ?? 1.0f;
        EnableParticleTicking = previousConfig?.EnableParticleTicking ?? true;
        
        // Keg settings
        KegCapacityLitres = previousConfig?.KegCapacityLitres ?? 100.0f;
        SpoilRateUntapped = previousConfig?.SpoilRateUntapped ?? 0.15f;
        SpoilRateTapped = previousConfig?.SpoilRateTapped ?? 0.75f;
        KegIronHoopDropChance = previousConfig?.KegIronHoopDropChance ?? 0.8f;
        KegTapDropChance = previousConfig?.KegTapDropChance ?? 0.9f;
    }
}