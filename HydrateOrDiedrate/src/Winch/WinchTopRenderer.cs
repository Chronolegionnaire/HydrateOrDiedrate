using System;
using HydrateOrDiedrate.winch;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
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
        private MeshRef liquidContentsMeshRef;
        private ItemStack lastBucketStack;
        private ItemStack lastContentStack;
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

        private void UpdateLiquidMesh(ItemStack bucketStack)
        {
            if (bucketStack?.Attributes?.HasAttribute("contents") != true)
            {
                if (liquidContentsMeshRef != null)
                {
                    liquidContentsMeshRef.Dispose();
                    liquidContentsMeshRef = null;
                    lastContentStack = null;
                }
                return;
            }

            var contents = bucketStack.Attributes.GetTreeAttribute("contents");
            if (contents == null) return;

            var contentStack = contents.GetItemstack("0");
            if (contentStack != null && contentStack.Collectible == null)
            {
                contentStack.ResolveBlockOrItem(api.World);
            }
            if (lastContentStack != null && 
                contentStack.Equals(api.World, lastContentStack, GlobalConstants.IgnoredStackAttributes))
            {
                return;
            }

            lastContentStack = contentStack.Clone();
            
            if (liquidContentsMeshRef != null)
            {
                liquidContentsMeshRef.Dispose();
                liquidContentsMeshRef = null;
            }

            if (contentStack?.Collectible == null) return;

            var props = BlockLiquidContainerBase.GetContainableProps(contentStack);
            if (props == null) return;

            string shapePath = props.IsOpaque 
                ? "game:shapes/block/wood/bucket/contents"
                : "game:shapes/block/wood/bucket/liquidcontents";

            Shape contentShape = Shape.TryGet(api, shapePath + ".json");
            if (contentShape == null) return;

            ContainerTextureSource textureSource = new ContainerTextureSource(
                api,
                contentStack,
                props.Texture
            );

            MeshData contentMesh;
            api.Tesselator.TesselateShape(GetType().Name, contentShape, out contentMesh, textureSource);

            if (contentMesh == null) return;
            if (props.ClimateColorMap != null)
            {
                int col = api.World.ApplyColorMapOnRgba(
                    props.ClimateColorMap,
                    null,
                    -1,
                    pos.X,
                    pos.Y,
                    pos.Z,
                    false
                );

                byte[] rgba = ColorUtil.ToBGRABytes(col);
                for (int i = 0; i < contentMesh.Rgba.Length; i++)
                {
                    contentMesh.Rgba[i] = (byte)(contentMesh.Rgba[i] * rgba[i % 4] / 255);
                }
            }
            for (int i = 0; i < contentMesh.FlagsCount; i++)
            {
                contentMesh.Flags[i] &= ~4096;
            }

            liquidContentsMeshRef = api.Render.UploadMesh(contentMesh);
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

                    if (lastBucketStack == null ||
                        !bucketStack.Equals(api.World, lastBucketStack, GlobalConstants.IgnoredStackAttributes))
                    {
                        lastBucketStack = bucketStack.Clone();
                        lastBucketDepth = targetBucketDepth;

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
                    else
                    {
                        lastBucketDepth = GameMath.Lerp(lastBucketDepth, targetBucketDepth, deltaTime * 50f);
                    }
                    UpdateLiquidMesh(bucketStack);
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
                        if (liquidContentsMeshRef != null)
                        {
                            var contents = bucketStack.Attributes?.GetTreeAttribute("contents");
                            if (contents != null)
                            {
                                var contentStack = contents.GetItemstack("0");
                                if (contentStack?.Collectible != null)
                                {
                                    var props = BlockLiquidContainerBase.GetContainableProps(contentStack);
                                    if (props != null)
                                    {
                                        IStandardShaderProgram contentProg = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
                                        contentProg.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;

                                        float maxLiquidHeight = 0.435f;
                                        float liquidPercentage = (float)contentStack.StackSize / (props.ItemsPerLitre * 10f);
                                        float liquidHeight = liquidPercentage * maxLiquidHeight;
                                        float liquidOffset = 0f;

                                        contentProg.ModelMatrix = ModelMat
                                            .Identity()
                                            .Translate(pos.X - camPos.X, pos.Y - camPos.Y - lastBucketDepth + liquidHeight + liquidOffset, pos.Z - camPos.Z)
                                            .Translate(0.5f, 0f, 0.5f)
                                            .RotateY(yRotationBucket)
                                            .Translate(-0.5f, 0f, -0.5f)
                                            .Values;

                                        contentProg.ViewMatrix = rpi.CameraMatrixOriginf;
                                        contentProg.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                                        rpi.RenderMesh(liquidContentsMeshRef);
                                        contentProg.Stop();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    bucketMeshRef?.Dispose();
                    liquidContentsMeshRef?.Dispose();
                    bucketMeshRef = null;
                    liquidContentsMeshRef = null;
                    lastBucketStack = null;
                    lastContentStack = null;
                }
            }
        }

        public void Dispose()
        {
            this.api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            this.meshref?.Dispose();
            this.bucketMeshRef?.Dispose();
            this.liquidContentsMeshRef?.Dispose();
            this.bucketMeshRef = null;
            this.liquidContentsMeshRef = null;
            this.meshref = null;
        }
    }
}