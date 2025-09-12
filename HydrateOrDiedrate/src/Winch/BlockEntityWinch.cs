using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.wellwater;
using HydrateOrDiedrate.Winch;
using HydrateOrDiedrate.Winch.Inventory;
using System;
using System.Collections.Generic;
using System.Text;
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
        public const float minTurnSpeed = 0.00001f;
        public const float minBucketDepth = 0.5f;
        public const string WinchBaseMeshPath = "shapes/block/winch/base.json";

        private WinchTopRenderer renderer;
        private BEBehaviorMPConsumer mpc;
        public bool ConnectedToMechanicalNetwork { get; private set; }

        public IPlayer RotationPlayer { get; private set; }

        public float BucketDepth { get; private set; } = minBucketDepth;

        private MeshData winchBaseMesh;

        public bool IsRaising { get; private set; }

        public EWinchRotationMode RotationMode => ConnectedToMechanicalNetwork ? EWinchRotationMode.MechanicalNetwork : EWinchRotationMode.Player;

        public float GetCurrentTurnSpeed() => RotationMode switch
        {
            EWinchRotationMode.MechanicalNetwork => mpc.TrueSpeed,
            EWinchRotationMode.Player => RotationPlayer is not null ? 1f : 0f,
            _ => 0f
        };

        public override string InventoryClassName => "winch";

        public override InventoryBase Inventory { get; }

        public BlockEntityWinch()
        {
            Inventory = new InventoryWinch(this, null, null);
            Inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api is not ICoreClientAPI capi)
            {
                RegisterGameTickListener(ChanceForSoundEffect, 1000);
                return;
            }

            winchBaseMesh = GetMesh(WinchBaseMeshPath);

            var yRotation = Block.Variant["side"] switch
            {
                "east" => GameMath.PIHALF,
                "south" => GameMath.PI,
                "west" => GameMath.PI + GameMath.PIHALF,
                _ => 0f
            };
            winchBaseMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, yRotation, 0f);

            renderer = new WinchTopRenderer(capi, this, Block.Variant["side"]);

            capi.Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "winch");
        }

        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);

            mpc = GetBehavior<BEBehaviorMPConsumer>();
            if (mpc == null) return;

            mpc.OnConnected = OnConnected;
            mpc.OnDisconnected = OnDisconnected;
        }

        private long? MPTickListenerId;

        private void OnConnected()
        {
            ConnectedToMechanicalNetwork = true;
            RotationPlayer = null;
            if (Api.Side == EnumAppSide.Server && MPTickListenerId is null) MPTickListenerId = RegisterGameTickListener((float deltaTime) => ContinueTurning(deltaTime), 100);
            MarkDirty();
        }

        private void OnDisconnected()
        {
            ConnectedToMechanicalNetwork = false;
            if (MPTickListenerId is not null)
            {
                UnregisterGameTickListener(MPTickListenerId.Value);
                MPTickListenerId = null;
            }
        }

        internal MeshData GetMesh(string path)
        {
            if (Api is not ICoreClientAPI capi) return null;

            Shape shape = Shape.TryGet(Api, new AssetLocation("hydrateordiedrate", path));
            if (shape is null) return null;

            capi.Tesselator.TesselateShape(Block, shape, out MeshData mesh);
            return mesh;
        }

        public void UpdateIsRaising() => IsRaising = RotationMode switch
        {
            EWinchRotationMode.MechanicalNetwork => mpc.Network.TurnDir == EnumRotDirection.Counterclockwise,
            EWinchRotationMode.Player => RotationPlayer?.Entity?.Controls.Sneak ?? false,
            _ => false,
        };

        private void ChanceForSoundEffect(float dt)
        {
            float speed = GetCurrentTurnSpeed();
            if (Api.World.Rand.NextDouble() < (speed / 2f))
            {
                Api.World.PlaySoundAt(
                    new AssetLocation("game", "sounds/block/woodcreak"),
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

        private void TryFillBucketAtPos(BlockPos pos)
        {
            if (InputSlot.Empty || InputSlot.Itemstack.Collectible is not BlockLiquidContainerBase container || !BucketIsEmpty()) return;

            int bucketCapacity = (int)container.CapacityLitres;
            var stack = ExtractStackAtPos(pos, bucketCapacity);
            if (stack is null || stack.StackSize <= 0) return;

            InputSlot.Itemstack.Attributes["contents"] = new TreeAttribute
            {
                ["0"] = new ItemstackAttribute(stack)
            };
            InputSlot.MarkDirty();
            MarkDirty();
        }

        public ItemStack ExtractStackAtPos(BlockPos pos, int litersToExtract)
        {
            Block block = Api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block?.Attributes is null) return null;

            var props = block.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            var stack = props?.WhenFilled?.Stack;
            if (stack is null || !stack.Resolve(Api.World, nameof(BlockEntityWinch))) return null;

            int extractedLiters = litersToExtract;
            if (block.Code?.Path?.StartsWith("wellwater") == true)
            {
                var spring = FindOwningSpringFor(pos);
                if (spring is not null)
                {
                    extractedLiters = spring.TryExtractLitersAt(pos, litersToExtract);
                    if (extractedLiters <= 0) return null;
                }
                else
                {
                    return null;
                }
            }

            var itemProps = stack.ResolvedItemstack.Collectible.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            if (itemProps is null) return null;

            stack.ResolvedItemstack.StackSize = (int)Math.Round(itemProps.ItemsPerLitre * extractedLiters);
            return stack.ResolvedItemstack;
        }

        private BlockEntityWellSpring FindOwningSpringFor(BlockPos pos)
        {
            var p = pos.DownCopy();
            const int maxDepth = 512;

            for (int i = 0; i < maxDepth && p.Y >= 0; i++, p.Y--)
            {
                var be = Api.World.BlockAccessor.GetBlockEntity(p);
                if (be is BlockEntityWellSpring spring) return spring;
            }

            return null;
        }

        public static BlockPos FindNaturalSourceInLiquidChain(IBlockAccessor blockAccessor, BlockPos pos, HashSet<BlockPos> visited = null)
        {
            visited ??= [];
            if (visited.Contains(pos)) return null;
            visited.Add(pos);

            Block currentBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (currentBlock is not null && currentBlock.Variant?["createdBy"] == "natural") return pos;

            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                BlockPos neighborPos = pos.AddCopy(facing);
                Block neighborBlock = blockAccessor.GetBlock(neighborPos, BlockLayersAccess.Fluid);

                if (neighborBlock is not null)
                {
                    if (neighborBlock.Code?.Domain == "game") continue;

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

        public bool TryStartTurning(IPlayer player)
        {
            if (ConnectedToMechanicalNetwork || InputSlot.Empty || (RotationPlayer is not null)) return false;

            RotationPlayer = player;
            UpdateIsRaising();

            if (!CanMove())
            {
                RotationPlayer = null;
                return false;
            }

            MarkDirty();
            return true;
        }

        public void StopTurning()
        {
            RotationPlayer = null;
            MarkDirty();
        }

        public bool ContinueTurning(float secondsPassed)
        {
            UpdateIsRaising();
            var speed = GetCurrentTurnSpeed();
            if (speed < minTurnSpeed) return false;

            if (TryProgressMovement(secondsPassed, speed))
            {
                MarkDirty();
                return true;
            }
            return false;
        }

        private bool TryProgressMovement(float secondsPassed, float speedMod)
        {
            float motion = IsRaising ? -ModConfig.Instance.GroundWater.WinchRaiseSpeed : ModConfig.Instance.GroundWater.WinchLowerSpeed;
            motion = motion * secondsPassed * speedMod;

            if (motion < 0)
            {
                if (BucketDepth != minBucketDepth) MarkDirty();

                BucketDepth = Math.Max(minBucketDepth, BucketDepth + motion);
                return BucketDepth != minBucketDepth;
            }

            if (motion > 0)
            {
                var nextBucketDepth = BucketDepth + motion;

                for (int nextBlockpos = (int)BucketDepth + 1; nextBlockpos <= nextBucketDepth; nextBlockpos++)
                {
                    if (!TryMoveToBucketDepth(nextBlockpos)) return false;
                }

                return TryMoveToBucketDepth(nextBucketDepth);
            }

            return false;
        }

        private bool TryMoveToBucketDepth(float targetBucketDepth)
        {
            BlockPos checkPos = Pos.DownCopy((int)Math.Ceiling(targetBucketDepth));
            if (checkPos.Y < 0) return false;

            bool isLiquid;
            if (SpaceFreeOrLiquid(Api.World, checkPos, out isLiquid))
            {
                if (isLiquid)
                {
                    TryFillBucketAtPos(checkPos);
                }
                BucketDepth = targetBucketDepth;
                return true;
            }
            return false;
        }

        private bool CanMove()
        {
            if (InputSlot.Empty) return false;
            if (IsRaising) return BucketDepth > minTurnSpeed;

            BlockPos checkPos = Pos.DownCopy((int)BucketDepth + 1);
            if (checkPos.Y < 0) return false;

            return SpaceFreeOrLiquid(Api.World, checkPos, out _);
        }

        private void OnSlotModified(int slotid)
        {
            if (slotid == 0 && InputSlot.Empty)
            {
                BucketDepth = minBucketDepth;
            }

            MarkDirty();
        }

        public bool IsWinchItemAtTop() => BucketDepth < 1f || InputSlot.Empty;

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel) => false;

        public ItemSlot InputSlot => Inventory[0];

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(winchBaseMesh);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree is not null)
            {
                Inventory.FromTreeAttributes(invTree);
                if (Api is not null) Inventory.AfterBlocksLoaded(Api.World);
            }

            BucketDepth = tree.GetFloat("bucketDepth", minBucketDepth);
            RotationPlayer = Api?.World.PlayerByUid(tree.GetString("RotationPlayerId"));
            renderer?.ScheduleMeshUpdate();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("bucketDepth", BucketDepth);
            if (RotationPlayer is not null) tree.SetString("RotationPlayerId", RotationPlayer.PlayerUID);
            UpdateIsRaising();

            TreeAttribute invTree = new();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, -BucketDepth, 0.5));
            }
            base.OnBlockBroken(byPlayer);
        }

        public virtual string DialogTitle => Lang.Get("hydrateordiedrate:Winch");

        private static bool SolidAllows(Block solid)
        {
            return solid == null
                   || solid.Code == null
                   || solid.Code.Path == "air"
                   || solid.Replaceable >= 500;
        }

        private static bool SpaceFreeOrLiquid(IWorldAccessor world, BlockPos pos, out bool isLiquid)
        {
            var ba = world.BlockAccessor;
            Block solid = ba.GetBlock(pos, BlockLayersAccess.Solid);
            Block fluid = ba.GetBlock(pos, BlockLayersAccess.Fluid);

            isLiquid = fluid?.IsLiquid() == true;
            if (isLiquid) return true;

            return SolidAllows(solid);
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
            dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.waterType", foundSpring.GetWaterType()));

            dsc.Append("  ");
            dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.outputRate", foundSpring.GetCurrentOutputRate()));

            dsc.Append("  ");
            dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.retentionVolume", foundSpring.GetRetentionDepth() * 70));

            dsc.Append("  ");
            dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.totalShaftVolume", foundSpring.GetTotalManagedVolume()));
        }

        public override void Dispose()
        {
            base.Dispose();

            renderer?.Dispose();
            renderer = null;
        }
    }
}
