using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.HUD
{
    public class DrinkHudOverlayRenderer : IRenderer, IDisposable
    {
        private const float CircleAlphaIn = 0.8F;
        private const float CircleAlphaOut = 0.8F;
        private const int CircleMaxSteps = 32;
        private const float OuterRadius = 24f;
        private const float InnerRadius = 18f;
        private const double DrinkDurationMs = 1000;
        
        private MeshRef circleMesh = null;
        private ICoreClientAPI api;
        private float circleAlpha = 0.0F;
        private float circleProgress = 0.0F;
        private float targetCircleProgress = 0.0F;
        
        private int drinkStartTime = 0;
        private bool isDrinking = false;

        public bool CircleVisible { get; set; }
        public bool IsDangerous { get; set; }

        public DrinkHudOverlayRenderer(ICoreClientAPI api)
        {
            this.api = api;
            api.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "drinkoverlay");
            UpdateCircleMesh(1);
        }

        public void ProcessDrinkProgress(float progress, bool drinking, bool dangerous)
        {
            IsDangerous = dangerous;
            if (drinking)
            {
                if (!isDrinking)
                {
                    drinkStartTime = Environment.TickCount;
                    isDrinking = true;
                    circleAlpha = 1.0f;
                }
                CircleVisible = true;
            }
            else
            {
                isDrinking = false;
                CircleVisible = false;
                targetCircleProgress = 0f;
            }
        }

        private void UpdateCircleMesh(float progress)
        {
            const float ringSize = (float)InnerRadius / OuterRadius;
            const float stepSize = 1.0F / CircleMaxSteps;
            int steps = Math.Max(1, 1 + (int)Math.Ceiling(Math.Min(CircleMaxSteps * progress, int.MaxValue / CircleMaxSteps)));
            int vertexCapacity = steps * 2;
            int indexCapacity = steps * 6;
            var data = new MeshData(vertexCapacity, indexCapacity, withUv: true, withRgba: false, withFlags: false);

            for (int i = 0; i < steps; i++)
            {
                var p = Math.Min(progress, i * stepSize) * Math.PI * 2;
                var x = (float)Math.Sin(p);
                var y = -(float)Math.Cos(p);

                data.AddVertex(x, y, 0, 0, 0);
                data.AddVertex(x * ringSize, y * ringSize, 0, 0, 0);

                if (i > 0)
                {
                    data.AddIndices(new[] { (i * 2) - 2, (i * 2) - 1, (i * 2) });
                    data.AddIndices(new[] { (i * 2), (i * 2) - 1, (i * 2) + 1 });
                }
            }

            if (progress == 1.0f)
            {
                data.AddIndices(new[] { (steps * 2) - 2, (steps * 2) - 1, 0 });
                data.AddIndices(new[] { 0, (steps * 2) - 1, 1 });
            }

            if (circleMesh != null)
            {
                api.Render.UpdateMesh(circleMesh, data);
            }
            else
            {
                circleMesh = api.Render.UploadMesh(data);
            }
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (CircleVisible && isDrinking)
            {
                circleAlpha = 1.0f;
                int elapsed = Environment.TickCount - drinkStartTime;
                float rawProgress = (float)elapsed / (float)DrinkDurationMs;
                float cycleProgress = rawProgress - (float)Math.Floor(rawProgress);
                targetCircleProgress = cycleProgress;
                float smoothingSpeed = 20f;
                circleProgress = GameMath.Lerp(circleProgress, targetCircleProgress, deltaTime * smoothingSpeed);
            }
            else if (circleAlpha > 0.0f)
            {
                circleAlpha = Math.Max(0.0f, circleAlpha - (deltaTime * CircleAlphaOut));
            }

            if (circleAlpha <= 0.0f && !CircleVisible)
            {
                circleProgress = 0.0f;
                targetCircleProgress = 0.0f;
            }

            if (circleAlpha > 0.0f || CircleVisible)
            {
                UpdateCircleMesh(circleProgress);
            }

            if (circleMesh != null)
            {
                Vec4f color = IsDangerous
                    ? new Vec4f(0.68f, 0.0f, 0.0f, circleAlpha)
                    : new Vec4f(0.4f, 0.85f, 1f, circleAlpha);

                IRenderAPI render = api.Render;
                IShaderProgram shader = render.CurrentActiveShader;

                shader.Uniform("rgbaIn", color);
                shader.Uniform("extraGlow", 0);
                shader.Uniform("applyColor", 0);
                shader.Uniform("tex2d", 0);
                shader.Uniform("noTexture", 1.0F);
                shader.UniformMatrix("projectionMatrix", render.CurrentProjectionMatrix);

                int x, y;
                if (api.Input.MouseGrabbed)
                {
                    x = api.Render.FrameWidth / 2;
                    y = api.Render.FrameHeight / 2;
                }
                else
                {
                    x = api.Input.MouseX;
                    y = api.Input.MouseY;
                }

                render.GlPushMatrix();
                render.GlTranslate(x, y, 0);
                render.GlScale(OuterRadius, OuterRadius, 0);
                shader.UniformMatrix("modelViewMatrix", render.CurrentModelviewMatrix);
                render.GlPopMatrix();

                render.RenderMesh(circleMesh);
            }
        }

        public void Dispose()
        {
            if (circleMesh != null)
            {
                api.Render.DeleteMesh(circleMesh);
                circleMesh = null;
            }
        }

        public double RenderOrder => 0.0;
        public int RenderRange => 1000;
    }
}
