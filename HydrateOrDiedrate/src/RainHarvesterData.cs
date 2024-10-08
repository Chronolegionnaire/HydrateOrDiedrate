using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Keg;
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
    public void SetRainMultiplier(float newMultiplier)
    {
        rainMultiplier = newMultiplier;
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
        if (adaptiveTickInterval <= 0 || adaptiveTickInterval > 40)
        {
            adaptiveTickInterval = calculatedTickInterval;
        }
        float gameSpeedMultiplier = (currentSpeedOfTime / 60f) * (currentCalendarSpeedMul / 0.5f);
        gameSpeedMultiplier = Math.Max(gameSpeedMultiplier, 1f);
        float intervalAtMaxRain = 20f;
        float intervalAtMinRain = 40f;

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
        calculatedTickInterval = Math.Clamp(calculatedTickInterval, 4, 40);
    }
    public void OnHarvest(float rainIntensity)
    {
        if (rainWaterStack == null || !IsOpenToSky(BlockEntity.Pos)) return;
        if (BlockEntity.Block is BlockKeg) return;

        float fillRate = CalculateFillRate(rainIntensity);
        rainWaterStack.StackSize = 100;
        float desiredLiters = (float)Math.Round(fillRate, 2);
        if (BlockEntity is BlockEntityBarrel barrelEntity)
        {
            var inputSlot = barrelEntity.Inventory[0];
            if (!inputSlot.Empty) return;
        }

        if (BlockEntity is BlockEntityGroundStorage groundStorage && !groundStorage.Inventory.Empty)
        {
            for (int i = 0; i < groundStorage.Inventory.Count; i++)
            {
                var slot = groundStorage.Inventory[i];
                if (slot?.Itemstack?.Collectible is BlockLiquidContainerBase blockContainer && blockContainer.IsTopOpened)
                {
                    if (slot.Itemstack.Block is BlockKeg) continue;

                    float addedAmount = blockContainer.TryPutLiquid(slot.Itemstack, rainWaterStack, desiredLiters);
                    if (addedAmount > 0) groundStorage.MarkDirty(true);
                }
            }
        }
        else if (BlockEntity.Block is BlockLiquidContainerBase blockContainerBase)
        {
            if (BlockEntity.Block is BlockKeg) return;

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

        return 0.2f * rainIntensity * gameSpeedMultiplier * rainMultiplier;
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
        if (BlockEntity.Block is BlockKeg) return;

        float rainIntensity = GetRainIntensity();
        if (rainIntensity > 0)
        {
            if (BlockEntity is BlockEntityGroundStorage groundStorage)
            {
                var positions = GetGroundStoragePositions(groundStorage);
                foreach (var pos in positions)
                {
                    if (BlockEntity.Block is BlockKeg) continue;

                    SpawnRainParticles(pos, rainIntensity);
                }
            }
            else
            {
                var adjustedPos = GetAdjustedPosition(BlockEntity);
                if (BlockEntity.Block is BlockKeg) return;

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
        particles.MinPos.Set(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
        particles.AddPos.Set(addPos, 0.0, addPos);
        particles.GravityEffect = gravityEffect;
        particles.MinVelocity.Set(velocity);
        particles.AddVelocity.Set(0.2f, velocity.Y, 0.2f);

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
            if (!groundStorage.Inventory[i].Empty)
            {
                positions.Add(GetItemPositionInStorage(groundStorage, i));
            }
        }
        return positions;
    }
    private Vec3d GetItemPositionInStorage(BlockEntityGroundStorage groundStorage, int slotIndex)
    {
        Vec3d basePos = BlockEntity.Pos.ToVec3d();
        Vec3d[] quadrantOffsets =
        {
            new Vec3d(0.2, 0.5, 0.2),
            new Vec3d(0.2, 0.5, 0.8),
            new Vec3d(0.8, 0.5, 0.2),
            new Vec3d(0.8, 0.5, 0.8)
        };
        if (slotIndex < 0 || slotIndex >= quadrantOffsets.Length)
        {
            if (harvesterManager != null)
            {
                harvesterManager.UnregisterHarvester(BlockEntity.Pos);
            }
            return null;
        }
        float meshAngle = groundStorage.MeshAngle;
        float tolerance = 0.01f;
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
                adjustedOffset = RecalculateMeshOffsets(slotIndex, 0, 2, 3, 1);
            }
            else if (Math.Abs(meshAngle - 3.1415927f) < tolerance ||
                     Math.Abs(meshAngle + 3.1415927f) < tolerance)
            {
                adjustedOffset = RecalculateMeshOffsets(slotIndex, 3, 2, 1, 0);
            }
            else if (Math.Abs(meshAngle - 1.5707964f) < tolerance)
            {
                adjustedOffset = RecalculateMeshOffsets(slotIndex, 1, 3, 0, 2);
            }
        }
        return basePos.AddCopy(adjustedOffset);
    }
    private Vec3d RecalculateMeshOffsets(int slotIndex, int val0, int val1, int val2, int val3)
    {
        Vec3d[] quadrantOffsets =
        {
            new Vec3d(0.2, 0.5, 0.2),
            new Vec3d(0.2, 0.5, 0.8),
            new Vec3d(0.8, 0.5, 0.2),
            new Vec3d(0.8, 0.5, 0.8)
        };
        switch (slotIndex)
        {
            case 0: return quadrantOffsets[val0];
            case 1: return quadrantOffsets[val1];
            case 2: return quadrantOffsets[val2];
            case 3: return quadrantOffsets[val3];
            default: return quadrantOffsets[0];
        }
    }
    private Vec3d GetAdjustedPosition(BlockEntity blockEntity)
    {
        return blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5);
    }
}