using HydrateOrDiedrate.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather
{
    public class EntityBehaviorBodyTemperatureHot : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private float _rawClothingCooling;
        private float _adjustedCooling;
        private float slowaccum;
        private float coolingCounter;
        private BlockPos plrpos;
        private bool inEnclosedRoom;
        private float nearHeatSourceStrength;
        private IWorldAccessor world;
        private Vec3d tmpPos = new Vec3d();
        private bool isMedievalExpansionInstalled;

        public float RawClothingCooling
        {
            get => _rawClothingCooling;
            set
            {
                float safeValue = float.IsNaN(value) ? 0f : GameMath.Clamp(value, 0, float.MaxValue);
                _rawClothingCooling = safeValue;
                entity.WatchedAttributes.SetFloat("currentCoolingHot", safeValue);
                entity.WatchedAttributes.MarkPathDirty("currentCoolingHot");
            }
        }
        public float AdjustedCooling
        {
            get => _adjustedCooling;
            set
            {
                float safeValue = float.IsNaN(value) ? 0f : GameMath.Clamp(value, 0, float.MaxValue);
                _adjustedCooling = safeValue;
                entity.WatchedAttributes.SetFloat("adjustedCoolingHot", safeValue);
                entity.WatchedAttributes.MarkPathDirty("adjustedCoolingHot");
            }
        }
        public float CoolingMultiplier { get; set; } = 1.0f;
        
        public EntityBehaviorBodyTemperatureHot(Entity entity) : base(entity)
        {
            RawClothingCooling = 0;
            AdjustedCooling = 0;
            CoolingMultiplier = 1.0f;
            LoadCooling();
            InitializeFields();
            isMedievalExpansionInstalled = IsMedievalExpansionInstalled(entity.World.Api);
        }

        private void InitializeFields()
        {
            slowaccum = 0f;
            coolingCounter = 0f;
            plrpos = entity.Pos.AsBlockPos.Copy();
            inEnclosedRoom = false;
            nearHeatSourceStrength = 0f;
            world = entity.World;
        }
        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive || !ModConfig.Instance.HeatAndCooling.HarshHeat) return;

            coolingCounter += deltaTime;
            if (coolingCounter > 10f)
            {
                UpdateCoolingFactor();
                coolingCounter = 0f;
            }

            slowaccum += deltaTime;
            if (slowaccum > 3f)
            {
                CheckRoom();
                slowaccum = 0f;
            }
        }
        public void UpdateCoolingFactor()
        {
            if (entity is not EntityAgent entityAgent) return;

            var behaviorContainer = entityAgent.GetBehavior<EntityBehaviorContainer>();
            if (behaviorContainer == null || behaviorContainer.Inventory == null) return;

            var inventory = behaviorContainer.Inventory;
            float finalCooling = 0f;
            float displayCoolingTotal = 0f;
            int unequippedSlots = 0;

            for (int i = 0; i < inventory.Count; i++)
            {
                var slot = inventory[i];
                float itemDisplayCooling = 0f;
                float itemActualCooling = 0f;

                if (slot?.Itemstack != null)
                {
                    itemActualCooling = CustomItemWearableExtensions.GetCooling(slot, entity.World.Api);
                    finalCooling += itemActualCooling;
                    float baseCooling = CoolingManager.GetMaxCooling(slot.Itemstack);
                    float condition = slot.Itemstack.Attributes.GetFloat("condition", 1f);
                    condition = float.IsNaN(condition) ? 1f : condition;
                    float rawValue = Math.Min(baseCooling, condition * 2f * baseCooling);
                    rawValue = float.IsNaN(rawValue) ? 0f : rawValue;
                    itemDisplayCooling = (float)Math.Round(rawValue, 1, MidpointRounding.AwayFromZero);
                }
                else
                {
                    if (i == 0 || i == 1 || i == 2 || i == 11 || i == 3 || i == 4 || i == 8 || i == 5)
                    {
                        unequippedSlots++;
                    }
                }
                displayCoolingTotal += itemDisplayCooling;
            }

            var config = ModConfig.Instance.HeatAndCooling;
            displayCoolingTotal += unequippedSlots * config.UnequippedSlotCooling;
            finalCooling += unequippedSlots * config.UnequippedSlotCooling;
            RawClothingCooling = float.IsNaN(displayCoolingTotal) ? 0f : displayCoolingTotal;
            if (entity.WatchedAttributes.GetFloat("wetness", 0f) > 0)
            {
                finalCooling *= config.WetnessCoolingFactor;
            }

            if (inEnclosedRoom)
            {
                finalCooling *= config.ShelterCoolingFactor;
            }

            finalCooling -= nearHeatSourceStrength * 0.5f;

            BlockPos entityPos = entity.SidedPos.AsBlockPos;
            int sunlightLevel = world.BlockAccessor.GetLightLevel(entityPos, EnumLightLevelType.TimeOfDaySunLight);
            double hourOfDay = world.Calendar?.HourOfDay ?? 0;

            float sunlightCooling = (16 - sunlightLevel) / 16f * config.SunlightCoolingFactor;
            double distanceTo4AM = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(4.0, hourOfDay, 24.0) / 12.0));
            double distanceTo3PM = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(15.0, hourOfDay, 24.0) / 12.0));

            double diurnalCooling = (0.5 - distanceTo4AM - distanceTo3PM) * config.DiurnalVariationAmplitude;
            finalCooling += (float)(sunlightCooling + diurnalCooling);
            finalCooling *= CoolingMultiplier;
            AdjustedCooling = float.IsNaN(finalCooling) ? 0f : Math.Max(0, finalCooling);
        }
        private void CheckRoom()
        {
            plrpos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
            plrpos.SetDimension(entity.Pos.AsBlockPos.dimension);
            var roomRegistry = entity.Api.ModLoader.GetModSystem<RoomRegistry>();

            if (roomRegistry == null) return;

            Room room = roomRegistry.GetRoomForPosition(plrpos);
            
            inEnclosedRoom = false;
            nearHeatSourceStrength = 0f;

            if (room != null)
            {
                inEnclosedRoom = (room.ExitCount == 0 && room.SkylightCount < room.NonSkylightCount);

                if (inEnclosedRoom)
                {
                    bool isRefrigeratedRoom = false;
                    if (IsMedievalExpansionInstalled(entity.Api))
                    {
                        isRefrigeratedRoom = CheckRefrigeration(room);
                    }

                    double px = entity.Pos.X;
                    double py = entity.Pos.Y + 0.9;
                    double pz = entity.Pos.Z;
                    double proximityPower = 0.875;

                    BlockPos min = new BlockPos(room.Location.X1, room.Location.Y1, room.Location.Z1, plrpos.dimension);
                    BlockPos max = new BlockPos(room.Location.X2, room.Location.Y2, room.Location.Z2, plrpos.dimension);

                    world.BlockAccessor.WalkBlocks(min, max, (block, x, y, z) =>
                    {
                        var blockEntity = world.BlockAccessor.GetBlockEntity(new BlockPos(x, y, z, plrpos.dimension));
                        if (blockEntity is IHeatSource heatSource)
                        {
                            tmpPos.Set(x, y, z);
                            float factor = Math.Min(
                                1f,
                                9f / (8f + (float)Math.Pow(tmpPos.DistanceTo(px, py, pz), proximityPower))
                            );
                            nearHeatSourceStrength +=
                                heatSource.GetHeatStrength(world, new BlockPos(x, y, z, plrpos.dimension), plrpos) * factor;
                        }
                    });

                    if (isRefrigeratedRoom)
                    {
                        AdjustedCooling += ModConfig.Instance.HeatAndCooling.RefrigerationCooling;
                    }
                }
            }
            nearHeatSourceStrength = float.IsNaN(nearHeatSourceStrength) ? 0f : nearHeatSourceStrength;
            entity.WatchedAttributes.MarkPathDirty("bodyTemp");
        }
        private bool CheckRefrigeration(Room room)
        {
            if (room == null) return false;

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "medievalexpansion");
            if (assembly == null) return false;

            var managerType = assembly.GetType("medievalexpansion.src.busineslogic.RoomRefridgePositionManager");
            if (managerType == null) return false;

            var instanceMethod = managerType.GetMethod("Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (instanceMethod == null) return false;

            var instance = instanceMethod.Invoke(null, null);
            if (instance == null) return false;

            var positionsProperty = managerType.GetProperty("RoomRefridgerPosition",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (positionsProperty == null) return false;

            var positions = positionsProperty.GetValue(instance) as IList<BlockPos>;
            if (positions == null) return false;

            foreach (var pos in positions)
            {
                if (room.Location.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }
        public void LoadCooling()
        {
            _rawClothingCooling = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0);
            _adjustedCooling = entity.WatchedAttributes.GetFloat("adjustedCoolingHot", 0);
        }
        public override string PropertyName() => "bodytemperaturehot";
        private bool IsMedievalExpansionInstalled(ICoreAPI api)
        {
            return api.ModLoader.IsModEnabled("medievalexpansion");
        }
    }
}
