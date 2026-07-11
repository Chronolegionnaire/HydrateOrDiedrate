using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Utility;

public class IntersectionTester(IWorldIntersectionSupplier blockSelectionTester)
{
    private BlockFacing hitOnBlockFaceTmp = BlockFacing.DOWN;
    private Vec3d hitPositionTmp = new();

    private Vec3d lastExitedBlockFacePos = new();

    private readonly IWorldIntersectionSupplier bsTester = blockSelectionTester;
    private readonly Cuboidd tmpCuboidd = new();

    private readonly Vec3d hitPosition = new();
    private Ray ray = new();
    private readonly BlockPos pos = new(Dimensions.WillSetLater);
    private BlockFacing hitOnBlockFace = BlockFacing.DOWN;
    private int hitOnSelectionBox = 0;
    private string hitOnSelectionBoxId = null;

    public void LoadRayAndPos(Ray ray)
    {
        this.ray = ray;
        pos.SetAndCorrectDimension(ray.origin);
    }

    public BlockSelection? GetFluidSelection(IPlayer player, float maxDistance)
    {
        var playerOrigin = player.Entity.Pos.XYZ.Add(player.Entity.LocalEyePos);
        var playerDir = player.Entity.Pos.GetViewVector().ToVec3d() * 5f;
        var playerRay = new Ray(playerOrigin, playerDir);
        LoadRayAndPos(playerRay);

        float distanceSq = 0;

        // Get the face where our ray will exit
        BlockFacing lastExitedBlockFace = GetExitingFullBlockFace(pos, ref lastExitedBlockFacePos);
        if (lastExitedBlockFace == null) return null;

        float maxDistanceSq = (maxDistance + 1) * (maxDistance + 1);

        // Wander along the block exiting faces until we collide with a block selection box
        while (!RayIntersectsBlockSelectionBox(pos))
        {
            if (distanceSq >= maxDistanceSq) return null;

            pos.Offset(lastExitedBlockFace);

            lastExitedBlockFace = GetExitingFullBlockFace(pos, ref lastExitedBlockFacePos);
            if (lastExitedBlockFace == null) return null;

            distanceSq = pos.DistanceSqTo(ray.origin.X - 0.5f, ray.origin.Y - 0.5f, ray.origin.Z - 0.5f);
        }
        if (!blockIntersected.ForFluidsLayer || blockIntersected.SideSolid.Any) return GetCurrentSelection();

        if (hitPosition.SquareDistanceTo(ray.origin) > maxDistance * maxDistance) return null;
        BlockSelection result = GetCurrentSelection();

        var solidBlock = bsTester.blockAccessor.GetBlock(pos, BlockLayersAccess.SolidBlocks);
        if (solidBlock is not null && solidBlock.Id != 0 && !solidBlock.Code.Path.StartsWith("furrowedland")) // hitbox of furrowedland does not match shape so manually excluding it
        {
            var hitboxes = solidBlock.GetSelectionBoxes(bsTester.blockAccessor, pos);
            if (TryIntersect(pos, hitboxes, solidBlock))
            {
                var solidSelection = GetCurrentSelection();

                if (solidSelection.FullPosition.SquareDistanceTo(ray.origin) <= result.FullPosition.SquareDistanceTo(ray.origin))
                {
                    return null;
                }
            }
        }

        return result;
    }

    private BlockSelection GetCurrentSelection() => new()
    {
        Face = hitOnBlockFace,
        Position = pos.CopyAndCorrectDimension(),
        HitPosition = hitPosition.SubCopy(pos.X, pos.InternalY, pos.Z),
        SelectionBoxIndex = hitOnSelectionBox,
        SelectionBoxId = hitOnSelectionBoxId,
        Block = blockIntersected
    };

    Block blockIntersected;
    public bool RayIntersectsBlockSelectionBox(BlockPos pos)
    {
        Cuboidf[] hitboxes;
        Block block = bsTester.blockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);

        if (block.SideSolid.Any)   // It's ice!
        {
            hitboxes = block.GetSelectionBoxes(bsTester.blockAccessor, pos);
        }
        else
        {
            if(block.Id == 0)
            {
                block = bsTester.blockAccessor.GetBlock(pos, BlockLayersAccess.SolidBlocks);
                if(block.Id == 0) return false;

                hitboxes = block.GetSelectionBoxes(bsTester.blockAccessor, pos);
            }
            else hitboxes = Block.DefaultCollisionSelectionBoxes;

        }
        if (hitboxes == null) return false;

        return TryIntersect(pos, hitboxes, block);
    }

    private bool TryIntersect(BlockPos pos, Cuboidf[] hitboxes, Block block)
    {
        bool intersects = false;
        bool wasDecor = false;

        for (int i = 0; i < hitboxes.Length; i++)
        {
            tmpCuboidd.Set(hitboxes[i]).Translate(pos.X, pos.InternalY, pos.Z);
            if (RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFaceTmp, ref hitPositionTmp))
            {
                bool isDecor = hitboxes[i] is DecorSelectionBox;
                if (intersects && (!wasDecor || isDecor) && hitPosition.SquareDistanceTo(ray.origin) <= hitPositionTmp.SquareDistanceTo(ray.origin))
                {
                    continue;
                }

                hitOnSelectionBox = i;
                hitOnSelectionBoxId = (hitboxes[i] as CuboidfWithId)?.Id;
                intersects = true;
                wasDecor = isDecor;
                hitOnBlockFace = hitOnBlockFaceTmp;
                hitPosition.Set(hitPositionTmp);
            }
        }

        if (intersects && hitboxes[hitOnSelectionBox] is DecorSelectionBox dsb)
        {
            Vec3i posAdjust = dsb.PosAdjust;
            if (posAdjust != null)
            {
                pos.Add(posAdjust);
            }
        }

        if (intersects) blockIntersected = block;
        return intersects;
    }

    public bool RayIntersectsWithCuboid(Cuboidd selectionBox, ref BlockFacing hitOnBlockFace, ref Vec3d hitPosition)
    {
        if (selectionBox == null) return false;

        double w = selectionBox.X2 - selectionBox.X1;
        double h = selectionBox.Y2 - selectionBox.Y1;
        double l = selectionBox.Z2 - selectionBox.Z1;

        for (int i = 0; i < BlockFacing.NumberOfFaces; i++)
        {
            BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
            Vec3i planeNormal = blockSideFacing.Normali;

            // Dot product of 2 vectors
            // If they are parallel the dot product is 1
            // At 90 degrees the dot product is 0
            double demon = planeNormal.X * ray.dir.X + planeNormal.Y * ray.dir.Y + planeNormal.Z * ray.dir.Z;

            // Does intersect this plane somewhere (only negative because we are not interested in the ray leaving a face, negative because the ray points into the cube, the plane normal points away from the cube)
            if (demon < -0.00001)
            {
                FastVec3d planeCenterPosition = new FastVec3d(blockSideFacing.PlaneCenter)
                    .Mul(w, h, l)
                    .Add(selectionBox.X1, selectionBox.Y1, selectionBox.Z1)
                ;

                FastVec3d pt = new FastVec3d(planeCenterPosition).Sub(ray.origin);
                double t = (pt.X * planeNormal.X + pt.Y * planeNormal.Y + pt.Z * planeNormal.Z) / demon;

                if (t >= 0)
                {
                    hitPosition.Set(ray.origin.X + ray.dir.X * t, ray.origin.Y + ray.dir.Y * t, ray.origin.Z + ray.dir.Z * t);
                    lastExitedBlockFacePos.Set(planeCenterPosition.ReverseSub(hitPosition));

                    // Does intersect this plane within the block
                    if (Math.Abs(lastExitedBlockFacePos.X) <= w / 2 && Math.Abs(lastExitedBlockFacePos.Y) <= h / 2 && Math.Abs(lastExitedBlockFacePos.Z) <= l / 2)
                    {
                        hitOnBlockFace = blockSideFacing;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private BlockFacing GetExitingFullBlockFace(BlockPos pos, ref Vec3d exitPos)
    {
        for (int i = 0; i < BlockFacing.NumberOfFaces; i++)
        {
            BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
            Vec3i planeNormal = blockSideFacing.Normali;

            double demon = planeNormal.X * ray.dir.X + planeNormal.Y * ray.dir.Y + planeNormal.Z * ray.dir.Z;

            if (demon > 0.00001)
            {
                FastVec3d planePosition = new FastVec3d(pos.X, pos.InternalY, pos.Z).Add(blockSideFacing.PlaneCenter);

                FastVec3d pt = new FastVec3d(planePosition).Sub(ray.origin);
                double t = (pt.X * planeNormal.X + pt.Y * planeNormal.Y + pt.Z * planeNormal.Z) / demon;

                if (t >= 0)
                {
                    FastVec3d pHit = new(ray.origin.X + ray.dir.X * t, ray.origin.Y + ray.dir.Y * t, ray.origin.Z + ray.dir.Z * t);
                    exitPos.Set(pHit.Sub(planePosition));

                    if (Math.Abs(exitPos.X) <= 0.5 && Math.Abs(exitPos.Y) <= 0.5 && Math.Abs(exitPos.Z) <= 0.5)
                    {
                        return blockSideFacing;
                    }
                }
            }
        }

        return null;
    }

}
