// HydrateOrDiedrate.Wells.WellWater/BEBehaviorWellSpringNode.cs
using System;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BEBehaviorWellSpringNode : BEBehaviorFluidBase
    {
        public BEBehaviorWellSpringNode(BlockEntity be) : base(be)
        {
            conductance = 1f;
        }

        private BlockEntityWellSpring Spring => Blockentity as BlockEntityWellSpring;

        // NEW: publish the exact item code this spring provides
        public override string ProvidedFluidCode
        {
            get
            {
                var lt = Spring?.LastWaterType; // e.g. "fresh-well-clean"
                if (string.IsNullOrEmpty(lt)) return null;
                return $"hydrateordiedrate:waterportion-{lt}";
            }
        }
        
        public override void SetVolumeFromNetwork(float newVolume, float delta)
        {
            if (delta < 0f && Spring != null)
            {
                int whole = (int)Math.Floor((-delta) + 1e-6f);
                if (whole > 0)
                {
                    // Trigger sync so column height updates immediately
                    Spring.TryChangeVolume(-whole, triggerSync: true);
                    MarkDirty(false);
                }
            }
            // No local buffer; Volume getter reads Spring.totalLiters.
        }

        public override float Pressure => 0f;
        public override float Volume   => Math.Max(0f, Spring?.totalLiters ?? 0f);
        public override float Capacity => Volume;
        public override float ProvideCap => Volume;
        public override float ReceiveCap => 0f;

        public override float GetConductance(BlockFacing towards) => conductance;
        public override void OnAfterFlowStep(float damping) { }

        public override void AddFluid(float amount) { /* no-op: provider only */ }

        public override void RemoveFluid(float amount)
        {
            if (amount <= 0f || Spring == null) return;

            float avail = Math.Max(0f, Spring.totalLiters);
            float take  = Math.Min(avail, amount);
            int whole = (int)Math.Floor(take + 1e-6f);
            if (whole <= 0) return;

            // Trigger sync so column height updates immediately
            Spring.TryChangeVolume(-whole, triggerSync: true);
            MarkDirty(false);
        }
    }
}
