using HydrateOrDiedrate.Config;
using InsanityLib.Generators.Attributes;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather;

[AutoRegistryName("HoD:bodytemperaturehot")]
public partial class EntityBehaviorBodyTemperatureHot : EntityBehavior
{
    public override string PropertyName() => "bodytemperaturehot";

    public EntityBehaviorBodyTemperatureHot(Entity entity) : base(entity)
    {
        entity.Stats.Register(HoDStats.CoolingEfficiency);
        entity.Stats.Register(HoDStats.Cooling, EnumStatBlendType.FlatSum);
        entity.Stats[HoDStats.Cooling].Set("base", 0, true);
    }

    public override void Initialize(EntityProperties properties, JsonObject attributes)
    {
        base.Initialize(properties, attributes);
        MapLegacyData();

        //HACK: manually updating blend types as workaround for https://github.com/anegostudios/VintageStory-Issues/issues/10278
        entity.WatchedAttributes.RegisterModifiedListener("stats", () =>
        {
            entity.Stats[HoDStats.Cooling].BlendType = EnumStatBlendType.FlatSum;
        });
    }

    public void UpdateStats()
    {
        var config = ModConfig.Instance.HeatAndCooling;
        if (!config.HarshHeat) return;


        UpdateRoomStats();
        UpdateGearStats();
        UpdateEnvironmentStats();

        var stat = entity.Stats[HoDStats.ThirstRate];
        var temperature = entity.World.BlockAccessor
            .GetClimateAt(entity.Pos.AsBlockPos, EnumGetClimateMode.NowValues)?
            .Temperature.GuardFinite() ?? 0f;

        float threshold = config.TemperatureThreshold;
        float tempOffsetPerCooling = config.CoolingTempOffsetPerPoint;
        float effectiveTemperature = temperature - Cooling * tempOffsetPerCooling;
        effectiveTemperature = Util.GuardFinite(effectiveTemperature);

        float effectiveTempDiff = effectiveTemperature - threshold;
        effectiveTempDiff = Util.GuardFinite(effectiveTempDiff);
        if (effectiveTempDiff > 0f)
        {
            var increase = effectiveTempDiff * config.HarshHeatThirstIncrease * Math.Pow(config.HarshHeatExponentialFactor, effectiveTempDiff);

            stat.Set("harshheat", 1 + (float)increase);
        }
        else stat.Remove("harshheat");
    }

    private void UpdateEnvironmentStats()
    {
        var config = ModConfig.Instance.HeatAndCooling;
        var stat = entity.Stats[HoDStats.Cooling];
        if (entity.WatchedAttributes.GetFloat("wetness", 0f) > 0f)
        {
            stat.Set("wetness", config.WetnessCoolingBonus);
        }
        else stat.Remove("wetness");

        int sunlightLevel = entity.World.BlockAccessor.GetLightLevel(
            entity.Pos.AsBlockPos,
            EnumLightLevelType.TimeOfDaySunLight
        );
        sunlightLevel = GameMath.Clamp(sunlightLevel, 0, 22);

        if (sunlightLevel < config.LowSunlightThreshold)
        {
            float darknessFraction = (float)(config.LowSunlightThreshold - sunlightLevel) / config.LowSunlightThreshold;
            darknessFraction = GameMath.Clamp(darknessFraction, 0f, 1f);
            float sunlightCoolingBonus = config.LowSunlightCoolingBonus * darknessFraction;

            stat.Set("lowSunlight", sunlightCoolingBonus);
        }

        if (!HasDirectSunlight())
        {
            stat.Set("shade", config.ShadeCoolingBonus);
        }
        else stat.Remove("shade");
    }

    private void UpdateGearStats()
    {

        if (entity.GetBehavior<EntityBehaviorContainer>()?.Inventory is not { } inventory) return;

        var config = ModConfig.Instance.HeatAndCooling;
        var stat = entity.Stats[HoDStats.Cooling];

        int unequippedSlots = 0;
        float gearCooling = 0f;

        for (int i = 0; i < inventory.Count; i++)
        {
            var slot = inventory[i];

            if (slot?.Itemstack is null)
            {
                //TODO make it clearer which slots this should count for and see if there is a better way then with these "magic numbers"
                //Only some slots count
                if (i == 0 || i == 1 || i == 2 || i == 11 || i == 3 || i == 4 || i == 8 || i == 5)
                {
                    unequippedSlots++;
                }

                continue;
            }

            if (slot.GetWearableBehavior() is not { } wearableBh) continue;

            var cooling = wearableBh.GetCooling(slot);

            cooling = (float)Math.Round(cooling, 1, MidpointRounding.AwayFromZero);

            gearCooling += cooling;
        }

        if(Math.Abs(gearCooling) > float.Epsilon)
        {
            stat.Set("gear", gearCooling);
        }
        else stat.Remove("gear");

        if(unequippedSlots > 0)
        {
            stat.Set("exposedSkin", unequippedSlots * config.UnequippedSlotCooling);
        }
        else stat.Remove("exposedSkin");
    }

    private void UpdateRoomStats()
    {
        var config = ModConfig.Instance.HeatAndCooling;
        var stat = entity.Stats[HoDStats.Cooling];
        var tempPos = entity.Pos.AsBlockPos;

        var roomRegistry = entity.Api.ModLoader.GetModSystem<RoomRegistry>();
        var room = roomRegistry.GetRoomForPosition(tempPos);

        if (room?.ExitCount == 0 && room.SkylightCount <= room.NonSkylightCount)
        {
            stat.Set("room", config.RoomCoolingBonus);
        }
        else stat.Remove("room");

        if (room?.ExitCount == 0)
        {
            const double proximityPower = 0.875;

            BlockPos min = new(room.Location.X1, room.Location.Y1, room.Location.Z1, tempPos.dimension);
            BlockPos max = new(room.Location.X2, room.Location.Y2, room.Location.Z2, tempPos.dimension);

            float nearbyHeatSourcesStrength = 0;
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

            nearbyHeatSourcesStrength *= 0.5f;

            if (Math.Abs(nearbyHeatSourcesStrength) > float.Epsilon)
            {
                stat.Set("roomHeating", -nearbyHeatSourcesStrength);
            }
            else stat.Remove("roomHeating");
        }
        else stat.Remove("roomHeating");
    }
    
    private bool HasDirectSunlight()
    {
        if (entity.World?.Calendar == null || entity is not EntityAgent agent)
            return false;

        var world = entity.World;
        var calendar = world.Calendar;

        Vec3d fromPos = agent.Pos.XYZ.Clone();
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

        world.RayTraceForSelection(
            fromPos,
            toPos,
            ref blockSel,
            ref entitySel,
            SunBlockFilter,
            null
        );
        return blockSel == null;
    }

    private static bool SunBlockFilter(BlockPos pos, Block block) => block?.BlockMaterial switch
    {
        null => false,
        EnumBlockMaterial.Glass or EnumBlockMaterial.Water or EnumBlockMaterial.Ice => false,
        EnumBlockMaterial.Leaves => true,
        _ => block.LightAbsorption > 0,
    };

}
