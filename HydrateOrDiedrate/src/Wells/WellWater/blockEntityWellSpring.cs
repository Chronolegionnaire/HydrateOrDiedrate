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

namespace HydrateOrDiedrate.Wells.WellWater;

public class BlockEntityWellSpring : BlockEntity, ITexPositionSource
{
    private const int updateIntervalMs = 500;

    private Block originBlock;
    public int totalLiters { get; private set; } = 0;
    public string currentPollution { get; private set; } = "clean";
    public bool IsFresh => (LastWaterType?.StartsWith("fresh") ?? true);
    private const int reconcileIntervalMs = 5000;
    private int PerBlockMax() => currentPollution == "muddy" ? 9 : 70;
    public int TryChangeVolume(int change, bool triggerSync = true)
    {
        if (change == 0) return 0;

        int capacity = ColumnCapacityUpperBound();
        totalLiters = Math.Clamp(totalLiters, 0, capacity);
        long proposed = (long)totalLiters + change;
        int clamped = (int)Math.Clamp(proposed, 0, capacity);

        int applied = clamped - totalLiters;
        totalLiters = clamped;

        if (triggerSync)
        {
            SyncWaterColumn();
            MarkDirty(true);
        }

        return applied;
    }
    private static int HeightFromVolume(int vol) => Math.Min(7, (vol + 9) / 10);
    private static int VolumeFromHeight(int height) => Math.Min(70, height * 10);
    public Block OriginBlock
    {
        get => originBlock;
        set
        {
            if (originBlock == value) return;
            IsShallow = value.IsSoil();
            if (IsShallow && value is not null && value.Variant.TryGetValue("grasscoverage", out var variant) && variant != "none")
            {
                var withoutGrass = Api.World.GetBlock(value.CodeWithVariant("grasscoverage", "none"));
                if(withoutGrass is not null) value = withoutGrass;
            }

            originBlock = value;
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
    
    private int ColumnCapacityUpperBound()
    {
        if (currentPollution == "muddy") return 9;
        return GetRetentionDepth() * PerBlockMax();
    }

    private double LastInGameDay = -1.0;

    private string cachedRingMaterial;
    
    private int partialValidatedHeight;
    
    private const double AquiferRatingToLitersOutputRatio = 0.5;

    public string LastWaterType { get; private set; }
    
    public double LastDailyLiters { get; private set; }

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

        api.Event.EnqueueMainThreadTask(ReconcileStoredVolumeWithWorld, "well-spring-reconcile");
        RegisterGameTickListener(_ => ReconcileStoredVolumeWithWorld(), reconcileIntervalMs);
        OriginBlock ??= api.World.FindMostLikelyOriginBlockFromNeighbors(Pos) ?? api.World.GetBlock(new AssetLocation("game", "rock-granite"));
        OnPeriodicShaftCheck(0);
        HandleWell(0);
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

        var shape = Shape.TryGet(Api, new AssetLocation("hydrateordiedrate","shapes/block/wellspring.json"));
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

            if (!WellBlockUtils.IsOurWellwater(fluid))
                break;
            var be = ba.GetBlockEntity<BlockEntityWellWaterSentinel>(pos);
            if (be != null)
                ba.RemoveBlockEntity(pos);
            ba.SetFluid(0, pos);
            ba.TriggerNeighbourBlockUpdate(pos);
        }
    }

    private bool HandleShallowWell(double elapsedDays)
    {
        bool changed = false;
        var (nearbySalty, nearbyFresh) = Api.World.BlockAccessor.CheckForNearbyGameWater(Pos);
        if (!nearbyFresh && !nearbySalty) return false;

        string newType = GetWaterType(nearbyFresh && !nearbySalty, "muddy");
        if (LastWaterType != newType)
        {
            LastWaterType = newType;
            changed = true;
        }

        const string newPollution = "muddy";
        if (currentPollution != newPollution)
        {
            currentPollution = newPollution;
            changed = true;
        }

        LastDailyLiters = ModConfig.Instance.GroundWater.ShallowWellLitersPerDay;
        accumulatedWater += LastDailyLiters * elapsedDays *
                            (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;

        if (accumulatedWater >= 1.0)
        {
            int wholeLiters = (int)accumulatedWater;
            accumulatedWater -= wholeLiters;
            int applied = TryChangeVolume(wholeLiters, triggerSync: false);
            if (applied != 0) changed = true;
        }

        return changed;
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

        string newType = GetWaterType(!aquifer.IsSalty);
        if (LastWaterType != newType)
        {
            LastWaterType = newType;
            changed = true;
        }

        const string newPollution = "clean";
        if (currentPollution != newPollution)
        {
            currentPollution = newPollution;
            changed = true;
        }

        LastDailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio)
                          * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;

        accumulatedWater += LastDailyLiters * elapsedDays;

        if (accumulatedWater >= 1.0)
        {
            int wholeLiters = (int)Math.Floor(accumulatedWater);
            accumulatedWater -= wholeLiters;

            int applied = TryChangeVolume(wholeLiters, triggerSync: false);
            if (applied != 0) changed = true;
        }

        return changed;
    }

    private bool HandleWell(double elapsedDays)
    {
        return IsShallow ? HandleShallowWell(elapsedDays) : HandleAquiferWell(elapsedDays);
    }

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
    }


    private void OnPeriodicShaftCheck(float dt)
    {
        var pos = Pos.UpCopy();
        cachedRingMaterial = CheckRingMaterial(Api.World.BlockAccessor, pos);
        partialValidatedHeight = CheckColumnForMaterial(Api.World.BlockAccessor, pos, cachedRingMaterial);
        MarkDirty();
    }
    private static int DetermineMaxDepthBasedOnCached(string ringMat, int validatedLevels)
    {
        int baseDepth = ModConfig.Instance.GroundWater.WellwaterDepthMaxBase;
        if (validatedLevels <= 0 || ringMat == "none") return baseDepth;
        if (ringMat == "brick")
        {
            int clayMax = ModConfig.Instance.GroundWater.WellwaterDepthMaxClay;
            int partialDepth = Math.Min(validatedLevels, clayMax);
            return Math.Max(baseDepth, partialDepth);
        }
        else if (ringMat == "stonebrick")
        {
            int stoneMax = ModConfig.Instance.GroundWater.WellwaterDepthMaxStone;
            int partialDepth = Math.Min(validatedLevels, stoneMax);
            return Math.Max(baseDepth, partialDepth);
        }
        return baseDepth;
    }

    private static bool IsGameBrick(Block block)
    {
        return block?.Code?.Domain == "game" && block.Code.Path.StartsWith("brick");
    }

    private static bool IsGameStoneBrick(Block block)
    {
        return block?.Code?.Domain == "game" && block.Code.Path.StartsWith("stonebrick");
    }

    private static bool IsAqueduct(Block block)
    {
        if (block?.Code is null) return false;
        if (block.Code.Domain != "hardcorewater") return false;

        var p = block.Code.Path;
        return p.StartsWith("aqueduct-") || p.StartsWith("closedaqueduct-");
    }

    private static string CheckRingMaterial(IBlockAccessor blockAccessor, BlockPos blockPos)
    {
        bool allAllowedForBrick = true;
        bool allAllowedForStone = true;
        bool hasAtLeastOneBrick = false;
        bool hasAtLeastOneStone = false;

        var pos = blockPos.Copy();

        foreach (var facing in BlockFacing.HORIZONTALS)
        {
            facing.IterateThruFacingOffsets(pos);
            Block b = blockAccessor.GetBlock(pos);

            if (IsAqueduct(b))
            {
            }
            else if (IsGameBrick(b))
            {
                hasAtLeastOneBrick = true;
                allAllowedForStone = false;
            }
            else if (IsGameStoneBrick(b))
            {
                hasAtLeastOneStone = true;
                allAllowedForBrick = false;
            }
            else
            {
                return "none";
            }
            if (!allAllowedForBrick && !allAllowedForStone)
            {
                return "none";
            }
        }

        if (allAllowedForStone && hasAtLeastOneStone) return "stonebrick";
        if (allAllowedForBrick && hasAtLeastOneBrick) return "brick";
        return "none";
    }

    public static int MaxDepthForRingMaterial(string ringMaterial) => ringMaterial switch
    {
        "brick" => ModConfig.Instance.GroundWater.WellwaterDepthMaxClay,
        "stonebrick" => ModConfig.Instance.GroundWater.WellwaterDepthMaxStone,
        _ => 0
    };

    private static int CheckColumnForMaterial(IBlockAccessor blockAccessor, BlockPos basePos, string ringMaterial)
    {
        int maxCheck = MaxDepthForRingMaterial(ringMaterial);
        if (maxCheck == 0) return 0;

        var pos = basePos.Copy();
        while (pos.Y < basePos.Y + maxCheck)
        {
            pos.Y++;
            var ringMaterialAtPos = CheckRingMaterial(blockAccessor, pos);
            if(ringMaterialAtPos != ringMaterial) return pos.Y - basePos.Y - 1;
        }

        return maxCheck;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine(Lang.Get("hydrateordiedrate:block-wellspring-description"));
        dsc.AppendLine();
        if(originBlock is not null)
        {
            dsc.AppendLine(Lang.Get("hydrateordiedrate:dug-in", originBlock.GetHeldItemName(new ItemStack(originBlock))));
        }
        if(!ModConfig.Instance.GroundWater.WellOutputInfo) return;
        dsc.AppendLine();
        GetWellOutputInfo(forPlayer, dsc);
    }

    public void GetWellOutputInfo(IPlayer forPlayer, StringBuilder dsc, bool addSpacing = false)
    {
        if(addSpacing) dsc.Append("  ");
        dsc.AppendLine(Lang.Get("hydrateordiedrate:well.waterType", string.IsNullOrEmpty(LastWaterType) ? string.Empty : Lang.Get($"hydrateordiedrate:item-waterportion-{LastWaterType}")));
        if(addSpacing) dsc.Append("  ");
        dsc.AppendLine(Lang.Get("hydrateordiedrate:well.outputRate", LastDailyLiters));
        if(addSpacing) dsc.Append("  ");
        dsc.AppendLine(Lang.Get("hydrateordiedrate:well.retentionVolume", GetMaxTotalVolume()));
        if(addSpacing) dsc.Append("  ");
        dsc.AppendLine(Lang.Get("hydrateordiedrate:well.totalShaftVolume", totalLiters));
    }

    private void SyncWaterColumn()
    {
        if (Api.Side != EnumAppSide.Server) return;
        var ba = Api.World.BlockAccessor;
        var baseCode = $"wellwater-{(IsFresh ? "fresh" : "salt")}-{currentPollution}";
        int perBlockMax = PerBlockMax();
        int retentionDepth = GetRetentionDepth();
        bool requireWalls = currentPollution != "muddy";
        int allowedDepth = 0;
        var scanPos = Pos.Copy();
        for (int i = 0; i < retentionDepth; i++)
        {
            scanPos.Y++;
            if (!WellBlockUtils.SolidAllows(ba.GetSolid(scanPos))) break;
            if (requireWalls && !HasSolidHorizNeighbors(ba, scanPos)) break;
            allowedDepth++;
        }
        int effectiveDepth = currentPollution == "muddy" ? Math.Min(allowedDepth, 1) : allowedDepth;
        int columnCap = currentPollution == "muddy" ? 9 : (effectiveDepth * perBlockMax);
        if (totalLiters > columnCap) totalLiters = columnCap;
        var pos = Pos.Copy();
        int neededBlocks = currentPollution == "muddy"
            ? (totalLiters > 0 ? 1 : 0)
            : (int)Math.Ceiling(totalLiters / (double)perBlockMax);
        for (int i = 0; i < effectiveDepth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            bool isOurs = fluid?.Code?.Path.StartsWith(baseCode) == true;

            if (i < neededBlocks)
            {
                if (!isOurs)
                {
                    var block = Api.World.GetBlock(
                        new AssetLocation("hydrateordiedrate", $"{baseCode}-natural-still-1"));
                    if (block == null) break;

                    ba.SetFluid(block.BlockId, pos);
                    ba.TriggerNeighbourBlockUpdate(pos);
                }
            }
            else
            {
                if (WellBlockUtils.IsOurWellwater(fluid))
                {
                    ba.SetFluid(0, pos);
                    ba.TriggerNeighbourBlockUpdate(pos);
                }
            }
        }
        int remaining = totalLiters;
        pos.Set(Pos);
        for (int i = 0; i < effectiveDepth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            if (fluid?.Code?.Path.StartsWith(baseCode) != true) break;

            if (remaining <= 0)
            {
                ba.SetFluid(0, pos);
                ba.TriggerNeighbourBlockUpdate(pos);
                continue;
            }

            int thisLevel = currentPollution == "muddy"
                ? Math.Min(9, remaining)
                : Math.Min(perBlockMax, remaining);

            remaining -= thisLevel;

            int newHeight = HeightFromVolume(thisLevel);
            string flow = fluid.Variant?["flow"] ?? "still";
            var newCode = new AssetLocation("hydrateordiedrate", $"{baseCode}-natural-{flow}-{newHeight}");
            var newBlock = Api.World.GetBlock(newCode);
            if (newBlock != null && newBlock.BlockId != fluid.BlockId)
            {
                ba.SetFluid(newBlock.BlockId, pos);
                ba.TriggerNeighbourBlockUpdate(pos);
            }
        }
        ClearExcessAboveRetention(baseCode, effectiveDepth);
    }

    private static (bool isFresh, string pollution) ParseTypeFromFluid(Block fluid)
    {
        bool isFresh = true;
        string pollution = "clean";
        if (fluid?.Code?.Path != null)
        {
            isFresh = fluid.Code.Path.Contains("wellwater-fresh");
            pollution = fluid.Variant?["pollution"] ?? pollution;
        }

        return (isFresh, pollution);
    }

    private void ReconcileStoredVolumeWithWorld()
    {
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();
        int depth = GetRetentionDepth();
        bool? detectedFresh = null;
        string detectedPollution = null;
        int fullVolumeNonMuddy = 0;
        int? partialHeight = null;
        int muddyBlockCount = 0;

        for (int i = 0; i < depth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            if (!WellBlockUtils.IsOurWellwater(fluid)) break;

            if (ba.GetBlockEntity(pos) is null)
            {
                ba.SpawnBlockEntity("HoD:BlockEntityWellWaterSentinel", pos);
            }

            if (detectedFresh == null)
            {
                var (isFresh, pol) = ParseTypeFromFluid(fluid);
                detectedFresh = isFresh;
                detectedPollution = string.IsNullOrEmpty(pol) ? "clean" : pol;
            }

            bool isMuddy = (fluid?.Variant?["pollution"]) == "muddy";
            if (isMuddy)
            {
                muddyBlockCount++;
                continue;
            }
            var hStr = fluid?.Variant?["height"];
            if (hStr == null)
            {
                fullVolumeNonMuddy += VolumeFromHeight(7);
                continue;
            }
            if (int.TryParse(hStr, out int h) && h > 0)
            {
                if (h >= 7) fullVolumeNonMuddy += VolumeFromHeight(7);
                else
                {
                    partialHeight = h;
                    break;
                }
            }
            else fullVolumeNonMuddy += VolumeFromHeight(7);
        }
        if (detectedFresh != null)
        {
            LastWaterType = GetWaterType(detectedFresh.Value, detectedPollution);
            currentPollution = detectedPollution;
        }

        int minVolume, maxVolume, reconcileTarget;
        if (detectedPollution == "muddy")
        {
            minVolume = 0;
            maxVolume = 9;
            reconcileTarget = Math.Clamp(totalLiters, minVolume, maxVolume);
        }
        else if (partialHeight.HasValue)
        {
            int h = partialHeight.Value;
            minVolume = fullVolumeNonMuddy + VolumeFromHeight(Math.Max(0, h - 1));
            maxVolume = fullVolumeNonMuddy + VolumeFromHeight(Math.Min(7, h + 1));
            reconcileTarget = fullVolumeNonMuddy + VolumeFromHeight(h);
        }
        else
        {
            minVolume = fullVolumeNonMuddy - VolumeFromHeight(1);
            maxVolume = fullVolumeNonMuddy;
            reconcileTarget = fullVolumeNonMuddy;
        }
        bool changed = false;

        if (totalLiters < minVolume || totalLiters > maxVolume)
        {
            totalLiters = reconcileTarget;
            changed = true;
        }
        int capacity = ColumnCapacityUpperBound();
        if (totalLiters > capacity)
        {
            totalLiters = capacity;
            changed = true;
        }
        if (detectedPollution == "muddy")
        {
            const int muddyCap = 9;
            if (totalLiters > muddyCap)
            {
                totalLiters = muddyCap;
                changed = true;
            }
        }
        if (changed)
        {
            SyncWaterColumn();
            MarkDirty(true);
        }
    }

    private bool RunContaminationChecks()
    {
        if (currentPollution != "clean" && currentPollution != "muddy") return false;

        if (CheckDeadEntityContaminationColumn()) return true;
        if (CheckPoisonedItemContaminationColumn()) return true;
        if (CheckNeighborContaminationColumn()) return true;

        return false;
    }

    private void ClampToCapacityAndSync()
    {
        int cap = ColumnCapacityUpperBound();
        if (totalLiters > cap) totalLiters = cap;
        SyncWaterColumn();
        MarkDirty(true);
    }

    private bool SetColumnPollution(string pollution)
    {
        if (currentPollution == pollution) return false;
        currentPollution = pollution;
        if (!string.IsNullOrEmpty(LastWaterType))
        {
            bool fresh = LastWaterType.StartsWith("fresh");
            LastWaterType = GetWaterType(fresh, pollution);
        }
        ClampToCapacityAndSync();
        return true;
    }
    private bool ForEachWaterLevel(System.Func<BlockPos, Block, bool> fn)
    {
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();
        int depth = GetRetentionDepth();

        for (int i = 0; i < depth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            if (!WellBlockUtils.IsOurWellwater(fluid)) break;
            if (fn(pos, fluid)) return true;
        }
        return false;
    }


    private bool CheckDeadEntityContaminationColumn()
    {
        bool isFresh = (LastWaterType?.StartsWith("fresh") ?? true);
        if (!isFresh) return false;

        var ba = Api.World.BlockAccessor;
        return ForEachWaterLevel((levelPos, block) =>
        {
            if (block == null) return false;

            var collBoxes = block.GetCollisionBoxes(ba, levelPos) ?? [ Cuboidf.Default() ];

            var nearbyEntities = Api.World.GetEntitiesAround(
                levelPos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f, 1.5f,
                e => e is EntityAgent
            );

            foreach (var box in collBoxes)
            {
                var min = new Vec3d(levelPos.X + box.X1, levelPos.Y + box.Y1, levelPos.Z + box.Z1);
                var max = new Vec3d(levelPos.X + box.X2, levelPos.Y + box.Y2, levelPos.Z + box.Z2);

                foreach (var e in nearbyEntities)
                {
                    if (e is not EntityAgent agent) continue;
                    var emin = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X1, agent.CollisionBox.Y1, agent.CollisionBox.Z1);
                    var emax = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X2, agent.CollisionBox.Y2, agent.CollisionBox.Z2);

                    bool intersects =
                        emin.X <= max.X && emax.X >= min.X &&
                        emin.Y <= max.Y && emax.Y >= min.Y &&
                        emin.Z <= max.Z && emax.Z >= min.Z;

                    if (intersects && !agent.Alive)
                    {
                        return SetColumnPollution("tainted");
                    }
                }
            }
            return false;
        });
    }

    private bool CheckPoisonedItemContaminationColumn()
    {
        var ba = Api.World.BlockAccessor;
        return ForEachWaterLevel((levelPos, block) =>
        {
            if (block == null) return false;

            var collBoxes = block.GetCollisionBoxes(ba, levelPos) ?? [ Cuboidf.Default() ];

            var nearbyItems = Api.World.GetEntitiesAround(
                levelPos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f, 1.5f,
                e => e is EntityItem
            );

            foreach (var box in collBoxes)
            {
                var min = new Vec3d(levelPos.X + box.X1, levelPos.Y + box.Y1, levelPos.Z + box.Y1);
                var max = new Vec3d(levelPos.X + box.X2, levelPos.Y + box.Y2, levelPos.Z + box.Y2);

                foreach (var e in nearbyItems)
                {
                    if (e is not EntityItem item) continue;
                    var stack = item.Itemstack;
                    if (stack?.Collectible?.Code == null) continue;

                    if (!stack.Collectible.Code.Equals(new AssetLocation("game", "mushroom-deathcap-normal"))) continue;

                    var emin = item.ServerPos.XYZ.AddCopy(item.CollisionBox.X1, item.CollisionBox.Y1, item.CollisionBox.Z1);
                    var emax = item.ServerPos.XYZ.AddCopy(item.CollisionBox.X2, item.CollisionBox.Y2, item.CollisionBox.Z2);

                    bool intersects =
                        emin.X <= max.X && emax.X >= min.X &&
                        emin.Y <= max.Y && emax.Y >= min.Y &&
                        emin.Z <= max.Z && emax.Z >= min.Z;

                    if (intersects)
                    {
                        SetColumnPollution("poisoned");
                        return true;
                    }
                }
            }
            return false;
        });
    }

    private bool CheckNeighborContaminationColumn()
    {
        var ba = Api.World.BlockAccessor;
        bool pollutedNeighborFound = false;

        ForEachWaterLevel((levelPos, _block) =>
        {
            foreach (var face in BlockFacing.ALLFACES)
            {
                var npos = levelPos.AddCopy(face);
                var nblock = ba.GetFluid(npos);
                if (!WellBlockUtils.IsOurWellwater(nblock)) continue;

                var pollution = nblock?.Variant?["pollution"];
                if (!string.IsNullOrEmpty(pollution) && pollution != "clean" && pollution != "muddy")
                {
                    SetColumnPollution(pollution);
                    pollutedNeighborFound = true;
                    return true;
                }
            }
            return false;
        });

        return pollutedNeighborFound;
    }
    private void ClearExcessAboveRetention(string baseCode, int retentionDepth)
    {
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();

        pos.Y += retentionDepth + 1;
        for (int i = 0; i < 64; i++)
        {
            var fluid = ba.GetFluid(pos);
            if (fluid?.Code?.Path?.StartsWith(baseCode) != true) break;
            if (!WellBlockUtils.IsOurWellwater(fluid)) break;

            ba.SetFluid(0, pos);
            ba.TriggerNeighbourBlockUpdate(pos);
            pos.Y++;
        }
    }
    
    private static bool HasSolidHorizNeighbors(IBlockAccessor ba, BlockPos pos)
    {
        foreach (var face in BlockFacing.HORIZONTALS)
        {
            var npos = pos.AddCopy(face);
            if (WellBlockUtils.SolidAllows(ba.GetSolid(npos))) return false;
        }
        return true;
    }
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetDouble("accumulatedWater", accumulatedWater);
        tree.SetDouble("lastDailyLiters", LastDailyLiters);
        tree.SetString("cachedRingMaterial", cachedRingMaterial);
        tree.SetString("lastWaterType", LastWaterType ?? string.Empty);
        tree.SetInt("partialValidatedHeight", partialValidatedHeight);
        tree.SetDouble("lastInGameTime", LastInGameDay);
        tree.SetInt("totalVolumeLiters", totalLiters);
        tree.SetString("currentPollution", currentPollution);
        if (OriginBlock is not null) tree.SetInt("OriginBlockId", OriginBlock.Id);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        accumulatedWater = tree.GetDouble("accumulatedWater", accumulatedWater);
        LastDailyLiters = tree.GetDouble("lastDailyLiters", LastDailyLiters);
        cachedRingMaterial = tree.GetString("cachedRingMaterial", cachedRingMaterial);
        partialValidatedHeight = tree.GetInt("partialValidatedHeight", partialValidatedHeight);
        LastWaterType = tree.GetString("lastWaterType", LastWaterType);
        LastInGameDay = tree.GetDouble("lastInGameTime", worldAccessForResolve.Calendar.TotalDays);
        totalLiters = tree.GetInt("totalVolumeLiters", totalLiters);
        currentPollution = tree.GetString("currentPollution", currentPollution);

        var originBlockId = tree.TryGetInt("OriginBlockId");
        if (originBlockId.HasValue && originBlockId != OriginBlock?.Id) OriginBlock = worldAccessForResolve.GetBlock(originBlockId.Value);
    }

    public static string GetWaterType(bool isFresh, string pollution = "clean") => $"{(isFresh ? "fresh" : "salt")}-well-{pollution}";

    public static int GetMaxVolumeForWaterType(string waterType) => waterType is not null && waterType.Contains("muddy") ? 9 : 70;

    public int GetMaxTotalVolume() => GetRetentionDepth() * GetMaxVolumeForWaterType(LastWaterType);

    public int GetRetentionDepth() => DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
}