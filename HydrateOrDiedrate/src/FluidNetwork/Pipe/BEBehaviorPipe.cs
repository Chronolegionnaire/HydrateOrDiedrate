using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Pipes.Pipe
{
    /// <summary>
    /// Simple pipe node: connects on all 6 faces, adopts neighbor networks, and
    /// provides sane capacity and conductance defaults.
    /// </summary>
    public class BEBehaviorPipe : BEBehaviorFluidBase
    {
        public BEBehaviorPipe(BlockEntity be) : base(be)
        {
            capacity    = 100f; // per-pipe capacity
            conductance = 1f;   // wide open; tune per variant if you like
        }

        public override BlockFacing DefaultOutFacing => BlockFacing.NORTH;

        // Pipes accept neighbors on all faces if that neighbor accepts the opposite face
        public override bool IsConnectedTowards(BlockFacing towards)
        {
            var pos  = GetPosition().AddCopy(towards);
            var blk  = Api.World.BlockAccessor.GetBlock(pos);

            // If neighbor implements IFluidBlock, respect its gating;
            // otherwise, if it has a fluid behavior, allow.
            if (blk is FluidInterfaces.IFluidBlock ifb)
                return ifb.HasFluidConnectorAt(Api.World, pos, towards.Opposite);

            var nbe = Api.World.BlockAccessor.GetBlockEntity(pos);
            return nbe?.GetBehavior<BEBehaviorFluidBase>() != null;
        }

        public override float GetConductance(BlockFacing towards) => conductance;

        public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            sb.AppendLine($"Pipe   volume: {volume:G3}/{capacity:G3}");
            sb.AppendLine($"Pipe pressure: {Pressure:G3}");
        }
    }
}
