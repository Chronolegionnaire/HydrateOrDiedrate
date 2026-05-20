using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Wells.WellWater
{
    public class BehaviorPickaxeWellMode : CollectibleBehavior, IDisposable
    {
        private static SkillItem digMode;
        private static SkillItem wellMode;
        private ICoreAPI api;
        private readonly List<SkillItem> customModes = new List<SkillItem>();

        public BehaviorPickaxeWellMode(CollectibleObject collObj) : base(collObj) { }
        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            base.OnLoaded(api);
            bool xskillsEnabled = api.ModLoader.Mods.Any(mod => mod.Info.ModID.StartsWith("xskill"));

            if (!xskillsEnabled)
            {
                CreateDefaultModes(api);
                customModes.AddRange(new[] { digMode, wellMode });

                SkillItem[] existingModes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
                if (existingModes == null)
                {
                    ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "pickaxeToolModes", () => customModes.ToArray());
                }
                else
                {
                    List<SkillItem> list = existingModes.ToList();

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
                CreateXSkillsWellMode(api);
                customModes.Add(wellMode);
                EnsureWellModeRegisteredWithXSkills(api);
            }
        }
        private void CreateDefaultModes(ICoreAPI api)
        {
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
        }
        private void CreateXSkillsWellMode(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
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
                wellMode = new SkillItem
                {
                    Code = new AssetLocation("digwellspring"),
                    Name = Lang.Get("hydrateordiedrate:pickaxewellmode-digwellspring")
                };
            }
        }
        private void EnsureWellModeRegisteredWithXSkills(ICoreAPI api)
        {
            if (wellMode == null) return;

            SkillItem[] xSkillsModes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
            if (xSkillsModes == null)
            {
                IEnumerable<SkillItem> combined = CreateXSkillsToolModes(api);
                if (combined == null) return;

                List<SkillItem> list = combined.ToList();

                if (!list.Any(m => m.Code.Path == wellMode.Code.Path))
                {
                    list.Add(wellMode);
                }

                ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "pickaxeToolModes", () => list.ToArray());
            }
            else
            {
                List<SkillItem> list = xSkillsModes.ToList();

                if (!list.Any(m => m.Code.Path == wellMode.Code.Path))
                {
                    list.Add(wellMode);
                }

                api.ObjectCache["pickaxeToolModes"] = list.ToArray();
            }
        }
        private IEnumerable<SkillItem> CreateXSkillsToolModes(ICoreAPI api)
        {
            Type pickaxeBehaviorType = Type.GetType("XSkills.PickaxeBehavior, xskills");
            if (pickaxeBehaviorType == null)
            {
                return null;
            }

            MethodInfo method = pickaxeBehaviorType.GetMethod(
                "CreateToolModes",
                BindingFlags.Public | BindingFlags.Static);

            if (method == null)
            {
                return null;
            }
            object result = method.Invoke(null, new object[] { api });
            return result as IEnumerable<SkillItem>;
            return null;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            Dispose();
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            SkillItem[] modes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");

            if (modes != null)
            {
                return modes;
            }

            return customModes.ToArray();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            bool xskillsEnabled = api.ModLoader.Mods.Any(mod => mod.Info.ModID.StartsWith("xskill"));

            SkillItem[] modes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");

            if (modes != null && modes.Length > 0)
            {
                toolMode = GameMath.Clamp(toolMode, 0, modes.Length - 1);
            }
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);

            if (!xskillsEnabled && modes != null && toolMode >= 0 && toolMode < modes.Length)
            {
                slot.Itemstack.Attributes.SetString("toolModeCode", modes[toolMode].Code.Path);
            }

            slot.MarkDirty();
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            int toolMode = slot.Itemstack.Attributes.GetInt("toolMode", 0);

            SkillItem[] modes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");

            if (modes == null || modes.Length == 0)
            {
                return 0;
            }

            return GameMath.Clamp(toolMode, 0, modes.Length - 1);
        }

        public override bool OnBlockBrokenWith(
            IWorldAccessor world,
            Entity byEntity,
            ItemSlot itemslot,
            BlockSelection blockSel,
            float dropQuantityMultiplier,
            ref EnumHandling bhHandling)
        {
            if (blockSel == null || byEntity == null || itemslot?.Itemstack == null)
            {
                return false;
            }

            string modeName = "";
            int modeIndex = itemslot.Itemstack.Attributes.GetInt("toolMode", 0);
            SkillItem[] modes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
            if (modes != null && modeIndex >= 0 && modeIndex < modes.Length)
            {
                modeName = modes[modeIndex].Code.Path;
            }

            if (modeName == "digwellspring")
            {
                Block targetBlock = world.BlockAccessor.GetBlock(blockSel.Position);
                if (targetBlock != null && 
                    (targetBlock.Code.Path.StartsWith("rock") || targetBlock.Code.Path.StartsWith("crackedrock")))
                {
                    Block wellSpringBlock = world.GetBlock(new AssetLocation("hydrateordiedrate:wellspring"));
                    if (wellSpringBlock != null)
                    {
                        bhHandling = EnumHandling.PreventDefault;
                        world.RegisterCallback((dt) =>
                        {
                            IBlockAccessor accessor = world.GetBlockAccessor(true, true, true);
                            accessor.ExchangeBlock(wellSpringBlock.BlockId, blockSel.Position);
                            accessor.SpawnBlockEntity("HoD:BlockEntityWellSpring", blockSel.Position, null);
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

            return base.OnBlockBrokenWith(
                world,
                byEntity,
                itemslot,
                blockSel,
                dropQuantityMultiplier,
                ref bhHandling);
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