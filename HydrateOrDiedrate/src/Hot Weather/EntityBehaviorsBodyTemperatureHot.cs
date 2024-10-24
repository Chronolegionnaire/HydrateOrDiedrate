﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather
{
    public class EntityBehaviorBodyTemperatureHot : EntityBehavior
    {
        public float CurrentCooling
        {
            get
            {
                return this._currentCooling;
            }
            set
            {
                this._currentCooling = GameMath.Clamp(value, 0f, float.MaxValue);
                this.entity.WatchedAttributes.SetFloat("currentCoolingHot", this._currentCooling);
                this.entity.WatchedAttributes.MarkPathDirty("currentCoolingHot");
            }
        }

        public float CoolingMultiplier { get; set; } = 1f;

        public EntityBehaviorBodyTemperatureHot(Entity entity)
            : base(entity)
        {
            this._config = new Config.Config();
            this._currentCooling = 0f;
            this.CoolingMultiplier = 1f;
            this.LoadCooling();
            this.InitializeFields();
            this.isMedievalExpansionInstalled = this.IsMedievalExpansionInstalled(entity.World.Api);
        }

        public EntityBehaviorBodyTemperatureHot(Entity entity, Config.Config config)
            : base(entity)
        {
            this._config = config;
            this._currentCooling = 0f;
            this.CoolingMultiplier = 1f;
            this.LoadCooling();
            this.InitializeFields();
            this.isMedievalExpansionInstalled = this.IsMedievalExpansionInstalled(entity.World.Api);
        }

        private void InitializeFields()
        {
            this.slowaccum = 0f;
            this.coolingCounter = 0f;
            this.plrpos = this.entity.Pos.AsBlockPos.Copy();
            this.inEnclosedRoom = false;
            this.nearHeatSourceStrength = 0f;
            this.world = this.entity.World;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!this.entity.Alive || !this._config.HarshHeat)
            {
                return;
            }
            this.coolingCounter += deltaTime;
            if (this.coolingCounter > 10f)
            {
                this.UpdateCoolingFactor();
                this.coolingCounter = 0f;
            }
            this.slowaccum += deltaTime;
            if (this.slowaccum > 3f)
            {
                this.CheckRoom();
                this.slowaccum = 0f;
            }
        }

        public void UpdateCoolingFactor()
        {
            float num = 0f;
            EntityAgent entityAgent = this.entity as EntityAgent;
            if (entityAgent == null || entityAgent.GearInventory == null)
            {
                return;
            }
            int num2 = 0;
            for (int i = 0; i < entityAgent.GearInventory.Count; i++)
            {
                if (i != 0 && i != 6 && i != 7 && i != 8 && i != 9 && i != 10)
                {
                    ItemSlot itemSlot = entityAgent.GearInventory[i];
                    if (((itemSlot != null) ? itemSlot.Itemstack : null) == null)
                    {
                        num2++;
                    }
                    else
                    {
                        float cooling = CustomItemWearableExtensions.GetCooling(itemSlot, this.entity.World.Api);
                        if (cooling != 0f)
                        {
                            num += cooling;
                        }
                    }
                }
            }
            num += (float)num2 * this._config.UnequippedSlotCooling;
            if (this.entity.WatchedAttributes.GetFloat("wetness", 0f) > 0f)
            {
                num *= this._config.WetnessCoolingFactor;
            }
            if (this.inEnclosedRoom)
            {
                num *= this._config.ShelterCoolingFactor;
            }
            num -= this.nearHeatSourceStrength * 0.5f;
            BlockPos asBlockPos = this.entity.SidedPos.AsBlockPos;
            int lightLevel = this.world.BlockAccessor.GetLightLevel(asBlockPos, EnumLightLevelType.TimeOfDaySunLight);
            IGameCalendar calendar = this.world.Calendar;
            double num3 = (double)((calendar != null) ? calendar.HourOfDay : 0f);
            float num4 = (float)(16 - lightLevel) / 16f * this._config.SunlightCoolingFactor;
            double num5 = GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(4.0, num3, 24.0) / 12.0));
            GameMath.SmoothStep(Math.Abs(GameMath.CyclicValueDistance(15.0, num3, 24.0) / 12.0));
            double num6 = (0.5 - num5) * (double)this._config.DiurnalVariationAmplitude;
            num += (float)((double)num4 + num6);
            num *= this.CoolingMultiplier;
            this.CurrentCooling = Math.Max(0f, num);
        }

        private void CheckRoom()
        {
            this.plrpos.Set((int)this.entity.Pos.X, (int)this.entity.Pos.Y, (int)this.entity.Pos.Z);
            this.plrpos.SetDimension(this.entity.Pos.AsBlockPos.dimension);
            RoomRegistry modSystem = this.entity.Api.ModLoader.GetModSystem<RoomRegistry>(true);
            if (modSystem == null)
            {
                return;
            }
            Room roomForPosition = modSystem.GetRoomForPosition(this.plrpos);
            this.inEnclosedRoom = false;
            this.nearHeatSourceStrength = 0f;
            if (roomForPosition != null)
            {
                this.inEnclosedRoom = roomForPosition.ExitCount == 0 && roomForPosition.SkylightCount < roomForPosition.NonSkylightCount;
                if (this.inEnclosedRoom)
                {
                    bool flag = false;
                    if (this.IsMedievalExpansionInstalled(this.entity.Api) && this.inEnclosedRoom)
                    {
                        flag = this.CheckRefrigeration(roomForPosition);
                    }
                    double px = this.entity.Pos.X;
                    double py = this.entity.Pos.Y + 0.9;
                    double pz = this.entity.Pos.Z;
                    double proximityPower = 0.875;
                    BlockPos blockPos = new BlockPos(roomForPosition.Location.X1, roomForPosition.Location.Y1, roomForPosition.Location.Z1, this.plrpos.dimension);
                    BlockPos blockPos2 = new BlockPos(roomForPosition.Location.X2, roomForPosition.Location.Y2, roomForPosition.Location.Z2, this.plrpos.dimension);
                    this.world.BlockAccessor.WalkBlocks(blockPos, blockPos2, delegate(Block block, int x, int y, int z)
                    {
                        IHeatSource heatSource = this.world.BlockAccessor.GetBlockEntity(new BlockPos(x, y, z, this.plrpos.dimension)) as IHeatSource;
                        if (heatSource != null)
                        {
                            this.tmpPos.Set((double)x, (double)y, (double)z);
                            float num = Math.Min(1f, 9f / (8f + (float)Math.Pow((double)this.tmpPos.DistanceTo(px, py, pz), proximityPower)));
                            this.nearHeatSourceStrength += heatSource.GetHeatStrength(this.world, new BlockPos(x, y, z, this.plrpos.dimension), this.plrpos) * num;
                        }
                    }, false);
                    if (flag)
                    {
                        this.CurrentCooling += this._config.RefrigerationCooling;
                    }
                }
            }
            this.entity.WatchedAttributes.MarkPathDirty("bodyTemp");
        }

        private bool CheckRefrigeration(Room room)
        {
            if (room == null)
            {
                return false;
            }
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name == "medievalexpansion");
            if (assembly == null)
            {
                return false;
            }
            Type type = assembly.GetType("medievalexpansion.src.busineslogic.RoomRefridgePositionManager");
            if (type == null)
            {
                return false;
            }
            MethodInfo method = type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                return false;
            }
            object obj = method.Invoke(null, null);
            if (obj == null)
            {
                return false;
            }
            PropertyInfo property = type.GetProperty("RoomRefridgerPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                return false;
            }
            IList<BlockPos> list = property.GetValue(obj) as IList<BlockPos>;
            if (list == null)
            {
                return false;
            }
            foreach (BlockPos blockPos in list)
            {
                if (room.Location.Contains(blockPos))
                {
                    return true;
                }
            }
            return false;
        }

        public void LoadCooling()
        {
            this._currentCooling = this.entity.WatchedAttributes.GetFloat("currentCoolingHot", 0f);
        }

        public override string PropertyName()
        {
            return "bodytemperaturehot";
        }

        private bool IsMedievalExpansionInstalled(ICoreAPI api)
        {
            return api.ModLoader.IsModEnabled("medievalexpansion");
        }

        private readonly Config.Config _config;
        private float _currentCooling;
        private float slowaccum;
        private float coolingCounter;
        private BlockPos plrpos;
        private bool inEnclosedRoom;
        private float nearHeatSourceStrength;
        private IWorldAccessor world;
        private Vec3d tmpPos = new Vec3d();
        private bool isMedievalExpansionInstalled;
    }
}