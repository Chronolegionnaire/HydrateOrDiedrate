using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private readonly ICoreAPI _api;
        private readonly Config _config;
        private bool _isShiftHeld = false;
        private static SimpleParticleProperties _waterParticles;
        private static SimpleParticleProperties _whiteParticles;
        private double _lastInteractionTime = 0;

        public WaterInteractionHandler(ICoreAPI api, Config config)
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

        public void CheckPlayerInteraction(float dt)
        {
            double currentTime = _api.World.Calendar.TotalHours * 3600; // convert in-game hours to seconds

            foreach (IServerPlayer player in _api.World.AllOnlinePlayers)
            {
                if (currentTime - _lastInteractionTime < 1) continue; // Skip if cooldown not over

                if (player.Entity.Controls.Sneak && player.Entity.Controls.RightMouseDown)
                {
                    if (!_isShiftHeld && player.Entity.RightHandItemSlot.Empty && !player.Entity.Swimming && !IsHeadInWater(player))
                    {
                        _isShiftHeld = true;
                        _lastInteractionTime = currentTime;
                        HandleWaterInteraction(player);
                    }
                }
                else
                {
                    _isShiftHeld = false;
                }
            }
        }

        private bool IsHeadInWater(IServerPlayer player)
        {
            var headPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            int dimensionId = (int)(player.Entity.ServerPos.Y / BlockPos.DimensionBoundary);
            var headBlockPos = new BlockPos((int)headPos.X, (int)headPos.Y, (int)headPos.Z, dimensionId);
            var block = _api.World.BlockAccessor.GetBlock(headBlockPos);
            return block.BlockMaterial == EnumBlockMaterial.Liquid;
        }

        private void HandleWaterInteraction(IServerPlayer player)
        {
            var blockSel = RayCastForFluidBlocks(player);
            if (blockSel == null) return;
            var block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block.BlockMaterial != EnumBlockMaterial.Liquid) return;
            string liquidCode = block.LiquidCode;
            var adjustedPos = blockSel.HitPosition;
            _api.World.PlaySoundAt(new AssetLocation("sounds/effect/water-pour"), adjustedPos.X, adjustedPos.Y, adjustedPos.Z, null, true, 32f, 1f);
            SpawnWaterParticles(adjustedPos);
            if (liquidCode.StartsWith("boilingwater"))
            {
                if (_config.EnableBoilingWaterDamage) ApplyHeatDamage(player);
                else QuenchThirst(player);
            }
            else if (liquidCode.StartsWith("saltwater"))
            {
                if (_config.EnableSaltWaterThirstIncrease) IncreaseThirst(player);
                else QuenchThirst(player);
            }
            else if (liquidCode.StartsWith("water"))
            {
                QuenchThirst(player);
            }
        }

        private BlockSelection RayCastForFluidBlocks(IServerPlayer player)
        {
            var fromPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            var toPos = fromPos.AheadCopy(5, player.Entity.ServerPos.Pitch, player.Entity.ServerPos.Yaw);
            var step = toPos.Sub(fromPos).Normalize().Mul(0.1);
            var currentPos = fromPos.Clone();
            int dimensionId = (int)(player.Entity.ServerPos.Y / BlockPos.DimensionBoundary);

            while (currentPos.SquareDistanceTo(fromPos) <= 5 * 5) // Check within 5 block distance
            {
                var blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z, dimensionId);
                var block = _api.World.BlockAccessor.GetBlock(blockPos);

                if (block.BlockMaterial == EnumBlockMaterial.Liquid)
                {
                    return new BlockSelection { Position = blockPos, HitPosition = currentPos.Clone() };
                }
                else if (block.BlockMaterial != EnumBlockMaterial.Air)
                {
                    return null;
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
            particles.MinPos.Set(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
            particles.AddPos.Set(addPos, 0.0, addPos);
            particles.GravityEffect = gravityEffect;
            particles.MinVelocity.Set(velocity);
            particles.AddVelocity.Set(0.2f, velocity.Y, 0.2f);

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

        private void QuenchThirst(IPlayer player)
        {
            ModifyThirst(player, _config.RegularWaterThirstDecrease);
        }

        private void IncreaseThirst(IPlayer player)
        {
            ModifyThirst(player, -_config.SaltWaterThirstIncrease);
        }

        public void ModifyThirst(IPlayer player, float amount)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.CurrentThirst += amount;
            }
        }

        private void ApplyHeatDamage(IPlayer player)
        {
            player.Entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heat
            }, _config.BoilingWaterDamage);
        }
    }
}
