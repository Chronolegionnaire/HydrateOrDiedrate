using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate
{
    public class DrinkHudOverlayRenderer : IRenderer, IDisposable
    {
        private ICoreClientAPI api;
        private MeshRef circleMesh;
        private float circleAlpha;
        public float CircleProgress { get; set; }
        public bool CircleVisible { get; set; }
        public bool IsDangerous { get; set; } // New flag for rendering red ring

        public DrinkHudOverlayRenderer(ICoreClientAPI api)
        {
            this.api = api;
            api.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "drinkoverlay");
            UpdateCircleMesh(1f);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!CircleVisible || CircleProgress <= 0f) return;

            // Calculate the alpha (visibility fade-in/out)
            circleAlpha = Math.Max(0f, Math.Min(1f, circleAlpha + deltaTime / (CircleVisible ? 0.2f : -0.4f)));
            if (circleAlpha <= 0f) return;

            // Update the circle mesh progress
            UpdateCircleMesh(CircleProgress);

            // Set the color based on the danger flag
            Vec4f color;
            if (IsDangerous)
            {
                color = new Vec4f(1.0f, 0.0f, 0.0f, circleAlpha); // Red color for dangerous drink source
            }
            else
            {
                color = new Vec4f(0.8f, 0.8f, 1.0f, circleAlpha); // Normal color
            }

            // Set the color and rendering properties
            IRenderAPI render = api.Render;
            IShaderProgram shader = render.CurrentActiveShader;
            shader.Uniform("rgbaIn", color);
            shader.Uniform("tex2d", 0);
            shader.Uniform("noTexture", 1f);

            // Position the circle at the center of the screen
            render.GlPushMatrix();
            int centerX = api.Render.FrameWidth / 2;
            int centerY = api.Render.FrameHeight / 2;
            render.GlTranslate(centerX, centerY, 0);
            render.GlScale(50f, 50f, 1f);

            // Render the circle mesh
            shader.UniformMatrix("projectionMatrix", render.CurrentProjectionMatrix);
            render.RenderMesh(circleMesh);
            render.GlPopMatrix();
        }

        private void UpdateCircleMesh(float progress)
        {
            int steps = 1 + (int)Math.Ceiling(16f * progress);
            MeshData meshData = new MeshData(steps * 2, steps * 6, false, false, true, false);

            for (int i = 0; i < steps; i++)
            {
                double angle = Math.Min(progress, i * 0.0625f) * Math.PI * 2;
                float x = (float)Math.Sin(angle);
                float y = -(float)Math.Cos(angle);
                meshData.AddVertexSkipTex(x, y, 0, -1);
                meshData.AddVertexSkipTex(x * 0.75f, y * 0.75f, 0, -1);

                if (i > 0)
                {
                    meshData.AddIndices(new int[] { i * 2 - 2, i * 2 - 1, i * 2 });
                    meshData.AddIndices(new int[] { i * 2, i * 2 - 1, i * 2 + 1 });
                }
            }

            if (circleMesh != null)
            {
                api.Render.UpdateMesh(circleMesh, meshData);
            }
            else
            {
                circleMesh = api.Render.UploadMesh(meshData);
            }
        }

        public void Dispose()
        {
            if (circleMesh != null)
            {
                api.Render.DeleteMesh(circleMesh);
            }
        }

        public double RenderOrder => 0.0;
        public int RenderRange => 1000;
    }
}