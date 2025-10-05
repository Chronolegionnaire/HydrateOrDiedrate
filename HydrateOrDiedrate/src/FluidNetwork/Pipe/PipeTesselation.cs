using System.Collections.Generic;
using System.Text;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Pipes.Pipe
{
    public class PipeTesselation
    {
        static readonly BlockFacing[] Faces =
        {
            BlockFacing.NORTH, BlockFacing.SOUTH,
            BlockFacing.EAST,  BlockFacing.WEST,
            BlockFacing.UP,    BlockFacing.DOWN
        };

        static readonly char[] FaceLetters = { 'n', 's', 'e', 'w', 'u', 'd' };

        readonly ICoreClientAPI capi;
        readonly Dictionary<string, MeshData> meshCache = new();

        public PipeTesselation(ICoreClientAPI capi)
        {
            this.capi = capi;
        }
        public bool TryTesselate(Block block, BlockPos pos, ICoreAPI api, ITerrainMeshPool mesher, ITesselatorAPI tess)
        {
            if (capi == null || block == null || api == null) return false;

            var (count, letters) = BuildKey(api, pos);
            string cacheKey = $"{count}-{letters}";

            MeshData mesh = GetOrCreateMesh(block, tess, count, letters, cacheKey);
            if (mesh == null) return false;

            if (mesh.SeasonColorMapIds == null)  mesh.SeasonColorMapIds  = new byte[mesh.VerticesCount];
            if (mesh.ClimateColorMapIds == null) mesh.ClimateColorMapIds = new byte[mesh.VerticesCount];

            mesher.AddMeshData(mesh);
            return true;
        }

        (int count, string letters) BuildKey(ICoreAPI api, BlockPos at)
        {
            var set = new HashSet<char>();
            int count = 0;

            for (int i = 0; i < Faces.Length; i++)
            {
                var npos = at.AddCopy(Faces[i]);
                var nb = api.World.BlockAccessor.GetBlock(npos);
                if (nb is BlockPipe)
                {
                    set.Add(FaceLetters[i]);
                    count++;
                }
            }

            const string order = "nsewud";
            var sb = new StringBuilder();
            foreach (char c in order)
            {
                if (set.Contains(c)) sb.Append(c);
            }

            return (count, sb.ToString());
        }

        MeshData GetOrCreateMesh(Block block, ITesselatorAPI tess, int count, string letters, string cacheKey)
        {
            if (meshCache.TryGetValue(cacheKey, out var cached)) return cached;

            string shapePath;

            if (count == 0)
            {
                shapePath = "hydrateordiedrate:shapes/block/pipes/2-way/pipe-2-ud.json";
            }
            else if (count == 1)
            {
                string twoWay = SingleToStraight2Way(letters);
                shapePath = $"hydrateordiedrate:shapes/block/pipes/2-way/pipe-2-{twoWay}.json";
            }
            else
            {
                string folder = $"{count}-way";
                shapePath = $"hydrateordiedrate:shapes/block/pipes/{folder}/pipe-{count}-{letters}.json";
            }

            var asset = capi.Assets.TryGet(shapePath);
            if (asset == null) return null;

            var shape = asset.ToObject<Shape>();
            if (shape == null) return null;

            tess.TesselateShape(block, shape, out MeshData mesh);
            meshCache[cacheKey] = mesh;
            return mesh;
        }

        static string SingleToStraight2Way(string letters)
        {
            char c = letters[0];
            return (c == 'n' || c == 's') ? "ns"
                : (c == 'e' || c == 'w') ? "ew"
                : "ud";
        }
    }
}
