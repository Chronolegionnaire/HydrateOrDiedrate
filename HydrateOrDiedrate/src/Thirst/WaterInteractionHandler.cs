using System;
using System.Collections.Generic;
using System.Linq;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.wellwater;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private ICoreAPI _api;

        private IServerNetworkChannel serverChannel;
        private static readonly HashSet<string> allowedBlockCodes = new HashSet<string>
        {
            "wellwater",
            "water",
            "boilingwater",
            "saltwater",
        };

        private class PlayerDrinkData
        {
            public bool IsDrinking = false;
            public long DrinkStartTime = 0;
        }

        private Dictionary<string, PlayerDrinkData> playerDrinkData = new Dictionary<string, PlayerDrinkData>();
        private const double drinkDuration = 1000;

        private static SimpleParticleProperties _waterParticles;
        private static SimpleParticleProperties _whiteParticles;

        public WaterInteractionHandler(ICoreAPI api)
        {
            _api = api;

            _waterParticles = CreateParticleProperties(
                ColorUtil.WhiteArgb,
                new Vec3f(-0.25f, 0, -0.25f),
                new Vec3f(0.25f, 0.5f, 0.25f),
                "climateWaterTint"
            );
            _whiteParticles = CreateParticleProperties(
                ColorUtil.ColorFromRgba(255, 255, 255, 128),
                new Vec3f(-0.1f, 0, -0.1f),
                new Vec3f(0.1f, 0.2f, 0.1f)
            );
        }

        public void Initialize(IServerNetworkChannel channel)
        {
            serverChannel = channel;
        }
        
        public void OnPlayerDisconnect(IServerPlayer player)
        {
            if (player?.PlayerUID != null)
            {
                playerDrinkData.Remove(player.PlayerUID);
            }
        }

        private SimpleParticleProperties CreateParticleProperties(int color, Vec3f minVelocity, Vec3f maxVelocity, string climateColorMap = null)
        {
            var particles = new SimpleParticleProperties(
                1, 1, color, new Vec3d(), new Vec3d(),
                minVelocity, maxVelocity, 0.1f, 0.1f, 0.5f, 1f, EnumParticleModel.Cube
            );
            particles.AddPos = new Vec3d(0.125 / 2, 2 / 16f, 0.125 / 2);
            particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);
            particles.AddQuantity = 1;
            particles.ShouldDieInLiquid = true;
            particles.ShouldSwimOnLiquid = true;
            particles.GravityEffect = 1.5f;
            particles.LifeLength = 2f;

            if (climateColorMap != null)
            {
                particles.ClimateColorMap = climateColorMap;
            }

            return particles;
        }

        public void CheckShiftRightClickBeforeInteractionForPlayer(float dt, IServerPlayer player)
        {
            if (player == null || player.Entity == null) return;
            CheckPlayerInteraction(dt, player);
        }

        private void StopDrinking(IServerPlayer player, PlayerDrinkData drinkData)
        {
            if (drinkData.IsDrinking)
            {
                drinkData.IsDrinking = false;
                drinkData.DrinkStartTime = 0;
                SendDrinkProgressToClient(player, 0f, false, false);
            }
        }

        private bool IsHeadInWater(IServerPlayer player)
        {
            var headPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            var headBlockPos = new BlockPos((int)headPos.X, (int)headPos.Y, (int)headPos.Z, (int)headPos.Y / 32768);
            var block = _api.World.BlockAccessor.GetBlock(headBlockPos);
            return block.BlockMaterial == EnumBlockMaterial.Liquid;
        }

        public void CheckPlayerInteraction(float dt, IServerPlayer player)
        {
            try
            {
                long currentTime = _api.World.ElapsedMilliseconds;
                if (!playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData))
                {
                    drinkData = new PlayerDrinkData();
                    playerDrinkData[player.PlayerUID] = drinkData;
                }

                bool drinkingKey = ModConfig.Instance.SprintToDrink ? player.Entity.Controls.Sprint : player.Entity.Controls.Sneak;
                if (drinkingKey && player.Entity.Controls.RightMouseDown)
                {
                    var blockSel = RayCastForFluidBlocks(player);
                    if (blockSel?.Position == null)
                    {
                        StopDrinking(player, drinkData);
                        return;
                    }

                    var block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (block.GetType().GetInterface("IAqueduct") != null || block.Code.Path.StartsWith("furrowedland"))
                    {
                        var fluidBlock =
                            player.Entity.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Fluid);
                        if (fluidBlock != null && fluidBlock.BlockMaterial == EnumBlockMaterial.Liquid)
                        {
                            block = fluidBlock;
                        }
                        else
                        {
                            StopDrinking(player, drinkData);
                            return;
                        }
                    }

                    if (block.BlockMaterial == EnumBlockMaterial.Liquid &&
                        ((player.Entity.RightHandItemSlot?.Itemstack != null) ||
                         (player.Entity.LeftHandItemSlot?.Itemstack != null)))
                    {
                        player.SendIngameError("handsfull", Lang.Get("hydrateordiedrate:waterinteraction-handsfree"));
                        StopDrinking(player, drinkData);
                        return;
                    }

                    if (IsHeadInWater(player))
                    {
                        StopDrinking(player, drinkData);
                        return;
                    }

                    if (block == null || block.Code == null || block.Code.Path == null ||
                        (!allowedBlockCodes.Any(prefix => block.Code.Path.StartsWith(prefix)) &&
                         !block.Code.Path.StartsWith("furrowedland") &&
                         block.GetType().GetInterface("IAqueduct") == null))
                    {
                        StopDrinking(player, drinkData);
                        return;
                    }

                    float hydrationValue = BlockHydrationManager.GetHydrationValue(block, "*");
                    if (hydrationValue != 0)
                    {
                        if (!drinkData.IsDrinking)
                        {
                            drinkData.IsDrinking = true;
                            drinkData.DrinkStartTime = currentTime;
                            bool isDangerous = hydrationValue < 0 || BlockHydrationManager.IsBlockBoiling(block);
                            SendDrinkProgressToClient(player, 0f, true, isDangerous);
                        }

                        HandleDrinkingStep(player, blockSel, currentTime, block, drinkData);
                    }
                    else
                    {
                        StopDrinking(player, drinkData);
                    }
                }
                else
                {
                    StopDrinking(player, drinkData);
                }
            }
            catch (Exception ex)
            {
                if (playerDrinkData.TryGetValue(player.PlayerUID, out var dd))
                {
                    StopDrinking(player, dd);
                }
            }
        }

        private void HandleDrinkingStep(IServerPlayer player, BlockSelection blockSel, long currentTime,
            CollectibleObject collectible, PlayerDrinkData drinkData)
        {
            if (!drinkData.IsDrinking) return;

            if (collectible == null)
            {
                StopDrinking(player, drinkData);
                return;
            }
            
            float progress = (float)(currentTime - drinkData.DrinkStartTime) / (float)drinkDuration;
            progress = Math.Min(1f, progress);

            float hydrationValue = BlockHydrationManager.GetHydrationValue(collectible, "*");
            bool isBoiling = BlockHydrationManager.IsBlockBoiling(collectible);
            int hungerReduction = BlockHydrationManager.GetBlockHungerReduction(collectible);
            int healthEffect = BlockHydrationManager.GetBlockHealth(collectible);

            bool isDangerous = hydrationValue < 0 || isBoiling;

            if (progress >= 1f)
            {
                SendDrinkProgressToClient(player, 1f, false, isDangerous);

                var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                var hungerBehavior = player.Entity.GetBehavior<EntityBehaviorHunger>();

                if (thirstBehavior == null || thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
                {
                    player.SendIngameError("fullhydration",
                        Lang.Get("hydrateordiedrate:waterinteraction-fullhydration"));
                    StopDrinking(player, drinkData);
                    return;
                }

                thirstBehavior.ModifyThirst(hydrationValue);

                if (hungerBehavior != null)
                {
                    if (hungerBehavior.Saturation >= hungerReduction)
                    {
                        hungerBehavior.Saturation -= hungerReduction;
                        thirstBehavior.HungerReductionAmount += hungerReduction;
                    }
                    else
                    {
                        thirstBehavior.HungerReductionAmount += hungerReduction;
                    }
                }

                if (healthEffect != 0)
                {
                    var damageSource = new DamageSource
                    {
                        Source = EnumDamageSource.Internal,
                        Type = healthEffect > 0 ? EnumDamageType.Heal : EnumDamageType.Poison
                    };

                    float damageAmount = Math.Abs(healthEffect);
                    player.Entity.ReceiveDamage(damageSource, damageAmount);
                }

                if (isBoiling) ApplyHeatDamage(player, ModConfig.Instance.Thirst.BoilingWaterDamage);

                var block = _api.World.BlockAccessor.GetBlock(blockSel.Position);
                if (block.Code.Path.StartsWith("wellwater"))
                {
                    var blockBehavior = block.GetBehavior<BlockBehaviorWellWaterFinite>();
                    var naturalSourcePos =
                        blockBehavior?.FindNaturalSourceInLiquidChain(_api.World.BlockAccessor, blockSel.Position);
                    if (naturalSourcePos != null)
                    {
                        var blockEntity = _api.World.BlockAccessor.GetBlockEntity(naturalSourcePos);
                        if (blockEntity is BlockEntityWellWaterData wellWaterData)
                        {
                            wellWaterData.Volume -= 1;
                            int afterVolume = wellWaterData.Volume;
                            if (afterVolume <= 0)
                            {
                                StopDrinking(player, drinkData);
                                return;
                            }
                        }
                    }
                }

                _api.World.PlaySoundAt(new AssetLocation("sounds/effect/water-pour"), blockSel.HitPosition.X,
                    blockSel.HitPosition.Y, blockSel.HitPosition.Z, null, true, 32f, 1f);
                SpawnWaterParticles(blockSel.HitPosition);
                if (player.Entity.Controls.RightMouseDown)
                {
                    drinkData.DrinkStartTime = currentTime;
                    SendDrinkProgressToClient(player, 0f, true, isDangerous);
                }
                else
                {
                    StopDrinking(player, drinkData);
                }
            }
        }

        public void SendDrinkProgressToClient(IServerPlayer player, float progress, bool isDrinking, bool isDangerous)
        {
            if (serverChannel != null && player != null)
            {
                serverChannel.SendPacket(
                    new DrinkProgressPacket { Progress = progress, IsDrinking = isDrinking, IsDangerous = isDangerous },
                    player
                );
            }
        }

        private static void ApplyHeatDamage(IServerPlayer player, float boilingWaterDamage)
        {
            if(boilingWaterDamage == 0) return;
            
            player.Entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heat
            }, boilingWaterDamage);
        }

        private BlockSelection RayCastForFluidBlocks(IServerPlayer player)
        {
            if (!(_api is ICoreServerAPI)) return null;
            if (player?.Entity == null) return null;

            var world = _api.World;
            var accessor = world?.BlockAccessor;
            if (accessor == null) return null;

            Vec3d eyePos = player.Entity.ServerPos.XYZ
                .Add(0, player.Entity.LocalEyePos.Y, 0);
            Vec3d direction = eyePos
                .AheadCopy(1, player.Entity.ServerPos.Pitch, player.Entity.ServerPos.Yaw)
                .Sub(eyePos)
                .Normalize();
            float maxDistance = 5f;

            int x = (int)Math.Floor(eyePos.X);
            int y = (int)Math.Floor(eyePos.Y);
            int z = (int)Math.Floor(eyePos.Z);

            double epsilon = 1e-6;
            double deltaX = Math.Abs(direction.X) > epsilon ? 1.0 / Math.Abs(direction.X) : double.MaxValue;
            double deltaY = Math.Abs(direction.Y) > epsilon ? 1.0 / Math.Abs(direction.Y) : double.MaxValue;
            double deltaZ = Math.Abs(direction.Z) > epsilon ? 1.0 / Math.Abs(direction.Z) : double.MaxValue;

            double tMaxX = (Math.Sign(direction.X) > 0 ? (x + 1 - eyePos.X) : (eyePos.X - x)) * deltaX;
            double tMaxY = (Math.Sign(direction.Y) > 0 ? (y + 1 - eyePos.Y) : (eyePos.Y - y)) * deltaY;
            double tMaxZ = (Math.Sign(direction.Z) > 0 ? (z + 1 - eyePos.Z) : (eyePos.Z - z)) * deltaZ;
            double t = 0;

            while (t < maxDistance)
            {
                var pos = new BlockPos(x, y, z);
                if (!accessor.IsValidPos(pos)) break;

                var block = accessor.GetBlock(pos);
                if (block != null
                    && block.Code != null
                    && (block.BlockMaterial == EnumBlockMaterial.Liquid
                        || block.Code.Path.StartsWith("furrowedland")
                        || block.GetType().GetInterface("IAqueduct") != null))
                {
                    var hitPos = eyePos.Add(direction.Mul(t));
                    return new BlockSelection
                    {
                        Position = pos,
                        HitPosition = hitPos
                    };
                }
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        x += Math.Sign(direction.X);
                        t = tMaxX;
                        tMaxX += deltaX;
                    }
                    else
                    {
                        z += Math.Sign(direction.Z);
                        t = tMaxZ;
                        tMaxZ += deltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        y += Math.Sign(direction.Y);
                        t = tMaxY;
                        tMaxY += deltaY;
                    }
                    else
                    {
                        z += Math.Sign(direction.Z);
                        t = tMaxZ;
                        tMaxZ += deltaZ;
                    }
                }
            }

            return null;
        }

        private void SpawnWaterParticles(Vec3d pos)
        {
            _waterParticles.MinPos = new Vec3d(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
            _waterParticles.AddPos = new Vec3d(0.4, 0.0, 0.4);
            _waterParticles.GravityEffect = 1.5f;
            _waterParticles.MinVelocity = new Vec3f(0, 0.8f, 0);
            _waterParticles.AddVelocity = new Vec3f(0.2f, 0.8f, 0.2f);

            float colorModifier = (float)_api.World.Rand.NextDouble() * 0.3f;
            _waterParticles.Color = ColorUtil.ColorFromRgba(
                185 + (int)(colorModifier * 70f),
                145 + (int)(colorModifier * 110f),
                50 + (int)(colorModifier * 205f),
                130 + (int)(colorModifier * 30f)
            );
            _waterParticles.AddQuantity = 10;
            _whiteParticles.MinPos = new Vec3d(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
            _whiteParticles.AddPos = new Vec3d(0.4, 0.0, 0.4);
            _whiteParticles.GravityEffect = 1.5f;
            _whiteParticles.MinVelocity = new Vec3f(0, 0.8f, 0);
            _whiteParticles.AddVelocity = new Vec3f(0.2f, 0.8f, 0.2f);
            _whiteParticles.AddQuantity = 5;
            _api.World.SpawnParticles(_waterParticles, null);
            _api.World.SpawnParticles(_whiteParticles, null);
        }
    }
}
