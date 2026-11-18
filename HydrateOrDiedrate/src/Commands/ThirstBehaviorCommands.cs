using InsanityLib.Auto.Command;
using System.ComponentModel;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate.Commands;

public static class ThirstBehaviorCommands
{
    public const string BehaviorMissing = "hydrateordiedrate:cmd-hydration-behaviormissing";

    [Description("hydrateordiedrate:cmd-hydration-set-desc")]
    [AutoCommand(Path = "hydration", Name = "set", RequiredPrivelege = "controlserver", Side = EnumAppSide.Server)]
    public static TextCommandResult SetHydration(Entity entity, float thirstValue)
    {
        if (entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return TextCommandResult.Error(BehaviorMissing, BehaviorMissing);

        thirstBehavior.CurrentHydration = thirstValue;

        return new TextCommandResult
        {
            Status = EnumCommandStatus.Success,
            StatusMessage = "hydrateordiedrate:cmd-hydration-set",
            MessageParams = [entity.GetName(), thirstBehavior.CurrentHydration, thirstValue]
        };
    }

    [Description("hydrateordiedrate:cmd-hydration-get-desc")]
    [AutoCommand(Path = "hydration", Name = "get", RequiredPrivelege = "controlserver", Side = EnumAppSide.Server)]
    public static TextCommandResult GetHydration(Entity entity)
    {
        if (entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return TextCommandResult.Error(BehaviorMissing, BehaviorMissing);

        return new TextCommandResult
        {
            Status = EnumCommandStatus.Success,
            StatusMessage = "hydrateordiedrate:cmd-hydration-get",
            MessageParams = [entity.GetName(), thirstBehavior.CurrentHydration]
        };
    }

    [Description("hydrateordiedrate:cmd-nutrientdeficit-set-desc")]
    [AutoCommand(Path = "nutrientdeficit", Name = "set", RequiredPrivelege = "controlserver", Side = EnumAppSide.Server)]
    public static TextCommandResult SetNutrientDeficit(Entity entity, float nutrientDeficitValue)
    {
        if (entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return TextCommandResult.Error(BehaviorMissing, BehaviorMissing);

        thirstBehavior.NutritionDeficitAmount = nutrientDeficitValue;

        return new TextCommandResult
        {
            Status = EnumCommandStatus.Success,
            StatusMessage = "hydrateordiedrate:cmd-nutrientdeficit-set",
            MessageParams = [entity.GetName(), thirstBehavior.NutritionDeficitAmount]
        };
    }

    [Description("hydrateordiedrate:cmd-nutrientdeficit-get-desc")]
    [AutoCommand(Path = "nutrientdeficit", Name = "get", RequiredPrivelege = "controlserver", Side = EnumAppSide.Server)]
    public static TextCommandResult GetNutrientDeficit(Entity entity)
    {
        if (entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return TextCommandResult.Error(BehaviorMissing, BehaviorMissing);

        return new TextCommandResult
        {
            Status = EnumCommandStatus.Success,
            StatusMessage = "hydrateordiedrate:cmd-nutrientdeficit-get",
            MessageParams = [entity.GetName(), thirstBehavior.NutritionDeficitAmount]
        };
    }

    [Description("hydrateordiedrate:cmd-hydrationlossdelay-set-desc")]
    [AutoCommand(Path = "hydrationlossdelay", Name = "set", RequiredPrivelege = "controlserver", Side = EnumAppSide.Server)]
    public static TextCommandResult SetHydrationLossDelay(Entity entity, int hydrationLossDelayInSeconds)
    {
        if (entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return TextCommandResult.Error(BehaviorMissing, BehaviorMissing);

        thirstBehavior.HydrationLossDelay = hydrationLossDelayInSeconds;

        return new TextCommandResult
        {
            Status = EnumCommandStatus.Success,
            StatusMessage = "hydrateordiedrate:cmd-hydrationlossdelay-set",
            MessageParams = [entity.GetName(), thirstBehavior.HydrationLossDelay, hydrationLossDelayInSeconds]
        };
    }
    
    [Description("hydrateordiedrate:cmd-hydrationlossdelay-set-desc")]
    [AutoCommand(Path = "hydrationlossdelay", Name = "get", RequiredPrivelege = "controlserver", Side = EnumAppSide.Server)]
    public static TextCommandResult GetHydrationLossDelay(Entity entity)
    {
        if (entity.GetBehavior<EntityBehaviorThirst>() is not { } thirstBehavior) return TextCommandResult.Error(BehaviorMissing, BehaviorMissing);

        return new TextCommandResult
        {
            Status = EnumCommandStatus.Success,
            StatusMessage = "hydrateordiedrate:cmd-hydrationlossdelay-get",
            MessageParams = [entity.GetName(), thirstBehavior.HydrationLossDelay]
        };
    }
}
