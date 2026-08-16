using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.Config.Patching;

public abstract class PatchBase
{
    public bool OverwriteExisting { get; set; }

    public abstract string Code { get; set; }

    public abstract float? Value { get; set; }
    public abstract Dictionary<string, float> ValueByType { get; set; }

    [JsonIgnore]
    public CollectibleTarget Target { get; private set; }

    private (CollectibleTarget target, float value)[] CompiledValues { get; set; }

    public void PreCompile()
    {
        Target ??= new CollectibleTarget(Code);

        if(ValueByType is not null)
        {
            if(ValueByType.TryGetValue("*", out var value)) //Extract the catchAll
            {
                Value = value;
                ValueByType.Remove("*");
            }

            if(ValueByType.Count > 0) CompiledValues = [.. ValueByType.Select(pair =>  (new CollectibleTarget(pair.Key), pair.Value))];
        }
    }

    public void Apply(CollectibleObject collectible)
    {
        collectible.Attributes ??= new JsonObject(new JObject()); //Ensure attributes exist

        if(CompiledValues is null)
        {
            if(Value is not null) Apply(collectible, Value.Value);
            return;
        }

        for (int i = 0; i < CompiledValues.Length; i++)
        {
            (CollectibleTarget target, float value) = CompiledValues[i];
            
            if(!target.IsMatch(collectible)) continue;
            Apply(collectible, value);
            return;
        }
        
        if(Value is not null) Apply(collectible, Value.Value);
    }

    public abstract void Apply(CollectibleObject collectible, float value);
}
