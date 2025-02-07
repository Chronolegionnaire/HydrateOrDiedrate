using System;
using HydrateOrDiedrate.winch;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch
{
    public class WinchTopRenderer : IRenderer, IDisposable
    {
        internal bool ShouldRender;
        internal bool ShouldRotateManual;
        internal bool ShouldRotateAutomated;
        public BEBehaviorMPConsumer mechPowerPart;
        private ICoreClientAPI api;
        private BlockPos pos;
        private MeshRef meshref;
        private MeshRef bucketMeshRef;
        private ItemStack lastBucketStack;
        private bool lastIsRaising = false;
        public Matrixf ModelMat = new Matrixf();
        public float AngleRad;
        private string Direction { get; }
        public double RenderOrder => 0.5;
        public int RenderRange => 24;
        
        private float lastBucketDepth;
        private float currentBucketDepth;
        private float targetBucketDepth;
        private float bucketInterpolationTime = 0.1f;

        public WinchTopRenderer(ICoreClientAPI coreClientAPI, BlockPos pos, MeshData topMesh, string direction)
        {
            this.api = coreClientAPI;
            this.pos = pos;
            this.Direction = direction;

            if (topMesh != null)
            {
                this.meshref = coreClientAPI.Render.UploadMesh(topMesh);
            }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (this.meshref == null || !this.ShouldRender) return;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true, EnumBlendMode.Standard);
            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;

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

            BlockEntity be = api.World.BlockAccessor.GetBlockEntity(pos);
            if (be is BlockEntityWinch beWinch)
            {
                if (ShouldRotateAutomated && mechPowerPart != null)
                {
                    float mechAngle = mechPowerPart.AngleRad;
                    if (Direction == "north" || Direction == "west")
                    {
                        mechAngle = -mechAngle;
                    }

                    float directionSign = beWinch.IsRaising ? -1f : 1f;
                    this.AngleRad = mechAngle * directionSign;
                }

                if (ShouldRotateManual)
                {
                    if ((beWinch.IsRaising && !beWinch.CanMoveUp()) ||
                        (!beWinch.IsRaising && !beWinch.CanMoveDown()))
                    {
                    }
                    else
                    {
                        AngleRad += deltaTime * 40f * GameMath.DEG2RAD * (beWinch.isRaising ? -1f : 1f);
                    }
                }
            }

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Translate(0.5f, 0.5f, 0.5f)
                .RotateY(yRotation)
                .RotateX(AngleRad)
                .Translate(-0.5f, 0.0f, -0.5f)
                .Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(this.meshref);
            prog.Stop();
            if (be is BlockEntityWinch beWinch2)
            {
                ItemStack bucketStack = beWinch2.InputSlot?.Itemstack;

                if (bucketStack != null && bucketStack.Collectible != null)
                {
                    targetBucketDepth = beWinch2.bucketDepth;
                    lastBucketDepth =
                        GameMath.Lerp(lastBucketDepth, targetBucketDepth, deltaTime * 10f);

                    if (lastBucketStack == null ||
                        !bucketStack.Equals(api.World, lastBucketStack, GlobalConstants.IgnoredStackAttributes))
                    {
                        lastBucketStack = bucketStack.Clone();
                        bucketMeshRef?.Dispose();
                        bucketMeshRef = null;

                        MeshData itemMesh;
                        if (bucketStack.Class == EnumItemClass.Item && bucketStack.Item != null)
                        {
                            api.Tesselator.TesselateItem(bucketStack.Item, out itemMesh);
                        }
                        else if (bucketStack.Class == EnumItemClass.Block && bucketStack.Block != null)
                        {
                            api.Tesselator.TesselateBlock(bucketStack.Block, out itemMesh);
                        }
                        else
                        {
                            return;
                        }

                        bucketMeshRef = api.Render.UploadMesh(itemMesh);
                    }

                    if (bucketMeshRef != null)
                    {
                        IStandardShaderProgram bucketProg = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);

                        if (bucketStack.Class == EnumItemClass.Item)
                        {
                            bucketProg.Tex2D = api.ItemTextureAtlas.AtlasTextures[0].TextureId;
                        }
                        else
                        {
                            bucketProg.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;
                        }

                        float yRotationBucket = 0f;
                        switch (Direction)
                        {
                            case "east": yRotationBucket = GameMath.PIHALF; break;
                            case "south": yRotationBucket = GameMath.PI; break;
                            case "west": yRotationBucket = GameMath.PI + GameMath.PIHALF; break;
                        }

                        bucketProg.ModelMatrix = ModelMat
                            .Identity()
                            .Translate(pos.X - camPos.X, pos.Y - camPos.Y - lastBucketDepth, pos.Z - camPos.Z)
                            .Translate(0.5f, 0f, 0.5f)
                            .RotateY(yRotationBucket)
                            .Translate(-0.5f, 0f, -0.5f)
                            .Values;

                        bucketProg.ViewMatrix = rpi.CameraMatrixOriginf;
                        bucketProg.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                        rpi.RenderMesh(bucketMeshRef);
                        bucketProg.Stop();
                    }
                }
                else
                {
                    bucketMeshRef?.Dispose();
                    bucketMeshRef = null;
                    lastBucketStack = null;
                }
            }
        }

        public void Dispose()
        {
            this.api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            this.meshref?.Dispose();
            this.bucketMeshRef?.Dispose();
            this.bucketMeshRef = null;
            this.meshref = null;
        }
    }
}