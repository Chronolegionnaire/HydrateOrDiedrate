﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private readonly ICoreAPI _api;
        private readonly Config.Config _config;
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
            
            if (!player.Entity.WatchedAttributes.GetBool("isFullyInitialized", false)) return;
            
            CheckPlayerInteraction(dt, player);
        }


        public void CheckPlayerInteraction(float dt, IServerPlayer player)
        {
            long currentTime = _api.World.ElapsedMilliseconds;

            if (!playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData))
            {
                drinkData = new PlayerDrinkData();
                playerDrinkData[player.PlayerUID] = drinkData;
            }

            if ((player.Entity.RightHandItemSlot?.Itemstack != null) ||
                (player.Entity.LeftHandItemSlot?.Itemstack != null))
            {
                StopDrinking(player, drinkData);
                return;
            }

            if (IsHeadInWater(player))
            {
                StopDrinking(player, drinkData);
                return;
            }

            if (player.Entity.Controls.Sneak && player.Entity.Controls.RightMouseDown)
            {
                var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                var hungerBehavior = player.Entity.GetBehavior<EntityBehaviorHunger>();
                var blockSel = RayCastForFluidBlocks(player);

                if (blockSel == null || thirstBehavior == null || hungerBehavior == null ||
                    thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
                {
                    StopDrinking(player, drinkData);
                    return;
                }

                var block = player.Entity.World.BlockAccessor.GetBlock(blockSel.Position);
                var blockHydrationConfig = BlockHydrationManager.GetBlockHydration(block.Code.Path);

                if (block.BlockMaterial == EnumBlockMaterial.Liquid && blockHydrationConfig != null)
                {
                    float hungerReduction = blockHydrationConfig.HungerReduction;

                    // Prevent drinking if the player's hunger (satiety) is too low to handle the hunger reduction
                    if (hungerBehavior.Saturation <= 0 || hungerBehavior.Saturation < hungerReduction)
                    {
                        // Not enough hunger to drink
                        StopDrinking(player, drinkData);
                        return;
                    }

                    // If the player has enough hunger, continue with drinking process
                    if (!drinkData.IsDrinking)
                    {
                        drinkData.IsDrinking = true;
                        drinkData.DrinkStartTime = currentTime;
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

        private void HandleDrinkingStep(IServerPlayer player, BlockSelection blockSel, long currentTime, Block block,
            PlayerDrinkData drinkData)
        {
            if (!drinkData.IsDrinking) return;
            float progress = (float)(currentTime - drinkData.DrinkStartTime) / (float)drinkDuration;
            progress = Math.Min(1f, progress);
            bool isDangerous = false;
            var blockHydrationConfig = BlockHydrationManager.GetBlockHydration(block.Code.Path);
            if (blockHydrationConfig != null)
            {
                float hydrationValue = blockHydrationConfig.HydrationByType.ContainsKey("*")
                    ? blockHydrationConfig.HydrationByType["*"]
                    : 0f;
                isDangerous =
                    hydrationValue < 0 || blockHydrationConfig.IsBoiling;
            }

            SendDrinkProgressToClient(player, progress, drinkData.IsDrinking, isDangerous);

            if (progress >= 1f)
            {
                SendDrinkProgressToClient(player, 1f, drinkData.IsDrinking, isDangerous);
                var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
                var hungerBehavior = player.Entity.GetBehavior<EntityBehaviorHunger>();

                if (thirstBehavior == null || thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
                {
                    StopDrinking(player, drinkData);
                    return;
                }

                if (blockHydrationConfig != null)
                {
                    bool isBoiling = blockHydrationConfig.IsBoiling;
                    float hydrationValue = blockHydrationConfig.HydrationByType.ContainsKey("*")
                        ? blockHydrationConfig.HydrationByType["*"]
                        : 0f;
                    float hungerReduction = blockHydrationConfig.HungerReduction;

                    // Modify thirst regardless of hunger state
                    thirstBehavior.ModifyThirst(hydrationValue);

                    // Check if hungerBehavior is not null and handle hunger reduction logic
                    if (hungerBehavior != null)
                    {
                        if (hungerBehavior.Saturation >= hungerReduction)
                        {
                            // If the player has enough satiety to reduce by hungerReduction, do so
                            hungerBehavior.Saturation -= hungerReduction;
                            thirstBehavior.HungerReductionAmount += hungerReduction;
                        }
                        else if (hungerBehavior.Saturation > 0)
                        {
                            // If the player has some satiety but not enough to meet the full hungerReduction, reduce to zero
                            thirstBehavior.HungerReductionAmount += hungerBehavior.Saturation;
                            hungerBehavior.Saturation = 0;
                        }
                        else
                        {
                            // If the player's satiety is already zero, prevent further drinking
                            StopDrinking(player, drinkData);
                            return;
                        }
                    }

                    if (isBoiling && _config.EnableBoilingWaterDamage)
                    {
                        ApplyHeatDamage(player);
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
                var blockPos = new BlockPos((int)currentPos.X, (int)currentPos.Y, (int)currentPos.Z, (int)currentPos.Y/32768);
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
    }
}