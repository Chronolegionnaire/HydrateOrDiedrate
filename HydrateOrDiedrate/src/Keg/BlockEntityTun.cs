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
        private Config.Config config;
        private const int UpdateIntervalMs = 1000;
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
            config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");

            if (config == null)
            {
                config = new Config.Config();
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

                float tunMultiplier = config?.TunSpoilRateMultiplier ?? 0.5f;
                inv.TransitionableSpeedMulByType[EnumTransitionType.Perish] = tunMultiplier;
            }
        }
        public BlockEntityTun()
        {
            this.inventory = new InventoryGeneric(1, null, null, null);
            inventory.BaseWeight = 1.0f;
            inventory.OnGetSuitability = GetSuitability;
        }
        private float GetRoomMultiplier()
        {
            RoomRegistry roomRegistry = api.ModLoader.GetModSystem<RoomRegistry>();
            if (roomRegistry == null)
            {
                return 1.0f;
            }
            Room room = roomRegistry.GetRoomForPosition(Pos);
            if (room == null)
            {
                return 1.0f;
            }
            if (room.ExitCount == 0)
            {
                return 0.5f;
            }
            if (room.SkylightCount > 0)
            {
                return 1.2f;
            }

            return 1.0f;
        }
        private float GetTemperatureFactor()
        {
            ClimateCondition climate = api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            if (climate == null)
            {
                return 1.0f;
            }
            float normalizedTemperature = climate.Temperature / 20f;
            return Math.Max(0.5f, Math.Min(2.0f, normalizedTemperature));
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
                float roomMultiplier = GetRoomMultiplier();
                float tunMultiplier = config?.TunSpoilRateMultiplier ?? 0.5f;
                float temperatureFactor = GetTemperatureFactor();
                float finalSpoilRate = tunMultiplier * roomMultiplier * temperatureFactor;
                inv.TransitionableSpeedMulByType[EnumTransitionType.Perish] = finalSpoilRate;
            }
        }
        private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == inventory[1])
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
                dsc.AppendLine(Lang.Get("Empty"));
            else
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", itemSlot.Itemstack.StackSize, itemSlot.Itemstack.GetName()));
            
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
