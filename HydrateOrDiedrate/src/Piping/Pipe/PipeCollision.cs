using System.Collections.Generic;
using System.Text;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public static class PipeCollision
    {
        static readonly BlockFacing[] Faces =
        {
            BlockFacing.NORTH, BlockFacing.SOUTH,
            BlockFacing.EAST,  BlockFacing.WEST,
            BlockFacing.UP,    BlockFacing.DOWN
        };

        static readonly char[] FaceLetters = { 'n', 's', 'e', 'w', 'u', 'd' };

        const float px = 1f / 16f;
        static readonly Cuboidf Center = new Cuboidf(6*px,  6*px,  6*px, 10*px, 10*px, 10*px);
        static readonly Cuboidf ArmN   = new Cuboidf(6*px,  6*px,  0*px, 10*px, 10*px,  6*px);
        static readonly Cuboidf ArmS   = new Cuboidf(6*px,  6*px, 10*px, 10*px, 10*px, 16*px);
        static readonly Cuboidf ArmE   = new Cuboidf(10*px, 6*px,  6*px, 16*px, 10*px, 10*px);
        static readonly Cuboidf ArmW   = new Cuboidf(0*px,  6*px,  6*px,  6*px, 10*px, 10*px);
        static readonly Cuboidf ArmU   = new Cuboidf(6*px, 10*px,  6*px, 10*px, 16*px, 10*px);
        static readonly Cuboidf ArmD   = new Cuboidf(6*px,  0*px,  6*px, 10*px,  6*px, 10*px);

        public static Cuboidf[] BuildPipeBoxes(Block selfBlock, IBlockAccessor ba, BlockPos at)
        {
            var (count, letters) = BuildKey(selfBlock, ba, at);

            if (count == 0)
            {
                letters = "ud";
            }
            else if (count == 1)
            {
                letters = SingleToStraight2Way(letters);
            }

            var list = new List<Cuboidf>(8) { Center };
            foreach (char c in letters)
            {
                switch (c)
                {
                    case 'n': list.Add(ArmN); break;
                    case 's': list.Add(ArmS); break;
                    case 'e': list.Add(ArmE); break;
                    case 'w': list.Add(ArmW); break;
                    case 'u': list.Add(ArmU); break;
                    case 'd': list.Add(ArmD); break;
                }
            }
            return list.ToArray();
        }
        static string SingleToStraight2Way(string letters)
        {
            char c = letters[0];
            return (c == 'n' || c == 's') ? "ns"
                : (c == 'e' || c == 'w') ? "ew"
                : "ud";
        }
        public static (int count, string letters) BuildKey(Block selfBlock, IBlockAccessor ba, BlockPos at)
        {
            var world = ba as IWorldAccessor;
            var selfFluid = selfBlock as IFluidBlock;

            var set = new HashSet<char>();
            int count = 0;

            for (int i = 0; i < Faces.Length; i++)
            {
                var face = Faces[i];
                var npos = at.AddCopy(face);
                var nb = ba.GetBlock(npos);
                bool selfAllows = world != null ? (selfFluid?.HasFluidConnectorAt(world, at, face) ?? true) : true;
                if (!selfAllows) continue;
                bool isWellBlock = nb is BlockWellSpring;
                bool isWellBE    = ba.GetBlockEntity(npos) is BlockEntityWellSpring;

                if (isWellBlock || isWellBE)
                {
                    set.Add(FaceLetters[i]); count++;
                    continue;
                }
                if (nb is IFluidBlock nFluid)
                {
                    bool neighborAllows = world != null ? nFluid.HasFluidConnectorAt(world, npos, face.Opposite) : true;
                    if (neighborAllows)
                    {
                        set.Add(FaceLetters[i]); count++;
                    }
                }
            }

            const string order = "nsewud";
            var sb = new StringBuilder(order.Length);
            foreach (char c in order)
                if (set.Contains(c)) sb.Append(c);

            return (count, sb.ToString());
        }
    }
}
