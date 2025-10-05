// HydrateOrDiedrate.FluidNetwork
using System;
using System.Collections.Generic;
using HydrateorDiedrate.FluidNetwork;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent; // AssetLocation tools

namespace HydrateOrDiedrate.FluidNetwork
{
    [ProtoContract]
    public class FluidNetwork
    {
        [ProtoMember(1)] public long networkId;

        // ---- Fluid typing (derived from actual liquid item) ----
        // Store as AssetLocation for stable identity; props cached for conversions.
        [ProtoMember(2)] public string FluidCodeShort; // serialized; use .ToShortString()
        public AssetLocation FluidCode => FluidCodeShort == null ? null : new AssetLocation(FluidCodeShort);
        public WaterTightContainableProps FluidProps { get; private set; }  // not serialized; refresh on load

        // Totals (unchanged idea)
        [ProtoMember(3)] public float TotalFluid { get; private set; }
        [ProtoMember(4)] public float AveragePressure { get; private set; }

        public Dictionary<BlockPos, FluidInterfaces.IFluidNode> nodes = new Dictionary<BlockPos, FluidInterfaces.IFluidNode>();
        [ProtoMember(5)] public Dictionary<Vec3i, int> inChunks = new Dictionary<Vec3i, int>();

        internal FluidModSystem mod;
        public bool fullyLoaded;
        public bool clientSeen;

        const float BaseConductance = 3.0f;   // was 0.12f  (tune: 1–10 is reasonable)
        const float Damping         = 0.995f;
        const float MaxPerTickFlow  = 1000f;
        public readonly HashSet<FluidInterfaces.IFluidExternalProvider> providers
            = new HashSet<FluidInterfaces.IFluidExternalProvider>();
        public FluidNetwork() { }
        public FluidNetwork(FluidModSystem mod, long id) { networkId = id; this.mod = mod; }

        // ------------------ Fluid Type API ------------------

        /// <summary>Try assign the network’s fluid type from an incoming liquid ItemStack.</summary>
        public bool TryAssignFluidFrom(ItemStack liquidStack, ICoreAPI api)
        {
            if (liquidStack == null) return false;
            var props = BlockLiquidContainerBase.GetContainableProps(liquidStack);
            if (props == null) return false;

            var code = liquidStack.Collectible?.Code?.ToShortString();
            if (code == null) return false;

            // If already assigned, they must match
            if (FluidCodeShort != null && !FluidCodeShort.Equals(code, StringComparison.Ordinal))
                return false;

            FluidProps = props;
            FluidCodeShort = code;  // adopt
            return true;
        }

        /// <summary>Clear fluid assignment if empty (so the net can accept a new liquid later).</summary>
        public void ClearFluidIfEmpty()
        {
            // Count only *positive* fluid across nodes; ignore small negatives from demand
            float positive = 0f;
            foreach (var n in nodes.Values) if (n.Volume > 0f) positive += n.Volume;

            if (positive <= 0.00001f)
            {
                TotalFluid = 0f;
                AveragePressure = 0f;
                FluidProps = null;
                FluidCodeShort = null;
            }
        }
        public void RegisterProvider(FluidInterfaces.IFluidExternalProvider p)
        {
            if (p != null) providers.Add(p);
        }
        public void UnregisterProvider(FluidInterfaces.IFluidExternalProvider p)
        {
            if (p != null) providers.Remove(p);
        }
        /// <summary>Helper: litres -> items based on assigned fluid props.</summary>
        public int LitresToItems(float litres)
        {
            var itemsPerLitre = FluidProps?.ItemsPerLitre ?? 1f;
            return (int)Math.Floor(litres * itemsPerLitre + 1e-6f);
        }

        /// <summary>Helper: items -> litres based on assigned fluid props.</summary>
        public float ItemsToLitres(int items)
        {
            var itemsPerLitre = FluidProps?.ItemsPerLitre ?? 1f;
            return items / Math.Max(0.00001f, itemsPerLitre);
        }

        // ------------------ Discovery & Merge rules ------------------

        /// <summary>Return whether two networks are compatible (fluid-type wise).</summary>
        public static bool FluidsCompatible(FluidNetwork a, FluidNetwork b)
        {
            if (a == null || b == null) return true;
            if (a.FluidCodeShort == null || b.FluidCodeShort == null) return true; // either unassigned -> OK
            return a.FluidCodeShort.Equals(b.FluidCodeShort, StringComparison.Ordinal);
        }

        // ------------------ Usual network bookkeeping (trimmed) ------------------

        public void Join(FluidInterfaces.IFluidNode node)
        {
            var pos = node.GetPosition();
            nodes[pos] = node;
            var cpos = new Vec3i(pos.X / 32, pos.Y / 32, pos.Z / 32);
            inChunks.TryGetValue(cpos, out int q);
            inChunks[cpos] = q + 1;

            mod?.Api?.Logger?.Notification($"[FluidNet] JOIN node {pos} -> net {networkId} (nodes={nodes.Count})");
        }

        public void Leave(FluidInterfaces.IFluidNode node)
        {
            var pos = node.GetPosition();
            nodes.Remove(pos);
            var cpos = new Vec3i(pos.X / 32, pos.Y / 32, pos.Z / 32);
            inChunks.TryGetValue(cpos, out int q);
            if (q <= 1) inChunks.Remove(cpos); else inChunks[cpos] = q - 1;

            mod?.Api?.Logger?.Notification($"[FluidNet] LEAVE node {pos} <- net {networkId} (nodes={nodes.Count})");
        }


        public void DidUnload(FluidInterfaces.IFluidDevice node) => fullyLoaded = false;

        public bool TestFullyLoaded(ICoreAPI api)
        {
            foreach (var cp in inChunks.Keys)
                if (api.World.BlockAccessor.GetChunk(cp.X, cp.Y, cp.Z) == null) return false;
            return true;
        }

        public void ClientTick(float dt) { }

        public void ServerTick(float dt, long tickNumber)
        {
            StepFlows(dt);
            if (tickNumber % 40 == 0) Broadcast();
        }

        void Broadcast()
        {
            mod.Broadcast(new FluidNetworkPacket
            {
                networkId = networkId,
                totalFluid = TotalFluid,
                averagePressure = AveragePressure
            });
        }

        public void UpdateFromPacket(FluidNetworkPacket p, bool isNew)
        {
            TotalFluid = p.totalFluid;
            AveragePressure = p.averagePressure;
        }

        public void SendBlocksUpdateToClient(IServerPlayer player)
        {
            foreach (var node in nodes.Values)
                (node as BEBehaviorFluidBase)?.Blockentity?.MarkDirty(false);
        }

        public void Rebuild(FluidInterfaces.IFluidDevice nowRemoved = null)
        {
            var nnodes = new List<FluidInterfaces.IFluidNode>(nodes.Values);
            foreach (var n in nnodes) n.LeaveNetwork();
            foreach (var n in nnodes)
                if (n is FluidInterfaces.IFluidDevice dev)
                    dev.CreateJoinAndDiscoverNetwork(dev.DefaultOutFacing ?? BlockFacing.NORTH);
        }

        // ------------------ Flow core (pressure still drives it) ------------------

        void StepFlows(float dt)
        {
            // Aggregate
            float total = 0f, pSum = 0f;
            foreach (var n in nodes.Values) { total += n.Volume; pSum += n.Pressure; }
            TotalFluid = total;
            AveragePressure = nodes.Count > 0 ? pSum / nodes.Count : 0f;

            // If no assigned fluid yet, allow equalization (pure structural), but it won’t matter until someone adds fluid.
            // If you want to forbid any flow until assigned, uncomment:
            // if (FluidCodeShort == null) return;

            var intents = new Dictionary<(BlockPos from, BlockPos to), float>();

            foreach (var kv in nodes)
            {
                var from = kv.Value;
                var fromPos = kv.Key;

                foreach (var face in BlockFacing.ALLFACES)
                {
                    var toPos = fromPos.AddCopy(face);
                    if (!nodes.TryGetValue(toPos, out var to)) continue;

                    // ---- Type gate: only flow if both nodes are compatible for this network ----
                    // With network-level typing, all members share the same type.
                    // If you choose to add per-node typing later, guard here (from.FluidType == to.FluidType || to.Empty).
                    float dp = from.Pressure - to.Pressure;
                    if (dp <= 0) continue;

// Capacity-scaled conductance: turn pressure delta into litres
                    float capScale = 0.5f * (from.Capacity + to.Capacity); // litres
                    float gFace    = 0.5f * (from.GetConductance(face) + to.GetConductance(face.Opposite)); // 0..1
                    float desired  = BaseConductance * gFace * dp * capScale * dt;  // L this tick

                    if (desired <= 0) continue;

// Normal guards
                    float freeCap = Math.Max(0, to.Capacity - to.Volume);
                    if (freeCap <= 0) continue;

                    desired = Math.Min(desired, freeCap);
                    desired = Math.Min(desired, MaxPerTickFlow);

                    if (desired > 0) intents[(fromPos, toPos)] = desired;
                }
            }

            foreach (var it in intents)
            {
                var from = nodes[it.Key.from];
                var to   = nodes[it.Key.to];
                float m  = it.Value;

                // IMPORTANT: these must be the negative-friendly versions
                from.RemoveFluid(m);  // may push ‘from’ down toward -1
                to.AddFluid(m);       // ‘to’ clamps to capacity
            }


            SatisfyDeficitsFromProviders(mod.Api);

            foreach (var n in nodes.Values) n.OnAfterFlowStep(Damping);

            ClearFluidIfEmpty();
        }

        private void SatisfyDeficitsFromProviders(ICoreAPI api)
        {
            if (providers.Count == 0) return;

            // Collect all deficits (negative volumes)
            var deficitList = new List<FluidInterfaces.IFluidNode>();
            float totalDeficit = 0f;
            foreach (var n in nodes.Values)
            {
                if (n.Volume < 0f)
                {
                    deficitList.Add(n);
                    totalDeficit += -n.Volume;
                }
            }

            if (totalDeficit <= 0f) return;

            // First, make sure the network is typed/retagged compatibly with the providers (wells handle this logic).
            // We'll try providers one by one until deficits are covered.
            float remaining = totalDeficit;

            foreach (var p in providers)
            {
                if (remaining <= 0f) break;

                // Let the provider declare its item and available litres without changing anything
                if (!p.CanProvide(out var item, out var avail) || avail <= 0f) continue;

                // Ensure/retag network type (wells allow retag if both are well-water; other providers can use TryAssignFluidFrom)
                if (!IsAssigned)
                {
                    if (!TryAssignFluidFrom(item, api)) continue;
                }
                else
                {
                    // If already assigned, let provider attempt retag if it’s a well-water provider;
                    // else, if different and incompatible, skip it
                    TryRetagToWell(item, api);
                    if (!FluidsCompatible(this, this))
                    {
                        /* keep existing; noop */
                    }
                }

                float ask = Math.Min(avail, remaining);
                float got = p.ProvideToNetwork(this, ask, api);
                if (got <= 0f) continue;

                // Distribute 'got' litres across deficits (simple proportional or greedy)
                float shareRemaining = got;

                // Greedy: fill the most negative first
                deficitList.Sort((a, b) => a.Volume.CompareTo(b.Volume)); // more negative first
                foreach (var node in deficitList)
                {
                    if (shareRemaining <= 0f) break;
                    if (node.Volume >= 0f) continue;

                    float need = -node.Volume;
                    float give = Math.Min(need, shareRemaining);

                    // IMPORTANT: add without clamping so we exactly cancel the negative
                    (node as BEBehaviorFluidBase)?.AddFluid(give);

                    shareRemaining -= give;
                }

                remaining -= got;
            }

            // If some deficits remain, they stay negative → will keep pulling the rest of the network next tick.
        }

        public bool IsAssigned => FluidCodeShort != null;

        /// <summary>Check if a short code is one of our well-water items, e.g. "hydrateordiedrate:waterportion-fresh-well-clean".</summary>
        public static bool IsWellWaterCode(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode)) return false;
            // very safe check: "domain:waterportion-<type>-well-<pollution>"
            // we only require "-well-" to be present to treat as well water.
            // (Your json guarantees these exact shapes.)
            return shortCode.Contains("waterportion-") && shortCode.Contains("-well-");
        }

        /// <summary>Try to retag the entire network to a new well-water item (no volume change).</summary>
        public bool TryRetagToWell(AssetLocation newCode, ICoreAPI api)
        {
            if (newCode == null) return false;
            var newShort = newCode.ToShortString();
            if (!IsWellWaterCode(newShort)) return false;

            // Only allow retagging if the current network also carries a well-water item
            if (!IsAssigned || IsWellWaterCode(FluidCodeShort))
            {
                var stack = new ItemStack(api.World.GetItem(newCode));
                var newProps = BlockLiquidContainerBase.GetContainableProps(stack);
                if (newProps == null) return false;

                FluidCodeShort = newShort;
                FluidProps     = newProps;
                return true;
            }

            return false;
        }

        /// <summary>Try to retag from a well-water itemstack.</summary>
        public bool TryRetagToWell(ItemStack wellLiquidStack, ICoreAPI api)
        {
            if (wellLiquidStack?.Collectible?.Code == null) return false;
            return TryRetagToWell(wellLiquidStack.Collectible.Code, api);
        }
    }
}
