using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.wellwater
{
	public class BlockWellWater : BlockForFluidsLayer, IBlockFlowing
	{
		public string Flow { get; set; }
		public Vec3i FlowNormali { get; set; }
		public bool IsLava
		{
			get
			{
				return false;
			}
		}
		public int Height { get; set; }
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			string f = this.Variant["flow"];
			this.Flow = ((f != null) ? string.Intern(f) : null);
			Vec3i vec3i;
			if (this.Flow == null)
			{
				vec3i = null;
			}
			else
			{
				Cardinal cardinal = Cardinal.FromInitial(this.Flow);
				vec3i = ((cardinal != null) ? cardinal.Normali : null);
			}
			this.FlowNormali = vec3i;
			string h = this.Variant["height"];
			this.Height = ((h != null) ? h.ToInt(0) : 7);
			this.freezable = this.Flow == "still" && this.Height == 7;
			if (this.Attributes != null)
			{
				this.freezable &= this.Attributes["freezable"].AsBool(true);
				this.iceBlock = api.World.GetBlock(AssetLocation.Create(this.Attributes["iceBlockCode"].AsString("lakeice"), this.Code.Domain));
				this.freezingPoint = this.Attributes["freezingPoint"].AsFloat(-4f);
			}
			else
			{
				this.iceBlock = api.World.GetBlock(AssetLocation.Create("lakeice", this.Code.Domain));
			}
			this.isBoiling = this.HasBehavior<BlockBehaviorSteaming>(false);
		}
		public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
		{
			return (float)((world.BlockAccessor.GetBlockId(pos.X, pos.Y + 1, pos.Z) == 0 && world.BlockAccessor.IsSideSolid(pos.X, pos.Y - 1, pos.Z, BlockFacing.UP)) ? 1 : 0);
		}
		public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
		{
			BlockBehavior[] blockBehaviors = this.BlockBehaviors;
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				blockBehaviors[i].OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
			}
		}
		public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
		{
			extra = null;
			if (!GlobalConstants.MeltingFreezingEnabled)
			{
				return false;
			}
			if (this.freezable && offThreadRandom.NextDouble() < 0.6 && world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y)
			{
				BlockPos nPos = pos.Copy();
				for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
				{
					BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(nPos);
					if ((world.BlockAccessor.GetBlock(nPos, 2) is BlockLakeIce || world.BlockAccessor.GetBlock(nPos).Replaceable < 6000) && world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, this.api.World.Calendar.TotalDays).Temperature <= this.freezingPoint)
					{
						return true;
					}
				}
			}
			return false;
		}
		public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
		{
			world.BlockAccessor.SetBlock(this.iceBlock.Id, pos, 2);
		}
		public override void OnGroundIdle(EntityItem entityItem)
		{
			entityItem.Die(EnumDespawnReason.Removed, null);
			if (entityItem.World.Side == EnumAppSide.Server)
			{
				Vec3d pos = entityItem.ServerPos.XYZ;
				WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(entityItem.Itemstack);
				float litres = (float)entityItem.Itemstack.StackSize / props.ItemsPerLitre;
				entityItem.World.SpawnCubeParticles(pos, entityItem.Itemstack, 0.75f, Math.Min(100, (int)(2f * litres)), 0.45f, null, null);
				entityItem.World.PlaySoundAt(new AssetLocation("sounds/environment/smallsplash"), (double)((float)pos.X), (double)((float)pos.Y), (double)((float)pos.Z), null, true, 32f, 1f);
				BlockEntityFarmland bef = this.api.World.BlockAccessor.GetBlockEntity(pos.AsBlockPos) as BlockEntityFarmland;
				if (bef != null)
				{
					bef.WaterFarmland((float)this.Height / 6f, false);
					bef.MarkDirty(true, null);
				}
			}
			base.OnGroundIdle(entityItem);
		}
		public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
		{
			Block oldBlock = world.BlockAccessor.GetBlock(blockSel.Position);
			if (oldBlock.DisplacesLiquids(world.BlockAccessor, blockSel.Position) && !oldBlock.IsReplacableBy(this))
			{
				failureCode = "notreplaceable";
				return false;
			}
			bool result = true;
			if (byPlayer != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
				failureCode = "claimed";
				return false;
			}
			bool preventDefault = false;
			foreach (BlockBehavior blockBehavior in this.BlockBehaviors)
			{
				EnumHandling handled = EnumHandling.PassThrough;
				bool behaviorResult = blockBehavior.CanPlaceBlock(world, byPlayer, blockSel, ref handled, ref failureCode);
				if (handled != EnumHandling.PassThrough)
				{
					result = result && behaviorResult;
					preventDefault = true;
				}
				if (handled == EnumHandling.PreventSubsequent)
				{
					return result;
				}
			}
			return !preventDefault || result;
		}
		public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
		{
			if (creatureType == EnumAICreatureType.SeaCreature && !this.isBoiling)
			{
				return 0f;
			}
			if (!this.isBoiling || creatureType == EnumAICreatureType.HeatProofCreature)
			{
				return 5f;
			}
			return 99999f;
		}
		private bool freezable;
		private Block iceBlock;
		private float freezingPoint = -4f;
		private bool isBoiling;
	}
}
