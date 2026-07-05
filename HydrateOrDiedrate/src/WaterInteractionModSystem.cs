using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Utility;
using HydrateOrDiedrate.Thirst;
using HydrateOrDiedrate.Wells;
using HydrateOrDiedrate.Wells.WellWater;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace HydrateOrDiedrate;

public class WaterInteractionModSystem : ModSystem
{
    private ICoreAPI api;

    private HydrateOrDiedrateModSystem HoD;

    private DrinkHudOverlayRenderer hudOverlayRenderer;

    IntersectionTester intersectionTester;

    private long? OnSlowGameTickListnerId;
    #nullable enable

    private BlockLiquidContainerBase? blockForDrinkingInteraction;

    private readonly Dictionary<string, PlayerDrinkData> playerDrinkData = [];
    
    private const double drinkDuration = 1000; //TODO config

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        this.api = api;
        HoD = api.ModLoader.GetModSystem<HydrateOrDiedrateModSystem>();
        intersectionTester = new IntersectionTester((IWorldIntersectionSupplier)api.World);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        HoD.NetworkChannel
            .RegisterMessageType<DrinkProgressPacket>();

        api.Event.PlayerDisconnect += OnPlayerDisconnect;
        OnSlowGameTickListnerId = api.Event.RegisterGameTickListener(OnSlowGameTick, 100);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        ((IClientNetworkChannel)HoD.NetworkChannel)
            .RegisterMessageType<DrinkProgressPacket>()
            .SetMessageHandler<DrinkProgressPacket>(OnDrinkProgressReceived);

        hudOverlayRenderer = new DrinkHudOverlayRenderer(api);
        api.Event.RegisterRenderer(hudOverlayRenderer, EnumRenderStage.Ortho, "drinkoverlay");
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);

        blockForDrinkingInteraction = api.World.GetBlock(new AssetLocation("game", "woodbucket")) as BlockLiquidContainerBase;
        if(blockForDrinkingInteraction is null) Mod.Logger.Error("BlockLiquidContainerBase used for drinking interaction not found. Drinking interaction may not work correctly.");
    }

    private void OnDrinkProgressReceived(DrinkProgressPacket packet)
    {
        if (hudOverlayRenderer is null) return;
        hudOverlayRenderer.ProcessDrinkProgress(packet.Progress, packet.IsDrinking, packet.IsDangerous);
    }

    private void StopDrinking(IServerPlayer player, PlayerDrinkData drinkData)
    {
        if (!drinkData.IsDrinking) return;

        drinkData.IsDrinking = false;
        drinkData.DrinkStartTime = 0;
        SendDrinkProgressToClient(player, 0f, false, false);
    }

    private void SendDrinkProgressToClient(IServerPlayer player, float progress, bool isDrinking, bool isDangerous)
    {
        if(HoD.NetworkChannel is not IServerNetworkChannel serverChannel) return;

        serverChannel.SendPacket(
            new DrinkProgressPacket
            {
                Progress = progress,
                IsDrinking = isDrinking,
                IsDangerous = isDangerous
            },
            player
        );
    }

    private void OnSlowGameTick(float dt)
    {
        foreach (IPlayer player in api.World.AllOnlinePlayers)
        {
            if(player is not IServerPlayer serverPlayer || serverPlayer.ConnectionState != EnumClientState.Playing) continue;
            
            try
            {
                CheckPlayerInteraction(serverPlayer);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error("Error in CheckPlayerInteraction for player {0} ({1}): {2}", player.PlayerName, player.PlayerUID, ex);
                
                if (playerDrinkData.TryGetValue(player.PlayerUID, out var dd)) StopDrinking(serverPlayer, dd);
            }
        }
    }

    //TODO more hooks

    private bool IsHeadInWater(IPlayer player)
    {
        var headPos = player.Entity.Pos.XYZ.Add(0, player.Entity.LocalEyePos.Y, 0);
        var headBlockPos = new BlockPos((int)headPos.X, (int)headPos.Y, (int)headPos.Z, (int)headPos.Y / 32768);
        var block = api.World.BlockAccessor.GetBlock(headBlockPos);
        return block.BlockMaterial == EnumBlockMaterial.Water;
    }

    public const float interactionDistance = 5f;

    private void CheckPlayerInteraction(IServerPlayer player)
    {
        if (!playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData)) drinkData = playerDrinkData[player.PlayerUID] = new();

        bool drinkingModifierKeyPressed = ModConfig.Instance.SprintToDrink ? player.Entity.Controls.Sprint : player.Entity.Controls.Sneak;
        if (!drinkingModifierKeyPressed || !player.Entity.Controls.RightMouseDown)
        {
            StopDrinking(player, drinkData);
            return;
        }


        var blockSel = intersectionTester.GetFluidSelection(player, interactionDistance);
        if (blockSel?.Position is null)
        {
            StopDrinking(player, drinkData);
            return;
        }

        var block = blockSel.Block;

        if (block.BlockMaterial == EnumBlockMaterial.Water && (!player.Entity.RightHandItemSlot.Empty || !player.Entity.LeftHandItemSlot.Empty))
        {
            player.SendIngameError("handsfull", Lang.Get("hydrateordiedrate:waterinteraction-handsfree"));
            StopDrinking(player, drinkData);
            return;
        }

        if (IsHeadInWater(player))
        {
            StopDrinking(player, drinkData);
            return;
        }

        float hydrationValue = HydrationManager.GetBlockHydration(api, block);
        if (hydrationValue == 0)
        {
            StopDrinking(player, drinkData);
            return;
        }

        long currentTime = api.World.ElapsedMilliseconds;
        if (!drinkData.IsDrinking)
        {
            drinkData.IsDrinking = true;
            drinkData.DrinkStartTime = currentTime;
            bool isDangerous = hydrationValue < 0 || HydrationManager.IsBoiling(api, block);
            SendDrinkProgressToClient(player, 0f, true, isDangerous);
        }
        else HandleDrinkingStep(player, blockSel, currentTime, block, drinkData);
    }
    
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "tryEatStop")]
    internal static extern void TryEatStop(BlockLiquidContainerBase instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity);


    private void HandleDrinkingStep(IServerPlayer player, BlockSelection blockSel, long currentTime, Block block, PlayerDrinkData drinkData)
    {
        if (block is null)
        {
            StopDrinking(player, drinkData);
            return;
        }

        float progress = (float)(currentTime - drinkData.DrinkStartTime) / (float)drinkDuration;
        progress = Math.Min(1f, progress);

        float hydrationValue = HydrationManager.GetBlockHydration(api, block);
        bool isBoiling = HydrationManager.IsBoiling(api, block);

        bool isDangerous = hydrationValue < 0 || isBoiling || HydrationManager.GetHealing(api, block) < 0;

        if (progress < 1f) return;

        SendDrinkProgressToClient(player, 1f, false, isDangerous);

        var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();

        if (thirstBehavior == null || thirstBehavior.CurrentThirst >= thirstBehavior.MaxThirst)
        {
            player.SendIngameError("fullhydration",
            Lang.Get("hydrateordiedrate:waterinteraction-fullhydration"));
            StopDrinking(player, drinkData);
            return;
        }

        if(blockForDrinkingInteraction is null) return;
        
        var liquidStack = block.GetLiquidFromBlock(api);
        if (liquidStack is null) return;

        float availableLiters = 1f;
        BlockEntityWellSpring? spring = null;

        if(block.HasBehavior<BlockBehaviorWellWaterFinite>())
        {
            spring = WellBlockUtils.FindGoverningSpring(api, block, blockSel.Position);
            if(spring is null)
            {
                StopDrinking(player, drinkData);
                return;
            }

            availableLiters = spring.totalLiters;
        }
        
        availableLiters = Math.Min(blockForDrinkingInteraction.CapacityLitres, availableLiters);

        //mimick normal drinking interaction
        var drinkStack = new ItemStack(blockForDrinkingInteraction);
        blockForDrinkingInteraction.SetContent(drinkStack,  liquidStack);
        blockForDrinkingInteraction.SetCurrentLitres(drinkStack, availableLiters);
        var dummy = new DummySlot(drinkStack);
        TryEatStop(blockForDrinkingInteraction, 1f, dummy, player.Entity);

        if(spring is not null)
        {
            var litersUsed = (int)Math.Ceiling(availableLiters - blockForDrinkingInteraction.GetCurrentLitres(drinkStack));
            spring.TryChangeVolume(-litersUsed);
        }

        //TODO boiling is not getting set on boiling water -_-
        //TODO use temperature system instead of only on liquid block (since otherwise you can just grab the liquid and consume it anyway)
        if (isBoiling) ApplyHeatDamage(player, ModConfig.Instance.Thirst.BoilingWaterDamage);

        api.World.PlaySoundAt(new AssetLocation("sounds/effect/water-pour"), blockSel.HitPosition.X, blockSel.HitPosition.Y, blockSel.HitPosition.Z, null, true, 32f, 1f);
        ParticleUtil.SpawnWaterParticles(api, blockSel.HitPosition);
        if (player.Entity.Controls.RightMouseDown)
        {
            drinkData.DrinkStartTime = currentTime;
            SendDrinkProgressToClient(player, 0f, true, isDangerous);
        }
        else
        {
            StopDrinking(player, drinkData);
        }
    }

    private static void ApplyHeatDamage(IServerPlayer player, float boilingWaterDamage)
    {
        if(boilingWaterDamage <= 0) return;
        
        player.Entity.ReceiveDamage(new DamageSource
        {
            Source = EnumDamageSource.Internal,
            Type = EnumDamageType.Heat
        }, boilingWaterDamage);
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        playerDrinkData.Remove(player.PlayerUID);
    }

    public override void Dispose()
    {
        base.Dispose();
        playerDrinkData.Clear();

        if(api is ICoreServerAPI sapi)
        {
            sapi.Event.PlayerDisconnect -= OnPlayerDisconnect;
        }
        else if (api is ICoreClientAPI capi)
        {
            capi.Event.UnregisterRenderer(hudOverlayRenderer, EnumRenderStage.Ortho);
        }

        if(OnSlowGameTickListnerId.HasValue)
        {
            api.Event.UnregisterGameTickListener(OnSlowGameTickListnerId.Value);
            OnSlowGameTickListnerId = null;
        }
    }
}
