using ProtoBuf;
using System;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class AquiferData
{
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

    public override string ToString() => GetDescription();
    
    //TODO maybe split 'heavy aquifer' and 'detected' part
    public string GetDescription() => AquiferRating switch
    {
        <= 10 => Lang.Get("hydrateordiedrate:aquifer-none"),
        <= 15 => Lang.Get("hydrateordiedrate:aquifer-very-poor", GetTypeDescription()),
        <= 20 => Lang.Get("hydrateordiedrate:aquifer-poor", GetTypeDescription()),
        <= 40 => Lang.Get("hydrateordiedrate:aquifer-light", GetTypeDescription()),
        <= 60 => Lang.Get("hydrateordiedrate:aquifer-moderate", GetTypeDescription()),
        _ => Lang.Get("hydrateordiedrate:aquifer-heavy", GetTypeDescription())
    };

    private string GetTypeDescription() => IsSalty ? Lang.Get("hydrateordiedrate:aquifer-salt") : Lang.Get("hydrateordiedrate:aquifer-fresh");
}
