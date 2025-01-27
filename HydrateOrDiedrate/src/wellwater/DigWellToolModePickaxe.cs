using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.wellwater
{
    public class DigWellToolModePickaxe : Item
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
                toolModes[0].WithIcon(
                    capi,
                    capi.Gui.LoadSvgWithPadding(
                        new AssetLocation("game:textures/icons/rocks.svg"), 48, 48, 5, null
                    )
                );
                toolModes[1].WithIcon(
                    capi,
                    capi.Gui.LoadSvgWithPadding(
                        new AssetLocation("hydrateordiedrate:textures/icons/well.svg"), 48, 48, 5, null
                    )
                );
            }
        }
        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            if (!api.ModLoader.IsModEnabled("xskills"))
            {
                return toolModes;
            }
            SkillItem[] xskillsModes = base.GetToolModes(slot, forPlayer, blockSel);
            if (xskillsModes == null || xskillsModes.Length == 0)
            {
                return toolModes;
            }
            int xCount = xskillsModes.Length;
            SkillItem digWellSpring = toolModes[1];
            SkillItem[] combined = new SkillItem[xCount + 1];
            Array.Copy(xskillsModes, combined, xCount);
            combined[xCount] = digWellSpring;

            return combined;
        }
        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (slot?.Itemstack == null) return 0;
            if (!api.ModLoader.IsModEnabled("xskills"))
            {
                return Math.Min(toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode", 0));
            }
            SkillItem[] mergedModes = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
            if (mergedModes == null || mergedModes.Length == 0)
            {
                return 0;
            }
            int storedMode = slot.Itemstack.Attributes.GetInt("toolMode", 0);
            int clampedMode = GameMath.Clamp(storedMode, 0, mergedModes.Length - 1);
            return clampedMode;
        }
        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            if (slot?.Itemstack == null) return;

            if (!api.ModLoader.IsModEnabled("xskills"))
            {
                slot.Itemstack.Attributes.SetInt("toolMode", GameMath.Clamp(toolMode, 0, toolModes.Length - 1));
                return;
            }
            SkillItem[] mergedModes = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
            if (mergedModes == null || mergedModes.Length == 0)
            {
                slot.Itemstack.Attributes.SetInt("toolMode", Math.Min(toolModes.Length - 1, toolMode));
                return;
            }
            int finalMode = GameMath.Clamp(toolMode, 0, mergedModes.Length - 1);
            slot.Itemstack.Attributes.SetInt("toolMode", finalMode);
        }
        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot,
            BlockSelection blockSel, float dropQuantityMultiplier = 1f)
        {
            if (blockSel == null) return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            int modeIndex = GetToolMode(itemslot, (byEntity as EntityPlayer)?.Player, blockSel);

            if (api.ModLoader.IsModEnabled("xskills"))
            {
                SkillItem[] mergedModes = GetToolModes(itemslot, (byEntity as EntityPlayer) as IClientPlayer, blockSel);
                if (mergedModes == null || mergedModes.Length == 0)
                {
                    return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
                }
                if (modeIndex == mergedModes.Length - 1)
                {
                    string blockCode = world.BlockAccessor.GetBlock(blockSel.Position).Code.Path;

                    if (blockCode.StartsWith("rock-"))
                    {
                        Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:wellspring"));
                        if (wellSpringBlock != null)
                        {
                            world.BlockAccessor.SetBlock(wellSpringBlock.BlockId, blockSel.Position);
                            return true;
                        }
                    }
                }
                else
                {
                    return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
                }
            }
            else
            {
                if (modeIndex == 1)
                {
                    string blockCode = world.BlockAccessor.GetBlock(blockSel.Position).Code.Path;

                    if (blockCode.StartsWith("rock-"))
                    {
                        Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:wellspring"));
                        if (wellSpringBlock != null)
                        {
                            world.BlockAccessor.SetBlock(wellSpringBlock.BlockId, blockSel.Position);
                            return true;
                        }
                    }
                }
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
        }
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            if (!api.ModLoader.IsModEnabled("xskills"))
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
            return base.GetHeldInteractionHelp(inSlot);
        }
    }
}
