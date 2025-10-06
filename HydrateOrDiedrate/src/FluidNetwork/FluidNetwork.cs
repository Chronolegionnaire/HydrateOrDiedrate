// HydrateOrDiedrate/FluidNetwork/FluidNetwork.cs
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.FluidNetwork
{
    // ----------------------------- Interfaces -----------------------------
    public class FluidInterfaces
    {
        public interface IFluidBlock
        {
            void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
            bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
            FluidNetwork GetNetwork(IWorldAccessor world, BlockPos pos);
        }

        public interface IFluidNode
        {
            BlockPos GetPosition();
            void JoinNetwork(FluidNetwork nw);
            void LeaveNetwork();

            float Volume   { get; } // current fluid
            float Capacity { get; } // max fluid
            float Pressure { get; } // typically Volume/Capacity

            float GetConductance(BlockFacing towards); // per-face openness
            void AddFluid(float amount);
            void RemoveFluid(float amount);
            void OnAfterFlowStep(float damping);
        }

        public interface IFluidDevice : IFluidNode
        {
            FluidNetwork Network { get; }
            long NetworkId { get; }
            BlockFacing DefaultOutFacing { get; }
            FluidNetwork CreateJoinAndDiscoverNetwork(BlockFacing outward);
            bool JoinAndSpreadNetworkToNeighbours(ICoreAPI api, FluidNetwork nw, BlockFacing entryFrom, out Vec3i missingChunkPos);
            bool TryConnect(BlockFacing towards);
            bool IsConnectedTowards(BlockFacing towards);
        }
    }

    // ----------------------------- Value Key -----------------------------
    /// <summary>Compact, immutable key for node positions (value semantics for dictionary lookups).</summary>
    public readonly struct PosKey : IEquatable<PosKey>
    {
        public readonly int X, Y, Z;
        public PosKey(int x, int y, int z) { X = x; Y = y; Z = z; }
        public PosKey(BlockPos p) : this(p.X, p.Y, p.Z) { }
        public static PosKey From(BlockPos p) => new PosKey(p);
        public PosKey Add(BlockFacing f) => new PosKey(X + f.Normali.X, Y + f.Normali.Y, Z + f.Normali.Z);

        public bool Equals(PosKey o) => X == o.X && Y == o.Y && Z == o.Z;
        public override bool Equals(object o) => o is PosKey pk && Equals(pk);
        public override int GetHashCode() => (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
        public BlockPos ToBlockPos() => new BlockPos(X, Y, Z);
        public override string ToString() => $"({X},{Y},{Z})";
    }

    // ----------------------------- Network -----------------------------
    public class FluidNetwork
    {
        public long networkId;

        // Nodes keyed by immutable value (no ref-identity pitfalls)
        public readonly Dictionary<PosKey, FluidInterfaces.IFluidNode> nodes =
            new Dictionary<PosKey, FluidInterfaces.IFluidNode>();

        internal FluidModSystem mod;

        // Simple tuning
        const float BaseConductance = 3f;    // 1–10 reasonable
        const float Damping         = 0.995f;
        const float MaxPerTickFlow  = 1000f;

        public FluidNetwork(FluidModSystem mod, long id)
        {
            this.mod = mod;
            networkId = id;
        }

        // ---------- membership ----------
        public void Join(FluidInterfaces.IFluidNode node)
        {
            var be = node as BEBehaviorFluidBase;
            if (be == null) return;

            var key = PosKey.From(be.GetPosition());
            nodes[key] = node;
        }

        public void Leave(FluidInterfaces.IFluidNode node)
        {
            var be = node as BEBehaviorFluidBase;
            if (be == null) return;

            var key = PosKey.From(be.GetPosition());
            nodes.Remove(key);
        }

        // ---------- ticking ----------
        public void ServerTick(float dt) => StepFlows(dt);

        // ---------- flow core ----------
        void StepFlows(float dt)
        {
            // Plan A->B transfers per undirected edge
            var intents = new Dictionary<(PosKey from, PosKey to), float>();

            foreach (var kv in nodes)
            {
                var aKey  = kv.Key;
                var nodeA = kv.Value;

                foreach (var face in BlockFacing.ALLFACES)
                {
                    var bKey = aKey.Add(face);
                    if (!nodes.TryGetValue(bKey, out var nodeB)) continue;

                    // Schedule each undirected edge once (lexicographic on position)
                    if (bKey.X < aKey.X || (bKey.X == aKey.X && (bKey.Y < aKey.Y || (bKey.Y == aKey.Y && bKey.Z < aKey.Z))))
                        continue;

                    // Orient high P -> low P
                    var fromNode = nodeA;
                    var toNode   = nodeB;
                    var pFrom    = fromNode.Pressure;
                    var pTo      = toNode.Pressure;
                    var useFace  = face;

                    if (pFrom <= pTo)
                    {
                        fromNode = nodeB;
                        toNode   = nodeA;
                        pFrom    = fromNode.Pressure;
                        pTo      = toNode.Pressure;
                        useFace  = face.Opposite;
                    }

                    float dP = pFrom - pTo;
                    if (dP <= 0f) continue;

                    float g1 = fromNode.GetConductance(useFace);
                    float g2 = toNode.GetConductance(useFace.Opposite);
                    float g  = BaseConductance * 0.5f * (g1 + g2);

                    float propose    = Math.Min(dP * g * dt, MaxPerTickFlow * dt);
                    if (propose <= 0f) continue;

                    float giveCap    = Math.Max(0f, fromNode.Volume);
                    float receiveCap = Math.Max(0f, toNode.Capacity - toNode.Volume);
                    float m          = Math.Min(propose, Math.Min(giveCap, receiveCap));
                    if (m <= 0f) continue;

                    var fromKey = PosKey.From((fromNode as BEBehaviorFluidBase).GetPosition());
                    var toKey   = PosKey.From((toNode   as BEBehaviorFluidBase).GetPosition());
                    var edge    = (from: fromKey, to: toKey);
                    if (!intents.TryAdd(edge, m)) intents[edge] += m;
                }
            }

            // Apply transfers with a safety cap
            foreach (var it in intents)
            {
                if (!nodes.TryGetValue(it.Key.from, out var from) ||
                    !nodes.TryGetValue(it.Key.to,   out var to)) continue;

                float giveCapFrom     = Math.Max(0f, from.Volume);
                float receiveCapInto  = Math.Max(0f, to.Capacity - to.Volume);
                float m               = Math.Min(it.Value, Math.Min(giveCapFrom, receiveCapInto));
                if (m <= 0f) continue;

                from.RemoveFluid(m);
                to.AddFluid(m);
            }

            // Per-node damping hook (lets nodes smooth/relax if they want)
            foreach (var n in nodes.Values) n.OnAfterFlowStep(Damping);
        }

        // Kept for discovery/merging calls in your behaviors.
        public static bool FluidsCompatible(FluidNetwork a, FluidNetwork b) => true;
    }

    // ----------------------------- Manager -----------------------------
    /// <summary>
    /// Lean manager for FluidNetwork instances.
    /// - Server-side tick only
    /// - No client channels, no chunk/full-load tracking, no render hooks
    /// - Simple create/get/delete APIs
    /// </summary>
    public class FluidModSystem : ModSystem
    {
        public ICoreAPI Api  { get; private set; }
        public ICoreServerAPI Sapi { get; private set; }

        private long nextNetworkId = 1;
        private readonly Dictionary<long, FluidNetwork> networksById = new Dictionary<long, FluidNetwork>();

        public override bool ShouldLoad(EnumAppSide side) => true;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;

            if (api.Side == EnumAppSide.Server)
            {
                Sapi = (ICoreServerAPI)api;
                // Tick callback every 20 ms (~50Hz). 'dt' is seconds.
                api.World.RegisterGameTickListener(OnServerTick, 20);
            }
        }

        private void OnServerTick(float dt)
        {
            // Tick only networks that currently have nodes
            foreach (var nw in networksById.Values)
            {
                if (nw.nodes.Count > 0)
                    nw.ServerTick(dt);
            }
        }

        /// <summary>Create a new fluid network and return it.</summary>
        public FluidNetwork CreateNetwork(FluidInterfaces.IFluidDevice _)
        {
            var id = nextNetworkId++;
            var nw = new FluidNetwork(this, id);
            networksById[id] = nw;
            return nw;
        }

        /// <summary>Delete an existing network.</summary>
        public void DeleteNetwork(FluidNetwork nw)
        {
            if (nw != null) networksById.Remove(nw.networkId);
        }

        /// <summary>Get an existing network by id or create an empty one for that id.</summary>
        public FluidNetwork GetOrCreateNetwork(long id)
        {
            if (!networksById.TryGetValue(id, out var nw))
            {
                nw = new FluidNetwork(this, id);
                networksById[id] = nw;

                // Keep id generation monotonic if a manual id appears.
                if (id >= nextNetworkId) nextNetworkId = id + 1;
            }
            return nw;
        }
    }
}
