using ProtoBuf;
using System;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class WaterCounts
{
    [ProtoMember(1)] public int NormalWaterBlockCount;
    [ProtoMember(2)] public int SaltWaterBlockCount;
    [ProtoMember(3)] public int BoilingWaterBlockCount;

    public int GetTotalWaterBlockCount() => NormalWaterBlockCount + SaltWaterBlockCount + BoilingWaterBlockCount;

    public void Increment(EWaterKind kind)
    {
        switch (kind)
        {
            case EWaterKind.None: break;

            case EWaterKind.Normal:
            case EWaterKind.Ice:
                NormalWaterBlockCount++;
                break;

            case EWaterKind.Salt:
                SaltWaterBlockCount++;
                break;

            case EWaterKind.Boiling:
                BoilingWaterBlockCount++;
                break;

            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}
