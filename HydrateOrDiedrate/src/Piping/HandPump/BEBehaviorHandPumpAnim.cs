// BEBehaviorHandPumpAnim.cs
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class BEBehaviorHandPumpAnim : BEBehaviorAnimatable
    {
        public bool IsPumping;

        public BEBehaviorHandPumpAnim(BlockEntity be) : base(be) {}

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            if (Api.Side == EnumAppSide.Client)
            {
                string side = Block?.Variant?["side"] ?? Block?.Variant?["horizontalorientation"] ?? "north";
                var face = BlockFacing.FromCode(side) ?? BlockFacing.NORTH;
                string key = $"{Block?.Code}-{side}";

                float yawDeg =
                    face == BlockFacing.NORTH ? 180f :
                    face == BlockFacing.EAST  ? 90f :
                    face == BlockFacing.SOUTH ? 0f :
                    270f;

                var rot = new Vec3f(0f, yawDeg, 0f);

                Shape shape;
                MeshData mesh = animUtil.CreateMesh(key, null, out shape, null);
                animUtil.InitializeAnimator(key, mesh, shape, rot, EnumRenderStage.Opaque);

                if (IsPumping) StartPumpAnim();
            }
        }

        public void StartPumpAnim()
        {
            IsPumping = true;

            float frames = 30f;
            float fps = 30f;          // your default
            float clipSeconds = frames / fps;   // 1.0
            float targetSeconds = 2f;
            float animSpeed = clipSeconds / targetSeconds;  // 0.6666667

            animUtil.StartAnimation(new AnimationMetaData {
                Code = "pump",
                Animation = "pump",
                AnimationSpeed = animSpeed,  // ≈ 0.6667
                EaseInSpeed = 8,
                EaseOutSpeed = 8
            });
            Blockentity.MarkDirty(false);
        }

        public void StopPumpAnim()
        {
            IsPumping = false;
            animUtil.StopAnimation("pump");
            Blockentity.MarkDirty(false);
        }
    }
}