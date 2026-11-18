using HydrateOrDiedrate.Wells.Aquifer;
using InsanityLib.Auto.Command;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Commands;

public static class AquiferCommands
{
    [Description("hydrateordiedrate:setaquifer-description")]
    [AutoCommand(Path = "", RequiredPrivelege = "controlserver")]
    public static TextCommandResult SetAquifer(ICoreServerAPI sapi, BlockPos pos, [Range(0, 100)] int rating, bool isSalty = false) => AquiferManager.TrySetAquiferRating(sapi.World, pos, rating, isSalty)
            ? TextCommandResult.Success(Lang.Get("hydrateordiedrate:setaquifer-success", rating))
            : TextCommandResult.Error(Lang.Get("hydrateordiedrate:setaquifer-failed"));

    [Description("hydrateordiedrate:getaquifer-description")]
    [AutoCommand(Path = "", RequiredPrivelege = "controlserver")]
    public static TextCommandResult GetAquifer(ICoreServerAPI sapi, BlockPos pos) => AquiferManager.GetAquiferChunkData(sapi.World, pos, sapi.Logger) is { } aqData
            ? TextCommandResult.Success(Lang.Get("hydrateordiedrate:getaquifer-success", aqData.Data.AquiferRating, aqData.Data.IsSalty))
            : TextCommandResult.Error(Lang.Get("hydrateordiedrate:getaquifer-not-found"));
}
