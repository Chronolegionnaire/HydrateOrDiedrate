using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    private ConcurrentQueue<Action> taskQueue;
    private CancellationTokenSource cancellationTokenSource;
    private Thread workerThread;

    public RainHarvesterManager(ICoreServerAPI api, Config.Config config)
    {
        serverAPI = api;
        _config = config;
        activeHarvesters = new Dictionary<BlockPos, RainHarvesterData>();
        inactiveHarvesters = new Dictionary<BlockPos, RainHarvesterData>();
        
        enableParticleTicking = config.EnableParticleTicking;
        taskQueue = new ConcurrentQueue<Action>();
        cancellationTokenSource = new CancellationTokenSource();
        workerThread = new Thread(ProcessTaskQueue)
        {
            IsBackground = true
        };
        workerThread.Start();

        if (config.EnableRainGathering)
        {
            api.Event.RegisterGameTickListener(ScheduleTickProcessing, 2000);
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
            tickListenerId = serverAPI.Event.RegisterGameTickListener(ScheduleTickProcessing, 2000);
            serverAPI.Event.RegisterGameTickListener(OnInactiveHarvesterCheck, 10000);
        }
    }

    public void RegisterHarvester(BlockPos position, RainHarvesterData data)
    {
        lock (activeHarvesters)
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
    }

    public void UnregisterHarvester(BlockPos position)
    {
        lock (activeHarvesters)
        {
            activeHarvesters.Remove(position);
            inactiveHarvesters.Remove(position);
        }
    }

    public void OnBlockRemoved(BlockPos position)
    {
        UnregisterHarvester(position);
    }

    public void OnBlockUnloaded(BlockPos position)
    {
        UnregisterHarvester(position);
    }

    private void ScheduleTickProcessing(float deltaTime)
    {
        if (!serverAPI.World.AllOnlinePlayers.Any()) return;

        // Add a task to the queue for processing the active harvesters
        taskQueue.Enqueue(() => ProcessTick(deltaTime));
    }

    private void ProcessTick(float deltaTime)
    {
        globalTickCounter++;
        if (globalTickCounter > 10) globalTickCounter = 1;
        

        List<BlockPos> toRemove = new List<BlockPos>();
        
        lock (activeHarvesters)
        {
            foreach (var entry in activeHarvesters.ToList())
            {
                BlockPos position = entry.Key;
                RainHarvesterData harvesterData = entry.Value;
                float rainIntensity = harvesterData.GetRainIntensity();
                if (enableParticleTicking)
                {
                    harvesterData.OnParticleTickUpdate(deltaTime);
                }
                harvesterData.UpdateCalculatedTickInterval(deltaTime, serverAPI.World.Calendar.SpeedOfTime,
                    serverAPI.World.Calendar.CalendarSpeedMul, rainIntensity);

                int tickIntervalDifference =
                    harvesterData.calculatedTickInterval - harvesterData.previousCalculatedTickInterval;
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
                    int newAdaptiveTickInterval =
                        harvesterData.adaptiveTickInterval + harvesterData.calculatedTickInterval;
                    if (newAdaptiveTickInterval > 10)
                    {
                        newAdaptiveTickInterval -= 10;
                    }

                    harvesterData.adaptiveTickInterval = newAdaptiveTickInterval;

                    harvesterData.OnHarvest(rainIntensity);

                    if (!IsRainyAndOpenToSky(harvesterData))
                    {
                        toRemove.Add(position);
                    }
                }
            }
        }

        lock (activeHarvesters)
        {
            foreach (var pos in toRemove)
            {
                inactiveHarvesters[pos] = activeHarvesters[pos];
                activeHarvesters.Remove(pos);
            }
        }
    }

    private void OnInactiveHarvesterCheck(float deltaTime)
    {
        taskQueue.Enqueue(() =>
        {
            List<BlockPos> toActivate = new List<BlockPos>();
            
            lock (inactiveHarvesters)
            {
                foreach (var entry in inactiveHarvesters.ToList())
                {
                    BlockPos position = entry.Key;
                    RainHarvesterData harvesterData = entry.Value;

                    if (IsRainyAndOpenToSky(harvesterData))
                    {
                        toActivate.Add(position);
                    }
                }
            }
            
            lock (activeHarvesters)
            {
                foreach (var position in toActivate)
                {
                    if (inactiveHarvesters.TryGetValue(position, out RainHarvesterData harvesterData))
                    {
                        inactiveHarvesters.Remove(position);
                        activeHarvesters[position] = harvesterData;
                    }
                }
            }
        });
    }

    private void ProcessTaskQueue()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (taskQueue.TryDequeue(out var task))
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {
                    serverAPI.Logger.Error($"Error in RainHarvesterManager: {ex}");
                }
            }
            Thread.Sleep(10);
        }
    }

    private bool IsRainyAndOpenToSky(RainHarvesterData harvesterData)
    {
        return harvesterData.GetRainIntensity() > 0 && harvesterData.IsOpenToSky(harvesterData.BlockEntity.Pos);
    }
}
