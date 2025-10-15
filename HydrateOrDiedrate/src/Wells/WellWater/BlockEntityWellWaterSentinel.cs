using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BlockEntityWellWaterSentinel : BlockEntity
    {
        private BlockPos governingSpringPos;   // persisted
        private long tickListenerId = -1;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            // Resolve governing spring once, if not already known (e.g., from save)
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

            // If we never found a spring, try once more (handles load order)
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

            // If we have a governing spring recorded, check exactly that spot
            if (governingSpringPos != null)
            {
                var be = Api.World.BlockAccessor.GetBlockEntity<BlockEntityWellSpring>(governingSpringPos);
                if (be == null)
                {
                    DeleteSelf(keepFluid: true);
                }
                return;
            }

            // Fallback: if we still don't know the spring, behave as before
            if (FindNearestSpringBelow(Pos) == null)
            {
                DeleteSelf(keepFluid: true);
            }
        }

        private BlockEntityWellSpring FindNearestSpringBelow(BlockPos start)
        {
            var ba = Api.World.BlockAccessor;
            var scan = start.DownCopy();

            // Only scan through "pass-through" cells; stop on any solid that blocks water.
            for (int i = 0; i < 64; i++)
            {
                // Prefer checking the block type first; avoids stale BE references
                var block = ba.GetBlock(scan);
                if (block is BlockWellSpring)
                {
                    var be = ba.GetBlockEntity<BlockEntityWellSpring>(scan);
                    if (be != null) return be;
                    // if block says spring but BE not yet spawned, allow another tick cycle to resolve
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

            // stop ticking first
            if (tickListenerId >= 0)
            {
                UnregisterGameTickListener(tickListenerId);
                tickListenerId = -1;
            }

            var ba = Api.World.BlockAccessor;

            // Remove only the BE, keep the fluid/block as requested
            ba.RemoveBlockEntity(Pos);

            // Optionally tidy fluids if requested
            if (!keepFluid)
            {
                var fluid = ba.GetFluid(Pos);
                ba.SetFluid(0, Pos);
            }

            // Notify clients / neighbors that the BE changed
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
