using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate.wellwater
{
    public class BlockEntityWellWaterData : BlockEntity
    {
        private int volume;
        private const int MaxVolume = 70;
        private long checkBlockListenerId;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api.Side == EnumAppSide.Server)
            {
                if (volume == 0 && TryGetHeightFromFluid(Pos, out int h))
                {
                    volume = GetVolumeForHeight(h);
                }
                checkBlockListenerId = api.Event.RegisterGameTickListener(CheckBlock, 3000);
            }
        }
        private bool TryGetHeightFromFluid(BlockPos pos, out int height)
        {
            height = 0;
            var fluid = GetFluid(pos);
            if (fluid?.Code == null || fluid.Variant == null) return false;

            if (fluid.Variant.TryGetValue("height", out string hs) && int.TryParse(hs, out int h) && h > 0)
            {
                height = h;
                return true;
            }
            return false;
        }
        private Block GetFluid(BlockPos p) => Api.World.BlockAccessor.GetBlock(p, 2);
        private void  SetFluid(int blockId, BlockPos p) => Api.World.BlockAccessor.SetBlock(blockId, p, 2);
        private static bool IsOurWellwater(Block b)
        {
            if (b?.Code == null) return false;
            return b.Code.Domain != null
                   && b.Code.Domain.Equals("hydrateordiedrate", StringComparison.OrdinalIgnoreCase)
                   && b.Code.Path.StartsWith("wellwater", StringComparison.Ordinal);
        }
        public int Volume
        {
            get => volume;
            set
            {
                if (Api.Side != EnumAppSide.Server) return;

                int clampedVolume = Math.Max(0, Math.Min(value, MaxVolume));
                if (clampedVolume == volume) return;

                volume = clampedVolume;

                if (volume <= 0)
                {
                    DeleteBlockAndEntity();
                    return;
                }

                int newHeight = GetHeightForVolume(volume);

                Block currentBlock = GetFluid(Pos);
                if (currentBlock?.Code == null || currentBlock.Variant == null) { MarkDirty(true); return; }

                int oldHeight = 0;
                if (currentBlock.Variant.TryGetValue("height", out string hs)) int.TryParse(hs, out oldHeight);

                if (newHeight != oldHeight)
                {
                    ChangeBlockHeight(currentBlock, newHeight);
                }

                MarkDirty(true);
            }
        }
        private int GetHeightForVolume(int vol) => Math.Min(7, (vol - 1) / 10 + 1);
        private int GetVolumeForHeight(int height) => height * 10 - 9;
        private void ChangeBlockHeight(Block currentBlock, int newHeight)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (currentBlock?.Code == null || currentBlock.Variant == null) return;

            string path = currentBlock.Code.Path;
            int dash = path.IndexOf('-');
            string baseType = dash > 0 ? path.Substring(0, dash) : path;

            if (!currentBlock.Variant.TryGetValue("createdBy", out string createdBy)) return;

            currentBlock.Variant.TryGetValue("flow", out string flow);

            if (string.IsNullOrEmpty(createdBy) || string.IsNullOrEmpty(flow)) return;

            string finalPath = $"{baseType}-{createdBy}-{flow}-{newHeight}";
            AssetLocation newBlockLoc = new AssetLocation(currentBlock.Code.Domain, finalPath);
            Block newBlock = Api.World.GetBlock(newBlockLoc);

            if (newBlock != null && newBlock != currentBlock)
            {
                SetFluid(newBlock.BlockId, Pos);
                NotifyNeighborsOfHeightChange();
            }
        }
        private void NotifyNeighborsOfHeightChange()
        {
            if (Api.Side != EnumAppSide.Server) return;

            foreach (var facing in BlockFacing.ALLFACES)
            {
                BlockPos neighborPos = Pos.AddCopy(facing);
                BlockEntity neighborEntity = Api.World.BlockAccessor.GetBlockEntity(neighborPos);
                if (neighborEntity is BlockEntityWellWaterData neighborWaterData)
                {
                    neighborWaterData.MarkDirty(true);
                }

                Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(neighborPos);
                Api.World.BlockAccessor.MarkBlockModified(neighborPos);
            }
        }

        private void CheckBlock(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            if (Api.World.BlockAccessor.GetBlockEntity(Pos) != this) return;
            var fluid = GetFluid(Pos);
            if (!IsValidNaturalWellWaterBlock(fluid))
            {
                DeleteBlockAndEntity();
                return;
            }

            if (volume <= 0)
            {
                DeleteBlockAndEntity();
                return;
            }
            CheckDeadEntityContamination(fluid);
            CheckPoisonedItemContamination(fluid);
            CheckNeighborContamination(fluid);
            TryTransferVolumeVertically();
        }

        private void DeleteBlockAndEntity(bool keepFluid = false)
        {
            if (Api.Side != EnumAppSide.Server) return;

            UnregisterTickListeners();

            if (CallbackHandlers != null)
            {
                foreach (long callbackHandler in CallbackHandlers)
                {
                    Api.Event.UnregisterCallback(callbackHandler);
                }
            }
            Api.World.BlockAccessor.RemoveBlockEntity(Pos);

            if (!keepFluid)
            {
                Block currentBlock = GetFluid(Pos);
                if (IsValidNaturalWellWaterBlock(currentBlock))
                {
                    SetFluid(0, Pos);
                }
            }
        }
        private bool IsValidNaturalWellWaterBlock(Block block)
        {
            if (!IsOurWellwater(block) || block.Variant == null) return false;
            if (block.Variant.TryGetValue("createdBy", out string createdBy) && createdBy.Equals("natural", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
        private void CheckDeadEntityContamination(Block currentBlock)
        {
            if (Api.Side != EnumAppSide.Server) return;
            string ourPath = currentBlock.Code.Path;
            bool isFresh = ourPath.Contains("fresh") || ourPath.Contains("muddy");
            bool isSalt = ourPath.Contains("salt") || ourPath.Contains("muddysalt");

            if (!isFresh && !isSalt) return;

            Cuboidf[] collBoxes = currentBlock.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
            if (collBoxes == null || collBoxes.Length == 0)
            {
                collBoxes = new Cuboidf[] { Cuboidf.Default() };
            }

            var nearbyEntities = Api.World.GetEntitiesAround(
                Pos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f,
                1.5f,
                (Entity e) => e is EntityAgent
            );

            foreach (Cuboidf box in collBoxes)
            {
                Vec3d blockMin = new Vec3d(Pos.X + box.X1, Pos.Y + box.Y1, Pos.Z + box.Z1);
                Vec3d blockMax = new Vec3d(Pos.X + box.X2, Pos.Y + box.Y2, Pos.Z + box.Z2);

                foreach (Entity entity in nearbyEntities)
                {
                    if (!(entity is EntityAgent agent)) continue;
                    var eMin = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X1, agent.CollisionBox.Y1, agent.CollisionBox.Z1);
                    var eMax = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X2, agent.CollisionBox.Y2, agent.CollisionBox.Z2);

                    bool intersects =
                        eMin.X <= blockMax.X && eMax.X >= blockMin.X &&
                        eMin.Y <= blockMax.Y && eMax.Y >= blockMin.Y &&
                        eMin.Z <= blockMax.Z && eMax.Z >= blockMin.Z;

                    if (!intersects) continue;

                    if (!agent.Alive)
                    {
                        ConvertToTaintedVariant(currentBlock, isSalt);
                        return;
                    }
                }
            }
        }

        private void CheckPoisonedItemContamination(Block currentBlock)
        {
            if (Api.Side != EnumAppSide.Server) return;

            string ourPath = currentBlock.Code?.Path ?? "";
            bool isFresh = ourPath.Contains("fresh") || ourPath.Contains("muddy");
            bool isSalt = ourPath.Contains("salt") || ourPath.Contains("muddysalt");

            if (!isFresh && !isSalt) return;

            Cuboidf[] collBoxes = currentBlock.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
            if (collBoxes == null || collBoxes.Length == 0)
            {
                collBoxes = new Cuboidf[] { Cuboidf.Default() };
            }

            var nearbyEntities = Api.World.GetEntitiesAround(
                Pos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f,
                1.5f,
                (Entity e) => e is EntityItem
            );

            foreach (Cuboidf box in collBoxes)
            {
                Vec3d blockMin = new Vec3d(Pos.X + box.X1, Pos.Y + box.Y1, Pos.Z + box.Z1);
                Vec3d blockMax = new Vec3d(Pos.X + box.X2, Pos.Y + box.Y2, Pos.Z + box.Z2);

                foreach (Entity entity in nearbyEntities)
                {
                    if (entity is EntityItem itemEntity)
                    {
                        var stack = itemEntity.Itemstack;
                        if (stack?.Collectible?.Code == null) continue;

                        AssetLocation code = stack.Collectible.Code;
                        if (!code.Equals(new AssetLocation("game", "mushroom-deathcap-normal"))) continue;

                        var eMin = itemEntity.ServerPos.XYZ.AddCopy(
                            itemEntity.CollisionBox.X1,
                            itemEntity.CollisionBox.Y1,
                            itemEntity.CollisionBox.Z1
                        );
                        var eMax = itemEntity.ServerPos.XYZ.AddCopy(
                            itemEntity.CollisionBox.X2,
                            itemEntity.CollisionBox.Y2,
                            itemEntity.CollisionBox.Z2
                        );

                        bool intersects =
                            eMin.X <= blockMax.X && eMax.X >= blockMin.X &&
                            eMin.Y <= blockMax.Y && eMax.Y >= blockMin.Y &&
                            eMin.Z <= blockMax.Z && eMax.Z >= blockMin.Z;

                        if (intersects)
                        {
                            ConvertToPoisonedVariant(currentBlock, isSalt);
                            return;
                        }
                    }
                }
            }
        }

        private void CheckNeighborContamination(Block currentBlock)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (currentBlock?.Code == null) return;
            string ourPath = currentBlock.Code.Path;
            if (ourPath.Contains("tainted") || ourPath.Contains("poisoned")) return;
            bool isSalt = ourPath.Contains("salt");
            bool isFreshish = ourPath.Contains("fresh") || ourPath.Contains("muddy") || ourPath.Contains("muddysalt");
            if (!isFreshish) return;
            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                BlockPos neighborPos = Pos.AddCopy(facing);
                Block neighborBlock = GetFluid(neighborPos);
                if (!IsOurWellwater(neighborBlock)) continue;
                string nPath = neighborBlock.Code.Path;
                bool neighborIsTainted = nPath.Contains("wellwatertainted");
                bool neighborIsPoisoned = nPath.Contains("wellwaterpoisoned");
                if (!neighborIsTainted && !neighborIsPoisoned) continue;
                bool neighborIsSalt = nPath.Contains("salt");
                string newBaseType = neighborIsTainted ? "wellwatertainted" : "wellwaterpoisoned";
                string finalBase = (isSalt || neighborIsSalt) ? newBaseType + "salt" : newBaseType;
                if (currentBlock.Variant == null) return;
                if (!currentBlock.Variant.TryGetValue("createdBy", out string createdBy)) return;
                currentBlock.Variant.TryGetValue("flow", out string flow);
                int height = 0;
                if (currentBlock.Variant.TryGetValue("height", out string hs)) int.TryParse(hs, out height);
                if (string.IsNullOrEmpty(createdBy) || string.IsNullOrEmpty(flow) || height <= 0) return;
                string finalPath = $"{finalBase}-{createdBy}-{flow}-{height}";
                AssetLocation newBlockLoc = new AssetLocation(Block.Code.Domain, finalPath);
                Block newBlock = Api.World.GetBlock(newBlockLoc);
                if (newBlock != null && newBlock != this.Block)
                {
                    SetFluid(newBlock.BlockId, Pos);
                    if (Api.World.BlockAccessor.GetBlockEntity(Pos) is BlockEntityWellWaterData newWaterData)
                    {
                        newWaterData.Volume = this.volume;
                        newWaterData.MarkDirty(true);
                    }
                }

                return;
            }
        }

        private void TryTransferVolumeVertically()
        {
            if (Api.Side != EnumAppSide.Server) return;

            var entitiesBelow = GetWellWaterEntitiesBelow();
            if (entitiesBelow.Count == 0) return;

            foreach (var entity in entitiesBelow)
            {
                if (this.volume <= 0)
                {
                    DeleteBlockAndEntity();
                    break;
                }

                int transferableVolume = Math.Min(this.volume, MaxVolume - entity.Volume);
                if (transferableVolume > 0)
                {
                    this.Volume -= transferableVolume;
                    entity.Volume += transferableVolume;
                }
            }
        }

        
        private List<BlockEntityWellWaterData> GetWellWaterEntitiesBelow()
        {
            List<BlockEntityWellWaterData> entitiesBelow = new List<BlockEntityWellWaterData>();
            BlockPos currentPos = Pos.DownCopy();

            while (true)
            {
                Block currentBlock = GetFluid(currentPos);
                if (IsOurWellwater(currentBlock))
                {
                    if (Api.World.BlockAccessor.GetBlockEntity(currentPos) is BlockEntityWellWaterData wellWaterEntity)
                    {
                        entitiesBelow.Add(wellWaterEntity);
                    }
                    currentPos = currentPos.DownCopy();
                }
                else
                {
                    break;
                }
            }

            entitiesBelow.Reverse();
            return entitiesBelow;
        }

        private void ConvertToPoisonedVariant(Block currentBlock, bool isSalt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (currentBlock.Code.Path.Contains("tainted")) return;
            if (currentBlock?.Variant == null) return;

            int oldVol = this.volume;

            if (!currentBlock.Variant.TryGetValue("createdBy", out string createdBy)) return;

            currentBlock.Variant.TryGetValue("flow", out string flow);
            int height = 0;
            if (currentBlock.Variant.TryGetValue("height", out string hs)) int.TryParse(hs, out height);

            if (string.IsNullOrEmpty(createdBy) || string.IsNullOrEmpty(flow) || height <= 0) return;

            string newBase = isSalt ? "wellwaterpoisonedsalt" : "wellwaterpoisoned";
            string finalPath = $"{newBase}-{createdBy}-{flow}-{height}";
            AssetLocation newLoc = new AssetLocation(currentBlock.Code.Domain, finalPath);
            Block newBlock = Api.World.GetBlock(newLoc);
            if (newBlock != null && newBlock != currentBlock)
            {
                SetFluid(newBlock.BlockId, Pos);
                if (Api.World.BlockAccessor.GetBlockEntity(Pos) is BlockEntityWellWaterData newWaterData)
                {
                    newWaterData.Volume = oldVol;
                    newWaterData.MarkDirty(true);
                }
            }
        }

        private void ConvertToTaintedVariant(Block currentBlock, bool isSalt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (currentBlock.Code.Path.Contains("poisoned")) return;
            if (currentBlock?.Variant == null) return;

            int oldVol = this.volume;

            if (!currentBlock.Variant.TryGetValue("createdBy", out string createdBy)) return;

            currentBlock.Variant.TryGetValue("flow", out string flow);
            int height = 0;
            if (currentBlock.Variant.TryGetValue("height", out string hs)) int.TryParse(hs, out height);

            if (string.IsNullOrEmpty(createdBy) || string.IsNullOrEmpty(flow) || height <= 0) return;

            string newBase = isSalt ? "wellwatertaintedsalt" : "wellwatertainted";
            string finalPath = $"{newBase}-{createdBy}-{flow}-{height}";
            AssetLocation newLoc = new AssetLocation(currentBlock.Code.Domain, finalPath);
            Block newBlock = Api.World.GetBlock(newLoc);
            if (newBlock != null && newBlock != currentBlock)
            {
                SetFluid(newBlock.BlockId, Pos);
                if (Api.World.BlockAccessor.GetBlockEntity(Pos) is BlockEntityWellWaterData newWaterData)
                {
                    newWaterData.Volume = oldVol;
                    newWaterData.MarkDirty(true);
                }
            }
        }

        public override void OnExchanged(Block block)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                Api.Event.RegisterCallback((dt) =>
                {
                    var fluid = Api.World.BlockAccessor.GetBlock(Pos, 2);

                    if (!IsOurWellwater(fluid))
                    {
                        DeleteBlockAndEntity(keepFluid: true);
                        return;
                    }
                    MarkDirty(true);

                }, 0);
            }
            base.OnExchanged(block);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("volume", volume);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            volume = tree.GetInt("volume");
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (Api.Side != EnumAppSide.Server) return;

            if (TryGetHeightFromFluid(Pos, out int h))
            {
                volume = GetVolumeForHeight(h);
            }
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Server)
            {
                UnregisterTickListeners();

                if (CallbackHandlers != null)
                {
                    foreach (long callbackHandler in CallbackHandlers)
                    {
                        Api.Event.UnregisterCallback(callbackHandler);
                    }
                }
            }

            base.OnBlockRemoved();
        }

        private void UnregisterTickListeners()
        {
            if (Api.Side != EnumAppSide.Server) return;
            Api.Event.UnregisterGameTickListener(checkBlockListenerId);
        }
    }
}
