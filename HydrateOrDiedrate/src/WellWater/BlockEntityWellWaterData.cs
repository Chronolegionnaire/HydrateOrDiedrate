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
                if (Block != null)
                {
                    int initialHeight = Block.Variant["height"].ToInt();
                    volume = GetVolumeForHeight(initialHeight);
                }
                checkBlockListenerId = api.Event.RegisterGameTickListener(CheckBlock, 3000);
            }
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
                Block currentBlock = Api.World.BlockAccessor.GetBlock(Pos);
                string[] parts = currentBlock.Code.Path.Split('-');
                if (parts.Length >= 4)
                {
                    int oldHeight = parts[3].ToInt();
                    if (newHeight != oldHeight)
                    {
                        ChangeBlockHeight(currentBlock, newHeight);
                    }
                }

                MarkDirty(true);
            }
        }
        private int GetHeightForVolume(int vol) => Math.Min(7, (vol - 1) / 10 + 1);
        private int GetVolumeForHeight(int height) => height * 10 - 9;
        private void ChangeBlockHeight(Block currentBlock, int newHeight)
        {
            if (Api.Side != EnumAppSide.Server) return;
            string[] parts = currentBlock.Code.Path.Split('-');
            if (parts.Length < 4)
            {
                return;
            }
            parts[3] = newHeight.ToString();
            string finalPath = string.Join("-", parts);

            AssetLocation newBlockLoc = new AssetLocation(currentBlock.Code.Domain, finalPath);
            Block newBlock = Api.World.GetBlock(newBlockLoc);

            if (newBlock != null && newBlock != currentBlock)
            {
                Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, Pos);
                Api.World.BlockAccessor.MarkBlockModified(Pos);
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
            Block currentBlock = Api.World.BlockAccessor.GetBlock(Pos);
            if (!IsValidNaturalWellWaterBlock(currentBlock))
            {
                DeleteBlockAndEntity();
                return;
            }

            if (volume <= 0)
            {
                DeleteBlockAndEntity();
                return;
            }
            CheckDeadEntityContamination(currentBlock);
            CheckPoisonedItemContamination(currentBlock);
            CheckNeighborContamination(currentBlock);
            TryTransferVolumeVertically();
        }

        private void DeleteBlockAndEntity()
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
            Block currentBlock = Api.World.BlockAccessor.GetBlock(Pos);
            if (IsValidNaturalWellWaterBlock(currentBlock))
            {
                Api.World.BlockAccessor.SetBlock(0, Pos, 2);
                Api.World.BlockAccessor.MarkBlockModified(Pos);
                Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
            }
        }
        private bool IsValidNaturalWellWaterBlock(Block block)
        {
            if (block?.Code?.Path == null) return false;

            string[] parts = block.Code.Path.Split('-');
            if (parts.Length < 4 || !parts[0].StartsWith("wellwater")) return false;

            string createdBy = parts[1];
            return createdBy.Equals("natural", StringComparison.OrdinalIgnoreCase);
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

            string ourPath = currentBlock.Code?.Path ?? "";
            if (ourPath.Contains("tainted") || ourPath.Contains("poisoned")) return;
            bool isFresh = ourPath.Contains("fresh") || ourPath.Contains("muddy");
            bool isSalt = ourPath.Contains("salt") || ourPath.Contains("muddysalt");

            if (!isFresh && !isSalt) return;

            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                BlockPos neighborPos = Pos.AddCopy(facing);
                Block neighborBlock = Api.World.BlockAccessor.GetBlock(neighborPos);
                if (neighborBlock?.Code == null) continue;

                string neighborPath = neighborBlock.Code.Path;
                bool neighborIsTainted = neighborPath.Contains("wellwatertainted");
                bool neighborIsPoisoned = neighborPath.Contains("wellwaterpoisoned");
                if (!neighborIsTainted && !neighborIsPoisoned) continue;

                bool neighborIsSalt = neighborPath.Contains("salt") || neighborPath.Contains("muddysalt");
                string newBaseType = neighborIsTainted ? "wellwatertainted" : "wellwaterpoisoned";
                string finalType = isSalt
                    ? newBaseType + "salt"
                    : (neighborIsSalt ? newBaseType + "salt" : newBaseType);

                string[] parts = ourPath.Split('-');
                if (parts.Length < 4) return;

                string createdBy = parts[1];
                string flowType = parts[2];
                string heightNum = parts[3];
                string finalPath = $"{finalType}-{createdBy}-{flowType}-{heightNum}";
                AssetLocation newBlockLoc = new AssetLocation(Block.Code.Domain, finalPath);
                Block newBlock = Api.World.GetBlock(newBlockLoc);
                if (newBlock != null && newBlock != this.Block)
                {
                    Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, Pos);
                    BlockEntity newBE = Api.World.BlockAccessor.GetBlockEntity(Pos);
                    if (newBE is BlockEntityWellWaterData newWaterData)
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
                Block currentBlock = Api.World.BlockAccessor.GetBlock(currentPos);
                if (currentBlock.Code.Path.Contains("wellwater"))
                {
                    BlockEntity entity = Api.World.BlockAccessor.GetBlockEntity(currentPos);
                    if (entity is BlockEntityWellWaterData wellWaterEntity)
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
            int oldVol = this.volume;
            string[] parts = currentBlock.Code.Path.Split('-');
            if (parts.Length < 4)
            {
                return;
            }
            string oldCreated  = parts[1]; 
            string oldFlowType = parts[2];
            int oldHeight      = parts[3].ToInt();
            string newBase = "wellwaterpoisoned";
            if (isSalt) newBase += "salt";
            string finalPath = $"{newBase}-{oldCreated}-{oldFlowType}-{oldHeight}";
            AssetLocation newLoc = new AssetLocation(currentBlock.Code.Domain, finalPath);
            Block newBlock = Api.World.GetBlock(newLoc);
            if (newBlock != null && newBlock != currentBlock)
            {
                Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, Pos);
                BlockEntity newBE = Api.World.BlockAccessor.GetBlockEntity(Pos);
                if (newBE is BlockEntityWellWaterData newWaterData)
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

            int oldVol = this.volume;
            string[] parts = currentBlock.Code.Path.Split('-');
            if (parts.Length < 4)
            {
                return;
            }
            string oldCreated  = parts[1]; 
            string oldFlowType = parts[2];
            int oldHeight      = parts[3].ToInt();
            string newBase = "wellwatertainted";
            if (isSalt) newBase += "salt";
            string finalPath = $"{newBase}-{oldCreated}-{oldFlowType}-{oldHeight}";
            AssetLocation newLoc = new AssetLocation(currentBlock.Code.Domain, finalPath);

            Block newBlock = Api.World.GetBlock(newLoc);
            if (newBlock != null && newBlock != currentBlock)
            {
                Api.World.BlockAccessor.ExchangeBlock(newBlock.BlockId, Pos);
                BlockEntity newBE = Api.World.BlockAccessor.GetBlockEntity(Pos);
                if (newBE is BlockEntityWellWaterData newWaterData)
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
                UnregisterTickListeners();
                if (CallbackHandlers != null)
                {
                    foreach (long callbackHandler in CallbackHandlers)
                    {
                        Api.Event.UnregisterCallback(callbackHandler);
                    }
                }
            }
            base.OnExchanged(block);

            if (Api.Side == EnumAppSide.Server)
            {
                Api.World.BlockAccessor.RemoveBlockEntity(Pos);
            }
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

            if (Block != null)
            {
                int initialHeight = Block.Variant["height"].ToInt();
                volume = GetVolumeForHeight(initialHeight);
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
