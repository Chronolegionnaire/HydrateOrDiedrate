namespace HydrateOrDiedrate;

/// <summary>
/// Stat keys exposed by HydrateOrDiedrate so other mods (races, traits, classes, etc.)
/// <br/>can adjust thirst rate and cooling without modifying HoD directly.
/// <br/>
/// <br/>Example: Desert-born race with slower thirst loss and improved cooling
/// <br/>- entity.Stats.Set(HoDStats.ThirstRateMul, "desertRace", -0.6f, false); // 60% reduction
/// <br/>- entity.Stats.Set(HoDStats.CoolingMul,    "desertRace", 0.5f, false); // 50% increase
/// </summary>
public static class HoDStats
{
    public const string ThirstRateMul = "HoD:ThirstRateMul";
    public const string CoolingMul    = "HoD:CoolingMul";
}