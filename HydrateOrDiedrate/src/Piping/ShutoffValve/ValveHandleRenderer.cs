using System;
using HydrateOrDiedrate.Piping.Networking; // for ValveToggleEventPacket
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.ShutoffValve
{
    public class ValveHandleRenderer : IRenderer
    {
        const string HandleMeshPath = "shapes/block/shutoffvalve/valve-handle.json";
        const int ToggleMs = 650;

        readonly ICoreClientAPI capi;
        readonly BlockEntityShutoffValve be;

        MultiTextureMeshRef handleRef;
        readonly Matrixf model = new();
        float animStartAngle, animTargetAngle, animT;
        long   lastMs;
        bool   animating;

        ILoadedSound clickSfx;
        static readonly AssetLocation clickSfxLoc = new AssetLocation("game", "sounds/block/cokeovendoor-close.ogg");

        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public ValveHandleRenderer(ICoreClientAPI capi, BlockEntityShutoffValve be)
        {
            this.capi = capi;
            this.be   = be;

            capi.Event.EnqueueMainThreadTask(LoadMesh, "valve-handle-load");
            EnsureSfx();
            // Initialize current pose
            animStartAngle  = animTargetAngle = AngleFor(be.Enabled);
            animT = 1f;
            lastMs = capi.InWorldEllapsedMilliseconds;
        }

        // Packet hook (no reflection)
        public static void OnClientValveToggleEvent(ICoreClientAPI capi, ValveToggleEventPacket pkt)
        {
            var pos = new BlockPos(pkt.X, pkt.Y, pkt.Z);
            var be  = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            if (be?.Api?.Side != EnumAppSide.Client) return;

            be.Enabled = pkt.Enabled;
            be.MarkDirty(true);
            be.HandleRenderer?.AnimateToggle(pkt.Enabled);
        }

        void LoadMesh()
        {
            Shape shape = Shape.TryGet(capi, new AssetLocation("hydrateordiedrate", HandleMeshPath));
            if (shape == null) return;
            capi.Tesselator.TesselateShape(be.Block, shape, out MeshData mesh);
            handleRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        void EnsureSfx()
        {
            if (clickSfx != null) return;
            clickSfx = capi.World.LoadSound(new SoundParams
            {
                Location = clickSfxLoc,
                ShouldLoop = false,
                DisposeOnFinish = false,
                RelativePosition = false,
                Range = 12,
                Position = be.Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f),
                Pitch = 1f
            });
        }

        public void OnOrientationChanged()
        {
            // Nothing persistent; we recompute transform every frame.
        }

        public void AnimateToggle(bool toEnabled)
        {
            animStartAngle  = CurrentAngle();
            animTargetAngle = AngleFor(toEnabled);
            animT = 0f;
            animating = true;

            if (clickSfx != null)
            {
                clickSfx.SetPitch((float)(0.10 + capi.World.Rand.NextDouble() * 0.90));
                clickSfx.SetPosition(be.Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f));
                if (clickSfx.IsPlaying) clickSfx.Stop();
                clickSfx.Start();
            }
        }

        float CurrentAngle() => animating ? GameMath.Lerp(animStartAngle, animTargetAngle, Ease(animT)) : AngleFor(be.Enabled);

        // Set your open/closed angles here (in radians). These rotate the handle around the pipe axis.
        static float AngleFor(bool enabled) => enabled ? 0f : GameMath.PIHALF; // 0 = open, 90° = closed

        static float Ease(float t)
        {
            t = GameMath.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
        
        static int Mod4(int x) => (x % 4 + 4) % 4;

        static Matrixf ApplyToggleByCase(Matrixf m, ValveAxis pipeAxis, int rollSteps, float angle)
        {
            int f = ((rollSteps % 4) + 4) % 4;

            switch (pipeAxis)
            {
                case ValveAxis.NS:
                    switch (f)
                    {
                        case 0: m.RotateZ(+angle); break;
                        case 1: m.RotateZ(-angle); break;
                        case 2: m.RotateZ(+angle); break;
                        case 3: m.RotateZ(-angle); break;
                    }
                    break;

                case ValveAxis.EW:
                    switch (f)
                    {
                        case 0: m.RotateZ(+angle); break;
                        case 1: m.RotateZ(-angle); break;
                        case 2: m.RotateZ(+angle); break;
                        case 3: m.RotateZ(-angle); break;
                    }
                    break;

                case ValveAxis.UD:
                    switch (f)
                    {
                        case 0: m.RotateZ(+angle); break;
                        case 1: m.RotateZ(+angle); break;
                        case 2: m.RotateZ(-angle); break;
                        case 3: m.RotateZ(-angle); break;
                    }
                    break;
            }

            return m;
        }


        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            if (handleRef == null) return;

            long now = capi.InWorldEllapsedMilliseconds;
            if (animating)
            {
                float d = (now - lastMs) / (float)ToggleMs;
                animT += d;
                if (animT >= 1f) { animT = 1f; animating = false; }
            }
            lastMs = now;

            var rpi = capi.Render;
            var cam = capi.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true, EnumBlendMode.Standard);
            var prog = rpi.PreparedStandardShader(be.Pos.X, be.Pos.Y, be.Pos.Z);

            BlockEntityShutoffValve.Orientation.GetRendererRotations(
                be.Axis, be.RollSteps,
                out float preX, out float preY, out float preZ,
                out float detentRoll
            );

            float angle = CurrentAngle(); // 0..PI/2

            var m = model
                .Identity()
                .Translate(be.Pos.X - cam.X, be.Pos.Y - cam.Y, be.Pos.Z - cam.Z)
                .Translate(0.5f, 0.5f, 0.5f)

                // (1) Pre-rotate authored-Y to the placed pipe axis
                .RotateX(preX).RotateY(preY).RotateZ(preZ)

                // (2) Apply base detent roll
                .RotateY(detentRoll);

            // (3) Per-orientation & facing animation axis/handedness
            m = ApplyToggleByCase(m, be.Axis, be.RollSteps, angle);

            prog.ModelMatrix = m
                .Translate(-0.5f, -0.5f, -0.5f)
                .Values;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMultiTextureMesh(handleRef, "tex", 0);
            prog.Stop();
        }

        public void Dispose()
        {
            if (handleRef != null) { handleRef.Dispose(); handleRef = null; }
            clickSfx?.Dispose();
            clickSfx = null;
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        }
    }
}
