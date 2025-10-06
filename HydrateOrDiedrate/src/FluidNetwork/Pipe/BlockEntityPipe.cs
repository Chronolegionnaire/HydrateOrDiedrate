// HydrateOrDiedrate.Pipes.Pipe/BlockEntityPipe.cs
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Pipes.Pipe
{
    public class BlockEntityPipe : BlockEntity
    {
        ICoreClientAPI capi;
        PipeTesselation pipeTess;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            capi = api as ICoreClientAPI;
            if (capi != null) pipeTess = new PipeTesselation(capi);
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tess)
        {
            if (pipeTess == null) return false;
            return pipeTess.TryTesselate(Block, Pos, Api, mesher, tess);
        }
    }
}