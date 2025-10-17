using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public class BlockEntityPipe : BlockEntityOpenableContainer
    {
        ICoreClientAPI capi;
        PipeTesselation pipeTess;

        public override string InventoryClassName => "pipe-disguise";
        public override InventoryBase Inventory { get; }
        public ItemSlot DisguiseSlot => Inventory[0];

        public BlockEntityPipe()
        {
            Inventory = new InventoryPipeDisguise(null, null);
            Inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            Inventory.LateInitialize($"{InventoryClassName}-{Pos.X}/{Pos.Y}/{Pos.Z}", api);

            capi = api as ICoreClientAPI;
            if (capi != null) pipeTess = new PipeTesselation(capi);
        }

        void OnSlotModified(int slotId)
        {
            MarkDirty();
            Api?.World?.BlockAccessor?.MarkBlockDirty(Pos);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tess)
        {
            var disguiseStack = DisguiseSlot?.Itemstack;
            var disguiseBlock = disguiseStack?.Block;

            if (capi != null && disguiseBlock != null)
            {
                capi.Tesselator.TesselateBlock(disguiseBlock, out var mesh);
                if (mesh == null) return false;

                if (mesh.SeasonColorMapIds == null)  mesh.SeasonColorMapIds  = new byte[mesh.VerticesCount];
                if (mesh.ClimateColorMapIds == null) mesh.ClimateColorMapIds = new byte[mesh.VerticesCount];

                mesher.AddMeshData(mesh);
                return true;
            }
            if (pipeTess == null) return false;
            return pipeTess.TryTesselate(Block, Pos, Api, mesher, tess);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            TryOpenGui(byPlayer);
            return true;
        }

        public void TryOpenGui(IPlayer player)
        {
            player?.InventoryManager?.OpenInventory(Inventory);

            if (Api?.Side != EnumAppSide.Client) return;

            var capi = (ICoreClientAPI)Api;
            var dlg = new GuiDialogPipeDisguise(capi, Inventory, Pos);
            dlg.TryOpen();
        }

        void OnClose() { }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            var invTree = new TreeAttribute();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            var invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null)
            {
                Inventory.FromTreeAttributes(invTree);
                if (Api != null) Inventory.AfterBlocksLoaded(Api.World);
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (DisguiseSlot?.Itemstack?.Block != null)
            {
                dsc.AppendLine(Lang.Get("Disguised as: {0}", DisguiseSlot.Itemstack.Block.GetPlacedBlockName(Api.World, Pos)));
            }
        }
    }
}
