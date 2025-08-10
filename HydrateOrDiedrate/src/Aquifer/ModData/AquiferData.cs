using ProtoBuf;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class AquiferData
{
    [ProtoMember(1)]
    public int AquiferRating { get; set; }
    [ProtoMember(2)]
    public bool IsSalty { get; set; }
}
