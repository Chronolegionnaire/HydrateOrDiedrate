using Newtonsoft.Json;
using System;

namespace HydrateOrDiedrate;

public static class Util
{
    public static T JsonCopy<T> (this T obj) where T : class => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
}
