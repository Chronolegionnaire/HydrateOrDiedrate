using System;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config
{
    public static class ModConfig
    {
        public static T ReadConfig<T>(ICoreAPI api, string jsonConfig) where T : class, IModConfig
        {
            T oldConfig = api.LoadModConfig<T>(jsonConfig);
            if (oldConfig == null)
            {
                oldConfig = Activator.CreateInstance<T>();
            }
            if (oldConfig is Config oldHydrateConfig)
            {
                var newHydrateConfig = new Config(api, oldHydrateConfig);
                WriteConfig(api, jsonConfig, newHydrateConfig);
                return newHydrateConfig as T;
            }
            WriteConfig(api, jsonConfig, oldConfig);
            return oldConfig;
        }

        public static void WriteConfig<T>(ICoreAPI api, string jsonConfig, T config) where T : class, IModConfig
        {
            api.StoreModConfig(config, jsonConfig);
        }
    }
}