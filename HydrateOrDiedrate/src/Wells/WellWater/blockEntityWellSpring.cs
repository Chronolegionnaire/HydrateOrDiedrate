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
    private int totalVolumeLiters = 0;
    private string currentPollution = "clean";
    public int TotalLiters => totalVolumeLiters;
    public string CurrentPollution => currentPollution;
    public bool IsFresh => (LastWaterType?.StartsWith("fresh") ?? true);
    private bool didInitialReconcile = false;
    private const int reconcileIntervalMs = 5000;
    public int TryChangeVolume(int change)
    {
        if (change == 0) return 0;

        int perBlockMax = GetMaxVolumeForWaterType(LastWaterType ?? "fresh-well-clean");
        int capacity = GetRetentionDepth() * perBlockMax;
        totalVolumeLiters = Math.Clamp(totalVolumeLiters, 0, capacity);
        long proposed = (long)totalVolumeLiters + change;
        int clamped = (int)Math.Clamp(proposed, 0, capacity);

        int applied = clamped - totalVolumeLiters;
        totalVolumeLiters = clamped;

        SyncWaterColumn();
        MarkDirty(true);

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

        ReconcileStoredVolumeWithWorld();
        didInitialReconcile = true;
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
        base.OnBlockRemoved();
        if(Api.Side == EnumAppSide.Server) AquiferManager.RemoveWellSpringFromChunk(Api.World, Pos);
    }

    private void HandleShallowWell(double elapsedDays)
    {
        var (nearbySalty, nearbyFresh) = Api.World.BlockAccessor.CheckForNearbyGameWater(Pos);
        if (!nearbyFresh && !nearbySalty) return;

        LastWaterType = GetWaterType(nearbyFresh && !nearbySalty, "muddy");
        LastDailyLiters = ModConfig.Instance.GroundWater.ShallowWellLitersPerDay;
        accumulatedWater += LastDailyLiters * elapsedDays * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        if (accumulatedWater < 1.0) return;

        int wholeLiters = (int)accumulatedWater;
        accumulatedWater -= wholeLiters;
        LastWaterType = GetWaterType(nearbyFresh && !nearbySalty, "muddy");
        currentPollution = "muddy";
        TryChangeVolume(wholeLiters);
    }

    private void HandleAquiferWell(double elapsedDays)
    {
        var chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);

        if (AquiferManager.GetAquiferChunkData(chunk, Api.Logger)?.Data is not { AquiferRating: not 0 } aquiferData) return;
        if (AquiferManager.GetWellspringsInChunk(chunk) is not { Count: not 0 } wellsprings) return;

        double remainingRating = (double)aquiferData.AquiferRating / wellsprings.Count;
        var thisSpring = wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos));
        if (thisSpring is null) return;

        LastWaterType = GetWaterType(!aquiferData.IsSalty);
        LastDailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio) * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
        accumulatedWater += LastDailyLiters * elapsedDays;
        if (accumulatedWater < 1.0) return;
        int wholeLiters = (int)Math.Floor(accumulatedWater);
        accumulatedWater -= wholeLiters;
        LastWaterType = GetWaterType(!aquiferData.IsSalty);
        currentPollution = "clean";
        TryChangeVolume(wholeLiters);
    }

    private void HandleWell(double elapsedDays)
    {
        if(IsShallow) HandleShallowWell(elapsedDays);
        else HandleAquiferWell(elapsedDays);
        MarkDirty();
    }

    private void OnServerTick(float dt)
    {
        double currentInGameDays = Api.World.Calendar.TotalDays;

        RunContaminationChecks();
        SyncWaterColumn();
        MarkDirty();

        double elapsedDays = currentInGameDays - LastInGameDay;
        if (elapsedDays <= 0.05) return;
        LastInGameDay = currentInGameDays;

        HandleWell(elapsedDays);
        SyncWaterColumn();
        MarkDirty();
    }


    private void OnPeriodicShaftCheck(float dt)
    {
        var pos = Pos.UpCopy();
        cachedRingMaterial = CheckBaseRingMaterial(Api.World.BlockAccessor, pos);
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

    public static string GetRingMaterial(Block block)
    {
        //TODO make a better mechanism for this
        if(block?.Code is null || block.Code.Domain != "game") return  "none";
        if(block.Code.Path.StartsWith("brick")) return "brick";
        if(block.Code.Path.StartsWith("stonebrick")) return "stonebrick";
        return "none";
    }

    private static string CheckBaseRingMaterial(IBlockAccessor blockAccessor, BlockPos blockPos)
    {
        string result = null;
        var pos = blockPos.Copy();
        
        var sidesToCheck = BlockFacing.HORIZONTALS;
        for (int i = 0; i < sidesToCheck.Length; i++)
        {
            sidesToCheck[i].IterateThruFacingOffsets(pos);
            var ringMaterial = GetRingMaterial(blockAccessor.GetBlock(pos));
            result ??= ringMaterial;
            if(ringMaterial == "none" || result != ringMaterial) return "none";
        }

        return result;
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
            var ringMaterialAtPos = CheckBaseRingMaterial(blockAccessor, pos);
            if(ringMaterialAtPos != ringMaterial) return pos.Y - basePos.Y - 1;
        }

        return maxCheck;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        dsc.AppendLine(Lang.Get("hydrateordiedrate:block-wellspring-description"));
    }
    
    private void SyncWaterColumn()
    {
        if (Api.Side != EnumAppSide.Server) return;
        var ba = Api.World.BlockAccessor;
        var baseCode = $"wellwater-{(LastWaterType?.StartsWith("fresh") == true ? "fresh" : "salt")}-{currentPollution}";
        int perBlockMax = GetMaxVolumeForWaterType(LastWaterType ?? "fresh-well-clean");
        int retentionDepth = GetRetentionDepth();
        var pos = Pos.Copy();
        int neededBlocks = (int)Math.Ceiling(Math.Min(totalVolumeLiters, retentionDepth * perBlockMax) / (double)perBlockMax);
        for (int i = 0; i < retentionDepth; i++)
        {
            pos.Y++;
            if (!WellBlockUtils.SolidAllows(ba.GetSolid(pos))) break;

            var fluid = ba.GetFluid(pos);
            bool isOurs = fluid?.Code?.Path.StartsWith(baseCode) == true;

            if (i < neededBlocks)
            {
                if (!isOurs)
                {
                    var block = Api.World.GetBlock(new AssetLocation("hydrateordiedrate", $"{baseCode}-natural-still-1"));
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
        int remaining = Math.Min(totalVolumeLiters, retentionDepth * perBlockMax);
        pos.Set(Pos);
        for (int i = 0; i < retentionDepth; i++)
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

            int thisLevel = Math.Min(perBlockMax, remaining);
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
        int cappedMax = retentionDepth * perBlockMax;
        if (totalVolumeLiters > cappedMax) totalVolumeLiters = cappedMax;
        ClearExcessAboveRetention(baseCode, retentionDepth);
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
        int inWorld = 0;
        bool? detectedFresh = null;
        string detectedPollution = null;

        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();
        int depth = GetRetentionDepth();

        for (int i = 0; i < depth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            if (!WellBlockUtils.IsOurWellwater(fluid)) break;

            if (detectedFresh == null)
            {
                var (isFresh, pol) = ParseTypeFromFluid(fluid);
                detectedFresh = isFresh;
                detectedPollution = string.IsNullOrEmpty(pol) ? "clean" : pol;
            }

            bool isMuddy = (fluid?.Variant?["pollution"]) == "muddy";
            int perBlockMax = isMuddy ? 9 : 70;

            var hStr = fluid?.Variant?["height"];
            if (isMuddy)
            {
                inWorld += Math.Min(9, perBlockMax);
            }
            else if (hStr != null && int.TryParse(hStr, out int h) && h > 0)
            {
                inWorld += Math.Min(VolumeFromHeight(h), perBlockMax);
            }
            else
            {
                inWorld += perBlockMax;
            }
        }

        if (detectedFresh != null)
        {
            LastWaterType = GetWaterType(detectedFresh.Value, detectedPollution);
            currentPollution = detectedPollution;
        }

        if (inWorld != totalVolumeLiters)
        {
            totalVolumeLiters = inWorld;
            MarkDirty(true);
        }

        int capacity = GetRetentionDepth() * GetMaxVolumeForWaterType(LastWaterType ?? "fresh-well-clean");
        if (totalVolumeLiters > capacity)
        {
            totalVolumeLiters = capacity;
            MarkDirty(true);
        }
    }

    private void RunContaminationChecks()
    {
        if (currentPollution != "clean" && currentPollution != "muddy") return;

        if (CheckDeadEntityContaminationColumn()) return;
        if (CheckPoisonedItemContaminationColumn()) return;
        if (CheckNeighborContaminationColumn()) return;
    }

    private void SetColumnPollution(string pollution)
    {
        currentPollution = pollution;
        SyncWaterColumn();
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
                        SetColumnPollution("tainted");
                        return true;
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
    
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetDouble("accumulatedWater", accumulatedWater);
        tree.SetDouble("lastDailyLiters", LastDailyLiters);
        tree.SetString("cachedRingMaterial", cachedRingMaterial);
        tree.SetString("lastWaterType", LastWaterType ?? string.Empty);
        tree.SetInt("partialValidatedHeight", partialValidatedHeight);
        tree.SetDouble("lastInGameTime", LastInGameDay);
        tree.SetInt("totalVolumeLiters", totalVolumeLiters);
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
        totalVolumeLiters = tree.GetInt("totalVolumeLiters", totalVolumeLiters);
        currentPollution = tree.GetString("currentPollution", currentPollution);

        var originBlockId = tree.TryGetInt("OriginBlockId");
        if (originBlockId.HasValue && originBlockId != OriginBlock?.Id) OriginBlock = worldAccessForResolve.GetBlock(originBlockId.Value);
    }

    public static string GetWaterType(bool isFresh, string pollution = "clean") => $"{(isFresh ? "fresh" : "salt")}-well-{pollution}";

    public static int GetMaxVolumeForWaterType(string waterType) => waterType.Contains("muddy") ? 9 : 70;

    public int GetMaxTotalVolume() => GetRetentionDepth() * GetMaxVolumeForWaterType(LastWaterType);

    public int GetRetentionDepth() => DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);
}