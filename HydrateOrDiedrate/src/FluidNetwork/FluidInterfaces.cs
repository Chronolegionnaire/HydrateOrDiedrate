using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.FluidNetwork;

public class FluidInterfaces
{
    public interface IFluidBlock
    {
        void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
        bool HasFluidConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face);
        HydrateOrDiedrate.FluidNetwork.FluidNetwork GetNetwork(IWorldAccessor world, BlockPos pos);
    }

    public interface IFluidNode
    {
        BlockPos GetPosition();
        void JoinNetwork(HydrateOrDiedrate.FluidNetwork.FluidNetwork nw);
        void LeaveNetwork();

        float Volume { get; }   // current fluid
        float Capacity { get; } // max fluid
        float Pressure { get; } // derived (e.g., Volume/Capacity * scale)

        float GetConductance(BlockFacing towards); // how “open” this side is
        void AddFluid(float amount);
        void RemoveFluid(float amount);
        void OnAfterFlowStep(float damping);
    }

    public interface IFluidDevice : IFluidNode
    {
        HydrateOrDiedrate.FluidNetwork.FluidNetwork Network { get; }
        long NetworkId { get; }
        BlockFacing DefaultOutFacing { get; }
        FluidNetwork CreateJoinAndDiscoverNetwork(BlockFacing outward);
        bool JoinAndSpreadNetworkToNeighbours(ICoreAPI api, HydrateOrDiedrate.FluidNetwork.FluidNetwork nw, BlockFacing entryFrom, out Vec3i missingChunkPos);
        bool TryConnect(BlockFacing towards);
        bool IsConnectedTowards(BlockFacing towards);
    }
    
    public interface IFluidExternalProvider
    {
        bool CanProvide(out ItemStack fluidItem, out float availableLitres);
        float ProvideToNetwork(FluidNetwork nw, float litresRequested, ICoreAPI api);
    }
}