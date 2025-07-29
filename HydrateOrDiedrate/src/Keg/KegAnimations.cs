using System.Collections.Generic;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Keg;

public static class KegAnimations
{
    internal static void StartAxeAnimation(IAnimationManager entityAnimManager)
    {
        if (entityAnimManager is null || entityAnimManager.IsAnimationActive("axechop")) return;

        entityAnimManager.StartAnimation(new AnimationMetaData
        {
            Animation = "axeready",
            Code = "axeready"
        });

        entityAnimManager.StartAnimation(new AnimationMetaData
        {
            Animation = "axechop",
            Code = "axechop",
            BlendMode = EnumAnimationBlendMode.AddAverage,
            AnimationSpeed = 1.65f,
            HoldEyePosAfterEasein = 0.3f,
            EaseInSpeed = 500f,
            EaseOutSpeed = 500f,
            Weight = 25f,
            ElementWeight = new Dictionary<string, float>
                {
                    { "UpperArmr", 20.0f },
                    { "LowerArmr", 20.0f },
                    { "UpperArml", 20.0f },
                    { "LowerArml", 20.0f },
                    { "UpperTorso", 20.0f },
                    { "ItemAnchor", 20.0f }
                },
            ElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>
                {
                    { "UpperArmr", EnumAnimationBlendMode.Add },
                    { "LowerArmr", EnumAnimationBlendMode.Add },
                    { "UpperArml", EnumAnimationBlendMode.Add },
                    { "LowerArml", EnumAnimationBlendMode.Add },
                    { "UpperTorso", EnumAnimationBlendMode.Add },
                    { "ItemAnchor", EnumAnimationBlendMode.Add }
                }
        });
    }

    internal static void StartTappingAnimation(IAnimationManager entityAnimManager)
    {
        if (entityAnimManager is null) return;
        StopTappingAnimation(entityAnimManager);

        entityAnimManager.StartAnimation(new AnimationMetaData()
        {
            Animation = "chiselready",
            Code = "chiselready"
        });
    }

    internal static void StartIdleAnimation(IAnimationManager entityAnimManager) => entityAnimManager.StartAnimation(new AnimationMetaData
    {
        Animation = "idle1",
        Code = "idle",
        BlendMode = EnumAnimationBlendMode.Add,
    });

    internal static void StopAxeAndTappingAnimation(IAnimationManager entityAnimManager)
    {
        if(entityAnimManager is null) return;

        if (entityAnimManager.IsAnimationActive("axechop") || entityAnimManager.IsAnimationActive("chiselready"))
        {
            StopAllAnimations(entityAnimManager);
        }
        else StartIdleAnimation(entityAnimManager);
    }

    internal static void StopTappingAnimation(IAnimationManager entityAnimManager)
    {
        if(entityAnimManager is null) return;

        if (entityAnimManager.IsAnimationActive("chiselready"))
        {
            StopAllAnimations(entityAnimManager);
        }
        else StartIdleAnimation(entityAnimManager);
    }

    internal static void StopAxeAnimation(IAnimationManager entityAnimManager)
    {
        if(entityAnimManager is null) return;

        if (entityAnimManager.IsAnimationActive("axechop"))
        {
            StopAllAnimations(entityAnimManager);
        }
        else StartIdleAnimation(entityAnimManager);
    }

    internal static void StopAllAnimations(IAnimationManager entityAnimManager)
    {
        if(entityAnimManager?.ActiveAnimationsByAnimCode is null) return;

        foreach(var key in entityAnimManager.ActiveAnimationsByAnimCode.Keys)
        {
            entityAnimManager.StopAnimation(key);
        }

        StartIdleAnimation(entityAnimManager);
    }

    internal static void PlayChoppingSound(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) => 
        world.PlaySoundAt(new AssetLocation("game", "sounds/block/chop2"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 32f, 1f);

    internal static void PlayTappingSound(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) => 
        world.PlaySoundAt(new AssetLocation("game", "sounds/block/barrel"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 32f, 1f);
}
