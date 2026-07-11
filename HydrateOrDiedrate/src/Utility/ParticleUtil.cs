using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Utility;

#nullable enable
public static class ParticleUtil
{
    public static SimpleParticleProperties CreateParticleProperties(int color, Vec3f minVelocity, Vec3f maxVelocity, string climateColorMap = null)
    {
        var particles = new SimpleParticleProperties(
            1, 1, color, new Vec3d(), new Vec3d(),
            minVelocity, maxVelocity, 0.1f, 0.1f, 0.5f, 1f, EnumParticleModel.Cube
        )
        {
            AddPos = new Vec3d(0.125 / 2, 2 / 16f, 0.125 / 2),
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f),
            AddQuantity = 1,
            ShouldDieInLiquid = true,
            ShouldSwimOnLiquid = true,
            GravityEffect = 1.5f,
            LifeLength = 2f
        };

        if (climateColorMap is not null)
        {
            particles.ClimateColorMap = climateColorMap;
        }

        return particles;
    }

    private static readonly SimpleParticleProperties _waterParticles = CreateParticleProperties(
        ColorUtil.WhiteArgb,
        new Vec3f(-0.25f, 0, -0.25f),
        new Vec3f(0.25f, 0.5f, 0.25f),
        "climateWaterTint"
    );
    
    private static readonly SimpleParticleProperties _whiteParticles = CreateParticleProperties(
        ColorUtil.ColorFromRgba(255, 255, 255, 128),
        new Vec3f(-0.1f, 0, -0.1f),
        new Vec3f(0.1f, 0.2f, 0.1f)
    );
    
    
    public static void SpawnWaterParticles(IWorldAccessor world, Vec3d pos, IPlayer? dualCallByPlayer = null)
    {
        _waterParticles.MinPos = new Vec3d(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
        _waterParticles.AddPos = new Vec3d(0.4, 0.0, 0.4);
        _waterParticles.GravityEffect = 1.5f;
        _waterParticles.MinVelocity = new Vec3f(0, 0.8f, 0);
        _waterParticles.AddVelocity = new Vec3f(0.2f, 0.8f, 0.2f);

        float colorModifier = (float)world.Rand.NextDouble() * 0.3f;
        _waterParticles.Color = ColorUtil.ColorFromRgba(
            185 + (int)(colorModifier * 70f),
            145 + (int)(colorModifier * 110f),
            50 + (int)(colorModifier * 205f),
            130 + (int)(colorModifier * 30f)
        );
        _waterParticles.AddQuantity = 10;
        _whiteParticles.MinPos = new Vec3d(pos.X - 0.2, pos.Y + 0.1, pos.Z - 0.2);
        _whiteParticles.AddPos = new Vec3d(0.4, 0.0, 0.4);
        _whiteParticles.GravityEffect = 1.5f;
        _whiteParticles.MinVelocity = new Vec3f(0, 0.8f, 0);
        _whiteParticles.AddVelocity = new Vec3f(0.2f, 0.8f, 0.2f);
        _whiteParticles.AddQuantity = 5;
        world.SpawnParticles(_waterParticles, dualCallByPlayer);
        world.SpawnParticles(_whiteParticles, dualCallByPlayer);
    }
    #nullable disable
}
