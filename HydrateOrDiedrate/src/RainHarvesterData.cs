using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate;

public class RainHarvesterData
{
    public BlockEntity BlockEntity { get; }
    private Vec3d positionBuffer;
    private ItemStack rainWaterStack;
    private float rainMultiplier;
    public int calculatedTickInterval;
    public int adaptiveTickInterval;
    public int previousCalculatedTickInterval = 0;
    private SimpleParticleProperties rainParticlesBlue;
    private WeatherSystemServer weatherSysServer;
    private RainHarvesterManager harvesterManager;
    private double accumulatedRainWater = 0.0;
    private double lastRainHarvestDay = -1.0;
    public RainHarvesterData(BlockEntity entity, float rainMultiplier)
    {
        BlockEntity = entity;
        
        this.rainMultiplier = rainMultiplier;
        positionBuffer = new Vec3d(entity.Pos.X + 0.5, entity.Pos.Y + 0.5, entity.Pos.Z + 0.5);
        var item = entity.Api.World.GetItem(new AssetLocation("hydrateordiedrate:rainwaterportion"));
        if (item != null)
        { 
            rainWaterStack = new ItemStack(item);
        }
        InitializeParticles();
        weatherSysServer = BlockEntity.Api.ModLoader.GetModSystem<WeatherSystemServer>();
        harvesterManager = BlockEntity.Api.ModLoader.GetModSystem<HydrateOrDiedrateModSystem>().GetRainHarvesterManager();
    }

    public void UpdateCalculatedTickInterval(float deltaTime, float currentSpeedOfTime, float currentCalendarSpeedMul,
        float rainIntensity)
    {
        if (previousCalculatedTickInterval == 0)
        {
            previousCalculatedTickInterval = calculatedTickInterval;
        }
        
        if (calculatedTickInterval != previousCalculatedTickInterval)
        {
            previousCalculatedTickInterval = calculatedTickInterval;
        }
        
        if (adaptiveTickInterval <= 1 || adaptiveTickInterval > 10)
        {
            adaptiveTickInterval = calculatedTickInterval;
        }
        
        float gameSpeedMultiplier = (currentSpeedOfTime / 60f) * (currentCalendarSpeedMul / 0.5f);
        gameSpeedMultiplier = Math.Max(gameSpeedMultiplier, 1f);

        float intervalAtMaxRain = 5f;
        float intervalAtMinRain = 10f;
        
        if (rainIntensity >= 1)
        {
            calculatedTickInterval = (int)(intervalAtMaxRain / gameSpeedMultiplier);
        }
        else if (rainIntensity <= 0.1)
        {
            calculatedTickInterval = (int)(intervalAtMinRain / gameSpeedMultiplier);
        }
        else
        {
            float interpolatedInterval =
                intervalAtMinRain + (intervalAtMaxRain - intervalAtMinRain) * (rainIntensity - 0.1f) / 0.9f;
            calculatedTickInterval = (int)(interpolatedInterval / gameSpeedMultiplier);
        }
        
        calculatedTickInterval = Math.Clamp(calculatedTickInterval, 1, 10);
    }

    public void OnHarvest(float rainIntensity)
    {
        if (rainWaterStack == null || !IsOpenToSky(BlockEntity.Pos)) return;
        if (BlockEntity is BlockEntityBarrel barrel && barrel.Sealed)
        {
            return;
        }
        double currentDay = BlockEntity.Api.World.Calendar.TotalDays;
        if (lastRainHarvestDay < 0)
        {
            lastRainHarvestDay = currentDay;
            return;
        }
        double elapsedDays = currentDay - lastRainHarvestDay;
        if (elapsedDays <= 0) return;
        lastRainHarvestDay = currentDay;
        double litersThisCycle = CalculateFillRate(rainIntensity) * elapsedDays;
        accumulatedRainWater += litersThisCycle;
        if (accumulatedRainWater < 0.2) return;
        int increments = (int)Math.Floor(accumulatedRainWater / 0.2);
        float waterToAdd = increments * 0.2f;
        accumulatedRainWater -= waterToAdd;

        if (BlockEntity is BlockEntityGroundStorage groundStorage && !groundStorage.Inventory.Empty)
        {
            for (int i = 0; i < groundStorage.Inventory.Count; i++)
            {
                var slot = groundStorage.Inventory[i];
                if (slot.Empty) continue;

                string itemName = slot.Itemstack?.Collectible?.Code?.Path?.ToLower() ?? "";
                if (itemName.Contains("raw") || itemName.Contains("unfired")) continue;

                if (slot?.Itemstack?.Collectible is BlockLiquidContainerBase blockContainer &&
                    blockContainer.IsTopOpened)
                {
                    rainWaterStack.StackSize = 100;
                    float desiredLiters = waterToAdd;
                    float addedAmount = blockContainer.TryPutLiquid(slot.Itemstack, rainWaterStack, desiredLiters);
                    if (addedAmount > 0) groundStorage.MarkDirty(true);
                }
            }
        }
        else if (BlockEntity.Block is BlockLiquidContainerBase blockContainerBase)
        {
            rainWaterStack.StackSize = 100;
            float desiredLiters = waterToAdd;
            blockContainerBase.TryPutLiquid(BlockEntity.Pos, rainWaterStack, desiredLiters);
        }
    }
    public float GetRainIntensity()
    {
        return weatherSysServer?.GetPrecipitation(positionBuffer) ?? 0f;
    }
    public float CalculateFillRate(float rainIntensity)
    {
        float currentSpeedOfTime = BlockEntity.Api.World.Calendar.SpeedOfTime;
        float currentCalendarSpeedMul = BlockEntity.Api.World.Calendar.CalendarSpeedMul;
        float gameSpeedMultiplier = (currentSpeedOfTime / 60f) * (currentCalendarSpeedMul / 0.5f);

        return 2f * rainIntensity * gameSpeedMultiplier * rainMultiplier;
    }
    public bool IsOpenToSky(BlockPos pos)
    {
        return BlockEntity.Api.World.BlockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y;
    }
    private void InitializeParticles()
    {
        rainParticlesBlue = new SimpleParticleProperties(
            1, 1, ColorUtil.ColorFromRgba(255, 200, 150, 255),
            new Vec3d(), new Vec3d(),
            new Vec3f(0, 1.8f, 0), new Vec3f(0, 1.8f, 0),
            0.05f, 0.05f, 0.25f, 0.5f, EnumParticleModel.Cube);
        rainParticlesBlue.ShouldDieInLiquid = true;
        rainParticlesBlue.ShouldSwimOnLiquid = true;
        rainParticlesBlue.GravityEffect = 1.5f;
        rainParticlesBlue.LifeLength = 0.15f;
        rainParticlesBlue.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f);
    }

    public void OnParticleTickUpdate(float deltaTime)
    {
        if (!IsOpenToSky(BlockEntity.Pos)) return;
        if (weatherSysServer == null) return;

        float rainIntensity = GetRainIntensity();
        if (rainIntensity > 0)
        {
            if (BlockEntity is BlockEntityGroundStorage groundStorage)
            {
                var positions = GetGroundStoragePositions(groundStorage);
                foreach (var pos in positions)
                {

                    SpawnRainParticles(pos, rainIntensity);
                }
            }
            else
            {
                var adjustedPos = GetAdjustedPosition(BlockEntity);

                SpawnRainParticles(adjustedPos, rainIntensity);
            }
        }
    }
    private void SpawnRainParticles(Vec3d pos, float rainIntensity)
    {
        int baseQuantity = 10;
        int quantity = (int)(baseQuantity * (rainIntensity / 2));

        Vec3d lowerPosition = pos.AddCopy(-0.10, -0.6, -0.1);
        Vec3f upwardsVelocity = new Vec3f(0, 1.8f, 0);

        SetParticleProperties(rainParticlesBlue, lowerPosition, 0.6, quantity, 1.5f, upwardsVelocity);
    }
    private void SetParticleProperties(SimpleParticleProperties particles, Vec3d pos, double addPos, int quantity, float gravityEffect, Vec3f velocity)
    {
        particles.MinPos.X = pos.X - 0.2;
        particles.MinPos.Y = pos.Y + 0.1;
        particles.MinPos.Z = pos.Z - 0.2;
        
        particles.AddPos.X = addPos;
        particles.AddPos.Y = 0.0;
        particles.AddPos.Z = addPos;

        particles.GravityEffect = gravityEffect;

        particles.MinVelocity.X = velocity.X;
        particles.MinVelocity.Y = velocity.Y;
        particles.MinVelocity.Z = velocity.Z;

        particles.AddVelocity.X = 0.2f;
        particles.AddVelocity.Y = velocity.Y;
        particles.AddVelocity.Z = 0.2f;

        for (int i = 0; i < quantity; i++)
        {
            BlockEntity.Api.World.SpawnParticles(particles, null);
        }
    }
    private List<Vec3d> GetGroundStoragePositions(BlockEntityGroundStorage groundStorage)
    {
        List<Vec3d> positions = new List<Vec3d>();
        Vec3d basePos = BlockEntity.Pos.ToVec3d();

        for (int i = 0; i < groundStorage.Inventory.Count; i++)
        {
            var slot = groundStorage.Inventory[i];
            if (slot.Empty) continue;
            string itemName = slot.Itemstack?.Collectible?.Code?.Path?.ToLower() ?? "";
            if (itemName.Contains("raw") || itemName.Contains("unfired")) continue;
            Vec3d position = GetItemPositionInStorage(groundStorage, i);
            if (position != null)
            {
                positions.Add(position);
            }
        }

        return positions;
    }

    private Vec3d GetItemPositionInStorage(BlockEntityGroundStorage groundStorage, int slotIndex)
    {
        Vec3d basePos = BlockEntity.Pos.ToVec3d();
        if (slotIndex < 0 || slotIndex >= 4)
        {
            if (harvesterManager != null)
            {
                harvesterManager.UnregisterHarvester(BlockEntity.Pos);
            }

            return basePos;
        }

        float meshAngle = groundStorage.MeshAngle;
        float tolerance = 0.01f;
        Vec3d[] offsets0Degrees =
        {
            new Vec3d(0.2, 0.5, 0.2),
            new Vec3d(0.2, 0.5, 0.8),
            new Vec3d(0.8, 0.5, 0.2),
            new Vec3d(0.8, 0.5, 0.8)
        };

        Vec3d[] offsets90DegreesClockwise =
        {
            new Vec3d(0.2, 0.5, 0.8),
            new Vec3d(0.8, 0.5, 0.8),
            new Vec3d(0.2, 0.5, 0.2),
            new Vec3d(0.8, 0.5, 0.2)
        };

        Vec3d[] offsets180Degrees =
        {
            new Vec3d(0.8, 0.5, 0.8),
            new Vec3d(0.8, 0.5, 0.2),
            new Vec3d(0.2, 0.5, 0.8),
            new Vec3d(0.2, 0.5, 0.2)
        };
        Vec3d[] offsets90DegreesCounterClockwise =
        {
            new Vec3d(0.8, 0.5, 0.2),
            new Vec3d(0.2, 0.5, 0.2),
            new Vec3d(0.8, 0.5, 0.8),
            new Vec3d(0.2, 0.5, 0.8)
        };
        Vec3d adjustedOffset;
        if (groundStorage.StorageProps.Layout == EnumGroundStorageLayout.SingleCenter)
        {
            adjustedOffset = new Vec3d(0.5, 0.5, 0.5);
        }
        else
        {
            if (Math.Abs(meshAngle) < tolerance)
            {
                adjustedOffset = offsets0Degrees[slotIndex];
            }
            else if (Math.Abs(meshAngle - 1.5707964f) < tolerance)
            {
                adjustedOffset = offsets90DegreesClockwise[slotIndex];
            }
            else if (Math.Abs(meshAngle - 3.1415927f) < tolerance || Math.Abs(meshAngle + 3.1415927f) < tolerance)
            {
                adjustedOffset = offsets180Degrees[slotIndex];
            }
            else if (Math.Abs(meshAngle + 1.5707964f) < tolerance)
            {
                adjustedOffset = offsets90DegreesCounterClockwise[slotIndex];
            }
            else
            {
                adjustedOffset = offsets0Degrees[slotIndex];
            }
        }
        return basePos.AddCopy(adjustedOffset);
    }

    private Vec3d GetAdjustedPosition(BlockEntity blockEntity)
    {
        return blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5);
    }
}