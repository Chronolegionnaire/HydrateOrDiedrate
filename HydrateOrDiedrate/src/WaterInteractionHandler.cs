using System;
using System.Collections.Generic;
using HydrateOrDiedrate.wellwater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private ICoreAPI _api;
        private Config.Config _config;
        private IServerNetworkChannel serverChannel;

        private class PlayerDrinkData
        {
            public bool IsDrinking = false;
            public long DrinkStartTime = 0;
        }

        private Dictionary<string, PlayerDrinkData> playerDrinkData = new Dictionary<string, PlayerDrinkData>();
        private const double drinkDuration = 1000;

        private static SimpleParticleProperties _waterParticles;
        private static SimpleParticleProperties _whiteParticles;

        public WaterInteractionHandler(ICoreAPI api, Config.Config config)
        {
            _api = api;
            _config = config;

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

        public void Reset(Config.Config newConfig)
        {
            _config = newConfig;
        }

        public void Initialize(IServerNetworkChannel channel)
        {
            serverChannel = channel;
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
                SendDrinkProgressToClient(player, 0f, false,false);
            }
        }

        private bool IsHeadInWater(IServerPlayer player)
        {
            var headPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            var headBlockPos = new BlockPos((int)headPos.X, (int)headPos.Y, (int)headPos.Z, (int)headPos.Y/32768);
            var block = _api.World.BlockAccessor.GetBlock(headBlockPos);
            return block.BlockMaterial == EnumBlockMaterial.Liquid;
        }

        public void CheckPlayerInteraction(float dt, IServerPlayer player)
        {
            long currentTime = _api.World.ElapsedMilliseconds;
            if (!playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData))
            {
                drinkData = new PlayerDrinkData();
                playerDrinkData[player.PlayerUID] = drinkData;
            }

            if (player.Entity.Controls.Sneak && player.Entity.Controls.RightMouseDown)
            {
                var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                var hungerBehavior = player.Entity.GetBehavior<EntityBehaviorHunger>();
                var blockSel = RayCastForFluidBlocks(player);
                if (blockSel == null)
                {
                    StopDrinking(player, drinkData);
                    return;
                }
                var block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
                if (block.BlockMaterial == EnumBlockMaterial.Liquid &&
                    ((player.Entity.RightHandItemSlot?.Itemstack != null) ||
                     (player.Entity.LeftHandItemSlot?.Itemstack != null)))
                {
                    player.SendIngameError("handsfull", "You must have both hands free to drink.");
                    StopDrinking(player, drinkData);
                    return;
                }
                if (IsHeadInWater(player))
                {
                    StopDrinking(player, drinkData);
                    return;
                }

                var collectible = GetCollectibleObject(block);
                if (collectible == null)
                {
                    StopDrinking(player, drinkData);
                    return;
                }

                float hydrationValue = BlockHydrationManager.GetHydrationValue(collectible, "*");
                if (hydrationValue != 0)
                {
                    if (!drinkData.IsDrinking)
                    {
                        drinkData.IsDrinking = true;
                        drinkData.DrinkStartTime = currentTime;
                    }

                    HandleDrinkingStep(player, blockSel, currentTime, collectible, drinkData);
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

        private CollectibleObject GetCollectibleObject(Block block)
        {
            if (block == null)
            {
                return null;
            }

            var collectible = _api.World.Collectibles?.Find(c => c.Code.Path == block.Code.Path);
            return collectible;
        }


        private void HandleDrinkingStep(IServerPlayer player, BlockSelection blockSel, long currentTime, CollectibleObject collectible, PlayerDrinkData drinkData)
        {
            if (!drinkData.IsDrinking) return;

            float progress = (float)(currentTime - drinkData.DrinkStartTime) / (float)drinkDuration;
            progress = Math.Min(1f, progress);

            float hydrationValue = BlockHydrationManager.GetHydrationValue(collectible, "*");
            bool isBoiling = BlockHydrationManager.IsBlockBoiling(collectible);
            int hungerReduction = BlockHydrationManager.GetBlockHungerReduction(collectible);
            int healthEffect = BlockHydrationManager.GetBlockHealth(collectible);

            bool isDangerous = hydrationValue < 0 || isBoiling;

            SendDrinkProgressToClient(player, progress, drinkData.IsDrinking, isDangerous);

            if (progress >= 1f)
            {
                SendDrinkProgressToClient(player, 1f, drinkData.IsDrinking, isDangerous);
                var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                var hungerBehavior = player.Entity.GetBehavior<EntityBehaviorHunger>();

                if (thirstBehavior == null || thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
                {
                    player.SendIngameError("fullhydration", "You are already fully hydrated!");
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

                if (isBoiling && _config.EnableBoilingWaterDamage)
                {
                    ApplyHeatDamage(player);
                }
                var block = _api.World.BlockAccessor.GetBlock(blockSel.Position);
                if (block.Code.Path.StartsWith("wellwater"))
                {
                    var blockBehavior = block.GetBehavior<BlockBehaviorWellWaterFinite>();
                    var naturalSourcePos = blockBehavior?.FindNaturalSourceInLiquidChain(
                        _api.World.BlockAccessor,
                        blockSel.Position
                    );
                    if (naturalSourcePos != null)
                    {
                        var blockEntity = _api.World.BlockAccessor.GetBlockEntity(naturalSourcePos);
                        if (blockEntity is BlockEntityWellWaterData wellWaterData)
                        {
                            int beforeVolume = wellWaterData.Volume;
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
                drinkData.DrinkStartTime = currentTime;
                SendDrinkProgressToClient(player, 0f, drinkData.IsDrinking, isDangerous);
            }
        }

        public void SendDrinkProgressToClient(IServerPlayer player, float progress, bool isDrinking, bool isDangerous)
        {
            serverChannel.SendPacket(new DrinkProgressPacket { Progress = progress, IsDrinking = isDrinking, IsDangerous = isDangerous }, player);
        }

        private void ApplyHeatDamage(IServerPlayer player)
        {
            player.Entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heat
            }, _config.BoilingWaterDamage);
        }

        private BlockSelection RayCastForFluidBlocks(IServerPlayer player)
        {
            var fromPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            var toPos = fromPos.AheadCopy(5, player.Entity.ServerPos.Pitch, player.Entity.ServerPos.Yaw);
            var step = toPos.Sub(fromPos).Normalize().Mul(0.1);
            var currentPos = fromPos.Clone();

            while (currentPos.SquareDistanceTo(fromPos) <= 5 * 5)
            {
                var blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z);
                var block = _api.World.BlockAccessor.GetBlock(blockPos);

                if (block.BlockMaterial == EnumBlockMaterial.Liquid)
                {
                    return new BlockSelection { Position = blockPos, HitPosition = currentPos.Clone() };
                }
                else
                {
                    var blockType = block.GetType();
                    var aqueductInterface = blockType.GetInterface("IAqueduct");
                    if (aqueductInterface != null)
                    {
                        var fluidBlock = _api.World.BlockAccessor.GetBlock(blockPos, BlockLayersAccess.Fluid);
                        if (fluidBlock != null && fluidBlock.LiquidLevel > 0)
                        {
                            return new BlockSelection { Position = blockPos, HitPosition = currentPos.Clone() };
                        }
                    }

                    if (block.BlockMaterial != EnumBlockMaterial.Air)
                    {
                        return null;
                    }
                }

                currentPos.Add(step);
            }
            return null;
        }

        private void SpawnWaterParticles(Vec3d pos)
        {
            SetParticleProperties(_waterParticles, pos, 0.4, 10, 1.5f, new Vec3f(0, 0.8f, 0), true);
            SetParticleProperties(_whiteParticles, pos, 0.4, 5, 1.5f, new Vec3f(0, 0.8f, 0));
        }

        private void SetParticleProperties(SimpleParticleProperties particles, Vec3d pos, double addPos, int quantity, float gravityEffect, Vec3f velocity, bool randomizeColor = false)
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
                if (randomizeColor)
                {
                    float colorModifier = (float)_api.World.Rand.NextDouble() * 0.3f;
                    particles.Color = ColorUtil.ColorFromRgba(
                        185 + (int)(colorModifier * 70f),
                        145 + (int)(colorModifier * 110f),
                        50 + (int)(colorModifier * 205f),
                        130 + (int)(colorModifier * 30f)
                    );
                }
                _api.World.SpawnParticles(particles, null);
            }
        }
    }
}
