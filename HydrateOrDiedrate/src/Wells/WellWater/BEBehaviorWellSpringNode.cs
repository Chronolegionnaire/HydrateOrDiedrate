// HydrateOrDiedrate.Wells.WellWater/BEBehaviorWellSpringNode.cs
using System;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater
{
    /// <summary>
    /// Provide-only well-spring node:
    /// - Pressure fixed at 0 (neutral). With network-wide non-positive pressures, it will only feed neighbors with negative pressure.
    /// - Capacity reports available litres (used as "give" cap).
    /// - Volume is 0 (no local buffer).
    /// - RemoveFluid() withdraws from the well store.
    /// - AddFluid() is a NO-OP (wells never take).
    /// </summary>
    public class BEBehaviorWellSpringNode : BEBehaviorFluidBase
    {
        public BEBehaviorWellSpringNode(BlockEntity be) : base(be)
        {
            conductance = 1f;   // unrestricted; logic controls direction
        }

        private BlockEntityWellSpring Spring => Blockentity as BlockEntityWellSpring;

        // --- IFluidNode overrides ---

        public override float Pressure => 0f;                       // neutral: only provides to negative neighbors
        public override float Volume   => 0f;                       // no local storage
        public override float Capacity => Math.Max(0f, Spring?.totalLiters ?? 0); // available to GIVE

        public override float GetConductance(BlockFacing towards) => conductance;

        public override void OnAfterFlowStep(float damping) { /* nothing */ }

        // PROVIDE-ONLY: ignore any attempt to add fluid to the well
        public override void AddFluid(float amount)
        {
            // Intentionally NO-OP: wells never accept return flow.
            // Leaving this as-is guarantees no mass is swallowed if something tries anyway.
        }

        public override void RemoveFluid(float amount)
        {
            if (amount <= 0f || Spring == null) return;

            // Clamp to available litres and subtract from the spring's stored total.
            float avail = Math.Max(0f, Spring.totalLiters);
            float take  = Math.Min(avail, amount);

            // The spring tracks whole litres; round conservatively to avoid overdraft.
            int whole = (int)Math.Floor(take + 1e-6f);
            if (whole <= 0) return;

            Spring.TryChangeVolume(-whole, triggerSync: false);
            MarkDirty();
        }
    }
}
