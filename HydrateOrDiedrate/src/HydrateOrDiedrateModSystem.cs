using System;
using HarmonyLib;
using HydrateOrDiedrate.Commands;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.encumbrance;
using HydrateOrDiedrate.Hot_Weather;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.Keg;
using HydrateOrDiedrate.patches;
using HydrateOrDiedrate.Wells.WellWater;
using HydrateOrDiedrate.Wells.Winch;
using HydrateOrDiedrate.XSkill;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using HydrateOrDiedrate.Config.Patching;
using HydrateOrDiedrate.Config.Patching.PatchTypes;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Util;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Newtonsoft.Json.Linq;
using HydrateOrDiedrate.Piping.HandPump;
using HydrateOrDiedrate.Piping.Networking;
using HydrateOrDiedrate.Piping.Pipe;
using HydrateOrDiedrate.Piping.ShutoffValve;
using Vintagestory.Common;

namespace HydrateOrDiedrate;

public class HydrateOrDiedrateModSystem : ModSystem
{
    public const string HarmonyID = "com.chronolegionnaire.hydrateordiedrate";
    public const string NetworkChannelID = "hydrateordiedrate";

    internal static ICoreServerAPI _serverApi { get; private set; }
    internal static ICoreClientAPI _clientApi { get; private set; }

    private HudElementThirstBar _thirstHud;
    private HudElementNutritionDeficitBar nutritionDeficitHud;
    private WaterInteractionHandler _waterInteractionHandler;
    private Harmony harmony;

    private RainHarvesterManager rainHarvesterManager;
    private DrinkHudOverlayRenderer hudOverlayRenderer;

    private long customHudListenerId;
    
    public const string PatchCategory_MarkDirtyThreshold = "HoD.MarkDirtyThreshold";
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        ConfigManager.EnsureModConfigLoaded(api);

        if (!Harmony.HasAnyPatches(HarmonyID))
        {
            harmony = new Harmony(HarmonyID);
            
            harmony.PatchAllUncategorized();
            TryCompatibilityPatch(harmony, api, "hardcorewater", "HydrateOrDiedrate.HardcoreWater");
            TryCompatibilityPatch(harmony, api, "aculinaryartillery", "HydrateOrDiedrate.ACulinaryArtillery");

            if (ModConfig.Instance.Advanced.IncreaseMarkDirtyThreshold)
            {
                harmony.PatchCategory(PatchCategory_MarkDirtyThreshold);
            }
        }
    }

    internal void TryCompatibilityPatch(Harmony harmony, ICoreAPI api, string modID, string category)
    {
        if(!api.ModLoader.IsModEnabled(modID)) return;
        try
        {
            harmony.PatchCategory(category);
        }
        catch (Exception ex)
        {
            Mod.Logger.Error("Failed to apply compatibility patches ({0}) for mod {1}: {2}",category, modID, ex);
        }
    }
    
    //NOTE: any higher then this and Gourmand will crash because it loads stuff rather early
    public override double ExecuteOrder() => 1.099;

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);

        if(api is not ICoreServerAPI serverApi) return; //This data is decided by the server and synced over to client automatically
        RecipeGenerator.RecipeGenerator.GenerateVariants(serverApi, Mod.Logger); //NOTE: has to happen here and not in `AssetsFinalize` because otherwise Gourmand will crash
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        if(api.Side != EnumAppSide.Server) return; //This data is decided by the server and synced over to client automatically
        try
        {
            WaterPatches.ApplyConfigSettings(api);
        }
        catch(Exception ex)
        {
            Mod.Logger.Error("Failed to apply config settings: {0}", ex);
        }
        EntityProperties playerEntity = api.World.GetEntityType(new AssetLocation("game", "player"));
        var HoDbehaviors = new List<JsonObject>(3);

        //Forcibly insert behaviors to ensure they are present //TODO most of these are only really needed for th server but some are on client as well for now for accessibility
        if (ModConfig.Instance.LiquidEncumbrance.Enabled) HoDbehaviors.Add(new(new JObject { ["code"] =  "HoD:liquidencumbrance" }));
        if (ModConfig.Instance.Thirst.Enabled) HoDbehaviors.Add(new(new JObject { ["code"] =  "HoD:thirst" }));
        if (ModConfig.Instance.HeatAndCooling.HarshHeat) HoDbehaviors.Add(new(new JObject { ["code"] =  "HoD:bodytemperaturehot" }));

        playerEntity.Server.BehaviorsAsJsonObj = [
            ..playerEntity.Server.BehaviorsAsJsonObj,
            ..HoDbehaviors
        ];
        
        playerEntity.Client.BehaviorsAsJsonObj = [
            ..playerEntity.Client.BehaviorsAsJsonObj,
            ..HoDbehaviors
        ];

        //TODO does this even do anything when HarshHeat is disabled?
        PatchCollection<CoolingPatch>.GetMerged(api, "HoD.AddCooling.json", CoolingPatch.GenerateDefaultPatchCollection()).ApplyPatches(api.World.Items);

        if (ModConfig.Instance.Thirst.Enabled)
        {
            PatchCollection<ItemHydrationPatch>.GetMerged(api, "HoD.AddItemHydration.json", ItemHydrationPatch.GenerateDefaultPatchCollection()).ApplyPatches(api.World.Items);
            
            PatchCollection<BlockHydrationPatch>.GetMerged(api, "HoD.AddBlockHydration.json", BlockHydrationPatch.GenerateDefaultPatchCollection()).ApplyPatches(api.World.Blocks);
        }

        foreach (var block in api.World.Blocks)
        {
            if (block is BlockLiquidContainerTopOpened || block is BlockBarrel || block is BlockGroundStorage)
            {
                EnsureRainHarvesterBehaviorPresent(block);
            }
        }

        Wells.Aquifer.AquiferManager.Initialize(api);
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityKeg"]       = "HoD:BlockEntityKeg";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityTun"]       = "HoD:BlockEntityTun";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityWellSpring"] = "HoD:BlockEntityWellSpring";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityWinch"]     = "HoD:BlockEntityWinch";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityHandPump"]  = "HoD:BlockEntityHandPump";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityHoDPipe"]   = "HoD:BlockEntityHoDPipe";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityShutoffValve"] = "HoD:BlockEntityShutoffValve";
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityWellWaterSentinel"] = "HoD:BlockEntityWellWaterSentinel";
        
        api.RegisterBlockClass("HoD:BlockKeg", typeof(BlockKeg));
        api.RegisterBlockEntityClass("HoD:BlockEntityKeg", typeof(BlockEntityKeg));
        api.RegisterItemClass("HoD:ItemKegTap", typeof(ItemKegTap));
        
        api.RegisterBlockClass("HoD:BlockTun", typeof(BlockTun));
        api.RegisterBlockEntityClass("HoD:BlockEntityTun", typeof(BlockEntityTun));
        
        api.RegisterCollectibleBehaviorClass("HoD:BehaviorPickaxeWellMode", typeof(BehaviorPickaxeWellMode));
        api.RegisterCollectibleBehaviorClass("HoD:BehaviorShovelWellMode", typeof(BehaviorShovelWellMode));

        api.RegisterBlockEntityClass("HoD:BlockEntityWellWaterSentinel", typeof(BlockEntityWellWaterSentinel));
        api.RegisterBlockBehaviorClass("HoD:BlockBehaviorWellWaterFinite", typeof(BlockBehaviorWellWaterFinite));
        api.RegisterBlockClass("HoD:BlockWellSpring", typeof(BlockWellSpring));
        api.RegisterBlockEntityClass("HoD:BlockEntityWellSpring", typeof(BlockEntityWellSpring));
        api.RegisterBlockClass("HoD:BlockHoDPipe", typeof(BlockPipe));
        api.RegisterBlockEntityClass("HoD:BlockEntityHoDPipe", typeof(BlockEntityPipe));
        api.RegisterBlockClass("HoD:BlockShutoffValve", typeof(BlockShutoffValve));
        api.RegisterBlockEntityClass("HoD:BlockEntityShutoffValve", typeof(BlockEntityShutoffValve));
        api.RegisterBlockClass("HoD:BlockHandPump", typeof(BlockHandPump));
        api.RegisterBlockEntityClass("HoD:BlockEntityHandPump", typeof(BlockEntityHandPump));
        api.RegisterBlockEntityBehaviorClass("HoD:HandPumpAnim", typeof(BEBehaviorHandPumpAnim));
        api.RegisterBlockClass("HoD:BlockWinch", typeof(BlockWinch));
        api.RegisterBlockEntityClass("HoD:BlockEntityWinch", typeof(BlockEntityWinch));

        api.ClassRegistry.RegisterParticlePropertyProvider(
            "HoD:PumpCubeParticles",
            typeof(Piping.HandPump.PumpCubeParticles)
        );
        
        if (ModConfig.Instance.LiquidEncumbrance.Enabled) api.RegisterEntityBehaviorClass("HoD:liquidencumbrance", typeof(EntityBehaviorLiquidEncumbrance));
        if (ModConfig.Instance.Thirst.Enabled) api.RegisterEntityBehaviorClass("HoD:thirst", typeof(EntityBehaviorThirst));
        if (ModConfig.Instance.HeatAndCooling.HarshHeat) api.RegisterEntityBehaviorClass("HoD:bodytemperaturehot", typeof(EntityBehaviorBodyTemperatureHot)); //TODO does this even do anything when thirst is disabled?

        _waterInteractionHandler = new WaterInteractionHandler(api);

        XLibSkills.Enabled = false;
        if (api.ModLoader.IsModEnabled("xlib") || api.ModLoader.IsModEnabled("xlibrabite"))
        {
            XLibSkills.Initialize(api);
            XLibSkills.Enabled = true;
        }

        api.RegisterBlockEntityBehaviorClass("RainHarvester", typeof(RegisterRainHarvester));

        if (api.ModLoader.IsModEnabled("smoothdigestion"))
        {
            EntityBehaviorSDHungerPatch.Apply(api);
        }
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _serverApi = api;
        base.StartServerSide(api);

        var serverChannel = api.Network.RegisterChannel(NetworkChannelID)
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<WellSpringBlockPacket>()
            .RegisterMessageType<PumpParticleBurstPacket>()
            .RegisterMessageType<PumpSfxPacket>()
            .RegisterMessageType<ValveToggleEventPacket>()
            .SetMessageHandler<WellSpringBlockPacket>(WellSpringBlockPacketReceived);
        
        _waterInteractionHandler.Initialize(serverChannel);
        rainHarvesterManager = new RainHarvesterManager(_serverApi);
        api.Event.PlayerDisconnect += _waterInteractionHandler.OnPlayerDisconnect;
        api.Event.RegisterGameTickListener(CheckPlayerInteraction, 100);

        ThirstCommands.Register(api);
        AquiferCommands.Register(api);
    }


    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;
        base.StartClientSide(api);

        api.Network.RegisterChannel(NetworkChannelID)
            .RegisterMessageType<DrinkProgressPacket>()
            .RegisterMessageType<WellSpringBlockPacket>()
            .RegisterMessageType<PumpParticleBurstPacket>()
            .RegisterMessageType<PumpSfxPacket>()
            .RegisterMessageType<ValveToggleEventPacket>()
            .SetMessageHandler<DrinkProgressPacket>(OnDrinkProgressReceived)
            .SetMessageHandler<PumpParticleBurstPacket>(msg => BlockEntityHandPump.PlayPumpParticleBurst(api, msg))
            .SetMessageHandler<PumpSfxPacket>(msg => BlockEntityHandPump.OnClientPumpSfx((ICoreClientAPI)api, msg))
            .SetMessageHandler<ValveToggleEventPacket>(pkt => ValveHandleRenderer.OnClientValveToggleEvent((ICoreClientAPI)api, pkt));

        hudOverlayRenderer = new DrinkHudOverlayRenderer(api);
        api.Event.RegisterRenderer(hudOverlayRenderer, EnumRenderStage.Ortho, "drinkoverlay");
        if (ModConfig.Instance.Thirst.Enabled)
        {
            customHudListenerId = api.Event.RegisterGameTickListener(CheckAndInitializeCustomHud, 20);
        }
        if(api.ModLoader.IsModEnabled("configlib")) ConfigLibCompatibility.Init(api);
    }

    public RainHarvesterManager GetRainHarvesterManager()
    {
        return rainHarvesterManager;
    }

    private void OnDrinkProgressReceived(DrinkProgressPacket msg)
    {
        if (hudOverlayRenderer is null) return;
        hudOverlayRenderer.ProcessDrinkProgress(msg.Progress, msg.IsDrinking, msg.IsDangerous);
    }


    //TODO: there should be a better way to do this, no?
    private void CheckAndInitializeCustomHud(float dt)
    {
        var vanillaHudStatbar = GetVanillaStatbarHud();

        if (vanillaHudStatbar != null && vanillaHudStatbar.IsOpened())
        {

            _thirstHud = new HudElementThirstBar(_clientApi);
            _clientApi.Event.RegisterGameTickListener(_thirstHud.OnGameTick, 1000);
            _clientApi.Gui.RegisterDialog(_thirstHud);

            nutritionDeficitHud = new HudElementNutritionDeficitBar(_clientApi);
            _clientApi.Event.RegisterGameTickListener(nutritionDeficitHud.OnGameTick, 1000);
            _clientApi.Gui.RegisterDialog(nutritionDeficitHud);

            _clientApi.Event.UnregisterGameTickListener(customHudListenerId);
        }
    }

    private HudStatbar GetVanillaStatbarHud()
    {
        foreach (var hud in _clientApi.Gui.OpenedGuis)
        {
            if (hud is HudStatbar statbar)
            {
                return statbar;
            }
        }

        return null;
    }

    public void CheckPlayerInteraction(float dt)
    {
        foreach (IServerPlayer player in _serverApi.World.AllOnlinePlayers)
        {
            if(player.ConnectionState != EnumClientState.Playing) continue;
            _waterInteractionHandler.CheckShiftRightClickBeforeInteractionForPlayer(dt, player);
        }
    }

    private static void EnsureRainHarvesterBehaviorPresent(Block block)
    {
        block.BlockEntityBehaviors ??= [];

        if (Array.Exists(block.BlockEntityBehaviors, b => b.Name == "RainHarvester")) return;
        
        block.BlockEntityBehaviors = block.BlockEntityBehaviors.Append(new BlockEntityBehaviorType
        {
            Name = "RainHarvester",
            properties = null
        });
    }

    private void WellSpringBlockPacketReceived(IServerPlayer sender, WellSpringBlockPacket packet)
    {
        if (_serverApi?.World is null) return;
        IBlockAccessor accessor = _serverApi.World.GetBlockAccessor(true, true, false);
        accessor.ExchangeBlock(packet.BlockId, packet.Position);
        accessor.SpawnBlockEntity("HoD:BlockEntityWellSpring", packet.Position, null);
    }

    public override void Dispose()
    {
        _thirstHud?.Dispose();
        nutritionDeficitHud?.Dispose();

        ConfigManager.UnloadModConfig();
        harmony?.UnpatchAll(HarmonyID);
        UnloadStatics();
        base.Dispose();
    }

    private static void UnloadStatics()
    {
        _serverApi = null;
        _clientApi = null;
        Wells.Aquifer.AquiferManager.Unload();
    }
}