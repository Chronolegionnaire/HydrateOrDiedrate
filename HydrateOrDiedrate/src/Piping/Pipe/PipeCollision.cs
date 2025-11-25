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
        static char FaceToLetter(BlockFacing f)
        {
            if (f == BlockFacing.NORTH) return 'n';
            if (f == BlockFacing.SOUTH) return 's';
            if (f == BlockFacing.EAST)  return 'e';
            if (f == BlockFacing.WEST)  return 'w';
            if (f == BlockFacing.UP)    return 'u';
            return 'd';
        }

        const float px = 1f / 16f;
        static readonly Cuboidf Center = new Cuboidf(6 * px, 6 * px, 6 * px, 10 * px, 10 * px, 10 * px);
        static readonly Cuboidf ArmN   = new Cuboidf(6 * px, 6 * px, 0 * px, 10 * px, 10 * px, 6 * px);
        static readonly Cuboidf ArmS   = new Cuboidf(6 * px, 6 * px, 10 * px, 10 * px, 10 * px, 16 * px);
        static readonly Cuboidf ArmE   = new Cuboidf(10 * px, 6 * px, 6 * px, 16 * px, 10 * px, 10 * px);
        static readonly Cuboidf ArmW   = new Cuboidf(0 * px, 6 * px, 6 * px, 6 * px, 10 * px, 10 * px);
        static readonly Cuboidf ArmU   = new Cuboidf(6 * px, 10 * px, 6 * px, 10 * px, 16 * px, 10 * px);
        static readonly Cuboidf ArmD   = new Cuboidf(6 * px, 0 * px, 6 * px, 10 * px, 6 * px, 10 * px);

        public static Cuboidf[] BuildPipeBoxes(Block selfBlock, IWorldAccessor world, BlockPos at)
        {
            var letters = BuildKey(selfBlock, world, at);
            int count = letters.Length;

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

        static string SingleToStraight2Way(string letters) => letters[0] switch
        {
            'n' or 's' => "ns",
            'e' or 'w' => "ew",
            _ => "ud",
        };
        public static string BuildKey(Block selfBlock, IWorldAccessor world, BlockPos at)
        {
            var selfFluid = selfBlock as IFluidBlock;
            var ba        = world.BlockAccessor;

            var set = new HashSet<char>();

            foreach (var face in BlockFacing.ALLFACES)
            {
                var npos = at.AddCopy(face);

                bool selfAllows = selfFluid?.HasFluidConnectorAt(world, at, face) ?? true;
                if (!selfAllows) continue;

                var nb = ba.GetBlock(npos);

                bool isWellBlock = nb is BlockWellSpring;
                bool isWellBE    = ba.GetBlockEntity(npos) is BlockEntityWellSpring;

                if (isWellBlock || isWellBE)
                {
                    set.Add(FaceToLetter(face));
                    continue;
                }

                if (nb is IFluidBlock nFluid)
                {
                    bool neighborAllows = nFluid.HasFluidConnectorAt(world, npos, face.Opposite);
                    if (neighborAllows)
                    {
                        set.Add(FaceToLetter(face));
                    }
                }
            }

            const string order = "nsewud";
            var sb = new StringBuilder(order.Length);

            foreach (char c in order)
            {
                if (set.Contains(c)) sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
