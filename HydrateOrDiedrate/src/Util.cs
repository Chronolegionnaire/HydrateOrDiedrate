using HarmonyLib;
using Newtonsoft.Json;
using System;
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
}
