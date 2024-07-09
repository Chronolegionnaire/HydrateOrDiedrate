using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.EntityBehavior
{
    public class EntityBehaviorBodyTemperatureHot : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private readonly Config _config;
        private float _currentCooling;
        private float slowaccum;
        private float coolingCounter;
        private BlockPos plrpos;
        private bool inEnclosedRoom;
        private float nearHeatSourceStrength;
        private IWorldAccessor world;
        private Vec3d tmpPos = new Vec3d();
        private bool isMedievalExpansionInstalled;

        public float CurrentCooling
        {
            get => _currentCooling;
            set
            {
                _currentCooling = GameMath.Clamp(value, 0, float.MaxValue);
                entity.WatchedAttributes.SetFloat("currentCoolingHot", _currentCooling);
                entity.WatchedAttributes.MarkPathDirty("currentCoolingHot");
            }
        }

        public float CoolingMultiplier { get; set; } = 1.0f;

        public EntityBehaviorBodyTemperatureHot(Entity entity) : base(entity)
        {
            _config = new Config();
            _currentCooling = 0;
            CoolingMultiplier = 1.0f;
            LoadCooling();
            InitializeFields();
            isMedievalExpansionInstalled = IsMedievalExpansionInstalled(entity.World.Api);
        }

        public EntityBehaviorBodyTemperatureHot(Entity entity, Config config) : base(entity)
        {
            _config = config;
            _currentCooling = 0;
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
            if (!entity.Alive || !_config.HarshHeat) return;

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
            float coolingFactor = 0f;
            var entityAgent = entity as EntityAgent;
            if (entityAgent == null || entityAgent.GearInventory == null) return;

            int unequippedSlots = 0;

            for (int i = 0; i < entityAgent.GearInventory.Count; i++)
            {
                if (i == 0 || i == 6 || i == 7 || i == 8 || i == 9 || i == 10)
                {
                    continue;
                }

                var slot = entityAgent.GearInventory[i];

                if (slot?.Itemstack == null)
                {
                    unequippedSlots++;
                }
                else
                {
                    var cooling = CustomItemWearableExtensions.GetCooling(slot, entity.World.Api);
                    if (cooling != 0)
                    {
                        coolingFactor += cooling;
                    }
                }
            }

            coolingFactor += unequippedSlots * _config.UnequippedSlotCooling;

            if (entity.WatchedAttributes.GetFloat("wetness", 0f) > 0)
            {
                coolingFactor *= _config.WetnessCoolingFactor;
            }

            if (inEnclosedRoom)
            {
                coolingFactor *= _config.ShelterCoolingFactor;
            }

            coolingFactor -= nearHeatSourceStrength * 0.5f;

            BlockPos entityPos = entity.SidedPos.AsBlockPos;
            int sunlightLevel = world.BlockAccessor.GetLightLevel(entityPos, EnumLightLevelType.TimeOfDaySunLight);
            double hourOfDay = world.Calendar?.HourOfDay ?? 0;

            float sunlightCooling = (16 - sunlightLevel) / 16f * _config.SunlightCoolingFactor;
            double distanceTo4AM = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(4.0, hourOfDay, 24.0) / 12.0));
            double distanceTo3PM = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(15.0, hourOfDay, 24.0) / 12.0));
            double diurnalCooling = (0.5 - distanceTo4AM) * _config.DiurnalVariationAmplitude;
            coolingFactor += (float)(sunlightCooling + diurnalCooling);
            coolingFactor *= CoolingMultiplier;
            CurrentCooling = Math.Max(0, coolingFactor);
        }

        private void CheckRoom()
        {
            plrpos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
            plrpos.SetDimension(entity.Pos.AsBlockPos.dimension);
            var roomRegistry = entity.Api.ModLoader.GetModSystem<RoomRegistry>();

            if (roomRegistry == null) return;

            Room room = roomRegistry.GetRoomForPosition(plrpos);

            // Reset room state
            inEnclosedRoom = false;
            nearHeatSourceStrength = 0f;

            if (room != null)
            {
                // Check if the player is in an enclosed room
                inEnclosedRoom = room.ExitCount == 0 && room.SkylightCount < room.NonSkylightCount;

                if (inEnclosedRoom)
                {
                    bool isRefrigeratedRoom = false;
                    if (IsMedievalExpansionInstalled(entity.Api) && inEnclosedRoom)
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
                        if (blockEntity is Vintagestory.GameContent.IHeatSource heatSource)
                        {
                            tmpPos.Set(x, y, z);
                            float factor = Math.Min(1f,
                                9f / (8f + (float)Math.Pow(tmpPos.DistanceTo(px, py, pz), proximityPower)));
                            nearHeatSourceStrength +=
                                heatSource.GetHeatStrength(world, new BlockPos(x, y, z, plrpos.dimension), plrpos) *
                                factor;
                        }
                    });

                    if (isRefrigeratedRoom)
                    {
                        CurrentCooling += _config.RefrigerationCooling;
                    }
                }
            }

            entity.WatchedAttributes.MarkPathDirty("bodyTemp");
        }

        private bool CheckRefrigeration(Room room)
        {
            if (room == null)
            {
                return false;
            }

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "medievalexpansion");
            if (assembly == null)
            {
                return false;
            }

            var managerType = assembly.GetType("medievalexpansion.src.busineslogic.RoomRefridgePositionManager");
            if (managerType == null)
            {
                return false;
            }

            var instanceMethod = managerType.GetMethod("Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (instanceMethod == null)
            {
                return false;
            }

            var instance = instanceMethod.Invoke(null, null);
            if (instance == null)
            {
                return false;
            }

            var positionsProperty = managerType.GetProperty("RoomRefridgerPosition",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (positionsProperty == null)
            {
                return false;
            }

            var positions = positionsProperty.GetValue(instance) as IList<BlockPos>;
            if (positions == null)
            {
                return false;
            }

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
            _currentCooling = entity.WatchedAttributes.GetFloat("currentCoolingHot", 0);
        }

        public override string PropertyName() => "bodytemperaturehot";

        private bool IsMedievalExpansionInstalled(ICoreAPI api)
        {
            return api.ModLoader.IsModEnabled("medievalexpansion");
        }
    }
}
