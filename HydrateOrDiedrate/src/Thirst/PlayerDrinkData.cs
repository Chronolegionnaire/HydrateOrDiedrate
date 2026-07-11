using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Thirst;
#nullable enable

public class PlayerDrinkData
{
    [MemberNotNullWhen(true, nameof(IsDrinking))]
    public BlockPos? DrinkPos { get; private set; }

    public bool IsDrinking { get; private set; } = false;

    public long DrinkStartTime { get; private set; } = 0;

    public bool IsDangerous { get; set; } = false;

    public void StartDrinking(IWorldAccessor world, BlockPos pos, bool isDangerous)
    {
        if(IsDrinking) StopDrinking();
        DrinkPos = pos;
        DrinkStartTime = world.ElapsedMilliseconds;
        IsDrinking = true;
        IsDangerous = isDangerous;
    }

    public void StopDrinking()
    {
        if(!IsDrinking) return;
        DrinkPos = null;
        IsDrinking = false;
    }
    
    private const double drinkDuration = 1000; //TODO config

    public float GetDrinkProgress(IWorldAccessor world) => IsDrinking ? (float)(world.ElapsedMilliseconds - DrinkStartTime) / (float)drinkDuration : 0f;
}