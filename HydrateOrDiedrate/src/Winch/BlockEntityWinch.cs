using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HydrateOrDiedrate.Config;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch
{
    public class BlockEntityWinch : BlockEntityOpenableContainer
    {
        private ICoreAPI api;
        private BlockWinch ownBlock;
        public float MeshAngle;
        private Config.Config config;
        private ILoadedSound ambientSound;
        internal InventoryWinch inventory;
        public float inputTurnTime;
        public float prevInputTurnTime;
        private GuiDialogBlockEntityWinch clientDialog;
        private WinchTopRenderer renderer;
        private bool automated;
        private BEBehaviorMPConsumer mpc;
        private float prevSpeed = float.NaN;
        private Dictionary<string, long> playersTurning = new Dictionary<string, long>();
        private int quantityPlayersTurning;
        private int nowOutputFace;
        private bool beforeTurning;
        
        public string Material
        {
            get
            {
                return base.Block.LastCodePart(1);
            }
        }
        
        public string Direction
        {
            get
            {
                return base.Block.LastCodePart(0);
            }
        }
        
        public float TurnSpeed
        {
            get
            {
                if (this.quantityPlayersTurning > 0)
                {
                    return 1f;
                }
                if (this.automated && this.mpc.Network != null)
                {
                    return this.mpc.TrueSpeed;
                }
                return 0f;
            }
        }
        
        private MeshData winchBaseMesh
        {
            get
            {
                object value;
                this.Api.ObjectCache.TryGetValue("winchbasemesh-" + this.Material, out value);
                return (MeshData)value;
            }
            set
            {
                this.Api.ObjectCache["winchbasemesh-" + this.Material] = value;
            }
        }
        
        private MeshData winchTopMesh
        {
            get
            {
                object value = null;
                this.Api.ObjectCache.TryGetValue("winchtopmesh-" + this.Material, out value);
                return (MeshData)value;
            }
            set
            {
                this.Api.ObjectCache["winchtopmesh-" + this.Material] = value;
            }
        }
        
        public virtual float maxTurningTime()
        {
            return 5f;
        }
        
        public override string InventoryClassName
        {
            get
            {
                return "winch";
            }
        }
        
        public virtual string DialogTitle
        {
            get
            {
                return Lang.Get("hydrateordiedrate:Winch", Array.Empty<object>());
            }
        }
        
        public override InventoryBase Inventory
        {
            get
            {
                return this.inventory;
            }
        }
        
        public BlockEntityWinch()
        {
            this.inventory = new InventoryWinch(null, null);
            this.inventory.SlotModified += this.OnSlotModifid;
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.api = api;
            this.ownBlock = this.Block as BlockWinch;
            config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json");

            if (config == null)
            {
                config = new Config.Config();
            }

            base.Initialize(api);
            this.inventory.LateInitialize($"winch-{this.Pos.X}/{this.Pos.Y}/{this.Pos.Z}", api);

            this.RegisterGameTickListener(new Action<float>(this.Every100ms), 100, 0);
            this.RegisterGameTickListener(new Action<float>(this.Every500ms), 500, 0);

            if (this.ambientSound == null && api.Side == EnumAppSide.Client)
            {
                this.ambientSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams
                {
                    Location = new AssetLocation("game:sounds/block/woodcreak_2.ogg"),
                    ShouldLoop = true,
                    Position = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.75f
                });
            }

            if (api.Side == EnumAppSide.Client)
            {
                this.renderer = new WinchTopRenderer(api as ICoreClientAPI, this.Pos, this.GenMesh("top"), this.Direction);
                this.renderer.mechPowerPart = this.mpc;

                if (this.automated)
                {
                    this.renderer.ShouldRender = true;
                    this.renderer.ShouldRotateAutomated = true;
                }

                (api as ICoreClientAPI).Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "winch");

                if (this.winchBaseMesh == null)
                {
                    this.winchBaseMesh = this.GenMesh("base");
                }

                if (this.winchTopMesh == null)
                {
                    this.winchTopMesh = this.GenMesh("top");
                }
            }
        }

        
        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
            this.mpc = base.GetBehavior<BEBehaviorMPConsumer>();
            if (this.mpc != null)
            {
                this.mpc.OnConnected = delegate
                {
                    this.automated = true;
                    this.quantityPlayersTurning = 0;
                    if (this.renderer != null)
                    {
                        this.renderer.ShouldRender = true;
                        this.renderer.ShouldRotateAutomated = true;
                    }
                };
                this.mpc.OnDisconnected = delegate
                {
                    this.automated = false;
                    if (this.renderer != null)
                    {
                        this.renderer.ShouldRender = false;
                        this.renderer.ShouldRotateAutomated = false;
                    }
                };
            }
        }
        
        public void IsTurning(IPlayer byPlayer)
        {
            this.SetPlayerTurning(byPlayer, true);
        }
        
        private void Every100ms(float dt)
        {
            float turnSpeed = this.TurnSpeed;
            if (this.Api.Side != EnumAppSide.Client)
            {
                if (this.CanTurn() && turnSpeed > 0f)
                {
                    this.inputTurnTime += dt * turnSpeed;
                    if (this.inputTurnTime >= this.maxTurningTime())
                    {
                        this.turnInput();
                        this.inputTurnTime = 0f;
                    }
                    this.MarkDirty(false, null);
                }
                return;
            }
            if (this.ambientSound != null && this.automated && this.mpc.TrueSpeed != this.prevSpeed)
            {
                this.prevSpeed = this.mpc.TrueSpeed;
                this.ambientSound.SetPitch((0.5f + this.prevSpeed) * 0.9f);
                this.ambientSound.SetVolume(Math.Min(1f, this.prevSpeed * 3f));
                return;
            }
            this.prevSpeed = float.NaN;
        }

        private void turnInput()
        {
            (string waterType, int extracted) = ExtractWater(10);

            if (extracted > 0)
            {
                if (this.InputSlot.Itemstack == null || this.InputSlot.Itemstack.Collectible.Code.Path != "woodbucket")
                {
                    return;
                }

                ItemStack filledBucket = this.InputSlot.Itemstack.Clone();

                TreeAttribute contents = new TreeAttribute();

                int totalWaterItems = extracted * 100;

                ItemStack waterStack = new ItemStack(
                    this.Api.World.GetItem(new AssetLocation($"hydrateordiedrate:wellwaterportion-{waterType}")),
                    totalWaterItems
                );

                contents["0"] = new ItemstackAttribute(waterStack);

                filledBucket.Attributes["contents"] = contents;

                if (this.OutputSlot.Itemstack == null)
                {
                    this.OutputSlot.Itemstack = filledBucket;
                }
                else if (this.OutputSlot.Itemstack.Collectible.GetMergableQuantity(this.OutputSlot.Itemstack,
                             filledBucket, EnumMergePriority.AutoMerge) > 0)
                {
                    this.OutputSlot.Itemstack.StackSize += filledBucket.StackSize;
                }
                else
                {
                    BlockFacing face = BlockFacing.HORIZONTALS[this.nowOutputFace];
                    this.nowOutputFace = (this.nowOutputFace + 1) % 4;

                    if (this.Api.World.BlockAccessor.GetBlock(this.Pos.AddCopy(face)).Replaceable < 6000)
                    {
                        return;
                    }

                    this.Api.World.SpawnItemEntity(
                        filledBucket,
                        this.Pos.ToVec3d().Add(0.5 + face.Normalf.X * 0.7, 0.75, 0.5 + face.Normalf.Z * 0.7),
                        new Vec3d(face.Normalf.X * 0.02, 0.0, face.Normalf.Z * 0.02)
                    );
                }

                this.InputSlot.TakeOut(1);
                this.InputSlot.MarkDirty();
                this.OutputSlot.MarkDirty();
            }

            this.inputTurnTime = 0f;
        }

        private (string, int) ExtractWater(int litersNeeded)
        {
            int remaining = litersNeeded;
            int totalExtracted = 0;
            string waterType = null;
            BlockPos currentPos = this.Pos.Copy().DownCopy();

            while (remaining > 0)
            {
                Block currentBlock = this.Api.World.BlockAccessor.GetBlock(currentPos);
                string codePath = currentBlock.Code.Path.ToLowerInvariant();

                if (codePath.StartsWith("game:water"))
                {
                    waterType = "water";
                    totalExtracted = litersNeeded;
                    break;
                }
                else if (codePath.StartsWith("game:saltwater"))
                {
                    waterType = "saltwater";
                    totalExtracted = litersNeeded;
                    break;
                }
                else if (codePath.StartsWith("game:boilingwater"))
                {
                    waterType = "water";
                    totalExtracted = litersNeeded;
                    break;
                }

                if (!codePath.Contains("wellwater"))
                {
                    break;
                }

                BlockEntity be = this.Api.World.BlockAccessor.GetBlockEntity(currentPos);
                if (be is HydrateOrDiedrate.wellwater.BlockEntityWellWaterData wellData)
                {
                    int available = wellData.Volume;
                    if (available > 0)
                    {
                        int extract = Math.Min(available, remaining);
                        wellData.Volume -= extract;
                        wellData.MarkDirty(true);
                        totalExtracted += extract;
                        remaining -= extract;

                        if (waterType == null)
                        {
                            if (codePath.Contains("muddy") && codePath.Contains("salt"))
                                waterType = "muddysalt";
                            else if (codePath.Contains("tainted") && codePath.Contains("salt"))
                                waterType = "taintedsalt";
                            else if (codePath.Contains("poisoned") && codePath.Contains("salt"))
                                waterType = "poisonedsalt";
                            else if (codePath.Contains("fresh"))
                                waterType = "fresh";
                            else if (codePath.Contains("salt"))
                                waterType = "salt";
                            else if (codePath.Contains("muddy"))
                                waterType = "muddy";
                            else if (codePath.Contains("tainted"))
                                waterType = "tainted";
                            else if (codePath.Contains("poisoned"))
                                waterType = "poisoned";
                            else
                                waterType = "fresh";
                        }
                    }
                }

                currentPos = currentPos.DownCopy();
            }

            return (waterType ?? "fresh", totalExtracted);
        }

        private void Every500ms(float dt)
        {
            if (this.Api.Side == EnumAppSide.Server && (this.TurnSpeed > 0f || this.prevInputTurnTime != this.inputTurnTime))
            {
                ItemStack itemstack = this.inventory[0].Itemstack;
                if (((itemstack != null) ? itemstack.Collectible.GrindingProps : null) != null)
                {
                    this.MarkDirty(false, null);
                }
            }
            this.prevInputTurnTime = this.inputTurnTime;
            foreach (KeyValuePair<string, long> val in this.playersTurning)
            {
                if (this.Api.World.ElapsedMilliseconds - val.Value > 1000L)
                {
                    this.playersTurning.Remove(val.Key);
                    break;
                }
            }
        }
        
        private void OnSlotModifid(int slotid)
        {
            if (this.Api is ICoreClientAPI)
            {
                this.clientDialog.Update(this.inputTurnTime, this.maxTurningTime());
            }
            if (slotid == 0)
            {
                if (this.InputSlot.Empty)
                {
                    this.inputTurnTime = 0f;
                }
                this.MarkDirty(false, null);
                if (this.clientDialog != null && this.clientDialog.IsOpened())
                {
                    this.clientDialog.SingleComposer.ReCompose();
                }
            }
        }
        public void SetPlayerTurning(IPlayer player, bool playerTurning)
        {
            if (!this.automated)
            {
                if (playerTurning)
                {
                    this.playersTurning[player.PlayerUID] = this.Api.World.ElapsedMilliseconds;
                }
                else
                {
                    this.playersTurning.Remove(player.PlayerUID);
                }
                this.quantityPlayersTurning = this.playersTurning.Count;
            }
            this.updateTurningState();
        }
        private void OnRetesselated()
        {
            if (this.renderer == null)
            {
                return;
            }
            this.renderer.ShouldRender = this.quantityPlayersTurning > 0 || this.automated;
        }
        public bool CanTurn()
        {
            ItemSlot slot = this.InputSlot;
            if (slot.Itemstack == null) return false;

            string itemCode = slot.Itemstack.Collectible.Code.ToString();

            return (itemCode == "game:woodbucket" || itemCode.StartsWith("vanvar:bucket-")) &&
                   (slot.Itemstack.Attributes == null || !slot.Itemstack.Attributes.HasAttribute("contents"));
        }

        internal MeshData GenMesh(string type = "base")
        {
            Block block = this.Api.World.BlockAccessor.GetBlock(this.Pos);
            if (block.BlockId == 0)
            {
                return null;
            }
            MeshData mesh;
            ((ICoreClientAPI)this.Api).Tesselator.TesselateShape(block, Shape.TryGet(this.Api, "hydrateordiedrate:shapes/block/winch/" + type + ".json"), out mesh, null, null, null);
            return mesh;
        }
        private void updateTurningState()
        {
            ICoreAPI api = this.Api;
            if (((api != null) ? api.World : null) == null)
            {
                return;
            }
            bool nowTurning = this.quantityPlayersTurning > 0 || (this.automated && this.mpc.TrueSpeed > 0f);
            if (nowTurning != this.beforeTurning)
            {
                if (this.renderer != null)
                {
                    this.renderer.ShouldRotateManual = this.quantityPlayersTurning > 0;
                }
                this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos, new Action(this.OnRetesselated));
                if (nowTurning)
                {
                    ILoadedSound loadedSound = this.ambientSound;
                    if (loadedSound != null)
                    {
                        loadedSound.Start();
                    }
                }
                else
                {
                    ILoadedSound loadedSound2 = this.ambientSound;
                    if (loadedSound2 != null)
                    {
                        loadedSound2.Stop();
                    }
                }
                if (this.Api.Side == EnumAppSide.Server)
                {
                    this.MarkDirty(false, null);
                }
            }
            this.beforeTurning = nowTurning;
        }
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            LogInputSlotContents(); 
            if (blockSel.SelectionBoxIndex == 1)
            {
                return false;
            }
            if (this.Api.Side == EnumAppSide.Client)
            {
                base.toggleInventoryDialogClient(byPlayer, delegate
                {
                    this.clientDialog = new GuiDialogBlockEntityWinch(this.DialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI);
                    this.clientDialog.Update(this.inputTurnTime, this.maxTurningTime());
                    return this.clientDialog;
                });
            }
            return true;
        }
        
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 1001)
            {
                (this.Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(this.Inventory);
                GuiDialogBlockEntity invDialog = this.invDialog;
                if (invDialog != null)
                {
                    invDialog.TryClose();
                }
                GuiDialogBlockEntity invDialog2 = this.invDialog;
                if (invDialog2 != null)
                {
                    invDialog2.Dispose();
                }
                this.invDialog = null;
            }
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
            this.Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            if (this.Api != null)
            {
                this.Inventory.AfterBlocksLoaded(this.Api.World);
            }
            this.inputTurnTime = tree.GetFloat("inputTurnTime", 0f);
            this.nowOutputFace = tree.GetInt("nowOutputFace", 0);
            if (worldForResolving.Side == EnumAppSide.Client)
            {
                List<int> clientIds = new List<int>((tree["clientIdsTurning"] as IntArrayAttribute).value);
                this.quantityPlayersTurning = clientIds.Count;
                foreach (string uid in this.playersTurning.Keys.ToArray())
                {
                    IPlayer plr = this.Api.World.PlayerByUid(uid);
                    if (plr == null || !clientIds.Contains(plr.ClientId))
                    {
                        this.playersTurning.Remove(uid);
                    }
                    else
                    {
                        clientIds.Remove(plr.ClientId);
                    }
                }
                for (int i = 0; i < clientIds.Count; i++)
                {
                    IEnumerable<IPlayer> allPlayers = worldForResolving.AllPlayers;
                    IPlayer plr2 = allPlayers.FirstOrDefault(p => p.ClientId == clientIds[i]);
                    if (plr2 != null)
                    {
                        this.playersTurning.Add(plr2.PlayerUID, worldForResolving.ElapsedMilliseconds);
                    }
                }
                this.updateTurningState();
            }

            ICoreAPI api = this.Api;
            if (api != null && api.Side == EnumAppSide.Client && this.clientDialog != null)
            {
                this.clientDialog.Update(this.inputTurnTime, this.maxTurningTime());
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);
            ITreeAttribute invtree = new TreeAttribute();
            this.Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;
            tree.SetFloat("inputTurnTime", this.inputTurnTime);
            tree.SetInt("nowOutputFace", this.nowOutputFace);
            List<int> vals = new List<int>();
            foreach (KeyValuePair<string, long> val in this.playersTurning)
            {
                IPlayer plr = this.Api.World.PlayerByUid(val.Key);
                if (plr != null)
                {
                    vals.Add(plr.ClientId);
                }
            }
            tree["clientIdsTurning"] = new IntArrayAttribute(vals.ToArray());
            
        }
        
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.ambientSound != null)
            {
                this.ambientSound.Stop();
                this.ambientSound.Dispose();
            }
            GuiDialogBlockEntityWinch guiDialogBlockEntityWinch = this.clientDialog;
            if (guiDialogBlockEntityWinch != null)
            {
                guiDialogBlockEntityWinch.TryClose();
            }
            WinchTopRenderer winchTopRenderer = this.renderer;
            if (winchTopRenderer != null)
            {
                winchTopRenderer.Dispose();
            }
            this.renderer = null;
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
        }
        ~BlockEntityWinch()
        {
            if (this.ambientSound != null)
            {
                this.ambientSound.Dispose();
            }
        }
        public ItemSlot InputSlot
        {
            get
            {
                return this.inventory[0];
                
            }
        }
        
        public void LogInputSlotContents()
{
    if (this.InputSlot.Itemstack == null)
    {
        return;
    }

    if (this.InputSlot.Itemstack.Attributes == null)
    {
        return;
    }

    try
    {
        string attributesStr = ConvertTreeAttributeToString(this.InputSlot.Itemstack.Attributes, 0);
    }
    catch (Exception ex)
    {
    }
}

private string ConvertTreeAttributeToString(ITreeAttribute treeAttr, int indent)
{
    StringBuilder sb = new StringBuilder();
    string indentStr = new string(' ', indent * 2);

    foreach (var kvp in treeAttr)
    {
        try
        {
            string key = kvp.Key;
            string valueStr = "";

            if (kvp.Value is ITreeAttribute nestedTree)
            {
                valueStr = "\n" + ConvertTreeAttributeToString(nestedTree, indent + 1);
            }
            else if (kvp.Value is IAttribute attr)
            {
                try
                {
                    object val = attr.GetValue();
                    valueStr = val != null ? val.ToString() : "null";
                }
                catch (Exception ex)
                {
                }
            }
            else
            {
                valueStr = kvp.Value?.ToString() ?? "null";
            }

            sb.AppendLine($"{indentStr}{key}: {valueStr}");
        }
        catch (Exception ex)
        {
        }
    }
    return sb.ToString();
}
        public ItemSlot OutputSlot
        {
            get
            {
                return this.inventory[1];
            }
        }
        
        public ItemStack InputStack
        {
            get
            {
                return this.inventory[0].Itemstack;
            }
            set
            {
                this.inventory[0].Itemstack = value;
                this.inventory[0].MarkDirty();
            }
        }
        
        public ItemStack OutputStack
        {
            get
            {
                return this.inventory[1].Itemstack;
            }
            set
            {
                this.inventory[1].Itemstack = value;
                this.inventory[1].MarkDirty();
            }
        }
        
        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (ItemSlot slot in this.Inventory)
            {
                if (slot.Itemstack != null)
                {
                    if (slot.Itemstack.Class == EnumItemClass.Item)
                    {
                        itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
                    }
                    else
                    {
                        blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
                    }
                    ItemStack itemstack = slot.Itemstack;
                    if (itemstack != null)
                    {
                        itemstack.Collectible.OnStoreCollectibleMappings(this.Api.World, slot, blockIdMapping, itemIdMapping);
                    }
                }
            }
        }
        
        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            foreach (ItemSlot slot in this.Inventory)
            {
                if (slot.Itemstack != null)
                {
                    if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                    {
                        slot.Itemstack = null;
                    }
                    ItemStack itemstack = slot.Itemstack;
                    if (itemstack != null)
                    {
                        itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping, resolveImports);
                    }
                }
            }
        }
        
        
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (base.Block == null) return false;
            if (this.winchBaseMesh == null || this.winchTopMesh == null) return false;
            MeshData baseMesh = this.winchBaseMesh.Clone();
            MeshData topMesh = this.winchTopMesh.Clone();
            float yRotation = 0f;
            switch (Direction)
            {
                case "east":
                    yRotation = GameMath.PIHALF;
                    break;
                case "south":
                    yRotation = GameMath.PI;
                    break;
                case "west":
                    yRotation = GameMath.PI + GameMath.PIHALF;
                    break;
            }
            baseMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, yRotation, 0f);
            mesher.AddMeshData(baseMesh, 1);

            if (this.quantityPlayersTurning == 0 && !this.automated)
            {
                topMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, yRotation, 0f);
                topMesh.Translate(0f, 0.5f, 0f);
                if (Direction == "east")
                {
                    topMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -this.renderer.AngleRad, 0f, -this.renderer.AngleRad);
                }
                if (Direction == "west")
                {
                    topMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, this.renderer.AngleRad);
                }
                else if (Direction == "south")
                {
                    topMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -this.renderer.AngleRad, 0f, 0f);
                }
                else
                {
                    topMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), this.renderer.AngleRad, 0f, 0f);
                }

                mesher.AddMeshData(topMesh, 1);
            }

            return true;
        }
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            WinchTopRenderer winchTopRenderer = this.renderer;
            if (winchTopRenderer != null)
            {
                winchTopRenderer.Dispose();
            }
            if (this.ambientSound != null)
            {
                this.ambientSound.Stop();
                this.ambientSound.Dispose();
            }
        }
    }
}