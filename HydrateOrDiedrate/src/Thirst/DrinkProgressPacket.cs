using ProtoBuf;

namespace HydrateOrDiedrate
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class DrinkProgressPacket
    {
        public float Progress { get; set; }
        public bool IsDrinking { get; set; }
        public bool IsDangerous { get; set; }
    }
}