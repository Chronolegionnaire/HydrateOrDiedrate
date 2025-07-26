using System;
using System.Collections.Generic;
using System.Text;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg
{
    public class BlockEntityTun : BlockEntityLiquidContainer
    {
        private ICoreAPI api;
        private BlockTun ownBlock;
        public float MeshAngle;
        private const int UpdateIntervalMs = 1000;
        private float tunSpoilRateMultiplier = ModConfig.Instance.Containers.TunSpoilRateMultiplier;
        public override string InventoryClassName => "tun";

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.api = api;
            this.ownBlock = this.Block as BlockTun;
            if (this.inventory is InventoryGeneric inv)
            {
                inv.OnGetSuitability = GetSuitability;
                UpdateTunMultiplier();
            }
            RegisterGameTickListener(UpdateSpoilRate, UpdateIntervalMs);
        }
        private void UpdateTunMultiplier()
        {
            if (this.inventory is InventoryGeneric inv && this.Block != null)
            {
                if (inv.TransitionableSpeedMulByType == null)
                {
                    inv.TransitionableSpeedMulByType = new Dictionary<EnumTransitionType, float>();
                }
                float tunMultiplier = tunSpoilRateMultiplier;
                inv.TransitionableSpeedMulByType[EnumTransitionType.Perish] = tunMultiplier;
            }
        }
        public BlockEntityTun()
        {
            this.inventory = new InventoryGeneric(1, null, null, null);
            inventory.BaseWeight = 1.0f;
            inventory.OnGetSuitability = GetSuitability;
        }
        private void UpdateSpoilRate(float dt)
        {
            if (this.inventory is InventoryGeneric inv)
            {
                if (inv.TransitionableSpeedMulByType == null)
                {
                    inv.TransitionableSpeedMulByType = new Dictionary<EnumTransitionType, float>();
                }
                else
                {
                    inv.TransitionableSpeedMulByType.Clear();
                }
                float tunMultiplier = tunSpoilRateMultiplier;
                inv.TransitionableSpeedMulByType[EnumTransitionType.Perish] = tunMultiplier;
            }
        }
        private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == inventory[0])
            {
                if (inventory[0].StackSize > 0)
                {
                    ItemStack currentStack = inventory[0].Itemstack;
                    ItemStack testStack = sourceSlot.Itemstack;
                    if (currentStack.Collectible.Equals(currentStack, testStack, GlobalConstants.IgnoredStackAttributes))
                        return -1;
                }
            }

            return (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) +
                   (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            ItemSlot itemSlot = inventory[0];
            if (itemSlot.Empty)
                dsc.AppendLine(Lang.Get("hydrateordiedrate:blockentitykeg-empty"));
            else
                dsc.AppendLine(Lang.Get("hydrateordiedrate:blockentitykeg-contents", itemSlot.Itemstack.StackSize, itemSlot.Itemstack.GetName()));
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (inventory != null)
            {
                foreach (var slot in inventory)
                {
                    if (!slot.Empty)
                    {
                        api.World.SpawnItemEntity(slot.TakeOutWhole(), Pos.ToVec3d());
                    }
                }
                inventory.Clear();
            }

            base.OnBlockBroken(byPlayer);
        }
    }
}
