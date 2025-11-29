namespace HydrateOrDiedrate
{
    // <summary>
    // Stat keys exposed by HydrateOrDiedrate so other mods (races, traits, classes, etc.)
    // can adjust thirst rate and cooling without modifying HoD directly.
    //
    // Example: Desert-born race with slower thirst loss and improved cooling
    //     entity.Stats.Set(HoDStats.ThirstRateMul, "desertRace", 0.6f, false);
    //     entity.Stats.Set(HoDStats.CoolingMul,    "desertRace", 1.5f, false);
    // </summary>
    public static class HoDStats
    {
        public const string ThirstRateMul = "HoD:ThirstRateMul";
        public const string CoolingMul    = "HoD:CoolingMul";
    }
}