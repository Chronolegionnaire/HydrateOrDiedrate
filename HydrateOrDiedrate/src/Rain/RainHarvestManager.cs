using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate
{
    public class RainHarvesterManager
    {
        private ICoreServerAPI serverAPI;
        private Config.Config _config;
        private ConcurrentDictionary<BlockPos, RainHarvesterData> activeHarvesters;
        private ConcurrentDictionary<BlockPos, RainHarvesterData> inactiveHarvesters;
        private long tickListenerId;
        private int globalTickCounter = 1;
        private bool enableParticleTicking;
        private const int PARALLEL_THRESHOLD = 20;

        public RainHarvesterManager(ICoreServerAPI api, Config.Config config)
        {
            serverAPI = api;
            _config = config;
            activeHarvesters = new ConcurrentDictionary<BlockPos, RainHarvesterData>();
            inactiveHarvesters = new ConcurrentDictionary<BlockPos, RainHarvesterData>();

            enableParticleTicking = config.EnableParticleTicking;

            if (config.EnableRainGathering)
            {
                tickListenerId = api.Event.RegisterGameTickListener(ScheduleTickProcessing, 2000);
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
            float rainIntensity = data.GetRainIntensity();
            if (IsRainyAndOpenToSky(data, rainIntensity))
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
            activeHarvesters.TryRemove(position, out _);
            inactiveHarvesters.TryRemove(position, out _);
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
            Task.Run(() => ProcessTick(deltaTime));
        }

        private void ProcessTick(float deltaTime)
        {
            globalTickCounter = (globalTickCounter % 10) + 1;
            var toRemove = new ConcurrentBag<BlockPos>();

            float speedOfTime = serverAPI.World.Calendar.SpeedOfTime;
            float calendarSpeedMul = serverAPI.World.Calendar.CalendarSpeedMul;
            if (activeHarvesters.Count > PARALLEL_THRESHOLD)
            {
                Parallel.ForEach(activeHarvesters.ToArray(), kvp =>
                {
                    ProcessHarvesterTick(kvp, deltaTime, speedOfTime, calendarSpeedMul, toRemove);
                });
            }
            else
            {
                foreach (var kvp in activeHarvesters.ToArray())
                {
                    ProcessHarvesterTick(kvp, deltaTime, speedOfTime, calendarSpeedMul, toRemove);
                }
            }

            foreach (var pos in toRemove)
            {
                if (activeHarvesters.TryRemove(pos, out var removedHarvester))
                {
                    inactiveHarvesters[pos] = removedHarvester;
                }
            }
        }
        private void ProcessHarvesterTick(KeyValuePair<BlockPos, RainHarvesterData> kvp, float deltaTime, float speedOfTime, float calendarSpeedMul, ConcurrentBag<BlockPos> toRemove)
        {
            BlockPos position = kvp.Key;
            RainHarvesterData harvesterData = kvp.Value;
            float rainIntensity = harvesterData.GetRainIntensity();

            if (enableParticleTicking)
            {
                harvesterData.OnParticleTickUpdate(deltaTime);
            }

            harvesterData.UpdateCalculatedTickInterval(deltaTime, speedOfTime, calendarSpeedMul, rainIntensity);
            int tickIntervalDifference = harvesterData.calculatedTickInterval - harvesterData.previousCalculatedTickInterval;
            harvesterData.adaptiveTickInterval += tickIntervalDifference;

            if (harvesterData.adaptiveTickInterval <= globalTickCounter &&
                harvesterData.previousCalculatedTickInterval > globalTickCounter)
            {
                harvesterData.adaptiveTickInterval = globalTickCounter + 2;
            }

            if (harvesterData.adaptiveTickInterval > 10)
            {
                harvesterData.adaptiveTickInterval -= 9;
            }

            if (globalTickCounter == harvesterData.adaptiveTickInterval)
            {
                int newAdaptiveTickInterval = harvesterData.adaptiveTickInterval + harvesterData.calculatedTickInterval;
                if (newAdaptiveTickInterval > 10)
                {
                    newAdaptiveTickInterval -= 10;
                }

                harvesterData.adaptiveTickInterval = newAdaptiveTickInterval;
                harvesterData.OnHarvest(rainIntensity);

                if (!IsRainyAndOpenToSky(harvesterData, rainIntensity))
                {
                    toRemove.Add(position);
                }
            }
        }
        private void OnInactiveHarvesterCheck(float deltaTime)
        {
            Task.Run(() =>
            {
                var toActivate = new List<BlockPos>();
                foreach (var kvp in inactiveHarvesters.ToArray())
                {
                    BlockPos position = kvp.Key;
                    RainHarvesterData harvesterData = kvp.Value;
                    float rainIntensity = harvesterData.GetRainIntensity();

                    if (IsRainyAndOpenToSky(harvesterData, rainIntensity))
                    {
                        toActivate.Add(position);
                    }
                }
                foreach (var position in toActivate)
                {
                    if (inactiveHarvesters.TryRemove(position, out var harvesterData))
                    {
                        activeHarvesters[position] = harvesterData;
                    }
                }
            });
        }

        private bool IsRainyAndOpenToSky(RainHarvesterData harvesterData, float rainIntensity)
        {
            return rainIntensity > 0 && harvesterData.IsOpenToSky(harvesterData.BlockEntity.Pos);
        }
    }
}
