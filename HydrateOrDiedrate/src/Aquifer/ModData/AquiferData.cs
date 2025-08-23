using ProtoBuf;

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
}
