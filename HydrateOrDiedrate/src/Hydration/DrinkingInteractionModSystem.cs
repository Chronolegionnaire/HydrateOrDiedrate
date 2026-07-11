using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Hydration.Interfaces;
using HydrateOrDiedrate.Thirst;
using HydrateOrDiedrate.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace HydrateOrDiedrate.Hydration;

public class DrinkingInteractionModSystem : ModSystem
{
    private ICoreAPI api;

    private HydrateOrDiedrateModSystem HoD;

    private DrinkHudOverlayRenderer hudOverlayRenderer;

    IntersectionTester intersectionTester;

    public DefaultDrinkingInteractionProvider DefaultDrinkingInteractionProvider { get; private set; }

    private long? OnSlowGameTickListnerId;
    #nullable enable

    private readonly Dictionary<string, PlayerDrinkData> playerDrinkData = [];

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

        if (api.World.GetBlock(new AssetLocation("game", "woodbucket")) is not BlockLiquidContainerBase blockForDrinkingInteraction)
        {
            Mod.Logger.Error("Standard BlockLiquidContainerBase used for drinking interaction not found, trying fallback... (Drinking interaction may not work correctly)");
            blockForDrinkingInteraction = api.World.Blocks.OfType<BlockLiquidContainerBase>().First(block => block.CanDrinkFrom);
        }

        DefaultDrinkingInteractionProvider = new DefaultDrinkingInteractionProvider(blockForDrinkingInteraction);
    }

    private void OnDrinkProgressReceived(DrinkProgressPacket packet)
    {
        if (hudOverlayRenderer is null) return;
        hudOverlayRenderer.ProcessDrinkProgress(packet.Progress, packet.IsDrinking, packet.IsDangerous);
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
                
                if (playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData))
                {
                    if(drinkData.IsDrinking) continue;

                    drinkData.StopDrinking();
                    SendDrinkProgressToClient(serverPlayer, drinkData);
                }
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

    private void SendDrinkProgressToClient(IServerPlayer player, PlayerDrinkData drinkData)
    {
        if(HoD.NetworkChannel is not IServerNetworkChannel serverChannel) return;

        serverChannel.SendPacket(
            new DrinkProgressPacket
            {
                Progress = drinkData.GetDrinkProgress(api.World),
                IsDrinking = drinkData.IsDrinking,
                IsDangerous = drinkData.IsDangerous
            },
            player
        );
    }

    private void CheckPlayerInteraction(IServerPlayer player)
    {
        if (!playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData)) drinkData = playerDrinkData[player.PlayerUID] = new();

        bool drinkingModifierKeyPressed = ModConfig.Instance.SprintToDrink ? player.Entity.Controls.Sprint : player.Entity.Controls.Sneak;
        if (!drinkingModifierKeyPressed || !player.Entity.Controls.RightMouseDown || IsHeadInWater(player))
        {
            if (drinkData.IsDrinking)
            {
                drinkData.StopDrinking();
                SendDrinkProgressToClient(player, drinkData);
            }
            return;
        }

        var blockSel = intersectionTester.GetFluidSelection(player, interactionDistance);

        if (blockSel?.Position is null || !blockSel.Block.ForFluidsLayer)
        {
            if (drinkData.IsDrinking)
            {
                drinkData.StopDrinking();
                SendDrinkProgressToClient(player, drinkData);
            }
            return;
        }

        var world = api.World;
        if(drinkData.IsDrinking && blockSel.Position != drinkData.DrinkPos) drinkData.StopDrinking();

        var drinkingProvider = blockSel.Block.GetInterface<IDrinkingInteraction>(world, blockSel.Position) ?? DefaultDrinkingInteractionProvider;

        if (!drinkData.IsDrinking)
        {
            drinkingProvider.StartDrinking(world, blockSel, player, drinkData);
            if(!drinkData.IsDrinking) return;
        }
        else drinkingProvider.ContinueDrinking(world, blockSel, player, drinkData);

        var progress = drinkData.GetDrinkProgress(world);
        if (progress >= 1f)
        {
            drinkingProvider.FinishDrinking(world, blockSel, player, drinkData);
            drinkData.StopDrinking();
        }

        SendDrinkProgressToClient(player, drinkData);
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
