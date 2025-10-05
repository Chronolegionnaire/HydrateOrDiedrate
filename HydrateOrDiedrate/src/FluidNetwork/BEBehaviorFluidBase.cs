using System;
using System.Linq;
using HydrateorDiedrate.FluidNetwork;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.FluidNetwork
{
    public abstract class BEBehaviorFluidBase : BlockEntityBehavior, FluidInterfaces.IFluidDevice
    {
        protected FluidModSystem manager;
        protected HydrateOrDiedrate.FluidNetwork.FluidNetwork network;

        // Basic node state
        public float volume;
        public float capacity = 100f;   // override per block
        public float conductance = 1f;  // 0..1 per-face openness
        const float NegativeFloor = -1f;

        // IFluidNode
        public float Volume   => volume;
        public float Capacity => capacity;
        public float Pressure => capacity > 0 ? volume / capacity : 0f;

        public long NetworkId { get; protected set; }
        public HydrateOrDiedrate.FluidNetwork.FluidNetwork Network => network;

        public virtual BlockFacing DefaultOutFacing => BlockFacing.NORTH;
        protected const bool VerboseLogging = true; // flip to false to reduce noise

        protected void Log(string msg)
        {
            if (Api == null) return;
            Api.Logger.Notification($"[FluidNet] {Blockentity?.Pos} {msg}");
        }

        protected void VLog(string msg)
        {
            if (!VerboseLogging || Api == null) return;
            Api.Logger.Notification($"[FluidNet][V] {Blockentity?.Pos} {msg}");
        }
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

        void SafeDiscover()
        {
            try
            {
                CreateJoinAndDiscoverNetwork(DefaultOutFacing);
            }
            catch (Exception) { /* ignore to avoid spam; can log if desired */ }
        }

        public BlockPos GetPosition() => Blockentity.Pos;

        public virtual float GetConductance(BlockFacing towards) => conductance;

        public void AddFluid(float amount)
        {
            // Positive inflow; clamp to capacity
            if (amount <= 0f) return;
            volume = Math.Min(capacity, volume + amount);
            MarkDirty();
        }

        public void RemoveFluid(float amount)
        {
            // Outflow/demand; allow negative down to -1
            if (amount <= 0f) return;
            volume = Math.Max(NegativeFloor, volume - amount);
            MarkDirty();
        }

        // Unclamped overloads (for “negative pull” behavior)
        public void AddFluid(float amount, bool clamp)
        {
            if (clamp) volume = Math.Min(capacity, volume + amount);
            else       volume = volume + amount;
            MarkDirty();
        }

        public void RemoveFluid(float amount, bool clamp)
        {
            if (clamp) volume = Math.Max(0, volume - amount);
            else       volume = volume - amount;
            MarkDirty();
        }

        public virtual void OnAfterFlowStep(float damping)
        {
            // hook for leakage / smoothing if needed later
        }

        // ---- Networking ----
        public virtual void JoinNetwork(HydrateOrDiedrate.FluidNetwork.FluidNetwork nw)
        {
            if (nw == null) return;
            if (network != null && network != nw)
            {
                Log($"Switching net {network.networkId} -> {nw.networkId}");
                LeaveNetwork();
            }

            if (network == null)
            {
                network = nw;
                nw.Join(this);
                Log($"JOINED net {nw.networkId}");
                // Opportunistically register adjacent providers (e.g., wells)
                if (Api?.Side == EnumAppSide.Server)
                {
                    var ba = Api.World.BlockAccessor;
                    foreach (var f in BlockFacing.ALLFACES)
                    {
                        var nbe = ba.GetBlockEntity(GetPosition().AddCopy(f));
                        var prov = nbe as FluidInterfaces.IFluidExternalProvider
                                   ?? (nbe?.Behaviors?.FirstOrDefault(b => b is FluidInterfaces.IFluidExternalProvider)
                                       as FluidInterfaces.IFluidExternalProvider);
                        if (prov != null) nw.RegisterProvider(prov);
                    }
                }
            }

            NetworkId = nw.networkId;
            MarkDirty();
        }

        public virtual void LeaveNetwork()
        {
            if (network != null)
            {
                Log($"LEAVING net {network.networkId}");
                network.Leave(this);
                network = null;
            }
            NetworkId = 0;
            MarkDirty();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            var nw = network;
            LeaveNetwork();

            // Do not delete network here. Let remaining nodes keep it alive.
            nw?.mod?.TestFullyLoaded(nw);
            nw?.Rebuild(this);
        }

        // ---- Connectivity policy ----
        // We consider connected if neighbor block advertises a connector on our opposite face
        public virtual bool IsConnectedTowards(BlockFacing towards)
        {
            var npos = GetPosition().AddCopy(towards);
            var ba   = Api.World.BlockAccessor;

            // 1) If neighbor block advertises IFluidBlock, respect its face gate
            if (ba.GetBlock(npos) is FluidInterfaces.IFluidBlock ifb)
            {
                bool ok = ifb.HasFluidConnectorAt(Api.World, npos, towards.Opposite);
                VLog($"Probe {towards.Code} -> {npos}: IFluidBlock={ok}");
                return ok;
            }

            // 2) Otherwise, connect if neighbor BE has a fluid behavior at all
            bool hasBeh = ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>() != null;
            VLog($"Probe {towards.Code} -> {npos}: BEBehaviorFluidBase={hasBeh}");
            return hasBeh;
        }


        public virtual bool TryConnect(BlockFacing towards)
        {
            var npos = GetPosition().AddCopy(towards);
            var ba = Api.World.BlockAccessor;

            FluidInterfaces.IFluidBlock ifb = ba.GetBlock(npos) as FluidInterfaces.IFluidBlock;
            var nbeFluid = ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>();

            // If neither IFluidBlock nor fluid BE exists, nothing to do
            if (ifb == null && nbeFluid == null)
            {
                VLog($"TryConnect {towards.Code} -> {npos}: no fluid-capable neighbor");
                return false;
            }

            // If there is an IFluidBlock gate, honor its face rule
            if (ifb != null && !ifb.HasFluidConnectorAt(Api.World, npos, towards.Opposite))
            {
                VLog($"TryConnect {towards.Code} -> {npos}: neighbor face closed");
                return false;
            }

            // 1) Try to adopt neighbor’s network (from IFluidBlock first)
            FluidNetwork neighborNet = ifb?.GetNetwork(Api.World, npos) ?? nbeFluid?.Network;

            if (neighborNet != null)
            {
                if (network != null && !FluidNetwork.FluidsCompatible(network, neighborNet))
                {
                    Log($"TryConnect {towards.Code}: INCOMPATIBLE fluids with net {neighborNet.networkId}");
                    return false;
                }

                Log($"TryConnect {towards.Code}: adopting neighbor net {neighborNet.networkId}");
                JoinNetwork(neighborNet);

                Vec3i _;
                bool ok = JoinAndSpreadNetworkToNeighbours(Api, neighborNet, towards.Opposite, out _);
                VLog($"Spread after adopt: {ok}");
                return ok;
            }

            // 2) If neighbor has a BE but no net yet, invite it to connect back into ours (or force-create ours)
            if (network == null)
            {
                var nw = manager.CreateNetwork(this);
                JoinNetwork(nw);
                Log($"TryConnect {towards.Code}: created net {nw.networkId} (no neighbor net yet)");
            }

            if (nbeFluid != null)
            {
                VLog($"TryConnect {towards.Code}: asking neighbor BE to join our net {network.networkId}");
                // Call their TryConnect from the other side so they adopt our net
                return nbeFluid.TryConnect(towards.Opposite);
            }

            return false;
        }


        public FluidNetwork CreateJoinAndDiscoverNetwork(BlockFacing outward)
        {
            var ba = Api.World.BlockAccessor;

            // 1) Look for ANY neighbor with a net (IFluidBlock first, then BE)
            FluidNetwork neighborNet = null;
            foreach (var f in BlockFacing.ALLFACES)
            {
                var npos = GetPosition().AddCopy(f);

                // IFluidBlock path
                if (ba.GetBlock(npos) is FluidInterfaces.IFluidBlock ifb)
                {
                    var nnet = ifb.GetNetwork(Api.World, npos);
                    if (nnet != null) { neighborNet = nnet; break; }
                }

                // BE behavior path
                var nbeFluid = ba.GetBlockEntity(npos)?.GetBehavior<BEBehaviorFluidBase>();
                if (nbeFluid?.Network != null) { neighborNet = nbeFluid.Network; break; }
            }

            // 2) Target: neighbor’s net or ours or create new
            var target = neighborNet ?? network ?? manager.CreateNetwork(this);
            Log(neighborNet != null
                ? $"Adopting neighbor net {target.networkId}"
                : (network != null ? $"Reusing net {target.networkId}" : $"Created net {target.networkId}"));

            JoinNetwork(target);

            // 3) Spread to all faces; missing chunks mark not-fully-loaded
            foreach (var f in BlockFacing.ALLFACES)
            {
                if (!IsConnectedTowards(f)) { VLog($"No connection on face {f.Code}"); continue; }
                var exitPos = GetPosition().AddCopy(f);
                if (!SpreadTo(Api, target, exitPos, f, out var missing))
                {
                    target.fullyLoaded = false;
                    Log($"Spread {f.Code} -> {exitPos}: missing chunk {missing}");
                }
                else
                {
                    VLog($"Spread {f.Code} -> {exitPos}: ok");
                }
            }

            return target;
        }

protected bool SpreadTo(ICoreAPI api, FluidNetwork nw, BlockPos exitPos, BlockFacing face, out Vec3i missingChunkPos)
{
    var ba   = api.World.BlockAccessor;
    var be   = ba.GetBlockEntity(exitPos);
    var next = be?.GetBehavior<BEBehaviorFluidBase>();
    var nblk = ba.GetBlock(exitPos) as FluidInterfaces.IFluidBlock;

    if (next != null || ba.GetChunkAtBlockPos(exitPos) != null)
    {
        if (next != null)
        {
            bool okFace = (nblk?.HasFluidConnectorAt(api.World, exitPos, face.Opposite)) ?? true;
            if (!okFace) { VLog($"Spread blocked by face gate at {exitPos}"); missingChunkPos = null; return true; }

            var nextNet = next.Network;
            if (nextNet != null && !FluidNetwork.FluidsCompatible(nw, nextNet))
            {
                VLog($"Spread blocked: incompatible fluids at {exitPos} (net {nextNet.networkId})");
                missingChunkPos = null; return true;
            }

            VLog($"Spread join -> {exitPos}");
            next.Api = api;
            return next.JoinAndSpreadNetworkToNeighbours(api, nw, face.Opposite, out missingChunkPos);
        }
        VLog($"Spread noop: chunk loaded at {exitPos} but no fluid BE");
        missingChunkPos = null; return true;
    }

    if (!OutsideMap(ba, exitPos))
    {
        missingChunkPos = new Vec3i(exitPos.X / 32, exitPos.Y / 32, exitPos.Z / 32);
        return false;
    }

    VLog($"Spread ignored: outside map at {exitPos}");
    missingChunkPos = null;
    return true;
}


        public virtual bool JoinAndSpreadNetworkToNeighbours(
            ICoreAPI api,
            HydrateOrDiedrate.FluidNetwork.FluidNetwork nw,
            BlockFacing entryFrom,
            out Vec3i missingChunkPos)
        {
            missingChunkPos = null;

            if (nw == null) return true;

            // Reject incompatible merges
            if (network != null && network.networkId != 0 && !FluidNetwork.FluidsCompatible(network, nw))
                return true; // treat as boundary; don't error

            if (network?.networkId == nw.networkId) return true;

            JoinNetwork(nw);

            // Let our block know we connected at this face
            (Block as FluidInterfaces.IFluidBlock)?.DidConnectAt(api.World, GetPosition(), entryFrom);

            // Fan out to neighbors
            foreach (var f in BlockFacing.ALLFACES)
            {
                if (!IsConnectedTowards(f)) continue;

                var exitPos = GetPosition().AddCopy(f);
                if (!SpreadTo(api, nw, exitPos, f, out missingChunkPos))
                {
                    return false; // a chunk missing, caller will handle
                }
            }

            return true;
        }

        bool OutsideMap(IBlockAccessor ba, BlockPos p)
            => p.X < 0 || p.X >= ba.MapSizeX || p.Y < 0 || p.Y >= ba.MapSizeY || p.Z < 0 || p.Z >= ba.MapSizeZ;

        protected void MarkDirty() => Blockentity?.MarkDirty(false);

        // --------------- Item/Litre helpers (unchanged semantics) ---------------
        public bool AddFluidFromItem(ItemStack portion, float litresRequested, out float acceptedLitres)
        {
            acceptedLitres = 0f;
            if (portion == null) return false;

            EnsureNetwork();

            if (network.FluidCodeShort == null)
            {
                if (!network.TryAssignFluidFrom(portion, Api)) return false;
            }
            else
            {
                var code = portion.Collectible?.Code?.ToShortString();
                if (!network.FluidCodeShort.Equals(code, StringComparison.Ordinal)) return false;
            }

            float free = Capacity - Volume;
            if (free <= 0) return false;

            acceptedLitres = Math.Min(free, Math.Max(0, litresRequested));
            AddFluid(acceptedLitres);
            return acceptedLitres > 0;
        }

        public bool AddItemsAsFluid(ItemStack portion, int items, out int acceptedItems)
        {
            acceptedItems = 0;
            if (portion == null || items <= 0) return false;

            EnsureNetwork();

            if (network.FluidCodeShort == null)
            {
                if (!network.TryAssignFluidFrom(portion, Api)) return false;
            }
            else
            {
                var code = portion.Collectible?.Code?.ToShortString();
                if (!network.FluidCodeShort.Equals(code, StringComparison.Ordinal)) return false;
            }

            var props = network.FluidProps ?? BlockLiquidContainerBase.GetContainableProps(portion);
            if (props == null) return false;

            float litresPerItem = 1f / Math.Max(0.00001f, props.ItemsPerLitre);
            float freeLitres    = Capacity - Volume;
            if (freeLitres <= 0) return false;

            int canAccept = (int)Math.Floor(freeLitres / litresPerItem + 1e-6f);
            acceptedItems = Math.Min(items, canAccept);
            if (acceptedItems <= 0) return false;

            AddFluid(acceptedItems * litresPerItem);
            return true;
        }

        public float RemoveFluidToLitres(float litresRequested)
        {
            float give = Math.Min(Volume, Math.Max(0, litresRequested));
            RemoveFluid(give);
            network?.ClearFluidIfEmpty();
            return give;
        }

        public int RemoveFluidToItems(int maxItems)
        {
            if (network?.FluidProps == null || Volume <= 0 || maxItems <= 0) return 0;

            float litresPerItem = 1f / Math.Max(0.00001f, network.FluidProps.ItemsPerLitre);
            int   maxByVolume   = (int)Math.Floor(Volume / litresPerItem + 1e-6f);
            int   items         = Math.Min(maxItems, maxByVolume);

            float litres = items * litresPerItem;
            RemoveFluid(litres);
            network.ClearFluidIfEmpty();
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
    }
}
