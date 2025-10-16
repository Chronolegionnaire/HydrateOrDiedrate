using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public static class BlockHandPumpHelpers
    {
        public static bool TryTransferLiquidInto(ItemStack from, ItemStack to)
        {
            if (from?.Collectible is not BlockLiquidContainerBase src || to?.Collectible is not BlockLiquidContainerBase dst) return false;

            var existingLitres = dst.GetCurrentLitres(to);
            var remainingSpace = dst.CapacityLitres - existingLitres;
            if (remainingSpace <= 0) return false;
            remainingSpace *= to.StackSize;

            var toContent   = dst.GetContent(to);
            var fromContent = src.GetContent(from);
            if (fromContent == null) return false;
            if (toContent != null && toContent.Collectible.Code != fromContent.Collectible.Code) return false;

            var taken = src.TryTakeLiquid(from, remainingSpace);
            if (taken == null) return false;

            taken.StackSize /= to.StackSize;
            if (toContent != null) taken.StackSize += toContent.StackSize;

            dst.SetContent(to, taken);
            return true;
        }
    }
}