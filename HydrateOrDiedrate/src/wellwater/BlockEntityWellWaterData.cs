using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.wellwater
{
    public class BlockEntityWellWaterData : BlockEntity
    {
        private int volume;
        private const int MaxVolume = 70;
        private long tickUpdateListenerId;
        private long checkValidityListenerId;
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

                tickUpdateListenerId = api.Event.RegisterGameTickListener(OnTickUpdate, 1000);
                checkValidityListenerId = api.Event.RegisterGameTickListener(CheckBlockValidity, 1000);
            }
        }
        public int Volume
        {
            get => volume;
            set
            {
                int clampedVolume = Math.Max(0, Math.Min(value, MaxVolume));

                if (clampedVolume != volume)
                {
                    volume = clampedVolume;

                    if (volume <= 0)
                    {
                        DeleteBlockAndEntity();
                        return;
                    }

                    int newHeight = GetHeightForVolume(volume);
                    if (newHeight != Block.Variant["height"].ToInt())
                    {
                        ChangeBlockHeight(newHeight);
                    }

                    MarkDirty(true);
                }
            }
        }
        private int GetHeightForVolume(int vol) => Math.Min(7, (vol - 1) / 10 + 1);
        private int GetVolumeForHeight(int height) => height * 10 - 1;

        private void ChangeBlockHeight(int newHeight)
        {
            Block block = Api.World.GetBlock(Block.CodeWithVariant("height", newHeight.ToString()));
            if (block != null)
            {
                Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);

                Api.World.BlockAccessor.MarkBlockModified(Pos);

                NotifyNeighborsOfHeightChange();
            }
        }
        private void NotifyNeighborsOfHeightChange()
        {
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


        private void CheckBlockValidity(float dt)
        {

            if (Api.World.BlockAccessor.GetBlockEntity(Pos) != this)
            {
                return;
            }

            Block currentBlock = Api.World.BlockAccessor.GetBlock(Pos);
            if (!IsValidNaturalWellWaterBlock(currentBlock))
            {
                DeleteBlockAndEntity();
            }
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
            if (block == null || block.Code == null) return false;

            string[] parts = block.Code.Path.Split('-');
            if (parts.Length < 4 || !parts[0].StartsWith("wellwater")) return false;
            string createdBy = parts[1];
            return createdBy.Equals("natural", StringComparison.OrdinalIgnoreCase);
        }
        private void OnTickUpdate(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (volume <= 0)
            {
                DeleteBlockAndEntity();
                return;
            }
            TryTransferVolumeVertically();
        }
        private void TryTransferVolumeVertically()
        {
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
        public override void OnExchanged(Block block)
        {
            UnregisterTickListeners();
            if (CallbackHandlers != null)
            {
                foreach (long callbackHandler in CallbackHandlers)
                    Api.Event.UnregisterCallback(callbackHandler);
            }
            base.OnExchanged(block);
            Api.World.BlockAccessor.RemoveBlockEntity(Pos);
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
            if (Block != null)
            {
                int initialHeight = Block.Variant["height"].ToInt();
                volume = GetVolumeForHeight(initialHeight);
            }
        }
        public override void OnBlockRemoved()
        {

            UnregisterTickListeners();

            if (CallbackHandlers != null)
            {
                foreach (long callbackHandler in CallbackHandlers)
                {
                    Api.Event.UnregisterCallback(callbackHandler);
                }
            }

            base.OnBlockRemoved();
        }
        private void UnregisterTickListeners()
        {
            Api.Event.UnregisterGameTickListener(tickUpdateListenerId);
            Api.Event.UnregisterGameTickListener(checkValidityListenerId);
        }
    }
}
