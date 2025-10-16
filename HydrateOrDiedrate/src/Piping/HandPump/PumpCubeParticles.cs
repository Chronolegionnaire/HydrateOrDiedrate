using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class PumpCubeParticles : StackCubeParticles
    {
        private float? fixedLifeLength;
        private (float min, float max)? lifeRange;
        private float? gravityOverride;
        private int? fixedRgbaColor;

        private bool useDirectional;
        private float lateralJitter = 0f;
        private float axialJitter   = 0f;

        public PumpCubeParticles() : base() { }

        public PumpCubeParticles(Vec3d collisionPos, ItemStack stack, float radius, int quantity, float scale, Vec3f velocity = null)
            : base(collisionPos, stack, radius, quantity, scale, velocity) { }

        public override float LifeLength
        {
            get
            {
                if (fixedLifeLength.HasValue) return fixedLifeLength.Value;
                if (lifeRange.HasValue)
                {
                    var (min, max) = lifeRange.Value;
                    var t = (float)rand.NextDouble();
                    return min + t * (max - min);
                }
                return base.LifeLength;
            }
        }

        public override float GravityEffect => gravityOverride ?? base.GravityEffect;
        public override int GetRgbaColor(ICoreClientAPI capi) => fixedRgbaColor ?? base.GetRgbaColor(capi);
        public override Vec3f GetVelocity(Vec3d pos)
        {
            if (!useDirectional || velocity == null) return base.GetVelocity(pos);
            var dir = new Vec3f(velocity.X, velocity.Y, velocity.Z);
            float speed = GameMath.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
            if (speed <= 1e-6f) return new Vec3f(0, 0, 0);
            dir.X /= speed; dir.Y /= speed; dir.Z /= speed;
            float rx = (float)(rand.NextDouble() * 2 - 1);
            float ry = (float)(rand.NextDouble() * 2 - 1);
            float rz = (float)(rand.NextDouble() * 2 - 1);
            var jitter = new Vec3f(rx, ry, rz);
            float dot = jitter.X * dir.X + jitter.Y * dir.Y + jitter.Z * dir.Z;
            jitter.X -= dot * dir.X; jitter.Y -= dot * dir.Y; jitter.Z -= dot * dir.Z;
            float jl = GameMath.Sqrt(jitter.X * jitter.X + jitter.Y * jitter.Y + jitter.Z * jitter.Z);
            if (jl > 1e-6f) { jitter.X *= lateralJitter / jl; jitter.Y *= lateralJitter / jl; jitter.Z *= lateralJitter / jl; }
            else { jitter.Set(0, 0, 0); }
            float aj = (float)(rand.NextDouble() * 2 - 1) * axialJitter;

            return new Vec3f(
                dir.X * (speed + aj) + jitter.X,
                dir.Y * (speed + aj) + jitter.Y,
                dir.Z * (speed + aj) + jitter.Z
            );
        }
        public PumpCubeParticles SetLife(float lifeSeconds) { fixedLifeLength = lifeSeconds; lifeRange = null; return this; }
        public PumpCubeParticles SetLifeRange(float minSeconds, float maxSeconds) { lifeRange = (minSeconds, maxSeconds); fixedLifeLength = null; return this; }
        public PumpCubeParticles ClearLifeOverride() { fixedLifeLength = null; lifeRange = null; return this; }
        public PumpCubeParticles SetGravity(float gravity) { gravityOverride = gravity; return this; }
        public PumpCubeParticles UseDirectional(float lateralJitter = 0.03f, float axialJitter = 0.01f)
        {
            this.useDirectional = true;
            this.lateralJitter = lateralJitter;
            this.axialJitter = axialJitter;
            return this;
        }
    }
}
