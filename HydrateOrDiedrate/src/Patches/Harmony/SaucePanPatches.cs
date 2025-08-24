using ACulinaryArtillery;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Patches.Harmony;

[HarmonyPatch]
[HarmonyPatchCategory("aculinaryartillery")]
public static class SaucePanPatches
{

    private static bool TryGetWaterContent(IEnumerable<ItemSlot> slots, out ItemStack result)
    {
        result = null;
        foreach(ItemSlot slot in slots)
        {
            if(slot.Empty) continue;
            if(result is not null) return false;
            result = slot.Itemstack;
        }
        if(result is null) return false;

        var code = result.Collectible.Code;
        if ((code.Domain != "game" || code.Path != "waterportion") && (code.Domain != "hydrateordiedrate" || code.Path != "rainwaterportion")) return false;

        return true;
    }

    [HarmonyPatch("ACulinaryArtillery.BlockSaucepan", "GetMeltingDuration")]
    [HarmonyPrefix]
    public static bool PatchMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ref float __result)
    {
        if(!TryGetWaterContent(cookingSlotsProvider.Slots, out var contentStack)) return true;

        List<ItemStack> simulated = [contentStack, contentStack.Clone()];

        foreach (var recipe in world.Api.GetSimmerRecipes())
        {
            var matchAmount = recipe.Match(simulated);
            if (matchAmount <= 0 || recipe.Simmering is null) continue;

            float meltDuration = recipe.Simmering.MeltingDuration;
            float speed = 10f;
            __result = meltDuration * matchAmount / speed;
            return false;
        }

        return true;
    }

    [HarmonyPatch("ACulinaryArtillery.BlockSaucepan", "GetOutputText")]
    [HarmonyPrefix]
    public static bool PatchOutputText(IWorldAccessor world, InventorySmelting inv, ref string __result)
    {
        if(!TryGetWaterContent([inv[3], inv[4], inv[5], inv[6]], out var contentStack)) return true;

        List<ItemStack> simulated = [contentStack, contentStack.Clone()];

        foreach (var recipe in world.Api.GetSimmerRecipes())
        {
            var matchAmount = recipe.Match(simulated);
            if (matchAmount <= 0) continue;
            var stack = recipe.Simmering?.SmeltedStack?.ResolvedItemstack;
            if(stack is null) continue;

            __result = Lang.Get("firepit-gui-willcreate", matchAmount, stack.GetName());
            return false;
        }

        return true;
    }

    [HarmonyPatch("ACulinaryArtillery.BlockSaucepan", "GetMeltingPoint")]
    [HarmonyPrefix]
    public static bool PatchMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ref float __result)
    {
        if(!TryGetWaterContent(cookingSlotsProvider.Slots, out var contentStack)) return true;

        List<ItemStack> simulated = [contentStack, contentStack.Clone()];

        foreach (var recipe in world.Api.GetSimmerRecipes())
        {
            var matchAmount = recipe.Match(simulated);
            if (matchAmount <= 0 || recipe.Simmering is null) continue;

            __result = recipe.Simmering.MeltingPoint;
            return false;
        }

        return true;
    }

    [HarmonyPatch("ACulinaryArtillery.BlockSaucepan", "CanSmelt")]
    [HarmonyPostfix]
    public static void PatchCanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ref bool __result)
    {
        if(!TryGetWaterContent(cookingSlotsProvider.Slots, out var contentStack)) return;

        List<ItemStack> simulated = [contentStack, contentStack.Clone()];

        foreach (var recipe in world.Api.GetSimmerRecipes())
        {
            var matchAmount = recipe.Match(simulated);
            if (matchAmount <= 0) continue;

            __result = true;
            return;
        }
    }

    [HarmonyPatch("ACulinaryArtillery.BlockSaucepan", "DoSmelt")]
    [HarmonyPrefix]
    public static void PatchDoSmelt(ISlotProvider cookingSlotsProvider)
    {
        if(!TryGetWaterContent(cookingSlotsProvider.Slots, out var contentStack)) return;

        foreach(var slot in cookingSlotsProvider.Slots)
        {
            if(!slot.Empty) continue;
            slot.Itemstack = contentStack.Clone();
            return;
        }

        //TODO there used to be a patch here that increased the amount of slots if there where no empty slots but these are never removed and could cause issues so I left it out for now.
    }
}