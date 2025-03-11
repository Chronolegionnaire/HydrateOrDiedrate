using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.wellwater
{
    public class BlockWellSpring : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            bool preventDefault = false;
            foreach (var blockBehavior in this.BlockBehaviors)
            {
                EnumHandling handling = EnumHandling.PassThrough;
                blockBehavior.OnBlockBroken(world, pos, byPlayer, ref handling);

                if (handling == EnumHandling.PreventDefault)
                {
                    preventDefault = true;
                }
                else if (handling == EnumHandling.PreventSubsequent)
                {
                    return;
                }
            }

            if (preventDefault)
            {
                return;
            }
            world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
            this.SpawnBlockBrokenParticles(pos);
            world.BlockAccessor.SetBlock(0, pos);
        }
    }
}