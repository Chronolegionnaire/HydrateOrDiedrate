using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class HandPumpContainerRenderer : IRenderer
    {
        private readonly ICoreClientAPI capi;
        private readonly BlockEntityHandPump be;

        private readonly Matrixf modelMat = new();
        private MultiTextureMeshRef containerMeshRef;
        private ItemStack lastStack;

        private const double BaseY = 0.0;
        private const double ForwardOffset = 0.5;
        private const double SideNudge = 0.00;
        private const float   TiltDeg = 0f;

        public double RenderOrder => 0.5;
        public int RenderRange => 24;
        public bool Disposed { get; private set; }

        public HandPumpContainerRenderer(ICoreClientAPI capi, BlockEntityHandPump be)
        {
            this.capi = capi;
            this.be = be;
            ScheduleMeshUpdate();
        }

        public void ScheduleMeshUpdate()
        {
            if (Disposed) return;
            capi.Event.EnqueueMainThreadTask(UpdateMesh, "hod-handpump-rebuildmesh");
        }

        private void UpdateMesh()
        {
            if (Disposed) return;

            var stack = be.ContainerSlot?.Itemstack;
            if (stack == null || stack.Collectible is not BlockLiquidContainerTopOpened cont)
            {
                Cleanup();
                return;
            }
            if (lastStack != null &&
                stack.Equals(capi.World, lastStack, GlobalConstants.IgnoredStackAttributes) &&
                SameLitres(cont, stack, lastStack))
            {
                return;
            }

            Cleanup();
            lastStack = stack.Clone();

            var content = cont.GetContent(lastStack);
            var mesh = cont.GenMesh(capi, content, null);
            if (mesh == null) return;

            containerMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        private static bool SameLitres(BlockLiquidContainerTopOpened cont, ItemStack a, ItemStack b)
            => cont.GetCurrentLitres(a) == cont.GetCurrentLitres(b);

        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            if (containerMeshRef == null) return;

            var rpi = capi.Render;
            var prog = rpi.PreparedStandardShader(be.Pos.X, be.Pos.Y, be.Pos.Z);
            var cam = capi.World.Player.Entity.CameraPos;

            string side = be.Block?.Variant?["side"] ?? be.Block?.Variant?["horizontalorientation"] ?? "north";
            var face = BlockFacing.FromCode(side) ?? BlockFacing.NORTH;


            float yaw = face == BlockFacing.NORTH ? GameMath.PI
                : face == BlockFacing.EAST  ? GameMath.PIHALF
                : face == BlockFacing.SOUTH ? 0f
                : GameMath.PI + GameMath.PIHALF;

            float tilt = TiltDeg * GameMath.DEG2RAD;

            prog.ModelMatrix = modelMat
                .Identity()
                .Translate(be.Pos.X - cam.X, be.Pos.Y - cam.Y, be.Pos.Z - cam.Z)
                .Translate(0.5f, (float)BaseY, 0.5f)
                .RotateY(yaw)
                .Translate((float)SideNudge, 0f, (float)ForwardOffset)
                .RotateX(tilt)
                .Translate(-0.5f, 0f, -0.5f)
                .Values;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMultiTextureMesh(containerMeshRef, "tex", 0);
            prog.Stop();
        }

        private void Cleanup()
        {
            lastStack = null;
            if (containerMeshRef != null)
            {
                containerMeshRef.Dispose();
                containerMeshRef = null;
            }
        }

        public void Dispose()
        {
            Disposed = true;
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            Cleanup();
        }
    }
}
