using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Config;

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
            MapLegacyData(ModConfig.Instance);
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

    private static void MapLegacyData(ModConfig instance)
    {
        if(instance?.LegacyData is null) return;
        
        var legacyData = new Dictionary<string, JToken>(instance.LegacyData, StringComparer.OrdinalIgnoreCase);
        var subConfigs = typeof(ModConfig)
            .GetProperties()
            .Where(prop => prop.PropertyType.IsClass)
            .Select(prop => prop.GetValue(instance));

        //Auto fields
        foreach(var subConfig in subConfigs)
        {
            foreach(var property in subConfig.GetType().GetProperties())
            {
                if(legacyData.TryGetValue(property.Name, out var token))
                {
                    try
                    {
                        property.SetValue(subConfig, token.ToObject(property.PropertyType));
                    }
                    catch
                    {
                        //Ignore unknown legacy data
                    }
                }
            }
        }

        //Manual fields
        if(TryGetBool(legacyData, "EnableBoilingWaterDamage", out var value) && !value) instance.Thirst.BoilingWaterDamage = 0;
        if(TryGetBool(legacyData, "EnableThirstMechanics", out value)) instance.Thirst.Enabled = value;
        if(TryGetBool(legacyData, "WaterPerish", out value)) instance.PerishRates.Enabled = value;
        if(TryGetBool(legacyData, "EnableLiquidEncumbrance", out value)) instance.LiquidEncumbrance.Enabled = value;

        instance.LegacyData = null;
    }

    private static bool TryGetBool(Dictionary<string, JToken> legacyData, string key, out bool result)
    {
        if(legacyData.TryGetValue(key, out var boolToken) && boolToken.Type == JTokenType.Boolean)
        {
            result = boolToken.ToObject<bool>();
            return true;
        }

        result = false;
        return false;
    }
}
