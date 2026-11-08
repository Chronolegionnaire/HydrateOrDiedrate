using HydrateOrDiedrate.Commands;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Config.Patching;
using HydrateOrDiedrate.Config.Patching.PatchTypes;
using HydrateOrDiedrate.HUD;
using HydrateOrDiedrate.patches;
using HydrateOrDiedrate.Piping.HandPump;
using HydrateOrDiedrate.Piping.Networking;
using HydrateOrDiedrate.Piping.ShutoffValve;
using HydrateOrDiedrate.XSkill;
using InsanityLib.Generators.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

[assembly: AutoRegistryName("HoD:{name}")]
namespace HydrateOrDiedrate;

public partial class HydrateOrDiedrateModSystem : ModSystem
{
    public const string NetworkChannelID = "hydrateordiedrate";

    public INetworkChannel NetworkChannel { get; private set; }

    internal static ICoreServerAPI _serverApi { get; private set; }
    internal static ICoreClientAPI _clientApi { get; private set; }

    private HudElementThirstBar _thirstHud;
    private HudElementNutritionDeficitBar nutritionDeficitHud;

    private RainHarvesterManager rainHarvesterManager;

    private long customHudListenerId;
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);
        AutoSetup(api);
        NetworkChannel = api.Network.RegisterChannel(NetworkChannelID);
    }
    
    //NOTE: any higher then this and Gourmand will crash because it loads stuff rather early
    public override double ExecuteOrder() => 1.099;

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        AutoAssetsLoaded(api);

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
        PatchCollection<CoolingPatch>.GetMerged(api, "HoD.AddCooling.json").ApplyPatches(api.World.Items);

        if (ModConfig.Instance.Thirst.Enabled)
        {
            PatchCollection<HydrationPatch>.GetMerged(api, "HoD.AddItemHydration.json").ApplyPatches(api.World.Items);
            
            PatchCollection<HydrationPatch>.GetMerged(api, "HoD.AddBlockHydration.json").ApplyPatches(api.World.Blocks);
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
        ClassRegistry.legacyBlockEntityClassNames["BlockEntityWellWaterSentinel"] = "HoD:BlockEntityWellWaterSentinel";
        
        api.ClassRegistry.RegisterParticlePropertyProvider("HoD:PumpCubeParticles", typeof(PumpCubeParticles));

        XLibSkills.Enabled = false;
        if (api.ModLoader.Mods.Any(mod => mod.Info.ModID.StartsWith("xlib")))
        {
            try
            {
                XLibSkills.Initialize(api);
                XLibSkills.Enabled = true;
            }
            catch (Exception ex)
            {
                api.Logger.Warning($"Failed to initialize XLib skills compatibility: {ex}");
                XLibSkills.Enabled = false;
            }
        }
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _serverApi = api;
        base.StartServerSide(api);

        ((IServerNetworkChannel)NetworkChannel)
            .RegisterMessageType<PumpParticleBurstPacket>()
            .RegisterMessageType<PumpSfxPacket>()
            .RegisterMessageType<ValveToggleEventPacket>();
        
        rainHarvesterManager = new RainHarvesterManager(_serverApi);

        ThirstCommands.Register(api);
        AquiferCommands.Register(api);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;
        base.StartClientSide(api);

        ((IClientNetworkChannel)NetworkChannel)
            .RegisterMessageType<PumpParticleBurstPacket>()
            .RegisterMessageType<PumpSfxPacket>()
            .RegisterMessageType<ValveToggleEventPacket>()
            .SetMessageHandler<PumpParticleBurstPacket>(msg => BlockEntityHandPump.PlayPumpParticleBurst(api, msg))
            .SetMessageHandler<PumpSfxPacket>(msg => BlockEntityHandPump.OnClientPumpSfx(api, msg))
            .SetMessageHandler<ValveToggleEventPacket>(pkt => ValveHandleRenderer.OnClientValveToggleEvent(api, pkt));
        
        _thirstHud = new HudElementThirstBar(_clientApi);
        _clientApi.Gui.RegisterDialog(_thirstHud);

        if (ModConfig.Instance.Thirst.Enabled)
        {
            customHudListenerId = api.Event.RegisterGameTickListener(CheckAndInitializeCustomHud, 20);
        }
    }

    public RainHarvesterManager GetRainHarvesterManager()
    {
        return rainHarvesterManager;
    }

    //TODO: there should be a better way to do this, no?
    private void CheckAndInitializeCustomHud(float dt)
    {
        var vanillaHudStatbar = _clientApi?.Gui?.OpenedGuis?.OfType<HudStatbar>().FirstOrDefault();

        if (vanillaHudStatbar != null && vanillaHudStatbar.IsOpened())
        {
            nutritionDeficitHud = new HudElementNutritionDeficitBar(_clientApi);
            _clientApi.Event.RegisterGameTickListener(nutritionDeficitHud.OnGameTick, 1000);
            _clientApi.Gui.RegisterDialog(nutritionDeficitHud);

            _clientApi.Event.UnregisterGameTickListener(customHudListenerId);
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

    public override void Dispose()
    {
        _thirstHud?.Dispose();
        nutritionDeficitHud?.Dispose();

        UnloadStatics();
        AutoDispose();
        base.Dispose();
    }

    private static void UnloadStatics()
    {
        _serverApi = null;
        _clientApi = null;
        Wells.Aquifer.AquiferManager.Unload();
        CharacterExtraDialogsPatch.cachedElements = null;
    }
}