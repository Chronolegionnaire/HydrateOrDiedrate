using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate;

public static class Util
{
    public static T JsonCopy<T> (this T obj) where T : class => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));

    public static bool HasToolInOffHand(this Entity entity, EnumTool tool) => entity is EntityAgent entityAgent && entityAgent.LeftHandItemSlot?.Itemstack?.Collectible?.Tool == tool;

    /// <summary>
    /// Protect number against turning into NaN or Infinity.
    /// </summary>
    /// <param name="value">the value to check</param>
    /// <param name="defaultValue">what to return if the value was Nan or Infinity</param>
    /// <returns>The value if not NaN or Infinity otherwise the defaultValue</returns>
    public static float GuardFinite(this float value, float defaultValue = 0f) => float.IsFinite(value) ? value : defaultValue;

    public static bool IsChunkValid(this IWorldChunk chunk) => chunk is not null && !chunk.Disposed && chunk.Data is not null && chunk.Data.Length > 0;

    public static Type FindGenericInterfaceDefinition(this Type type, Type genericInterfaceType) =>
        type.GetInterfaces()
        .SingleOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericInterfaceType);

    public static ReadOnlySpan<char> ExtractSegment(this ReadOnlySpan<char> str, char seperator, out ReadOnlySpan<char> remainder)
    {
        var index = str.IndexOf(seperator);
        if(index == -1)
        {
            remainder = [];
            return str;
        }

        remainder = str[(index + 1)..];
        return str[..index];
    }

    public static void SetValue(this MemberInfo memberInfo, object value, object instance = null)
    {
        switch (memberInfo)
        {
            case PropertyInfo property:
                property.SetValue(instance, value);
                break;
            case FieldInfo field:
                field.SetValue(instance, value);
                break;
        }
    }

    public static bool CanSetValue(this MemberInfo memberInfo) => memberInfo switch
    {
        PropertyInfo property => property.CanWrite && property.GetIndexParameters().Length == 0,
        FieldInfo => true,
        _ => false,
    };
}
