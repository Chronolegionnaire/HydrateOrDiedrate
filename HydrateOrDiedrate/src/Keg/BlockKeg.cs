using HydrateOrDiedrate.Config;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg;

//TODO: The 2 keg blocks really should be variants of each other to better link them together (just don't feel like touching the remapper right now)
public class BlockKeg : BlockLiquidContainerBase
{
    public const float snapAngle = 0.3926991f;
    public override float CapacityLitres => ModConfig.Instance.Containers.KegCapacityLitres;
    
    public override bool AllowHeldLiquidTransfer => false;
    public override bool CanDrinkFrom => false; 
    
    private float resistance = 100f;
    
    //TODO: this field is used very poorly and will have weird effects if 2 players are both interacting with different blocks (since all Keg Blocks share this class instance)
    private float playNextSound = 0.7f;
    private bool choppingComplete = false;
    
    //Note: CanDrinkFrom does not fully prevent you from drinking, unsure why but this is why we are overiding the Eat methods 
    protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1) => handling = EnumHandHandling.PreventDefaultAction;
    
    protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null) => false;
    
    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        //Not edible
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        ItemSlot offHandSlot = byPlayer.Entity.LeftHandItemSlot;

        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKeg blockEntityKeg)
        {
            ItemStack heldItem = activeHotbarSlot?.Itemstack;
            ItemStack offHandItem = offHandSlot?.Itemstack;

            if (heldItem?.Collectible?.Tool == EnumTool.Axe)
            {
                choppingComplete = false;
                playNextSound = 0.7f;

                StartAxeAnimation(byPlayer);
                PlayChoppingSound(world, byPlayer, blockSel);
                return true;
            }

            if (heldItem?.Collectible is ItemKegTap && offHandItem?.Collectible?.Code?.Path?.Contains("hammer") == true)
            {
                choppingComplete = false;
                playNextSound = 0.7f;
                Block currentBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                if (currentBlock.Code.Path != "kegtapped")
                {
                    StartTappingAnimation(byPlayer);
                    PlayTappingSound(world, byPlayer, blockSel);
                    return true;
                }
            }

            return HandleLiquidInteraction(world, byPlayer, blockSel, blockEntityKeg, heldItem);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    { 
        //TODO why these overrides? because of chopping?
        if (choppingComplete) return false;

        BlockEntityKeg kegEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityKeg;
        ItemStack heldItem = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
        if (heldItem?.Collectible?.Tool == EnumTool.Axe)
        {
            if (secondsUsed >= playNextSound)
            {
                PlayChoppingSound(world, byPlayer, blockSel);
                playNextSound += 0.65f;
            }

            if (secondsUsed >= (resistance / 50) - 0.1f)
            {
                world.RegisterCallback((dt) =>
                {
                    StopAxeAnimation(byPlayer);
                    world.BlockAccessor.ExchangeBlock(0, blockSel.Position);
                    world.BlockAccessor.RemoveBlockEntity(blockSel.Position);
                    DropKegItems(world, blockSel.Position, this.Code.Path == "kegtapped");
                    choppingComplete = true;
                }, 500);

                return false;
            }

            return true;
        }
        else if (heldItem?.Collectible is ItemKegTap && byPlayer.Entity.LeftHandItemSlot.Itemstack?.Collectible?.Code?.Path?.Contains("hammer") == true)
        {
            if (secondsUsed >= playNextSound)
            {
                PlayTappingSound(world, byPlayer, blockSel);
                playNextSound += 0.65f;
            }

            if (secondsUsed >= (resistance / 50) - 0.1f)
            {
                world.RegisterCallback((dt) =>
                {
                    StopTappingAnimation(byPlayer);
                    Block tappedKegBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:kegtapped"));
                    ITreeAttribute tree = new TreeAttribute();
                    kegEntity.ToTreeAttributes(tree);
                    world.BlockAccessor.ExchangeBlock(tappedKegBlock.BlockId, blockSel.Position);

                    BlockEntity newBlockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
                    if (newBlockEntity is BlockEntityKeg newBlockEntityKeg)
                    {
                        newBlockEntityKeg.FromTreeAttributes(tree, world);
                        newBlockEntityKeg.MarkDirty(true);
                    }
                    ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                    if (activeHotbarSlot.Itemstack?.Collectible is ItemKegTap)
                    {
                        activeHotbarSlot.TakeOut(1);
                        activeHotbarSlot.MarkDirty();
                    }

                    choppingComplete = true;
                }, 500);
                return false;
            }

            return true;
        }

        return false;
    }

    public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (!choppingComplete)
        {
            StopAxeAnimation(byPlayer);
            StopTappingAnimation(byPlayer);
        }
    }

    public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
    {
        if (!choppingComplete)
        {
            StopAxeAnimation(byPlayer);
            StopTappingAnimation(byPlayer);
        }

        return true;
    }

    //TODO why not in the GetDrops method?
    private void DropKegItems(IWorldAccessor world, BlockPos position, bool isTappedKeg)
    {
        var random = world.Rand;
        for (int i = 0; i < 2; i++)
        {
            if (random.NextDouble() < ModConfig.Instance.Containers.KegIronHoopDropChance)
            {
                world.SpawnItemEntity(new ItemStack(world.GetItem(new AssetLocation("game","hoop-iron"))), position.ToVec3d());
            }
        }
        if (isTappedKeg && random.NextDouble() < ModConfig.Instance.Containers.KegTapDropChance)
        {
            world.SpawnItemEntity(new ItemStack(world.GetItem(new AssetLocation("hydrateordiedrate", "kegtap"))), position.ToVec3d());
        }
    }

    private bool HandleLiquidInteraction(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, BlockEntityKeg blockEntityKeg, ItemStack heldItem)
    {
        if (heldItem is null || heldItem.Collectible is null) return false;

        if (heldItem.Collectible is ILiquidInterface liquidItem)
        {
            Block kegBlock = world.BlockAccessor.GetBlock(blockSel.Position);
            bool isTappedKeg = kegBlock.Code.Path == "kegtapped";

            if (liquidItem is ILiquidSource)
            {
                ItemStack liquidInHand = liquidItem.GetContent(heldItem);
                ItemStack contentInKeg = blockEntityKeg.GetContent();
                WaterTightContainableProps contentProps = GetContainableProps(contentInKeg);
                
                float currentLitres = contentInKeg != null && contentProps != null
                    ? (float)contentInKeg.StackSize / contentProps.ItemsPerLitre
                    : 0;

                bool isEmpty = currentLitres <= 0;
                if (liquidInHand != null && (contentInKeg == null ||
                                             liquidInHand.Collectible.Equals(liquidInHand, contentInKeg,
                                                 GlobalConstants.IgnoredStackAttributes)))
                {
                    return base.OnBlockInteractStart(world, byPlayer, blockSel);
                }
            }

            if (liquidItem is ILiquidSink)
            {
                ItemStack contentInKeg = blockEntityKeg.GetContent();
                WaterTightContainableProps contentProps = GetContainableProps(contentInKeg);
                float currentLitres = contentInKeg != null && contentProps != null
                    ? (float)contentInKeg.StackSize / contentProps.ItemsPerLitre
                    : 0;
                
                if (currentLitres > 0)
                {
                    if (!isTappedKeg)
                    {
                        return true;
                    }
                    else
                    {
                        return base.OnBlockInteractStart(world, byPlayer, blockSel);
                    }
                }
            }
        }
        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool flag = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

        if (flag && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKeg blockEntityKeg)
        {
            float playerYaw = byPlayer.Entity.Pos.Yaw;
            float snappedYaw = (float)(Math.Round(playerYaw / snapAngle) * snapAngle);
            blockEntityKeg.MeshAngle = snappedYaw;
            blockEntityKeg.MarkDirty(true, null);
        }

        return flag;
    }

    private void StartAxeAnimation(IPlayer byPlayer)
    {
        var entityAnimManager = byPlayer.Entity.AnimManager;

        if (!entityAnimManager.IsAnimationActive("axechop"))
        {
            entityAnimManager?.StartAnimation(new AnimationMetaData()
            {
                Animation = "axeready",
                Code = "axeready"
            });

            entityAnimManager?.StartAnimation(new AnimationMetaData()
            {
                Animation = "axechop",
                Code = "axechop",
                BlendMode = EnumAnimationBlendMode.AddAverage,
                AnimationSpeed = 1.65f,
                HoldEyePosAfterEasein = 0.3f,
                EaseInSpeed = 500f,
                EaseOutSpeed = 500f,
                Weight = 25f,
                ElementWeight = new Dictionary<string, float>
                {
                    { "UpperArmr", 20.0f },
                    { "LowerArmr", 20.0f },
                    { "UpperArml", 20.0f },
                    { "LowerArml", 20.0f },
                    { "UpperTorso", 20.0f },
                    { "ItemAnchor", 20.0f }
                },
                ElementBlendMode = new Dictionary<string, EnumAnimationBlendMode>
                {
                    { "UpperArmr", EnumAnimationBlendMode.Add },
                    { "LowerArmr", EnumAnimationBlendMode.Add },
                    { "UpperArml", EnumAnimationBlendMode.Add },
                    { "LowerArml", EnumAnimationBlendMode.Add },
                    { "UpperTorso", EnumAnimationBlendMode.Add },
                    { "ItemAnchor", EnumAnimationBlendMode.Add }
                }
            });
        }
    }

    private void StopAxeAnimation(IPlayer byPlayer)
    {
        var entityAnimManager = byPlayer.Entity.AnimManager;

        if (entityAnimManager?.IsAnimationActive("axechop") == true)
        {
            entityAnimManager?.StopAnimation("axechop");
            entityAnimManager?.ResetAnimation("axechop");
            StopAllPlayerAnimations(byPlayer);
        }

        entityAnimManager?.StartAnimation(new AnimationMetaData()
        {
            Animation = "idle1",
            Code = "idle",
            BlendMode = EnumAnimationBlendMode.Add,
        });
    }

    private void PlayChoppingSound(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        world.PlaySoundAt(new AssetLocation("game:sounds/block/chop2"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 32f, 1f);
    }

    private void StartTappingAnimation(IPlayer byPlayer)
    {
        var entityAnimManager = byPlayer.Entity.AnimManager;
        StopTappingAnimation(byPlayer);

        if (!entityAnimManager.IsAnimationActive("chiselready"))
        {
            entityAnimManager?.StartAnimation(new AnimationMetaData()
            {
                Animation = "chiselready",
                Code = "chiselready"
            });
        }
    }

    private void StopTappingAnimation(IPlayer byPlayer)
    {
        var entityAnimManager = byPlayer.Entity.AnimManager;
        if (entityAnimManager?.IsAnimationActive("chiselready") == true)
        {
            entityAnimManager?.StopAnimation("chiselready");
            entityAnimManager?.ResetAnimation("chiselready");
            StopAllPlayerAnimations(byPlayer);
        }

        entityAnimManager?.StartAnimation(new AnimationMetaData()
        {
            Animation = "idle1",
            Code = "idle",
            BlendMode = EnumAnimationBlendMode.Add,
        });
    }

    private void PlayTappingSound(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        world.PlaySoundAt(new AssetLocation("game", "sounds/block/barrel"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, true, 32f, 1f);
    }

    private void StopAllPlayerAnimations(IPlayer byPlayer)
    {
        var entityAnimManager = byPlayer.Entity.AnimManager;

        if (entityAnimManager != null && entityAnimManager.ActiveAnimationsByAnimCode != null)
        {
            foreach (var animCode in entityAnimManager.ActiveAnimationsByAnimCode.Keys)
            {
                entityAnimManager.StopAnimation(animCode);
            }
        }

        entityAnimManager?.StartAnimation(new AnimationMetaData()
        {
            Animation = "idle1",
            Code = "idle",
            BlendMode = EnumAnimationBlendMode.Add,
        });
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        var beh = world.BlockAccessor.GetBlockEntity<BlockEntityKeg>(pos);
        if(beh is not null)
        {
            var contentCode = beh.GetContent()?.Collectible.Code;

            //Drop content if we don't drop with liquid or if the content has become rotten
            if(!ModConfig.Instance.Containers.KegDropWithLiquid || (contentCode is not null && contentCode.Domain == "game" && contentCode.Path == "rot"))
            {
                beh.DropContents(byPlayer);
            }
        }

        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }
}
