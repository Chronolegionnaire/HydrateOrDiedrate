using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private readonly ICoreAPI _api;
        private readonly Config _config;
        private bool isShiftHeld = false;
        private static SimpleParticleProperties waterParticles;

        public WaterInteractionHandler(ICoreAPI api, Config config)
        {
            _api = api;
            _config = config;
            InitWaterParticles();
        }

        private void InitWaterParticles()
        {
            waterParticles = new SimpleParticleProperties(
                1, 1, ColorUtil.WhiteArgb, new Vec3d(), new Vec3d(),
                new Vec3f(-1.5f, 0, -1.5f), new Vec3f(1.5f, 3f, 1.5f), 1f, 1f, 0.33f, 0.75f, EnumParticleModel.Cube
            );

            waterParticles.AddPos = new Vec3d(0.125 / 2, 2 / 16f, 0.125 / 2);
            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -1f);
            waterParticles.ClimateColorMap = "climateWaterTint";
            waterParticles.AddQuantity = 1;
        }

        public void CheckPlayerInteraction(float dt)
        {
            foreach (IServerPlayer player in _api.World.AllOnlinePlayers)
            {
                if (player.Entity.Controls.Sneak && player.Entity.Controls.RightMouseDown)
                {
                    if (!isShiftHeld && player.Entity.RightHandItemSlot.Empty)
                    {
                        isShiftHeld = true;
                        HandleWaterInteraction(player);
                    }
                }
                else
                {
                    isShiftHeld = false;
                }
            }
        }

        public void HandleWaterInteraction(IServerPlayer player)
        {
            EntityAgent byEntity = player.Entity;

            BlockSelection blockSel = RayCastForFluidBlocks(player);
            if (blockSel == null)
            {
                return;
            }

            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block.BlockMaterial != EnumBlockMaterial.Liquid)
            {
                return;
            }

            string liquidCode = block.LiquidCode;

            // Adjust the position slightly above the intersection point
            Vec3d adjustedPos = blockSel.Position.ToVec3d().Add(0.5, 0.0625, 0.5);

            // Play small splash sound
            _api.Logger.Debug("Playing splash sound at {0}, {1}, {2}", adjustedPos.X, adjustedPos.Y, adjustedPos.Z);
            PlaySoundAt("sounds/environment/smallsplash.ogg", adjustedPos.X, adjustedPos.Y, adjustedPos.Z, player);

            // Spawn water particles
            _api.Logger.Debug("Spawning water particles at {0}, {1}, {2}", adjustedPos.X, adjustedPos.Y, adjustedPos.Z);
            SpawnWaterParticles(adjustedPos, player);

            if (liquidCode.StartsWith("boilingwater"))
            {
                if (_config.EnableBoilingWaterDamage)
                {
                    ApplyHeatDamage(player);
                }
                else
                {
                    QuenchThirst(player);
                }
            }
            else if (liquidCode.StartsWith("saltwater"))
            {
                if (_config.EnableSaltWaterThirstIncrease)
                {
                    IncreaseThirst(player);
                }
                else
                {
                    QuenchThirst(player);
                }
            }
            else if (liquidCode.StartsWith("water"))
            {
                QuenchThirst(player);
            }
        }

        private BlockSelection RayCastForFluidBlocks(IServerPlayer player)
        {
            Vec3d fromPos = player.Entity.ServerPos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
            Vec3d toPos = fromPos.AheadCopy(5, player.Entity.ServerPos.Pitch, player.Entity.ServerPos.Yaw); // 5 blocks distance

            Vec3d step = toPos.Sub(fromPos).Normalize().Mul(0.1); // Small steps for accuracy
            Vec3d currentPos = fromPos.Clone();
            BlockSelection blockSel = new BlockSelection();

            while (currentPos.SquareDistanceTo(fromPos) <= toPos.SquareDistanceTo(fromPos))
            {
                BlockPos blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z);
                Block block = _api.World.BlockAccessor.GetBlock(blockPos);
                if (block.BlockMaterial == EnumBlockMaterial.Liquid)
                {
                    blockSel.Position = blockPos;
                    return blockSel;
                }

                currentPos.Add(step);
            }

            return null;
        }

        private void SpawnWaterParticles(Vec3d pos, IPlayer player)
        {
            waterParticles.MinPos = pos;
            (_api.World as IServerWorldAccessor)?.SpawnParticles(waterParticles, player);
        }

        private void QuenchThirst(IPlayer player)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.CurrentThirst += _config.RegularWaterThirstDecrease;
            }
        }

        private void IncreaseThirst(IPlayer player)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.CurrentThirst -= _config.SaltWaterThirstIncrease;
            }
        }

        private void ApplyHeatDamage(IPlayer player)
        {
            player.Entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heat
            }, _config.BoilingWaterDamage);
        }

        private void PlaySoundAt(string location, double posx, double posy, double posz, IPlayer player)
        {
            var serverApi = _api as ICoreServerAPI;
            if (serverApi != null)
            {
                serverApi.World.PlaySoundAt(new AssetLocation(location), posx, posy, posz, player, true, 32f, 1f);
            }
        }
    }
}
