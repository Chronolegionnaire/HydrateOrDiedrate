using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.wellwater
{
    public class BlockEntityWellWaterData : BlockEntity
    {
        private int volume;
        private const int MaxVolume = 70;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (Block != null)
            {
                int initialHeight = Block.Variant["height"].ToInt();
                volume = GetVolumeForHeight(initialHeight);
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

                    if (volume == 0)
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

        private int GetHeightForVolume(int vol)
        {
            return Math.Min(7, (vol - 1) / 10 + 1);
        }

        private int GetVolumeForHeight(int height)
        {
            return height * 10 - 1;
        }

        private void ChangeBlockHeight(int newHeight)
        {
            Block block = Api.World.GetBlock(Block.CodeWithVariant("height", newHeight.ToString()));
            if (block != null)
            {
                Api.World.BlockAccessor.ExchangeBlock(block.BlockId, Pos);
            }
        }

        private void DeleteBlockAndEntity()
        {
            Api.World.BlockAccessor.SetBlock(0, Pos, 2);
            Api.World.BlockAccessor.RemoveBlockEntity(Pos);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            volume = tree.GetInt("volume");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("volume", volume);
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
    }
}
