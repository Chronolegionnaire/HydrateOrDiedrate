using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Tun;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Tun
{
    public class BlockEntityTun : BlockEntityLiquidContainer
    {
        private ICoreAPI api;
        private BlockTun ownBlock;
        public float MeshAngle;
        private Config.Config config;
        private const int UpdateIntervalMs = 1000;
        private bool[] hasTemperatureBeenUpdated = new bool[4];
        private float[] dailyTemperatures = new float[4];
        private readonly int[] sampleHours = { 6, 12, 18, 24 };
        private int lastSampleIndex = -1;
        private bool isTransitionSpeedDelegateRegistered = false;
        public override string InventoryClassName => "tun";

        private bool isTickListenerRegistered = false;

        public override void Initialize(ICoreAPI api)
        {
            if (this.inventory == null)
            {
                this.inventory = new InventoryGeneric(1, null, null, null);
            }

            base.Initialize(api);
            this.api = api;
            
            this.ownBlock = this.Block as BlockTun;

            if (this.inventory is InventoryGeneric inv)
            {
                inv.OnGetSuitability = GetSuitability;
            }

            config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");
            if (config == null)
            {
                config = new Config.Config();
            }

            InitializeTemperatureData();
            
            if (api.Side == EnumAppSide.Server && !isTickListenerRegistered)
            {
                RegisterGameTickListener(UpdateSpoilRate, UpdateIntervalMs);
                isTickListenerRegistered = true;
            }
        }
        private void InitializeTemperatureData()
        {
            if (dailyTemperatures.All(t => t == 0))
            {
                float initialTemp = GetCurrentTemperature();
                for (int i = 0; i < dailyTemperatures.Length; i++)
                {
                    dailyTemperatures[i] = initialTemp;
                }
            }
        }

        private float GetCurrentTemperature()
        {
            ClimateCondition climate = api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.NowValues);
            return climate?.Temperature ?? 20f;
        }
        private void SampleTemperature()
        {
            float currentHour = api.World.Calendar.HourOfDay;
            int newSampleIndex = -1;
            if (currentHour >= 0 && currentHour < 6)
            {
                newSampleIndex = 0;
            }
            else if (currentHour >= 6 && currentHour < 12)
            {
                newSampleIndex = 1;
            }
            else if (currentHour >= 12 && currentHour < 18)
            {
                newSampleIndex = 2;
            }
            else if (currentHour >= 18 && currentHour < 24)
            {
                newSampleIndex = 3;
            }
            if (newSampleIndex != -1 && !hasTemperatureBeenUpdated[newSampleIndex])
            {
                dailyTemperatures[newSampleIndex] = GetCurrentTemperature();
                hasTemperatureBeenUpdated[newSampleIndex] = true;
                for (int i = 0; i < hasTemperatureBeenUpdated.Length; i++)
                {
                    if (i != newSampleIndex)
                    {
                        hasTemperatureBeenUpdated[i] = false;
                    }
                }
            }
        }
        private string GetTimeSlotName(int index)
        {
            return index switch
            {
                0 => "Morning",
                1 => "Noon",
                2 => "Evening",
                3 => "Night",
                _ => "Unknown"
            };
        }


        private float CalculateAverageTemperature()
        {
            return dailyTemperatures.Average();
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
            float averageTemp = CalculateAverageTemperature();
            float normalizedTemperature = averageTemp / 20f;
            return Math.Max(0.5f, Math.Min(2.0f, normalizedTemperature));
        }

        private void UpdateSpoilRate(float dt)
        {
            SampleTemperature();

            if (this.inventory is InventoryGeneric inv)
            {
                inv.TransitionableSpeedMulByType = new Dictionary<EnumTransitionType, float>();
                float roomMultiplier = GetRoomMultiplier();
                float temperatureFactor = GetTemperatureFactor();
                float finalSpoilRate = roomMultiplier * temperatureFactor;
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

            ITreeAttribute tempTree = tree.GetTreeAttribute("dailyTemperatures");
            if (tempTree != null)
            {
                for (int i = 0; i < dailyTemperatures.Length; i++)
                {
                    dailyTemperatures[i] = tempTree.GetFloat($"temp{i}", dailyTemperatures[i]);
                }
            }

            lastSampleIndex = tree.GetInt("lastSampleIndex", -1);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);

            ITreeAttribute tempTree = new TreeAttribute();
            for (int i = 0; i < dailyTemperatures.Length; i++)
            {
                tempTree.SetFloat($"temp{i}", dailyTemperatures[i]);
            }
            tree["dailyTemperatures"] = tempTree;

            tree.SetInt("lastSampleIndex", lastSampleIndex);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            ItemSlot itemSlot = inventory[0];
            if (itemSlot.Empty)
                dsc.AppendLine(Lang.Get("Empty"));
            else
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", itemSlot.Itemstack.StackSize, itemSlot.Itemstack.GetName()));
        }
    }
}
