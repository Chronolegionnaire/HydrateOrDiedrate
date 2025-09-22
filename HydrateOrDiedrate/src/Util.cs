using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

    public static int GetStoredLocalIndex(this CodeInstruction instruction)
    {
        if(!instruction.IsStloc()) throw new ArgumentException("passed instruction is not for storing locals", nameof(instruction));
        if(instruction.operand is LocalBuilder builder) return builder.LocalIndex;
        return instruction.LocalIndex();
    }

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

    /// <summary>
    /// Helper method for cloning objects that ensures the higest clone method is called
    /// (in case someone overrides the method and just hides it with `new` keyword)
    /// </summary>
    public static bool TryClone<T>(this T instance, out T result)
    {
        result = default;
        if(instance is null) return false;
        try
        {
            var type = instance.GetType();
            var cloneMethods = type.GetMethods(AccessTools.all)
                .Where(method => type.IsAssignableFrom(method.ReturnType) && !method.IsStatic && method.GetParameters().Length == 0)
                .ToArray();

            if(cloneMethods.Length != 1) return false;
            
            result = (T)cloneMethods[0].Invoke(instance, []);

            return result is not null;
        }
        catch
        {
            return false;
        }
    }
}
