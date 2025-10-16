using ProtoBuf;

namespace HydrateOrDiedrate.Piping.Networking
{
    [ProtoContract]
    public class PumpParticleBurstPacket
    {
        [ProtoMember(1)]  public double PosX { get; set; }
        [ProtoMember(2)]  public double PosY { get; set; }
        [ProtoMember(3)]  public double PosZ { get; set; }

        [ProtoMember(4)]  public float DirX { get; set; }
        [ProtoMember(5)]  public float DirY { get; set; }
        [ProtoMember(6)]  public float DirZ { get; set; }

        [ProtoMember(7)]  public int   Quantity  { get; set; }
        [ProtoMember(8)]  public float Radius    { get; set; }
        [ProtoMember(9)]  public float Scale     { get; set; }
        [ProtoMember(10)] public float Speed     { get; set; }
        [ProtoMember(11)] public float VelJitter { get; set; }
        [ProtoMember(12)] public float LifeMin   { get; set; }
        [ProtoMember(13)] public float LifeMax   { get; set; }
        [ProtoMember(14)] public float Gravity   { get; set; }

        [ProtoMember(15)] public byte[] StackBytes { get; set; }
    }
}