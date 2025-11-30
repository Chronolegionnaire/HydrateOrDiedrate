using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough;

public class BlockWateringTroughDoubleBlock : BlockTroughBase
{
  public override void OnLoaded(ICoreAPI api)
  {
    if (this.Variant["part"] == "large-feet")
      this.RootOffset.Set(BlockFacing.FromCode(this.Variant["side"]).Opposite.Normali);
    base.OnLoaded(api);
    this.init();
  }

  public BlockFacing OtherPartFacing()
  {
    BlockFacing blockFacing = BlockFacing.FromCode(this.Variant["side"]);
    if (this.Variant["part"] == "large-feet")
      blockFacing = blockFacing.Opposite;
    return blockFacing;
  }

  public BlockPos OtherPartPos(BlockPos pos)
  {
    BlockFacing facing = BlockFacing.FromCode(this.Variant["side"]);
    if (this.Variant["part"] == "large-feet")
      facing = facing.Opposite;
    return pos.AddCopy(facing);
  }

  public override bool TryPlaceBlock(
    IWorldAccessor world,
    IPlayer byPlayer,
    ItemStack itemstack,
    BlockSelection blockSel,
    ref string failureCode)
  {
    if (!this.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
      return false;
    BlockFacing[] blockFacingArray = Block.SuggestedHVOrientation(byPlayer, blockSel);
    BlockPos blockPos = blockSel.Position.AddCopy(blockFacingArray[0]);
    IWorldAccessor world1 = world;
    IPlayer byPlayer1 = byPlayer;
    BlockSelection blockSel1 = new BlockSelection();
    blockSel1.Position = blockPos;
    blockSel1.Face = blockSel.Face;
    ref string local = ref failureCode;
    if (!this.CanPlaceBlock(world1, byPlayer1, blockSel1, ref local))
      return false;
    string code1 = blockFacingArray[0].Opposite.Code;
    Block block = world.BlockAccessor.GetBlock(this.CodeWithVariants(new string[2]
    {
      "part",
      "side"
    }, new string[2] { "large-head", code1 }));
    IWorldAccessor world2 = world;
    IPlayer byPlayer2 = byPlayer;
    BlockSelection blockSel2 = new BlockSelection();
    blockSel2.Position = blockPos;
    blockSel2.Face = blockSel.Face;
    ItemStack byItemStack = itemstack;
    block.DoPlaceBlock(world2, byPlayer2, blockSel2, byItemStack);
    AssetLocation code2 = this.CodeWithVariants(new string[2]
    {
      "part",
      "side"
    }, new string[2] { "large-feet", code1 });
    world.BlockAccessor.GetBlock(code2).DoPlaceBlock(world, byPlayer, blockSel, itemstack);
    return true;
  }

  public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
  {
    if (blockSel == null)
      return base.OnBlockInteractStart(world, byPlayer, blockSel);

    bool flag = world.BlockAccessor
                  .GetBlockEntity(blockSel.Position.AddCopy(this.RootOffset)) is BlockEntityWateringTrough be
                && (be.OnInteractWithLiquid(byPlayer, blockSel) || be.OnInteract(byPlayer, blockSel));

    if (!flag || world.Side != EnumAppSide.Client) return flag;

    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
    return true;
  }

  public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
  {
    BlockFacing facing = BlockFacing.FromCode(this.Variant["side"]);
    if (this.Variant["part"] == "large-feet")
      facing = facing.Opposite;

    BlockPos otherPos = pos.AddCopy(facing);
    Block block = world.BlockAccessor.GetBlock(otherPos);

    if (block is BlockWateringTroughDoubleBlock && block.Variant["part"] != this.Variant["part"])
    {
      if (this.Variant["part"] == "large-feet" &&
          world.BlockAccessor.GetBlockEntity(otherPos) is BlockEntityWateringTrough be)
      {
        be.OnBlockBroken(null);
      }

      world.BlockAccessor.SetBlock(0, otherPos);
    }

    base.OnBlockRemoved(world, pos);
  }

  public override BlockDropItemStack[] GetDropsForHandbook(
    ItemStack handbookStack,
    IPlayer forPlayer)
  {
    return this.GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
  }

  public override ItemStack[] GetDrops(
    IWorldAccessor world,
    BlockPos pos,
    IPlayer byPlayer,
    float dropQuantityMultiplier = 1f)
  {
    return new ItemStack[1]
    {
      new ItemStack(world.BlockAccessor.GetBlock(this.CodeWithVariants(new string[2]
      {
        "part",
        "side"
      }, new string[2] { "large-head", "north" })))
    };
  }

  public override AssetLocation GetRotatedBlockCode(int angle)
  {
    int index = GameMath.Mod(BlockFacing.FromCode(this.Variant["side"]).HorizontalAngleIndex - angle / 90, 4);
    return this.CodeWithParts(BlockFacing.HORIZONTALS_ANGLEORDER[index].Code);
  }

  public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
  {
    return new ItemStack(world.BlockAccessor.GetBlock(this.CodeWithVariants(new string[2]
    {
      "part",
      "side"
    }, new string[2] { "large-head", "north" })));
  }

  public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
  {
    BlockFacing blockFacing = BlockFacing.FromCode(this.Variant["side"]);
    return blockFacing.Axis == axis ? this.CodeWithParts(blockFacing.Opposite.Code) : this.Code;
  }

  public override void GetHeldItemInfo(
    ItemSlot inSlot,
    StringBuilder dsc,
    IWorldAccessor world,
    bool withDebugInfo)
  {
    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
  }

  public override string GetPlacedBlockInfo(
    IWorldAccessor world,
    BlockPos pos,
    IPlayer forPlayer)
  {
    if (this.Variant["part"] == "large-feet")
    {
      BlockFacing opposite = BlockFacing.FromCode(this.Variant["side"]).Opposite;
      pos = pos.AddCopy(opposite);
    }

    if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityWateringTrough blockEntity))
      return base.GetPlacedBlockInfo(world, pos, forPlayer);
    StringBuilder dsc = new StringBuilder();
    dsc.AppendLine(string.Format("Water: {0:0.#}/{1:0.#} L", blockEntity.WaterLitres, blockEntity.MaxWaterLitres));
    blockEntity.GetBlockInfo(forPlayer, dsc);
    return dsc.ToString();
  }

  public override int GetRandomColor(
    ICoreClientAPI capi,
    BlockPos pos,
    BlockFacing facing,
    int rndIndex = -1)
  {
    CompositeTexture compositeTexture;
    if (this.Textures.TryGetValue("aged", out compositeTexture))
      capi.BlockTextureAtlas.GetRandomColor(compositeTexture.Baked.TextureSubId, rndIndex);
    return base.GetRandomColor(capi, pos, facing, rndIndex);
  }

  public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
  {
    CompositeTexture compositeTexture;
    return this.Textures.TryGetValue("aged", out compositeTexture)
      ? capi.BlockTextureAtlas.GetAverageColor(compositeTexture.Baked.TextureSubId)
      : base.GetColorWithoutTint(capi, pos);
  }
}
