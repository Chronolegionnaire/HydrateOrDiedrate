using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Hydration.Interfaces;
using HydrateOrDiedrate.Hydration.Packets;
using HydrateOrDiedrate.Thirst;
using HydrateOrDiedrate.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client;
using Vintagestory.GameContent;


namespace HydrateOrDiedrate.Hydration;

public class DrinkingInteractionModSystem : ModSystem
{
    private ICoreAPI api;

    private HydrateOrDiedrateModSystem HoD;

    private DrinkHudOverlayRenderer hudOverlayRenderer;

    IntersectionTester intersectionTester;

    public DefaultDrinkingInteractionProvider DefaultDrinkingInteractionProvider { get; private set; }

    private long? HandleDrinkingListnerId;
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

        ((IServerNetworkChannel)HoD.NetworkChannel)
            .RegisterMessageType<DrinkProgressPacket>()
            .SetMessageHandler<DrinkProgressPacket>(OnDrinkPacketReceivedFromClient);

        api.Event.PlayerDisconnect += OnPlayerDisconnect;
        HandleDrinkingListnerId = api.Event.RegisterGameTickListener(HandleDrinkingServer, 100);
    }

    private PlayerDrinkData GetOrCreateDrinkData(IPlayer player)
    {
        if(playerDrinkData.TryGetValue(player.PlayerUID, out var drinkData)) return drinkData;
        
        return playerDrinkData[player.PlayerUID] = new();
    }

    private void OnDrinkPacketReceivedFromClient(IServerPlayer fromPlayer, DrinkProgressPacket packet)
    {
        var drinkData = GetOrCreateDrinkData(fromPlayer);
        if (!packet.IsDrinking)
        {
            drinkData.StopDrinking();
            SendDrinkProgressToClient(fromPlayer, drinkData);
            return;
        }
        if(drinkData.IsDrinking) return;

        TryStartDrinking(fromPlayer, drinkData);
        SendDrinkProgressToClient(fromPlayer, drinkData);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        ((IClientNetworkChannel)HoD.NetworkChannel)
            .RegisterMessageType<DrinkProgressPacket>()
            .SetMessageHandler<DrinkProgressPacket>(OnDrinkPacketReceivedFromServer);

        hudOverlayRenderer = new DrinkHudOverlayRenderer(api);
        api.Event.RegisterRenderer(hudOverlayRenderer, EnumRenderStage.Ortho, "drinkoverlay");

        api.Input.RegisterHotKey("hydrateordiedrate:drinking", Lang.Get("hydrateordiedrate:drinking"), (GlKeys)242, ctrlPressed: true);

        HandleDrinkingListnerId = api.Event.RegisterGameTickListener(HandleDrinkingClient, 100);
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

    public bool IsLocalPlayerDrinking { get; private set; }
    private void OnDrinkPacketReceivedFromServer(DrinkProgressPacket packet)
    {
        IsLocalPlayerDrinking = packet.IsDrinking;
        if (hudOverlayRenderer is null) return;
        hudOverlayRenderer.ProcessDrinkProgress(packet.Progress, packet.IsDrinking, packet.IsDangerous);
    }

    private void HandleDrinkingServer(float dt)
    {
        if(api is not ICoreServerAPI sapi) return;
        foreach((var playerId, var drinkData) in playerDrinkData)
        {
            if(!drinkData.IsDrinking || sapi.World.PlayerByUid(playerId) is not IServerPlayer player) continue;

            try
            {
                TryContinueDrinking(player, drinkData);
                SendDrinkProgressToClient(player, drinkData);
            }
            catch(Exception ex)
            {
                Mod.Logger.Error("Error in player drinking interaction {0} ({1}): {2}", player.PlayerName, player.PlayerUID, ex);
               
                if(drinkData.IsDrinking) continue;

                drinkData.StopDrinking();
                SendDrinkProgressToClient(player, drinkData);
            }
        }
    }

    private void HandleDrinkingClient(float dt)
    {
        if(api is not ICoreClientAPI capi || capi.Input.GetHotKeyByCode("hydrateordiedrate:drinking") is not { } drinkingHotKey) return;

        var drinkingHotkeyPressed = IsPressed(drinkingHotKey.CurrentMapping);

        if (IsLocalPlayerDrinking)
        {
            if (!drinkingHotkeyPressed)
            {
                SendIsDrinkingToServer(false);
            }
            return;
        }

        if (!drinkingHotkeyPressed) return;

        var drinkData = new PlayerDrinkData();
        TryStartDrinking(capi.World.Player, drinkData);

        if (drinkData.IsDrinking)
        {
            SendIsDrinkingToServer(true);
        }
    }

    //Normal hotkey hooks don't always trigger when I would expect them too so it could get stuck if we don't check manually.
    private static bool IsPressed(KeyCombination mapping)
    {
        var modifiers = ScreenManager.KeyboardModifiers;
        if(mapping.Alt && !modifiers.AltPressed) return false;
        if(mapping.Shift && !modifiers.ShiftPressed) return false;
        if(mapping.Ctrl && !modifiers.CtrlPressed) return false;

        if (!IsPressed(mapping, mapping.KeyCode)) return false;
        if (mapping.SecondKeyCode is not null && !IsPressed(mapping, mapping.SecondKeyCode.Value)) return false;

        return true;
    }

    private static bool IsPressed(KeyCombination mapping, int keyCode)
    {
        if (mapping.IsMouseButton(keyCode))
        {
            return ScreenManager.MouseButtonState[keyCode - KeyCombination.MouseStart];
        }
        return ScreenManager.KeyboardKeyState[keyCode];
    }

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

    private void SendIsDrinkingToServer(bool isDrinking)
    {
        if(HoD.NetworkChannel is not IClientNetworkChannel clientChannel) return;

        clientChannel.SendPacket(new DrinkProgressPacket { IsDrinking = isDrinking });
    }

    private void TryContinueDrinking(IServerPlayer player, PlayerDrinkData drinkData)
    {
        if (IsHeadInWater(player))
        {
            drinkData.StopDrinking();
            return;
        }

        var blockSel = intersectionTester.GetFluidSelection(player, interactionDistance);

        if (blockSel?.Position is null || !blockSel.Block.ForFluidsLayer)
        {
            drinkData.StopDrinking();
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
    }

    private void TryStartDrinking(IPlayer player, PlayerDrinkData drinkData)
    {
        if (IsHeadInWater(player))
        {
            drinkData.StopDrinking();
            return;
        }

        var blockSel = intersectionTester.GetFluidSelection(player, interactionDistance);

        if (blockSel?.Position is null || !blockSel.Block.ForFluidsLayer)
        {
            drinkData.StopDrinking();
            return;
        }

        var world = api.World;
        if(drinkData.IsDrinking && blockSel.Position != drinkData.DrinkPos)
        {
            drinkData.StopDrinking();
            return;
        }

        var drinkingProvider = blockSel.Block.GetInterface<IDrinkingInteraction>(world, blockSel.Position) ?? DefaultDrinkingInteractionProvider;

        drinkingProvider.StartDrinking(world, blockSel, player, drinkData);
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

        if(HandleDrinkingListnerId.HasValue)
        {
            api.Event.UnregisterGameTickListener(HandleDrinkingListnerId.Value);
            HandleDrinkingListnerId = null;
        }
    }
}
