using System;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config;

[ProtoContract]
public class Config : IModConfig
{
    // Thirst Settings
    [ProtoMember(1)] public float MaxThirst { get; set; }
    [ProtoMember(2)] public float ThirstDamage { get; set; }
    [ProtoMember(3)] public float ThirstDecayRate { get; set; }
    [ProtoMember(4)] public float ThirstDecayRateMax { get; set; }
    [ProtoMember(5)] public float HydrationLossDelayMultiplier { get; set; }
    [ProtoMember(6, IsRequired = true)] public bool EnableThirstMechanics { get; set; }
    [ProtoMember(7)] public float WaterSatiety { get; set; }
    [ProtoMember(8)] public float SaltWaterSatiety { get; set; }
    [ProtoMember(9)] public float BoilingWaterSatiety { get; set; }
    [ProtoMember(10)] public float RainWaterSatiety { get; set; }
    [ProtoMember(11)] public float DistilledWaterSatiety { get; set; }
    [ProtoMember(12)] public float BoiledWaterSatiety { get; set; }
    [ProtoMember(13)] public float BoiledRainWaterSatiety { get; set; }
    [ProtoMember(14)] public float SprintThirstMultiplier { get; set; }
    [ProtoMember(15, IsRequired = true)] public bool EnableBoilingWaterDamage { get; set; }
    [ProtoMember(16)] public float BoilingWaterDamage { get; set; }

    // Movement Speed Penalty Settings
    [ProtoMember(17)] public float MaxMovementSpeedPenalty { get; set; }
    [ProtoMember(18)] public float MovementSpeedPenaltyThreshold { get; set; }

    // Liquid Encumbrance Settings
    [ProtoMember(19, IsRequired = true)] public bool EnableLiquidEncumbrance { get; set; }
    [ProtoMember(20)] public float EncumbranceLimit { get; set; }
    [ProtoMember(21)] public float LiquidEncumbranceMovementSpeedDebuff { get; set; }

    // Temperature and Heat Settings
    [ProtoMember(22, IsRequired = true)] public bool HarshHeat { get; set; }
    [ProtoMember(23)] public float TemperatureThreshold { get; set; }
    [ProtoMember(24)] public float ThirstIncreasePerDegreeMultiplier { get; set; }
    [ProtoMember(25)] public float HarshHeatExponentialGainMultiplier { get; set; }

    // Cooling Factors
    [ProtoMember(26)] public float UnequippedSlotCooling { get; set; }
    [ProtoMember(27)] public float WetnessCoolingFactor { get; set; }
    [ProtoMember(28)] public float ShelterCoolingFactor { get; set; }
    [ProtoMember(29)] public float SunlightCoolingFactor { get; set; }
    [ProtoMember(30)] public float DiurnalVariationAmplitude { get; set; }
    [ProtoMember(31)] public float RefrigerationCooling { get; set; }

    // XSkills Settings
    [ProtoMember(32)] public float DromedaryMultiplierPerLevel { get; set; }
    [ProtoMember(33)]
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public float[] EquatidianCoolingMultipliers { get; set; }

    // Rain Gathering Settings
    [ProtoMember(34, IsRequired = true)] public bool EnableRainGathering { get; set; }
    [ProtoMember(35)] public float RainMultiplier { get; set; }
    [ProtoMember(36, IsRequired = true)] public bool EnableParticleTicking { get; set; }

    // Keg Settings
    [ProtoMember(37)] public float KegCapacityLitres { get; set; }
    [ProtoMember(38)] public float SpoilRateUntapped { get; set; }
    [ProtoMember(39)] public float SpoilRateTapped { get; set; }
    [ProtoMember(40)] public float KegIronHoopDropChance { get; set; }
    [ProtoMember(41)] public float KegTapDropChance { get; set; }

    // Tun Settings
    [ProtoMember(42)] public float TunCapacityLitres { get; set; }
    [ProtoMember(43)] public float TunSpoilRateMultiplier { get; set; }

    // Misc Settings
    [ProtoMember(44, IsRequired = true)] public bool DisableDrunkSway { get; set; }

    // Well/Aquifer Settings
    [ProtoMember(45)]
    public float WellSpringOutputMultiplier { get; set; }
    
    [ProtoMember(46)]
    public int WellwaterDepthMaxBase { get; set; }
    
    [ProtoMember(47)]
    public int WellwaterDepthMaxClay { get; set; }
    
    [ProtoMember(48)]
    public int WellwaterDepthMaxStone { get; set; }
    
    [ProtoMember(49)]
    public double AquiferRandomMultiplierChance { get; set; }
    
    [ProtoMember(50)]
    public int AquiferStep { get; set; }

    [ProtoMember(51)]
    public double AquiferWaterBlockMultiplier { get; set; }

    [ProtoMember(52)]
    public double AquiferSaltWaterMultiplier { get; set; }

    [ProtoMember(53)]
    public int AquiferBoilingWaterMultiplier { get; set; }
    
    [ProtoMember(54)] 
    public float WellWaterFreshSatiety { get; set; }
    
    [ProtoMember(55)] 
    public float WellWaterSaltSatiety { get; set; }
    
    [ProtoMember(56)] 
    public float WellWaterMuddySatiety { get; set; }
    
    [ProtoMember(57)] 
    public float WellWaterTaintedSatiety { get; set; }
    
    [ProtoMember(58)] 
    public float WellWaterPoisonedSatiety { get; set; }
    
    [ProtoMember(59)] 
    public float WellWaterMuddySaltSatiety { get; set; }
    
    [ProtoMember(60)] 
    public float WellWaterTaintedSaltSatiety { get; set; }
    
    [ProtoMember(61)] 
    public float WellWaterPoisonedSaltSatiety { get; set; }
    
    [ProtoMember(62, IsRequired = true)] 
    public bool AquiferDepthScaling { get; set; }
    
    // Liquid Perish Rates
    
    [ProtoMember(63)] 
    public float RainWaterFreshHours { get; set; }
    
    [ProtoMember(64)] 
    public float RainWaterTransitionHours { get; set; }
    
    [ProtoMember(65)] 
    public float BoiledWaterFreshHours { get; set; }
    
    [ProtoMember(66)] 
    public float BoiledWaterTransitionHours { get; set; }
    
    [ProtoMember(67)] 
    public float BoiledRainWaterFreshHours { get; set; }
    
    [ProtoMember(68)] 
    public float BoiledRainWaterTransitionHours { get; set; }
    
    [ProtoMember(69)] 
    public float DistilledWaterFreshHours { get; set; }
    
    [ProtoMember(70)] 
    public float DistilledWaterTransitionHours { get; set; }
    
    [ProtoMember(71)] 
    public float WellWaterFreshFreshHours { get; set; }
    
    [ProtoMember(72)] 
    public float WellWaterFreshTransitionHours { get; set; }
    
    [ProtoMember(73)] 
    public float WellWaterSaltFreshHours { get; set; }
    
    [ProtoMember(74)] 
    public float WellWaterSaltTransitionHours { get; set; }
    
    [ProtoMember(75)] 
    public float WellWaterMuddyFreshHours { get; set; }
    
    [ProtoMember(76)] 
    public float WellWaterMuddyTransitionHours { get; set; }
    
    [ProtoMember(77)] 
    public float WellWaterTaintedFreshHours { get; set; }
    
    [ProtoMember(78)] 
    public float WellWaterTaintedTransitionHours { get; set; }
    
    [ProtoMember(79)] 
    public float WellWaterPoisonedFreshHours { get; set; }
    
    [ProtoMember(80)] 
    public float WellWaterPoisonedTransitionHours { get; set; }
    
    [ProtoMember(81)] 
    public float WellWaterMuddySaltFreshHours { get; set; }
    
    [ProtoMember(82)] 
    public float WellWaterMuddySaltTransitionHours { get; set; }
    
    [ProtoMember(83)] 
    public float WellWaterTaintedSaltFreshHours { get; set; }
    
    [ProtoMember(84)] 
    public float WellWaterTaintedSaltTransitionHours { get; set; }
    
    [ProtoMember(85)] 
    public float WellWaterPoisonedSaltFreshHours { get; set; }
    
    [ProtoMember(86)] 
    public float WellWaterPoisonedSaltTransitionHours { get; set; }


    public Config()
    {
        // Thirst Settings
        MaxThirst = 1500.0f;
        ThirstDamage = 1f;
        ThirstDecayRate = 10f;
        ThirstDecayRateMax = 5.0f;
        HydrationLossDelayMultiplier = 0.05f;
        EnableThirstMechanics = true;
        WaterSatiety = -100f;
        SaltWaterSatiety = -100f;
        BoilingWaterSatiety = 0f;
        RainWaterSatiety = -50f;
        DistilledWaterSatiety = 0f;
        BoiledWaterSatiety = 0f;
        BoiledRainWaterSatiety = 0f;
        SprintThirstMultiplier = 1.5f;
        EnableBoilingWaterDamage = true;
        BoilingWaterDamage = 5.0f;

        // Movement Speed Penalty Settings
        MaxMovementSpeedPenalty = 0.3f;
        MovementSpeedPenaltyThreshold = 600.0f;

        // Liquid Encumbrance
        EnableLiquidEncumbrance = true;
        EncumbranceLimit = 4.0f;
        LiquidEncumbranceMovementSpeedDebuff = 0.4f;

        // Temperature and Heat
        HarshHeat = true;
        TemperatureThreshold = 27.0f;
        ThirstIncreasePerDegreeMultiplier = 5f;
        HarshHeatExponentialGainMultiplier = 0.2f;

        // Cooling Factors
        UnequippedSlotCooling = 1.0f;
        WetnessCoolingFactor = 1.5f;
        ShelterCoolingFactor = 1.5f;
        SunlightCoolingFactor = 1.0f;
        DiurnalVariationAmplitude = 18f;
        RefrigerationCooling = 20.0f;

        // XSkills
        DromedaryMultiplierPerLevel = 0.3f;
        EquatidianCoolingMultipliers = new float[] { 1.25f, 1.5f, 2.0f };

        // Rain Gathering
        EnableRainGathering = true;
        RainMultiplier = 1.0f;
        EnableParticleTicking = false;

        // Keg
        KegCapacityLitres = 100.0f;
        SpoilRateUntapped = 0.15f;
        SpoilRateTapped = 0.65f;
        KegIronHoopDropChance = 0.8f;
        KegTapDropChance = 0.9f;

        // Tun
        TunCapacityLitres = 950f;
        TunSpoilRateMultiplier = 1.0f;

        // Misc
        DisableDrunkSway = true;

        // Well/Aquifer
        WellSpringOutputMultiplier = 1.0f;
        WellwaterDepthMaxBase = 5;
        WellwaterDepthMaxClay = 7;
        WellwaterDepthMaxStone = 10;
        AquiferRandomMultiplierChance = 0.02;
        AquiferStep = 4;
        AquiferWaterBlockMultiplier = 4.0;
        AquiferSaltWaterMultiplier = 4.0;
        AquiferBoilingWaterMultiplier = 100;
        WellWaterFreshSatiety = 0f;
        WellWaterSaltSatiety = -100f;
        WellWaterMuddySatiety = -75f;
        WellWaterTaintedSatiety = -400f;
        WellWaterPoisonedSatiety = 0f;
        WellWaterMuddySaltSatiety = -75f;
        WellWaterTaintedSaltSatiety = -400f;
        WellWaterPoisonedSaltSatiety = 0f;
        AquiferDepthScaling = true;
        
        // Liquid Perish Rates
        RainWaterFreshHours = 150f;
        RainWaterTransitionHours = 36f;
        BoiledWaterFreshHours = 75f;
        BoiledWaterTransitionHours = 18f;
        BoiledRainWaterFreshHours = 75f;
        BoiledRainWaterTransitionHours = 18f;
        DistilledWaterFreshHours = 150f;
        DistilledWaterTransitionHours = 36f;
        WellWaterFreshFreshHours = 150f;
        WellWaterFreshTransitionHours = 36f;
        WellWaterSaltFreshHours = 150f;
        WellWaterSaltTransitionHours = 36f;
        WellWaterMuddyFreshHours = 150f;
        WellWaterMuddyTransitionHours = 36f;
        WellWaterTaintedFreshHours = 150f;
        WellWaterTaintedTransitionHours = 36f;
        WellWaterPoisonedFreshHours = 150f;
        WellWaterPoisonedTransitionHours = 36f;
        WellWaterMuddySaltFreshHours = 150f;
        WellWaterMuddySaltTransitionHours = 36f;
        WellWaterTaintedSaltFreshHours = 150f;
        WellWaterTaintedSaltTransitionHours = 36f;
        WellWaterPoisonedSaltFreshHours = 150f;
        WellWaterPoisonedSaltTransitionHours = 36f;
    }
    public Config(ICoreAPI api, Config previousConfig = null)
    {
        // Thirst Settings
        MaxThirst = previousConfig?.MaxThirst ?? 1500.0f;
        ThirstDamage = previousConfig?.ThirstDamage ?? 1f;
        ThirstDecayRate = previousConfig?.ThirstDecayRate ?? 10f;
        ThirstDecayRateMax = previousConfig?.ThirstDecayRateMax ?? 5.0f;
        HydrationLossDelayMultiplier = previousConfig?.HydrationLossDelayMultiplier ?? 0.05f;
        EnableThirstMechanics = previousConfig?.EnableThirstMechanics ?? true;
        WaterSatiety = previousConfig?.WaterSatiety ?? -100f;
        SaltWaterSatiety = previousConfig?.SaltWaterSatiety ?? -100f;
        BoilingWaterSatiety = previousConfig?.BoilingWaterSatiety ?? 0f;
        RainWaterSatiety = previousConfig?.RainWaterSatiety ?? -50f;
        DistilledWaterSatiety = previousConfig?.DistilledWaterSatiety ?? 0f;
        BoiledWaterSatiety = previousConfig?.BoiledWaterSatiety ?? 0f;
        BoiledRainWaterSatiety = previousConfig?.BoiledRainWaterSatiety ?? 0f;
        SprintThirstMultiplier = previousConfig?.SprintThirstMultiplier ?? 1.5f;
        EnableBoilingWaterDamage = previousConfig?.EnableBoilingWaterDamage ?? true;
        BoilingWaterDamage = previousConfig?.BoilingWaterDamage ?? 5.0f;

        // Movement Speed Penalty Settings
        MaxMovementSpeedPenalty = previousConfig?.MaxMovementSpeedPenalty ?? 0.3f;
        MovementSpeedPenaltyThreshold = previousConfig?.MovementSpeedPenaltyThreshold ?? 600.0f;

        // Liquid Encumbrance Settings
        EnableLiquidEncumbrance = previousConfig?.EnableLiquidEncumbrance ?? true;
        EncumbranceLimit = previousConfig?.EncumbranceLimit ?? 4.0f;
        LiquidEncumbranceMovementSpeedDebuff = previousConfig?.LiquidEncumbranceMovementSpeedDebuff ?? 0.4f;

        // Temperature and Heat Settings
        HarshHeat = previousConfig?.HarshHeat ?? true;
        TemperatureThreshold = previousConfig?.TemperatureThreshold ?? 27.0f;
        ThirstIncreasePerDegreeMultiplier = previousConfig?.ThirstIncreasePerDegreeMultiplier ?? 5f;
        HarshHeatExponentialGainMultiplier = previousConfig?.HarshHeatExponentialGainMultiplier ?? 0.2f;

        // Cooling Factors
        UnequippedSlotCooling = previousConfig?.UnequippedSlotCooling ?? 1.0f;
        WetnessCoolingFactor = previousConfig?.WetnessCoolingFactor ?? 1.5f;
        ShelterCoolingFactor = previousConfig?.ShelterCoolingFactor ?? 1.5f;
        SunlightCoolingFactor = previousConfig?.SunlightCoolingFactor ?? 1.0f;
        DiurnalVariationAmplitude = previousConfig?.DiurnalVariationAmplitude ?? 18f;
        RefrigerationCooling = previousConfig?.RefrigerationCooling ?? 20.0f;

        // XSkills Settings
        DromedaryMultiplierPerLevel = previousConfig?.DromedaryMultiplierPerLevel ?? 0.3f;
        EquatidianCoolingMultipliers = previousConfig?.EquatidianCoolingMultipliers ?? new float[] { 1.25f, 1.5f, 2.0f };

        // Rain Gathering Settings
        EnableRainGathering = previousConfig?.EnableRainGathering ?? true;
        RainMultiplier = previousConfig?.RainMultiplier ?? 1.0f;
        EnableParticleTicking = previousConfig?.EnableParticleTicking ?? false;

        // Keg Settings
        KegCapacityLitres = previousConfig?.KegCapacityLitres ?? 100.0f;
        SpoilRateUntapped = previousConfig?.SpoilRateUntapped ?? 0.15f;
        SpoilRateTapped = previousConfig?.SpoilRateTapped ?? 0.65f;
        KegIronHoopDropChance = previousConfig?.KegIronHoopDropChance ?? 0.8f;
        KegTapDropChance = previousConfig?.KegTapDropChance ?? 0.9f;

        // Tun Settings
        TunCapacityLitres = previousConfig?.TunCapacityLitres ?? 950.0f;
        TunSpoilRateMultiplier = previousConfig?.TunSpoilRateMultiplier ?? 1.0f;

        // Misc Settings
        DisableDrunkSway = previousConfig?.DisableDrunkSway ?? true;

        // Well/Aquifer Settings
        WellSpringOutputMultiplier = previousConfig?.WellSpringOutputMultiplier ?? 1f;
        WellwaterDepthMaxBase = previousConfig?.WellwaterDepthMaxBase ?? 5;
        WellwaterDepthMaxClay = previousConfig?.WellwaterDepthMaxClay ?? 7;
        WellwaterDepthMaxStone = previousConfig?.WellwaterDepthMaxStone ?? 10;
        AquiferRandomMultiplierChance = previousConfig?.AquiferRandomMultiplierChance ?? 0.02;
        AquiferStep = previousConfig?.AquiferStep ?? 4;
        AquiferWaterBlockMultiplier = previousConfig?.AquiferWaterBlockMultiplier ?? 4.0;
        AquiferSaltWaterMultiplier = previousConfig?.AquiferSaltWaterMultiplier ?? 4.0;
        AquiferBoilingWaterMultiplier = previousConfig?.AquiferBoilingWaterMultiplier ?? 100;
        WellWaterFreshSatiety = previousConfig?.WellWaterFreshSatiety ?? 0f;
        WellWaterSaltSatiety = previousConfig?.WellWaterSaltSatiety ?? -100f;
        WellWaterMuddySatiety = previousConfig?.WellWaterMuddySatiety ?? -75f;
        WellWaterTaintedSatiety = previousConfig?.WellWaterTaintedSatiety ?? -400f;
        WellWaterPoisonedSatiety = previousConfig?.WellWaterPoisonedSatiety ?? 0f;
        WellWaterMuddySaltSatiety = previousConfig?.WellWaterMuddySaltSatiety ?? -75f;
        WellWaterTaintedSaltSatiety = previousConfig?.WellWaterTaintedSaltSatiety ?? -400f;
        WellWaterPoisonedSaltSatiety = previousConfig?.WellWaterPoisonedSaltSatiety ?? 0f;
        AquiferDepthScaling = previousConfig?.AquiferDepthScaling ?? true;
        
        // Liquid Perish Rates
         RainWaterFreshHours = previousConfig?.RainWaterFreshHours ?? 150f;
         RainWaterTransitionHours = previousConfig?.RainWaterTransitionHours ?? 36f;
         BoiledWaterFreshHours = previousConfig?.BoiledWaterFreshHours ?? 75f;
         BoiledWaterTransitionHours = previousConfig?.BoiledWaterTransitionHours ?? 18f;
         BoiledRainWaterFreshHours = previousConfig?.BoiledRainWaterFreshHours ?? 75f;
         BoiledRainWaterTransitionHours = previousConfig?.BoiledRainWaterTransitionHours ?? 18f;
         DistilledWaterFreshHours = previousConfig?.DistilledWaterFreshHours ?? 150f;
         DistilledWaterTransitionHours = previousConfig?.DistilledWaterTransitionHours ?? 36f;
         WellWaterFreshFreshHours = previousConfig?.WellWaterFreshFreshHours ?? 150f;
         WellWaterFreshTransitionHours = previousConfig?.WellWaterFreshTransitionHours ?? 36f;
         WellWaterSaltFreshHours = previousConfig?.WellWaterSaltFreshHours ?? 150f;
         WellWaterSaltTransitionHours = previousConfig?.WellWaterSaltTransitionHours ?? 36f;
         WellWaterMuddyFreshHours = previousConfig?.WellWaterMuddyFreshHours ?? 150f;
         WellWaterMuddyTransitionHours = previousConfig?.WellWaterMuddyTransitionHours ?? 36f;
         WellWaterTaintedFreshHours = previousConfig?.WellWaterTaintedFreshHours ?? 150f;
         WellWaterTaintedTransitionHours = previousConfig?.WellWaterTaintedTransitionHours ?? 36f;
         WellWaterPoisonedFreshHours = previousConfig?.WellWaterPoisonedFreshHours ?? 150f;
         WellWaterPoisonedTransitionHours = previousConfig?.WellWaterPoisonedTransitionHours ?? 36f;
         WellWaterMuddySaltFreshHours = previousConfig?.WellWaterMuddySaltFreshHours ?? 150f;
         WellWaterMuddySaltTransitionHours = previousConfig?.WellWaterMuddySaltTransitionHours ?? 36f;
         WellWaterTaintedSaltFreshHours = previousConfig?.WellWaterTaintedSaltFreshHours ?? 150f;
         WellWaterTaintedSaltTransitionHours = previousConfig?.WellWaterTaintedSaltTransitionHours ?? 36f;
         WellWaterPoisonedSaltFreshHours = previousConfig?.WellWaterPoisonedSaltFreshHours ?? 150f;
         WellWaterPoisonedSaltTransitionHours = previousConfig?.WellWaterPoisonedSaltTransitionHours ?? 36f;
    }
}
