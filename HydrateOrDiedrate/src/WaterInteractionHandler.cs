using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using System.Linq;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private readonly ICoreAPI _api;
        private readonly Config.Config _config;
        private bool _isDrinking = false;
        private long _drinkStartTime = 0;
        private long _lastStepTime = 0;
        private const double drinkDuration = 3000; 
        private const double stepInterval = 500;
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
            long currentTime = _api.World.ElapsedMilliseconds;

            foreach (IServerPlayer player in _api.World.AllOnlinePlayers)
            {
                if (player == null || player.Entity == null) continue;
                if ((player.Entity.RightHandItemSlot != null && player.Entity.RightHandItemSlot.Itemstack != null) ||
                    (player.Entity.LeftHandItemSlot != null && player.Entity.LeftHandItemSlot.Itemstack != null))
                {
                    continue;
                }
                if (IsHeadInWater(player))
                {
                    continue;
                }

                if (player.Entity.Controls.Sneak && player.Entity.Controls.RightMouseDown)
                {
                    var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                    var blockSel = RayCastForFluidBlocks(player);
                    if (blockSel == null || thirstBehavior == null ||
                        thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
                    {
                        _isDrinking = false;
                        continue;
                    }

                    var block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (block.BlockMaterial == EnumBlockMaterial.Liquid && !_isDrinking)
                    {
                        _isDrinking = true;
                        _drinkStartTime = currentTime;
                        _lastStepTime = currentTime;

                        HandleDrinkingStep(player, blockSel, currentTime, block);
                    }
                }
                else
                {
                    _isDrinking = false;
                }
                if (_isDrinking)
                {
                    HandleDrinkingStep(player, null, currentTime, null);
                }
            }
        }

        private bool IsHeadInWater(IServerPlayer player)
        {
            var headPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            var headBlockPos = new BlockPos((int)headPos.X, (int)headPos.Y, (int)headPos.Z);
            var block = _api.World.BlockAccessor.GetBlock(headBlockPos);
            return block.BlockMaterial == EnumBlockMaterial.Liquid;
        }

        private void HandleDrinkingStep(IServerPlayer player, BlockSelection blockSel, long currentTime, Block block)
        {
            if (!_isDrinking) return;

            if (blockSel == null)
            {
                blockSel = RayCastForFluidBlocks(player);
                if (blockSel == null)
                {
                    _isDrinking = false;
                    return;
                }

                block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
            }

            if (currentTime - _lastStepTime >= stepInterval)
            {
                _lastStepTime = currentTime;

                var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                var hungerBehavior = player.Entity.GetBehavior<Vintagestory.GameContent.EntityBehaviorHunger>();

                if (thirstBehavior == null || thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
                {
                    _isDrinking = false;
                    return;
                }

                var blockHydrationConfig = BlockHydrationManager.GetBlockHydration(block.Code.Path);

                if (blockHydrationConfig != null)
                {
                    bool isBoiling = blockHydrationConfig.IsBoiling;
                    float hydrationValue = blockHydrationConfig.HydrationByType.ContainsKey("*") ? blockHydrationConfig.HydrationByType["*"] : 0f;
                    float hungerReduction = blockHydrationConfig.HungerReduction;

                    thirstBehavior.ModifyThirst(hydrationValue);

                    if (hungerBehavior != null && hungerBehavior.Saturation > hungerReduction)
                    {
                        hungerBehavior.Saturation -= hungerReduction;
                        thirstBehavior.HungerReductionAmount += hungerReduction;
                    }

                    if (isBoiling && _config.EnableBoilingWaterDamage)
                    {
                        ApplyHeatDamage(player);
                    }
                }

                _api.World.PlaySoundAt(new AssetLocation("sounds/effect/water-pour"), blockSel.HitPosition.X, blockSel.HitPosition.Y, blockSel.HitPosition.Z, null, true, 32f, 1f);
                SpawnWaterParticles(blockSel.HitPosition);

                if (currentTime - _drinkStartTime >= drinkDuration)
                {
                    _isDrinking = false;
                }
            }
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

                if (player is IClientPlayer clientPlayer && IsLookingAtInteractable(clientPlayer))
                {
                    return null;
                }

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

        private bool IsLookingAtInteractable(IClientPlayer clientPlayer)
        {
            if (clientPlayer == null || clientPlayer.CurrentBlockSelection == null) return false;

            if (clientPlayer.CurrentBlockSelection.Block?.HasBehavior<BlockBehaviorBlockEntityInteract>() ??
                clientPlayer.CurrentBlockSelection.Block is BlockGroundStorage)
                return true;

            return clientPlayer.Entity.EntitySelection?.Entity?.IsInteractable ?? false;
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
    }
}
