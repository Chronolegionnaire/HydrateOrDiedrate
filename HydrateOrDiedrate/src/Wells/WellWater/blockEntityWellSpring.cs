using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Wells.Aquifer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Wells.WellWater;

public partial class BlockEntityWellSpring : BlockEntity, ITexPositionSource
{
    private const int updateIntervalMs = 500;
    private const int reconcileIntervalMs = 5000;
    public const float LitersPerFullBlock = 70f;
    
    private float itemsPerLiter = 0;

    public Block WaterBlock { get; private set; }

    public CollectibleObject WaterItem { get; private set; }

    public int WellShaftHeight { get; private set; }

    private bool TryUpdateWaterBlock(IWorldAccessor world, AssetLocation location) => WaterBlock?.Code != location && TryUpdateWaterBlock(world, world.GetBlock(location));

    private bool TryUpdateWaterBlock(IWorldAccessor world, int id) => WaterBlock?.Id != id && TryUpdateWaterBlock(world, world.GetBlock(id));

    private bool TryUpdateWaterBlock(IWorldAccessor world, Block block)
    {
        if(block is null) return false;

        var item =  block.GetWhenFilledStack(world)?.Collectible;
        if(item is null) return false;
        WaterBlock = block;
        WaterItem = item;
        var props = HydrationManager.GetProps(world, item);
        itemsPerLiter = props.ItemsPerLitre;

        return true;
    }

    public bool TryEnsureWaterVariant(string variant, string value, bool triggerSync = false)
    {
        if(!WaterBlock.Variant.TryGetValue(variant, out var currentValue)) return false;
        if(currentValue == value) return true;

        var result = TryUpdateWaterBlock(Api.World, WaterBlock.CodeWithVariant(variant, value));
        if (result)
        {
            if(triggerSync) SyncWaterColumn();
            MarkDirty();
        }

        return result;
    }

    public float TotalLiters { get; private set; } = 0;

    public bool IsFresh => WaterBlock is null || WaterBlock.Variant["type"] == "fresh";

    /// <summary>
    /// True when a sync is supposed to happen but hasn't yet (most likely because neighboring chunks weren't loaded yet).
    /// </summary>
    private bool SyncPending = false;

    public float TryChangeVolume(float change, bool triggerSync = true)
    {
        if (change == 0) return 0;

        var capacity = CapacityLitres;
        TotalLiters = GameMath.Clamp(TotalLiters, 0, capacity);
        var clamped = GameMath.Clamp(TotalLiters + change, 0, capacity);

        if(clamped < 0.005) clamped = 0;

        var applied = clamped - TotalLiters;
        TotalLiters = clamped;

        if (triggerSync)
        {
            SyncWaterColumn();
            MarkDirty();
        }

        if(!IsShallow && TotalLiters <= 0f)
        {
            TryEnsureWaterVariant("pollution", "clean");
        }

        return applied;
    }
    
    private static int HeightFromLiters(float vol) =>Math.Min(7, (int)Math.Ceiling(vol / 10f));

    private static float LitersFromHeight(int height) => Math.Min(LitersPerFullBlock, height * 10f);

    public Block OriginBlock
    {
        get;
        set
        {
            if (field == value) return;
            IsShallow = value.IsSoil();

            if (IsShallow)
            {
                if (value.Code.Path.StartsWith("forestfloor-") && value.Drops is not null)
                {
                    var newValue = value.Drops.FirstOrDefault(block => BlockUtils.IsSoil(block.ResolvedItemstack?.Block));
                    if(newValue is not null)
                    {
                        value = newValue.ResolvedItemstack.Block;
                    }
                }
                else if(value.Variant.TryGetValue("grasscoverage", out var variant) && variant != "none")
                {
                    var withoutGrass = Api.World.GetBlock(value.CodeWithVariant("grasscoverage", "none"));
                    if(withoutGrass is not null) value = withoutGrass;
                }
            }

            field = value;
            UpdateTextureSources();
            MarkDirty(true);
        }
    }

    private void UpdateTextureSources()
    {
        if(Api is not ICoreClientAPI capi) return;

        textureSources = OriginBlock is null ? null : [
            capi.Tesselator.GetTextureSource(OriginBlock, returnNullWhenMissing: true),
            capi.Tesselator.GetTextureSource(Block, returnNullWhenMissing: true)
        ];
    }
    
    private ITexPositionSource[] textureSources = [];
    public Size2i AtlasSize => ((ICoreClientAPI)Api).BlockTextureAtlas.Size;

    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            var unknown = ((ICoreClientAPI)Api).BlockTextureAtlas.UnknownTexturePosition;
            TextureAtlasPosition result;

            for (int i = 0; i < textureSources.Length; i++)
            {
                result = textureSources[i][textureCode];
                if(result is not null && result != unknown) return result;
                
                if(textureCode == "all")
                {
                    result = textureSources[i]["north"]; //HACK: base game annoyingly empties the 'all' identifier
                    if(result is not null && result != unknown) return result;
                }
            }
            
            for (int i = 0; i < textureSources.Length - 1; i++)
            {
                result = textureSources[i]["all"];
                if(result is not null&& result != unknown) return result;
            }

            return unknown;
        }
    }

    public bool IsShallow { get; private set; }

    private double LastInGameDay = -1.0;
    
    private const double AquiferRatingToLitersOutputRatio = 0.5;
    
    public double LastDailyLiters 
    { 
        get; 
        private set
        {
            if(value != field)
            {
                
                MarkDirty();
                field = value;
            }
        }
    }

    private double accumulatedWater = 0.0;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if ((int)LastInGameDay == -1) LastInGameDay = api.World.Calendar.TotalDays;

        UpdateTextureSources();
        if (api.Side != EnumAppSide.Server) return;

        AquiferManager.AddWellSpringToChunk(api.World, Pos);
        RegisterGameTickListener(OnServerTick, updateIntervalMs);
        RegisterGameTickListener(OnPeriodicShaftCheck, 30000);

        if(WaterBlock is null)
        {
            var location = Block.Attributes["WellWaterBlock"].AsObject<AssetLocation>(null, Block.Code.Domain) 
                ?? new AssetLocation(Constants.ModID, "wellwater-fresh-clean-natural-still-7");

            TryUpdateWaterBlock(api.World, location);
        }

        api.Event.EnqueueMainThreadTask(ReconcileStoredVolumeWithWorld, "well-spring-reconcile");
        RegisterGameTickListener(_ => ReconcileStoredVolumeWithWorld(), reconcileIntervalMs);
        api.Event.EnqueueMainThreadTask(
            () => OriginBlock ??= api.World.FindMostLikelyOriginBlockFromNeighbors(Pos) ?? api.World.GetBlock(new AssetLocation("game", "rock-granite")),
            "HoD:WellSpringEnsureOriginSet"
        );
        api.Event.EnqueueMainThreadTask(() =>
        {
            OnPeriodicShaftCheck(0);
            HandleWell(0);
        }, "well-spring-init");
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        if(base.OnTesselation(mesher, tessThreadTesselator)) return true;

        var meshData = GetMesh();
        if(meshData is null) return false;
        mesher.AddMeshData(meshData);
    
        return true;
    }

    public MeshData GetMesh()
    {
        if(Api is not ICoreClientAPI capi || OriginBlock is null) return null;
        if(!capi.ObjectCache.TryGetValue("wellspringmeshes", out var obj) || obj is not Dictionary<AssetLocation, MeshData> cachedMeshes)
        {
            capi.ObjectCache["wellspringmeshes"] = cachedMeshes = [];
        }
        if(cachedMeshes.TryGetValue(OriginBlock.Code, out var result)) return result;

        var shape = Shape.TryGet(Api, new AssetLocation(Constants.ModID, "shapes/block/wellspring.json"));
        capi.Tesselator.TesselateShape("wellspirng", shape, out result, this, new Vec3f(Block.Shape.rotateX, Block.Shape.rotateY, Block.Shape.rotateZ), 0, 0, 0, null, null);
        cachedMeshes[OriginBlock.Code] = result;
        return result;
    }

    public override void OnBlockRemoved()
    {
        if (Api.Side == EnumAppSide.Server)
        {
            var posCopy = Pos.Copy();
            AquiferManager.RemoveWellSpringFromChunk(Api.World, posCopy);
            CleanupWaterColumn();
        }

        base.OnBlockRemoved();
    }

    private void CleanupWaterColumn()
    {
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();
        for (int i = 0; i < 128; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);

            if (!IsOurWellWater(fluid)) break;
            ba.SetFluid(0, pos);
            ba.TriggerNeighbourBlockUpdate(pos);
        }
    }

    private bool HandleShallowWell(double elapsedDays)
    {
        bool changed = false;
        var (nearbySalty, nearbyFresh) = Api.World.BlockAccessor.CheckForNearbyGameWater(Pos);
        if (!nearbyFresh && !nearbySalty)
        {
            LastDailyLiters = 0;
            return false;
        }

        var oldWaterBlock = WaterBlock;
        TryEnsureWaterVariant("pollution", "muddy");
        TryEnsureWaterVariant("type", nearbyFresh && !nearbySalty ? "fresh" : "salt");

        changed |= oldWaterBlock != WaterBlock;

        LastDailyLiters = ModConfig.Instance.GroundWater.ShallowWellLitersPerDay * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        accumulatedWater += LastDailyLiters * elapsedDays;

        return HandleAccumelatedVolume() || changed;
    }

    private bool HandleAquiferWell(double elapsedDays)
    {
        bool changed = false;
        var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);

        var aquifer = AquiferManager.GetAquiferChunkData(chunk, Api.Logger)?.Data;
        var wellsprings = AquiferManager.GetWellspringsInChunk(chunk);
        if (aquifer is not { AquiferRating: not 0 } || wellsprings is not { Count: not 0 }) return false;

        double remainingRating = (double)aquifer.AquiferRating / wellsprings.Count;
        if (wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos)) is null) return false;

        var oldWaterBlock = WaterBlock;
        TryEnsureWaterVariant("type", !aquifer.IsSalty ? "fresh" : "salt");

        changed |= oldWaterBlock != WaterBlock;

        LastDailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio) * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        accumulatedWater += LastDailyLiters * elapsedDays;

        return HandleAccumelatedVolume() || changed;
    }

    private bool HandleAccumelatedVolume()
    {
        if (accumulatedWater >= 1.0)
        {
            
            float applied = TryChangeVolume((float)accumulatedWater, triggerSync: false);
            accumulatedWater = 0f;
            if (applied != 0f) return true;
        }
        return false;
    }

    private bool HandleWell(double elapsedDays) => IsShallow ? HandleShallowWell(elapsedDays) : HandleAquiferWell(elapsedDays);

    private void OnServerTick(float dt)
    {
        double currentInGameDays = Api.World.Calendar.TotalDays;
        bool changed = RunContaminationChecks();
        double elapsedDays = currentInGameDays - LastInGameDay;
        if (elapsedDays > 0.05)
        {
            LastInGameDay = currentInGameDays;
            changed |= HandleWell(elapsedDays);
        }

        if (changed)
        {
            SyncWaterColumn();
            MarkDirty();
        }
        else if(SyncPending) SyncWaterColumn();
    }

    private void OnPeriodicShaftCheck(float dt)
    {
        if(Api is not ICoreServerAPI serverAPI || !serverAPI.World.IsFullyLoadedChunk(Pos)) return; //Only check shaft if neighboring chunks are loaded
        var pos = Pos.Copy();
        int validHeight = 0;
        var ba = Api.World.BlockAccessor;

        if (IsShallow)
        {
            pos.Y++;
            if (WellBlockUtils.SolidAllows(ba.GetSolid(pos)))
            {
                validHeight = 1;
            }
        }
        else
        {
            //A shaft can retain water up to height Y if the shaft walls up to that point all have retention >= Y.
            //e.g. a shaft consisting of 6 ashlar layers, 1 dirt layer, then 3 more ashlar layers will retain 6 layers of water.
            int minRetentionSeen = int.MaxValue;
            for(int i = 0; i < int.MaxValue; i++)
            {
                pos.Y++;
                if(!WellBlockUtils.SolidAllows(ba.GetSolid(pos))) break;

                if(!HasValidShaftWalls(ba, pos, ref minRetentionSeen)) break;

                if(minRetentionSeen <= i) break;

                validHeight++;
            }
        }


        if(WellShaftHeight != validHeight)
        {
            WellShaftHeight = validHeight;
            MarkDirty();
        }
    }

    private static bool IsGameBrick(Block block) => block?.Code?.Domain == "game" && block.Code.Path.StartsWith("brick");

    private static bool IsGameStoneBrick(Block block) => block?.Code?.Domain == "game" && block.Code.Path.StartsWith("stonebrick");

    private static bool IsAqueduct(Block block)
    {
        if (block?.Code is null) return false;
        if (block.Code.Domain != "hardcorewater") return false;

        var p = block.Code.Path;
        return p.StartsWith("aqueduct-") || p.StartsWith("closedaqueduct-");
    }

    private static bool HasValidShaftWalls(IBlockAccessor blockAccessor, BlockPos blockPos, ref int retentionHeight)
    {
        var pos = blockPos.Copy();

        var cfg = ModConfig.Instance.GroundWater;

        foreach (var facing in BlockFacing.HORIZONTALS)
        {
            facing.IterateThruFacingOffsets(pos);
            Block block = blockAccessor.GetBlock(pos);

            if(block.GetLiquidBarrierHeightOnSide(facing.Opposite, pos) < 1 && !IsAqueduct(block)) return false;
            
            if (IsGameBrick(block))
            {
                retentionHeight = Math.Min(retentionHeight, cfg.WellwaterDepthMaxClay);
            }
            else if (IsGameStoneBrick(block))
            {
                retentionHeight = Math.Min(retentionHeight, cfg.WellwaterDepthMaxStone);
            }
            else
            {
                retentionHeight = Math.Min(retentionHeight, cfg.WellwaterDepthMaxBase);
            }
        }

        return true;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine(Lang.Get("hydrateordiedrate:block-wellspring-description"));

        if(ModConfig.Instance.GroundWater.ShowOutputInfo)
        {
            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("hydrateordiedrate:wellspring-output"));
            AppendOutputInfo(forPlayer, dsc);
            dsc.AppendLine();
        }
    }

    private void SyncWaterColumn()
    {
        if (Api is not ICoreServerAPI serverApi) return;
        if (!serverApi.World.IsFullyLoadedChunk(Pos))
        {
            SyncPending = true;
            return; //Wait with syncing until neighboring chunks are loaded
        }
        SyncPending = false;

        OnPeriodicShaftCheck(0);
        var ba = Api.World.BlockAccessor;

        TotalLiters = GameMath.Clamp(TotalLiters, 0, CapacityLitres);

        var pos = Pos.Copy();

        float neededBlocks = TotalLiters / LitersPerFullBlock;

        int allowedDepth = WellShaftHeight;
        for (int i = 1; i <= allowedDepth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);

            if(i <= neededBlocks)
            {
                if(fluid != WaterBlock)
                {
                    ba.SetFluid(WaterBlock.BlockId, pos);
                    ba.TriggerNeighbourBlockUpdate(pos);
                }
            }
            else 
            {
                var remainingLiters = TotalLiters - (LitersPerFullBlock * (i - 1));
                var heightLevel = HeightFromLiters(remainingLiters);
                if(heightLevel <= 0)
                {
                    ba.SetFluid(0, pos);
                }
                else
                {
                    var partialWaterBlock = ba.GetBlock(WaterBlock.CodeWithVariant("height", heightLevel.ToString()));
                    if(partialWaterBlock is not null && partialWaterBlock != fluid)
                    {
                        ba.SetFluid(partialWaterBlock.BlockId, pos);
                        ba.TriggerNeighbourBlockUpdate(pos);
                    }
                }
                break;
            }
        }

        ClearExcessAboveRetention(allowedDepth);
    }

    private void ReconcileStoredVolumeWithWorld()
    {
        var oldWaterBlock = WaterBlock;
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();
        int depth = WellShaftHeight;
        
        string targetPollution = null;
        float targetLiters = 0;

        for (int i = 0; i < depth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            if (!IsOurWellWater(fluid)) break;

            targetPollution ??= fluid.Variant["pollution"];

            if(!int.TryParse(fluid.Variant["height"], out var height)) height = 7;

            targetLiters += LitersFromHeight(height);

            if(height < 7) break;
        }

        if(targetPollution is not null) TryEnsureWaterVariant("pollution", targetPollution);

        var leeway = LitersFromHeight(1);
        targetLiters = GameMath.Clamp(TotalLiters, targetLiters - leeway, Math.Min(targetLiters + leeway, CapacityLitres));

        bool changed;
        if(targetLiters != TotalLiters)
        {
            TotalLiters = targetLiters;

            if(!IsShallow && TotalLiters <= 0f)
            {
                TryEnsureWaterVariant("pollution", "clean");
            }
            changed = true;
        }
        else changed = oldWaterBlock != WaterBlock;

        if (changed)
        {
            SyncWaterColumn();
            MarkDirty();
        }
    }

    private void ClearExcessAboveRetention(int retentionDepth)
    {
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();

        pos.Y += retentionDepth + 1;
        for (int i = 0; i < 64; i++)
        {
            var fluid = ba.GetFluid(pos);

            if(!IsOurWellWater(fluid)) break;

            ba.SetFluid(0, pos);
            ba.TriggerNeighbourBlockUpdate(pos);
            pos.Y++;
        }
    }

    public bool IsOurWellWater(Block fluidBlock)
    {
        if(fluidBlock?.Code is not AssetLocation code) return false;
        var targetCode = WaterBlock.Code;
        if(code.Domain != targetCode.Domain) return false;
        
        var targetPath = targetCode.Path;
        var seperatorIndex = targetPath.IndexOf('-');

        var basePath = targetPath.AsSpan(0, seperatorIndex == -1 ? targetPath.Length : seperatorIndex);

        return code.Path.StartsWith(basePath);
    }
   
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetDouble("accumulatedWater", accumulatedWater);
        tree.SetDouble("lastDailyLiters", LastDailyLiters);
        tree.SetInt("WellShaftHeight", WellShaftHeight);
        tree.SetInt("WaterBlockId", WaterBlock.Id);
        tree.SetDouble("lastInGameTime", LastInGameDay);
        tree.SetFloat("totalVolumeLiters", TotalLiters);
        if (OriginBlock is not null) tree.SetInt("OriginBlockId", OriginBlock.Id);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        accumulatedWater = tree.GetDouble("accumulatedWater", accumulatedWater);
        LastDailyLiters = tree.GetDouble("lastDailyLiters", LastDailyLiters);

        WellShaftHeight = tree.GetInt("WellShaftHeight", WellShaftHeight);
        LastInGameDay = tree.GetDouble("lastInGameTime", worldAccessForResolve.Calendar.TotalDays);

        TotalLiters = tree.TryGetFloat("totalVolumeLiters") ?? tree.TryGetInt("totalVolumeLiters") ?? TotalLiters;

        var waterBlockId = tree.TryGetInt("WaterBlockId");
        if(waterBlockId.HasValue) TryUpdateWaterBlock(worldAccessForResolve, waterBlockId.Value);

        var originBlockId = tree.TryGetInt("OriginBlockId");
        if (originBlockId.HasValue && originBlockId != OriginBlock?.Id) OriginBlock = worldAccessForResolve.GetBlock(originBlockId.Value);
    }

    public void AppendOutputInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        if(LastDailyLiters > 0)
        {
            dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:well.waterType", WaterItem is null ? string.Empty : WaterItem.GetHeldItemName(new ItemStack(WaterItem))));
        }
        dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:well.outputRate", LastDailyLiters));
        dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:well.retentionVolume", CapacityLitres));
        dsc.Append("  "); dsc.AppendLine(Lang.Get("hydrateordiedrate:well.totalShaftVolume", TotalLiters));
    }
}