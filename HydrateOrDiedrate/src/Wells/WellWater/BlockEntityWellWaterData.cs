using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using XLib.XEffects;

namespace HydrateOrDiedrate.Wells.WellWater
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
            var ba = Api.World.BlockAccessor;
            var fluid = ba.GetFluid(pos);
            if (fluid?.Code == null || fluid.Variant == null) return false;

            if (fluid.Variant.TryGetValue("height", out string hs) && int.TryParse(hs, out int h) && h > 0)
            {
                height = h;
                return true;
            }
            return false;
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

                var ba = Api.World.BlockAccessor;
                Block currentBlock = ba.GetFluid(Pos);
                if (currentBlock?.Code == null || currentBlock.Variant == null) 
                {
                    MarkDirty(true);
                    return;
                }

                if(!currentBlock.Variant.TryGetValue("height", out string hs) || !int.TryParse(hs, out int oldHeight)) return;

                if (newHeight != oldHeight) ChangeBlockHeight(currentBlock, newHeight);

                MarkDirty(true);
            }
        }

        public void ChangeFluid(int blockId, bool keepVolume = true)
        {
            var blockAccessor = Api.World.BlockAccessor;
            blockAccessor.SetFluid(blockId, Pos);
            if(keepVolume && blockAccessor.GetBlockEntity(Pos) is BlockEntityWellWaterData newBE)
            {
                newBE.Volume = Volume;
                newBE.MarkDirty();
            }
        }

        private static int GetHeightForVolume(int vol) => Math.Min(7, (vol - 1) / 10 + 1);
        
        private static int GetVolumeForHeight(int height) => height * 10 - 9;

        private void ChangeBlockHeight(Block currentBlock, int newHeight)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (currentBlock?.Code == null || currentBlock.Variant == null) return;

            Block newBlock = Api.World.GetBlock(currentBlock.CodeWithVariant("height", newHeight.ToString()));

            if (newBlock is null || newBlock == currentBlock) return;

            ChangeFluid(newBlock.BlockId);
            NotifyNeighborsOfHeightChange();
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

            var ba = Api.World.BlockAccessor;
            if (ba.GetBlockEntity(Pos) != this) return;

            var fluid = ba.GetFluid(Pos);
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
                var ba = Api.World.BlockAccessor;
                Block currentBlock = ba.GetFluid(Pos);
                if (IsValidNaturalWellWaterBlock(currentBlock))
                {
                    ChangeFluid(0, false);
                }
            }
        }
        
        private static bool IsValidNaturalWellWaterBlock(Block block)
        {
            if (!WellBlockUtils.IsOurWellwater(block) || block.Variant == null) return false;
            if (block.Variant.TryGetValue("createdBy", out string createdBy) && createdBy.Equals("natural", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private void CheckDeadEntityContamination(Block currentBlock)
        {
            if (Api.Side != EnumAppSide.Server || !WellBlockUtils.IsOurWellwater(currentBlock) || !IsClean(currentBlock, allowMuddy: true)) return;
            string ourPath = currentBlock.Code.Path;
            bool isFresh = ourPath.Contains("fresh");
            bool isSalt = ourPath.Contains("salt");

            if (!isFresh && !isSalt) return;

            Cuboidf[] collBoxes = currentBlock.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
            if (collBoxes == null || collBoxes.Length == 0)
            {
                collBoxes = [ Cuboidf.Default() ];
            }

            var nearbyEntities = Api.World.GetEntitiesAround(
                Pos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f,
                1.5f,
                (Entity e) => e is EntityAgent
            );

            foreach (Cuboidf box in collBoxes)
            {
                Vec3d blockMin = new(Pos.X + box.X1, Pos.Y + box.Y1, Pos.Z + box.Z1);
                Vec3d blockMax = new(Pos.X + box.X2, Pos.Y + box.Y2, Pos.Z + box.Z2);

                foreach (Entity entity in nearbyEntities)
                {
                    if (entity is not EntityAgent agent) continue;
                    var eMin = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X1, agent.CollisionBox.Y1, agent.CollisionBox.Z1);
                    var eMax = agent.ServerPos.XYZ.AddCopy(agent.CollisionBox.X2, agent.CollisionBox.Y2, agent.CollisionBox.Z2);

                    bool intersects =
                        eMin.X <= blockMax.X && eMax.X >= blockMin.X &&
                        eMin.Y <= blockMax.Y && eMax.Y >= blockMin.Y &&
                        eMin.Z <= blockMax.Z && eMax.Z >= blockMin.Z;

                    if (!intersects) continue;

                    if (!agent.Alive)
                    {
                        Pollute(currentBlock, "tainted");
                        return;
                    }
                }
            }
        }

        private void CheckPoisonedItemContamination(Block currentBlock)
        {
            if (Api.Side != EnumAppSide.Server || !IsClean(currentBlock, allowMuddy: true)) return;

            Cuboidf[] collBoxes = currentBlock.GetCollisionBoxes(Api.World.BlockAccessor, Pos);
            if (collBoxes == null || collBoxes.Length == 0)
            {
                collBoxes = [ Cuboidf.Default() ];
            }

            var nearbyEntities = Api.World.GetEntitiesAround(
                Pos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f,
                1.5f,
                (Entity e) => e is EntityItem
            );

            foreach (Cuboidf box in collBoxes)
            {
                Vec3d blockMin = new(Pos.X + box.X1, Pos.Y + box.Y1, Pos.Z + box.Z1);
                Vec3d blockMax = new(Pos.X + box.X2, Pos.Y + box.Y2, Pos.Z + box.Z2);

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
                            Pollute(currentBlock, "poisoned");
                            return;
                        }
                    }
                }
            }
        }

        private static bool IsClean(Block block, bool allowMuddy = false)
        {
            var pollution = block.Variant["pollution"];
            return string.IsNullOrEmpty(pollution) || pollution == "clean" || (allowMuddy && pollution == "muddy");
        }

        private void CheckNeighborContamination(Block currentBlock)
        {
            if (Api.Side != EnumAppSide.Server || currentBlock?.Code == null || !IsClean(currentBlock, allowMuddy: true)) return;
            var ba = Api.World.BlockAccessor;

            var neighborPos = Pos.Copy();
            var sidesToCheck = BlockFacing.ALLFACES;
            for (int i = 0; i < sidesToCheck.Length; i++)
            {
                sidesToCheck[i].IterateThruFacingOffsets(neighborPos);

                Block neighborBlock = ba.GetFluid(neighborPos);
                if (!WellBlockUtils.IsOurWellwater(neighborBlock)) continue;

                if (IsClean(neighborBlock, allowMuddy: true)) continue;

                var type = currentBlock.Variant["type"] == "salt" ? "salt" : neighborBlock.Variant["type"];
                AssetLocation newBlockCode = new("hydrateordiedrate", $"wellwater-{type}-{neighborBlock.Variant["pollution"]}-{currentBlock.Variant["createdBy"]}-{currentBlock.Variant["flow"]}-{currentBlock.Variant["height"]}");

                Block newBlock = Api.World.GetBlock(newBlockCode);
                if (newBlock == null || newBlock == currentBlock) return;

                ChangeFluid(newBlock.BlockId);
                return;
            }
        }

        private void TryTransferVolumeVertically()
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (volume <= 0) DeleteBlockAndEntity();
            if (PromoteBelowIfSpreading()) return;
            foreach (var entity in GetWellWaterEntitiesBelow())
            {
                int transferableVolume = Math.Min(volume, MaxVolume - entity.Volume); //TODO this doesn't respect the override on muddy wellwater capacity in GetMaxVolumeForWaterType...?
                if (transferableVolume > 0)
                {
                    Volume -= transferableVolume;
                    entity.Volume += transferableVolume;
                }

                if (volume <= 0) break;
            }
            
            if (volume <= 0) DeleteBlockAndEntity();
        }
        
        private bool PromoteBelowIfSpreading()
        {
            var ba = Api.World.BlockAccessor;
            var belowPos = Pos.DownCopy();
            Block below = ba.GetFluid(belowPos);

            if (!WellBlockUtils.IsOurWellwater(below) || below?.Variant == null) return false;
            if (!below.Variant.TryGetValue("createdBy", out string cb) || cb == "natural") return false;

            string type = below.Variant["type"];
            string pollution = below.Variant["pollution"];
            string flow = below.Variant["flow"];
            string height = below.Variant["height"];

            var naturalCode = new AssetLocation("hydrateordiedrate",
                $"wellwater-{type}-{pollution}-natural-{flow}-{height}");

            Block naturalBlock = Api.World.GetBlock(naturalCode);
            if (naturalBlock == null) return false;

            ba.SetFluid(naturalBlock.BlockId, belowPos);

            if(ba.GetBlockEntity(belowPos) is BlockEntityWellWaterData be)
            {
                be.Volume = volume;
                be.MarkDirty(true);
            }
            Volume = 0;
            return true;
        }

        
        private IEnumerable<BlockEntityWellWaterData> GetWellWaterEntitiesBelow()
        {
            var ba = Api.World.BlockAccessor;
            BlockPos currentPos = Pos.DownCopy();

            while (true)
            {
                Block currentBlock = ba.GetFluid(currentPos);
                if (!WellBlockUtils.IsOurWellwater(currentBlock)) break;
                if (ba.GetBlockEntity(currentPos) is BlockEntityWellWaterData wellWaterEntity) yield return wellWaterEntity;

                currentPos.Y--;
            }
        }

        private void Pollute(Block currentBlock, string pollution)
        {
            if (Api.Side != EnumAppSide.Server || !IsClean(currentBlock, allowMuddy: true)) return;
            AssetLocation newBlockCode = currentBlock.CodeWithVariant("pollution", pollution);

            Block newBlock = Api.World.GetBlock(newBlockCode);
            if (newBlock == null || newBlock == currentBlock) return;

            ChangeFluid(newBlock.Id);
        }

        public override void OnExchanged(Block block)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                Api.Event.RegisterCallback((dt) =>
                {
                    var ba = Api.World.BlockAccessor;
                    var fluid = ba.GetFluid(Pos);

                    if (!WellBlockUtils.IsOurWellwater(fluid))
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

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
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
