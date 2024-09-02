using System;
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
        private long tickListenerHandle;
        private long particleTickListenerHandle;
        private ItemStack rainWaterStack;
        private static SimpleParticleProperties rainParticlesBlue;
        private static SimpleParticleProperties rainParticlesWhite;
        private int currentTickInterval = 10000;
        private int particleTickInterval = 200;
        private bool initialized = false;
        private bool hasHarvestedSuccessfully = false;

        public RainHarvester(BlockEntity blockentity) : base(blockentity)
        {
            positionBuffer = new Vec3d(0.5, 0.5, 0.5);
            TryUpdatePosition(blockentity);
        }

        private void TryUpdatePosition(BlockEntity blockentity)
        {
            if (blockentity?.Pos != null)
            {
                positionBuffer.Set(blockentity.Pos.X + 0.5, blockentity.Pos.Y + 0.5, blockentity.Pos.Z + 0.5);
            }
            else
            {
                blockentity.Api?.World.Logger.Warning("RainHarvester: Block entity position is null during construction. Will use default position until corrected.");
            }
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            api.Event.RegisterCallback((dt) =>
            {
                InitializeBehavior(api);
            }, 1000);
        }

        private void InitializeBehavior(ICoreAPI api)
        {
            if (initialized) return;

            try
            {
                TryUpdatePosition(Blockentity);

                var item = api.World.GetItem(new AssetLocation("hydrateordiedrate:rainwaterportion"));
                if (item != null)
                {
                    rainWaterStack = new ItemStack(item);
                }
                else
                {
                    api.World.Logger.Error("RainHarvester: Could not find item 'hydrateordiedrate:rainwaterportion'. Initialization failed.");
                    return;
                }

                // Create particles using the same approach as WaterInteractionHandler
                rainParticlesBlue = CreateParticleProperties(
                    ColorUtil.ColorFromRgba(255, 200, 150, 255), // Pure blue
                    new Vec3f(0, 0, 0), // No velocity
                    new Vec3f(0, 0, 0)  // No velocity
                );
                rainParticlesWhite = CreateParticleProperties(
                    ColorUtil.ColorFromRgba(255, 255, 255, 255),
                    new Vec3f(-0.05f, 0, -0.05f),
                    new Vec3f(0.05f, 0.1f, 0.05f)
                );

                if (api.Side == EnumAppSide.Server)
                {
                    weatherSysServer = api.ModLoader.GetModSystem<WeatherSystemServer>();

                    if (weatherSysServer != null)
                    {
                        tickListenerHandle = api.Event.RegisterGameTickListener(OnTickUpdateServer, currentTickInterval);

                        // Immediate harvest attempt after initialization
                        float rainIntensity = GetRainIntensity();
                        if (rainIntensity > 0)
                        {
                            float fillRate = CalculateFillRate(rainIntensity);
                            HarvestRainwater(Blockentity, fillRate);
                        }

                        api.World.Logger.Event("RainHarvester: Successfully initialized WeatherSystemServer.");
                    }
                    else
                    {
                        api.World.Logger.Error("RainHarvester: WeatherSystemServer not found during initialization.");
                    }

                    Blockentity.MarkDirty(true);
                }

                initialized = true;
            }
            catch (Exception ex)
            {
                api.World.Logger.Error("Error initializing RainHarvester: {0}", ex.Message);
                throw;
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            Blockentity.UnregisterGameTickListener(tickListenerHandle);
            Blockentity.UnregisterGameTickListener(particleTickListenerHandle);
        }

        private SimpleParticleProperties CreateParticleProperties(int color, Vec3f minVelocity, Vec3f maxVelocity, string climateColorMap = null)
        {
            var particles = new SimpleParticleProperties(
                1, 1, color, new Vec3d(), new Vec3d(),
                minVelocity, maxVelocity, 0.05f, 0.05f, 0.25f, 0.5f, EnumParticleModel.Cube // Adjusted sizes here
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

        private void OnTickUpdateServer(float deltaTime)
        {
            if (weatherSysServer == null) return;

            float rainIntensity = GetRainIntensity();

            if (rainIntensity > 0)
            {
                float fillRate = CalculateFillRate(rainIntensity);
                HarvestRainwater(Blockentity, fillRate);

                // Calculate tick interval based on rain intensity
                float tickInterval = 20000 - ((rainIntensity - 0.1f) * 11111.11f);

                // Round to the nearest 100 and cast to float
                tickInterval = (float)Math.Round(tickInterval / 100f) * 100f;

                UpdateTickInterval((int)tickInterval);
            }
            else
            {
                UpdateTickInterval(30000);
            }
        }

        private void OnParticleTickUpdate(float deltaTime)
        {
            if (weatherSysServer == null) return;

            float rainIntensity = GetRainIntensity();
            if (rainIntensity > 0)
            {
                var adjustedPos = GetAdjustedPosition(Blockentity);
                SpawnRainParticles(adjustedPos, rainIntensity);
            }
        }

        private float GetRainIntensity()
        {
            return weatherSysServer.GetPrecipitation(positionBuffer);
        }

        private float CalculateFillRate(float rainIntensity)
        {
            return rainIntensity * 0.1f;
        }

        private void HarvestRainwater(BlockEntity blockEntity, float rainIntensity)
        {
            if (rainWaterStack == null)
            {
                Api.World.Logger.Error("RainHarvester: rainWaterStack is null. Aborting rainwater harvest.");
                StopParticleTicking();
                return;
            }

            // Set the stack size to 100 items (1 liter)
            rainWaterStack.StackSize = 100;

            bool collected = false;

            // Calculate the desired liters based on rain intensity and round to the nearest hundredth
            float desiredLiters = (float)Math.Round(rainIntensity, 2); // Round to 2 decimal places
            float desiredFillAmount = desiredLiters * 100; // Convert to water items (100 items = 1 liter)

            Api.World.Logger.Notification($"RainHarvester: Calculating based on rain intensity - Attempting to add {desiredFillAmount} water items ({desiredLiters} liters).");

            if (blockEntity.Block is BlockLiquidContainerBase blockContainer)
            {
                // Use the correct TryPutLiquid signature with BlockPos
                float addedAmount = blockContainer.TryPutLiquid(blockEntity.Pos, rainWaterStack, desiredLiters);
                Api.World.Logger.Notification($"RainHarvester: blockContainer.TryPutLiquid returned {addedAmount} water items. DesiredItems: {desiredFillAmount}, AvailableItems: {rainWaterStack.StackSize}, PlaceableItems: {blockContainer.CapacityLitres * 100}");

                if (addedAmount > 0)
                {
                    collected = true;

                    Api.World.Logger.Notification($"RainHarvester: Added {addedAmount / 100.0f} liters of water to {blockEntity.Block.Code}.");

                    if (!hasHarvestedSuccessfully)
                    {
                        hasHarvestedSuccessfully = true;
                        StartParticleTicking(); // Start ticking particles on first success
                    }
                }
            }

            if (!collected)
            {
                StopParticleTicking(); // Stop ticking particles on failure
            }
        }

        private void StartParticleTicking()
        {
            if (particleTickListenerHandle == 0 && hasHarvestedSuccessfully) // Ensure ticking starts only after the first success
            {
                particleTickListenerHandle = Api.Event.RegisterGameTickListener(OnParticleTickUpdate, particleTickInterval);
            }
        }

        private void StopParticleTicking()
        {
            if (particleTickListenerHandle != 0)
            {
                Api.Event.UnregisterGameTickListener(particleTickListenerHandle);
                particleTickListenerHandle = 0; // Reset the handle to indicate it's stopped
            }
        }

        private void SpawnRainParticles(Vec3d pos, float rainIntensity)
        {
            int baseQuantity = 10; // Set the base number of particles for intensity 1
            int quantity = (int)(baseQuantity * (rainIntensity / 2));

            SetParticleProperties(rainParticlesBlue, pos, 0.4, quantity, 1.5f, new Vec3f(0, 0.8f, 0));
            SetParticleProperties(rainParticlesWhite, pos, 0.4, quantity, 1.5f, new Vec3f(0, 0.8f, 0));
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

        private void UpdateTickInterval(int newInterval)
        {
            if (newInterval != currentTickInterval)
            {
                Api.Event.UnregisterGameTickListener(tickListenerHandle);
                currentTickInterval = newInterval;
                tickListenerHandle = Api.Event.RegisterGameTickListener(OnTickUpdateServer, currentTickInterval);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
        }
    }
}
