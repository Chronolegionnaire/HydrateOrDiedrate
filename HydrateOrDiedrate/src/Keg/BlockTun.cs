using HydrateOrDiedrate.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg;

public class BlockTun : BlockLiquidContainerBase
{

    public override float CapacityLitres => ModConfig.Instance.Containers.TunCapacityLitres;
    public override bool CanDrinkFrom => false;
    public override bool AllowHeldLiquidTransfer => false;

    //Note: CanDrinkFrom does not fully prevent you from drinking, unsure why but this is why we are overiding the Eat methods
    protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1) => handling = EnumHandHandling.PreventDefaultAction;

    protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null) => false;

    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        //Not edible
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        var beh = world.BlockAccessor.GetBlockEntity<BlockEntityTun>(pos);
        if(beh is not null)
        {
            var contentCode = beh.GetContent()?.Collectible.Code;

            //Drop content if we don't drop with liquid or if the content has become rotten
            if(!ModConfig.Instance.Containers.TunDropWithLiquid || (contentCode is not null && contentCode.Domain == "game" && contentCode.Path == "rot"))
            {
                beh.DropContents(byPlayer);
            }
        }

        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }
}
