using System;
using Vintagestory.API.Client;
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
		public Matrixf ModelMat = new Matrixf();
		public float AngleRad;
		private string Direction { get; }
		public WinchTopRenderer(ICoreClientAPI coreClientAPI, BlockPos pos, MeshData mesh, string direction)
		{
			this.api = coreClientAPI;
			this.pos = pos;
			this.meshref = coreClientAPI.Render.UploadMesh(mesh);
			this.Direction = direction;
		}
		public double RenderOrder
		{
			get
			{
				return 0.5;
			}
		}
		public int RenderRange
		{
			get
			{
				return 24;
			}
		}
		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if (this.meshref == null || !this.ShouldRender)
			{
				return;
			}

			IRenderAPI rpi = this.api.Render;
			Vec3d camPos = this.api.World.Player.Entity.CameraPos;

			rpi.GlDisableCullFace();
			rpi.GlToggleBlend(true, EnumBlendMode.Standard);

			IStandardShaderProgram standardShaderProgram = rpi.PreparedStandardShader(this.pos.X, this.pos.Y, this.pos.Z, null);

			standardShaderProgram.Tex2D = this.api.BlockTextureAtlas.AtlasTextures[0].TextureId;

			// Determine Y-rotation based on block Direction
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

			// Apply transformations: Translate -> Rotate Y -> Rotate X (animation) -> Translate Back
			standardShaderProgram.ModelMatrix = this.ModelMat.Identity()
				.Translate(this.pos.X - camPos.X, this.pos.Y - camPos.Y, this.pos.Z - camPos.Z) // Translate to block position
				.Translate(0.5f, 0.5f, 0.5f)  // Center for rotation
				.RotateY(yRotation)           // Apply directional rotation
				.RotateX(this.AngleRad)       // Apply dynamic animation
				.Translate(-0.5f, -0.0f, -0.5f) // Translate back
				.Values;

			standardShaderProgram.ViewMatrix = rpi.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = rpi.CurrentProjectionMatrix;

			rpi.RenderMesh(this.meshref);
			standardShaderProgram.Stop();

			if (this.ShouldRotateManual)
			{
				this.AngleRad += deltaTime * 40f * GameMath.DEG2RAD; // Rotate manually
			}

			if (this.ShouldRotateAutomated)
			{
				if (this.Direction == "north" || this.Direction == "west")
				{
					this.AngleRad = -this.mechPowerPart.AngleRad;
				}
				else
				{
					this.AngleRad = this.mechPowerPart.AngleRad;
				}
			}
		}

		public void Dispose()
		{
			this.api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
			this.meshref.Dispose();
		}
	}
}
