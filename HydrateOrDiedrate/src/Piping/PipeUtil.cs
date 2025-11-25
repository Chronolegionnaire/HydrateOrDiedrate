using System;
using System.Collections.Generic;
using System.Linq;
using HydrateOrDiedrate.Piping.ShutoffValve;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping;

public class PipeUtil
{
    private static readonly Dictionary<char, Vec3f> Dir = new()
    {
        ['e'] = new Vec3f(1, 0, 0),
        ['w'] = new Vec3f(-1, 0, 0),
        ['u'] = new Vec3f(0, 1, 0),
        ['d'] = new Vec3f(0, -1, 0),
        ['s'] = new Vec3f(0, 0, 1),
        ['n'] = new Vec3f(0, 0, -1),
    };

    private readonly struct Rot
    {
        public readonly float Rx;
        public readonly float Ry;
        public readonly float Rz;
        public readonly float[] M;

        public Rot(float rx, float ry, float rz)
        {
            Rx = rx;
            Ry = ry;
            Rz = rz;

            float cx = GameMath.Cos(rx), sx = GameMath.Sin(rx);
            float cy = GameMath.Cos(ry), sy = GameMath.Sin(ry);
            float cz = GameMath.Cos(rz), sz = GameMath.Sin(rz);

            float[] RxM = { 1, 0, 0, 0, cx, -sx, 0, sx, cx };
            float[] RyM = { cy, 0, sy, 0, 1, 0, -sy, 0, cy };
            float[] RzM = { cz, -sz, 0, sz, cz, 0, 0, 0, 1 };

            M = Mul3(Mul3(RxM, RyM), RzM);
        }
    }

    private static float[] Mul3(float[] A, float[] B) => new float[]
    {
        A[0] * B[0] + A[1] * B[3] + A[2] * B[6], A[0] * B[1] + A[1] * B[4] + A[2] * B[7],
        A[0] * B[2] + A[1] * B[5] + A[2] * B[8],
        A[3] * B[0] + A[4] * B[3] + A[5] * B[6], A[3] * B[1] + A[4] * B[4] + A[5] * B[7],
        A[3] * B[2] + A[4] * B[5] + A[5] * B[8],
        A[6] * B[0] + A[7] * B[3] + A[8] * B[6], A[6] * B[1] + A[7] * B[4] + A[8] * B[7],
        A[6] * B[2] + A[7] * B[5] + A[8] * B[8],
    };

    private static Vec3f Apply(float[] M, Vec3f v) =>
        new Vec3f(
            M[0] * v.X + M[1] * v.Y + M[2] * v.Z,
            M[3] * v.X + M[4] * v.Y + M[5] * v.Z,
            M[6] * v.X + M[7] * v.Y + M[8] * v.Z
        );

    private static IEnumerable<Rot> AllCubeRotations()
    {
        var steps = new[] { 0f, GameMath.PIHALF, GameMath.PI, 3 * GameMath.PIHALF };
        var seen = new HashSet<string>();

        foreach (var rx in steps)
        foreach (var ry in steps)
        foreach (var rz in steps)
        {
            var r = new Rot(rx, ry, rz);
            string key = string.Join(",", r.M.Select(f =>
                Math.Abs(f) < 1e-4 ? "0" : (f > 0 ? "1" : "-1")
            ));

            if (seen.Add(key))
            {
                yield return r;
            }
        }
    }

    private static char SnapToLetter(Vec3f v)
    {
        float ax = Math.Abs(v.X), ay = Math.Abs(v.Y), az = Math.Abs(v.Z);
        if (ax > ay && ax > az) return v.X > 0 ? 'e' : 'w';
        if (ay > ax && ay > az) return v.Y > 0 ? 'u' : 'd';
        return v.Z > 0 ? 's' : 'n';
    }

    private static IEnumerable<string> Permutations(string s)
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
            {
                yield return s[i] + sub;
            }
        }
    }
    public static (bool ok, float rx, float ry, float rz) TrySolveRotation(
        string canonicalLetters,
        string targetLetters)
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
                    char got = SnapToLetter(rotated[i]);
                    if (got != want)
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return (true, rot.Rx, rot.Ry, rot.Rz);
                }
            }
        }

        return (false, 0, 0, 0);
    }
    public static class ValveOrientationUtil
    {
        private static readonly Vec3f BlockCenter = new Vec3f(0.5f, 0.5f, 0.5f);

        public static void ApplyPipe(MeshData mesh, EValveAxis axis, int rollSteps)
        {
            if (mesh == null) return;
            switch (axis)
            {
                case EValveAxis.NS:
                    mesh.Rotate(BlockCenter, GameMath.PIHALF, 0f, 0f);
                    break;

                case EValveAxis.EW:
                    mesh.Rotate(BlockCenter, 0f, 0f, -GameMath.PIHALF);
                    break;
            }
            float roll = (rollSteps & 3) * GameMath.PIHALF;
            switch (axis)
            {
                case EValveAxis.UD:
                    mesh.Rotate(BlockCenter, 0f, roll, 0f);
                    break;

                case EValveAxis.NS:
                    mesh.Rotate(BlockCenter, 0f, 0f, roll);
                    break;

                case EValveAxis.EW:
                    mesh.Rotate(BlockCenter, roll, 0f, 0f);
                    break;
            }
        }

        public static void GetRendererRotations(
            EValveAxis axis, int rollSteps,
            out float preX, out float preY, out float preZ,
            out float rollAroundAxis)
        {
            preX = preY = preZ = 0f;

            if (axis == EValveAxis.NS) preX = GameMath.PIHALF;
            if (axis == EValveAxis.EW) preZ = -GameMath.PIHALF;

            rollAroundAxis = (rollSteps & 3) * GameMath.PIHALF;
        }
    }
}