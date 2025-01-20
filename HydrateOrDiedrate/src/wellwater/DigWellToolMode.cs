using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate.wellwater
{
    public class DigWellToolMode : Item
    {
        private SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            ICoreClientAPI capi = api as ICoreClientAPI;
            toolModes = new SkillItem[]
            {
                new SkillItem
                {
                    Code = new AssetLocation("digmode"),
                    Name = "Dig Mode"
                },
                new SkillItem
                {
                    Code = new AssetLocation("digwellspring"),
                    Name = "Dig Well Spring"
                }
            };

            if (capi != null)
            {
                toolModes[0].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("game:textures/icons/rocks.svg"), 48, 48, 5, null));
                toolModes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("hydrateordiedrate:textures/icons/well.svg"), 48, 48, 5, null));
            }
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return Math.Min(toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode", 0));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f)
        {
            int toolMode = GetToolMode(itemslot, (byEntity as EntityPlayer)?.Player, blockSel);

            if (toolMode == 1 && blockSel != null)
            {
                string heldItemName = itemslot.Itemstack?.Item?.Code?.Path ?? "";
                string blockCode = world.BlockAccessor.GetBlock(blockSel.Position).Code.Path;

                bool isPickaxe = heldItemName.Contains("pickaxe");
                bool isShovel = heldItemName.Contains("shovel");

                if ((isPickaxe && blockCode.StartsWith("rock-")) ||
                    (isShovel && (blockCode.StartsWith("soil-") || blockCode.StartsWith("gravel-") || blockCode.StartsWith("sand-"))))
                {
                    Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:wellspring"));
                    if (wellSpringBlock != null)
                    {
                        world.BlockAccessor.SetBlock(wellSpringBlock.BlockId, blockSel.Position);
                        return true;
                    }
                }
            }

            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "Change tool mode",
                    HotKeyCodes = new string[] { "toolmodeselect" },
                    MouseButton = EnumMouseButton.None
                }
            };
        }
    }
}
