using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.winch
{
    public class WinchRopeRenderer: IRenderer, IDisposable
    {
        private ICoreClientAPI api;
        private BlockPos pos;
        private MeshRef ropeMesh;
        private int ropeTexture;
        private string Direction { get; }

        public double RenderOrder => 0.6; // Render slightly above the winch
        public int RenderRange => 24;

        public WinchRopeRenderer(ICoreClientAPI api, BlockPos pos, string direction)
        {
            this.api = api;
            this.pos = pos;
            this.Direction = direction;
            this.ropeTexture = api.Render.GetOrLoadTexture(new AssetLocation("game:textures/entity/erel/pristine/rope.png")); // Replace with your rope texture path
            CreateRopeMesh(0.5f); // Adjust rope thickness as needed
        }

        private void CreateRopeMesh(float width)
        {
            ropeMesh?.Dispose();
            MeshData meshData = new MeshData(42, 42, false, true, false, false);
            meshData.SetMode(EnumDrawMode.Triangles);

            for (int i = 0; i <= 20; i++)
            {
                float u = (float)i / 20f;
                meshData.AddVertex(-width, width, 0, u, 0);
                meshData.AddVertex(width, -width, 0, u, 1);
            }

            for (int i = 0; i < 20; i++)
            {
                int index = 2 * i;
                meshData.AddIndex(index);
                meshData.AddIndex(index + 3);
                meshData.AddIndex(index + 2);

                meshData.AddIndex(index);
                meshData.AddIndex(index + 1);
                meshData.AddIndex(index + 3);
            }

            ropeMesh = api.Render.UploadMesh(meshData);
        }


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (ropeMesh == null) return;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            BlockEntity be = api.World.BlockAccessor.GetBlockEntity(pos);
            if (be is BlockEntityWinch beWinch)
            {
                Vec3f ropeStart = new Vec3f(0.5f, 1f, 0.5f); // Top of the winch
                Vec3f ropeEnd;

                // Calculate rope end position based on bucket depth and winch direction
                float yOffset = -beWinch.bucketDepth;
                ropeEnd = new Vec3f(0.5f, yOffset + 0.5f, 0.5f);


                // Adjust rope positions for winch direction
                float yRotation = 0f;
                switch (Direction)
                {
                    case "east":
                        yRotation = GameMath.PIHALF;
                        break;
                    case "south":
                        yRotation = GameMath.PI;
                        break;
                    case "west":
                        yRotation = GameMath.PI + GameMath.PIHALF;
                        break;
                }

                Matrixf modelMat = new Matrixf()
                  .Identity()
                  .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                  .RotateY(yRotation);


                // Apply transformations for start and end points
                Vec4f start4f = modelMat.TransformVector(new Vec4f(ropeStart.X, ropeStart.Y, ropeStart.Z, 1f));
                Vec3f start = new Vec3f(start4f.X, start4f.Y, start4f.Z);

                Vec4f end4f = modelMat.TransformVector(new Vec4f(ropeEnd.X, ropeEnd.Y, ropeEnd.Z, 1f));
                Vec3f end = new Vec3f(end4f.X, end4f.Y, end4f.Z);



                // Calculate the distance between the start and end points
                float ropeLength = start.DistanceTo(end);

                // Calculate the rotation of the rope
                Vec3f diff = end - start;
                float ropeYaw = (float)Math.Atan2(diff.X, diff.Z);
                float ropePitch = (float)Math.Asin(diff.Y / ropeLength);

                IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
                prog.Tex2D = ropeTexture;

                prog.ModelMatrix = new Matrixf()
                  .Identity()
                  .Translate(start.X, start.Y, start.Z)
                  .RotateY(ropeYaw)
                  .RotateX(-ropePitch)
                  .Values;

                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

                rpi.RenderMesh(ropeMesh);
                prog.Stop();
            }
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            ropeMesh?.Dispose();
            ropeMesh = null;
        }
    }
}