using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.wellwater
{
	public class BlockWellWaterfall : BlockForFluidsLayer
	{
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if (api.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).Settings.Int.AddWatcher("particleLevel", new OnSettingsChanged<int>(this.OnParticleLevelChanged));
				this.OnParticleLevelChanged(0);
			}
			this.isBoiling = this.HasBehavior<BlockBehaviorSteaming>(false);
		}
		private void OnParticleLevelChanged(int newValue)
		{
			this.particleQuantity = 0.2f * (float)(this.api as ICoreClientAPI).Settings.Int["particleLevel"] / 100f;
		}
		public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
		{
			for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
			{
				BlockFacing facing = BlockFacing.HORIZONTALS[i];
				if (world.BlockAccessor.GetBlockOnSide(pos, facing, 0).Replaceable >= 6000 && !world.BlockAccessor.GetBlockOnSide(pos, facing, 2).IsLiquid())
				{
					return 1f;
				}
			}
			return 0f;
		}
		public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
		{
			isWindAffected = true;
			return pos.Y >= 2 && world.BlockAccessor.GetBlockBelow(pos, 1, 0).Replaceable >= BlockWaterfall.ReplacableThreshold;
		}
		public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
		{
			if (this.ParticleProperties != null && this.ParticleProperties.Length > 0)
			{
				for (int i = 0; i < this.ParticleProperties.Length; i++)
				{
					if (this.api.World.Rand.NextDouble() <= (double)this.particleQuantity)
					{
						BlockFacing facing = BlockFacing.HORIZONTALS[i % BlockFacing.HORIZONTALS.Length];
						if (!manager.BlockAccess.GetBlockOnSide(pos, facing, 0).SideSolid[facing.Opposite.Index] 
						    && manager.BlockAccess.GetBlockOnSide(pos, facing, 2).BlockId == 0)
						{
							AdvancedParticleProperties bps = this.ParticleProperties[i];
							bps.basePos.X = (double)((float)pos.X + this.TopMiddlePos.X);
							bps.basePos.Y = (double)pos.Y;
							bps.basePos.Z = (double)((float)pos.Z + this.TopMiddlePos.Z);
							bps.WindAffectednes = windAffectednessAtPos * 0.25f;
							bps.HsvaColor[3].avg = 180f * Math.Min(1f, secondsTicking / 7f);
							bps.Quantity.avg = 1f;
							bps.Velocity[1].avg = -0.4f;
							bps.Velocity[0].avg = GlobalConstants.CurrentWindSpeedClient.X * windAffectednessAtPos;
							bps.Velocity[2].avg = GlobalConstants.CurrentWindSpeedClient.Z * windAffectednessAtPos;
							bps.Size.avg = 0.05f;
							bps.Size.var = 0f;
							bps.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.8f);
							manager.Spawn(bps);
						}
					}
				}
			}
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
		private float particleQuantity = 0.2f;
		private bool isBoiling;
		public static int ReplacableThreshold = 5000;
	}
}
