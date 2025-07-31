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
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch;

public class BlockEntityWinch : BlockEntityOpenableContainer
{
    public const float minTurnpeed = 0.00001f;
    public const string KnotMeshPath = "shapes/block/winch/knot.json";
    public const string RopeMeshPath = "shapes/block/winch/rope1x1.json";
    public const string WinchTopMeshPath = "shapes/block/winch/top.json";
    public const string WinchBaseMeshPath = "shapes/block/winch/base.json";

    public float MeshAngle;

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
    
    //TODO for clarity this should probably be a directional enum
    public bool isRaising;
    public bool IsRaising => isRaising;

    public bool CanMoveDown()
    {
        BlockPos checkPos = Pos.DownCopy((int)Math.Ceiling(bucketDepth + 0.1f));

        if (checkPos.Y < 0) return false;

        Block blockBelow = Api.World.BlockAccessor.GetBlock(checkPos);

        return blockBelow.Replaceable >= 6000 || blockBelow.IsLiquid();
    }


    public bool CanMoveUp() => bucketDepth > 0.5f;

    public bool CanMove() => IsRaising ? CanMoveUp() : CanMoveDown();

    public float GetCurrentTurnSpeed()
    {
        if (quantityPlayersTurning > 0) return 1f;
        if (automated && mpc?.Network is not null) return mpc.TrueSpeed;
        return 0f;
    }

    public override string InventoryClassName => "winch";
    public override InventoryBase Inventory => inventory;
    private readonly InventoryWinch inventory; //TODO maybe limit inventory to only accept buckets lol

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

        if (api is not ICoreClientAPI capi)
        {
            RegisterGameTickListener(Server_Every100ms, 100);
            RegisterGameTickListener(Server_Every500ms, 500);
            RegisterGameTickListener(ChanceForSoundEffect, 1000);
            return;
        }

        winchBaseMesh = GetMesh(WinchBaseMeshPath);
        winchTopMesh = GetMesh(WinchTopMeshPath);

        renderer = new WinchTopRenderer(capi, this, winchTopMesh, Block.Variant["side"])
        {
            mechPowerPart = mpc
        };

        if (automated)
        {
            renderer.ShouldRotateAutomated = true;
        }

        capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "winch");
    }

    private void OnConnected()
    {
        automated = true;
        quantityPlayersTurning = 0;
        
        if (renderer is null) return;
        renderer.ShouldRotateAutomated = true;
    }

    private void OnDisconnected()
    {
        automated = false;
        if (renderer is null) return;
        
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

    private void UpdateIsRaising()
    {
        if (automated && mpc?.Network is not null)
        {
            isRaising = mpc.Network.TurnDir == EnumRotDirection.Counterclockwise;
        }
        else if(quantityPlayersTurning > 0)
        {
            isRaising = playersTurning.Keys.Any(uid => Api.World.PlayerByUid(uid)?.Entity?.Controls?.Sneak == true);
        }
    }

    private void Server_Every100ms(float dt)
    {
        if (GetCurrentTurnSpeed() > minTurnpeed)
        {
            UpdateIsRaising();

            if (StepMovement())
            {
                MarkDirty();
            }
            else
            {
                playersTurning.Clear();
                quantityPlayersTurning = 0;
            }
        }

        UpdateTurningState();
    }


    private void Server_Every500ms(float dt)
    {
        foreach (var kvp in playersTurning.ToArray())
        {
            if (Api.World.ElapsedMilliseconds - kvp.Value > 1000)
            {
                playersTurning.Remove(kvp.Key);
            }
        }
    }

    private void ChanceForSoundEffect(float dt)
    {
        float speed = GetCurrentTurnSpeed();
        if (Api.World.Rand.NextDouble() < (speed / 2f))
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
        Block blockBelow = Api.World.BlockAccessor.GetBlock(belowPos, BlockLayersAccess.Fluid);

        if (!isRaising && blockBelow.IsLiquid())
        {
            TryFillBucketAtPos(belowPos);
        }

        inventory.TakeLocked = bucketDepth > 1f;
        MarkDirty(true);

        return true;
    }


    private void TryFillBucketAtPos(BlockPos pos)
    {
        if (InputSlot.Empty || InputSlot.Itemstack.Collectible is not BlockLiquidContainerBase container) return;
        
        if (!BucketIsEmpty()) return;
        int bucketCapacity = (int) container.CapacityLitres;
        var stack = ExtractStackAtPos(pos, bucketCapacity);
        if(stack is null || stack.StackSize <= 0) return;
        
        InputSlot.Itemstack.Attributes["contents"] = new TreeAttribute
        {
            ["0"] = new ItemstackAttribute(stack)
        };
        InputSlot.MarkDirty();
    }

    //TODO all those different well water blocks should really be variants instead
    public ItemStack ExtractStackAtPos(BlockPos pos, int litersToExtract)
    {
        Block block = Api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
        if(block.Attributes is null) return null;

        var props = block.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
        var stack = props?.WhenFilled?.Stack;
        if(stack is null || !stack.Resolve(Api.World, nameof(BlockEntityWinch))) return null;

        if (block.Code.Path.StartsWith("wellwater"))
        {
            if(Api.World.BlockAccessor.GetBlockEntity(pos) is not BlockEntityWellWaterData wellData)
            {
                BlockPos naturalPos = FindNaturalSourceInLiquidChain(Api.World.BlockAccessor, pos);
                if (naturalPos != null)
                {
                    wellData = Api.World.BlockAccessor.GetBlockEntity<BlockEntityWellWaterData>(naturalPos);
                }
                else wellData = null;
            }

            if(wellData is not null)
            {

                litersToExtract = Math.Min(wellData.Volume, litersToExtract);
                wellData.Volume -= litersToExtract;
                wellData.MarkDirty(true);
            }
        }

        var itemProps = stack.ResolvedItemstack.Collectible.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
        if(itemProps is null) return null;

        stack.ResolvedItemstack.StackSize = (int)Math.Round(itemProps.ItemsPerLitre * litersToExtract);

        return stack.ResolvedItemstack;
    }

    public static BlockPos FindNaturalSourceInLiquidChain(IBlockAccessor blockAccessor, BlockPos pos, HashSet<BlockPos> visited = null)
    {
        visited ??= [];
        if (visited.Contains(pos)) return null;
        visited.Add(pos);

        Block currentBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
        if (currentBlock is not null && currentBlock.Variant["createdBy"] == "natural") return pos;

        foreach (BlockFacing facing in BlockFacing.ALLFACES)
        {
            BlockPos neighborPos = pos.AddCopy(facing);
            Block neighborBlock = blockAccessor.GetBlock(neighborPos, BlockLayersAccess.Fluid);
            
            if(neighborBlock is not null)
            {
                if (neighborBlock.Code?.Domain == "game") continue; //TODO: why only ignore game domain? does this mean liquids from other mods are OK but base game not?

                if (neighborBlock.IsLiquid())
                {
                    var naturalSourcePos = FindNaturalSourceInLiquidChain(blockAccessor, neighborPos, visited);
                    if (naturalSourcePos is not null)
                    {
                        return naturalSourcePos;
                    }
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
        return contents is null || contents.GetItemstack("0") is null;
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

        UpdateTurningState();
    }

    private void UpdateTurningState()
    {
        if (Api?.World == null) return;
        
        bool nowTurning = GetCurrentTurnSpeed() > minTurnpeed;
        if (nowTurning != beforeTurning)
        {
            if (renderer is not null) renderer.ShouldRotateManual = quantityPlayersTurning > 0;

            Api.World.BlockAccessor.MarkBlockDirty(Pos); //TODO check

            if (Api.Side == EnumAppSide.Server) MarkDirty();
        }
        beforeTurning = nowTurning;
    }

    public bool CanTurn() => InputSlot.Itemstack is not null;

    private void OnSlotModified(int slotid)
    {
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
            toggleInventoryDialogClient(byPlayer, () => new GuiDialogBlockEntityWinch(
                DialogTitle,
                Inventory,
                Pos,
                capi
            ));
        }

        return false;
    }

    public ItemSlot InputSlot => inventory[0];

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if (Block is null || winchBaseMesh is null || winchTopMesh is null) return false;

        MeshData baseMeshCloned = winchBaseMesh.Clone();
        
        var yRotation = Block.Variant["side"] switch
        {
            "east" => GameMath.PIHALF,
            "south" => GameMath.PI,
            "west" => GameMath.PI + GameMath.PIHALF,
            _ => 0f
        };
        baseMeshCloned.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, yRotation, 0f);
        mesher.AddMeshData(baseMeshCloned);

        if (InputSlot.Empty) return true;

        MeshData ropeSegmentMesh = GetMesh(RopeMeshPath);
        if (ropeSegmentMesh is not null)
        {
            const float ropeSegmentHeight = 1.0f;
            const float initialOffset = 0.75f;
            const float overlapFactor = 0.125f;
            const float segmentSpacing = ropeSegmentHeight * overlapFactor;
        
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
                        const float knotYOffset = 0.1f;
                        const float knotScaleFactor = 1.3f;
                        
                        var scaleOrigin = new Vec3f(0.5f, 0.5f, 0.5f);
                        knotMesh.Scale(scaleOrigin, knotScaleFactor, knotScaleFactor, knotScaleFactor);
                        knotMesh.Translate(0f, yPosition + knotYOffset, 0f);
        
                        mesher.AddMeshData(knotMesh);
                    }
                }
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

        nowOutputFace = tree.GetInt("nowOutputFace", 0);
        bucketDepth = tree.GetFloat("bucketDepth", 0.5f);
        isRaising = tree.GetBool("isRaising", false);
        inventory.TakeLocked = bucketDepth > 1f;
        inventory.PutLocked = false;
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("meshAngle", MeshAngle);
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