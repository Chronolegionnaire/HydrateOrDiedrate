using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods.NoObf;

namespace HydrateOrDiedrate;

public class WaterPatches
{
    public void PrepareWaterSatietyPatches(ICoreAPI api)
    {
        float waterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.WaterSatiety;
        float saltWaterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.SaltWaterSatiety;
        float boilingWaterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.BoilingWaterSatiety;
        float rainWaterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.RainWaterSatiety;
        float distilledWaterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.DistilledWaterSatiety;
        float boiledWaterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.BoiledWaterSatiety;
        float boiledRainWaterSatiety = HydrateOrDiedrateModSystem.LoadedConfig.BoiledRainWaterSatiety;

        ApplySatietyPatch(api, "game:itemtypes/liquid/waterportion.json", waterSatiety);
        ApplySatietyPatch(api, "game:itemtypes/liquid/saltwaterportion.json", saltWaterSatiety);
        ApplySatietyPatch(api, "game:itemtypes/liquid/boilingwaterportion.json", boilingWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/rainwaterportion.json", rainWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/distilledwaterportion.json", distilledWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledwaterportion.json", boiledWaterSatiety);
        ApplySatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledrainwaterportion.json", boiledRainWaterSatiety);
    }

    private void ApplySatietyPatch(ICoreAPI api, string jsonFilePath, float satietyValue)
    {
        JsonPatch ensureNutritionProps = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre",
            Value = new JsonObject(JToken.FromObject(new { })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchSatiety = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre/satiety",
            Value = new JsonObject(JToken.FromObject(satietyValue)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchFoodCategory = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/attributes/waterTightContainerProps/nutritionPropsPerLitre/foodcategory",
            Value = new JsonObject(JToken.FromObject("NoNutrition")),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:ensureNutritionProps"), ensureNutritionProps, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicsatietypatch"), patchSatiety, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicfoodcategorypatch"), patchFoodCategory, ref applied, ref notFound, ref errorCount);
    }

    public void PrepareWellWaterSatietyPatches(ICoreAPI api)
    {
        var wellWaterSatietyValues = new Dictionary<string, float>
        {
            { "fresh", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterFreshSatiety },
            { "salt", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterSaltSatiety },
            { "muddy", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterMuddySatiety },
            { "tainted", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterTaintedSatiety },
            { "poisoned", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterPoisonedSatiety },
            { "muddysalt", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterMuddySaltSatiety },
            { "taintedsalt", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterTaintedSaltSatiety },
            { "poisonedsalt", HydrateOrDiedrateModSystem.LoadedConfig.WellWaterPoisonedSaltSatiety }
        };
        foreach (var kvp in wellWaterSatietyValues)
        {
            ApplyWellWaterSatietyPatch(api, "hydrateordiedrate:itemtypes/liquid/wellwaterportion.json", kvp.Key, kvp.Value);
        }
    }

    private void ApplyWellWaterSatietyPatch(ICoreAPI api, string jsonFilePath, string waterType, float satietyValue)
    {
        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        JsonPatch ensureNutritionProps = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/attributesByType/*-{waterType}/waterTightContainerProps/nutritionPropsPerLitre",
            Value = new JsonObject(JToken.FromObject(new { })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchSatiety = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/attributesByType/*-{waterType}/waterTightContainerProps/nutritionPropsPerLitre/satiety",
            Value = new JsonObject(JToken.FromObject(satietyValue)),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        JsonPatch patchFoodCategory = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/attributesByType/*-{waterType}/waterTightContainerProps/nutritionPropsPerLitre/foodcategory",
            Value = new JsonObject(JToken.FromObject("NoNutrition")),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwaterensure-{waterType}"), ensureNutritionProps, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwatersatiety-{waterType}"), patchSatiety, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwaterfoodcat-{waterType}"), patchFoodCategory, ref applied, ref notFound, ref errorCount);
    }
    public void PrepareWaterPerishPatches(ICoreAPI api)
{
    if (!HydrateOrDiedrateModSystem.LoadedConfig.WaterPerish)
    {
        // When water perish is disabled, remove perish properties from the water JSON files.
        RemoveWaterPerishPatches(api);
        return;
    }

    // Otherwise, apply the perish patches as before.
    float rainWaterFreshFreshHours = HydrateOrDiedrateModSystem.LoadedConfig.RainWaterFreshHours;
    float rainWaterFreshTransitionHours = HydrateOrDiedrateModSystem.LoadedConfig.RainWaterTransitionHours;
    float boiledWaterFreshFreshHours = HydrateOrDiedrateModSystem.LoadedConfig.BoiledWaterFreshHours;
    float boiledWaterFreshTransitionHours = HydrateOrDiedrateModSystem.LoadedConfig.BoiledWaterFreshHours;
    float boiledRainWaterFreshFreshHours = HydrateOrDiedrateModSystem.LoadedConfig.BoiledRainWaterFreshHours;
    float boiledRainWaterFreshTransitionHours = HydrateOrDiedrateModSystem.LoadedConfig.BoiledRainWaterTransitionHours;
    float distilledWaterFreshFreshHours = HydrateOrDiedrateModSystem.LoadedConfig.DistilledWaterFreshHours;
    float distilledWaterFreshTransitionHours = HydrateOrDiedrateModSystem.LoadedConfig.DistilledWaterTransitionHours;
    
    ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/rainwaterportion.json", rainWaterFreshFreshHours, rainWaterFreshTransitionHours);
    ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/distilledwaterportion.json", distilledWaterFreshFreshHours, distilledWaterFreshTransitionHours);
    ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledwaterportion.json", boiledWaterFreshFreshHours, boiledWaterFreshTransitionHours);
    ApplyPerishPatch(api, "hydrateordiedrate:itemtypes/liquid/boiledrainwaterportion.json", boiledRainWaterFreshFreshHours, boiledRainWaterFreshTransitionHours);
}

    private void RemoveWaterPerishPatches(ICoreAPI api)
    {
        string[] jsonFiles = new string[]
        {
            "hydrateordiedrate:itemtypes/liquid/rainwaterportion.json",
            "hydrateordiedrate:itemtypes/liquid/distilledwaterportion.json",
            "hydrateordiedrate:itemtypes/liquid/boiledwaterportion.json",
            "hydrateordiedrate:itemtypes/liquid/boiledrainwaterportion.json"
        };

        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        foreach (var jsonFilePath in jsonFiles)
        {
            int applied = 0, notFound = 0, errorCount = 0;
            JsonPatch removePatch = new JsonPatch
            {
                Op = EnumJsonPatchOp.Remove,
                Path = "/transitionableProps",
                File = new AssetLocation(jsonFilePath),
                Side = EnumAppSide.Server
            };
            patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:removeperishpatch"), removePatch,
                ref applied, ref notFound, ref errorCount);
        }
    }

    public void PrepareWellWaterPerishPatches(ICoreAPI api)
    {
        if (!HydrateOrDiedrateModSystem.LoadedConfig.WaterPerish)
        {
            RemoveWellWaterPerishPatches(api);
            return;
        }

        var wellWaterPerishRateValues = new Dictionary<string, (float fresh, float transition)>
        {
            {
                "fresh",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterFreshFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterFreshTransitionHours)
            },
            {
                "salt",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterSaltFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterSaltTransitionHours)
            },
            {
                "muddy",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterMuddyFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterMuddyTransitionHours)
            },
            {
                "tainted",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterTaintedFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterTaintedTransitionHours)
            },
            {
                "poisoned",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterPoisonedFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterPoisonedTransitionHours)
            },
            {
                "muddysalt",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterMuddySaltFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterMuddySaltTransitionHours)
            },
            {
                "taintedsalt",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterTaintedSaltFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterTaintedSaltTransitionHours)
            },
            {
                "poisonedsalt",
                (HydrateOrDiedrateModSystem.LoadedConfig.WellWaterPoisonedSaltFreshHours,
                    HydrateOrDiedrateModSystem.LoadedConfig.WellWaterPoisonedSaltTransitionHours)
            }
        };
        foreach (var kvp in wellWaterPerishRateValues)
        {
            ApplyWellWaterPerishRatePatch(api, "hydrateordiedrate:itemtypes/liquid/wellwaterportion.json", kvp.Key,
                kvp.Value.fresh, kvp.Value.transition);
        }
    }

    private void RemoveWellWaterPerishPatches(ICoreAPI api)
    {
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        int applied = 0, notFound = 0, errorCount = 0;
        JsonPatch removePatch = new JsonPatch
        {
            Op = EnumJsonPatchOp.Remove,
            Path = "/transitionablePropsByType",
            File = new AssetLocation("hydrateordiedrate:itemtypes/liquid/wellwaterportion.json"),
            Side = EnumAppSide.Server
        };

        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:removewellwaterperishpatch"), removePatch,
            ref applied, ref notFound, ref errorCount);
    }

    private void ApplyPerishPatch(ICoreAPI api, string jsonFilePath, float freshHours, float transitionHours)
    {
        JsonPatch patchFreshHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/transitionableProps/0/freshHours",
            Value = new JsonObject(JToken.FromObject(new { avg = freshHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
    
        JsonPatch patchTransitionHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = "/transitionableProps/0/transitionHours",
            Value = new JsonObject(JToken.FromObject(new { avg = transitionHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };

        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamicfreshhourspatch"), patchFreshHours, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation("hydrateordiedrate:dynamictransitionhourspatch"), patchTransitionHours, ref applied, ref notFound, ref errorCount);
    }

    private void ApplyWellWaterPerishRatePatch(ICoreAPI api, string jsonFilePath, string waterType, float freshHours,
        float transitionHours)
    {
        int applied = 0, notFound = 0, errorCount = 0;
        ModJsonPatchLoader patchLoader = api.ModLoader.GetModSystem<ModJsonPatchLoader>();
        JsonPatch patchFreshHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/transitionablePropsByType/*-{waterType}/0/freshHours",
            Value = new JsonObject(JToken.FromObject(new { avg = freshHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        JsonPatch patchTransitionHours = new JsonPatch
        {
            Op = EnumJsonPatchOp.AddMerge,
            Path = $"/transitionablePropsByType/*-{waterType}/0/transitionHours",
            Value = new JsonObject(JToken.FromObject(new { avg = transitionHours })),
            File = new AssetLocation(jsonFilePath),
            Side = EnumAppSide.Server
        };
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwaterfreshperishrate-{waterType}"),
            patchFreshHours, ref applied, ref notFound, ref errorCount);
        patchLoader.ApplyPatch(0, new AssetLocation($"hydrateordiedrate:wellwatertransitionperishrate-{waterType}"),
            patchTransitionHours, ref applied, ref notFound, ref errorCount);
    }
}