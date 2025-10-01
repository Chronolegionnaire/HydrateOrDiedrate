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
            tickId = RegisterGameTickListener(Tick, 1000);
        }

        private void Tick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (Api.World.BlockAccessor.GetBlockEntity(Pos) != this) return;

            ageSeconds += dt;

            var ba = Api.World.BlockAccessor;
            var fluid = ba.GetFluid(Pos);
            if (!WellBlockUtils.IsOurWellwater(fluid))
            {
                DeleteSelf(keepFluid: true);
                return;
            }
            bool hasSpring = HasGoverningSpringBelow(Pos);

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

        private bool HasGoverningSpringBelow(BlockPos start)
        {
            var ba = Api.World.BlockAccessor;
            var scan = start.DownCopy();
            for (int i = 0; i < 64; i++)
            {
                var be = ba.GetBlockEntity(scan);
                if (be is WellWater.BlockEntityWellSpring) return true;
                if (!WellBlockUtils.SolidAllows(ba.GetSolid(scan))) break;
                scan.Y--;
            }
            return false;
        }

        public bool TryGetGoverningSpring(out BlockPos springPos, out BlockEntityWellSpring spring)
        {
            springPos = null;
            spring = null;
            if (Api == null) return false;
            var ba = Api.World.BlockAccessor;

            var scan = Pos.DownCopy();
            for (int i = 0; i < 64; i++)
            {
                var be = ba.GetBlockEntity(scan);
                if (be is BlockEntityWellSpring ws)
                {
                    springPos = scan.Copy();
                    spring = ws;
                    return true;
                }
                if (!WellBlockUtils.SolidAllows(ba.GetSolid(scan))) break;
                scan.Y--;
            }
            return false;
        }

        private void DeleteSelf(bool keepFluid)
        {
            if (Api.Side != EnumAppSide.Server) return;

            UnregisterGameTickListener(tickId);
            Api.World.BlockAccessor.RemoveBlockEntity(Pos);

            if (!keepFluid)
            {
                var ba = Api.World.BlockAccessor;
                var fluid = ba.GetFluid(Pos);
                if (WellBlockUtils.IsOurWellwater(fluid))
                {
                    ba.SetFluid(0, Pos);
                    ba.TriggerNeighbourBlockUpdate(Pos);
                }
            }
        }

        public override void OnBlockRemoved()
        {
            if (Api?.Side == EnumAppSide.Server) UnregisterGameTickListener(tickId);
            base.OnBlockRemoved();
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
