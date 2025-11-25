using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace HydrateOrDiedrate.Wells.Winch;

public class WinchTopRenderer : IRenderer
{
    public const string RopeKnotMeshPath = "shapes/block/winch/knot.json";
    public const string RopeSegmentMeshPath = "shapes/block/winch/rope1x1.json";
    public const string WinchTopMeshPath = "shapes/block/winch/top.json";

    private readonly ICoreClientAPI Capi;
    private readonly BlockEntityWinch Winch;
    private readonly BEBehaviorMPConsumer mpc;
    
    private readonly Matrixf ModelMat = new();
    private readonly string Direction;

    private MultiTextureMeshRef WinchTopMeshRef;
    private MultiTextureMeshRef RopeSegmentMeshRef;
    private MultiTextureMeshRef RopeKnotMeshRef;

    private MultiTextureMeshRef containerMeshRef;
    private ItemStack lastContainerStack;
    
    private float containerScale = 1f;
    private readonly Vec3f containerOffset = new();
    private readonly Vec3f containerRotRad = new(); 
    
    public double RenderOrder => 0.5;
    public int RenderRange => 24;
    
    private float AngleRad;
    private float lastBucketDepth;

    public WinchTopRenderer(ICoreClientAPI coreClientAPI, BlockEntityWinch winch, string direction)
    {
        Capi = coreClientAPI;
        Winch = winch;
        mpc = Winch.GetBehavior<BEBehaviorMPConsumer>();
        Direction = direction;
        
        coreClientAPI.Event.EnqueueMainThreadTask(LoadStaticMeshes, "mesh-mesh-gen");
        ScheduleMeshUpdate();
    }

    private void LoadStaticMeshes()
    {
        if(Disposed) return;
        var mesh = Winch.GetMesh(WinchTopMeshPath);
        if (mesh is not null) WinchTopMeshRef = Capi.Render.UploadMultiTextureMesh(mesh);

        mesh = Winch.GetMesh(RopeSegmentMeshPath);
        if(mesh is not null) RopeSegmentMeshRef = Capi.Render.UploadMultiTextureMesh(mesh);

        mesh = Winch.GetMesh(RopeKnotMeshPath);
        if(mesh is not null)
        {
            const float knotScaleFactor = 1.3f;
            mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), knotScaleFactor, knotScaleFactor, knotScaleFactor);
            RopeKnotMeshRef = Capi.Render.UploadMultiTextureMesh(mesh);
        }
    }

    private void UpdateMesh()
    {
        if (Disposed) return;

        var containerStack = Winch.InputSlot.Itemstack;
        if (containerStack is null || containerStack.Collectible is not BlockLiquidContainerTopOpened container)
        {
            CleanupContainerData();
            return;
        }

        if (lastContainerStack is not null)
        {
            if (containerStack.Equals(Capi.World, lastContainerStack, GlobalConstants.IgnoredStackAttributes) &&
                ((BlockLiquidContainerTopOpened)lastContainerStack.Collectible).GetCurrentLitres(lastContainerStack) == container.GetCurrentLitres(containerStack))
            {
                return;
            }

            CleanupContainerData();
        }

        lastContainerStack = containerStack.Clone();
        var mesh = container.GenMesh(Capi, container.GetContent(lastContainerStack), null);
        if (mesh is null) return;

        bool isJug = lastContainerStack.Collectible.Code.Path.StartsWith("jug-", StringComparison.Ordinal);
        if (isJug)
        {
            containerScale = 0.85f;

            containerOffset.Set(0f, 0f, -0.075f);
            containerRotRad.Set(
                0f * GameMath.DEG2RAD,
                0f,
                -0.3f
            );
        }
        else
        {
            containerScale = 1f;

            containerOffset.Set(0f, 0f, 0f);
            containerRotRad.Set(0f, 0f, 0f);
        }

        containerMeshRef = Capi.Render.UploadMultiTextureMesh(mesh);
    }

    public void ScheduleMeshUpdate() => Capi.Event.EnqueueMainThreadTask(UpdateMesh, "mesh-mesh-gen");

    private void UpdateAngleRad(float deltaTime)
    {
        switch (Winch.RotationMode)
        {
            case EWinchRotationMode.MechanicalNetwork:
                float mechAngle = mpc.AngleRad;
                float turnDirSign = (mpc.Network.TurnDir == EnumRotDirection.Counterclockwise) ? 1f : -1f;
                
                AngleRad = Direction switch
                {
                    "east" => -mechAngle * turnDirSign,
                    "west" => mechAngle * turnDirSign,
                    "south" => mechAngle,
                    _ => -mechAngle,
                };
                break;

            case EWinchRotationMode.Player:
                if (Winch.RotationPlayer is not null)
                {
                    var controls = Winch.RotationPlayer.Entity?.Controls;
                    if (controls == null || !controls.RightMouseDown)
                    {
                        Winch.StopTurning();
                    }
                    else
                    {
                        AngleRad += deltaTime * 200f * GameMath.DEG2RAD * (Winch.IsRaising ? -1f : 1f);
                    }
                }

                break;
        }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (WinchTopMeshRef is null) return;
        var Pos = Winch.Pos;
        
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
        rpi.RenderMultiTextureMesh(WinchTopMeshRef, "tex", 0);

        lastBucketDepth = GameMath.Lerp(lastBucketDepth, Winch.BucketDepth, deltaTime * 10);

        if (containerMeshRef is null)
        {
            prog.Stop();
            return;
        }

        prog.ModelMatrix = ModelMat
            .Identity()
            .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y - lastBucketDepth, Pos.Z - camPos.Z)
            .Translate(0.5f, 0f, 0.5f)
            .RotateY(yRotation)
            .Translate(containerOffset.X, containerOffset.Y, containerOffset.Z)
            .Translate(0.5f, 0.5f, 0.5f)
            .RotateX(containerRotRad.X)
            .RotateY(containerRotRad.Y)
            .RotateZ(containerRotRad.Z)
            .Scale(containerScale, containerScale, containerScale)
            .Translate(-0.5f, -0.5f, -0.5f)
            .Translate(-0.5f, 0f, -0.5f)
            .Values;

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        rpi.RenderMultiTextureMesh(containerMeshRef, "tex", 0);
        
        if (RopeKnotMeshRef is not null)
        {
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y - lastBucketDepth, Pos.Z - camPos.Z)
                .Translate(0.5f, 0.85f, 0.5f)
                .RotateY(yRotation)
                .Translate(-0.5f, 0f, -0.5f)
                .Values;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMultiTextureMesh(RopeKnotMeshRef, "tex", 0);
        }

        if (RopeSegmentMeshRef is not null)
        {
            const float knotLocalY = 0.85f;
            const float ropeSegmentHeight = 0.125f;
            float segmentSpacing = ropeSegmentHeight;
            float knotY = -lastBucketDepth + knotLocalY;
            
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y + knotY, Pos.Z - camPos.Z)
                .Translate(0.5f, 0f, 0.5f)
                .RotateY(yRotation)
                .Translate(-0.5001f, -0.1f, -0.5001f)
                .Values;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMultiTextureMesh(RopeSegmentMeshRef, "tex", 0);

            const float ropeEntryLocalY = 0.65f;
            float stubTopY = knotY + ropeSegmentHeight;
            float availableLength = ropeEntryLocalY - stubTopY;

            if (availableLength > 0f)
            {
                int extraSegments = Math.Max(0, (int)(availableLength / segmentSpacing));

                for (int i = 0; i < extraSegments; i++)
                {
                    float yOffset = stubTopY + i * segmentSpacing;

                    prog.ModelMatrix = ModelMat
                        .Identity()
                        .Translate(Pos.X - camPos.X, Pos.Y - camPos.Y + yOffset, Pos.Z - camPos.Z)
                        .Translate(0.5f, 0f, 0.5f)
                        .RotateY(yRotation)
                        .Translate(-0.5001f, -0.1f, -0.5001f)
                        .Values;

                    prog.ViewMatrix = rpi.CameraMatrixOriginf;
                    prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
                    rpi.RenderMultiTextureMesh(RopeSegmentMeshRef, "tex", 0);
                }
            }
        }
        prog.Stop();
    }

    private void CleanupContainerData()
    {
        if(containerMeshRef is not null)
        {
            containerMeshRef.Dispose();
            containerMeshRef = null;
        }
        lastContainerStack = null;

        containerScale = 1f;
        containerOffset.Set(0f, 0f, 0f);
        containerRotRad.Set(0f, 0f, 0f);
    }

    public bool Disposed { get; private set; }
    public void Dispose()
    {
        Disposed = true;
        Capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        
        CleanupContainerData();

        if(WinchTopMeshRef is not null)
        {
            WinchTopMeshRef.Dispose();
            WinchTopMeshRef = null;
        }

        if(RopeKnotMeshRef is not null)
        {
            RopeKnotMeshRef.Dispose();
            RopeKnotMeshRef = null;
        }

        if(RopeSegmentMeshRef is not null)
        {
            RopeSegmentMeshRef.Dispose();
            RopeSegmentMeshRef = null;
        }
    }
}