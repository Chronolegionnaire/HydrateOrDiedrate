using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Config;

[ProtoContract]
public class Config : IModConfig
{
    // Thirst Settings
    [ProtoMember(1)] public float MaxThirst { get; set; }

    [ProtoMember(2)] public float ThirstDamage { get; set; }

    [ProtoMember(3)] public float ThirstDecayRate { get; set; }

    [ProtoMember(4)] public float ThirstIncreasePerDegreeMultiplier { get; set; }

    [ProtoMember(5)] public float ThirstDecayRateMax { get; set; }

    [ProtoMember(6)] public float HydrationLossDelayMultiplier { get; set; }

    [ProtoMember(7)] public bool EnableThirstMechanics { get; set; }

    [ProtoMember(8)] public float WaterSatiety { get; set; }

    [ProtoMember(9)] public float SaltWaterSatiety { get; set; }

    [ProtoMember(10)] public float BoilingWaterSatiety { get; set; }

    [ProtoMember(11)] public float RainWaterSatiety { get; set; }

    [ProtoMember(12)] public float DistilledWaterSatiety { get; set; }

    // Movement Speed Penalty Settings
    [ProtoMember(13)] public float MaxMovementSpeedPenalty { get; set; }

    [ProtoMember(14)] public float MovementSpeedPenaltyThreshold { get; set; }

    // Temperature and Heat Settings
    [ProtoMember(15)] public bool HarshHeat { get; set; }

    [ProtoMember(16)] public float TemperatureThreshold { get; set; }

    [ProtoMember(17)] public float HarshHeatExponentialGainMultiplier { get; set; }

    [ProtoMember(18)] public float BoilingWaterDamage { get; set; }

    [ProtoMember(19)] public bool EnableBoilingWaterDamage { get; set; }

    // Cooling Factors
    [ProtoMember(20)] public float UnequippedSlotCooling { get; set; }

    [ProtoMember(21)] public float WetnessCoolingFactor { get; set; }

    [ProtoMember(22)] public float ShelterCoolingFactor { get; set; }

    [ProtoMember(23)] public float SunlightCoolingFactor { get; set; }

    [ProtoMember(24)] public float DiurnalVariationAmplitude { get; set; }

    [ProtoMember(25)] public float RefrigerationCooling { get; set; }

    // Other Settings
    [ProtoMember(26)] public float SprintThirstMultiplier { get; set; }

    [ProtoMember(27)] public bool EnableLiquidEncumbrance { get; set; }

    [ProtoMember(28)] public float EncumbranceLimit { get; set; }

    [ProtoMember(29)] public float LiquidEncumbranceMovementSpeedDebuff { get; set; }

    // New Ability Settings
    [ProtoMember(30)] public float DromedaryMultiplierPerLevel { get; set; }

    [ProtoMember(31)]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public float[] EquatidianCoolingMultipliers { get; set; }

    // Rain Gathering Settings
    [ProtoMember(32)] public bool EnableRainGathering { get; set; }

    [ProtoMember(33)] public float RainMultiplier { get; set; }

    [ProtoMember(34)] public bool EnableParticleTicking { get; set; }

    // Keg Settings
    [ProtoMember(35)] public float KegCapacityLitres { get; set; }

    [ProtoMember(36)] public float SpoilRateUntapped { get; set; }

    [ProtoMember(37)] public float SpoilRateTapped { get; set; }

    [ProtoMember(38)] public float KegIronHoopDropChance { get; set; }

    [ProtoMember(39)] public float KegTapDropChance { get; set; }



    public Config()
    {
    }

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
        EquatidianCoolingMultipliers =
            previousConfig?.EquatidianCoolingMultipliers ?? new float[] { 1.25f, 1.5f, 2.0f };

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