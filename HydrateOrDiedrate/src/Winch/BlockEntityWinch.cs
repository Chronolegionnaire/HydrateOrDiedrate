using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.wellwater;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch;

public class BlockEntityWinch : BlockEntityOpenableContainer
{
    public const string KnotMeshPath = "shapes/block/winch/knot.json";
    public const string RopeMeshPath = "shapes/block/winch/rope1x1.json";
    public const string WinchTopMeshPath = "shapes/block/winch/top.json";
    public const string WinchBaseMeshPath = "shapes/block/winch/base.json";

    public float MeshAngle;

    public float inputTurnTime;
    public float prevInputTurnTime;
    private GuiDialogBlockEntityWinch clientDialog;
    private WinchTopRenderer renderer;
    private bool automated;
    private BEBehaviorMPConsumer mpc;
    private Dictionary<string, long> playersTurning = [];

    private int quantityPlayersTurning;
    private bool beforeTurning;
    private int nowOutputFace;
    public float bucketDepth = 0.5f;

    private MeshData winchBaseMesh;
    private MeshData winchTopMesh;
    
    public bool isRaising;
    public bool IsRaising => isRaising;

    public bool CanMoveDown()
    {
        float nextDepth = bucketDepth + 0.1f;
        BlockPos checkPos = Pos.DownCopy((int)Math.Ceiling(nextDepth));

        if (checkPos.Y < 0) return false;

        Block blockBelow = Api.World.BlockAccessor.GetBlock(checkPos);

        if (blockBelow == Block) return true;

        if (blockBelow.Replaceable < 6000 && !IsLiquidBlock(blockBelow)) return false;

        return true;
    }


    public bool CanMoveUp() => bucketDepth > 0.5f;

    public override string InventoryClassName => "winch";
    public override InventoryBase Inventory => inventory;
    private readonly InventoryWinch inventory;

    public BlockEntityWinch()
    {
        inventory = new InventoryWinch(null, null);
        inventory.SlotModified += OnSlotModified;
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        //TODO: base class already late initializes with a different Id... we should get rid of this but it will probably break existing winch inventories
        inventory.LateInitialize($"winch-{Pos.X}/{Pos.Y}/{Pos.Z}", api);

        RegisterGameTickListener(Every100ms, 100);
        RegisterGameTickListener(Every500ms, 500);
        RegisterGameTickListener(OnEverySecond, 1000);

        if (api is not ICoreClientAPI capi) return;

        winchBaseMesh = GetMesh(WinchBaseMeshPath);
        winchTopMesh = GetMesh(WinchTopMeshPath);

        renderer = new WinchTopRenderer(capi, Pos, winchTopMesh, Block.Variant["side"])
        {
            mechPowerPart = mpc
        };

        if (automated)
        {
            renderer.ShouldRender = true;
            renderer.ShouldRotateAutomated = true;
        }

        capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "winch");
    }

    private void OnConnected()
    {
        automated = true;
        quantityPlayersTurning = 0;
        
        if (renderer is null) return;
        renderer.ShouldRender = true;
        renderer.ShouldRotateAutomated = true;
    }

    private void OnDisconnected()
    {
        automated = false;
        if (renderer is null) return;
        
        renderer.ShouldRender = false;
        renderer.ShouldRotateAutomated = false;
    }

    public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
    {
        base.CreateBehaviors(block, worldForResolve);
        mpc = GetBehavior<BEBehaviorMPConsumer>();
        if (mpc == null) return;

        mpc.OnConnected = OnConnected;
        mpc.OnDisconnected = OnDisconnected;
    }

    private MeshData GetMesh(string path)
    {
        if (Api is not ICoreClientAPI capi) return null;

        Shape shape = Shape.TryGet(Api, new AssetLocation("hydrateordiedrate", path));
        if (shape is null) return null;

        capi.Tesselator.TesselateShape(Block, shape, out MeshData mesh);
        return mesh;
    }

    private void Every100ms(float dt)
    {
        if (Api.Side == EnumAppSide.Server && quantityPlayersTurning > 0)
        {
            bool anySneaking = playersTurning.Keys.Any(uid =>
                Api.World.PlayerByUid(uid)?.Entity?.Controls?.Sneak == true
            );
            isRaising = anySneaking;

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
            isRaising = (turnDir == EnumRotDirection.Counterclockwise);

            if (StepMovement())
            {
                MarkDirty();
            }
        }

        updateTurningState();
    }


    private void Every500ms(float dt)
    {
        if (Api.Side == EnumAppSide.Server && (GetCurrentTurnSpeed() > 0f || prevInputTurnTime != inputTurnTime))
        {
            ItemStack itemstack = inventory?[0]?.Itemstack;
            if (itemstack?.Collectible?.IsLiquid() != null)
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

    private void OnEverySecond(float dt)
    {
        float speed = GetCurrentTurnSpeed();
        if (Api.World.Rand.NextDouble() < (speed / 4f))
        {
            Api.World.PlaySoundAt(
                new AssetLocation("game","sounds/block/woodcreak"),
                Pos.X + 0.5,
                Pos.Y + 0.5,
                Pos.Z + 0.5,
                null,
                0.85f + speed,
                32f,
                1f
            );
        }
    }
    private bool StepMovement()
    {
        if (isRaising)
        {
            if (!CanMoveUp()) return false;
            bucketDepth -= ModConfig.Instance.GroundWater.WinchRaiseSpeed;
        }
        else
        {
            float nextDepth = bucketDepth + ModConfig.Instance.GroundWater.WinchLowerSpeed;

            if (!CanMoveDown()) return false;

            bucketDepth = nextDepth;
        }

        bucketDepth = GameMath.Clamp(bucketDepth, 0.5f, float.MaxValue);

        BlockPos belowPos = Pos.DownCopy((int)Math.Ceiling(bucketDepth));
        Block blockBelow = Api.World.BlockAccessor.GetBlock(belowPos);
        bool notRaising = !isRaising;
        bool isLiquid = IsLiquidBlock(blockBelow);
        bool bucketEmpty = BucketIsEmpty();
        if (notRaising && isLiquid && bucketEmpty)
        {
            FillBucketAtPos(belowPos);
        }

        inventory.TakeLocked = (bucketDepth > 1f);
        MarkDirty(true);

        return true;
    }


    private void FillBucketAtPos(BlockPos pos)
    {
        if (InputSlot.Empty || InputSlot.Itemstack.Collectible is not BlockLiquidContainerBase container) return;
        
        if (!BucketIsEmpty()) return;
        int bucketCapacity = (int) container.CapacityLitres;
        var (waterType, extracted) = ExtractWaterAtPos(pos, bucketCapacity);
        if (extracted <= 0) return;
        ItemStack filledBucket = InputSlot.Itemstack.Clone();
        int totalWaterItems = extracted * 100; //TODO maybe use the actual props to calculate just in case someone changes those
        
        AssetLocation waterAsset = waterType switch
        {
            "saltwater" => new AssetLocation("game", "saltwaterportion"),
            "water" => new AssetLocation("game", "waterportion"),
            _ => new AssetLocation("hydrateordiedrate", $"wellwaterportion-{waterType}"),
        };
        
        ItemStack waterStack = new(Api.World.GetItem(waterAsset), totalWaterItems);
        
        filledBucket.Attributes["contents"] = new TreeAttribute
        {
            ["0"] = new ItemstackAttribute(waterStack)
        };
        InputSlot.Itemstack = filledBucket;
        InputSlot.MarkDirty();
    }

    private (string, int) ExtractWaterAtPos(BlockPos pos, int litersNeeded)
    {
        Block block = Api.World.BlockAccessor.GetBlock(pos);
        string codePath = block?.Code?.Path?.ToLowerInvariant() ?? "";

        if (codePath.StartsWith("water") || codePath.StartsWith("saltwater") || codePath.StartsWith("boilingwater"))
        {
            return (codePath.Contains("salt") ? "saltwater" : "water", litersNeeded);
        }

        if (codePath.Contains("wellwater"))
        {
            BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
            if (be is not BlockEntityWellWaterData)
            {
                BlockPos naturalPos = FindNaturalSourceInLiquidChain(Api.World.BlockAccessor, pos);
                if (naturalPos != null)
                {
                    be = Api.World.BlockAccessor.GetBlockEntity(naturalPos);
                }
            }

            if (be is BlockEntityWellWaterData wellData)
            {
                int available = wellData.Volume;
                if (available > 0)
                {
                    int extract = Math.Min(available, litersNeeded);
                    wellData.Volume -= extract;
                    wellData.MarkDirty(true);

                    string waterType = "fresh";
                    if (codePath.Contains("muddysalt"))
                        waterType = "muddysalt";
                    else if (codePath.Contains("taintedsalt"))
                        waterType = "taintedsalt";
                    else if (codePath.Contains("poisonedsalt"))
                        waterType = "poisonedsalt";
                    else if (codePath.Contains("muddy"))
                        waterType = "muddy";
                    else if (codePath.Contains("tainted"))
                        waterType = "tainted";
                    else if (codePath.Contains("poisoned"))
                        waterType = "poisoned";
                    else if (codePath.Contains("salt"))
                        waterType = "salt";

                    return (waterType, extract);
                }
            }
        }

        return ("fresh", 0);
    }

    public BlockPos FindNaturalSourceInLiquidChain(IBlockAccessor blockAccessor, BlockPos pos, HashSet<BlockPos> visited = null)
    {
        visited ??= [];
        if (visited.Contains(pos)) return null;
        visited.Add(pos);

        Block currentBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
        if (currentBlock != null && currentBlock.Variant["createdBy"] == "natural")
        {
            return pos;
        }

        foreach (BlockFacing facing in BlockFacing.ALLFACES)
        {
            BlockPos neighborPos = pos.AddCopy(facing);
            Block neighborBlock = blockAccessor.GetBlock(neighborPos, BlockLayersAccess.Fluid);
            if (neighborBlock != null && neighborBlock.Code?.Domain == "game")
            {
                continue;
            }

            if (neighborBlock != null && neighborBlock.IsLiquid())
            {
                var naturalSourcePos = FindNaturalSourceInLiquidChain(blockAccessor, neighborPos, visited);
                if (naturalSourcePos != null)
                {
                    return naturalSourcePos;
                }
            }
        }

        return null;
    }

    private bool BucketIsEmpty()
    {
        if (InputSlot.Empty) return false;

        if (!InputSlot.Itemstack.Attributes.HasAttribute("contents")) return true;

        ITreeAttribute contents = InputSlot.Itemstack.Attributes.GetTreeAttribute("contents");
        if (contents == null)
        {
            return true;
        }

        var contentStack = contents.GetItemstack("0");
        return contentStack == null;
    }
    private bool IsLiquidBlock(Block block)
    {
        if (block == null) return false;
        string codePath = block.Code?.Path?.ToLowerInvariant() ?? "";
        return codePath.StartsWith("water")
            || codePath.Contains("saltwater")
            || codePath.Contains("boilingwater")
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
                MarkDirty();
            }
        }
        updateTurningState();
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
        if (blockSel is not null && blockSel.SelectionBoxIndex == 0 && Api is ICoreClientAPI capi)
        {
            toggleInventoryDialogClient(byPlayer, () =>
            {
                //clientDialog = new GuiDialogBlockEntityInventory(
                //        DialogTitle,
                //        Inventory,
                //        Pos,
                //        1,
                //        capi
                //    );
                clientDialog = new GuiDialogBlockEntityWinch(
                    DialogTitle,
                    Inventory,
                    Pos,
                    capi
                );
                clientDialog.Update();
                return clientDialog;
            });
        }

        return false;
    }

    public ItemSlot InputSlot => inventory[0];

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if (Block == null) return false;
        if (winchBaseMesh == null || winchTopMesh == null) return false;

        MeshData baseMeshCloned = winchBaseMesh.Clone();
        MeshData topMeshCloned = winchTopMesh.Clone();

        float yRotation = 0f;
        switch (Block.Variant["side"])
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

            switch (Block.Variant["side"])
            {
                case "east":
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, -renderer.AngleRad);
                    break;

                case "west":
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, 0f, renderer.AngleRad);
                    break;

                case "south":
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), -renderer.AngleRad, 0f, 0f);
                    break;

                default:
                    topMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), renderer.AngleRad, 0f, 0f);
                    break;
            }

            mesher.AddMeshData(topMeshCloned);
        }

        if (!InputSlot.Empty)
        {
            MeshData ropeSegmentMesh = GetMesh(RopeMeshPath);
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
                        MeshData knotMesh = GetMesh(KnotMeshPath);
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
            try
            {
                MeshData bucketMesh;
                if (InputSlot.Itemstack.Class == EnumItemClass.Block)
                {
                    if (InputSlot.Itemstack.Block == null) return true;
                    tessThreadTesselator.TesselateBlock(InputSlot.Itemstack.Block, out bucketMesh);
                }
                else
                {
                    if (InputSlot.Itemstack.Item == null) return true;
                    tessThreadTesselator.TesselateItem(InputSlot.Itemstack.Item, out bucketMesh);
                }

                if (bucketMesh != null && bucketMesh.VerticesCount > 0)
                {
                    float bucketYRotation = Block.Variant["side"] switch
                    {
                        "east" => GameMath.PIHALF,
                        "south" => GameMath.PI,
                        "west" => GameMath.PI + GameMath.PIHALF,
                        _ => 0
                    };

                    bucketMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, bucketYRotation, 0f);
                    bucketMesh.Translate(0f, -bucketDepth, 0f);
                    mesher.AddMeshData(bucketMesh);
                    string containerCodePath = InputSlot.Itemstack?.Collectible?.Code?.Path ?? ""; //TODO
                    if (containerCodePath.StartsWith("woodbucket") ||
                        containerCodePath.StartsWith("temporalbucket"))
                    {
                        if (InputSlot.Itemstack.Attributes?.HasAttribute("contents") == true)
                        {
                            var contents = InputSlot.Itemstack.Attributes.GetTreeAttribute("contents");
                            if (contents != null)
                            {
                                var contentStack = contents.GetItemstack("0");
                                if (contentStack != null && contentStack.Collectible == null)
                                {
                                    contentStack.ResolveBlockOrItem(Api.World);
                                }

                                if (contentStack?.Collectible != null)
                                {
                                    var props = BlockLiquidContainerBase.GetContainableProps(contentStack);
                                    if (props != null)
                                    {
                                        Shape contentShape = null;
                                        string shapePath = "game:shapes/block/wood/bucket/liquidcontents";
                                        if (props.IsOpaque)
                                        {
                                            shapePath = "game:shapes/block/wood/bucket/contents";
                                        }

                                        contentShape = Shape.TryGet(Api, shapePath + ".json");
                                        if (contentShape != null)
                                        {
                                            ContainerTextureSource textureSource = new ContainerTextureSource(
                                                Api as ICoreClientAPI,
                                                contentStack,
                                                props.Texture
                                            );
                                            
                                            tessThreadTesselator.TesselateShape(GetType().Name, contentShape, out MeshData contentMesh, textureSource);

                                            if (contentMesh != null)
                                            {
                                                float maxLiquidHeight = 0.435f;
                                                int bucketCapacity = InputSlot.Itemstack.Collectible is BlockLiquidContainerBase container
                                                    ? (int)container.CapacityLitres
                                                    : 10;
                                                
                                                float liquidPercentage =
                                                    (float)contentStack.StackSize /
                                                    (props.ItemsPerLitre * bucketCapacity);
                                                float liquidHeight = liquidPercentage * maxLiquidHeight;
                                                float liquidOffset = 0f;
                                                contentMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, bucketYRotation, 0f);
                                                contentMesh.Translate(0f, -bucketDepth + liquidHeight + liquidOffset, 0f);
                                                mesher.AddMeshData(contentMesh);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Api.Logger.Error("Error rendering bucket contents: {0}", e);
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
            if (Api != null)
            {
                inventory.AfterBlocksLoaded(Api.World);
            }
        }
        inputTurnTime = tree.GetFloat("inputTurnTime", 0f);
        nowOutputFace = tree.GetInt("nowOutputFace", 0);
        bucketDepth = tree.GetFloat("bucketDepth", 0.5f);
        isRaising = tree.GetBool("isRaising", false);

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
        tree.SetFloat("inputTurnTime", inputTurnTime);
        tree.SetInt("nowOutputFace", nowOutputFace);
        tree.SetFloat("bucketDepth", bucketDepth);
        tree.SetBool("isRaising", isRaising);

        TreeAttribute invTree = new();
        inventory.ToTreeAttributes(invTree);
        tree["inventory"] = invTree;

        List<int> clientIds = new(playersTurning.Count);
        foreach (var kvp in playersTurning)
        {
            IPlayer plr = Api.World.PlayerByUid(kvp.Key);
            if (plr is not null)
            {
                clientIds.Add(plr.ClientId);
            }
        }
        tree["clientIdsTurning"] = new IntArrayAttribute(clientIds.ToArray());
    }

    public override void Dispose()
    {
        base.Dispose();

        if(clientDialog is not null)
        {
            if(clientDialog.IsOpened()) clientDialog.TryClose();
            clientDialog.Dispose();
        }
        
        renderer?.Dispose();
        renderer = null;
    }

    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            Inventory.DropAll(Pos.ToVec3d().Add(0.5, -bucketDepth, 0.5));
        }
        base.OnBlockBroken(byPlayer);
    }
    
    public virtual string DialogTitle => Lang.Get("hydrateordiedrate:Winch");

    private long GetTotalShaftWaterVolume(BlockPos springPos)
    {
        long total = 0;
        var pos = Pos.DownCopy();
        while (pos.Y >= 0 && pos.Y != springPos.Y)
        {
            if (Api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityWellWaterData waterData)
            {
                total += waterData.Volume;
            }

            pos.Y--;
        }
        return total;
    }

    
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (!ModConfig.Instance.GroundWater.WinchOutputInfo) return;

        
        BlockEntityWellSpring foundSpring = null;
        const int maxSearchDepth = 256;
        int searchLevels = 0;

        BlockPos searchPos = Pos.DownCopy();
        while (searchPos.Y >= 0 && searchLevels < maxSearchDepth)
        {
            if (Api.World.BlockAccessor.GetBlockEntity(searchPos) is BlockEntityWellSpring spring)
            {
                foundSpring = spring;
                break;
            }
            
            searchPos.Y--;
            searchLevels++;
        }

        if (foundSpring is null)
        {
            dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.noSpring"));
            return;
        }
        
        dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.springDetected"));
        dsc.Append("  ");
        dsc.AppendLine(Lang.Get(
            "hydrateordiedrate:winch.waterType",
            foundSpring.GetWaterType()
        ));

        dsc.Append("  ");
        dsc.AppendLine(Lang.Get(
            "hydrateordiedrate:winch.outputRate",
            foundSpring.GetCurrentOutputRate()
        ));

        dsc.Append("  ");
        dsc.AppendLine(Lang.Get(
            "hydrateordiedrate:winch.retentionVolume",
            foundSpring.GetRetentionDepth() * 70
        ));

        dsc.Append("  ");
        dsc.AppendLine(Lang.Get(
            "hydrateordiedrate:winch.totalShaftVolume",
            GetTotalShaftWaterVolume(foundSpring.Pos)
        ));
    }
}