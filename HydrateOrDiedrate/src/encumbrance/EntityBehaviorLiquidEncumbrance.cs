﻿using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.encumbrance
{
    public class EntityBehaviorLiquidEncumbrance : Vintagestory.API.Common.Entities.EntityBehavior
    {
        private Config.Config _config;
        private int _tickCounter;
        private float _currentPenaltyAmount;
        private bool _isPenaltyApplied;
        private ICoreServerAPI _serverApi;

        public bool IsPenaltyApplied => _isPenaltyApplied;

        public EntityBehaviorLiquidEncumbrance(Entity entity) : base(entity)
        {
            _serverApi = entity.Api as ICoreServerAPI;

            if (_serverApi != null)
            {
                _config = ModConfig.ReadConfig<Config.Config>(_serverApi, "HydrateOrDiedrateConfig.json");
            }
            _tickCounter = 0;
            _currentPenaltyAmount = 0f;
            _isPenaltyApplied = false;
        }

        public override void OnGameTick(float deltaTime)
        {
            if (!entity.Alive || !_config.EnableLiquidEncumbrance) return;

            var player = entity as EntityPlayer;
            if (player?.Player?.WorldData?.CurrentGameMode != EnumGameMode.Survival) return;

            _tickCounter++;
            if (_tickCounter >= 30)
            {
                CheckInventoryForEncumbrance();
                _tickCounter = 0;
            }
        }

        private void CheckInventoryForEncumbrance()
        {
            var player = entity as EntityPlayer;
            if (player?.Player == null) return;

            var inventoryManager = player.Player.InventoryManager;
            if (inventoryManager == null) return;

            bool isEncumbered = false;
            
            var backpackInventory = inventoryManager.GetOwnInventory("backpack");
            if (backpackInventory != null)
            {
                isEncumbered = CheckInventorySlots(backpackInventory);
            }
            
            var hotbarInventory = inventoryManager.GetOwnInventory("hotbar");
            if (!isEncumbered && hotbarInventory != null)
            {
                isEncumbered = CheckInventorySlots(hotbarInventory);
            }

            if (isEncumbered)
            {
                ApplyMovementSpeedPenalty(_config.LiquidEncumbranceMovementSpeedDebuff);
            }
            else
            {
                RemoveMovementSpeedPenalty();
            }
        }

        private bool CheckInventorySlots(IInventory inventory)
        {
            foreach (var slot in inventory)
            {
                if (slot?.Itemstack == null) continue;

                var attributes = slot.Itemstack.Collectible?.Attributes;
                if (attributes == null) continue;

                var isLiquidContainer = slot.Itemstack.Block is BlockLiquidContainerBase;
                if (isLiquidContainer)
                {
                    float totalLitresInStack = GetTotalLitresInStack(slot.Itemstack);

                    if (totalLitresInStack > _config.EncumbranceLimit)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private float GetTotalLitresInStack(ItemStack itemStack)
        {
            BlockLiquidContainerBase block = itemStack.Block as BlockLiquidContainerBase;
            if (block == null) return 0f;
            
            float litresPerContainer = block.GetCurrentLitres(itemStack);
            
            int stackSize = itemStack.StackSize;

            return litresPerContainer * stackSize;
        }


        private float GetCurrentLitres(ItemStack itemStack)
        {
            BlockLiquidContainerBase block = itemStack.Block as BlockLiquidContainerBase;
            if (block == null) return 0f;

            return block.GetCurrentLitres(itemStack);
        }

        private void ApplyMovementSpeedPenalty(float penaltyAmount)
        {
            if (_currentPenaltyAmount == penaltyAmount && _isPenaltyApplied) return;

            _currentPenaltyAmount = penaltyAmount;
            UpdateWalkSpeed();
            _isPenaltyApplied = true;
        }

        private void RemoveMovementSpeedPenalty()
        {
            if (_currentPenaltyAmount == 0f && !_isPenaltyApplied) return;

            _currentPenaltyAmount = 0f;
            UpdateWalkSpeed();
            _isPenaltyApplied = false;
        }

        private void UpdateWalkSpeed()
        {
            entity.Stats.Set("walkspeed", "liquidEncumbrancePenalty", -_currentPenaltyAmount, true);
        }

        public override string PropertyName() => "liquidencumbrance";

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            _serverApi?.Event.EnqueueMainThreadTask(() => 
            {
                if (!entity.HasBehavior<EntityBehaviorLiquidEncumbrance>())
                {
                    entity.AddBehavior(new EntityBehaviorLiquidEncumbrance(entity));
                }
            }, "AddLiquidEncumbranceBehavior");
        }
    }
}
