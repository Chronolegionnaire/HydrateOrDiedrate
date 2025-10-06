// HydrateOrDiedrate.FluidNetwork.HandPump/BEBehaviorHandPump.cs
using System;
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
            capacity    = 5f;
            conductance = 1f;
        }

        public override float GetConductance(BlockFacing towards) => conductance;

        /// <summary>
        /// After each flow step, drive the node's demand based on whether it's being pumped.
        /// - Pumping: set demand to our target (here: fill to capacity) and opportunistically
        ///   do a one-hop pull to accelerate response.
        /// - Idle: release demand (0) so we don't keep pulling.
        /// </summary>
        public override void OnAfterFlowStep(float damping)
        {
            var pumpBE = Blockentity as BlockEntityHandPump;
            bool isPumping = pumpBE?.PumpingPlayer != null;

            if (isPumping)
            {
                // Target a full local buffer while actively pumping.
                SetDemand(capacity);

                // Optional: immediate one-hop pull to reduce latency (no negative volumes).
                // You can tune this 'maxLitres' burst; using capacity is fine for a small pump.
                TryImmediatePullFromNeighbors(capacity);
                return;
            }

            // Idle → no ongoing target; pressure returns to neutral as volume equals demand (0).
            SetDemand(0f);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            sb.AppendLine($"Pump volume:   {volume:G3}/{capacity:G3}");
            sb.AppendLine($"Pump pressure: {Pressure:G3}");
            // Optional:
            // sb.AppendLine($"Pump demand:   {GetDemand():G3}");
        }
    }
}
