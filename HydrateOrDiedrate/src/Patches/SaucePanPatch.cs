using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches
{
    [HarmonyPatch]
    public static class BlockSaucepan_GetMeltingDuration_Patch
    {
        static MethodBase TargetMethod()
        {
            Type targetType = AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan");
            return targetType != null ? AccessTools.Method(targetType, "GetMeltingDuration") : null;
        }
        static bool Prepare() => AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan") != null;

        [HarmonyPrefix]
        public static bool Prefix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, object __instance, ref float __result)
        {
            List<ItemStack> contents = new List<ItemStack>();
            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty) contents.Add(slot.Itemstack);
            }
            if (contents.Count == 1)
            {
                string code = contents[0].Collectible.Code.ToString();
                if (code == "game:waterportion" || code == "hydrateordiedrate:rainwaterportion")
                {
                    List<ItemStack> simulated = new List<ItemStack> { contents[0], contents[0].Clone() };
                    FieldInfo apiField = AccessTools.Field(__instance.GetType().BaseType, "api");
                    if (apiField == null) return true;
                    ICoreAPI coreApi = apiField.GetValue(__instance) as ICoreAPI;
                    if (coreApi == null) return true;

                    Type apiAdditionsType = AccessTools.TypeByName("Vintagestory.GameContent.ApiAdditions");
                    MethodInfo getSimmerRecipes = apiAdditionsType.GetMethod(
                        "GetSimmerRecipes",
                        BindingFlags.Static | BindingFlags.Public);
                    if (getSimmerRecipes == null)
                    {
                        return true;
                    }
                    object recipesObj = getSimmerRecipes.Invoke(null, new object[] { coreApi });
                    if (recipesObj == null) return true;
                    foreach (object recipe in (IEnumerable<object>)recipesObj)
                    {
                        MethodInfo matchMethod = recipe.GetType().GetMethod("Match");
                        if (matchMethod == null) continue;
                        int matchAmount = (int)matchMethod.Invoke(recipe, new object[] { simulated });
                        if (matchAmount > 0)
                        {
                            FieldInfo simmeringField = recipe.GetType().GetField("Simmering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (simmeringField == null) continue;
                            object simmering = simmeringField.GetValue(recipe);
                            if (simmering == null) continue;
                            FieldInfo meltingDurationField = simmering.GetType().GetField("MeltingDuration", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (meltingDurationField == null) continue;
                            float meltDuration = (float)Convert.ChangeType(meltingDurationField.GetValue(simmering), typeof(float));
                            float speed = 10f;
                            __result = meltDuration * matchAmount / speed;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
    [HarmonyPatch]
    public static class BlockSaucepan_GetOutputText_Patch
    {
        static MethodBase TargetMethod()
        {
            Type targetType = AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan");
            return targetType != null ? AccessTools.Method(targetType, "GetOutputText") : null;
        }

        static bool Prepare() => AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan") != null;

        [HarmonyPrefix]
        public static bool Prefix(IWorldAccessor world, InventorySmelting inv, object __instance, ref string __result)
        {
            List<ItemStack> contents = new List<ItemStack>();
            foreach (ItemSlot slot in new ItemSlot[] { inv[3], inv[4], inv[5], inv[6] })
            {
                if (!slot.Empty)
                    contents.Add(slot.Itemstack);
            }

            if (contents.Count == 1)
            {
                string code = contents[0].Collectible.Code.ToString();
                if (code == "game:waterportion" || code == "hydrateordiedrate:rainwaterportion")
                {
                    List<ItemStack> simulated = new List<ItemStack> { contents[0], contents[0].Clone() };
                    FieldInfo apiField = AccessTools.Field(__instance.GetType().BaseType, "api");
                    if (apiField == null) return true;
                    ICoreAPI coreApi = apiField.GetValue(__instance) as ICoreAPI;
                    if (coreApi == null) return true;

                    Type apiAdditionsType = AccessTools.TypeByName("Vintagestory.GameContent.ApiAdditions");
                    if (apiAdditionsType == null)
                    {
                        return true;
                    }
                    MethodInfo getSimmerRecipes = apiAdditionsType.GetMethod("GetSimmerRecipes", BindingFlags.Static | BindingFlags.Public);
                    if (getSimmerRecipes == null)
                    {
                        return true;
                    }
                    object recipesObj = getSimmerRecipes.Invoke(null, new object[] { coreApi });

                    object match = null;
                    int amount = 0;
                    foreach (object recipe in (IEnumerable<object>)recipesObj)
                    {
                        MethodInfo matchMethod = recipe.GetType().GetMethod("Match");
                        if (matchMethod == null) continue;
                        int m = (int)matchMethod.Invoke(recipe, new object[] { simulated });
                        if (m > 0)
                        {
                            match = recipe;
                            amount = m;
                            break;
                        }
                    }
                    if (match == null)
                        return true;

                    FieldInfo simmeringField = match.GetType().GetField("Simmering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (simmeringField == null) return true;
                    object simmering = simmeringField.GetValue(match);
                    if (simmering == null) return true;

                    FieldInfo smeltedStackField = simmering.GetType().GetField("SmeltedStack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (smeltedStackField == null) return true;
                    object smeltedStack = smeltedStackField.GetValue(simmering);
                    if (smeltedStack == null) return true;

                    FieldInfo resolvedItemstackField = smeltedStack.GetType().GetField("ResolvedItemstack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (resolvedItemstackField == null) return true;
                    object resolvedItemstack = resolvedItemstackField.GetValue(smeltedStack);
                    if (resolvedItemstack == null) return true;

                    __result = Lang.Get("firepit-gui-willcreate", amount, ((ItemStack)resolvedItemstack).GetName());
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch]
    public static class BlockSaucepan_GetMeltingPoint_Patch
    {
        static MethodBase TargetMethod()
        {
            Type targetType = AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan");
            return targetType != null ? AccessTools.Method(targetType, "GetMeltingPoint") : null;
        }
        static bool Prepare() => AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan") != null;

        [HarmonyPrefix]
        public static bool Prefix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, object __instance, ref float __result)
        {
            List<ItemStack> contents = new List<ItemStack>();
            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty)
                    contents.Add(slot.Itemstack);
            }
            if (contents.Count == 1)
            {
                string code = contents[0].Collectible.Code.ToString();
                if (code == "game:waterportion" || code == "hydrateordiedrate:rainwaterportion")
                {
                    List<ItemStack> simulated = new List<ItemStack> { contents[0], contents[0].Clone() };

                    FieldInfo apiField = AccessTools.Field(__instance.GetType().BaseType, "api");
                    if (apiField == null) return true;
                    ICoreAPI coreApi = apiField.GetValue(__instance) as ICoreAPI;
                    if (coreApi == null) return true;
                    Type apiAdditionsType = AccessTools.TypeByName("Vintagestory.GameContent.ApiAdditions");
                    if (apiAdditionsType == null) return true;
                    MethodInfo getSimmerRecipes = apiAdditionsType.GetMethod(
                        "GetSimmerRecipes",
                        BindingFlags.Static | BindingFlags.Public);
                    if (getSimmerRecipes == null) return true;
                    object recipesObj = getSimmerRecipes.Invoke(null, new object[] { coreApi });
                    if (recipesObj == null) return true;

                    object match = null;
                    foreach (object recipe in (IEnumerable<object>)recipesObj)
                    {
                        MethodInfo matchMethod = recipe.GetType().GetMethod("Match", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (matchMethod == null) continue;
                        int m = (int)matchMethod.Invoke(recipe, new object[] { simulated });
                        if (m > 0)
                        {
                            match = recipe;
                            break;
                        }
                    }
                    if (match == null) return true;
                    FieldInfo simmeringField = match.GetType().GetField("Simmering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (simmeringField == null) return true;
                    object simmering = simmeringField.GetValue(match);
                    if (simmering == null) return true;
                    FieldInfo meltingPointField = simmering.GetType().GetField("MeltingPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (meltingPointField == null) return true;
                    __result = (float)Convert.ChangeType(meltingPointField.GetValue(simmering), typeof(float));
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch]
    public static class BlockSaucepan_CanSmelt_Patch
    {
        static MethodBase TargetMethod()
        {
            Type targetType = AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan");
            return targetType != null ? AccessTools.Method(targetType, "CanSmelt") : null;
        }

        static bool Prepare() => AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan") != null;

        static void Postfix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack, ref bool __result, object __instance)
        {
            if (__result) return;
            List<ItemStack> stacks = new List<ItemStack>();
            foreach (var slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty)
                    stacks.Add(slot.Itemstack.Clone());
            }
            if (stacks.Count == 1)
            {
                string code = stacks[0].Collectible.Code.ToString();
                if (code == "game:waterportion" || code == "hydrateordiedrate:rainwaterportion")
                {
                    List<ItemStack> simulated = new List<ItemStack> { stacks[0], stacks[0].Clone() };

                    FieldInfo apiField = AccessTools.Field(__instance.GetType().BaseType, "api");
                    if (apiField == null) return;
                    ICoreAPI coreApi = apiField.GetValue(__instance) as ICoreAPI;
                    if (coreApi == null) return;
                    Type apiAdditionsType = AccessTools.TypeByName("Vintagestory.GameContent.ApiAdditions");
                    if (apiAdditionsType == null) return;
                    MethodInfo getSimmerRecipes = apiAdditionsType.GetMethod(
                        "GetSimmerRecipes",
                        BindingFlags.Static | BindingFlags.Public);
                    if (getSimmerRecipes == null) return;
                    object recipesObj = getSimmerRecipes.Invoke(null, new object[] { coreApi });
                    if (recipesObj == null) return;

                    foreach (object recipe in (IEnumerable<object>)recipesObj)
                    {
                        MethodInfo matchMethod = recipe.GetType().GetMethod("Match", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (matchMethod == null) continue;
                        int matchAmount = (int)matchMethod.Invoke(recipe, new object[] { simulated });
                        if (matchAmount > 0)
                        {
                            __result = true;
                            return;
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch]
    public static class BlockSaucepan_DoSmelt_Patch
    {
        static MethodBase TargetMethod()
        {
            Type targetType = AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan");
            return targetType != null ? AccessTools.Method(targetType, "DoSmelt") : null;
        }

        static bool Prepare() => AccessTools.TypeByName("ACulinaryArtillery.BlockSaucepan") != null;

        [HarmonyPrefix]
        public static bool Prefix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot, object __instance)
        {
            List<ItemSlot> nonEmptySlots = new List<ItemSlot>();
            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty)
                    nonEmptySlots.Add(slot);
            }
            if (nonEmptySlots.Count == 1)
            {
                string code = nonEmptySlots[0].Itemstack.Collectible.Code.ToString();
                if (code == "game:waterportion" || code == "hydrateordiedrate:rainwaterportion")
                {
                    bool duplicated = false;
                    for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                    {
                        if (cookingSlotsProvider.Slots[i].Empty)
                        {
                            cookingSlotsProvider.Slots[i].Itemstack = nonEmptySlots[0].Itemstack.Clone();
                            duplicated = true;
                            break;
                        }
                    }
                    if (!duplicated && cookingSlotsProvider.Slots.Length == 1)
                    {
                        ItemSlot originalSlot = cookingSlotsProvider.Slots[0];
                        ItemSlot duplicateSlot = (ItemSlot)Activator.CreateInstance(
                            originalSlot.GetType(),
                            originalSlot.Inventory,
                            0,
                            null);
                        duplicateSlot.Itemstack = originalSlot.Itemstack.Clone();
                        ItemSlot[] newSlots = new ItemSlot[2] { originalSlot, duplicateSlot };
                        FieldInfo slotsField = AccessTools.Field(cookingSlotsProvider.GetType(), "slots");
                        if (slotsField != null)
                        {
                            slotsField.SetValue(cookingSlotsProvider, newSlots);
                        }
                    }
                }
            }
            return true;
        }
    }
}
