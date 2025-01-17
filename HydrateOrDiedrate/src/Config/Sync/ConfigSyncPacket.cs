using ProtoBuf;
using System.Collections.Generic;

namespace HydrateOrDiedrate.src.Config.Sync;


[ProtoContract]
public class ConfigSyncPacket
{
    [ProtoMember(1)] public HydrateOrDiedrate.Config.Config ServerConfig { get; set; }

    [ProtoMember(2)] public List<string> HydrationPatches { get; set; }

    [ProtoMember(3)] public List<string> BlockHydrationPatches { get; set; }

    [ProtoMember(4)] public List<string> CoolingPatches { get; set; }
}
