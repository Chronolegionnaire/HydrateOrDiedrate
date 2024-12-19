using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.wellwater
{
	public class BlockWellWaterflowing : BlockForFluidsLayer
	{
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
    
			if (api.Side == EnumAppSide.Client)
			{
				(api as ICoreClientAPI).Settings.Int.AddWatcher("particleLevel", new OnSettingsChanged<int>(this.OnParticelLevelChanged));
				this.OnParticelLevelChanged(0);
			}
			if (this.ParticleProperties != null && this.ParticleProperties.Length > 0)
			{
				this.ParticleProperties[0].SwimOnLiquid = true;
			}
			else
			{
			}

			this.isBoiling = this.HasBehavior<BlockBehaviorSteaming>(false);
		}
		private void OnParticelLevelChanged(int newValue)
		{
			this.particleQuantity = 0.4f * (float)(this.api as ICoreClientAPI).Settings.Int["particleLevel"] / 100f;
		}
		public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
		{
			if (world.BlockAccessor.GetBlockAbove(pos, 1, 0).Replaceable >= 6000 && !world.BlockAccessor.GetBlockAbove(pos, 1, 2).IsLiquid())
			{
				return 1f;
			}
			return 0f;
		}
		public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
		{
			if (this.api == null || this.api.World == null) return;

			if (this.ParticleProperties == null || this.ParticleProperties.Length == 0 || this.ParticleProperties[0] == null)
			{
				return;
			}

			AdvancedParticleProperties bps = this.ParticleProperties[0];

			if (bps.Velocity == null || bps.Velocity.Length < 3 || bps.PosOffset == null || bps.PosOffset.Length < 2)
			{
				return;
			}

			if (base.PushVector == null)
			{
				return;
			}
			
			if (this.BlockBehaviors != null)
			{
				for (int i = 0; i < this.BlockBehaviors.Length; i++)
				{
					this.BlockBehaviors[i]?.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
				}
			}

			if (this.api.World.Rand.NextDouble() > this.particleQuantity) return;
			
			bps.basePos.X = pos.X;
			bps.basePos.Y = pos.Y;
			bps.basePos.Z = pos.Z;

			bps.Velocity[0].avg = (float)(base.PushVector.X * 500f);
			bps.Velocity[1].avg = (float)(base.PushVector.Y * 1000f);
			bps.Velocity[2].avg = (float)(base.PushVector.Z * 500f);

			bps.GravityEffect.avg = 0.5f;
			bps.HsvaColor[3].avg = 180f * Math.Min(1f, secondsTicking / 7f);
			bps.Quantity.avg = 1f;
			bps.PosOffset[1].avg = 0.125f;
			bps.PosOffset[1].var = (float)this.LiquidLevel / 8f * 0.75f;
			bps.SwimOnLiquid = true;
			bps.Size.avg = 0.05f;
			bps.Size.var = 0f;
			bps.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, 0.8f);

			manager.Spawn(bps);
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
	}
}
