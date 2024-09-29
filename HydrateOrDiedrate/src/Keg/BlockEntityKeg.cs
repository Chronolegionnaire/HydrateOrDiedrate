using System;
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
        public bool isTapped;
        private Config.Config config;
        public override string InventoryClassName => "keg";

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.api = api;
            this.ownBlock = this.Block as BlockKeg;
            if (this.inventory is InventoryGeneric inv)
            {
                inv.OnAcquireTransitionSpeed += AdjustPerishSpeedInKeg;
            }
            config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");

            if (config == null)
            {
                config = new Config.Config();
            }
        }

        
        public BlockEntityKeg()
        {
            this.inventory = new InventoryGeneric(1, null, null, null);
            inventory.BaseWeight = 1.0f;
            inventory.OnGetSuitability = GetSuitability;
            isTapped = false;
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

        private float AdjustPerishSpeedInKeg(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            if (transType == EnumTransitionType.Perish)
            {
                float kegMultiplier = isTapped ? config.SpoilRateTapped : config.SpoilRateUntapped;
                return baseMul * kegMultiplier;
            }

            return baseMul;
        }

        public void TapKeg()
        {
            isTapped = true;
            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            ItemSlot itemSlot = inventory[0];
            if (itemSlot.Empty)
                dsc.AppendLine(Lang.Get("Empty"));
            else
                dsc.AppendLine(Lang.Get("Contents: {0}x{1}", itemSlot.Itemstack.StackSize, itemSlot.Itemstack.GetName()));

            dsc.AppendLine(isTapped ? Lang.Get("Tapped") : Lang.Get("Untapped"));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
            isTapped = tree.GetBool("isTapped", false);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);
            tree.SetBool("isTapped", isTapped);
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