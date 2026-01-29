using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hot_Weather
{
    public class EntityBehaviorBodyTemperatureHoD : EntityBehavior
    {
        private ITreeAttribute tempTree;
        private ICoreAPI api;
        private EntityAgent eagent;
        private float accum;
        private float slowaccum;
        private float veryslowaccum;
        private BlockPos plrpos = new BlockPos();
        private BlockPos tmpPos = new BlockPos();
        private bool inEnclosedRoom;
        private float tempChange;
        private float clothingWarmthBonus;
        private float clothingCoolingBonus;
        private float damagingFreezeHours;
        private float damagingHeatHours;
        private int sprinterCounter;
        private double lastWearableHoursTotalUpdate;
        private float bodyTemperatureResistance;
        private ICachingBlockAccessor blockAccess;
        public float NormalBodyTemperature;
        private bool firstTick;
        private long lastMoveMs;
        private float comfortTemperature = 22f;
        private EnumDamageType heatDamageType = EnumDamageType.Fire;
        private float heatParticleAccum;

        public override string PropertyName()
        {
            return "bodytemperature";
        }

        public float CurBodyTemperature
        {
            get { return this.tempTree.GetFloat("bodytemp", 0f); }
            set
            {
                this.tempTree.SetFloat("bodytemp", value);
                this.entity.WatchedAttributes.MarkPathDirty("bodyTemp");
            }
        }

        protected float nearHeatSourceStrength
        {
            get { return this.tempTree.GetFloat("nearHeatSourceStrength", 0f); }
            set { this.tempTree.SetFloat("nearHeatSourceStrength", value); }
        }

        public float Wetness
        {
            get { return this.entity.WatchedAttributes.GetFloat("wetness", 0f); }
            set { this.entity.WatchedAttributes.SetFloat("wetness", value); }
        }

        public double LastWetnessUpdateTotalHours
        {
            get { return this.entity.WatchedAttributes.GetDouble("lastWetnessUpdateTotalHours", 0.0); }
            set { this.entity.WatchedAttributes.SetDouble("lastWetnessUpdateTotalHours", value); }
        }

        public double BodyTempUpdateTotalHours
        {
            get { return this.tempTree.GetDouble("bodyTempUpdateTotalHours", 0.0); }
            set
            {
                this.tempTree.SetDouble("bodyTempUpdateTotalHours", value);
                this.entity.WatchedAttributes.MarkPathDirty("bodyTemp");
            }
        }

        public EntityBehaviorBodyTemperatureHoD(Entity entity) : base(entity)
        {
            this.eagent = (entity as EntityAgent);
        }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            api = entity.World.Api;
            blockAccess = api.World.GetCachingBlockAccessor(false, false);

            tempTree = entity.WatchedAttributes.GetTreeAttribute("bodyTemp");

            NormalBodyTemperature = typeAttributes["defaultBodyTemperature"].AsFloat(37f);
            comfortTemperature = typeAttributes["comfortTemperature"].AsFloat(22f);

            // Heat damage type (configurable)
            string heatDmgStr = typeAttributes["heatDamageType"].AsString("Fire");
            if (!Enum.TryParse(heatDmgStr, true, out heatDamageType))
            {
                heatDamageType = EnumDamageType.Fire;
            }

            // Ensure tree exists
            if (tempTree == null)
            {
                tempTree = new TreeAttribute();
                entity.WatchedAttributes.SetAttribute("bodyTemp", tempTree);

                // IMPORTANT: start at normal, not Normal+4
                CurBodyTemperature = NormalBodyTemperature;

                BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
                LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
            }
            else
            {
                float cur = CurBodyTemperature;
                cur = GameMath.Clamp(cur, 31f, 52f);
                if (Math.Abs(cur - (NormalBodyTemperature + 4f)) < 0.001f)
                {
                    cur = NormalBodyTemperature;
                }

                CurBodyTemperature = cur;

                BodyTempUpdateTotalHours = api.World.Calendar.TotalHours;
                LastWetnessUpdateTotalHours = api.World.Calendar.TotalHours;
            }
            bodyTemperatureResistance = entity.World.Config.GetString("bodyTemperatureResistance", null).ToFloat(0f);
            entity.WatchedAttributes.SetFloat("freezingEffectStrength", 0f);
            entity.WatchedAttributes.SetFloat("overheatingEffectStrength", 0f);
            damagingFreezeHours = 0f;
            damagingHeatHours = 0f;
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            ICachingBlockAccessor cachingBlockAccessor = this.blockAccess;
            if (cachingBlockAccessor != null) cachingBlockAccessor.Dispose();
            this.blockAccess = null;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!this.firstTick && this.api.Side == EnumAppSide.Client)
            {
                EntityShapeRenderer esr = this.entity.Properties.Client.Renderer as EntityShapeRenderer;
                if (esr != null)
                {
                    esr.getFrostAlpha = delegate ()
                    {
                        float temp = this.api.World.BlockAccessor.GetClimateAt(this.entity.Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.api.World.Calendar.TotalDays).Temperature;
                        float freezestrength = GameMath.Clamp((this.NormalBodyTemperature - this.CurBodyTemperature) / 4f - 0.5f, 0f, 1f);
                        return GameMath.Clamp((Math.Max(0f, -temp) - 5f) / 5f, 0f, 1f) * freezestrength;
                    };
                }
            }

            this.firstTick = true;
            if (this.blockAccess == null) return;

            this.updateFreezingAnimState();
            this.updateOverheatingAnimState();

            this.accum += deltaTime;
            this.slowaccum += deltaTime;
            this.veryslowaccum += deltaTime;

            this.plrpos.Set((int)this.entity.Pos.X, (int)this.entity.Pos.Y, (int)this.entity.Pos.Z);

            if (this.api.Side == EnumAppSide.Client)
            {
                float heatStr = this.entity.WatchedAttributes.GetFloat("overheatingEffectStrength", 0f);
                if (heatStr > 0.05f && this.entity.Alive)
                {
                    this.heatParticleAccum += deltaTime;
                    float interval = GameMath.Lerp(1.2f, 0.15f, heatStr);
                    if (this.heatParticleAccum >= interval)
                    {
                        this.heatParticleAccum = 0f;
                        SpawnHeatParticles(heatStr);
                    }
                }
                else
                {
                    this.heatParticleAccum = 0f;
                }
            }

            if (this.veryslowaccum > 10f && (this.damagingFreezeHours > 3f || this.damagingHeatHours > 3f))
            {
                bool harshWinters = this.api.World.Config.GetString("harshWinters", null).ToBool(true);
                bool harshSummers = this.api.World.Config.GetString("harshSummers", null).ToBool(true);

                if (this.damagingFreezeHours > 3f && harshWinters)
                {
                    this.entity.ReceiveDamage(new DamageSource
                    {
                        DamageTier = 0,
                        Source = EnumDamageSource.Weather,
                        Type = EnumDamageType.Frost
                    }, 0.2f);
                }
                else if (this.damagingHeatHours > 3f && harshSummers)
                {
                    this.entity.ReceiveDamage(new DamageSource
                    {
                        DamageTier = 0,
                        Source = EnumDamageSource.Weather,
                        Type = this.heatDamageType
                    }, 0.2f);
                }

                this.veryslowaccum = 0f;

                if (this.eagent.Controls.Sprint)
                {
                    this.sprinterCounter = GameMath.Clamp(this.sprinterCounter + 1, 0, 10);
                }
                else
                {
                    this.sprinterCounter = GameMath.Clamp(this.sprinterCounter - 1, 0, 10);
                }
            }

            if (this.slowaccum > 3f)
            {
                if (this.api.Side == EnumAppSide.Server)
                {
                    Room room = this.api.ModLoader.GetModSystem<RoomRegistry>(true).GetRoomForPosition(this.plrpos);
                    this.inEnclosedRoom = (room.ExitCount == 0 || room.SkylightCount < room.NonSkylightCount);

                    this.nearHeatSourceStrength = 0f;

                    double px = this.entity.Pos.X;
                    double py = this.entity.Pos.Y + 0.9;
                    double pz = this.entity.Pos.Z;

                    double proximityPower = this.inEnclosedRoom ? 0.875 : 1.25;

                    BlockPos min;
                    BlockPos max;

                    if (this.inEnclosedRoom && room.Location.SizeX >= 1 && room.Location.SizeY >= 1 && room.Location.SizeZ >= 1)
                    {
                        min = new BlockPos(room.Location.MinX, room.Location.MinY, room.Location.MinZ);
                        max = new BlockPos(room.Location.MaxX, room.Location.MaxY, room.Location.MaxZ);
                    }
                    else
                    {
                        min = this.plrpos.AddCopy(-3, -3, -3);
                        max = this.plrpos.AddCopy(3, 3, 3);
                    }

                    this.blockAccess.Begin();
                    this.blockAccess.WalkBlocks(min, max, delegate (Block block, int x, int y, int z)
                    {
                        IHeatSource src = block.GetInterface<IHeatSource>(this.api.World, this.tmpPos.Set(x, y, z));
                        if (src != null)
                        {
                            float factor = Math.Min(1f, 9f / (8f + (float)Math.Pow(this.tmpPos.DistanceSqToNearerEdge(px, py, pz), proximityPower)));
                            this.nearHeatSourceStrength += src.GetHeatStrength(this.api.World, this.tmpPos, this.plrpos) * factor;
                        }
                    }, false);
                }

                this.updateWearableConditions();
                this.entity.WatchedAttributes.MarkPathDirty("bodyTemp");
                this.slowaccum = 0f;
            }

            if (this.accum > 1f && this.api.Side == EnumAppSide.Server)
            {
                EntityPlayer eplr = this.entity as EntityPlayer;
                IPlayer plr = (eplr != null) ? eplr.Player : null;

                if (this.api.Side == EnumAppSide.Server)
                {
                    IServerPlayer serverPlayer = plr as IServerPlayer;
                    if (serverPlayer == null || serverPlayer.ConnectionState != EnumClientState.Playing) return;
                }

                if ((plr != null && plr.WorldData.CurrentGameMode == EnumGameMode.Creative) ||
                    (plr != null && plr.WorldData.CurrentGameMode == EnumGameMode.Spectator))
                {
                    this.CurBodyTemperature = this.NormalBodyTemperature;
                    this.entity.WatchedAttributes.SetFloat("freezingEffectStrength", 0f);
                    this.entity.WatchedAttributes.SetFloat("overheatingEffectStrength", 0f);
                    this.damagingFreezeHours = 0f;
                    this.damagingHeatHours = 0f;
                    return;
                }

                if (plr != null && (eplr.Controls.TriesToMove || eplr.Controls.Jump || eplr.Controls.LeftMouseDown || eplr.Controls.RightMouseDown))
                {
                    this.lastMoveMs = this.entity.World.ElapsedMilliseconds;
                }

                ClimateCondition conds = this.api.World.BlockAccessor.GetClimateAt(this.plrpos, EnumGetClimateMode.NowValues, 0.0);
                if (conds == null) return;

                Vec3d windspeed = this.api.World.BlockAccessor.GetWindSpeedAt(this.plrpos);
                bool rainExposed = this.api.World.BlockAccessor.GetRainMapHeightAt(this.plrpos) <= this.plrpos.Y;

                float wetnessFromRain = conds.Rainfall * (rainExposed ? 0.06f : 0f) * ((conds.Temperature < -1f) ? 0.05f : 1f);

                if (wetnessFromRain > 0f && eplr != null)
                {
                    IInventory ownInventory = eplr.Player.InventoryManager.GetOwnInventory("character");
                    ItemSlot headSlot = ownInventory?.FirstOrDefault((ItemSlot slot) => (slot as ItemSlotCharacter).Type == EnumCharacterDressType.Head);

                    if (headSlot != null && !headSlot.Empty)
                    {
                        wetnessFromRain *= GameMath.Clamp(1f - headSlot.Itemstack.ItemAttributes["rainProtectionPerc"].AsFloat(0f), 0f, 1f);
                    }
                }

                float swimmingWetness = this.entity.Swimming ? 1f : 0f;

                double nowHrs = this.api.World.Calendar.TotalHours;

                float dryFactor = (float)Math.Max(0.0,
                    (nowHrs - this.LastWetnessUpdateTotalHours) * (double)GameMath.Clamp(this.nearHeatSourceStrength, 1f, 2f));

                this.Wetness = GameMath.Clamp(
                    this.Wetness + wetnessFromRain + swimmingWetness - dryFactor,
                    0f, 1f
                );

                this.LastWetnessUpdateTotalHours = nowHrs;
                this.accum = 0f;

                float sprintBonus = (float)this.sprinterCounter / 2f;
                float wetnessDebuff = (float)Math.Max(0.0, (double)this.Wetness - 0.1) * 15f;

                float baseAmbient = conds.Temperature + sprintBonus - wetnessDebuff;

                float warmthApplied = baseAmbient < this.comfortTemperature ? this.clothingWarmthBonus : 0f;
                float coolingApplied = baseAmbient > this.comfortTemperature ? this.clothingCoolingBonus : 0f;

                float hereTemperature = baseAmbient + warmthApplied - coolingApplied;

                float tempDiff = hereTemperature - GameMath.Clamp(hereTemperature, this.bodyTemperatureResistance, 50f);
                if (tempDiff == 0f)
                {
                    tempDiff = Math.Max(hereTemperature - this.bodyTemperatureResistance, 0f);
                }

                float ambientTempChange = GameMath.Clamp(tempDiff / 6f, -6f, 6f);

                this.tempChange =
                    this.nearHeatSourceStrength
                    + (this.inEnclosedRoom
                        ? 1f
                        : (-(float)Math.Max((windspeed.Length() - 0.15) * 2.0, 0.0) + ambientTempChange));

                EntityBehaviorTiredness behavior = this.entity.GetBehavior<EntityBehaviorTiredness>();
                if (behavior != null && behavior.IsSleeping)
                {
                    if (this.inEnclosedRoom)
                    {
                        this.tempChange = GameMath.Clamp(this.NormalBodyTemperature - this.CurBodyTemperature, -0.15f, 0.15f);
                    }
                    else if (!rainExposed)
                    {
                        this.tempChange += GameMath.Clamp(this.NormalBodyTemperature - this.CurBodyTemperature, 1f, 1f);
                    }
                }

                if (this.entity.IsOnFire)
                {
                    this.tempChange = Math.Max(25f, this.tempChange);
                }

                float tempUpdateHoursPassed = (float)(nowHrs - this.BodyTempUpdateTotalHours);

                if ((double)tempUpdateHoursPassed > 0.01)
                {
                    if ((double)this.tempChange < -0.5 || this.tempChange > 0f)
                    {
                        if ((double)this.tempChange > 0.5) this.tempChange *= 2f;

                        this.CurBodyTemperature = GameMath.Clamp(
                            this.CurBodyTemperature + this.tempChange * tempUpdateHoursPassed,
                            31f,
                            52f
                        );
                    }

                    this.BodyTempUpdateTotalHours = nowHrs;

                    float coldStr = GameMath.Clamp((this.NormalBodyTemperature - this.CurBodyTemperature) / 4f - 0.5f, 0f, 1f);
                    this.entity.WatchedAttributes.SetFloat("freezingEffectStrength", coldStr);

                    float heatStr = GameMath.Clamp((this.CurBodyTemperature - this.NormalBodyTemperature) / 4f - 0.5f, 0f, 1f);
                    this.entity.WatchedAttributes.SetFloat("overheatingEffectStrength", heatStr);

                    if (this.NormalBodyTemperature - this.CurBodyTemperature > 4f)
                    {
                        this.damagingFreezeHours += tempUpdateHoursPassed;
                    }
                    else
                    {
                        this.damagingFreezeHours = 0f;
                    }

                    if (this.CurBodyTemperature - this.NormalBodyTemperature > 4f)
                    {
                        this.damagingHeatHours += tempUpdateHoursPassed;
                    }
                    else
                    {
                        this.damagingHeatHours = 0f;
                    }
                }
            }
        }

        private void SpawnHeatParticles(float heatStr)
        {
            if (!(this.api is ICoreClientAPI capi)) return;

            Vec3d pos = this.entity.Pos.XYZ.Add(0, this.entity.SelectionBox.Y2 * 0.6, 0);
            float qty = GameMath.Lerp(2f, 10f, heatStr);

            SimpleParticleProperties spp = new SimpleParticleProperties(
                (int)qty, (int)(qty + 2),
                ColorUtil.ToRgba(40, 255, 255, 255),
                pos.AddCopy(-0.2, -0.1, -0.2),
                pos.AddCopy(0.2, 0.15, 0.2),
                new Vec3f(-0.05f, 0.05f, -0.05f),
                new Vec3f(0.05f, 0.18f + 0.25f * heatStr, 0.05f),
                0.25f, 0.6f + 0.8f * heatStr,
                0.4f, 1.2f
            );

            spp.GravityEffect = -0.02f;
            spp.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.8f);
            spp.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.8f);

            capi.World.SpawnParticles(spp);
        }

        private void updateFreezingAnimState()
        {
            float str = this.entity.WatchedAttributes.GetFloat("freezingEffectStrength", 0f);

            EntityAgent entityAgent = this.entity as EntityAgent;

            bool held = false;
            if (entityAgent != null)
            {
                held = (entityAgent.LeftHandItemSlot?.Itemstack != null) || (entityAgent.RightHandItemSlot?.Itemstack != null);
            }

            EntityPlayer entityPlayer = this.entity as EntityPlayer;
            EnumGameMode? mode = entityPlayer?.Player?.WorldData?.CurrentGameMode;

            if ((this.damagingFreezeHours <= 0f && (double)str <= 0.4) ||
                mode.GetValueOrDefault() == EnumGameMode.Creative ||
                mode.GetValueOrDefault() == EnumGameMode.Spectator ||
                !this.entity.Alive)
            {
                if (this.entity.AnimManager.IsAnimationActive(new string[] { "coldidle" }) ||
                    this.entity.AnimManager.IsAnimationActive(new string[] { "coldidleheld" }))
                {
                    this.entity.StopAnimation("coldidle");
                    this.entity.StopAnimation("coldidleheld");
                }
                return;
            }

            if (held)
            {
                this.entity.StartAnimation("coldidleheld");
                this.entity.StopAnimation("coldidle");
                return;
            }

            this.entity.StartAnimation("coldidle");
            this.entity.StopAnimation("coldidleheld");
        }

        private void updateOverheatingAnimState()
        {
            float str = this.entity.WatchedAttributes.GetFloat("overheatingEffectStrength", 0f);

            EntityAgent entityAgent = this.entity as EntityAgent;

            bool held = false;
            if (entityAgent != null)
            {
                held = (entityAgent.LeftHandItemSlot?.Itemstack != null) || (entityAgent.RightHandItemSlot?.Itemstack != null);
            }

            EntityPlayer entityPlayer = this.entity as EntityPlayer;
            EnumGameMode? mode = entityPlayer?.Player?.WorldData?.CurrentGameMode;

            if ((this.damagingHeatHours <= 0f && (double)str <= 0.4) ||
                mode.GetValueOrDefault() == EnumGameMode.Creative ||
                mode.GetValueOrDefault() == EnumGameMode.Spectator ||
                !this.entity.Alive)
            {
                if (this.entity.AnimManager.IsAnimationActive(new string[] { "hotidle" }) ||
                    this.entity.AnimManager.IsAnimationActive(new string[] { "hotidleheld" }))
                {
                    this.entity.StopAnimation("hotidle");
                    this.entity.StopAnimation("hotidleheld");
                }
                return;
            }

            if (held)
            {
                this.entity.StartAnimation("hotidleheld");
                this.entity.StopAnimation("hotidle");
                return;
            }

            this.entity.StartAnimation("hotidle");
            this.entity.StopAnimation("hotidleheld");
        }

        public void didConsume(ItemStack stack, float intensity = 1f)
        {
            Math.Abs(stack.Collectible.GetTemperature(this.api.World, stack) - this.CurBodyTemperature);
        }

        private void updateWearableConditions()
        {
            double hoursPassed = this.api.World.Calendar.TotalHours - this.lastWearableHoursTotalUpdate;

            if (hoursPassed < -1.0)
            {
                this.lastWearableHoursTotalUpdate = this.api.World.Calendar.TotalHours;
                return;
            }

            if (hoursPassed < 0.5) return;

            EntityAgent entityAgent = this.entity as EntityAgent;

            this.clothingWarmthBonus = 0f;
            this.clothingCoolingBonus = 0f;

            float conditionloss = 0f;
            if (this.entity.World.ElapsedMilliseconds - this.lastMoveMs <= 3000L)
            {
                conditionloss = -(float)hoursPassed / 1296f;
            }

            EntityBehaviorPlayerInventory bh = (entityAgent != null) ? entityAgent.GetBehavior<EntityBehaviorPlayerInventory>() : null;
            if (bh?.Inventory != null)
            {
                foreach (ItemSlot slot in bh.Inventory)
                {
                    ItemStack itemstack = slot.Itemstack;
                    ItemWearable wearableItem = (itemstack?.Collectible as ItemWearable);

                    if (wearableItem != null && !wearableItem.IsArmor)
                    {
                        this.clothingWarmthBonus += wearableItem.GetWarmth(slot);
                        float cooling = slot.Itemstack?.ItemAttributes?["cooling"].AsFloat(0f) ?? 0f;
                        this.clothingCoolingBonus += cooling;
                        wearableItem.ChangeCondition(slot, conditionloss);
                    }
                }
            }

            this.lastWearableHoursTotalUpdate = this.api.World.Calendar.TotalHours;
        }

        public override void OnEntityRevive()
        {
            this.BodyTempUpdateTotalHours = this.api.World.Calendar.TotalHours;
            this.LastWetnessUpdateTotalHours = this.api.World.Calendar.TotalHours;
            this.Wetness = 0f;
            this.CurBodyTemperature = this.NormalBodyTemperature + 4f;
        }
    }
}
