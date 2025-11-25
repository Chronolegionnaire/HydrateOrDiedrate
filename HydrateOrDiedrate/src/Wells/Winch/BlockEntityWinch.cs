using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Wells.WellWater;
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

namespace HydrateOrDiedrate.Wells.Winch
{
    public class BlockEntityWinch : BlockEntityOpenableContainer
    {
        public const float minTurnpeed = 0.00001f;
        public const float minBucketDepth = 0.5f;
        public const string WinchBaseMeshPath = "shapes/block/winch/base.json";
        private static readonly AssetLocation WaterFillSound = new AssetLocation("game", "sounds/effect/water-fill.ogg");
        
        private long? playerTurnTickListenerId;

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

        private const int InfoMaxSearchDepth = 256;

        public BlockEntityWinch()
        {
            Inventory = new WinchInventory(this, null, null);
            Inventory.SlotModified += OnSlotModified;
        }

        private static BlockEntityWellSpring FindWellSpringBelow(IBlockAccessor ba, BlockPos from, int maxDepth)
        {
            if (ba == null || from == null) return null;

            var cur = from.DownCopy();
            for (int d = 0; d < maxDepth && cur.Y >= 0; d++, cur.Down())
            {
                if (ba.GetBlockEntity(cur) is BlockEntityWellSpring spring)
                    return spring;
            }
            return null;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is not ICoreClientAPI capi)
            {
                RegisterGameTickListener(ChanceForSoundEffect, 5000);
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
            capi.Event.RegisterRenderer(renderer, EnumRenderStage.ShadowFar, "winch");
            capi.Event.RegisterRenderer(renderer, EnumRenderStage.ShadowNear,"winch");
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
            if (Api.Side == EnumAppSide.Server && MPTickListenerId is null)
                MPTickListenerId = RegisterGameTickListener(deltaTime => ContinueTurning(deltaTime), 100);
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

        private bool TryFillBucketAtPos(BlockPos pos)
        {
            if (InputSlot.Empty || InputSlot.Itemstack.Collectible is not BlockLiquidContainerBase container) return false;

            int remainingCapacity = (int)(container.CapacityLitres - container.GetCurrentLitres(InputSlot.Itemstack));
            if (remainingCapacity < 1) return false;

            var content = container.GetContent(InputSlot.Itemstack);

            var stack = ExtractStackAtPos(pos, remainingCapacity, content?.Collectible.Code);
            if (stack is null || stack.StackSize <= 0) return false;

            if (content is not null) stack.StackSize += content.StackSize;

            container.SetContent(InputSlot.Itemstack, stack);
            InputSlot.MarkDirty();
            MarkDirty();
            return true;
        }

        public ItemStack ExtractStackAtPos(BlockPos pos, int litersToExtract, AssetLocation filter = null)
        {
            Block block = Api.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block.Attributes is null) return null;

            var props = block.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            var stack = props?.WhenFilled?.Stack;
            if (stack is null || !stack.Resolve(Api.World, nameof(BlockEntityWinch))) return null;
            if (filter is not null && stack.Code != filter) return null;

            if (WellBlockUtils.IsOurWellwater(block))
            {
                var spring = WellBlockUtils.FindGoverningSpring(Api, block, pos);

                if (spring != null && litersToExtract > 0)
                {
                    int delta = spring.TryChangeVolume(-litersToExtract);
                    litersToExtract = -delta;
                }
                else
                {
                    litersToExtract = 0;
                }
            }

            var itemProps = stack.ResolvedItemstack.Collectible.Attributes?["waterTightContainerProps"]
                .AsObject<WaterTightContainableProps>();
            if (itemProps is null) return null;

            stack.ResolvedItemstack.StackSize = (int)Math.Round(itemProps.ItemsPerLitre * litersToExtract);

            return stack.ResolvedItemstack;
        }

        public static BlockPos FindNaturalSourceInLiquidChain(IBlockAccessor blockAccessor, BlockPos pos, HashSet<BlockPos> visited = null)
        {
            visited ??= [];
            if (visited.Contains(pos)) return null;
            visited.Add(pos);

            Block currentBlock = blockAccessor.GetBlock(pos, 2);
            if (currentBlock is not null && currentBlock.Variant["createdBy"] == "natural") return pos;

            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                BlockPos neighborPos = pos.AddCopy(facing);
                Block neighborBlock = blockAccessor.GetBlock(neighborPos, 2);

                if (neighborBlock is not null)
                {
                    if (neighborBlock.Code?.Domain != "hydrateordiedrate") continue;

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

            if (Api.Side == EnumAppSide.Server && playerTurnTickListenerId is null)
            {
                playerTurnTickListenerId = RegisterGameTickListener(PlayerTurnSanityCheck, 50);
            }

            MarkDirty();
            return true;
        }

        public void StopTurning()
        {
            RotationPlayer = null;

            if (playerTurnTickListenerId is not null)
            {
                UnregisterGameTickListener(playerTurnTickListenerId.Value);
                playerTurnTickListenerId = null;
            }

            MarkDirty();
        }
        
        private void PlayerTurnSanityCheck(float dt)
        {
            if (RotationMode != EWinchRotationMode.Player || RotationPlayer == null)
            {
                if (playerTurnTickListenerId is not null)
                {
                    UnregisterGameTickListener(playerTurnTickListenerId.Value);
                    playerTurnTickListenerId = null;
                }
                return;
            }

            var entity   = RotationPlayer.Entity;
            var controls = entity?.Controls;
            if (controls == null || !controls.RightMouseDown)
            {
                StopTurning();
                return;
            }
            var sel = RotationPlayer.CurrentBlockSelection;
            if (sel == null || !sel.Position.Equals(Pos) || sel.SelectionBoxIndex != 1)
            {
                StopTurning();
            }
        }
        // Hacky fix for infinite turning glitch


        public bool ContinueTurning(float secondsPassed)
        {
            if (RotationMode == EWinchRotationMode.Player)
            {
                var controls = RotationPlayer?.Entity?.Controls;
                if (controls == null || !controls.RightMouseDown)
                {
                    StopTurning();
                    return false;
                }
            }

            UpdateIsRaising();
            var speed = GetCurrentTurnSpeed();
            if (speed < minTurnpeed) return false;

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
            var ba = Api.World.BlockAccessor;
            var checkPos = Pos.DownCopy((int)Math.Ceiling(targetBucketDepth));

            if (checkPos.Y < 0) return false;

            if (WellBlockUtils.FluidIsLiquid(ba, checkPos))
            {
                if (Api.Side == EnumAppSide.Server && TryFillBucketAtPos(checkPos))
                {
                    Api.World.PlaySoundAt(
                        WaterFillSound,
                        Pos.X + 0.5,
                        (Pos.Y - Math.Ceiling(targetBucketDepth)) + 0.5,
                        Pos.Z + 0.5
                    );
                }

                BucketDepth = targetBucketDepth;
                return true;
            }

            if (WellBlockUtils.CellAllowsMove(ba, checkPos))
            {
                BucketDepth = targetBucketDepth;
                return true;
            }

            return false;
        }

        private bool CanMove()
        {
            if (InputSlot.Empty) return false;
            if (IsRaising) return BucketDepth > minBucketDepth;

            var ba = Api.World.BlockAccessor;

            var checkPos = new BlockPos(Pos.dimension);
            checkPos.Set(Pos.X, Pos.Y - ((int)BucketDepth + 1), Pos.Z);
            if (checkPos.Y < 0) return false;

            return WellBlockUtils.CellAllowsMove(ba, checkPos);
        }

        private void OnSlotModified(int slotid)
        {
            if (slotid == 0 && InputSlot.Empty) BucketDepth = minBucketDepth;
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

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetFloat("bucketDepth", BucketDepth);
            tree.SetString("RotationPlayerId", RotationPlayer?.PlayerUID ?? "");

            TreeAttribute invTree = new();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
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

            var playerId = tree.GetString("RotationPlayerId");
            if (string.IsNullOrEmpty(playerId))
            {
                RotationPlayer = null;
            }
            else
            {
                RotationPlayer = Api?.World.PlayerByUid(playerId);
            }

            renderer?.ScheduleMeshUpdate();
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server)
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, -BucketDepth, 0.5));
            base.OnBlockBroken(byPlayer);
        }

        public virtual string DialogTitle => Lang.Get("hydrateordiedrate:Winch");

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (!ModConfig.Instance.GroundWater.WinchOutputInfo) return;

            var foundSpring = FindWellSpringBelow(Api.World.BlockAccessor, Pos, InfoMaxSearchDepth);
            if (foundSpring is null)
            {
                dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.noSpring"));
                return;
            }

            dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.springDetected"));
            dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.waterType", string.IsNullOrEmpty(foundSpring.LastWaterType) ? string.Empty : Lang.Get($"hydrateordiedrate:item-waterportion-{foundSpring.LastWaterType}")));
            dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.outputRate", foundSpring.LastDailyLiters));
            dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.retentionVolume", foundSpring.GetMaxTotalVolume()));
            dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:winch.totalShaftVolume", foundSpring.totalLiters));
        }

        public override void Dispose()
        {
            base.Dispose();
            renderer?.Dispose();
            renderer = null;
        }
        private sealed class WinchInventory : InventorySingleTopOpenedContainer
        {
            private readonly BlockEntityWinch winch;

            public WinchInventory(BlockEntityWinch winch, string inventoryID, ICoreAPI api)
                : base(inventoryID, api, () => winch.IsWinchItemAtTop())
            {
                this.winch = winch;
            }

            public ItemSlot InputSlot => ContainerSlot;

            private static bool IsDisallowedWinchItem(ItemSlot slot)
            {
                var code = slot?.Itemstack?.Collectible?.Code;
                return code != null
                       && code.Domain == "game"
                       && code.Path.StartsWith("bowl-");
            }

            protected override ItemSlot NewSlot(int i) =>
                new WinchTopOpenedContainerSlot(this, () => winch.IsWinchItemAtTop());

            public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
            {
                if (IsDisallowedWinchItem(sourceSlot))
                    return false;

                return base.CanContain(sinkSlot, sourceSlot);
            }

            public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
            {
                if (targetSlot == InputSlot && IsDisallowedWinchItem(sourceSlot))
                    return 0f;

                if (targetSlot == InputSlot && sourceSlot.Itemstack?.Collectible is BlockBucket)
                {
                    if (sourceSlot.Itemstack.StackSize > 1) return 0f;
                    if (!sourceSlot.Itemstack.Attributes.HasAttribute("contents")) return 4f;
                }

                return base.GetSuitability(sourceSlot, targetSlot, isMerge);
            }
        }
        public class WinchTopOpenedContainerSlot : TopOpenedContainerSlot
        {
            public WinchTopOpenedContainerSlot(InventoryBase inventory, Func<bool> canTakePredicate = null)
                : base(inventory, canTakePredicate)
            {
            }

            public override bool CanHold(ItemSlot sourceSlot)
            {
                var code = sourceSlot?.Itemstack?.Collectible?.Code;
                if (code != null && code.Domain == "game" && code.Path.StartsWith("bowl-"))
                    return false;

                return base.CanHold(sourceSlot);
            }
        }
    }
}
