using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.FluidNetwork.HandPump
{
    public class HandPumpRenderer : IRenderer
    {
        private readonly ICoreClientAPI capi;
        private readonly BlockEntityHandPump be;

        private MultiTextureMeshRef pumpMeshRef;
        private MultiTextureMeshRef containerMeshRef;
        private ItemStack lastContainerStack;

        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public HandPumpRenderer(ICoreClientAPI capi, BlockEntityHandPump be)
        {
            this.capi = capi;
            this.be = be;

            capi.Event.EnqueueMainThreadTask(LoadMeshes, "handpump-mesh");
        }

        private void LoadMeshes()
        {
            // TODO: tesselate your hand pump model
            // pumpMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
            UpdateContainerMesh();
        }

        public void UpdateContainerMesh()
        {
            var stack = be.ContainerSlot.Itemstack;
            if (stack == null || stack.Collectible is not BlockLiquidContainerTopOpened top) { ClearContainerMesh(); return; }

            if (lastContainerStack != null)
            {
                var topPrev = lastContainerStack.Collectible as BlockLiquidContainerTopOpened;
                if (stack.Equals(capi.World, lastContainerStack, GlobalConstants.IgnoredStackAttributes) &&
                    topPrev?.GetCurrentLitres(lastContainerStack) == top.GetCurrentLitres(stack)) return;

                ClearContainerMesh();
            }

            lastContainerStack = stack.Clone();
            var mesh = top.GenMesh(capi, top.GetContent(lastContainerStack), null);
            if (mesh == null) return;
            containerMeshRef = capi.Render.UploadMultiTextureMesh(mesh);
        }

        private void ClearContainerMesh()
        {
            containerMeshRef?.Dispose();
            containerMeshRef = null;
            lastContainerStack = null;
        }

        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            if (pumpMeshRef == null && containerMeshRef == null) return;

            var rpi = capi.Render;
            var cam = capi.World.Player.Entity.CameraPos;
            var pos = be.Pos;

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            var model = new Matrixf();

            // pump base
            if (pumpMeshRef != null)
            {
                prog.ModelMatrix = model
                    .Identity()
                    .Translate(pos.X - cam.X, pos.Y - cam.Y, pos.Z - cam.Z)
                    .Values;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                rpi.RenderMultiTextureMesh(pumpMeshRef, "tex", 0);
            }

            // inserted container (position appropriately)
            if (containerMeshRef != null)
            {
                prog.ModelMatrix = model
                    .Identity()
                    .Translate(pos.X - cam.X + 0.5f, pos.Y - cam.Y + 0.75f, pos.Z - cam.Z + 0.5f) // tweak
                    .Values;
                rpi.RenderMultiTextureMesh(containerMeshRef, "tex", 0);
            }

            prog.Stop();
        }

        public void Dispose()
        {
            capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            pumpMeshRef?.Dispose(); pumpMeshRef = null;
            containerMeshRef?.Dispose(); containerMeshRef = null;
            lastContainerStack = null;
        }
    }
}
