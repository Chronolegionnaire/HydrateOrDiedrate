using ProtoBuf;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.wellwater
{
    [ProtoContract]
    public class WellSpringBlockPacket
    {
        [ProtoMember(1)] public int BlockId { get; set; }

        [ProtoMember(2)] public BlockPos Position { get; set; }
    }
}