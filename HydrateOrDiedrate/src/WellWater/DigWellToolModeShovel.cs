using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.wellwater
{
    public class BehaviorShovelWellMode : CollectibleBehavior, IDisposable
    {
        private static SkillItem digMode;
        private static SkillItem wellMode;

        private ICoreAPI api;
        private readonly List<SkillItem> customModes = new List<SkillItem>();

        public BehaviorShovelWellMode(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;

                digMode = new SkillItem
                {
                    Code = new AssetLocation("digmode"),
                    Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digmode")
                }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                    new AssetLocation("game:textures/icons/rocks.svg"),
                    48, 48, 5, ColorUtil.WhiteArgb));

                wellMode = new SkillItem
                {
                    Code = new AssetLocation("digwellspring"),
                    Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digwellspring")
                }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                    new AssetLocation("hydrateordiedrate:textures/icons/well.svg"),
                    48, 48, 5, ColorUtil.WhiteArgb));
            }
            else
            {
                digMode = new SkillItem
                {
                    Code = new AssetLocation("digmode"),
                    Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digmode")
                };
                wellMode = new SkillItem
                {
                    Code = new AssetLocation("digwellspring"),
                    Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digwellspring")
                };
            }

            customModes.Add(digMode);
            customModes.Add(wellMode);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            foreach (SkillItem mode in customModes)
            {
                mode?.Dispose();
            }
            customModes.Clear();
            base.OnUnloaded(api);
        }
        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return customModes.ToArray();
        }
        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            SkillItem[] modes = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
            if (modes == null || modes.Length == 0)
            {
                slot.Itemstack.Attributes.SetString("toolMode", "digmode");
            }
            else
            {
                int clamped = GameMath.Clamp(toolMode, 0, modes.Length - 1);
                string modeName = modes[clamped].Code.Path;
                slot.Itemstack.Attributes.SetString("toolMode", modeName);
            }
            slot.MarkDirty();
        }
        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            string current = slot.Itemstack.Attributes.GetString("toolMode", "digmode");
            SkillItem[] modes = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
            if (modes == null || modes.Length == 0) return 0;

            for (int i = 0; i < modes.Length; i++)
            {
                if (modes[i].Code.Path == current) return i;
            }
            return 0;
        }
        public override bool OnBlockBrokenWith(
            IWorldAccessor world,
            Entity byEntity,
            ItemSlot itemslot,
            BlockSelection blockSel,
            float dropQuantityMultiplier,
            ref EnumHandling handling)
        {
            if (blockSel == null || byEntity == null)
            {
                return false;
            }
            string modeName = itemslot.Itemstack.Attributes.GetString("toolMode", "digmode");

            if (modeName == "digwellspring")
            {
                Block targetBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                if (targetBlock != null)
                {
                    string path = targetBlock.Code?.Path ?? "";
                    if (path.StartsWith("soil-") || path.StartsWith("gravel-") || path.StartsWith("sand-"))
                    {
                        Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:wellspring"));
                        if (wellSpringBlock != null)
                        {
                            handling = EnumHandling.PreventDefault;
                            world.RegisterCallback((dt) =>
                            {
                                IBlockAccessor accessor = world.GetBlockAccessor(true, true, true);
                                accessor.ExchangeBlock(wellSpringBlock.BlockId, blockSel.Position);
                                accessor.SpawnBlockEntity("BlockEntityWellSpring", blockSel.Position, null);
                                if (api.Side == EnumAppSide.Client)
                                {
                                    WellSpringBlockPacket packet = new WellSpringBlockPacket
                                    {
                                        BlockId = wellSpringBlock.BlockId,
                                        Position = blockSel.Position
                                    };
                                    ICoreClientAPI capi = api as ICoreClientAPI;
                                    capi?.Network
                                        .GetChannel("hydrateordiedrate")
                                        .SendPacket(packet);
                                }
                            }, 5);

                            return true;
                        }
                    }
                }
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref handling);
        }

        public void Dispose()
        {
            foreach (SkillItem mode in customModes)
            {
                mode?.Dispose();
            }
            customModes.Clear();
        }
    }
}
