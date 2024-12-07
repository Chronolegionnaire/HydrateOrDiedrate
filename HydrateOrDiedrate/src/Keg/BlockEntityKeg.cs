using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HydrateOrDiedrate.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg
{
    public class BlockEntityKeg : BlockEntityLiquidContainer
    {
        private ICoreAPI api;
        private BlockKeg ownBlock;
        public float MeshAngle;
        private Config.Config config;
        private const int UpdateIntervalMs = 1000;
        private bool[] hasTemperatureBeenUpdated = new bool[4];
        private float[] dailyTemperatures = new float[4];
        private readonly int[] sampleHours = { 6, 12, 18, 24 };
        private int lastSampleIndex = -1;

        public override string InventoryClassName => "keg";

        public BlockEntityKeg()
        {
            this.inventory = new InventoryGeneric(1, null, null, null);
            inventory.BaseWeight = 1.0f;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            this.api = api;
            this.ownBlock = this.Block as BlockKeg;

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
            RegisterGameTickListener(UpdateSpoilRate, UpdateIntervalMs);
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
            int newSampleIndex = currentHour switch
            {
                >= 0 and < 6 => 0,
                >= 6 and < 12 => 1,
                >= 12 and < 18 => 2,
                >= 18 and < 24 => 3,
                _ => -1
            };

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

        private float CalculateAverageTemperature()
        {
            return dailyTemperatures.Average();
        }

        private float GetRoomMultiplier()
        {
            RoomRegistry roomRegistry = api.ModLoader.GetModSystem<RoomRegistry>();
            if (roomRegistry == null) return 1.0f;

            Room room = roomRegistry.GetRoomForPosition(Pos);
            if (room == null) return 1.0f;

            if (room.ExitCount == 0) return 0.5f;
            if (room.SkylightCount > 0) return 1.2f;

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
                if (inv.TransitionableSpeedMulByType == null)
                {
                    inv.TransitionableSpeedMulByType = new Dictionary<EnumTransitionType, float>();
                }
                else
                {
                    inv.TransitionableSpeedMulByType.Clear();
                }

                float roomMultiplier = GetRoomMultiplier();
                float temperatureFactor = GetTemperatureFactor();
                float kegMultiplier = (this.Block.Code.Path == "kegtapped")
                    ? config?.SpoilRateTapped ?? 1.0f
                    : config?.SpoilRateUntapped ?? 1.0f;

                float finalSpoilRate = kegMultiplier * roomMultiplier * temperatureFactor;
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            ItemSlot itemSlot = inventory[0];
            if (itemSlot.Empty)
                dsc.AppendLine(Lang.Get("Empty"));
            else
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", itemSlot.Itemstack.StackSize, itemSlot.Itemstack.GetName()));

            dsc.AppendLine(Lang.Get("Average Daily Temperature: {0}°C", CalculateAverageTemperature()));
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

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            tesselator.TesselateBlock(this.Block, out mesh);
            Vec3f rotationOrigin = new Vec3f(0.5f, 0.5f, 0.5f);
            mesh.Rotate(rotationOrigin, 0f, this.MeshAngle, 0f);
            mesher.AddMeshData(mesh);
            return true;
        }
    }
}
