using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace HydrateOrDiedrate.Config.Patching;

public class PatchCollection<T> where T : PatchBase, new()
{
    public int Priority { get; set; }

    public T[] Patches { get; set; } = [];

    public void ApplyPatches(IEnumerable<CollectibleObject> collectibles)
    {
        for (int i = 0; i < Patches.Length; i++) Patches[i].PreCompile();

        foreach (var collectible in collectibles)
        {
            for (int i = 0; i < Patches.Length; i++)
            {
                T patch = Patches[i];
                if(!patch.Target.IsMatch(collectible)) continue;

                patch.Apply(collectible);
            }
        }
    }

    public void MergeMissing(PatchCollection<T> otherCollection)
    {
        var missingPatches = otherCollection.Patches
            .Where(patch => !Array.Exists(Patches, existingPatch => existingPatch.Code == patch.Code))
            .ToArray();
        
        Patches = Patches.Append(missingPatches);
    }

    public static PatchCollection<T> GetMerged(ICoreAPI api, string path)
    {
        var defaults = api.Assets.GetMany<PatchCollection<T>>(api.Logger, "config/" + path.ToLower()).Values.OrderByDescending(collection => collection.Priority).ToArray();
        int start = 0;
        
        PatchCollection<T> result = null;
        try
        {
            result = api.LoadModConfig<PatchCollection<T>>(path);
            if(result is null)
            {
                if(defaults.Length > 0)
                {
                    result = defaults[0];
                    start = 1;
                }
                else result = new PatchCollection<T>();
            }
            else
            {
                result.Patches = [.. result.Patches.Where(static patch => patch is not null)];
            }

            for(int i = start; i < defaults.Length; i++)
            {
                result.MergeMissing(defaults[i]);
            }

            api.StoreModConfig(result, path);
        }
        catch(Exception ex)
        {
            api.Logger.Error(ex);
        }
        return result ?? new();
    }
}
