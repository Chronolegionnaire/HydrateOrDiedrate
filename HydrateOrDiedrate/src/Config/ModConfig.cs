using System;
using System.IO;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config
{
    public static class ModConfig
    {
        public static T ReadConfig<T>(ICoreAPI api, string jsonConfig) where T : class, IModConfig
        {
            T config = api.LoadModConfig<T>(jsonConfig);
            if (config == null)
            {
                config = Activator.CreateInstance<T>();
                if (config is Config hydrateConfig)
                {
                    // Thirst Settings
                    hydrateConfig.MaxThirst = 1500.0f;
                    hydrateConfig.ThirstDamage = 1f;
                    hydrateConfig.ThirstDecayRate = 10f;
                    hydrateConfig.ThirstDecayRateMax = 5.0f;
                    hydrateConfig.HydrationLossDelayMultiplier = 0.05f;
                    hydrateConfig.EnableThirstMechanics = true;
                    hydrateConfig.WaterSatiety = -100f;
                    hydrateConfig.SaltWaterSatiety = -100f;
                    hydrateConfig.BoilingWaterSatiety = 0f;
                    hydrateConfig.RainWaterSatiety = -50f;
                    hydrateConfig.DistilledWaterSatiety = 0f;
                    hydrateConfig.SprintThirstMultiplier = 1.5f;
                    hydrateConfig.EnableBoilingWaterDamage = true;
                    hydrateConfig.BoilingWaterDamage = 5.0f;

                    // Movement Speed Penalty Settings
                    hydrateConfig.MaxMovementSpeedPenalty = 0.3f;
                    hydrateConfig.MovementSpeedPenaltyThreshold = 600.0f;

                    // Liquid Encumbrance Settings
                    hydrateConfig.EnableLiquidEncumbrance = true;
                    hydrateConfig.EncumbranceLimit = 4.0f;
                    hydrateConfig.LiquidEncumbranceMovementSpeedDebuff = 0.4f;

                    // Temperature and Heat Settings
                    hydrateConfig.HarshHeat = true;
                    hydrateConfig.TemperatureThreshold = 27.0f;
                    hydrateConfig.ThirstIncreasePerDegreeMultiplier = 5f;
                    hydrateConfig.HarshHeatExponentialGainMultiplier = 0.2f;

                    // Cooling Factors
                    hydrateConfig.UnequippedSlotCooling = 1.0f;
                    hydrateConfig.WetnessCoolingFactor = 1.5f;
                    hydrateConfig.ShelterCoolingFactor = 1.5f;
                    hydrateConfig.SunlightCoolingFactor = 1.0f;
                    hydrateConfig.DiurnalVariationAmplitude = 18f;
                    hydrateConfig.RefrigerationCooling = 20.0f;

                    // XSkills Settings
                    hydrateConfig.DromedaryMultiplierPerLevel = 0.3f;
                    hydrateConfig.EquatidianCoolingMultipliers = new float[] { 1.25f, 1.5f, 2.0f };

                    // Rain Gathering Settings
                    hydrateConfig.EnableRainGathering = true;
                    hydrateConfig.RainMultiplier = 1.0f;

                    // Keg Settings
                    hydrateConfig.KegCapacityLitres = 100.0f;
                    hydrateConfig.SpoilRateUntapped = 0.15f;
                    hydrateConfig.SpoilRateTapped = 0.65f;
                    hydrateConfig.KegIronHoopDropChance = 0.8f;
                    hydrateConfig.KegTapDropChance = 0.9f;

                    // Tun Settings
                    hydrateConfig.TunCapacityLitres = 950f;
                    hydrateConfig.TunSpoilRateMultiplier = 1.0f;
                }
                WriteConfig(api, jsonConfig, config);
            }
            return config;
        }

        public static void WriteConfig<T>(ICoreAPI api, string jsonConfig, T config) where T : class, IModConfig
        {
            api.StoreModConfig(config, jsonConfig);
        }
    }
}
