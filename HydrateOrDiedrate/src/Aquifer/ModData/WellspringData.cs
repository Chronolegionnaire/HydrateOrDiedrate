using ProtoBuf;
using System.Collections.Generic;

namespace HydrateOrDiedrate.Aquifer.ModData;

[ProtoContract]
public class WellspringData
{
    [ProtoMember(1)]
    public List<WellspringInfo> Wellsprings { get; set; }
}