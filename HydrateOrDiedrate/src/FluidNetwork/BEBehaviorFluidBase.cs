using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent; // for BlockLiquidContainerBase.GetContainableProps

namespace HydrateOrDiedrate.FluidNetwork
{
    public abstract class BEBehaviorFluidBase : BlockEntityBehavior, FluidInterfaces.IFluidDevice
    {
        protected FluidModSystem manager;
        protected FluidNetwork network;

        // Basic node state
        public float volume;
        public float capacity = 100f;   // override per block
        public float conductance = 1f;  // 0..1 per-face openness

        // New: target/set-point in litres (0..capacity). While volume < demand -> negative pressure (pull).
        protected float demand = 0f;

        // IFluidNode
        public virtual float Volume   => volume;
        public virtual float Capacity => capacity;

        /// <summary>
        /// Pressure relative to desired fill level. Negative => below target (pull), positive => above target (push).
        /// Returned as an integer-valued float (litres) to stabilize directionality.
        /// </summary>
        public virtual float Pressure
        {
            get
            {
                var rawDeltaLitres = capacity > 0f ? (volume - demand) : 0f;
                int intLitres = (int)MathF.Round(rawDeltaLitres);
                return intLitres;
            }
        }

        /// <summary>
        /// How much this node can PROVIDE right now for the network step (in litres),
        /// respecting conservation. Default: you can only give what you physically hold.
        /// Providers (e.g., well-spring) should override to surface their external store.
        /// </summary>
        public virtual float ProvideCap => Math.Max(0f, Volume);

        /// <summary>
        /// How much this node can RECEIVE right now (in litres). Default: free local space.
        /// Consumers with special buffering can override if needed.
        /// </summary>
        public virtual float ReceiveCap => Math.Max(0f, Capacity - Volume);

        public long NetworkId { get; protected set; }
        public FluidNetwork Network => network;

        public virtual BlockFacing DefaultOutFacing => BlockFacing.NORTH;

        public BEBehaviorFluidBase(BlockEntity be) : base(be) {}

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            manager = api.ModLoader.GetModSystem<FluidModSystem>();

            // Discover only on server, and do it next tick to let neighbors appear first.
            if (api.Side == EnumAppSide.Server)
            {
                api.World.RegisterCallback(_ => SafeDiscover(), 0);
            }
        }
        
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("fluid-vol", volume);
            tree.SetFloat("fluid-cap", capacity);
            tree.SetFloat("fluid-cond", conductance);
            tree.SetFloat("fluid-demand", demand);
            tree.SetLong("fluid-netid", NetworkId);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            volume      = tree.GetFloat("fluid-vol", volume);
            capacity    = tree.GetFloat("fluid-cap", capacity);
            conductance = tree.GetFloat("fluid-cond", conductance);
            demand      = tree.GetFloat("fluid-demand", demand);
            // Clamp demand in case capacity changed via config or block swap
            demand = GameMath.Clamp(demand, 0f, Math.Max(0f, capacity));
            // Also clamp volume to new capacity and non-negative
            volume = GameMath.Clamp(volume, 0f, Math.Max(0f, capacity));
            NetworkId   = tree.GetLong("fluid-netid", NetworkId);
        }

        // Change this so we actually sync to clients when state changed
        protected void MarkDirty(bool send = true) => Blockentity?.MarkDirty(send);

        void SafeDiscover()
        {
            try
            {
                CreateJoinAndDiscoverNetwork(DefaultOutFacing);
            }
            catch (Exception)
            {
                // Swallow to avoid log spam; optional: add logging here
            }
        }

        public BlockPos GetPosition() => Blockentity.Pos;

        public virtual float GetConductance(BlockFacing towards) => conductance;

        public virtual void AddFluid(float amount)
        {
            if (amount <= 0f) return;
            // Mass is conserved: never let volume go negative or exceed capacity
            volume = Math.Min(capacity, Math.Max(0f, volume + amount));
            MarkDirty(true);
        }

        public virtual void RemoveFluid(float amount)
        {
            if (amount <= 0f) return;
            // Never sink below 0 now
            volume = Math.Max(0f, volume - amount);
            MarkDirty(true);
        }

        public virtual void OnAfterFlowStep(float damping)
        {
            // IMPORTANT: do not damp volume. That deleted mass and erased suction.
            // If you want smoothing, you can optionally relax demand:
            // demand *= damping;
            // if (demand < 1e-4f) demand = 0f;    // (only if your nodes set demand dynamically)
        }

        public virtual void JoinNetwork(FluidNetwork nw)
        {
            if (nw == null) return;
            if (network != null && network != nw) LeaveNetwork();
            if (network == null)
            {
                network = nw;
                nw.Join(this);
            }
            NetworkId = nw.networkId;
            MarkDirty(true);
        }

        public virtual void LeaveNetwork()
        {
            if (network != null)
            {
                network.Leave(this);
                network = null;
            }
            NetworkId = 0;
            MarkDirty(true);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            LeaveNetwork(); // lean network: no rebuild/fullyLoaded bookkeeping
        }

        // ---- Connectivity policy ----
        // Consider connected if neighbor block advertises a connector on our opposite face,
        // or if neighbor BE has a fluid behavior at all.
        public virtual bool IsConnectedTowards(BlockFacing towards)
        {
            var npos = GetPosition().AddCopy(towards);
            var ba   = Api.World.BlockAccessor;

            if (ba.GetBlock(npos) is FluidInterfaces.IFluidBlock ifb)
                return ifb.HasFluidConnectorAt(Api.World, npos, towards.Opposite);

            return ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>() != null;
        }

        public virtual bool TryConnect(BlockFacing towards)
        {
            var npos = GetPosition().AddCopy(towards);
            var ba   = Api.World.BlockAccessor;

            var ifb      = ba.GetBlock(npos) as FluidInterfaces.IFluidBlock;
            var nbeFluid = ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>();

            // No fluid neighbor
            if (ifb == null && nbeFluid == null) return false;

            // Gate by IFluidBlock face if present
            if (ifb != null && !ifb.HasFluidConnectorAt(Api.World, npos, towards.Opposite))
                return false;

            // 1) Adopt neighbor network if available
            var neighborNet = ifb?.GetNetwork(Api.World, npos) ?? nbeFluid?.Network;
            if (neighborNet != null)
            {
                if (network != null && !FluidNetwork.FluidsCompatible(network, neighborNet))
                    return false;

                JoinNetwork(neighborNet);
                Vec3i _;
                return JoinAndSpreadNetworkToNeighbours(Api, neighborNet, towards.Opposite, out _);
            }

            // 2) If neighbor has a BE but no net yet, create ours and invite back
            if (network == null)
            {
                var nw = manager.CreateNetwork(this);
                JoinNetwork(nw);
            }

            if (nbeFluid != null)
                return nbeFluid.TryConnect(towards.Opposite);

            return false;
        }

        public FluidNetwork CreateJoinAndDiscoverNetwork(BlockFacing outward)
        {
            var ba = Api.World.BlockAccessor;

            // Look for ANY neighbor with an existing network (IFluidBlock first, then BE)
            FluidNetwork neighborNet = null;

            foreach (var f in BlockFacing.ALLFACES)
            {
                var npos = GetPosition().AddCopy(f);

                if (ba.GetBlock(npos) is FluidInterfaces.IFluidBlock ifb)
                {
                    var nnet = ifb.GetNetwork(Api.World, npos);
                    if (nnet != null) { neighborNet = nnet; break; }
                }

                var nbeFluid = ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>();
                if (nbeFluid?.Network != null) { neighborNet = nbeFluid.Network; break; }
            }

            // Use neighbor’s net, ours, or create new
            var target = neighborNet ?? network ?? manager.CreateNetwork(this);
            JoinNetwork(target);

            // Fan out to all valid faces (lean: no chunk/fullyLoaded checks)
            foreach (var f in BlockFacing.ALLFACES)
            {
                if (!IsConnectedTowards(f)) continue;
                var exitPos = GetPosition().AddCopy(f);
                SpreadTo(Api, target, exitPos, f, out _);
            }

            return target;
        }

        protected bool SpreadTo(ICoreAPI api, FluidNetwork nw, BlockPos exitPos, BlockFacing face, out Vec3i _)
        {
            _ = null; // lean network: ignore missing chunk bookkeeping

            var ba   = api.World.BlockAccessor;
            var be   = ba.GetBlockEntity(exitPos);
            var next = be?.GetBehavior<BEBehaviorFluidBase>();
            var nblk = ba.GetBlock(exitPos) as FluidInterfaces.IFluidBlock;

            // If we have a neighbor behavior, try to join/spread into it (respecting IFluidBlock gating)
            if (next != null)
            {
                bool okFace = (nblk?.HasFluidConnectorAt(api.World, exitPos, face.Opposite)) ?? true;
                if (!okFace) return true;

                var nextNet = next.Network;
                if (nextNet != null && !FluidNetwork.FluidsCompatible(nw, nextNet))
                    return true;

                next.Api = api;
                return next.JoinAndSpreadNetworkToNeighbours(api, nw, face.Opposite, out _);
            }

            // If there’s no neighbor BE, nothing to do
            return true;
        }

        public virtual bool JoinAndSpreadNetworkToNeighbours(
            ICoreAPI api,
            FluidNetwork nw,
            BlockFacing entryFrom,
            out Vec3i missingChunkPos)
        {
            missingChunkPos = null;
            if (nw == null) return true;

            // Reject incompatible merges (always true in lean network, but call remains)
            if (network != null && network.networkId != 0 && !FluidNetwork.FluidsCompatible(network, nw))
                return true;

            if (network?.networkId == nw.networkId) return true;

            JoinNetwork(nw);

            // Let the block know we connected on this face
            (Block as FluidInterfaces.IFluidBlock)?.DidConnectAt(api.World, GetPosition(), entryFrom);

            // Fan out to neighbors
            foreach (var f in BlockFacing.ALLFACES)
            {
                if (!IsConnectedTowards(f)) continue;
                var exitPos = GetPosition().AddCopy(f);
                if (!SpreadTo(api, nw, exitPos, f, out missingChunkPos))
                {
                    // lean network: we never report missing chunks, just continue
                    continue;
                }
            }

            return true;
        }

        bool OutsideMap(IBlockAccessor ba, BlockPos p)
            => p.X < 0 || p.X >= ba.MapSizeX || p.Y < 0 || p.Y >= ba.MapSizeY || p.Z < 0 || p.Z >= ba.MapSizeZ;

        // --------------- Item/Litre helpers (lean, no network typing) ---------------
        public bool AddFluidFromItem(ItemStack portion, float litresRequested, out float acceptedLitres)
        {
            acceptedLitres = 0f;
            if (portion == null) return false;

            EnsureNetwork();

            float free = Capacity - Volume;
            if (free <= 0) return false;

            acceptedLitres = Math.Min(free, Math.Max(0, litresRequested));
            if (acceptedLitres <= 0f) return false;

            AddFluid(acceptedLitres);
            return true;
        }

        public bool AddItemsAsFluid(ItemStack portion, int items, out int acceptedItems)
        {
            acceptedItems = 0;
            if (portion == null || items <= 0) return false;

            EnsureNetwork();

            // Use item props if available; otherwise default 1 item = 1 litre
            var props = BlockLiquidContainerBase.GetContainableProps(portion);
            float litresPerItem = (props != null && props.ItemsPerLitre > 0f)
                ? 1f / props.ItemsPerLitre
                : 1f;

            float freeLitres = Capacity - Volume;
            if (freeLitres <= 0f) return false;

            int canAccept = (int)Math.Floor(freeLitres / Math.Max(1e-6f, litresPerItem) + 1e-6f);
            acceptedItems = Math.Min(items, canAccept);
            if (acceptedItems <= 0) return false;

            AddFluid(acceptedItems * litresPerItem);
            return true;
        }

        public float RemoveFluidToLitres(float litresRequested)
        {
            float give = Math.Min(Volume, Math.Max(0, litresRequested));
            RemoveFluid(give);
            return give;
        }

        public int RemoveFluidToItems(int maxItems)
        {
            if (Volume <= 0 || maxItems <= 0) return 0;

            // Without network typing, assume 1 item = 1 litre here.
            // If you need item-aware conversion, perform it at the caller using a known portion type.
            float litresPerItem = 1f;

            int maxByVolume = (int)Math.Floor(Volume / Math.Max(1e-6f, litresPerItem) + 1e-6f);
            int items       = Math.Min(maxItems, maxByVolume);

            float litres = items * litresPerItem;
            RemoveFluid(litres);
            return items;
        }

        protected void EnsureNetwork()
        {
            if (network != null) return;

            // Try adopting any neighbor first
            foreach (var f in BlockFacing.ALLFACES)
            {
                var npos = GetPosition().AddCopy(f);
                var nblk = Api.World.BlockAccessor.GetBlock(npos) as FluidInterfaces.IFluidBlock;
                var nnet = nblk?.GetNetwork(Api.World, npos);
                if (nnet != null) { JoinNetwork(nnet); return; }
            }

            // Otherwise create new
            var nw = manager.CreateNetwork(this);
            JoinNetwork(nw);
        }

        /// <summary>
        /// Optional helper for pumps/consumers to tug one hop immediately.
        /// Uses neighbors' ProvideCap (so providers like wells can give without local buffer).
        /// </summary>
        public void TryImmediatePullFromNeighbors(float maxLitres)
        {
            if (Network == null || maxLitres <= 0f) return;

            // Need is how far we are below target (if at/above target, do nothing).
            float shortfall = Math.Max(0f, demand - volume);
            if (shortfall <= 0f) return;

            float need = Math.Min(shortfall, maxLitres);

            var myPos = GetPosition();
            var ba    = Api.World.BlockAccessor;

            foreach (var face in BlockFacing.ALLFACES)
            {
                if (need <= 0f) break;
                if (!IsConnectedTowards(face)) continue;

                var npos = myPos.AddCopy(face);
                var nbe  = ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>();
                if (nbe == null || !nbe.IsConnectedTowards(face.Opposite)) continue;

                var key = new PosKey(npos);
                if (!Network.nodes.TryGetValue(key, out var node)) continue;
                if (ReferenceEquals(node, this)) continue;

                // Respect neighbor's declared provide capacity (pipes: actual volume; wells: available store).
                float giveCap = (node as BEBehaviorFluidBase)?.ProvideCap ?? Math.Max(0f, node.Volume);
                if (giveCap <= 0f) continue;

                float give = Math.Min(giveCap, need);
                (node as BEBehaviorFluidBase)?.RemoveFluid(give);
                AddFluid(give);
                need -= give;
            }
        }

        // ---- Convenience for pumps/consumers to set target fill (litres) ----
        public virtual void SetDemand(float targetLitres)
        {
            demand = GameMath.Clamp(targetLitres, 0f, Math.Max(0f, capacity));
            MarkDirty(true);
        }

        public virtual float GetDemand() => demand;
    }
}
