using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate;

public static class Util
{
    public static T JsonCopy<T> (this T obj) where T : class => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));

    public static bool HasToolInOffHand(this Entity entity, EnumTool tool) => entity is EntityAgent entityAgent && entityAgent.LeftHandItemSlot?.Itemstack?.Collectible?.Tool == tool;
}
