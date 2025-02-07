﻿using System;
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
using Vintagestory.API.Server;
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
        private bool beforeTurning;
        private int nowOutputFace;
        public float bucketDepth = 0.5f;
        private float movementAccumTime = 0f;
        private const float secondsPerBlock = 0.5f;
        public bool isRaising;
        private bool wasRaising = false;
        private MeshData winchBaseMesh;
        private MeshData winchTopMesh;
        public bool IsRaising
        {
            get { return isRaising; }
        }
        public bool CanMoveDown()
        {
            float nextDepth = bucketDepth + 0.1f;
            BlockPos checkPos = Pos.DownCopy((int)Math.Ceiling(nextDepth));

            if (checkPos.Y < 0) return false;

            Block blockBelow = Api.World.BlockAccessor.GetBlock(checkPos);

            if (blockBelow == this.Block) return true;

            if (blockBelow.Replaceable < 6000 && !IsLiquidBlock(blockBelow)) return false;

            return true;
        }


        public bool CanMoveUp()
        {
            return (bucketDepth > 0.5f);
        }
        public override string InventoryClassName => "winch";
        public override InventoryBase Inventory => inventory;
        public BlockEntityWinch()
        {
            this.inventory = new InventoryWinch(null, null);
            this.inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.api = api;
            this.ownBlock = this.Block as BlockWinch;
            config = ModConfig.ReadConfig<Config.Config>(api, "HydrateOrDiedrateConfig.json") ?? new Config.Config();
            if (inventory == null)
            {
                inventory = new InventoryWinch($"winch-{Pos.X}/{Pos.Y}/{Pos.Z}", api);
                inventory.SlotModified += OnSlotModified;
            }
            else
            {
                inventory.LateInitialize($"winch-{Pos.X}/{Pos.Y}/{Pos.Z}", api);
            }
            RegisterGameTickListener(Every100ms, 100);
            RegisterGameTickListener(Every500ms, 500);

            if (api.Side == EnumAppSide.Client)
            {
                ambientSound = (api.World as IClientWorldAccessor)?.LoadSound(new SoundParams
                {
                    Location = new AssetLocation("game:sounds/block/woodcreak_2.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 0.75f
                });
            }
            if (api.Side == EnumAppSide.Client)
            {
                winchBaseMesh = GenMesh("base");
                winchTopMesh  = GenMesh("top");
                renderer = new WinchTopRenderer(api as ICoreClientAPI, Pos, winchTopMesh, Direction);
                renderer.mechPowerPart = mpc;

                if (automated)
                {
                    renderer.ShouldRender = true;
                    renderer.ShouldRotateAutomated = true;
                }
    
                ICoreClientAPI capi = api as ICoreClientAPI;
                capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "winch");
            }
        }
        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);
            mpc = GetBehavior<BEBehaviorMPConsumer>();
            if (mpc != null)
            {
                mpc.OnConnected = () =>
                {
                    automated = true;
                    quantityPlayersTurning = 0;
                    if (renderer != null)
                    {
                        renderer.ShouldRender = true;
                        renderer.ShouldRotateAutomated = true;
                    }
                };
                mpc.OnDisconnected = () =>
                {
                    automated = false;
                    if (renderer != null)
                    {
                        renderer.ShouldRender = false;
                        renderer.ShouldRotateAutomated = false;
                    }
                };
            }
        }
        private MeshData GenMesh(string subPart)
        {
            if (Api is ICoreClientAPI capi)
            {
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                if (block == null || block.BlockId == 0) return null;

                string shapePath = "hydrateordiedrate:shapes/block/winch/" + subPart + ".json";
                Shape shape = Shape.TryGet(Api, shapePath);
                if (shape == null) return null;

                MeshData mesh;
                capi.Tesselator.TesselateShape(block, shape, out mesh);
                return mesh;
            }
            return null;
        }
        
        private MeshData GenRopeMesh()
        {
            if (Api is ICoreClientAPI capi)
            {
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                string shapePath = "hydrateordiedrate:shapes/block/winch/rope1x1.json";
                Shape shape = Shape.TryGet(Api, shapePath);
                if (shape == null) return null;

                MeshData mesh;
                capi.Tesselator.TesselateShape(block, shape, out mesh);
                return mesh;
            }
            return null;
        }
        
        private MeshData GenKnotMesh()
        {
            if (Api is ICoreClientAPI capi)
            {
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                string shapePath = "hydrateordiedrate:shapes/block/winch/knot.json";
                Shape shape = Shape.TryGet(Api, shapePath);
                if (shape == null) return null;

                MeshData mesh;
                capi.Tesselator.TesselateShape(block, shape, out mesh);
                return mesh;
            }
            return null;
        }

        private void Every100ms(float dt)
        {
            float turnSpeed = GetCurrentTurnSpeed();

            if (Api.Side == EnumAppSide.Server && quantityPlayersTurning > 0)
            {
                bool anySneaking = playersTurning.Keys.Any(uid =>
                    Api.World.PlayerByUid(uid)?.Entity?.Controls?.Sneak == true
                );
                bool oldRaising = isRaising;
                isRaising = anySneaking;

                if (isRaising != oldRaising)
                {
                    movementAccumTime = 0f;
                    wasRaising = isRaising;
                }

                movementAccumTime += dt;

                if (StepMovement())
                {
                    MarkDirty();
                }
                else
                {
                    playersTurning.Clear();
                    quantityPlayersTurning = 0;
                    updateTurningState();
                }
            }

            if (automated && mpc != null && mpc.Network != null)
            {
                EnumRotDirection turnDir = mpc.Network.TurnDir;
                bool oldRaising = isRaising;
                isRaising = (turnDir == EnumRotDirection.Counterclockwise);
                Console.WriteLine(turnDir);
                if (isRaising != oldRaising)
                {
                    movementAccumTime = 0f;
                    wasRaising = isRaising;
                }

                movementAccumTime += dt;

                if (StepMovement())
                {
                    MarkDirty();
                }
            }

            if (Api.Side == EnumAppSide.Client && automated && ambientSound != null && mpc != null)
            {
                if (mpc.TrueSpeed > 0f)
                {
                    if (!ambientSound.IsPlaying) ambientSound.Start();
                }
                else
                {
                    ambientSound.Stop();
                }
            }

            updateTurningState();
        }


        private void Every500ms(float dt)
        {
            if (Api.Side == EnumAppSide.Server && (GetCurrentTurnSpeed() > 0f || prevInputTurnTime != inputTurnTime))
            {
                ItemStack itemstack = inventory?[0]?.Itemstack;
                if (itemstack?.Collectible?.GrindingProps != null)
                {
                    MarkDirty(false);
                }
            }
            prevInputTurnTime = inputTurnTime;
            foreach (var kvp in playersTurning.ToArray())
            {
                if (Api.World.ElapsedMilliseconds - kvp.Value > 1000)
                {
                    playersTurning.Remove(kvp.Key);
                }
            }
        }
        private bool StepMovement()
        {
            if (isRaising)
            {
                if (!CanMoveUp()) return false;
                bucketDepth -= 0.1f;
            }
            else
            {
                float nextDepth = bucketDepth + 0.1f;

                if (!CanMoveDown()) return false;

                bucketDepth = nextDepth;
            }

            bucketDepth = GameMath.Clamp(bucketDepth, 0.5f, float.MaxValue);

            BlockPos belowPos = Pos.DownCopy((int)Math.Ceiling(bucketDepth));

            if (!isRaising && IsLiquidBlock(Api.World.BlockAccessor.GetBlock(belowPos)) && BucketIsEmpty())
            {
                FillBucketAtPos(belowPos);
            }

            inventory.TakeLocked = (bucketDepth > 1f);
            MarkDirty(true);

            return true;
        }


        private void FillBucketAtPos(BlockPos pos)
        {
            var (waterType, extracted) = ExtractWaterAtPos(pos, 10);
            if (extracted <= 0) return;
            if (!BucketIsEmpty()) return;
            if (InputSlot.Itemstack?.Collectible?.Code.Path != "woodbucket") return;
    
            ItemStack filledBucket = InputSlot.Itemstack.Clone();
            TreeAttribute contents = new TreeAttribute();
            int totalWaterItems = extracted * 100;
            ItemStack waterStack = new ItemStack(
                Api.World.GetItem(new AssetLocation($"hydrateordiedrate:wellwaterportion-{waterType}")),
                totalWaterItems
            );
            contents["0"] = new ItemstackAttribute(waterStack);
            filledBucket.Attributes["contents"] = contents;
            InputSlot.Itemstack = filledBucket;
            InputSlot.MarkDirty();
        }

        private (string, int) ExtractWaterAtPos(BlockPos pos, int litersNeeded)
        {
            Block block = Api.World.BlockAccessor.GetBlock(pos);
            string codePath = block?.Code?.Path?.ToLowerInvariant() ?? "";

            if (codePath.StartsWith("game:water")
                || codePath.StartsWith("game:saltwater")
                || codePath.StartsWith("game:boilingwater"))
            {
                return (codePath.Contains("salt") ? "saltwater" : "water", litersNeeded);
            }

            if (codePath.Contains("wellwater"))
            {
                BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
                if (be is HydrateOrDiedrate.wellwater.BlockEntityWellWaterData wellData)
                {
                    int available = wellData.Volume;
                    if (available > 0)
                    {
                        int extract = Math.Min(available, litersNeeded);
                        wellData.Volume -= extract;
                        wellData.MarkDirty(true);

                        string waterType = "fresh";
                        if (codePath.Contains("muddysalt")) waterType = "muddysalt";
                        else if (codePath.Contains("taintedsalt")) waterType = "taintedsalt";
                        else if (codePath.Contains("poisonedsalt")) waterType = "poisonedsalt";
                        else if (codePath.Contains("muddy")) waterType = "muddy";
                        else if (codePath.Contains("tainted")) waterType = "tainted";
                        else if (codePath.Contains("poisoned")) waterType = "poisoned";
                        else if (codePath.Contains("salt")) waterType = "salt";

                        return (waterType, extract);
                    }
                }
            }

            return ("fresh", 0);
        }
        private bool BucketIsEmpty()
        {
            if (InputSlot.Empty) return false;
            return !InputSlot.Itemstack.Attributes.HasAttribute("contents");
        }
        private bool IsLiquidBlock(Block block)
        {
            if (block == null) return false;
            string codePath = block.Code?.Path?.ToLowerInvariant() ?? "";
            return codePath.StartsWith("game:water")
                || codePath.StartsWith("game:saltwater")
                || codePath.StartsWith("game:boilingwater")
                || codePath.Contains("wellwater");
        }
        private float GetCurrentTurnSpeed()
        {
            if (quantityPlayersTurning > 0) return 1f;
            if (automated && mpc?.Network != null) return mpc.TrueSpeed;
            return 0f;
        }
        public void SetPlayerTurning(IPlayer player, bool playerTurning)
        {
            if (!automated)
            {
                if (playerTurning) playersTurning[player.PlayerUID] = Api.World.ElapsedMilliseconds;
                else playersTurning.Remove(player.PlayerUID);
                quantityPlayersTurning = playersTurning.Count;
                bool anySneaking = playersTurning.Keys.Any(uid =>
                    Api.World.PlayerByUid(uid)?.Entity?.Controls?.Sneak == true
                );
                bool oldRaising = isRaising;
                isRaising = anySneaking;
                if (isRaising != oldRaising)
                {
                    movementAccumTime = 0f;
                    wasRaising = isRaising; 
                    MarkDirty();
                }
            }
            updateTurningState();
        }
        private bool MovementBlocked()
        {
            if (isRaising)
            {
                return !CanMoveUp();
            }
            else
            {
                return !CanMoveDown();
            }
        }

        private void updateTurningState()
        {
            if (Api?.World == null) return;
            
            bool nowTurning = (quantityPlayersTurning > 0) || (automated && mpc?.TrueSpeed > 0f);
            if (nowTurning != beforeTurning)
            {
                if (renderer != null)
                {
                    renderer.ShouldRotateManual = (quantityPlayersTurning > 0);
                }
                Api.World.BlockAccessor.MarkBlockDirty(Pos, OnRetesselated);
                
                if (nowTurning)
                {
                    ambientSound?.Start();
                }
                else
                {
                    ambientSound?.Stop();
                }

                if (Api.Side == EnumAppSide.Server)
                {
                    MarkDirty(false);
                }
            }
            beforeTurning = nowTurning;
        }

        private void OnRetesselated()
        {
            if (renderer == null) return;
            renderer.ShouldRender = (quantityPlayersTurning > 0 || automated);
        }
        public bool CanTurn()
        {
            if (InputSlot.Itemstack == null) return false;
            return true;
        }
        private void OnSlotModified(int slotid)
        {
            if (Api is ICoreClientAPI && clientDialog != null)
            {
                clientDialog.Update();
            }
            if (slotid == 0 && InputSlot.Empty)
            {
                bucketDepth = 0.5f;
            }
            MarkDirty(true);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null) return false;
            if (blockSel.SelectionBoxIndex == 0)
            {
                if (Api.Side == EnumAppSide.Client)
                {
                    toggleInventoryDialogClient(byPlayer, () =>
                    {
                        clientDialog = new GuiDialogBlockEntityWinch(
                            this.DialogTitle,
                            this.Inventory,
                            this.Pos,
                            this.Api as ICoreClientAPI
                        );
                        clientDialog.Update();
                        return clientDialog;
                    });
                }
                return true;
            }
            else if (blockSel.SelectionBoxIndex == 1)
            {
                return true;
            }

            return false;
        }
        public ItemSlot InputSlot => inventory[0];

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (Block == null) return false;
            if (winchBaseMesh == null || winchTopMesh == null) return false;

            MeshData baseMeshCloned = winchBaseMesh.Clone();
            MeshData topMeshCloned = winchTopMesh.Clone();

            float yRotation = 0f;
            switch (Direction)
            {
                case "east": yRotation = GameMath.PIHALF; break;
                case "south": yRotation = GameMath.PI; break;
                case "west": yRotation = GameMath.PI + GameMath.PIHALF; break;
            }

            baseMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, yRotation, 0f);
            mesher.AddMeshData(baseMeshCloned);

            if (quantityPlayersTurning == 0 && !automated && topMeshCloned != null)
            {
                topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, yRotation, 0f);
                topMeshCloned.Translate(0f, 0.5f, 0f);

                if (Direction == "east")
                {
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, -renderer.AngleRad);
                }
                else if (Direction == "west")
                {
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, renderer.AngleRad);
                }
                else if (Direction == "south")
                {
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -renderer.AngleRad, 0f, 0f);
                }
                else
                {
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), renderer.AngleRad, 0f, 0f);
                }

                mesher.AddMeshData(topMeshCloned);
            }

            if (!InputSlot.Empty)
            {
                MeshData ropeSegmentMesh = GenRopeMesh();
                if (ropeSegmentMesh != null)
                {
                    float ropeSegmentHeight = 1.0f;
                    float initialOffset = 0.75f;
                    float overlapFactor = 0.125f;
                    float segmentSpacing = ropeSegmentHeight * overlapFactor;

                    float effectiveRopeLength = bucketDepth;

                    int segmentCount = (int)Math.Floor(effectiveRopeLength / segmentSpacing);
                    if (effectiveRopeLength % segmentSpacing > 0)
                    {
                        segmentCount++;
                    }
                    segmentCount = Math.Max(segmentCount - 2, 2);

                    for (int i = 0; i < segmentCount; i++)
                    {
                        MeshData segmentMesh = ropeSegmentMesh.Clone();
                        float yPosition = -bucketDepth + initialOffset + i * segmentSpacing;
                        segmentMesh.Translate(0f, yPosition, 0f);
                        mesher.AddMeshData(segmentMesh);

                        if (i == 0)
                        {
                            MeshData knotMesh = GenKnotMesh();
                            if (knotMesh != null)
                            {
                                float knotYOffset = 0.1f;
                                float knotScaleFactor = 1.3f;
                                Vec3f scaleOrigin = new Vec3f(0.5f, 0.5f, 0.5f);
                                knotMesh.Scale(scaleOrigin, knotScaleFactor, knotScaleFactor, knotScaleFactor);
                                knotMesh.Translate(0f, yPosition + knotYOffset, 0f);

                                mesher.AddMeshData(knotMesh);
                            }
                        }
                    }
                }
            }
            if (!automated && quantityPlayersTurning == 0 && !InputSlot.Empty)
            {
                MeshData bucketMesh;
                if (InputSlot.Itemstack.Class == EnumItemClass.Block)
                {
                    tesselator.TesselateBlock(InputSlot.Itemstack.Block, out bucketMesh);
                }
                else
                {
                    tesselator.TesselateItem(InputSlot.Itemstack.Item, out bucketMesh);
                }

                if (bucketMesh != null)
                {
                    float bucketYRotation = 0f;
                    switch (Direction)
                    {
                        case "east": bucketYRotation = GameMath.PIHALF; break;
                        case "south": bucketYRotation = GameMath.PI; break;
                        case "west": bucketYRotation = GameMath.PI + GameMath.PIHALF; break;
                    }
                    bucketMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, bucketYRotation, 0f);
                    bucketMesh.Translate(0f, -bucketDepth, 0f);
                    mesher.AddMeshData(bucketMesh);
                }
            }

            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null)
            {
                inventory.FromTreeAttributes(invTree);
                if (this.Api != null)
                {
                    inventory.AfterBlocksLoaded(this.Api.World);
                }
            }
            inputTurnTime = tree.GetFloat("inputTurnTime", 0f);
            nowOutputFace = tree.GetInt("nowOutputFace", 0);
            bucketDepth = tree.GetFloat("bucketDepth", 0.5f);
            isRaising = tree.GetBool("isRaising", false);
            movementAccumTime = 0f;
            inventory.TakeLocked = (bucketDepth > 1f);
            inventory.PutLocked = false;
            if (worldForResolving.Side == EnumAppSide.Client)
            {
                clientDialog?.Update();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);
            if (inventory != null)
            {
                TreeAttribute invTree = new TreeAttribute();
                inventory.ToTreeAttributes(invTree);
                tree["inventory"] = invTree;
            }
            tree.SetFloat("inputTurnTime", inputTurnTime);
            tree.SetInt("nowOutputFace", nowOutputFace);
            tree.SetFloat("bucketDepth", bucketDepth);
            tree.SetBool("isRaising", isRaising);
            List<int> clientIds = new List<int>();
            foreach (var kvp in playersTurning)
            {
                IPlayer plr = Api.World.PlayerByUid(kvp.Key);
                if (plr != null)
                {
                    clientIds.Add(plr.ClientId);
                }
            }
            tree["clientIdsTurning"] = new IntArrayAttribute(clientIds.ToArray());
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            ambientSound?.Stop();
            ambientSound?.Dispose();
            clientDialog?.TryClose();
            renderer?.Dispose();
            renderer = null;
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                Vec3d dropPos = this.Pos.ToVec3d().Add(0.5, -this.bucketDepth, 0.5);
                this.Inventory.DropAll(dropPos);
            }
            base.OnBlockBroken(byPlayer);
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            renderer?.Dispose();
            if (ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
            }
        }
        ~BlockEntityWinch()
        {
            if (ambientSound != null) ambientSound.Dispose();
        }
        public string Direction
        {
            get { return Block?.LastCodePart(0) ?? "north"; }
        }
        public virtual string DialogTitle
        {
            get { return Lang.Get("hydrateordiedrate:Winch"); }
        }
    }
}