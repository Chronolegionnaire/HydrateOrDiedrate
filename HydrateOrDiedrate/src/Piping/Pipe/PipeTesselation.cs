using System;
using System.Collections.Generic;
using System.Linq;
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
        const string SUPPORT_BASE_SHAPE = "hydrateordiedrate:shapes/block/pipes/pipesupport.json";
        static readonly Vec3f BlockCenter = new Vec3f(0.5f, 0.5f, 0.5f);
        readonly Dictionary<string, MeshData> pipeMeshCache = new();
        static readonly char[] FaceLetters = { 'n', 's', 'e', 'w', 'u', 'd' };
        const int SUPPORT_SPACING = 2;
        static readonly BlockFacing[] PriorityEW = { BlockFacing.DOWN, BlockFacing.UP, BlockFacing.NORTH, BlockFacing.SOUTH };
        static readonly BlockFacing[] PriorityNS = { BlockFacing.DOWN, BlockFacing.UP, BlockFacing.EAST,  BlockFacing.WEST  };
        static readonly BlockFacing[] PriorityUD = { BlockFacing.NORTH, BlockFacing.SOUTH, BlockFacing.EAST, BlockFacing.WEST };

        readonly ICoreClientAPI capi;
        readonly Dictionary<string, MeshData> meshCache = new();
        static readonly string LetterOrder = "nsewud";

        static string NormalizeLetters(string letters)
        {
            var set = new HashSet<char>(letters);
            var sb = new StringBuilder(6);
            foreach (var c in LetterOrder) if (set.Contains(c)) sb.Append(c);
            return sb.ToString();
        }

        static float Deg(float d) => (float)(Math.PI / 180.0) * d;
        static string ModelKeyFromPath(string shapePath)
        {
            int slash = shapePath.LastIndexOf('/');
            int dot   = shapePath.LastIndexOf('.');
            if (slash >= 0 && dot > slash) return shapePath.Substring(slash + 1, dot - slash - 1);
            return shapePath;
        }
        readonly Dictionary<char, Vec3f> Dir = new()
        {
            ['e'] = new Vec3f( 1,  0,  0),
            ['w'] = new Vec3f(-1,  0,  0),
            ['u'] = new Vec3f( 0,  1,  0),
            ['d'] = new Vec3f( 0, -1,  0),
            ['s'] = new Vec3f( 0,  0,  1),
            ['n'] = new Vec3f( 0,  0, -1),
        };
        struct Rot
        {
            public float rx, ry, rz;
            public float[] M;
            public Rot(float rx, float ry, float rz)
            {
                this.rx = rx; this.ry = ry; this.rz = rz;

                float cx = GameMath.Cos(rx), sx = GameMath.Sin(rx);
                float cy = GameMath.Cos(ry), sy = GameMath.Sin(ry);
                float cz = GameMath.Cos(rz), sz = GameMath.Sin(rz);

                float[] Rx = {1,0,0,  0,cx,-sx,  0,sx,cx};
                float[] Ry = {cy,0,sy,  0,1,0,  -sy,0,cy};
                float[] Rz = {cz,-sz,0,  sz,cz,0,  0,0,1};
                M = Mul3(Mul3(Rx, Ry), Rz);
            }
        }
        static float[] Mul3(float[] A, float[] B) => new float[]
        {
            A[0]*B[0]+A[1]*B[3]+A[2]*B[6], A[0]*B[1]+A[1]*B[4]+A[2]*B[7], A[0]*B[2]+A[1]*B[5]+A[2]*B[8],
            A[3]*B[0]+A[4]*B[3]+A[5]*B[6], A[3]*B[1]+A[4]*B[4]+A[5]*B[7], A[3]*B[2]+A[4]*B[5]+A[5]*B[8],
            A[6]*B[0]+A[7]*B[3]+A[8]*B[6], A[6]*B[1]+A[7]*B[4]+A[8]*B[7], A[6]*B[2]+A[7]*B[5]+A[8]*B[8],
        };
        static Vec3f Apply(float[] M, Vec3f v) =>
            new Vec3f(M[0]*v.X+M[1]*v.Y+M[2]*v.Z, M[3]*v.X+M[4]*v.Y+M[5]*v.Z, M[6]*v.X+M[7]*v.Y+M[8]*v.Z);

        static IEnumerable<Rot> AllCubeRotations()
        {
            var steps = new[] {0f, GameMath.PIHALF, GameMath.PI, 3*GameMath.PIHALF};
            var seen = new HashSet<string>();
            foreach (var rx in steps) foreach (var ry in steps) foreach (var rz in steps)
            {
                var r = new Rot(rx, ry, rz);
                string key = string.Join(",", r.M.Select(f => Math.Abs(f) < 1e-4 ? "0" : (f > 0 ? "1" : "-1")));
                if (seen.Add(key)) yield return r;
            }
        }

        static char SnapToLetter(Vec3f v)
        {
            float ax = Math.Abs(v.X), ay = Math.Abs(v.Y), az = Math.Abs(v.Z);
            if (ax > ay && ax > az) return v.X > 0 ? 'e' : 'w';
            if (ay > ax && ay > az) return v.Y > 0 ? 'u' : 'd';
            return v.Z > 0 ? 's' : 'n';
        }
        static IEnumerable<string> Permutations(string s)
        {
            if (s.Length <= 1)
            {
                yield return s;
                yield break;
            }

            for (int i = 0; i < s.Length; i++)
            {
                string before = s[..i];
                string after = s[(i + 1)..];
                foreach (string sub in Permutations(before + after))
                    yield return s[i] + sub;
            }
        }
        (bool ok, float rx, float ry, float rz) TrySolveRotation(string canonicalLetters, string targetLetters)
        {
            var canon = canonicalLetters.ToCharArray();
            var canonDirs = canon.Select(c => Dir[c]).ToArray();
            foreach (var rot in AllCubeRotations())
            {
                var rotated = canonDirs.Select(v => Apply(rot.M, v)).ToArray();
                foreach (var perm in Permutations(targetLetters))
                {
                    bool allMatch = true;
                    for (int i = 0; i < canon.Length; i++)
                    {
                        char want = perm[i];
                        char got  = SnapToLetter(rotated[i]);
                        if (got != want) { allMatch = false; break; }
                    }
                    if (allMatch) return (true, rot.rx, rot.ry, rot.rz);
                }
            }
            return (false, 0, 0, 0);
        }
        public PipeTesselation(ICoreClientAPI capi)
        {
            this.capi = capi;
        }

        public bool TryTesselate(Block block, BlockPos pos, ICoreAPI api, ITerrainMeshPool mesher, ITesselatorAPI tess)
        {
            if (capi == null || block == null || api == null) return false;

            var (count, letters) = BuildKey(block, api, pos);
            MeshData baseMesh = GetOrCreateMesh(block, tess, count, letters);
            if (baseMesh == null) return false;
            mesher.AddMeshData(baseMesh.Clone());
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

        MeshData GetOrCreateMesh(Block block, ITesselatorAPI tess, int count, string letters)
        {
            string targetLetters = (count == 1) ? SingleToStraight2Way(letters) : letters;
            int targetCount = (count == 1) ? 2 : count;
            if (count == 0) { targetLetters = "ud"; targetCount = 2; }

            string targetForSolve = targetLetters;
            string targetForKey   = NormalizeLetters(targetLetters);

            foreach (var (canonLetters, shapePath) in GetCanonicalBasesForCount(targetCount))
            {
                var solved = TrySolveRotation(canonLetters, targetForSolve);
                if (!solved.ok) continue;

                string modelKey = ModelKeyFromPath(shapePath);
                string variant = block?.Code?.ToShortString() ?? "block";
                string cacheKey = $"{modelKey}|{targetForKey}|{variant}";

                if (pipeMeshCache.TryGetValue(cacheKey, out var cached))
                    return cached;

                var asset = capi.Assets.TryGet(shapePath);
                if (asset == null) continue;
                var shape = asset.ToObject<Shape>();
                if (shape == null) continue;

                capi.Tesselator.TesselateShape(block, shape, out MeshData mesh);
                if (mesh == null) continue;
                mesh.Rotate(BlockCenter, solved.rx, solved.ry, solved.rz);

                pipeMeshCache[cacheKey] = mesh;
                return mesh;
            }
            return null;
        }

        static string SingleToStraight2Way(string letters)
        {
            char c = letters[0];
            return (c == 'n' || c == 's') ? "ns"
                : (c == 'e' || c == 'w') ? "ew"
                : "ud";
        }

        List<(string letters, string shapePath)> GetCanonicalBasesForCount(int count)
        {
            var list = new List<(string, string)>();
            switch (count)
            {
                case 2:
                    list.Add(("ud", "hydrateordiedrate:shapes/block/pipes/2-way/pipe-2-ud.json"));
                    list.Add(("eu", "hydrateordiedrate:shapes/block/pipes/2-way/pipe-2-eu.json"));
                    break;

                case 3:
                    list.Add(("sud", "hydrateordiedrate:shapes/block/pipes/3-way/pipe-3-sud.json"));
                    list.Add(("seu", "hydrateordiedrate:shapes/block/pipes/3-way/pipe-3-seu.json"));
                    break;

                case 4:
                    list.Add(("nsud", "hydrateordiedrate:shapes/block/pipes/4-way/pipe-4-nsud.json"));
                    list.Add(("nseu", "hydrateordiedrate:shapes/block/pipes/4-way/pipe-4-nseu.json"));
                    break;

                case 5:
                    list.Add(("nseud", "hydrateordiedrate:shapes/block/pipes/5-way/pipe-5-nseud.json"));
                    break;

                case 6:
                    list.Add(("nsewud", "hydrateordiedrate:shapes/block/pipes/6-way/pipe-6-nsewud.json"));
                    break;
            }
            return list;
        }

        void TryAddSupport(Block block, BlockPos pos, ICoreAPI api, ITerrainMeshPool mesher, ITesselatorAPI tess,
            int _count, string _letters)
        {
            string axis = GetStraightAxisAt(block, api.World, pos);
            if (axis == null) return;
            if (!IsSupportPosition(axis, pos)) return;
            var pick = PickAttachFace(axis, api.World.BlockAccessor, pos);
            if (pick == null) return;
            var attachLetter = pick.Value.letter;
            var mesh = GetOrCreateSupportMesh(block, axis, attachLetter);
            if (mesh == null) return;
            mesher.AddMeshData(mesh);
        }

        MeshData GetOrCreateSupportMesh(Block block, string axis, char attachLetter)
        {
            string cacheKey = $"supp-{axis}-{attachLetter}";
            if (meshCache.TryGetValue(cacheKey, out var cached)) return cached;

            var asset = capi.Assets.TryGet(SUPPORT_BASE_SHAPE);
            if (asset == null) return null;
            var shape = asset.ToObject<Shape>();
            if (shape == null) return null;

            capi.Tesselator.TesselateShape(block, shape, out var baseMesh);
            if (baseMesh == null) return null;

            var mesh = baseMesh.Clone();
            AlignSupportToAxis(mesh, axis);

            RotateSupportForAttach(mesh, axis, attachLetter);

            meshCache[cacheKey] = mesh;
            return mesh;
        }

        static void AlignSupportToAxis(MeshData mesh, string axis)
        {
            if (axis == "ns")
            {
                mesh.Rotate(BlockCenter, 0f, GameMath.PIHALF, 0f);
            }
            else if (axis == "ud")
            {
                mesh.Rotate(BlockCenter, 0f, 0f, -GameMath.PIHALF);
            }
        }

        static void RotateSupportForAttach(MeshData mesh, string axis, char letter)
        {
            float rx = 0f, ry = 0f, rz = 0f;

            switch (axis)
            {
                case "ew":
                    rx = letter switch
                    {
                        'd' => 0f,
                        'u' => GameMath.PI,
                        'n' =>  GameMath.PIHALF,
                        's' => -GameMath.PIHALF,
                        'e' => 0f,
                        'w' => GameMath.PI,
                        _   => 0f
                    };
                    break;

                case "ns":
                    rz = letter switch
                    {
                        'd' => 0f,
                        'u' => GameMath.PI,
                        'e' =>  GameMath.PIHALF,
                        'w' => -GameMath.PIHALF,
                        _   => 0f
                    };
                    break;

                case "ud":
                    ry = letter switch
                    {
                        'w' => 0f,
                        'e' => GameMath.PI,
                        'n' => -GameMath.PIHALF,
                        's' =>  GameMath.PIHALF,
                        _   => 0f
                    };
                    break;
            }

            mesh.Rotate(BlockCenter, rx, ry, rz);
        }

        string GetStraightAxisAt(Block block, IWorldAccessor world, BlockPos pos)
        {
            bool e = IsConnectedToPipeLike(block, world, pos, BlockFacing.EAST);
            bool w = IsConnectedToPipeLike(block, world, pos, BlockFacing.WEST);
            bool n = IsConnectedToPipeLike(block, world, pos, BlockFacing.NORTH);
            bool s = IsConnectedToPipeLike(block, world, pos, BlockFacing.SOUTH);
            bool u = IsConnectedToPipeLike(block, world, pos, BlockFacing.UP);
            bool d = IsConnectedToPipeLike(block, world, pos, BlockFacing.DOWN);

            if ((e && w) || ((e ^ w) && !n && !s && !u && !d)) return "ew";
            if ((n && s) || ((n ^ s) && !e && !w && !u && !d)) return "ns";
            if ((u && d) || ((u ^ d) && !e && !w && !n && !s)) return "ud";

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
