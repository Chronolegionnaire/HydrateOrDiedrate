using System;
using System.IO;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config
{
    public static class ModConfig
    {
        public static T ReadConfig<T>(ICoreAPI api, string jsonConfig) where T : class, IModConfig
        {
            string configPath = GetConfigPath(api);
            string configFilePath = Path.Combine(configPath, jsonConfig);

            EnsureDirectoryExists(configPath);
            return LoadOrGenerateConfig<T>(api, configFilePath);
        }

        public static void WriteConfig<T>(ICoreAPI api, string jsonConfig, T config) where T : class, IModConfig
        {
            string configPath = GetConfigPath(api);
            string configFilePath = Path.Combine(configPath, jsonConfig);

            EnsureDirectoryExists(configPath);
            api.StoreModConfig(config, configFilePath);
        }

        private static T LoadOrGenerateConfig<T>(ICoreAPI api, string configFilePath) where T : class, IModConfig
        {
            T config = api.LoadModConfig<T>(configFilePath);
            if (config == null)
            {
                api.StoreModConfig(Activator.CreateInstance<T>(), configFilePath);
                config = api.LoadModConfig<T>(configFilePath);
            }
            else
            {
                api.StoreModConfig(config, configFilePath);
            }
            return config;
        }

        public static string GetConfigPath(ICoreAPI api)
        {

            string basePath = api.GetOrCreateDataPath("ModConfig");
            
            if (!basePath.EndsWith("hydrateordiedrate"))
            {
                basePath = Path.Combine(basePath, "hydrateordiedrate");
            }

            return basePath;
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