using System.Collections.Generic;
using HydrateOrDiedrate.Piping.Networking;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.ShutoffValve
{
    public class BlockEntityShutoffValve : BlockEntity
    {
        ICoreClientAPI capi;

        public bool Enabled { get; set; } = true;
        public ValveAxis Axis { get; set; } = ValveAxis.UD;
        public bool AxisInitialized { get; set; } = false;
        public int RollSteps { get; set; } = 0;

        MeshData pipeMeshAuthored;
        MeshData pipeMeshWorldOriented;
        internal ValveHandleRenderer HandleRenderer { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            capi = api as ICoreClientAPI;

            if (api.Side == EnumAppSide.Client)
            {
                pipeMeshAuthored ??= LoadMesh("shapes/block/shutoffvalve/valve-pipe.json");
                RebuildWorldOrientedPipeMesh();
                HandleRenderer?.Dispose();
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

        void RebuildWorldOrientedPipeMesh()
        {
            if (pipeMeshAuthored == null || capi == null) return;

            pipeMeshWorldOriented = pipeMeshAuthored.Clone();
            Orientation.ApplyPipe(pipeMeshWorldOriented, Axis, RollSteps);
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
            var mesh = pipeMeshAuthored.Clone();
            Orientation.ApplyPipe(mesh, Axis, RollSteps);

            mesher.AddMeshData(mesh);
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);

            bool prevEnabled = Enabled;
            var  prevAxis    = Axis;
            int  prevRoll    = RollSteps;

            Enabled         = tree.GetBool("enabled", Enabled);
            Axis            = (ValveAxis)tree.GetInt("axis", (int)Axis);
            AxisInitialized = tree.GetBool("axisInit", AxisInitialized);
            RollSteps       = tree.GetInt("roll", RollSteps);

            if (Api?.Side == EnumAppSide.Client)
            {
                bool geomChanged = (prevAxis != Axis) || (prevRoll != RollSteps);
                if (geomChanged)
                {
                    RebuildWorldOrientedPipeMesh();
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

            var srv = (Vintagestory.API.Server.ICoreServerAPI)Api;
            var pkt = new ValveToggleEventPacket { X = Pos.X, Y = Pos.Y, Z = Pos.Z, Enabled = Enabled };
            srv.Network.GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID).BroadcastPacket(pkt);
            return true;
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (Api?.Side == EnumAppSide.Client)
            {
                HandleRenderer?.Dispose();
                HandleRenderer = null;
            }
        }

        internal static class Orientation
        {
            public static void ApplyPipe(MeshData mesh, ValveAxis axis, int rollSteps)
            {
                var c = new Vec3f(0.5f, 0.5f, 0.5f);
                switch (axis)
                {
                    case ValveAxis.NS: mesh.Rotate(c, GameMath.PIHALF, 0f, 0f);       break;
                    case ValveAxis.EW: mesh.Rotate(c, 0f, 0f, -GameMath.PIHALF);      break;
                }
                float roll = (rollSteps & 3) * GameMath.PIHALF;
                switch (axis)
                {
                    case ValveAxis.UD: mesh.Rotate(c, 0f, roll, 0f); break;
                    case ValveAxis.NS: mesh.Rotate(c, 0f, 0f, roll); break;
                    case ValveAxis.EW: mesh.Rotate(c, roll, 0f, 0f); break;
                }
            }

            public static void GetRendererRotations(ValveAxis axis, int rollSteps,
                out float preX, out float preY, out float preZ, out float rollAroundAxis)
            {
                preX = preY = preZ = 0f;
                if (axis == ValveAxis.NS) preX = GameMath.PIHALF;
                if (axis == ValveAxis.EW) preZ = -GameMath.PIHALF;

                rollAroundAxis = (rollSteps & 3) * GameMath.PIHALF;
            }
        }
    }
}
