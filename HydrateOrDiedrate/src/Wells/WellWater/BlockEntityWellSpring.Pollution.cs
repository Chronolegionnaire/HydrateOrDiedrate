using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Wells.WellWater;

public partial class BlockEntityWellSpring
{
    public string CurrentPollution => WaterBlock?.Variant["pollution"] ?? "clean";

    private bool RunContaminationChecks()
    {
        if (CurrentPollution != "clean" && CurrentPollution != "muddy") return false;

        if (CheckDeadEntityContaminationColumn()) return true;
        if (CheckPoisonedItemContaminationColumn()) return true;
        if (CheckNeighborContaminationColumn()) return true;

        return false;
    }

    private bool ForEachWaterLevel(System.Func<BlockPos, Block, bool> fn)
    {
        var ba = Api.World.BlockAccessor;
        var pos = Pos.Copy();
        int depth = WellShaftHeight;

        for (int i = 0; i < depth; i++)
        {
            pos.Y++;
            var fluid = ba.GetFluid(pos);
            if (!WellBlockUtils.IsOurWellwater(fluid)) break;
            if (fn(pos, fluid)) return true;
        }
        return false;
    }


    private bool CheckDeadEntityContaminationColumn()
    {
        if (!IsFresh) return false;

        var ba = Api.World.BlockAccessor;
        return ForEachWaterLevel((levelPos, block) =>
        {
            if (block == null) return false;

            var collBoxes = block.GetCollisionBoxes(ba, levelPos) ?? [ Cuboidf.Default() ];

            var nearbyEntities = Api.World.GetEntitiesAround(
                levelPos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f, 1.5f,
                e => e is EntityAgent
            );

            foreach (var box in collBoxes)
            {
                var min = new Vec3d(levelPos.X + box.X1, levelPos.Y + box.Y1, levelPos.Z + box.Z1);
                var max = new Vec3d(levelPos.X + box.X2, levelPos.Y + box.Y2, levelPos.Z + box.Z2);

                foreach (var e in nearbyEntities)
                {
                    if (e is not EntityAgent agent) continue;
                    var emin = agent.Pos.XYZ.AddCopy(agent.CollisionBox.X1, agent.CollisionBox.Y1, agent.CollisionBox.Z1);
                    var emax = agent.Pos.XYZ.AddCopy(agent.CollisionBox.X2, agent.CollisionBox.Y2, agent.CollisionBox.Z2);

                    bool intersects =
                        emin.X <= max.X && emax.X >= min.X &&
                        emin.Y <= max.Y && emax.Y >= min.Y &&
                        emin.Z <= max.Z && emax.Z >= min.Z;

                    if (intersects && !agent.Alive)
                    {
                        TryEnsureWaterVariant("pollution", "tainted");
                        return true;
                    }
                }
            }
            return false;
        });
    }

    private bool CheckPoisonedItemContaminationColumn()
    {
        var ba = Api.World.BlockAccessor;
        return ForEachWaterLevel((levelPos, block) =>
        {
            if (block == null) return false;

            var collBoxes = block.GetCollisionBoxes(ba, levelPos) ?? [ Cuboidf.Default() ];

            var nearbyItems = Api.World.GetEntitiesAround(
                levelPos.ToVec3d().Add(0.5, 0.5, 0.5),
                1.5f, 1.5f,
                e => e is EntityItem
            );

            foreach (var box in collBoxes)
            {
                var min = new Vec3d(levelPos.X + box.X1, levelPos.Y + box.Y1, levelPos.Z + box.Y1);
                var max = new Vec3d(levelPos.X + box.X2, levelPos.Y + box.Y2, levelPos.Z + box.Y2);

                foreach (var e in nearbyItems)
                {
                    if (e is not EntityItem item) continue;
                    var stack = item.Itemstack;
                    if (stack?.Collectible?.Code == null) continue;

                    if (!stack.Collectible.Code.Equals(new AssetLocation("game", "mushroom-deathcap-normal"))) continue;

                    var emin = item.Pos.XYZ.AddCopy(item.CollisionBox.X1, item.CollisionBox.Y1, item.CollisionBox.Z1);
                    var emax = item.Pos.XYZ.AddCopy(item.CollisionBox.X2, item.CollisionBox.Y2, item.CollisionBox.Z2);

                    bool intersects =
                        emin.X <= max.X && emax.X >= min.X &&
                        emin.Y <= max.Y && emax.Y >= min.Y &&
                        emin.Z <= max.Z && emax.Z >= min.Z;

                    if (intersects)
                    {
                        TryEnsureWaterVariant("pollution", "poisoned");
                        return true;
                    }
                }
            }
            return false;
        });
    }

    private bool CheckNeighborContaminationColumn()
    {
        var ba = Api.World.BlockAccessor;

        return ForEachWaterLevel((levelPos, _block) =>
        {
            foreach (var face in BlockFacing.ALLFACES)
            {
                var npos = levelPos.AddCopy(face);
                var nblock = ba.GetFluid(npos);
                if (!WellBlockUtils.IsOurWellwater(nblock)) continue;

                var pollution =  nblock?.Variant["pollution"];
                if (!string.IsNullOrEmpty(pollution) && pollution != "clean" && pollution != "muddy")
                {
                    TryEnsureWaterVariant("pollution", pollution);
                    return true;
                }
            }
            return false;
        });
    }
}
