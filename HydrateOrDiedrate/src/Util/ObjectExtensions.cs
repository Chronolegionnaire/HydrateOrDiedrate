using Newtonsoft.Json;

namespace HydrateOrDiedrate.Util;

public static class ObjectExtensions
{
    public static T JsonCopy<T> (this T obj) where T : class => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
}
