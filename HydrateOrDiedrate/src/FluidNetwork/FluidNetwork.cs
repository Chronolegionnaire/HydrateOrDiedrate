// HydrateOrDiedrate/FluidNetwork/FluidNetwork.cs
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            float Pressure { get; } // typically Volume - Demand (rounded to int)

            float GetConductance(BlockFacing towards); // per-face openness [0..1]
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

    // ----------------------------- Network (compiled solver) -----------------------------
    public class FluidNetwork
    {
        public long networkId;
        
        public string FluidCode { get; private set; } = null;

        // Logical membership (cold path; discovery/joins/leaves)
        public readonly Dictionary<PosKey, FluidInterfaces.IFluidNode> nodes =
            new Dictionary<PosKey, FluidInterfaces.IFluidNode>();

        internal FluidModSystem mod;

        // Tuning
        const float BaseConductance = 3f;      // 1–10 reasonable
        const float Damping         = 0.995f;  // passed to OnAfterFlowStep
        const float MaxPerTickFlow  = 1000f;
        const float EpsDP           = 0.01f;   // small hysteresis

        // Dirty flags
        bool dirtyTopology = true;  // edges/node set changed
        bool dirtyWeights  = true;  // conductance masks changed

        // Compiled arrays for hot loop
        CompiledNetwork compiled;

        // Internal: one compiled instance reused
        sealed class CompiledNetwork
        {
            public int NodeCount;
            public int EdgeCount;

            public PosKey[] NodePos;
            public FluidInterfaces.IFluidNode[] NodeRefs;

            public float[] Volume;
            public float[] Capacity;
            public float[] Demand;         // from BEBehaviorFluidBase.GetDemand()
            public int[]   PressureL;      // int litres = round(Volume - Demand)
            public float[] Delta;          // per-node accumulator

            public byte[] CondMask;        // 6-bit openness per node (N,E,S,W,U,D)

            public Edge[]  Edges;
            public float[] G;              // per-edge conductance

            public Dictionary<PosKey, int> IndexOf; // cold path (debug/patching)
        }

        struct Edge
        {
            public int A, B;
            public byte FaceAB; // 0..5
        }

        public FluidNetwork(FluidModSystem mod, long id)
        {
            this.mod = mod;
            networkId = id;
            compiled = new CompiledNetwork
            {
                NodePos  = Array.Empty<PosKey>(),
                NodeRefs = Array.Empty<FluidInterfaces.IFluidNode>(),
                Volume   = Array.Empty<float>(),
                Capacity = Array.Empty<float>(),
                Demand   = Array.Empty<float>(),
                PressureL= Array.Empty<int>(),
                Delta    = Array.Empty<float>(),
                CondMask = Array.Empty<byte>(),
                Edges    = Array.Empty<Edge>(),
                G        = Array.Empty<float>(),
                IndexOf  = new Dictionary<PosKey, int>()
            };
        }

        public void EnsureFluidCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            if (FluidCode == null) FluidCode = code;
        }

        /// <summary>Try to adopt a new code; success if same or currently null.</summary>
        public bool TrySetFluidCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return true; // no-op
            if (FluidCode == null) { FluidCode = code; return true; }
            return string.Equals(FluidCode, code, StringComparison.Ordinal);
        }

        /// <summary>Networks are compatible if either is untyped or types match.</summary>
        public static bool FluidsCompatible(FluidNetwork a, FluidNetwork b)
        {
            if (a == null || b == null) return true;
            if (a.FluidCode == null || b.FluidCode == null) return true;
            return string.Equals(a.FluidCode, b.FluidCode, StringComparison.Ordinal);
        }

        // ---------- membership ----------
        public void Join(FluidInterfaces.IFluidNode node)
        {
            var be = node as BEBehaviorFluidBase;
            if (be == null) return;
            var key = PosKey.From(be.GetPosition());
            nodes[key] = node;
            dirtyTopology = true;
        }

        public void Leave(FluidInterfaces.IFluidNode node)
        {
            var be = node as BEBehaviorFluidBase;
            if (be == null) return;
            var key = PosKey.From(be.GetPosition());
            if (nodes.Remove(key)) dirtyTopology = true;
        }

        // Called by BEs when their capacity/conductance gating or face openness changes
        public void MarkDirtyTopology() => dirtyTopology = true;
        public void MarkDirtyWeights()  => dirtyWeights  = true;

        // ---------- ticking ----------
        public void ServerTick(float dt)
        {
            if (nodes.Count == 0) return;

            if (dirtyTopology)
            {
                RebuildCompiled(); // also recomputes weights
                dirtyTopology = false;
                dirtyWeights = false;
            }
            else if (dirtyWeights)
            {
                RecomputeEdgeWeights();
                dirtyWeights = false;
            }

            // hot loop
            TickCompiled(dt);

            // post-step hook back to BEs (damping hook)
            var refs = compiled.NodeRefs;
            for (int i = 0; i < compiled.NodeCount; i++)
            {
                refs[i].OnAfterFlowStep(Damping);
            }
        }

        // ---------- compile network ----------
        void RebuildCompiled()
        {
            var cn = compiled;

            // allocate arrays
            int n = nodes.Count;
            EnsureCapacity(n);

            // pack nodes
            cn.IndexOf.Clear();
            int idx = 0;
            foreach (var kv in nodes)
            {
                cn.NodePos[idx]  = kv.Key;
                cn.NodeRefs[idx] = kv.Value;
                cn.IndexOf[kv.Key] = idx;
                idx++;
            }
            cn.NodeCount = n;

            // refresh per-node scalar state from BEs (cheap; does not require rebuild)
            RefreshNodeScalars(cn);

            // Build edges (undirected, once per adjacency)
            var tmpEdges = new List<Edge>(Math.Max(6, n * 3));
            for (int a = 0; a < n; a++)
            {
                var posA = cn.NodePos[a];

                // Check 6 neighbors
                for (byte f = 0; f < 6; f++)
                {
                    var face = IndexToFace(f);
                    var posB = new PosKey(posA.X + face.Normali.X, posA.Y + face.Normali.Y, posA.Z + face.Normali.Z);
                    if (!cn.IndexOf.TryGetValue(posB, out int b)) continue;

                    // schedule each undirected edge once (a < b)
                    if (b <= a) continue;

                    // only connect if both nodes consider the face connected
                    var nodeA = cn.NodeRefs[a] as BEBehaviorFluidBase;
                    var nodeB = cn.NodeRefs[b] as BEBehaviorFluidBase;
                    bool okA = nodeA?.IsConnectedTowards(face) ?? false;
                    bool okB = nodeB?.IsConnectedTowards(face.Opposite) ?? false;
                    if (!okA || !okB) continue;

                    tmpEdges.Add(new Edge { A = a, B = b, FaceAB = f });
                }
            }

            cn.Edges = tmpEdges.ToArray();
            cn.EdgeCount = cn.Edges.Length;

            // compute cond masks and per-edge conductance
            ComputeCondMasks(cn);
            RecomputeEdgeWeights();
        }

        void RecomputeEdgeWeights()
        {
            var cn = compiled;
            int eCount = cn.EdgeCount;
            if (cn.G.Length != eCount) cn.G = new float[eCount];

            for (int i = 0; i < eCount; i++)
            {
                ref readonly var ed = ref cn.Edges[i];
                // conductance per face from bitmask
                float ga = ConductanceFromMask(cn.CondMask[ed.A], ed.FaceAB);
                float gb = ConductanceFromMask(cn.CondMask[ed.B], OppIndex(ed.FaceAB));
                cn.G[i] = BaseConductance * 0.5f * (ga + gb);
            }
        }

        void ComputeCondMasks(CompiledNetwork cn)
        {
            int n = cn.NodeCount;
            if (cn.CondMask.Length != n) cn.CondMask = new byte[n];

            for (int i = 0; i < n; i++)
            {
                byte mask = 0;
                var node = cn.NodeRefs[i] as BEBehaviorFluidBase;
                if (node == null)
                {
                    cn.CondMask[i] = 0;
                    continue;
                }

                // Pack 6 bits by checking "IsConnectedTowards" and scaling by GetConductance
                for (byte f = 0; f < 6; f++)
                {
                    var face = IndexToFace(f);
                    if (!node.IsConnectedTowards(face)) continue;

                    // convert conductance [0..1] into 0..1 bit (open/closed) but store strength in mask by 0..255 if needed
                    // here we just store 1 bit open/closed and use GetConductance for G once (more precise)
                    mask |= (byte)(1 << f);
                }
                cn.CondMask[i] = mask;
            }
        }

        void RefreshNodeScalars(CompiledNetwork cn)
        {
            int n = cn.NodeCount;

            EnsureArray(ref cn.Volume,   n);
            EnsureArray(ref cn.Capacity, n);
            EnsureArray(ref cn.Demand,   n);
            EnsureArray(ref cn.PressureL,n);
            EnsureArray(ref cn.Delta,    n);

            for (int i = 0; i < n; i++)
            {
                var beb = cn.NodeRefs[i] as BEBehaviorFluidBase;
                if (beb != null)
                {
                    cn.Volume[i]   = beb.Volume;
                    cn.Capacity[i] = beb.Capacity;
                    cn.Demand[i]   = beb.GetDemand();
                }
                else
                {
                    cn.Volume[i]   = cn.Volume[i];
                    cn.Capacity[i] = cn.Capacity[i];
                    cn.Demand[i]   = cn.Demand[i];
                }
            }
        }

        void EnsureCapacity(int n)
        {
            var cn = compiled;

            if (cn.NodePos.Length < n)
            {
                int cap = Math.Max(n, cn.NodePos.Length == 0 ? 8 : cn.NodePos.Length * 2);
                cn.NodePos  = new PosKey[cap];
                cn.NodeRefs = new FluidInterfaces.IFluidNode[cap];
                cn.Volume   = new float[cap];
                cn.Capacity = new float[cap];
                cn.Demand   = new float[cap];
                cn.PressureL= new int[cap];
                cn.Delta    = new float[cap];
                cn.CondMask = new byte[cap];
            }
        }

        static void EnsureArray<T>(ref T[] arr, int n)
        {
            if (arr == null || arr.Length < n) arr = new T[n];
        }

        // ---------- hot loop ----------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int IntPressure(float v, float d) => (int)MathF.Round(v - d);

        void TickCompiled(float dt)
        {
            var nw = compiled;
            int n = nw.NodeCount;

            // 0) pull latest Volume/Capacity/Demand from BEs (so AddFluid/SetDemand outside solver is seen immediately)
            RefreshNodeScalars(nw);

            // 1) compute integer pressures once
            for (int i = 0; i < n; i++)
            {
                nw.PressureL[i] = IntPressure(nw.Volume[i], nw.Demand[i]);
            }

            // 2) plan along edges (no dictionaries, no virtual calls)
            int eCount = nw.EdgeCount;
            for (int e = 0; e < eCount; e++)
            {
                ref var ed = ref nw.Edges[e];
                int a = ed.A, b = ed.B;

                int pa = nw.PressureL[a];
                int pb = nw.PressureL[b];
                if (pa == pb) continue;

                int from = pa > pb ? a : b;
                int to   = pa > pb ? b : a;

                float dP = Math.Abs(pa - pb);
                if (dP <= EpsDP) continue;

                float propose = MathF.Min(dP * nw.G[e] * dt, MaxPerTickFlow * dt);
                if (propose <= 0f) continue;

                float giveCap = nw.Volume[from];
                float recvCap = nw.Capacity[to] - nw.Volume[to];
                if (giveCap <= 0f || recvCap <= 0f) continue;

                float move = MathF.Min(propose, MathF.Min(giveCap, recvCap));
                if (move <= 0f) continue;

                nw.Delta[from] -= move;
                nw.Delta[to]   += move;
            }

            // 3) apply deltas + push back to BEs only when changed
            var refs = nw.NodeRefs;
            for (int i = 0; i < n; i++)
            {
                float dv = nw.Delta[i];
                if (dv != 0f)
                {
                    float v = nw.Volume[i] + dv;
                    if (v < 0f) v = 0f;
                    float cap = nw.Capacity[i];
                    if (v > cap) v = cap;
                    nw.Volume[i] = v;

                    // PASS THE DELTA to the node
                    (refs[i] as BEBehaviorFluidBase)?.SetVolumeFromNetwork(v, dv);

                    nw.Delta[i] = 0f;
                }
            }
        }

        // ---------- helpers ----------
        static BlockFacing IndexToFace(int idx)
        {
            return idx switch
            {
                0 => BlockFacing.NORTH,
                1 => BlockFacing.EAST,
                2 => BlockFacing.SOUTH,
                3 => BlockFacing.WEST,
                4 => BlockFacing.UP,
                5 => BlockFacing.DOWN,
                _ => BlockFacing.NORTH
            };
        }

        static int OppIndex(int idx) => idx switch
        {
            0 => 2, // N <-> S
            1 => 3, // E <-> W
            2 => 0,
            3 => 1,
            4 => 5, // U <-> D
            5 => 4,
            _ => 0
        };

        static float ConductanceFromMask(byte mask, int faceIdx)
        {
            int bit = 1 << faceIdx;
            return (mask & bit) != 0 ? 1f : 0f;
        }
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

                if (id >= nextNetworkId) nextNetworkId = id + 1;
            }
            return nw;
        }
    }
}
