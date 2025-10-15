// HydrateOrDiedrate.FluidNetwork.HandPump/BEBehaviorHandPump.cs
using System.Text;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.FluidNetwork.HandPump
{
    public class BEBehaviorHandPump : BEBehaviorFluidBase
    {
        public BEBehaviorHandPump(BlockEntity be) : base(be)
        {
            capacity    = 1f;
            conductance = 1f;
        }

        // Only allow connection downward from the pump node itself
        public override bool IsConnectedTowards(BlockFacing towards)
        {
            if (towards != BlockFacing.DOWN) return false;

            var pos = GetPosition().AddCopy(towards);
            var ba  = Api.World.BlockAccessor;

            if (ba.GetBlock(pos) is FluidInterfaces.IFluidBlock ifb)
                return ifb.HasFluidConnectorAt(Api.World, pos, BlockFacing.UP);

            return ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorFluidBase>() != null;
        }

        public override float GetConductance(BlockFacing towards) => conductance;

        /// <summary>
        /// After each flow step, drive the node's demand based on whether it's being pumped.
        /// </summary>
        public override void OnAfterFlowStep(float damping)
        {
            var pumpBE = Blockentity as BlockEntityHandPump;
            bool isPumping = pumpBE?.PumpingPlayer != null;

            if (isPumping)
            {
                SetDemand(capacity);
                // One-hop tug to reduce latency
                TryImmediatePullFromNeighbors(capacity);
                return;
            }

            SetDemand(0f);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            sb.AppendLine($"Pump volume:   {volume:G3}/{capacity:G3}");
            sb.AppendLine($"Pump pressure: {Pressure:G3}");
        }
    }
}