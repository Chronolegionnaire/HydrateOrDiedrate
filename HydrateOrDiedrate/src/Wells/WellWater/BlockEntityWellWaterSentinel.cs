using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BlockEntityWellWaterSentinel : BlockEntity
    {
        private long tickId;
        private float ageSeconds;
        private bool everSawSpring;
        private const float PlayerGraceSec = 2.0f;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;
            RegisterGameTickListener(ServerTick, 1000);
        }

        private void ServerTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            ageSeconds += dt;

            bool hasSpring = HasGoverningSpringBelow(Pos) is not null;

            everSawSpring |= hasSpring;

            if (hasSpring)
            {
                return;
            }

            if (!everSawSpring && ageSeconds <= PlayerGraceSec)
            {
                DeleteSelf(keepFluid: true);
                return;
            }
            DeleteSelf(keepFluid: false);
        }

        private BlockEntityWellSpring HasGoverningSpringBelow(BlockPos start)
        {
            var ba = Api.World.BlockAccessor;
            var scan = start.DownCopy();
            for (int i = 0; i < 64; i++)
            {
                var be = ba.GetBlockEntity<BlockEntityWellSpring>(scan);
                if(be is not null) return be;
                if (!WellBlockUtils.SolidAllows(ba.GetSolid(scan))) break;
                scan.Y--;
            }

            return null;
        }

        public BlockEntityWellSpring TryGetGoverningSpring() => HasGoverningSpringBelow(Pos);

        private void DeleteSelf(bool keepFluid)
        {
            if (Api.Side != EnumAppSide.Server) return;

            Api.World.BlockAccessor.RemoveBlockEntity(Pos);

            if (!keepFluid)
            {
                var ba = Api.World.BlockAccessor;
                var fluid = ba.GetFluid(Pos);
                ba.SetFluid(0, Pos);
                ba.TriggerNeighbourBlockUpdate(Pos);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("ww_ageSec", ageSeconds);
            tree.SetBool("ww_everSawSpring", everSawSpring);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            ageSeconds = tree.GetFloat("ww_ageSec");
            everSawSpring = tree.GetBool("ww_everSawSpring");
        }
    }
}
