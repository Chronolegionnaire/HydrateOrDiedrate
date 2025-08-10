using ProtoBuf;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class WaterCounts
{
    [ProtoMember(1)] public int NormalWaterBlockCount;
    [ProtoMember(2)] public int SaltWaterBlockCount;
    [ProtoMember(3)] public int BoilingWaterBlockCount;

    public int GetTotalWaterBlockCount() => NormalWaterBlockCount + SaltWaterBlockCount + BoilingWaterBlockCount;
}
