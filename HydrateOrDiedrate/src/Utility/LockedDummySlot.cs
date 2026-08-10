using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Utility;

public class LockedDummySlot(ItemStack stack) : DummySlot(stack)
{
    public override bool CanTake() => false;

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) => false;

    public override bool CanHold(ItemSlot sourceSlot) => false;
}
