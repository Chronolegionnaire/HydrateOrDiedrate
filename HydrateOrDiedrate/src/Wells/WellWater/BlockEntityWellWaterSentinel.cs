using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BlockEntityWellWaterSentinel : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;
            RegisterGameTickListener(ServerTick, 1000);
        }

        private void ServerTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (HasGoverningSpringBelow(Pos) == null)
            {
                DeleteSelf(keepFluid: true);
            }
        }

        private BlockEntityWellSpring HasGoverningSpringBelow(BlockPos start)
        {
            var ba = Api.World.BlockAccessor;
            var scan = start.DownCopy();
            for (int i = 0; i < 64; i++)
            {
                var be = ba.GetBlockEntity<BlockEntityWellSpring>(scan);
                if (be != null) return be;
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
    }
}
