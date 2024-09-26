using System.Collections.Generic;
using HydrateOrDiedrate.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate;

public class RainHarvesterManager
{
    private ICoreServerAPI serverAPI;
    private Dictionary<BlockPos, RainHarvesterData> registeredHarvesters;
    private long tickListenerId;
    private int globalTickCounter = 0;

    public RainHarvesterManager(ICoreServerAPI api)
    {
        serverAPI = api;
        registeredHarvesters = new Dictionary<BlockPos, RainHarvesterData>();

        var config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");
        if (config.EnableRainGathering)
        {
            tickListenerId = api.Event.RegisterGameTickListener(OnTick, 500);
        }
    }

    public void RegisterHarvester(BlockPos position, RainHarvesterData data)
    {
        if (!registeredHarvesters.ContainsKey(position))
        {
            int newAdaptiveTickInterval = globalTickCounter + data.calculatedTickInterval;
            if (newAdaptiveTickInterval > 40)
            {
                newAdaptiveTickInterval -= 40;
            }

            data.adaptiveTickInterval = newAdaptiveTickInterval;
            registeredHarvesters.Add(position, data);
            var config = ModConfig.ReadConfig<Config.Config>(serverAPI, "HydrateOrDiedrateConfig.json");
            if (config != null)
            {
                data.SetRainMultiplier(config.RainMultiplier);
            }

            MarkDictionaryDirty();
        }
    }

    public void UnregisterHarvester(BlockPos position)
    {
        if (registeredHarvesters.ContainsKey(position))
        {
            registeredHarvesters.Remove(position);
            MarkDictionaryDirty();
        }
    }

    private void OnTick(float deltaTime)
    {
        globalTickCounter++;
        if (globalTickCounter > 40) globalTickCounter = 0;
        foreach (var entry in registeredHarvesters)
        {
            BlockPos position = entry.Key;
            RainHarvesterData harvesterData = entry.Value;
            float rainIntensity = harvesterData.GetRainIntensity();
            harvesterData.UpdateCalculatedTickInterval(deltaTime, serverAPI.World.Calendar.SpeedOfTime,
                serverAPI.World.Calendar.CalendarSpeedMul, rainIntensity);
            int tickIntervalDifference =
                harvesterData.calculatedTickInterval - harvesterData.previousCalculatedTickInterval;
            harvesterData.adaptiveTickInterval += tickIntervalDifference;
            if (harvesterData.adaptiveTickInterval <= globalTickCounter
                && harvesterData.previousCalculatedTickInterval > globalTickCounter)
            {
                harvesterData.adaptiveTickInterval = globalTickCounter + 2;
            }

            if (harvesterData.adaptiveTickInterval > 40)
            {
                harvesterData.adaptiveTickInterval -= 40;
            }

            if (globalTickCounter == harvesterData.adaptiveTickInterval)
            {
                int newAdaptiveTickInterval = harvesterData.adaptiveTickInterval + harvesterData.calculatedTickInterval;
                if (newAdaptiveTickInterval > 40)
                {
                    newAdaptiveTickInterval -= 40;
                }

                harvesterData.adaptiveTickInterval = newAdaptiveTickInterval;
                registeredHarvesters[position] = harvesterData;
                harvesterData.OnHarvest(rainIntensity);
            }

            harvesterData.OnParticleTickUpdate(deltaTime);
        }
    }

    private void MarkDictionaryDirty()
    {
        foreach (var data in registeredHarvesters.Values)
        {
            data.BlockEntity.MarkDirty(true);
        }
    }
}