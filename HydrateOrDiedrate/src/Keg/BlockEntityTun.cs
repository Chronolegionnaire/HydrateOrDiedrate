using HydrateOrDiedrate.Config;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Keg;

public class BlockEntityTun : BlockEntityLiquidContainer
{
    public override string InventoryClassName => "tun";

    public BlockEntityTun()
    {
        inventory = new InventoryGeneric(1, null, null, null)
        {
            BaseWeight = 1.0f,
            OnGetSuitability = GetSuitability,
            TransitionableSpeedMulByType = new Dictionary<EnumTransitionType, float>
            {
                [EnumTransitionType.Perish] = ModConfig.Instance.Containers.TunSpoilRateMultiplier
            }
        };
    }

    private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
    {
        if (targetSlot == inventory[0] && inventory[0].StackSize > 0)
        {
            ItemStack currentStack = inventory[0].Itemstack;
            ItemStack testStack = sourceSlot.Itemstack;
            if (currentStack.Collectible.Equals(currentStack, testStack, GlobalConstants.IgnoredStackAttributes))
                return -1;
        }

        return (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) +
               (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
    }

    public void DropContents(IPlayer byPlayer)
    {
        if (Api is not ICoreServerAPI coreServerAPI) return;

        if (!Inventory.Empty)
        {
            StringBuilder stringBuilder = new($"{byPlayer?.PlayerName} broke container {Block.Code} at {Pos} dropped: ");
            foreach (ItemSlot item in Inventory)
            {
                if (item.Itemstack != null)
                {
                    stringBuilder.Append(item.Itemstack.StackSize).Append("x ").Append(item.Itemstack.Collectible?.Code).Append(", ");
                }
            }

            coreServerAPI.Logger.Audit(stringBuilder.ToString());
        }

        Inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
    }

    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
        //Base method would drop contents which is not always intended, that is now handled by the block.
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnBlockBroken(byPlayer);
        }
    }
}
