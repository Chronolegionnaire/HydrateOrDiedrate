using ProtoBuf;
using System;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Wells.Aquifer.ModData;

[ProtoContract]
public class AquiferData
{
    public const string OreReadingKey = "$aquifer$";

    /// <summary>
    /// The currently known aquifer rating depending on wether smoothing is enabled and has happend this will either return <see cref="AquiferRatingRaw"/> or <see cref="AquiferRatingSmoothed"/>
    /// </summary>
    public int AquiferRating => AquiferRatingSmoothed ?? AquiferRatingRaw;

    [ProtoMember(1)]
    public int AquiferRatingRaw { get; set; }

    [ProtoMember(3)]
    public int? AquiferRatingSmoothed { get; set; }

    [ProtoMember(2)]
    public bool IsSalty { get; set; }

    public static implicit operator OreReading(AquiferData aquiferData) => new()
    {
        DepositCode = aquiferData.IsSalty ? "salty" : "fresh",
        PartsPerThousand = aquiferData.AquiferRating,
        TotalFactor = (int)Math.Floor(aquiferData.AquiferRating / (100.0 / 6))
    };

    public override string ToString() => GetDescription(AquiferRating, IsSalty);

    public string GetProPickDescription() => GetProPickDescription(AquiferRating, IsSalty);

    public static string GetProPickDescription(int aquiferRating, bool isSalty) => Lang.Get("hydrateordiedrate:aquifer-detected", GetDescription(aquiferRating, isSalty));
    
    public static string GetDescription(int aquiferRating, bool isSalty) => Lang.Get("hydrateordiedrate:aquifer", aquiferRating < MinimumAquiferRatingForDetection ? GetStrengthDescription(aquiferRating) : $"{GetStrengthDescription(aquiferRating)} {GetTypeDescription(isSalty).ToLower()}");

    public const int MinimumAquiferRatingForDetection = 10;
    public static string GetStrengthDescription(int aquiferRating) => aquiferRating switch
    {
        <= MinimumAquiferRatingForDetection => Lang.Get("hydrateordiedrate:none"),
        <= 15 => Lang.Get("hydrateordiedrate:verypoor"),
        <= 20 => Lang.Get("hydrateordiedrate:poor"),
        <= 40 => Lang.Get("hydrateordiedrate:light"),
        <= 60 => Lang.Get("hydrateordiedrate:moderate"),
        _ => Lang.Get("hydrateordiedrate:heavy")
    };

    public static string GetTypeDescription(bool isSalty) => isSalty ? Lang.Get("hydrateordiedrate:saltwater") : Lang.Get("hydrateordiedrate:freshwater");
}
