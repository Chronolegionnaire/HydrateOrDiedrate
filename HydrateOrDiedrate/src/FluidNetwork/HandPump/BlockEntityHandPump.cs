using System;
using System.Linq;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.FluidNetwork.HandPump
{
    public class BlockEntityHandPump : BlockEntityOpenableContainer
    {
        // Tune these
        public const float PumpRateLitresPerSec = 1.25f;   // demand rate while held
        public const float IntakeLitresPerSec   = 8f;      // how fast we bottle arriving fluid from our node

        public IPlayer PumpingPlayer { get; private set; }

        public override string InventoryClassName => "handpump";
        public override InventoryBase Inventory { get; }

        public ItemSlot ContainerSlot => Inventory[0];

        // Shortcuts to embedded fluid behavior (attached via block JSON "entityBehaviors": [{ "name": "BEBehaviorPipe" }] or your fluid behavior name)
        public BEBehaviorFluidBase Fluid { get; private set; }

        public BlockEntityHandPump()
        {
            Inventory = new InventoryGeneric(1, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Fluid = GetBehavior<BEBehaviorFluidBase>();
            // Optional: smaller local capacity so this node doesn’t “store”, it just pulls and forwards
            if (Fluid != null)
            {
                Fluid.capacity     = 5f;
                Fluid.conductance  = 1f;
            }

            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(ServerTick, 200);
            }
        }

        public void TryAutoJoinNetwork()
        {
            if (Fluid == null) return;
            foreach (var f in BlockFacing.ALLFACES)
                Fluid.CreateJoinAndDiscoverNetwork(f);
        }


        // Start on selectionboxindex 1
        public bool TryStartPumping(IPlayer player)
        {
            if (PumpingPlayer != null) return false;
            if (ContainerSlot.Empty) return false;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerBase) return false;

            PumpingPlayer = player;
            MarkDirty();
            return true;
        }

        public void StopPumping()
        {
            PumpingPlayer = null;
            MarkDirty();
        }

        // Continuously called while right-click held
        public bool ContinuePumping(float dt)
        {
            if (PumpingPlayer == null || Fluid == null || ContainerSlot.Empty) return false;

            // 1) Create network demand by making our node go negative unclamped
            var draw = PumpRateLitresPerSec * dt;
            if (Fluid is BEBehaviorFluidBase be)
            {
                be.RemoveFluid(draw, clamp: false); // go negative
            }
            else
            {
                Fluid.RemoveFluid(draw); // fallback (clamped)
            }

            // 2) Bottle any fluid currently available in this node (what arrived this tick)
            TryFillContainerFromNode(IntakeLitresPerSec * dt);

            // stay in “use”
            return true;
        }

        // Pull up to 'maxLitres' from our node into the container
        private void TryFillContainerFromNode(float maxLitres)
        {
            if (ContainerSlot.Empty || Fluid == null) return;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerBase container) return;

            // Ensure network type consistency: only fill if network is assigned and the container either empty or same type
            var net = Fluid.Network;
            if (net == null || net.FluidCodeShort == null) return;

            var contContent = container.GetContent(ContainerSlot.Itemstack);
            if (contContent != null && contContent.Collectible?.Code?.ToShortString() != net.FluidCodeShort) return;

            if (maxLitres <= 0) return;

            // How much space is in the container?
            var curL = container.GetCurrentLitres(ContainerSlot.Itemstack);
            var free = Math.Max(0f, container.CapacityLitres - curL);
            if (free <= 0f) return;

            var toTake = Math.Min(free, maxLitres);

            // Remove *actual* litres from our node (clamped to what we have >=0 first)
            // If our node is still negative (no arrival yet), skip—next tick it will be refilled by providers/peers.
            if (Fluid.Volume <= 0f) return;

            float got = Fluid.RemoveFluidToLitres(Math.Min(toTake, Fluid.Volume));
            if (got <= 0f) return;

            // Convert litres to items sized by the network’s fluid props
            var itemsPerL = net.FluidProps?.ItemsPerLitre ?? 100f;
            int addItems = (int)Math.Floor(got * itemsPerL + 1e-6f);
            if (addItems <= 0) return;

            var toStack = EnsureContainerItemType(container, ContainerSlot.Itemstack, net, Api);
            if (toStack == null) return;

            toStack.StackSize += addItems;
            container.SetContent(ContainerSlot.Itemstack, toStack);
            ContainerSlot.MarkDirty();
            MarkDirty();
        }

        private ItemStack EnsureContainerItemType(BlockLiquidContainerBase container, ItemStack containerStack, FluidNetwork net, ICoreAPI api)
        {
            var existing = container.GetContent(containerStack);
            if (existing != null) return existing;

            // Seed the container content stack for this network’s fluid
            var item = api.World.GetItem(net.FluidCode);
            if (item == null) return null;

            var seed = new ItemStack(item, 0);
            return seed;
        }

        private void ServerTick(float dt)
        {
            // If player stopped holding, nothing to do here.
            // Could add ambient sounds or idle update if desired.
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);

            if (tree.HasAttribute("inventory"))
            {
                var inv = tree.GetTreeAttribute("inventory");
                Inventory.FromTreeAttributes(inv);
                if (Api != null) Inventory.AfterBlocksLoaded(Api.World);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            var invTree = new TreeAttribute();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Server && Inventory != null)
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            base.OnBlockRemoved();
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel) => false;
    }
}
