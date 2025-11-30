using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Thirst;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather;

public partial class EntityBehaviorBodyTemperatureHot(Entity entity) : EntityBehavior(entity), IThirstRateModifier
{
    public override string PropertyName() => "bodytemperaturehot";

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);
        InitBodyHeatAttributes();
    }

    public float OnThirstRateCalculate(float currentModifier)
    {
        if (!ModConfig.Instance.HeatAndCooling.HarshHeat) return currentModifier;
        UpdateCoolingFactor();

        var config = ModConfig.Instance.HeatAndCooling;

        var temperature = entity.World.BlockAccessor
            .GetClimateAt(entity.ServerPos.AsBlockPos, EnumGetClimateMode.NowValues)?
            .Temperature.GuardFinite() ?? 0f;

        float threshold = config.TemperatureThreshold;
        if (temperature > threshold)
        {
            float rawTempDiff = temperature - threshold;

            float tempOffsetPerCooling = config.CoolingTempOffsetPerPoint;
            float effectiveTempDiff = rawTempDiff - (Cooling * tempOffsetPerCooling);

            if (effectiveTempDiff <= 0f)
            {
                return currentModifier;
            }
            float expArgument = config.HarshHeatExponentialGainMultiplier * effectiveTempDiff;
            float heatIncrease = config.ThirstIncreasePerDegreeMultiplier * ((float)Math.Exp(expArgument) - 1f);
            heatIncrease = Util.GuardFinite(heatIncrease);

            currentModifier += heatIncrease;
        }
        if (Cooling < 0f)
        {
            float negCoolingMult = GetNegativeCoolingThirstMultiplier();
            currentModifier *= negCoolingMult;
        }
        float minThirst = ModConfig.Instance.Thirst.ThirstDecayRate;
        if (currentModifier < minThirst)
        {
            currentModifier = minThirst;
        }

        return currentModifier;
    }

    public void UpdateCoolingFactor()
    {
        if (entity is not EntityAgent entityAgent) return;
        UpdateRoomInfo();

        HasWetnessCoolingBonus = false;
        HasRoomCoolingBonus = false;
        HasLowSunlightCoolingBonus = false;
        HasShadeCoolingBonus = false;

        var behaviorContainer = entityAgent.GetBehavior<EntityBehaviorContainer>();
        if (behaviorContainer is null || behaviorContainer.Inventory is null) return;

        var inventory = behaviorContainer.Inventory;

        int unequippedSlots = 0;
        float finalCooling = 0f;
        float gearCooling = 0f;

        for (int i = 0; i < inventory.Count; i++)
        {
            var slot = inventory[i];

            if (slot?.Itemstack is null)
            {
                if (i == 0 || i == 1 || i == 2 || i == 11 || i == 3 || i == 4 || i == 8 || i == 5)
                {
                    unequippedSlots++;
                }

                continue;
            }

            var cooling = CustomItemWearableExtensions.GetCooling(slot, entity.World.Api);

            cooling = (float)Math.Round(cooling, 1, MidpointRounding.AwayFromZero);

            gearCooling += cooling;
            finalCooling += cooling;
        }

        var config = ModConfig.Instance.HeatAndCooling;

        float nakedCooling = unequippedSlots * config.UnequippedSlotCooling;

        gearCooling += nakedCooling;
        finalCooling += nakedCooling;

        GearCooling = Util.GuardFinite(gearCooling);
        finalCooling = Util.GuardFinite(finalCooling);

        if (entity.WatchedAttributes.GetFloat("wetness", 0f) > 0f)
        {
            finalCooling += config.WetnessCoolingBonus;
            HasWetnessCoolingBonus = true;
        }

        if (inEnclosedRoom)
        {
            finalCooling += config.RoomCoolingBonus;
            HasRoomCoolingBonus = true;
        }

        finalCooling -= nearbyHeatSourcesStrength * 0.5f;

        
        int sunlightLevel = entity.World.BlockAccessor.GetLightLevel(
            entity.SidedPos.AsBlockPos,
            EnumLightLevelType.TimeOfDaySunLight
        );
        sunlightLevel = GameMath.Clamp(sunlightLevel, 0, 22);

        int threshold = config.LowSunlightThreshold;

        if (sunlightLevel < threshold)
        {
            float darknessFraction = (float)(threshold - sunlightLevel) / threshold;
            darknessFraction = GameMath.Clamp(darknessFraction, 0f, 1f);
            float sunlightCoolingBonus = config.LowSunlightCoolingBonus * darknessFraction;
            finalCooling += sunlightCoolingBonus;

            if (sunlightCoolingBonus > 0f)
            {
                HasLowSunlightCoolingBonus = true;
            }
        }

        bool hasDirectSun = HasDirectSunlight();
        if (!hasDirectSun)
        {
            finalCooling += config.ShadeCoolingBonus;
            HasShadeCoolingBonus = true;
        }

        float coolingMul = entity.Stats.GetBlended(HoDStats.CoolingMul);
        if (!float.IsFinite(coolingMul) || coolingMul <= 0f)
        {
            coolingMul = 1f;
        }

        coolingMul = GameMath.Clamp(coolingMul, 0.01f, 100f);

        float totalCoolingMult = CoolingMultiplier * coolingMul;

        finalCooling = ApplyBidirectionalMultiplier(finalCooling, totalCoolingMult);

        Cooling = GameMath.Clamp(Util.GuardFinite(finalCooling), -100f, 100f);
        SyncCoolingToWatchedAttributes();
    }

    private static float ApplyBidirectionalMultiplier(float value, float factor)
    {
        if (factor <= 0f || value == 0f) return value;
        if (value > 0f) return value * factor;
        return value / factor;
    }
    
    private float GetNegativeCoolingThirstMultiplier()
    {
        if (Cooling >= 0f) return 1f;

        var config = ModConfig.Instance.HeatAndCooling;
        float magnitude = -Cooling;
        float linearPerPoint = config.NegativeCoolingThirstLinearPerPoint;
        float maxMult       = config.NegativeCoolingThirstMaxMultiplier; 

        float mult = 1f + magnitude * linearPerPoint;
        mult = GameMath.Clamp(Util.GuardFinite(mult), 1f, maxMult);

        return mult;
    }
    
    private bool HasDirectSunlight()
    {
        if (entity.World?.Calendar == null || entity is not EntityAgent agent)
            return false;

        var world = entity.World;
        var calendar = world.Calendar;

        Vec3d fromPos = agent.SidedPos.XYZ.Clone();
        fromPos.Y += agent.LocalEyePos.Y;

        Vec3f sunDirF = calendar.GetSunPosition(fromPos, calendar.TotalDays);
        var sunDir = new Vec3d(sunDirF.X, sunDirF.Y, sunDirF.Z);
        if (sunDir.Y <= 0)
            return false;

        const double maxDist = 512;
        Vec3d toPos = fromPos.AddCopy(
            sunDir.X * maxDist,
            sunDir.Y * maxDist,
            sunDir.Z * maxDist
        );

        BlockSelection blockSel = null;
        EntitySelection entitySel = null;

        BlockFilter sunBlockFilter = (BlockPos pos, Block block) =>
        {
            if (block == null) return false;
            if (block.BlockMaterial == EnumBlockMaterial.Glass ||
                block.BlockMaterial == EnumBlockMaterial.Liquid ||
                block.BlockMaterial == EnumBlockMaterial.Ice)
            {
                return false;
            }
            if (block.BlockMaterial == EnumBlockMaterial.Leaves)
                return true;
            return block.LightAbsorption > 0;
        };

        world.RayTraceForSelection(
            fromPos,
            toPos,
            ref blockSel,
            ref entitySel,
            sunBlockFilter,
            null
        );
        return blockSel == null;
    }

    private bool inEnclosedRoom;
    private float nearbyHeatSourcesStrength;
    private bool inGreenhouse;
    private void UpdateRoomInfo()
    {
        var roomRegistry = entity.Api.ModLoader.GetModSystem<RoomRegistry>();
        if (roomRegistry is null) return;

        var tempPos = entity.Pos.AsBlockPos;
        Room room = roomRegistry.GetRoomForPosition(tempPos);

        inEnclosedRoom = false;
        inGreenhouse   = false;
        nearbyHeatSourcesStrength = 0f;

        if (room is null) return;

        bool isClosedRoom        = room.ExitCount == 0;
        bool isSkylightDominant  = room.SkylightCount > room.NonSkylightCount;

        bool isGreenhouse = isClosedRoom && isSkylightDominant;

        if (isGreenhouse)
        {
            inGreenhouse = true;
        }
        else if (isClosedRoom)
        {
            inEnclosedRoom = true;
        }

        if (!inEnclosedRoom) return;

        const double proximityPower = 0.875;

        BlockPos min = new(room.Location.X1, room.Location.Y1, room.Location.Z1, tempPos.dimension);
        BlockPos max = new(room.Location.X2, room.Location.Y2, room.Location.Z2, tempPos.dimension);

        var entityPos = tempPos.Copy();
        entity.World.BlockAccessor.WalkBlocks(min, max, (block, x, y, z) =>
        {
            tempPos.Set(x, y, z);
            var blockEntity = entity.World.BlockAccessor.GetBlockEntity(tempPos);
            if (blockEntity is IHeatSource heatSource)
            {
                float factor = Math.Min(
                    1f,
                    9f / (8f + (float)Math.Pow(
                        tempPos.DistanceTo(entityPos.X, entityPos.Y + 0.9f, entityPos.Z),
                        proximityPower
                    ))
                );

                nearbyHeatSourcesStrength += heatSource.GetHeatStrength(entity.World, tempPos, entityPos) * factor;
            }
        });

        nearbyHeatSourcesStrength = nearbyHeatSourcesStrength.GuardFinite();
    }
}
