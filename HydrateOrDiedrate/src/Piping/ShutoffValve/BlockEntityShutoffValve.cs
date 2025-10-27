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

        // State
        public bool Enabled { get; set; } = true;
        public ValveAxis Axis { get; set; } = ValveAxis.UD;
        public bool AxisInitialized { get; set; } = false;
        public int RollSteps { get; set; } = 0;

        // Client-only caches
        MeshData pipeMeshAuthored;       // original authoring-space pipe mesh
        MeshData pipeMeshWorldOriented;  // rotated for current axis+roll

        // EXPOSED: renderer without reflection
        internal ValveHandleRenderer HandleRenderer { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            capi = api as ICoreClientAPI;

            if (api.Side == EnumAppSide.Client)
            {
                // Load authored pipe once
                pipeMeshAuthored ??= LoadMesh("shapes/block/shutoffvalve/valve-pipe.json");
                RebuildWorldOrientedPipeMesh();

                // Create/refresh renderer for the handle
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

            // Always start from the authored mesh and orient it here so we’re never out of sync
            if (pipeMeshAuthored == null)
            {
                pipeMeshAuthored = LoadMesh("shapes/block/shutoffvalve/valve-pipe.json");
                if (pipeMeshAuthored == null) return true;
            }

            // Fresh clone → apply orientation → push
            var mesh = pipeMeshAuthored.Clone();
            Orientation.ApplyPipe(mesh, Axis, RollSteps);

            mesher.AddMeshData(mesh);

            // Handle is drawn by IRenderer
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
                    HandleRenderer?.OnOrientationChanged(); // keep handle aligned
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

                // Map authored Y → target pipe axis
                switch (axis)
                {
                    case ValveAxis.NS: mesh.Rotate(c, GameMath.PIHALF, 0f, 0f);       break; // +90° around X: Y→+Z
                    case ValveAxis.EW: mesh.Rotate(c, 0f, 0f, -GameMath.PIHALF);      break; // -90° around Z: Y→+X
                    // UD: no-op (Y stays Y)
                }

                // Roll detents around pipe axis
                float roll = (rollSteps & 3) * GameMath.PIHALF;
                switch (axis)
                {
                    case ValveAxis.UD: mesh.Rotate(c, 0f, roll, 0f); break; // around Y
                    case ValveAxis.NS: mesh.Rotate(c, 0f, 0f, roll); break; // around Z
                    case ValveAxis.EW: mesh.Rotate(c, roll, 0f, 0f); break; // around X
                }
            }

            public static void GetRendererRotations(ValveAxis axis, int rollSteps,
                out float preX, out float preY, out float preZ, out float rollAroundAxis)
            {
                preX = preY = preZ = 0f;

                // Same mapping chain as the baked mesh
                if (axis == ValveAxis.NS) preX = GameMath.PIHALF;   // Y→Z
                if (axis == ValveAxis.EW) preZ = -GameMath.PIHALF;  // Y→X

                rollAroundAxis = (rollSteps & 3) * GameMath.PIHALF;
            }
        }
    }
}
