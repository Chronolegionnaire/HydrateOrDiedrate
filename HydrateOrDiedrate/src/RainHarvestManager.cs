using System;
using System.Collections.Generic;
using System.Linq;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate;

public class RainHarvesterManager
{
    private ICoreServerAPI serverAPI;
    private Config.Config _config;
    private Dictionary<BlockPos, RainHarvesterData> activeHarvesters;
    private Dictionary<BlockPos, RainHarvesterData> inactiveHarvesters;
    private long tickListenerId;
    private int globalTickCounter = 1;
    private bool enableParticleTicking;
    public RainHarvesterManager(ICoreServerAPI api, Config.Config config)
    {
        serverAPI = api;
        _config = config;
        activeHarvesters = new Dictionary<BlockPos, RainHarvesterData>();
        inactiveHarvesters = new Dictionary<BlockPos, RainHarvesterData>();
        
        enableParticleTicking = config.EnableParticleTicking;
        if (config.EnableRainGathering)
        {
            tickListenerId = api.Event.RegisterGameTickListener(OnTick, 2000);
            api.Event.RegisterGameTickListener(OnInactiveHarvesterCheck, 10000);
        }
    }
    public void Reset(Config.Config newConfig)
    {
        _config = newConfig;
        enableParticleTicking = _config.EnableParticleTicking;
        
        serverAPI.Event.UnregisterGameTickListener(tickListenerId);
        if (_config.EnableRainGathering)
        {
            tickListenerId = serverAPI.Event.RegisterGameTickListener(OnTick, 2000);
            serverAPI.Event.RegisterGameTickListener(OnInactiveHarvesterCheck, 10000);
        }
    }

    public void RegisterHarvester(BlockPos position, RainHarvesterData data)
    {
        if (IsRainyAndOpenToSky(data))
        {
            activeHarvesters[position] = data;
        }
        else
        {
            inactiveHarvesters[position] = data;
        }
    }

    public void UnregisterHarvester(BlockPos position)
    {
        activeHarvesters.Remove(position);
        inactiveHarvesters.Remove(position);
    }

    public void OnBlockRemoved(BlockPos position)
    {
        UnregisterHarvester(position);
    }

    public void OnBlockUnloaded(BlockPos position)
    {
        UnregisterHarvester(position);
    }

    private void OnTick(float deltaTime)
    {
        if (!serverAPI.World.AllOnlinePlayers.Any())
        {
            return;
        }

        globalTickCounter++;
        if (globalTickCounter > 10) globalTickCounter = 1;

        foreach (var entry in activeHarvesters)
        {
            BlockPos position = entry.Key;
            RainHarvesterData harvesterData = entry.Value;
            float rainIntensity = harvesterData.GetRainIntensity();
            harvesterData.UpdateCalculatedTickInterval(deltaTime, serverAPI.World.Calendar.SpeedOfTime,
                serverAPI.World.Calendar.CalendarSpeedMul, rainIntensity);

            int tickIntervalDifference = harvesterData.calculatedTickInterval - harvesterData.previousCalculatedTickInterval;
            harvesterData.adaptiveTickInterval += tickIntervalDifference;

            if (harvesterData.adaptiveTickInterval <= globalTickCounter &&
                harvesterData.previousCalculatedTickInterval > globalTickCounter)
            {
                harvesterData.adaptiveTickInterval = globalTickCounter + 2;
            }

            if (harvesterData.adaptiveTickInterval > 10)
            {
                harvesterData.adaptiveTickInterval -= 10;
            }

            if (globalTickCounter == harvesterData.adaptiveTickInterval)
            {
                int newAdaptiveTickInterval = harvesterData.adaptiveTickInterval + harvesterData.calculatedTickInterval;
                if (newAdaptiveTickInterval > 10)
                {
                    newAdaptiveTickInterval -= 10;
                }

                harvesterData.adaptiveTickInterval = newAdaptiveTickInterval;
                activeHarvesters[position] = harvesterData;
                harvesterData.OnHarvest(rainIntensity);
            }

            if (enableParticleTicking)
            {
                harvesterData.OnParticleTickUpdate(deltaTime);
            }
        }
    }


    private void OnInactiveHarvesterCheck(float deltaTime)
    {
        var toActivate = new List<BlockPos>();

        foreach (var entry in inactiveHarvesters)
        {
            BlockPos position = entry.Key;
            RainHarvesterData harvesterData = entry.Value;

            if (IsRainyAndOpenToSky(harvesterData))
            {
                toActivate.Add(position);
            }
        }

        foreach (var position in toActivate)
        {
            RainHarvesterData harvesterData = inactiveHarvesters[position];
            inactiveHarvesters.Remove(position);
            activeHarvesters[position] = harvesterData;
        }
        
    }

    private bool IsRainyAndOpenToSky(RainHarvesterData harvesterData)
    {
        return harvesterData.GetRainIntensity() > 0 && harvesterData.IsOpenToSky(harvesterData.BlockEntity.Pos);
    }
}