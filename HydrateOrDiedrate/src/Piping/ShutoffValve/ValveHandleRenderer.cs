using System;
using HydrateOrDiedrate.Piping.Networking;
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
            animStartAngle  = animTargetAngle = AngleFor(be.Enabled);
            animT = 1f;
            lastMs = capi.InWorldEllapsedMilliseconds;
        }
        public static void OnClientValveToggleEvent(ICoreClientAPI capi, ValveToggleEventPacket pkt)
        {
            var pos = new BlockPos(pkt.X, pkt.Y, pkt.Z);
            var be  = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityShutoffValve;
            if (be?.Api?.Side != EnumAppSide.Client) return;

            be.HandleRenderer?.AnimateToggle(pkt.Enabled);
            be.Enabled = pkt.Enabled;
            be.MarkDirty(true);
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

        static float AngleFor(bool enabled) => enabled ? 0f : GameMath.PIHALF;

        static float Ease(float t)
        {
            t = GameMath.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        static Matrixf ApplyToggle(Matrixf m, ValveAxis axis, int rollSteps, float angle)
        {
            int f = ((rollSteps % 4) + 4) % 4;
            int sign = axis == ValveAxis.UD
                ? 1 - 2 * (f >> 1)
                : 1 - 2 * (f & 1);

            return m.RotateZ(sign * angle);
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

            float angle = CurrentAngle();

            var m = model
                .Identity()
                .Translate(be.Pos.X - cam.X, be.Pos.Y - cam.Y, be.Pos.Z - cam.Z)
                .Translate(0.5f, 0.5f, 0.5f)
                .RotateX(preX).RotateY(preY).RotateZ(preZ)
                .RotateY(detentRoll);
            m = ApplyToggle(m, be.Axis, be.RollSteps, angle);

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
