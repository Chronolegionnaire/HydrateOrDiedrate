﻿using Newtonsoft.Json;
using System.ComponentModel;

namespace HydrateOrDiedrate.Config.SubConfigs;

//TODO: Why not just use the built in XLib config system? this would allow for more flexibility and easier integration.
public class XLibConfig
{
    [DefaultValue(3d)]
    public float DromedaryMultiplierPerLevel { get; set; } = 3f;
    
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public float[] EquatidianCoolingMultipliers { get; set; } = [1.25f, 1.5f, 2.0f];
}
