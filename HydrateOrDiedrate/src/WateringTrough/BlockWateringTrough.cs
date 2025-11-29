using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough
{
	public class BlockWateringTrough : BlockTroughBase
	{
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			base.init();
		}
		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			if (blockSel != null)
			{
				BlockPos pos = blockSel.Position;
				BlockEntityTrough betr = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityTrough;
				if (betr != null)
				{
					bool flag = betr.OnInteract(byPlayer, blockSel);
					if (flag && world.Side == EnumAppSide.Client)
					{
						(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					}
					return flag;
				}
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		public override AssetLocation GetRotatedBlockCode(int angle)
		{
			if (Math.Abs(angle) == 90 || Math.Abs(angle) == 270)
			{
				string orient = this.Variant["side"];
				return base.CodeWithVariant("side", (orient == "we") ? "ns" : "we");
			}
			return this.Code;
		}
		public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
		{
			BlockFacing facing = BlockFacing.FromCode(base.LastCodePart(0));
			if (facing.Axis == axis)
			{
				return base.CodeWithParts(facing.Opposite.Code);
			}
			return this.Code;
		}
		public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
		{
			if (base.LastCodePart(1) == "feet")
			{
				BlockFacing facing = BlockFacing.FromCode(base.LastCodePart(0)).Opposite;
				pos = pos.AddCopy(facing);
			}
			BlockEntityTrough betr = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityTrough;
			if (betr != null)
			{
				StringBuilder dsc = new StringBuilder();
				betr.GetBlockInfo(forPlayer, dsc);
				return dsc.ToString();
			}
			return base.GetPlacedBlockInfo(world, pos, forPlayer);
		}
		public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
		{
			return capi.BlockTextureAtlas.GetRandomColor(this.Textures["wood"].Baked.TextureSubId, rndIndex);
		}
		public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
		{
			int texSubId = this.Textures["wood"].Baked.TextureSubId;
			return capi.BlockTextureAtlas.GetAverageColor(texSubId);
		}
	}
}
