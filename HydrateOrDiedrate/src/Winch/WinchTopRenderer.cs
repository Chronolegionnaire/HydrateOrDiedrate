using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.winch;

public class WinchTopRenderer : IRenderer
{
    private readonly ICoreClientAPI Capi;
    private readonly BlockEntityWinch Winch;
    private readonly BlockPos Pos;

    internal bool ShouldRotateManual;
    internal bool ShouldRotateAutomated;
    
    internal BEBehaviorMPConsumer mechPowerPart;
    
    private MultiTextureMeshRef meshref;
    private MultiTextureMeshRef bucketMeshRef;
    private MultiTextureMeshRef liquidContentsMeshRef;

    private ItemStack lastBucketStack;
    private ItemStack lastContentStack;

    public Matrixf ModelMat = new Matrixf();
    public float AngleRad;
    private readonly string Direction;
    
    public double RenderOrder => 0.5;
    public int RenderRange => 24;
    
    private float lastBucketDepth;
    private float targetBucketDepth;

    public WinchTopRenderer(ICoreClientAPI coreClientAPI, BlockEntityWinch winch, MeshData topMesh, string direction)
    {
        Capi = coreClientAPI;
        Winch = winch;
        Pos = winch.Pos;
        Direction = direction;

        if (topMesh is not null)
        {
            meshref = coreClientAPI.Render.UploadMultiTextureMesh(topMesh);
        }
    }

    private void UpdateLiquidMesh(ItemStack bucketStack, out ItemStack contentStack)
    {
        string containerCodePath = bucketStack.Collectible.Code?.Path ?? string.Empty; //TODO
        if (!containerCodePath.Contains("bucket"))
        {
            contentStack = null;
            CleanupBucketLiquidData();
            return;
        }

        contentStack = bucketStack.Attributes
            ?.GetTreeAttribute("contents")
            ?.GetItemstack("0");

        if (contentStack is null || (contentStack.Collectible is null && !contentStack.ResolveBlockOrItem(Capi.World)))
        {
            CleanupBucketLiquidData();
            return;
        }

        if (lastContentStack is not null && contentStack.Equals(Capi.World, lastContentStack, GlobalConstants.IgnoredStackAttributes)) return;
        CleanupBucketLiquidData();
        lastContentStack = contentStack.Clone();

        var props = BlockLiquidContainerBase.GetContainableProps(contentStack);
        if (props is null) return;

        Shape contentShape = Shape.TryGet(Capi, new AssetLocation("game", props.IsOpaque ? "shapes/block/wood/bucket/contents.json" : "shapes/block/wood/bucket/liquidcontents.json"));
        if (contentShape is null) return;

        var textureSource = new ContainerTextureSource(
            Capi,
            contentStack,
            props.Texture
        );

        Capi.Tesselator.TesselateShape(nameof(WinchTopRenderer), contentShape, out MeshData contentMesh, textureSource);
        if (contentMesh == null) return;

        if (props.ClimateColorMap is not null)
        {
            int col = Capi.World.ApplyColorMapOnRgba(
                props.ClimateColorMap,
                null,
                -1,
                Pos.X,
                Pos.Y,
                Pos.Z,
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

        liquidContentsMeshRef = Capi.Render.UploadMultiTextureMesh(contentMesh);
    }

    private void UpdateBucketMesh(ItemStack bucketStack)
    {
        //TODO this should need to be checked when the itemslot changes
        if (lastBucketStack is not null && bucketStack.Equals(Capi.World, lastBucketStack, GlobalConstants.IgnoredStackAttributes)) return;
        
        CleanupBucketData();
        lastBucketStack = bucketStack.Clone();
        lastBucketDepth = targetBucketDepth;

        MeshData itemMesh;
        switch (bucketStack.Class)
        {
            case EnumItemClass.Item:
                Capi.Tesselator.TesselateItem(bucketStack.Item, out itemMesh);
                break;

            case EnumItemClass.Block:
                Capi.Tesselator.TesselateBlock(bucketStack.Block, out itemMesh);
                break;

            default: return;
        }

        bucketMeshRef = Capi.Render.UploadMultiTextureMesh(itemMesh);
    }

    private void UpdateAngleRad(float deltaTime)
    {
        if (ShouldRotateAutomated && mechPowerPart is not null)
        {
            float mechAngle = mechPowerPart.AngleRad;
            float turnDirSign = (mechPowerPart.Network.TurnDir == EnumRotDirection.Counterclockwise) ? 1f : -1f;
            
            AngleRad = Direction switch
            {
                "east" => -mechAngle * turnDirSign,
                "west" => mechAngle * turnDirSign,
                "south" => mechAngle, //TODO: South and north don't need multiplier?
                _ => -mechAngle,
            };
        }
        else if (ShouldRotateManual && Winch.CanMove())
        {
            AngleRad += deltaTime * 200f * GameMath.DEG2RAD * (Winch.isRaising ? -1f : 1f);
        }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (meshref is null) return;

        IRenderAPI rpi = Capi.Render;
        Vec3d camPos = Capi.World.Player.Entity.CameraPos;
        rpi.GlDisableCullFace();
        rpi.GlToggleBlend(true, EnumBlendMode.Standard);
        IStandardShaderProgram prog = rpi.PreparedStandardShader(Pos.X, Pos.Y, Pos.Z);

        float yRotation = Direction switch
        {
            "east" => GameMath.PIHALF,
            "south" => GameMath.PI,
            "west" => GameMath.PI + GameMath.PIHALF,
            _ => 0f
        };

        UpdateAngleRad(deltaTime);

        prog.ModelMatrix = ModelMat
            .Identity()
            .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y, Pos.Z - camPos.Z)
            .Translate(0.5f, 0.5f, 0.5f)
            .RotateY(yRotation)
            .RotateX(AngleRad)
            .Translate(-0.5f, 0.0f, -0.5f)
            .Values;

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        rpi.RenderMultiTextureMesh(meshref, "tex", 0);
        prog.Stop();

        ItemStack bucketStack = Winch.InputSlot.Itemstack;
        if (bucketStack?.Collectible is null)
        {
            CleanupBucketData();
            CleanupBucketLiquidData();
            return;
        }


        targetBucketDepth = Winch.bucketDepth;

        UpdateBucketMesh(bucketStack);

        if (bucketMeshRef is null)
        {
            CleanupBucketLiquidData();
            return;
        }
        
        lastBucketDepth = GameMath.Lerp(lastBucketDepth, targetBucketDepth, deltaTime * 50f);

        if (bucketMeshRef is null) return;

        IStandardShaderProgram bucketProg = rpi.PreparedStandardShader(Pos.X, Pos.Y, Pos.Z);

        bucketProg.ModelMatrix = ModelMat
            .Identity()
            .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y - lastBucketDepth, Pos.Z - camPos.Z)
            .Translate(0.5f, 0f, 0.5f)
            .RotateY(yRotation)
            .Translate(-0.5f, 0f, -0.5f)
            .Values;

        bucketProg.ViewMatrix = rpi.CameraMatrixOriginf;
        bucketProg.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        rpi.RenderMultiTextureMesh(bucketMeshRef, "tex", 0);
        bucketProg.Stop();

        UpdateLiquidMesh(bucketStack, out var contentStack);
        if (liquidContentsMeshRef is null) return;

        var props = BlockLiquidContainerBase.GetContainableProps(contentStack);
        if (props is null) return;

        IStandardShaderProgram contentProg = rpi.PreparedStandardShader(Pos.X, Pos.Y, Pos.Z);
        
        //TODO this shouldn't need to be re-calculated all the time (hook into OnSlotModified instead)
        float maxLiquidHeight = 0.435f;
        float containerCapacity = bucketStack.Collectible is BlockLiquidContainerBase container ? container.CapacityLitres : 10f;
        float liquidPercentage = (float)contentStack.StackSize / (props.ItemsPerLitre * containerCapacity);
        float liquidHeight = liquidPercentage * maxLiquidHeight;
        float liquidOffset = 0f;

        contentProg.ModelMatrix = ModelMat
            .Identity()
            .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y - lastBucketDepth + liquidHeight + liquidOffset, Pos.Z - camPos.Z)
            .Translate(0.5f, 0f, 0.5f)
            .RotateY(yRotation)
            .Translate(-0.5f, 0f, -0.5f)
            .Values;

        contentProg.ViewMatrix = rpi.CameraMatrixOriginf;
        contentProg.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        rpi.RenderMultiTextureMesh(liquidContentsMeshRef, "tex", 0);
        contentProg.Stop();
    }

    private void CleanupBucketData()
    {
        if(bucketMeshRef is not null)
        {
            bucketMeshRef.Dispose();
            bucketMeshRef = null;
        }
        lastBucketStack = null;
    }

    private void CleanupBucketLiquidData()
    {
        if(liquidContentsMeshRef is not null)
        {
            liquidContentsMeshRef.Dispose();
            liquidContentsMeshRef = null;
        }
        lastContentStack = null;
    }

    public void Dispose()
    {
        Capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        
        CleanupBucketData();
        CleanupBucketLiquidData();

        if(meshref is not null)
        {
            meshref.Dispose();
            meshref = null;
        }
    }
}