using HydrateOrDiedrate.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.src.Config;

public static class ConfigManager
{
    public static void EnsureModConfigLoaded(ICoreAPI api)
    {
        if(ModConfig.Instance is not null) return; //Config already loaded

        if(api.Side == EnumAppSide.Server)
        {
            LoadModConfigFromDisk(api);
            StoreModConfigToWorldConfig(api);
        }
        else LoadModConfigFromWorldConfig(api);
    }

    private static void LoadModConfigFromDisk(ICoreAPI api)
    {
        try
        {
            ModConfig.Instance = api.LoadModConfig<ModConfig>(ModConfig.ConfigPath);
            ModConfig.Instance ??= new ModConfig();
            SaveModConfig(api, ModConfig.Instance);
        }
        catch(Exception ex)
        {
            api.Logger.Error(ex);
            api.Logger.Warning("[{0}] using default config", "HydrateOrDiedrate");
            ModConfig.Instance = new ModConfig();
        }
    }

    public static void StoreModConfigToWorldConfig(ICoreAPI api)
    {
        var serializedConfig = JsonConvert.SerializeObject(ModConfig.Instance, Formatting.None);

        //base64 encode for safety (because base game StringAttribute doesn't properly escape content when converting to JToken)
        var base64EncodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedConfig));

        api.World.Config.SetString(ModConfig.ConfigPath, base64EncodedConfig);
    }

    private static void LoadModConfigFromWorldConfig(ICoreAPI api)
    {
        var base64EncodedConfig = api.World.Config.GetString(ModConfig.ConfigPath);
        var serializedConfig = Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedConfig));

        ModConfig.Instance = JsonConvert.DeserializeObject<ModConfig>(serializedConfig);
    }

    public static void SaveModConfig(ICoreAPI api, ModConfig configInstance)
    {
        api.StoreModConfig(configInstance, ModConfig.ConfigPath);
    }

    internal static void UnloadModConfig() => ModConfig.Instance = null;
}
