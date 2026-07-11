using HydrateOrDiedrate.Hydration.Interfaces;
using HydrateOrDiedrate.Thirst;
using HydrateOrDiedrate.Utility;
using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Hydration;

#nullable enable
public class DefaultDrinkingInteractionProvider(BlockLiquidContainerBase interactionBlock) : IDrinkingInteraction
{
    public void StartDrinking(IWorldAccessor world, BlockSelection blockSel, IServerPlayer player, PlayerDrinkData drinkData)
    {
        var liquidStack = blockSel.Block.GetLiquidForDrinking(world, blockSel.Position);
        if (liquidStack is null) return;

        var hydration = liquidStack.GetHydration();
        if (hydration == 0f) return;

        if (!ValidatePlayer(player)) return;

        bool isDangerous = hydration < 0f || HydrationManager.IsBoiling(world.Api, liquidStack.Collectible);
        if (!isDangerous)
        {
            var drinkStack = new ItemStack(interactionBlock);
            interactionBlock.SetContent(drinkStack, liquidStack);
            var contentProps = interactionBlock.GetContentProps(drinkStack);

            isDangerous = contentProps?.NutritionPropsPerLitre is { Health: < 0 };
        }

        drinkData.StartDrinking(world, blockSel.Position, isDangerous);
    }

    public void ContinueDrinking(IWorldAccessor world, BlockSelection blockSel, IServerPlayer player, PlayerDrinkData drinkData)
    {
        if (!ValidatePlayer(player))
        {
            drinkData.StopDrinking();
        }
    }

    public static bool ValidatePlayer(IServerPlayer player)
    {
        if (!player.Entity.RightHandItemSlot.Empty || !player.Entity.LeftHandItemSlot.Empty)
        {
            player.SendIngameError("handsfull", Lang.Get("hydrateordiedrate:waterinteraction-handsfree"));
            return false;
        }

        var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
        if (thirstBehavior is null || thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
        {
            player.SendIngameError("fullhydration", Lang.Get("hydrateordiedrate:waterinteraction-fullhydration"));
            return false;
        }

        return true;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "tryEatStop")]
    internal static extern void TryEatStop(BlockLiquidContainerBase instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity);

    public void FinishDrinking(IWorldAccessor world, BlockSelection blockSel, IServerPlayer player, PlayerDrinkData drinkData)
    {
        var liquidStack = blockSel.Block.GetLiquidForDrinking(world, blockSel.Position);
        if(liquidStack is null) return;

        var drinkStack = new ItemStack(interactionBlock);
        var containableProps = BlockLiquidContainerBase.GetContainableProps(liquidStack);
        if(containableProps is null) return; //Should never happen, but just in case

        var liquidSource = HydrationManager.GetLiquidSourceForDrinking(blockSel.Block, world, blockSel.Position);
        
        if(liquidSource is not null)
        {
            liquidStack.StackSize = (int)Math.Min(containableProps.ItemsPerLitre, liquidStack.StackSize);
            liquidStack = liquidSource.TryTakeContent(blockSel.Position, liquidStack.StackSize);
        }
        else liquidStack.StackSize = (int)containableProps.ItemsPerLitre;

        interactionBlock.SetContent(drinkStack,  liquidStack);

        var dummy = new DummySlot(drinkStack);
        TryEatStop(interactionBlock, 1f, dummy, player.Entity);

        world.PlaySoundAt(new AssetLocation("sounds/effect/water-pour"), blockSel.HitPosition.X, blockSel.HitPosition.Y, blockSel.HitPosition.Z, null, true, 32f, 1f);
        ParticleUtil.SpawnWaterParticles(world, blockSel.HitPosition);
    }
}
