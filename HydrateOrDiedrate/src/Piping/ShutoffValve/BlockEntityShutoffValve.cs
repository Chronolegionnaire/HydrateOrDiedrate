using System.Collections.Generic;
using HydrateOrDiedrate.Piping;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Piping.Networking;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Piping.ShutoffValve
{
    public class BlockEntityShutoffValve : BlockEntity
    {
        ICoreClientAPI capi;

        public bool Enabled { get; set; } = true;
        public EValveAxis Axis { get; set; } = EValveAxis.UD;
        public bool AxisInitialized { get; set; } = false;
        public int RollSteps { get; set; } = 0;

        static MeshData pipeMeshAuthored;
        static readonly Dictionary<(EValveAxis axis, int roll), MeshData> PipeMeshByOrientation = new();

        internal ValveHandleRenderer HandleRenderer { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            capi = api as ICoreClientAPI;

            if (api.Side == EnumAppSide.Client)
            {
                pipeMeshAuthored ??= LoadMesh("shapes/block/shutoffvalve/valve-pipe.json");

                HandleRenderer = new ValveHandleRenderer(capi, this);
                capi.Event.RegisterRenderer(HandleRenderer, EnumRenderStage.Opaque, "valve-handle");
            }
        }

        MeshData LoadMesh(string path)
        {
            if (Api is not ICoreClientAPI cc) return null;

            Shape shape = Shape.TryGet(Api, new AssetLocation("hydrateordiedrate", path));
            if (shape == null) return null;

            cc.Tesselator.TesselateShape(Block, shape, out MeshData mesh);
            return mesh;
        }
        MeshData GetOrientedMesh()
        {
            if (pipeMeshAuthored == null) return null;

            int rollKey = RollSteps & 3;
            var key = (Axis, rollKey);

            if (PipeMeshByOrientation.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var mesh = pipeMeshAuthored.Clone();
            PipeUtil.ValveOrientationUtil.ApplyPipe(mesh, Axis, rollKey);
            PipeMeshByOrientation[key] = mesh;
            return mesh;
        }

        public void MarkDirty(bool redrawNow)
        {
            base.MarkDirty(redrawNow);
            Api?.World?.BlockAccessor?.MarkBlockDirty(Pos);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tess)
        {
            if (Api.Side != EnumAppSide.Client) return true;

            if (pipeMeshAuthored == null)
            {
                pipeMeshAuthored = LoadMesh("shapes/block/shutoffvalve/valve-pipe.json");
                if (pipeMeshAuthored == null) return true;
            }
            var oriented = GetOrientedMesh();
            if (oriented == null) return true;

            mesher.AddMeshData(oriented.Clone());
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);

            bool prevEnabled = Enabled;
            var  prevAxis    = Axis;
            int  prevRoll    = RollSteps;

            Enabled         = tree.GetBool("enabled", Enabled);
            Axis            = (EValveAxis)tree.GetInt("axis", (int)Axis);
            AxisInitialized = tree.GetBool("axisInit", AxisInitialized);
            RollSteps       = tree.GetInt("roll", RollSteps);

            if (Api?.Side == EnumAppSide.Client)
            {
                bool geomChanged = (prevAxis != Axis) || (prevRoll != RollSteps);
                if (geomChanged)
                {
                    Api.World.BlockAccessor.MarkBlockDirty(Pos);
                }

                if (prevEnabled != Enabled)
                {
                    HandleRenderer?.AnimateToggle(Enabled);
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("enabled", Enabled);
            tree.SetInt("axis", (int)Axis);
            tree.SetBool("axisInit", AxisInitialized);
            tree.SetInt("roll", RollSteps);
        }

        public bool ServerToggleEnabled(IPlayer byPlayer = null)
        {
            if (Api?.Side != EnumAppSide.Server) return false;

            Enabled = !Enabled;
            MarkDirty(true);

            FluidNetworkState.InvalidateNetwork();

            var srv = (ICoreServerAPI)Api;
            var pkt = new ValveToggleEventPacket { X = Pos.X, Y = Pos.Y, Z = Pos.Z, Enabled = Enabled };
            srv.Network.GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID).BroadcastPacket(pkt);
            return true;
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (Api?.Side == EnumAppSide.Server)
            {
                FluidNetworkState.InvalidateNetwork();
            }
            if (Api?.Side == EnumAppSide.Client)
            {
                HandleRenderer?.Dispose();
                HandleRenderer = null;
            }
        }
    }
}
