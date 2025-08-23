using ProtoBuf;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class AquiferChunkData
{
    [ProtoMember(1)]
    public AquiferData Data { get; set; }

    [ProtoMember(3)]
    public int Version { get; set; }
}