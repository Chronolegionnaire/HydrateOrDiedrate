using System.Text;
using HarmonyLib;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.AnimalThirst.Patches;

[HarmonyPatch(typeof(EntityBehaviorMultiply), "GetInfoText")]
public static class MultiplyInfoHydrationPatch
{
    static void Postfix(EntityBehaviorMultiply __instance, StringBuilder infotext)
    {
        var ent = __instance.entity;
        var tree = ent.WatchedAttributes.GetTreeAttribute("hunger");
        if (tree == null) return;

        float hydration = tree.GetFloat("hydration", 0f);
        double lastDrink = tree.GetDouble("lastDrinkHours", double.NegativeInfinity);
        double hoursNow = ent.World.Calendar?.TotalHours ?? 0;

        string hydText;
        if (hydration <= 0)
        {
            hydText = Lang.Get("Very thirsty");
        }
        else if (hoursNow - lastDrink > 24)
        {
            hydText = Lang.Get("Has not drunk in a long time");
        }
        else
        {
            hydText = Lang.Get("Hydration: {0}", hydration.ToString("0.0"));
        }

        infotext.AppendLine(hydText);
    }
}
