using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BlockEntityWellWaterSentinel : BlockEntity
    {
        private BlockPos governingSpringPos;
        private long tickListenerId = -1;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            if (governingSpringPos == null)
            {
                var be = FindNearestSpringBelow(Pos);
                if (be != null) governingSpringPos = be.Pos.Copy();
            }

            tickListenerId = RegisterGameTickListener(ServerTick, 1000);
        }

        private void ServerTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return;

            if (governingSpringPos == null)
            {
                var be = FindNearestSpringBelow(Pos);
                if (be != null)
                {
                    governingSpringPos = be.Pos.Copy();
                    MarkDirty();
                    return;
                }
            }

            if (governingSpringPos != null)
            {
                var be = Api.World.BlockAccessor.GetBlockEntity<BlockEntityWellSpring>(governingSpringPos);
                if (be == null)
                {
                    DeleteSelf(keepFluid: true);
                }
                return;
            }

            if (FindNearestSpringBelow(Pos) == null)
            {
                DeleteSelf(keepFluid: true);
            }
        }

        private BlockEntityWellSpring FindNearestSpringBelow(BlockPos start)
        {
            var ba = Api.World.BlockAccessor;
            var scan = start.DownCopy();

            for (int i = 0; i < 64; i++)
            {
                var block = ba.GetBlock(scan);
                if (block is BlockWellSpring)
                {
                    var be = ba.GetBlockEntity<BlockEntityWellSpring>(scan);
                    if (be != null) return be;
                    break;
                }

                if (!WellBlockUtils.SolidAllows(ba.GetSolid(scan))) break;
                scan.Y--;
            }
            return null;
        }

        public BlockEntityWellSpring TryGetGoverningSpring()
        {
            if (governingSpringPos == null) return null;
            return Api.World.BlockAccessor.GetBlockEntity<BlockEntityWellSpring>(governingSpringPos);
        }

        private void DeleteSelf(bool keepFluid)
        {
            if (Api.Side != EnumAppSide.Server) return;

            if (tickListenerId >= 0)
            {
                UnregisterGameTickListener(tickListenerId);
                tickListenerId = -1;
            }

            var ba = Api.World.BlockAccessor;

            ba.RemoveBlockEntity(Pos);

            if (!keepFluid)
            {
                var fluid = ba.GetFluid(Pos);
                ba.SetFluid(0, Pos);
            }

            ba.TriggerNeighbourBlockUpdate(Pos);
            MarkDirty();
        }

        #region Persistence
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (governingSpringPos != null)
            {
                var tp = new TreeAttribute();
                tp.SetInt("x", governingSpringPos.X);
                tp.SetInt("y", governingSpringPos.Y);
                tp.SetInt("z", governingSpringPos.Z);
                tree["governingSpringPos"] = tp;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            if (tree.TryGetAttribute("governingSpringPos", out var attr))
            {
                var tp = attr as ITreeAttribute;
                if (tp != null)
                {
                    governingSpringPos = new BlockPos(tp.GetInt("x"), tp.GetInt("y"), tp.GetInt("z"));
                }
            }
        }
        #endregion
    }
}
