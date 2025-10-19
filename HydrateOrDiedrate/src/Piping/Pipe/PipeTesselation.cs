using System;
using System.Collections.Generic;
using System.Text;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.Pipe
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
        const int SUPPORT_SPACING = 2;
        static readonly BlockFacing[] PriorityEW = { BlockFacing.DOWN, BlockFacing.UP, BlockFacing.NORTH, BlockFacing.SOUTH };
        static readonly BlockFacing[] PriorityNS = { BlockFacing.DOWN, BlockFacing.UP, BlockFacing.EAST,  BlockFacing.WEST  };
        static readonly BlockFacing[] PriorityUD = { BlockFacing.NORTH, BlockFacing.SOUTH, BlockFacing.EAST, BlockFacing.WEST };

        readonly ICoreClientAPI capi;
        readonly Dictionary<string, MeshData> meshCache = new();

        public PipeTesselation(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        public bool TryTesselate(Block block, BlockPos pos, ICoreAPI api, ITerrainMeshPool mesher, ITesselatorAPI tess)
        {
            if (capi == null || block == null || api == null) return false;

            var (count, letters) = BuildKey(block, api, pos);
            string cacheKey = $"{count}-{letters}";

            MeshData mesh = GetOrCreateMesh(block, tess, count, letters, cacheKey);
            if (mesh == null) return false;

            EnsureColorArrays(mesh);
            mesher.AddMeshData(mesh);
            TryAddSupport(block, pos, api, mesher, tess, count, letters);
            return true;
        }

        (int count, string letters) BuildKey(Block selfBlock, ICoreAPI api, BlockPos at)
        {
            var selfFluid = selfBlock as IFluidBlock;
            var set = new HashSet<char>();
            int count = 0;

            for (int i = 0; i < Faces.Length; i++)
            {
                var face = Faces[i];
                var npos = at.AddCopy(face);
                var nb = api.World.BlockAccessor.GetBlock(npos);
                bool selfAllows = selfFluid?.HasFluidConnectorAt(api.World, at, face) ?? true;
                if (!selfAllows) continue;

                bool isWellBlock = nb is BlockWellSpring;
                bool isWellBE    = api.World.BlockAccessor.GetBlockEntity(npos) is BlockEntityWellSpring;

                if (isWellBlock || isWellBE)
                {
                    set.Add(FaceLetters[i]);
                    count++;
                    continue;
                }

                if (nb is IFluidBlock nFluid && nFluid.HasFluidConnectorAt(api.World, npos, face.Opposite))
                {
                    set.Add(FaceLetters[i]);
                    count++;
                }
            }

            const string order = "nsewud";
            var sb = new StringBuilder(order.Length);
            foreach (char c in order)
                if (set.Contains(c)) sb.Append(c);

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

            capi.Tesselator.TesselateShape(block, shape, out MeshData mesh);
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
        void TryAddSupport(Block block, BlockPos pos, ICoreAPI api, ITerrainMeshPool mesher, ITesselatorAPI tess,
            int _count, string _letters)
        {
            string axis = GetStraightAxisAt(block, api.World, pos);
            if (axis == null) return;
            if (!IsSupportPosition(axis, pos)) return;

            var pick = PickAttachFace(axis, api.World.BlockAccessor, pos);
            if (pick == null) return;

            var attachFace = pick.Value.face;
            var attachLetter = pick.Value.letter;

            string shapePath = string.Concat(
                "hydrateordiedrate:shapes/block/pipesupport/pipe-supp-", axis, "-",
                attachLetter.ToString(), ".json");
            string cacheKey = string.Concat("supp-", axis, "-", attachLetter.ToString());

            if (!meshCache.TryGetValue(cacheKey, out var mesh))
            {
                var asset = capi.Assets.TryGet(shapePath);
                if (asset == null) return;
                var shape = asset.ToObject<Shape>();
                if (shape == null) return;

                capi.Tesselator.TesselateShape(block, shape, out mesh);
                if (mesh == null) return;
                meshCache[cacheKey] = mesh;
            }

            EnsureColorArrays(mesh);
            mesher.AddMeshData(mesh);
        }

        string GetStraightAxisAt(Block block, IWorldAccessor world, BlockPos pos)
        {
            bool e = IsConnectedToPipeLike(block, world, pos, BlockFacing.EAST);
            bool w = IsConnectedToPipeLike(block, world, pos, BlockFacing.WEST);
            if (e && w) return "ew";

            bool n = IsConnectedToPipeLike(block, world, pos, BlockFacing.NORTH);
            bool s = IsConnectedToPipeLike(block, world, pos, BlockFacing.SOUTH);
            if (n && s) return "ns";

            bool u = IsConnectedToPipeLike(block, world, pos, BlockFacing.UP);
            bool d = IsConnectedToPipeLike(block, world, pos, BlockFacing.DOWN);
            if (u && d) return "ud";

            return null;
        }
        
        bool IsConnectedToPipeLike(Block block, IWorldAccessor world, BlockPos pos, BlockFacing face)
        {
            var ba   = world.BlockAccessor;
            var npos = pos.AddCopy(face);
            var nb   = ba.GetBlock(npos);
            bool selfAllows = (block as IFluidBlock)?.HasFluidConnectorAt(world, pos, face) ?? true;
            if (!selfAllows) return false;

            if (nb is HydrateOrDiedrate.Wells.WellWater.BlockWellSpring) return true;
            if (ba.GetBlockEntity(npos) is HydrateOrDiedrate.Wells.WellWater.BlockEntityWellSpring) return true;

            if (nb is IFluidBlock nFluid && nFluid.HasFluidConnectorAt(world, npos, face.Opposite))
                return true;

            return false;
        }
        static void EnsureColorArrays(MeshData mesh)
        {
            if (mesh.SeasonColorMapIds == null)  mesh.SeasonColorMapIds  = new byte[mesh.VerticesCount];
            if (mesh.ClimateColorMapIds == null) mesh.ClimateColorMapIds = new byte[mesh.VerticesCount];
        }

        static bool IsSupportPosition(string axis, BlockPos pos)
        {
            int P(int v) => ((v % SUPPORT_SPACING) + SUPPORT_SPACING) % SUPPORT_SPACING;

            return axis switch
            {
                "ew" => P(pos.X) == 0,
                "ns" => P(pos.Z) == 0,
                "ud" => P(pos.Y) == 0,
                _    => false
            };
        }

        (BlockFacing face, char letter)? PickAttachFace(string axis, IBlockAccessor ba, BlockPos pos)
        {
            BlockFacing[] order = axis == "ew" ? PriorityEW
                                 : axis == "ns" ? PriorityNS
                                 : PriorityUD;

            foreach (var f in order)
            {
                if (HasSolidNeighbor(ba, pos, f))
                {
                    char letter = FaceToLetter(f);
                    if (axis == "ew" && (letter == 'd' || letter == 'u' || letter == 'n' || letter == 's')) return (f, letter);
                    if (axis == "ns" && (letter == 'd' || letter == 'u' || letter == 'e' || letter == 'w')) return (f, letter);
                    if (axis == "ud" && (letter == 'n' || letter == 's' || letter == 'e' || letter == 'w')) return (f, letter);
                }
            }

            return null;
        }

        static bool HasSolidNeighbor(IBlockAccessor ba, BlockPos pos, BlockFacing towards)
        {
            var npos = pos.AddCopy(towards);
            var nb = ba.GetBlock(npos);
            if (nb == null) return false;
            var ss = nb.SideSolid;
            return ss != null && ss[towards.Opposite.Index];
        }
        static char FaceToLetter(BlockFacing f)
        {
            if (f == BlockFacing.NORTH) return 'n';
            if (f == BlockFacing.SOUTH) return 's';
            if (f == BlockFacing.EAST)  return 'e';
            if (f == BlockFacing.WEST)  return 'w';
            if (f == BlockFacing.UP)    return 'u';
            return 'd';
        }
    }
}
