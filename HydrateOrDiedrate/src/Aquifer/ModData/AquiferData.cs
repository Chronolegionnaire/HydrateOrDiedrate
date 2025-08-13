using ProtoBuf;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class AquiferData
{
    
    public int AquiferRating => AquiferRatingSmoothed ?? AquiferRatingRaw;

    [ProtoMember(1)]
    public int AquiferRatingRaw { get; set; }

    [ProtoMember(3)]
    public int? AquiferRatingSmoothed { get; set; }

    [ProtoMember(2)]
    public bool IsSalty { get; set; }
}
