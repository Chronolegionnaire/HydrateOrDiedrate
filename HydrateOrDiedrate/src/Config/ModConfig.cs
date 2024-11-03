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
                    hydrateConfig.EquatidianCoolingMultipliers = new float[] { 1.25f, 1.5f, 2.0f };
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