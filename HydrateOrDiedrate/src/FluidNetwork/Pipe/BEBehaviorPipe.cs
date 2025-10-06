// HydrateOrDiedrate.Pipes.Pipe/BEBehaviorPipe.cs
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Pipes.Pipe
{
    /// <summary>Simple pipe node with 6-way connectivity.</summary>
    public class BEBehaviorPipe : BEBehaviorFluidBase
    {
        public BEBehaviorPipe(BlockEntity be) : base(be)
        {
            capacity    = 100f; // per-pipe capacity
            conductance = 1f;   // wide open; tune per variant if desired
        }

        public override BlockFacing DefaultOutFacing => BlockFacing.NORTH;

        // Pipes accept neighbors on all faces if that neighbor accepts the opposite face
        public override bool IsConnectedTowards(BlockFacing towards)
        {
            var pos = GetPosition().AddCopy(towards);
            var ba  = Api.World.BlockAccessor;

            // If neighbor implements IFluidBlock, respect its gating, otherwise allow if it has a fluid behavior
            if (ba.GetBlock(pos) is FluidInterfaces.IFluidBlock ifb)
                return ifb.HasFluidConnectorAt(Api.World, pos, towards.Opposite);

            return ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorFluidBase>() != null;
        }

        public override float GetConductance(BlockFacing towards) => conductance;

        public override void GetBlockInfo(IPlayer forPlayer, System.Text.StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            sb.AppendLine($"Pipe   volume: {volume:G3}/{capacity:G3}");
            sb.AppendLine($"Pipe pressure: {Pressure:G3}");
            // Optional (uncomment if you want to show target fill):
            // sb.AppendLine($"Pipe  demand:  {GetDemand():G3}");
        }
    }
}