using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.FluidNetwork
{
    public interface IFluidBlock
    {
        bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
        void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
    }
    public interface IFluidGate
    {
        bool AllowsFluidPassage(IWorldAccessor world, BlockPos pos, BlockFacing from, BlockFacing to);
    }
}
