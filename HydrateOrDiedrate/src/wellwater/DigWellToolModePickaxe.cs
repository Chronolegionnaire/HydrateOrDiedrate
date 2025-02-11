﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.wellwater
{
    public class BehaviorPickaxeWellMode : CollectibleBehavior, IDisposable
    {
        private static SkillItem digMode;
        private static SkillItem wellMode;
        private ICoreAPI api;
        private List<SkillItem> customModes = new List<SkillItem>();

        public BehaviorPickaxeWellMode(CollectibleObject collObj) : base(collObj) { }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            base.OnLoaded(api);
            bool xskillsEnabled = api.ModLoader.IsModEnabled("xskills");

            if (!xskillsEnabled)
            {
                if (api.Side == EnumAppSide.Client)
                {
                    ICoreClientAPI capi = api as ICoreClientAPI;
                    digMode = new SkillItem
                    {
                        Code = new AssetLocation("digmode"),
                        Name = Lang.Get("Dig Mode")
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                        new AssetLocation("game:textures/icons/rocks.svg"),
                        48, 48, 5, ColorUtil.WhiteArgb));

                    wellMode = new SkillItem
                    {
                        Code = new AssetLocation("digwellspring"),
                        Name = Lang.Get("Dig Well Spring")
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                        new AssetLocation("hydrateordiedrate:textures/icons/well.svg"),
                        48, 48, 5, ColorUtil.WhiteArgb));
                }
                else
                {
                    digMode = new SkillItem
                    {
                        Code = new AssetLocation("digmode"),
                        Name = "Dig Mode"
                    };

                    wellMode = new SkillItem
                    {
                        Code = new AssetLocation("digwellspring"),
                        Name = "Dig Well Spring"
                    };
                }
                customModes.AddRange(new[] { digMode, wellMode });
            }
            else
            {
                if (api.Side == EnumAppSide.Client)
                {
                    ICoreClientAPI capi = api as ICoreClientAPI;
                    wellMode = new SkillItem
                    {
                        Code = new AssetLocation("digwellspring"),
                        Name = Lang.Get("Dig Well Spring")
                    }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(
                        new AssetLocation("hydrateordiedrate:textures/icons/well.svg"),
                        48, 48, 5, ColorUtil.WhiteArgb));
                    customModes.Add(wellMode);
                    SkillItem[] xSkillsModes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
                    if (xSkillsModes == null)
                    {
                        IEnumerable<SkillItem> combined = CreateXSkillsToolModes(api);
                        if (combined != null)
                        {
                            List<SkillItem> list = combined.ToList();
                            foreach (SkillItem mode in customModes)
                            {
                                if (!list.Any(cm => cm.Code.Path == mode.Code.Path))
                                {
                                    list.Add(mode);
                                }
                            }
                            ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "pickaxeToolModes", () => list.ToArray());
                        }
                    }
                    else
                    {
                        List<SkillItem> list = xSkillsModes.ToList();
                        foreach (SkillItem mode in customModes)
                        {
                            if (!list.Any(m => m.Code.Path == mode.Code.Path))
                            {
                                list.Add(mode);
                            }
                        }
                        api.ObjectCache["pickaxeToolModes"] = list.ToArray();
                    }
                }
                else
                {
                    wellMode = new SkillItem
                    {
                        Code = new AssetLocation("digwellspring"),
                        Name = "Dig Well Spring"
                    };
                    customModes.Add(wellMode);
                }
            }
        }
        private IEnumerable<SkillItem> CreateXSkillsToolModes(ICoreAPI api)
        {
            try
            {
                Type pickaxeBehaviorType = Type.GetType("XSkills.PickaxeBehaivor, xskills");
                if (pickaxeBehaviorType != null)
                {
                    MethodInfo method = pickaxeBehaviorType.GetMethod("CreateToolModes", BindingFlags.Public | BindingFlags.Static);
                    if (method != null)
                    {
                        object result = method.Invoke(null, new object[] { api });
                        return result as IEnumerable<SkillItem>;
                    }
                }
            }
            catch (Exception e)
            {
                api.Logger.Error("Error invoking XSkills.PickaxeBehaivor.CreateToolModes: " + e);
            }
            return null;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            foreach (SkillItem mode in customModes)
            {
                mode?.Dispose();
            }
            customModes.Clear();
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            if (api.ModLoader.IsModEnabled("xskills"))
            {
                return ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
            }
            else
            {
                return customModes.ToArray();
            }
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            if (api.ModLoader.IsModEnabled("xskills"))
            {
                slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
            }
            else
            {
                SkillItem[] combined = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
                if (combined == null || combined.Length == 0)
                {
                    slot.Itemstack.Attributes.SetString("toolMode", "digmode");
                }
                else
                {
                    toolMode = GameMath.Clamp(toolMode, 0, combined.Length - 1);
                    string modeName = combined[toolMode].Code.Path;
                    slot.Itemstack.Attributes.SetString("toolMode", modeName);
                }
            }
            slot.MarkDirty();
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (api.ModLoader.IsModEnabled("xskills"))
            {
                return slot.Itemstack.Attributes.GetInt("toolMode", 0);
            }
            else
            {
                string storedMode = slot.Itemstack.Attributes.GetString("toolMode", "digmode");
                SkillItem[] combined = GetToolModes(slot, byPlayer as IClientPlayer, blockSel);
                if (combined == null || combined.Length == 0)
                {
                    return 0;
                }
                for (int i = 0; i < combined.Length; i++)
                {
                    if (combined[i].Code.Path == storedMode)
                        return i;
                }
                return 0;
            }
        }

        public override bool OnBlockBrokenWith(
            IWorldAccessor world,
            Entity byEntity,
            ItemSlot itemslot,
            BlockSelection blockSel,
            float dropQuantityMultiplier,
            ref EnumHandling bhHandling)
        {
            if (blockSel == null || byEntity == null)
                return false;

            string modeName = "";
            if (api.ModLoader.IsModEnabled("xskills"))
            {
                int modeIndex = itemslot.Itemstack.Attributes.GetInt("toolMode", 0);
                SkillItem[] modes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
                if (modes != null && modeIndex >= 0 && modeIndex < modes.Length)
                {
                    modeName = modes[modeIndex].Code.Path;
                }
            }
            else
            {
                modeName = itemslot.Itemstack.Attributes.GetString("toolMode", "digmode");
            }

            if (modeName == "digwellspring")
            {
                Block targetBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                if (targetBlock?.Code.Path.StartsWith("rock-") == true)
                {
                    Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:wellspring"));
                    if (wellSpringBlock != null)
                    {
                        bhHandling = EnumHandling.PreventDefault;
                        api.World.RegisterCallback((float dt) =>
                        {
                            world.BlockAccessor.ExchangeBlock(wellSpringBlock.BlockId, blockSel.Position);
                            world.BlockAccessor.SpawnBlockEntity("BlockEntityWellSpring", blockSel.Position, null);
                        }, 200);
                        return true;
                    }
                }
            }
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
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
