﻿using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate;

public class RegisterRainHarvester : BlockEntityBehavior
{
    private RainHarvesterManager harvesterManager;
    private RainHarvesterData harvesterData;

    public RegisterRainHarvester(BlockEntity blockEntity) : base(blockEntity) { }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        if (api.Side == EnumAppSide.Server)
        {
            harvesterManager = api.ModLoader.GetModSystem<HydrateOrDiedrateModSystem>().GetRainHarvesterManager();
            harvesterData = new RainHarvesterData(Blockentity, 1.0f);
            harvesterManager.RegisterHarvester(Blockentity.Pos, harvesterData);
        }
    }

    public override void OnBlockRemoved()
    {
        if (harvesterManager != null && harvesterData != null)
        {
            harvesterManager.UnregisterHarvester(Blockentity.Pos);
        }
        base.OnBlockRemoved();
    }

    // This method is called when the block is unloaded from memory (e.g., when a chunk is unloaded)
    public override void OnBlockUnloaded()
    {
        if (harvesterManager != null && harvesterData != null)
        {
            harvesterManager.UnregisterHarvester(Blockentity.Pos);
        }
        base.OnBlockUnloaded();
    }
}