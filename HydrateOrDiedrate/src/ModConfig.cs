using System;
using System.IO;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Configuration
{
    public static class ModConfig
    {
        public static T ReadConfig<T>(ICoreAPI api, string jsonConfig) where T : class, IModConfig
        {
            string configPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), "hydrateordiedrate");
            EnsureDirectoryExists(configPath);
            string configFilePath = Path.Combine(configPath, jsonConfig);

            T config;

            try
            {
                config = LoadConfig<T>(api, configFilePath);

                if (config == null)
                {
                    GenerateConfig<T>(api, configFilePath);
                    config = LoadConfig<T>(api, configFilePath);
                }
                else
                {
                    GenerateConfig(api, configFilePath, config);
                }
            }
            catch
            {
                GenerateConfig<T>(api, configFilePath);
                config = LoadConfig<T>(api, configFilePath);
            }

            return config;
        }

        public static void WriteConfig<T>(ICoreAPI api, string jsonConfig, T config) where T : class, IModConfig
        {
            string configPath = Path.Combine(api.GetOrCreateDataPath("ModConfig"), "hydrateordiedrate");
            EnsureDirectoryExists(configPath);
            string configFilePath = Path.Combine(configPath, jsonConfig);

            GenerateConfig(api, configFilePath, config);
        }

        private static T LoadConfig<T>(ICoreAPI api, string configFilePath) where T : class, IModConfig
        {
            return api.LoadModConfig<T>(configFilePath);
        }

        private static void GenerateConfig<T>(ICoreAPI api, string configFilePath, T previousConfig = null) where T : class, IModConfig
        {
            api.StoreModConfig(CloneConfig(api, previousConfig), configFilePath);
        }

        private static T CloneConfig<T>(ICoreAPI api, T config = null) where T : class, IModConfig
        {
            return (T)Activator.CreateInstance(typeof(T), new object[] { api, config });
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
