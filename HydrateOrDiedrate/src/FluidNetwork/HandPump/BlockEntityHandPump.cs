// HydrateOrDiedrate.FluidNetwork.HandPump/BlockEntityHandPump.cs
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
        // Rates
        public const float PumpRateLitresPerSec = 1.25f; // draw rate from the node (creates deficit)
        public const float IntakeLitresPerSec   = 8f;    // bottling rate into the container
        private float bottleCarryLitres = 0f;
        public IPlayer PumpingPlayer { get; private set; }

        public override string       InventoryClassName => "handpump";
        public override InventoryBase Inventory         { get; }

        public ItemSlot ContainerSlot => Inventory[0];

        // Our embedded fluid behavior
        public BEBehaviorFluidBase Fluid { get; private set; }

        public BlockEntityHandPump()
        {
            Inventory = new InventoryGeneric(1, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            // Attach the hand-pump fluid behavior
            Fluid = GetBehavior<BEBehaviorHandPump>();
            if (Fluid != null)
            {
                Fluid.capacity    = 5f;
                Fluid.conductance = 1f;
            }

            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(ServerTick, 200);
            }
        }

        public void TryAutoJoinNetwork()
        {
            if (Fluid == null) return;
            // Only connect downward
            Fluid.CreateJoinAndDiscoverNetwork(BlockFacing.DOWN);
        }

        public bool TryStartPumping(IPlayer player)
        {
            if (PumpingPlayer != null) return false;
            if (ContainerSlot.Empty)   return false;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerBase) return false;

            PumpingPlayer = player;
            Fluid?.SetDemand(Fluid.capacity);   // start pulling immediately
            MarkDirty();
            return true;
        }

        public void StopPumping()
        {
            PumpingPlayer = null;
            Fluid?.SetDemand(0f);               // stop pulling
            MarkDirty();
        }

        public bool ContinuePumping(float dt)
        {
            if (PumpingPlayer == null || Fluid == null || ContainerSlot.Empty) return false;

            // Pull toward a full local buffer while pumping
            Fluid.SetDemand(Fluid.capacity);

            // Give the network a little tug so the pump fills promptly
            var burstLitres = Math.Max(0.1f, PumpRateLitresPerSec * dt);
            Fluid.TryImmediatePullFromNeighbors(burstLitres);

            // Bottle what actually arrived
            TryFillContainerFromNode(IntakeLitresPerSec * dt);

            return true;
        }

        // HydrateOrDiedrate.FluidNetwork.HandPump/BlockEntityHandPump.cs
        private void TryFillContainerFromNode(float maxLitres)
        {
            if (maxLitres <= 0f) return;
            if (ContainerSlot.Empty || Fluid == null) return;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerBase container) return;

            var contentStack = container.GetContent(ContainerSlot.Itemstack);

            // If empty, seed with the network's fluid code so type is known
            if (contentStack == null)
            {
                var netCode = Fluid?.Network?.FluidCode;
                if (string.IsNullOrWhiteSpace(netCode)) return; // require priming if net is untyped

                var liq = Api.World.GetItem(new AssetLocation(netCode)) as CollectibleObject
                          ?? Api.World.GetBlock(new AssetLocation(netCode)) as CollectibleObject;
                if (liq == null) return;

                var seed = new ItemStack(liq, 0);
                if (BlockLiquidContainerBase.GetContainableProps(seed) == null) return;

                container.SetContent(ContainerSlot.Itemstack, seed);
                contentStack = seed;
            }

            // Free space (L) in the target stack
            var currentLitres = container.GetCurrentLitres(ContainerSlot.Itemstack);
            var freeLitres = Math.Max(0f, container.CapacityLitres - currentLitres);
            if (freeLitres <= 0f) return;

            // Conversion for THIS liquid
            var props = BlockLiquidContainerBase.GetContainableProps(contentStack);
            float itemsPerLitre = (props != null && props.ItemsPerLitre > 0f) ? props.ItemsPerLitre : 100f;

            // Peek available, cap by our intake rate
            float availLitres = Math.Min(maxLitres, Math.Max(0f, Fluid.Volume));
            float totalLitres = bottleCarryLitres + availLitres;

            // How many items could we mint with what we have *right now*?
            int potentialItems = (int)Math.Floor(totalLitres * itemsPerLitre + 1e-6f);
            if (potentialItems <= 0) return;

            // Don’t overfill the container (convert free L -> free items)
            int freeItemsCapacity = (int)Math.Floor(freeLitres * itemsPerLitre + 1e-6f);
            if (freeItemsCapacity <= 0) return;

            int itemsToAdd = Math.Min(potentialItems, freeItemsCapacity);
            if (itemsToAdd <= 0) return;

            // EXACT litres required for those items
            float litresNeeded = itemsToAdd / itemsPerLitre;

            // ---- NEW: actively pull the deficit from the network before removing ----
            float missing = Math.Max(0f, litresNeeded - Fluid.Volume);
            if (missing > 0f)
            {
                // keep suction on; we only have a 5L buffer so this will be clamped internally anyway
                Fluid.SetDemand(Fluid.capacity);
                Fluid.TryImmediatePullFromNeighbors(missing);
            }
            // -------------------------------------------------------------------------

            // Remove exactly what we will bottle (debited from node/network)
            float gotLitres = Fluid.RemoveFluidToLitres(litresNeeded);
            if (gotLitres <= 0f) return;

            // Recompute items from actual litres removed (safety against rounding)
            itemsToAdd = (int)Math.Floor((bottleCarryLitres + gotLitres) * itemsPerLitre + 1e-6f);
            if (itemsToAdd <= 0)
            {
                bottleCarryLitres += gotLitres;
                return;
            }

            float litresConsumed = itemsToAdd / itemsPerLitre;
            bottleCarryLitres = bottleCarryLitres + gotLitres - litresConsumed;
            if (bottleCarryLitres < 1e-6f) bottleCarryLitres = 0f;

            contentStack.StackSize += itemsToAdd;
            container.SetContent(ContainerSlot.Itemstack, contentStack);

            ContainerSlot.MarkDirty();
            MarkDirty();
        }

        private void ServerTick(float dt)
        {
            // Idle hook (sound, particles, etc.) — currently unused.
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

            bottleCarryLitres = tree.GetFloat("bottle-carry-l", bottleCarryLitres);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            var invTree = new TreeAttribute();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetFloat("bottle-carry-l", bottleCarryLitres);
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
