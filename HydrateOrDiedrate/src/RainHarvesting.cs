using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Configuration;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.EntityBehavior
{
    public class RainHarvester : BlockEntityBehavior
    {
        private WeatherSystemServer weatherSysServer;
        private Vec3d positionBuffer;
        private long combinedListenerHandle;
        private ItemStack rainWaterStack;
        private static SimpleParticleProperties rainParticlesBlue;
        private int particleTickInterval = 200;
        private bool initialized = false;
        private bool hasHarvestedSuccessfully = false;
        private int tickCounter = 0;
        private int particleTickCounter = 0;
        private const float DefaultSpeedOfTime = 60f;
        private const float DefaultCalendarSpeedMul = 0.5f;
        private int calculatedTickInterval = 20000;
        private bool enableRainGathering;
        private float rainMultiplier;

        public RainHarvester(BlockEntity blockentity) : base(blockentity)
        {
            positionBuffer = new Vec3d(0.5, 0.5, 0.5);
            TryUpdatePosition(blockentity);
        }
        private bool IsBlockEntityValid()
        {
            if (Blockentity == null || Blockentity.Api == null || Blockentity.Block == null || Blockentity.Block.Id == 0)
            {
                StopAllBehaviors();
                return false;
            }
            return true;
        }
        
        private void StopAllBehaviors()
        {
            if (combinedListenerHandle != 0)
            {
                Api.Logger.Notification($"RainHarvester: Unregistering combinedListenerHandle for BlockEntity at {Blockentity.Pos} with handle {combinedListenerHandle}");
                Api.Event.UnregisterGameTickListener(combinedListenerHandle);
                combinedListenerHandle = 0;
            }

            initialized = false;
        }

        private void TryUpdatePosition(BlockEntity blockentity)
        {
            if (blockentity?.Pos != null)
            {
                positionBuffer.Set(blockentity.Pos.X + 0.5, blockentity.Pos.Y + 0.5, blockentity.Pos.Z + 0.5);
            }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            var config = ModConfig.ReadConfig<Config>(api, "HydrateOrDiedrateConfig.json");
            if (config == null)
            {
                config = new Config();
            }
            enableRainGathering = config.EnableRainGathering;
            rainMultiplier = config.RainMultiplier;

            if (enableRainGathering)
            {
                api.Event.RegisterCallback((dt) =>
                {
                    InitializeBehavior(api);
                }, 1000);
            }
        }
        private void InitializeBehavior(ICoreAPI api)
        {
            if (initialized) return;

            Block blockAtPosition = api.World.BlockAccessor.GetBlock(Blockentity.Pos);
            if (blockAtPosition == null || blockAtPosition.BlockId != Blockentity.Block.BlockId)
            {
                return;
            }
            TryUpdatePosition(Blockentity);

            var item = api.World.GetItem(new AssetLocation("hydrateordiedrate:rainwaterportion"));
            if (item != null)
            {
                rainWaterStack = new ItemStack(item);
            }
            else
            {
                return;
            }

            rainParticlesBlue = CreateParticleProperties(
                ColorUtil.ColorFromRgba(255, 200, 150, 255),
                new Vec3f(0, 0, 0),
                new Vec3f(0, 0, 0)
            );

            if (api.Side == EnumAppSide.Server)
            {
                weatherSysServer = api.ModLoader.GetModSystem<WeatherSystemServer>();

                if (weatherSysServer != null)
                {
                    // Combine the listeners into a single handler
                    combinedListenerHandle = api.Event.RegisterGameTickListener(CombinedTickUpdate, 50);

                    // Logging to identify the unique listener
                    api.Logger.Notification($"RainHarvester: Registered combinedListenerHandle for BlockEntity at {Blockentity.Pos} with handle {combinedListenerHandle}");
                }

                Blockentity.MarkDirty(true);
            }

            initialized = true;
        }
        
        private void CombinedTickUpdate(float deltaTime)
        {
            if (!IsBlockEntityValid()) return;

            // Update the calculated tick interval
            UpdateCalculatedTickInterval(deltaTime);

            // Particle ticking logic
            if (particleTickCounter >= particleTickInterval / 50)
            {
                OnParticleTickUpdate(deltaTime);
                particleTickCounter = 0; // Reset the counter for the next particle tick interval
            }

            // Server tick (rain harvesting) logic
            if (tickCounter >= calculatedTickInterval / 50)
            {
                tickCounter = 0;  // Reset the counter for the next harvesting interval
                float fillRate = CalculateFillRate(GetRainIntensity());
                HarvestRainwater(Blockentity, fillRate);
            }

            particleTickCounter++;
            tickCounter++;
        }


        public override void OnBlockRemoved()
        {
            StopAllBehaviors(); 
            base.OnBlockRemoved();
        }

        private SimpleParticleProperties CreateParticleProperties(int color, Vec3f minVelocity, Vec3f maxVelocity, string climateColorMap = null)
        {
            var particles = new SimpleParticleProperties(
                1, 1, color, new Vec3d(), new Vec3d(),
                minVelocity, maxVelocity, 0.05f, 0.05f, 0.25f, 0.5f, EnumParticleModel.Cube
            );
            particles.AddPos = new Vec3d(0.125 / 2, 2 / 16f, 0.125 / 2);
            particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f);
            particles.AddQuantity = 1;
            particles.ShouldDieInLiquid = true;
            particles.ShouldSwimOnLiquid = true;
            particles.GravityEffect = 1.5f;
            particles.LifeLength = 0.15f;

            if (climateColorMap != null)
            {
                particles.ClimateColorMap = climateColorMap;
            }

            return particles;
        }

        private void UpdateCalculatedTickInterval(float deltaTime)
        {
            float currentSpeedOfTime = Blockentity.Api.World.Calendar.SpeedOfTime;
            float currentCalendarSpeedMul = Blockentity.Api.World.Calendar.CalendarSpeedMul;
            float speedOfTimeRatio = currentSpeedOfTime / DefaultSpeedOfTime;
            float calendarSpeedRatio = currentCalendarSpeedMul / DefaultCalendarSpeedMul;
            float gameSpeedMultiplier = speedOfTimeRatio * calendarSpeedRatio;
            gameSpeedMultiplier = (float)Math.Pow(gameSpeedMultiplier, 8.0);
            if (gameSpeedMultiplier < 1f) gameSpeedMultiplier = 1f;

            float rainIntensity = GetRainIntensity();

            if (rainIntensity > 0)
            {
                // Desired tick intervals in milliseconds
                float tickIntervalAtMaxRain = 10000f;  // 10 seconds at rainIntensity = 1
                float tickIntervalAtMinRain = 20000f;  // 20 seconds at rainIntensity = 0.1

                // Linear interpolation between the two based on rain intensity
                float tickInterval = tickIntervalAtMinRain - (rainIntensity * (tickIntervalAtMinRain - tickIntervalAtMaxRain));

                // Apply game speed multiplier
                tickInterval /= gameSpeedMultiplier;

                // Round to nearest 100ms
                calculatedTickInterval = (int)(Math.Round(tickInterval / 100f) * 100f);
            }
            else
            {
                // If no rain, set a default high interval
                calculatedTickInterval = 30000;  // e.g., 30 seconds if no rain
            }
        }

        private void OnTickUpdateServer(float deltaTime)
        {
            if (!IsBlockEntityValid()) return;

            tickCounter++;
            if (tickCounter >= calculatedTickInterval / 50)
            {
                tickCounter = 0;
                float fillRate = CalculateFillRate(GetRainIntensity());
                HarvestRainwater(Blockentity, fillRate);
            }
        }

        private void OnParticleTickUpdate(float deltaTime)
        {
            if (!IsBlockEntityValid()) return;

            if (!IsBlockOpenToSky(Blockentity.Pos)) return;

            if (weatherSysServer == null) return;

            float rainIntensity = GetRainIntensity();
            if (rainIntensity > 0)
            {
                if (Blockentity is BlockEntityGroundStorage groundStorage)
                {
                    var positions = GetGroundStoragePositions(groundStorage);
                    foreach (var pos in positions)
                    {
                        SpawnRainParticles(pos, rainIntensity);
                    }
                }
                else
                {
                    var adjustedPos = GetAdjustedPosition(Blockentity);
                    SpawnRainParticles(adjustedPos, rainIntensity);
                }
            }
        }

        private float GetRainIntensity()
        {
            return weatherSysServer.GetPrecipitation(positionBuffer);
        }

        private float CalculateFillRate(float rainIntensity)
        {
            return rainIntensity * 0.1f * rainMultiplier;
        }

        private void HarvestRainwater(BlockEntity blockEntity, float fillRate)
        {
            if (rainWaterStack == null || !IsBlockOpenToSky(blockEntity.Pos)) return;

            rainWaterStack.StackSize = 100;
            bool collected = false;
            float desiredLiters = (float)Math.Round(fillRate, 2);
            float desiredFillAmount = desiredLiters * 100;

            if (blockEntity is BlockEntityGroundStorage groundStorage && !groundStorage.Inventory.Empty)
            {
                for (int i = 0; i < groundStorage.Inventory.Count; i++)
                {
                    var slot = groundStorage.Inventory[i];
                    if (slot != null && i >= 0 && i < groundStorage.Inventory.Count && !slot.Empty &&
                        slot.Itemstack != null &&
                        slot.Itemstack.Collectible is BlockLiquidContainerBase blockContainer &&
                        blockContainer.IsTopOpened)
                    {
                        float addedAmount = blockContainer.TryPutLiquid(slot.Itemstack, rainWaterStack, desiredLiters);

                        if (addedAmount > 0)
                        {
                            collected = true;
                            hasHarvestedSuccessfully = true;
                        }
                    }
                }

                groundStorage.MarkDirty(true);
            }

            if (blockEntity?.Block is BlockLiquidContainerBase blockContainerBase && rainWaterStack != null)
            {
                float addedAmount = blockContainerBase.TryPutLiquid(blockEntity.Pos, rainWaterStack, desiredLiters);

                if (addedAmount > 0)
                {
                    collected = true;
                    hasHarvestedSuccessfully = true;
                }
            }

            if (!collected)
            {
                hasHarvestedSuccessfully = false;
            }
        }

        private bool IsBlockOpenToSky(BlockPos pos)
        {
            return Api.World.BlockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y;
        }

        private void SpawnRainParticles(Vec3d pos, float rainIntensity)
        {
            int baseQuantity = 10;
            int quantity = (int)(baseQuantity * (rainIntensity / 2));
            Vec3d lowerPosition = pos.AddCopy(-0.10, -0.6, -0.1);
            Vec3d spawnAreaRadius = new Vec3d(0.3, 0, 0.3);
            Vec3f upwardsVelocity = new Vec3f(0, 1.8f, 0);
            SetParticleProperties(rainParticlesBlue, lowerPosition, 0.6, quantity, 1.5f, upwardsVelocity);
        }

        private void SetParticleProperties(SimpleParticleProperties particles, Vec3d pos, double addPos, int quantity, float gravityEffect, Vec3f velocity)
        {
            particles.MinPos.Set(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
            particles.AddPos.Set(addPos, 0.0, addPos);
            particles.GravityEffect = gravityEffect;
            particles.MinVelocity.Set(velocity);
            particles.AddVelocity.Set(0.2f, velocity.Y, 0.2f);

            for (int i = 0; i < quantity; i++)
            {
                Api.World.SpawnParticles(particles, null);
            }
        }

        private Vec3d GetAdjustedPosition(BlockEntity blockEntity)
        {
            Vec3d adjustedPos = blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5);

            if (blockEntity.Block.Shape?.rotateY != 0 || blockEntity.Block.Shape?.rotateX != 0 || blockEntity.Block.Shape?.rotateZ != 0)
            {
                adjustedPos.X += blockEntity.Block.Shape.offsetX;
                adjustedPos.Y += blockEntity.Block.Shape.offsetY;
                adjustedPos.Z += blockEntity.Block.Shape.offsetZ;
            }

            return adjustedPos;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
        }

        private List<Vec3d> GetGroundStoragePositions(BlockEntityGroundStorage groundStorage)
        {
            List<Vec3d> positions = new List<Vec3d>();
            Vec3d basePos = Blockentity.Pos.ToVec3d();
            for (int i = 0; i < groundStorage.Inventory.Count; i++)
            {
                var slot = groundStorage.Inventory[i];

                if (!slot.Empty)
                {
                    Vec3d itemPosition = GetItemPositionInStorage(groundStorage, i);
                    positions.Add(itemPosition);
                }
            }

            return positions;
        }

        private Vec3d GetItemPositionInStorage(BlockEntityGroundStorage groundStorage, int slotIndex)
        {
            Vec3d basePos = Blockentity.Pos.ToVec3d();
            Vec3d[] quadrantOffsets =
            {
                new Vec3d(0.2, 0.5, 0.2),
                new Vec3d(0.2, 0.5, 0.8),
                new Vec3d(0.8, 0.5, 0.2),
                new Vec3d(0.8, 0.5, 0.8)
            };
            float meshAngle = groundStorage.MeshAngle;
            float tolerance = 0.0001f;
            Vec3d adjustedOffset = quadrantOffsets[slotIndex];
            if (groundStorage.StorageProps.Layout == EnumGroundStorageLayout.SingleCenter)
            {
                adjustedOffset = new Vec3d(0.5, 0.5, 0.5);
            }
            else
            {
                if (Math.Abs(meshAngle) < tolerance)
                {
                    adjustedOffset = quadrantOffsets[slotIndex];
                }
                else if (Math.Abs(meshAngle + 1.5707964f) < tolerance)
                {
                    switch (slotIndex)
                    {
                        case 0:
                            adjustedOffset = quadrantOffsets[2];
                            break;
                        case 1:
                            adjustedOffset = quadrantOffsets[0];
                            break;
                        case 2:
                            adjustedOffset = quadrantOffsets[3];
                            break;
                        case 3:
                            adjustedOffset = quadrantOffsets[1];
                            break;
                    }
                }
                else if (Math.Abs(meshAngle - 3.1415927f) < tolerance ||
                         Math.Abs(meshAngle + 3.1415927f) < tolerance)
                {
                    switch (slotIndex)
                    {
                        case 0:
                            adjustedOffset = quadrantOffsets[3];
                            break;
                        case 1:
                            adjustedOffset = quadrantOffsets[2];
                            break;
                        case 2:
                            adjustedOffset = quadrantOffsets[1];
                            break;
                        case 3:
                            adjustedOffset = quadrantOffsets[0];
                            break;
                    }
                }
                else if (Math.Abs(meshAngle - 1.5707964f) < tolerance)
                {
                    switch (slotIndex)
                    {
                        case 0:
                            adjustedOffset = quadrantOffsets[1];
                            break;
                        case 1:
                            adjustedOffset = quadrantOffsets[3];
                            break;
                        case 2:
                            adjustedOffset = quadrantOffsets[0];
                            break;
                        case 3:
                            adjustedOffset = quadrantOffsets[2];
                            break;
                    }
                }
            }

            Vec3d position = basePos.AddCopy(adjustedOffset);
            return position;
        }
    }
}

