using HydrateOrDiedrate.Thirst;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HydrateOrDiedrate.Hydration.Interfaces;
#nullable enable 

public interface IDrinkingInteraction
{
    void StartDrinking(IWorldAccessor world, BlockSelection blockSel, IPlayer player, PlayerDrinkData drinkData);

    void ContinueDrinking(IWorldAccessor world, BlockSelection blockSel, IServerPlayer player, PlayerDrinkData drinkData);

    void FinishDrinking(IWorldAccessor world, BlockSelection blockSel, IServerPlayer player, PlayerDrinkData drinkData);
}
