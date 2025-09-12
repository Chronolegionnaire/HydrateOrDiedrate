using HydrateOrDiedrate.Aquifer;
using HydrateOrDiedrate.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate.wellwater
{
    public class BlockEntityWellSpring : BlockEntity
    {
        private const int UpdateIntervalMs = 500;
        private const int ShaftHealthCheckMs = 30000;
        private const double AquiferRatingToLitersOutputRatio = 0.5;
        private static int HeightForVolume(int vol) => Math.Min(7, (vol - 1) / 10 + 1);
        private static int VolumeForHeight(int height) => height * 10 - 9;
        private List<int> volumes = new List<int>();
        private List<string> types = new List<string>();
        private double accumulatedWater = 0.0;
        private double LastInGameDay = -1.0;
        private string cachedRingMaterial = "none";
        private bool canPlaceToConfiguredLevel = false;
        private int partialValidatedHeight = 0;
        private string lastWaterType = null;
        private double lastDailyLiters = 0.0;
        private bool isMuddyAtBase = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            AquiferManager.AddWellSpringToChunk(api.World, Pos);

            RegisterGameTickListener(OnServerTick, UpdateIntervalMs);
            RegisterGameTickListener(OnPeriodicShaftCheck, ShaftHealthCheckMs);
            OnPeriodicShaftCheck(0);
            EnsureManagedArraysSized(GetRetentionDepth());
            SyncWorldFromState(forceRebuild: true);
        }
        public int TryExtractLitersAt(BlockPos worldPos, int requestedLiters)
        {
            if (Api?.World == null || requestedLiters <= 0) return 0;
            int idx = worldPos.Y - (Pos.Y + 1);
            if (idx < 0) return 0;
            EnsureManagedArraysSized(GetRetentionDepth());
            if (idx >= volumes.Count) return 0;
            if (IsSolidBlocking(GetSolid(worldPos))) return 0;
            var cur = GetFluid(worldPos);
            if (!IsOurWellwater(cur)) return 0;
            int available = Math.Max(0, volumes[idx]);
            if (available <= 0) return 0;
            int take = Math.Min(requestedLiters, available);
            volumes[idx] -= take;
            SettleDownward();
            SyncWorldFromState(forceRebuild: false);
            return take;
        }

        public int GetTotalManagedVolume()
        {
            int sum = 0;
            for (int i = 0; i < volumes.Count; i++) sum += Math.Max(0, volumes[i]);
            return sum;
        }

        public override void OnBlockRemoved()
        {
            if (Api?.World != null && Api.Side == EnumAppSide.Server)
            {
                ConvertManagedToGameWater();
                AquiferManager.RemoveWellSpringFromChunk(Api.World, Pos);
            }
            base.OnBlockRemoved();
        }
        private Block GetFluid(BlockPos p) => Api.World.BlockAccessor.GetBlock(p, BlockLayersAccess.Fluid);
        private Block GetSolid(BlockPos p) => Api.World.BlockAccessor.GetBlock(p, BlockLayersAccess.Solid);
        private void SetFluid(int blockId, BlockPos p) => Api.World.BlockAccessor.SetBlock(blockId, p, BlockLayersAccess.Fluid);

        private static bool IsSolidBlocking(Block solidBlock)
            => solidBlock != null && solidBlock.Code != null && solidBlock.Code.Path != "air" && solidBlock.Replaceable < 500;

        private bool IsOurWellwater(Block b)
        {
            if (b?.Code == null) return false;
            return b.Code.Domain?.Equals("hydrateordiedrate", StringComparison.OrdinalIgnoreCase) == true
                   && b.Code.Path.StartsWith("wellwater", StringComparison.Ordinal);
        }

        private bool IsBlockingBlock(Block block) => block?.Code is not null && block.Code.Path != "air" && !block.Code.Path.Contains("wellwater");

        private void OnServerTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            double currentInGameDays = Api.World.Calendar.TotalDays;
            if (LastInGameDay < 0)
            {
                LastInGameDay = currentInGameDays;
                return;
            }

            double elapsedDays = currentInGameDays - LastInGameDay;
            if (elapsedDays <= 0) return;
            LastInGameDay = currentInGameDays;

            var blockAccessor = Api.World.BlockAccessor;
            isMuddyAtBase = IsSurroundedBySoil(blockAccessor, Pos);

            var chunk = blockAccessor.GetChunkAtBlockPos(Pos);
            var aquiferData = AquiferManager.GetAquiferChunkData(chunk, Api.Logger)?.Data;
            var wellsprings = AquiferManager.GetWellspringsInChunk(chunk);
            if ((aquiferData is null || aquiferData.AquiferRating == 0) && !isMuddyAtBase) return;
            if (wellsprings.Count == 0) return;
            var (isSaltyNearby, isFreshNearby) = CheckForNearbyGameWater(blockAccessor, Pos);
            string productionType = null;
            if (isMuddyAtBase)
            {
                if (isFreshNearby || isSaltyNearby)
                {
                    productionType = isSaltyNearby ? "muddysalt" : "muddy";
                }
                else
                {
                    return;
                }
            }
            else
            {
                var thisSpring = wellsprings.FirstOrDefault(ws => ws.Position.Equals(Pos));
                if (thisSpring is null) return;

                double remainingRating = (double)aquiferData.AquiferRating / wellsprings.Count;
                double dailyLiters = Math.Max(0, remainingRating * AquiferRatingToLitersOutputRatio) * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
                lastDailyLiters = dailyLiters;
                MarkDirty(true);

                double litersThisTick = dailyLiters * elapsedDays;
                accumulatedWater += litersThisTick;

                productionType = aquiferData.IsSalty ? "salt" : "fresh";
            }

            lastWaterType = productionType;
            MarkDirty(true);
            if (isMuddyAtBase)
            {
                accumulatedWater += 0.001 * (double)ModConfig.Instance.GroundWater.WellSpringOutputMultiplier;
            }

            if (accumulatedWater >= 1.0)
            {
                int whole = (int)Math.Floor(accumulatedWater);
                accumulatedWater -= whole;

                AddLiters(productionType, whole);
            }
            SettleDownward();
            ContaminationPass();
            SyncWorldFromState(forceRebuild: false);
        }

        private void OnPeriodicShaftCheck(float dt)
        {
            cachedRingMaterial = CheckBaseRingMaterial(Api.World.BlockAccessor, Pos);
            int validatedLevels = CheckColumnForMaterial(Api.World.BlockAccessor, Pos, cachedRingMaterial);
            partialValidatedHeight = validatedLevels;

            canPlaceToConfiguredLevel = cachedRingMaterial switch
            {
                "brick" when validatedLevels >= ModConfig.Instance.GroundWater.WellwaterDepthMaxClay => true,
                "stonebrick" when validatedLevels >= ModConfig.Instance.GroundWater.WellwaterDepthMaxStone => true,
                _ => false,
            };

            EnsureManagedArraysSized(GetRetentionDepth());
            SyncWorldFromState(forceRebuild: false);
            MarkDirty(true);
        }
        private int MaxPerBlock(string baseType)
        {
            if (baseType == "muddy" || baseType == "muddysalt") return 9;
            if (baseType == "tainted" || baseType == "taintedsalt") return 70;
            if (baseType == "poisoned" || baseType == "poisonedsalt") return 70;
            return 70;
        }

        private int DetermineMaxDepthBasedOnCached(string ringMat, int validatedLevels)
        {
            int baseDepth = ModConfig.Instance.GroundWater.WellwaterDepthMaxBase;
            if (isMuddyAtBase) return 1;
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

        private void EnsureManagedArraysSized(int desiredDepth)
        {
            if (desiredDepth < 0) desiredDepth = 0;
            while (volumes.Count < desiredDepth) { volumes.Add(0); types.Add(lastWaterType ?? "fresh"); }
            while (volumes.Count > desiredDepth) { volumes.RemoveAt(volumes.Count - 1); types.RemoveAt(types.Count - 1); }
            if (volumes.Count == 0 && isMuddyAtBase)
            {
                volumes.Add(0);
                types.Add(lastWaterType ?? "muddy");
            }
        }
        private void AddLiters(string producedType, int liters)
        {
            EnsureManagedArraysSized(GetRetentionDepth());
            if (volumes.Count == 0) return;
            int idx = 0;
            while (liters > 0 && idx < volumes.Count)
            {
                if (volumes[idx] <= 0 || string.IsNullOrEmpty(types[idx]) || types[idx] == "fresh" || types[idx] == "salt" || types[idx] == "muddy" || types[idx] == "muddysalt")
                {
                    if (volumes[idx] <= 0 || string.IsNullOrEmpty(types[idx]))
                        types[idx] = producedType;
                    if (volumes[idx] <= 0 && (producedType == "muddy" || producedType == "muddysalt"))
                        types[idx] = producedType;
                }

                int cap = MaxPerBlock(types[idx]);
                int canAdd = Math.Max(0, cap - volumes[idx]);
                if (canAdd > 0)
                {
                    int add = Math.Min(canAdd, liters);
                    volumes[idx] += add;
                    liters -= add;
                }

                idx++;
            }
        }

        private void SettleDownward()
        {
            if (volumes.Count == 0) return;

            for (int i = volumes.Count - 1; i > 0; i--)
            {
                if (volumes[i] <= 0) continue;

                int below = i - 1;
                int capBelow = MaxPerBlock(types[below]);
                int roomBelow = Math.Max(0, capBelow - volumes[below]);

                if (roomBelow > 0)
                {
                    int xfer = Math.Min(roomBelow, volumes[i]);
                    volumes[below] += xfer;
                    volumes[i] -= xfer;
                    if (volumes[below] > 0 && (types[below] == null || types[below].Length == 0))
                        types[below] = types[i];
                }
            }
            for (int i = volumes.Count - 1; i >= 0; i--)
            {
                if (volumes[i] == 0)
                {
                }
            }
        }

        private void SyncWorldFromState(bool forceRebuild)
        {
            var ba = Api.World.BlockAccessor;
            int depth = volumes.Count;

            for (int i = 0; i < depth; i++)
            {
                BlockPos p = Pos.UpCopy(i + 1);
                if (IsSolidBlocking(GetSolid(p))) break;
                int vol = volumes[i];
                string baseType = types[i] ?? (lastWaterType ?? "fresh");
                if (!isMuddyAtBase && vol > 0 && !IsValidPlacement(ba, p)) continue;

                if (vol <= 0)
                {
                    Block cur = GetFluid(p);
                    if (IsOurWellwater(cur))
                    {
                        SetFluid(0, p);
                        ba.TriggerNeighbourBlockUpdate(p);
                        ba.MarkBlockModified(p);
                    }
                    continue;
                }
                string finalBase = BaseTypeToWellwater(baseType);
                int height = HeightForVolume(vol);
                string path = $"{finalBase}-natural-still-{height}";
                Block newBlock = Api.World.GetBlock(new AssetLocation("hydrateordiedrate", path));
                if (newBlock == null) continue;
                Block curFluid = GetFluid(p);
                bool replace = curFluid == null || curFluid.Code == null || curFluid != newBlock;

                if (replace)
                {
                    SetFluid(newBlock.BlockId, p);
                    ba.TriggerNeighbourBlockUpdate(p);
                    ba.MarkBlockModified(p);
                }
            }
            ClearForeignAbove(depth);
        }

        private void ClearForeignAbove(int startLevel)
        {
            var ba = Api.World.BlockAccessor;
            for (int i = startLevel; i < startLevel + 32; i++)
            {
                BlockPos p = Pos.UpCopy(i + 1);
                Block cur = GetFluid(p);
                if (IsOurWellwater(cur))
                {
                    SetFluid(0, p);
                    ba.TriggerNeighbourBlockUpdate(p);
                    ba.MarkBlockModified(p);
                }
                else
                {
                    if (cur == null || cur.Code == null || cur.Code.Path == "air") break;
                }
            }
        }

        private string BaseTypeToWellwater(string baseType)
        {
            switch (baseType)
            {
                case "fresh": return "wellwaterfresh";
                case "salt": return "wellwatersalt";
                case "muddy": return "wellwatermuddy";
                case "muddysalt": return "wellwatermuddysalt";
                case "tainted": return "wellwatertainted";
                case "taintedsalt": return "wellwatertaintedsalt";
                case "poisoned": return "wellwaterpoisoned";
                case "poisonedsalt": return "wellwaterpoisonedsalt";
                default:
                    return "wellwaterfresh";
            }
        }

        private string WithSaltVariant(string baseType, bool salt)
        {
            if (!salt) return baseType;

            return baseType switch
            {
                "fresh" => "salt",
                "muddy" => "muddysalt",
                "tainted" => "taintedsalt",
                "poisoned" => "poisonedsalt",
                _ => baseType.Contains("salt") ? baseType : baseType + "salt"
            };
        }

        private void ConvertManagedToGameWater()
        {
            var ba = Api.World.BlockAccessor;
            int depth = volumes.Count;

            for (int i = 0; i < depth; i++)
            {
                BlockPos p = Pos.UpCopy(i + 1);
                if (IsSolidBlocking(GetSolid(p))) break;

                Block cur = GetFluid(p);
                if (!IsOurWellwater(cur)) continue;
                Block gameWater = Api.World.GetBlock(new AssetLocation("game", "water-still-7"));
                if (gameWater != null)
                {
                    SetFluid(gameWater.BlockId, p);
                    ba.TriggerNeighbourBlockUpdate(p);
                    ba.MarkBlockModified(p);
                }
            }
        }
        private void ContaminationPass()
        {
            if (volumes.Count == 0) return;
            for (int i = 0; i < volumes.Count; i++)
            {
                if (volumes[i] <= 0) continue;

                BlockPos p = Pos.UpCopy(i + 1);
                Block cur = GetFluid(p);
                if (!IsOurWellwater(cur)) continue;

                string currentBase = DeriveBaseTypeFromPath(cur?.Code?.Path);
                bool isSalt = currentBase.Contains("salt");
                if (CheckDeadEntityOverlap(cur, p))
                {
                    types[i] = WithSaltVariant("tainted", isSalt);
                    continue;
                }
                if (CheckDeathcapOverlap(cur, p))
                {
                    types[i] = WithSaltVariant("poisoned", isSalt);
                    continue;
                }
                if (CheckNeighborInfection(p, out bool neighborTainted, out bool neighborPoisoned, out bool neighborSalt))
                {
                    if (neighborPoisoned) types[i] = WithSaltVariant("poisoned", isSalt || neighborSalt);
                    else if (neighborTainted) types[i] = WithSaltVariant("tainted", isSalt || neighborSalt);
                }
            }
        }

        private string DeriveBaseTypeFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "fresh";
            if (path.Contains("poisonedsalt")) return "poisonedsalt";
            if (path.Contains("poisoned")) return "poisoned";
            if (path.Contains("taintedsalt")) return "taintedsalt";
            if (path.Contains("tainted")) return "tainted";
            if (path.Contains("muddysalt")) return "muddysalt";
            if (path.Contains("muddy")) return "muddy";
            if (path.Contains("salt")) return "salt";
            return "fresh";
        }

        private bool CheckDeadEntityOverlap(Block currentBlock, BlockPos pos)
        {
            Cuboidf[] collBoxes = currentBlock?.GetCollisionBoxes(Api.World.BlockAccessor, pos) ?? new Cuboidf[] { Cuboidf.Default() };
            var nearby = Api.World.GetEntitiesAround(
                pos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f, 1.5f,
                (Entity e) => e is EntityAgent
            );

            foreach (var box in collBoxes)
            {
                Vec3d bMin = new Vec3d(pos.X + box.X1, pos.Y + box.Y1, pos.Z + box.Z1);
                Vec3d bMax = new Vec3d(pos.X + box.X2, pos.Y + box.Y2, pos.Z + box.Z2);

                foreach (var e in nearby)
                {
                    if (e is EntityAgent agent && !agent.Alive)
                    {
                        var eMin = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X1, agent.CollisionBox.Y1, agent.CollisionBox.Z1);
                        var eMax = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X2, agent.CollisionBox.Y2, agent.CollisionBox.Z2);

                        bool intersects =
                            eMin.X <= bMax.X && eMax.X >= bMin.X &&
                            eMin.Y <= bMax.Y && eMax.Y >= bMin.Y &&
                            eMin.Z <= bMax.Z && eMax.Z >= bMin.Z;

                        if (intersects) return true;
                    }
                }
            }
            return false;
        }

        private bool CheckDeathcapOverlap(Block currentBlock, BlockPos pos)
        {
            Cuboidf[] collBoxes = currentBlock?.GetCollisionBoxes(Api.World.BlockAccessor, pos) ?? new Cuboidf[] { Cuboidf.Default() };
            var nearby = Api.World.GetEntitiesAround(
                pos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f, 1.5f,
                (Entity e) => e is EntityItem
            );

            var target = new AssetLocation("game", "mushroom-deathcap-normal");

            foreach (var box in collBoxes)
            {
                Vec3d bMin = new Vec3d(pos.X + box.X1, pos.Y + box.Y1, pos.Z + box.Z1);
                Vec3d bMax = new Vec3d(pos.X + box.X2, pos.Y + box.Y2, pos.Z + box.Z2);

                foreach (var e in nearby)
                {
                    if (e is EntityItem item && item.Itemstack?.Collectible?.Code != null)
                    {
                        if (!item.Itemstack.Collectible.Code.Equals(target)) continue;

                        var eMin = item.ServerPos.XYZ.AddCopy(item.CollisionBox.X1, item.CollisionBox.Y1, item.CollisionBox.Z1);
                        var eMax = item.ServerPos.XYZ.AddCopy(item.CollisionBox.X2, item.CollisionBox.Y2, item.CollisionBox.Z2);

                        bool intersects =
                            eMin.X <= bMax.X && eMax.X >= bMin.X &&
                            eMin.Y <= bMax.Y && eMax.Y >= bMin.Y &&
                            eMin.Z <= bMax.Z && eMax.Z >= bMin.Z;

                        if (intersects) return true;
                    }
                }
            }
            return false;
        }

        private bool CheckNeighborInfection(BlockPos pos, out bool tainted, out bool poisoned, out bool salt)
        {
            tainted = false; poisoned = false; salt = false;

            foreach (BlockFacing f in BlockFacing.ALLFACES)
            {
                Block n = GetFluid(pos.AddCopy(f));
                if (!IsOurWellwater(n)) continue;
                string p = n.Code.Path;

                bool nt = p.Contains("wellwatertainted");
                bool np = p.Contains("wellwaterpoisoned");
                bool ns = p.Contains("salt");

                if (np) { poisoned = true; }
                if (nt) { tainted = true; }
                if (ns) { salt = true; }

                if ((poisoned || tainted) && salt) return true;
            }

            return tainted || poisoned;
        }
        private bool IsValidPlacement(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block northBlock = blockAccessor.GetBlock(pos.NorthCopy());
            Block eastBlock = blockAccessor.GetBlock(pos.EastCopy());
            Block southBlock = blockAccessor.GetBlock(pos.SouthCopy());
            Block westBlock = blockAccessor.GetBlock(pos.WestCopy());

            bool northSolid = northBlock != null && northBlock.SideSolid[BlockFacing.SOUTH.Index];
            bool eastSolid = eastBlock != null && eastBlock.SideSolid[BlockFacing.WEST.Index];
            bool southSolid = southBlock != null && southBlock.SideSolid[BlockFacing.NORTH.Index];
            bool westSolid = westBlock != null && westBlock.SideSolid[BlockFacing.EAST.Index];

            return northSolid && eastSolid && southSolid && westSolid;
        }

        private string CheckBaseRingMaterial(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block[] neighbors =
            [
                blockAccessor.GetBlock(pos.NorthCopy()),
                blockAccessor.GetBlock(pos.EastCopy()),
                blockAccessor.GetBlock(pos.SouthCopy()),
                blockAccessor.GetBlock(pos.WestCopy())
            ];

            bool allBrick = Array.TrueForAll(neighbors, b => b?.Code?.Domain == "game" && b.Code.Path.StartsWith("brick"));
            if (allBrick) return "brick";

            bool allStone = Array.TrueForAll(neighbors, b => b?.Code?.Domain == "game" && b.Code.Path.StartsWith("stonebrick"));
            if (allStone) return "stonebrick";

            return "none";
        }

        private int CheckColumnForMaterial(IBlockAccessor blockAccessor, BlockPos basePos, string ringMaterial)
        {
            if (ringMaterial == "none") return 0;

            int maxCheck = ringMaterial == "brick"
                ? ModConfig.Instance.GroundWater.WellwaterDepthMaxClay
                : (ringMaterial == "stonebrick" ? ModConfig.Instance.GroundWater.WellwaterDepthMaxStone : 0);

            int validatedHeight = 0;
            for (int level = 1; level <= maxCheck; level++)
            {
                BlockPos checkPos = basePos.UpCopy(level);
                Block[] neighbors = new Block[]
                {
                    blockAccessor.GetBlock(checkPos.NorthCopy()),
                    blockAccessor.GetBlock(checkPos.EastCopy()),
                    blockAccessor.GetBlock(checkPos.SouthCopy()),
                    blockAccessor.GetBlock(checkPos.WestCopy())
                };

                if (ringMaterial == "brick")
                {
                    bool allBrick = neighbors.All(b => b?.Code?.Domain == "game" && b.Code.Path.StartsWith("brick"));
                    if (!allBrick) break;
                }
                else if (ringMaterial == "stonebrick")
                {
                    bool allStone = neighbors.All(b => b?.Code?.Domain == "game" && b.Code.Path.StartsWith("stonebrick"));
                    if (!allStone) break;
                }

                validatedHeight++;
            }
            return validatedHeight;
        }

        private bool IsSurroundedBySoil(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block[] blocks = new Block[]
            {
                blockAccessor.GetBlock(pos.NorthCopy()),
                blockAccessor.GetBlock(pos.EastCopy()),
                blockAccessor.GetBlock(pos.SouthCopy()),
                blockAccessor.GetBlock(pos.WestCopy())
            };

            return blocks.Any(b =>
                b?.Code?.Path.StartsWith("soil-") == true ||
                b?.Code?.Path.StartsWith("sand-") == true ||
                b?.Code?.Path.StartsWith("gravel-") == true
            );
        }

        private (bool isSalty, bool isFresh) CheckForNearbyGameWater(IBlockAccessor blockAccessor, BlockPos centerPos)
        {
            bool saltyFound = false;
            bool freshFound = false;
            var checkPos = new BlockPos();
            int cx = centerPos.X, cy = centerPos.Y, cz = centerPos.Z;
            for (int dx = -3; dx <= 2; dx++)
            for (int dy = -3; dy <= 2; dy++)
            for (int dz = -3; dz <= 2; dz++)
            {
                if (dx == 0 && dy == 0 && dz == 0) continue;

                checkPos.Set(cx + dx, cy + dy, cz + dz);

                Block checkBlock = blockAccessor.GetBlock(checkPos, BlockLayersAccess.Fluid);

                if (checkBlock?.Code?.Domain == "game")
                {
                    if (checkBlock.Code.Path.StartsWith("saltwater-")) saltyFound = true;
                    else if (checkBlock.Code.Path.StartsWith("water-") || checkBlock.Code.Path.StartsWith("boilingwater-")) freshFound = true;
                }

                if (saltyFound && freshFound) return (true, true);
            }
            return (saltyFound, freshFound);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            accumulatedWater = tree.GetDouble("accumulatedWater", 0.0);
            lastDailyLiters = tree.GetDouble("lastDailyLiters", lastDailyLiters);
            cachedRingMaterial = tree.GetString("cachedRingMaterial", cachedRingMaterial);
            partialValidatedHeight = tree.GetInt("partialValidatedHeight", partialValidatedHeight);
            lastWaterType = tree.GetString("lastWaterType", lastWaterType);
            LastInGameDay = tree.GetDouble("lastInGameTime", LastInGameDay);
            string vols = tree.GetString("managedVolumesCsv", "");
            string typs = tree.GetString("managedTypesCsv", "");
            volumes = ParseCsvToIntList(vols);
            types = ParseCsvToStringList(typs);

            EnsureManagedArraysSized(GetRetentionDepth());
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetDouble("accumulatedWater", accumulatedWater);
            tree.SetDouble("lastDailyLiters", lastDailyLiters);
            tree.SetString("cachedRingMaterial", cachedRingMaterial ?? "none");
            tree.SetString("lastWaterType", lastWaterType ?? "");
            tree.SetInt("partialValidatedHeight", partialValidatedHeight);
            tree.SetDouble("lastInGameTime", LastInGameDay);

            tree.SetString("managedVolumesCsv", string.Join(",", volumes));
            tree.SetString("managedTypesCsv", string.Join(",", types.Select(s => s ?? "")));
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            string description = Lang.Get("hydrateordiedrate:block-wellspring-description");
            dsc.AppendLine(description);
        }

        public string GetWaterType() => lastWaterType;
        public double GetCurrentOutputRate() => lastDailyLiters;
        public int GetRetentionDepth() => DetermineMaxDepthBasedOnCached(cachedRingMaterial, partialValidatedHeight);

        private static List<int> ParseCsvToIntList(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            var parts = csv.Split(',');
            var list = new List<int>(parts.Length);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int v)) list.Add(v);
                else list.Add(0);
            }
            return list;
        }

        private static List<string> ParseCsvToStringList(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
            return csv.Split(',').Select(s => s.Trim()).ToList();
        }
    }
}
