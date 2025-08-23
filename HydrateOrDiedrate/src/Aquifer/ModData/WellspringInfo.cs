using ProtoBuf;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class WellspringInfo
{
    [ProtoMember(1)]
    public BlockPos Position { get; set; }
}
