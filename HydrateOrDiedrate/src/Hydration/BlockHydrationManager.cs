using System;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate
{
    public static class BlockHydrationManager
    {
        //TODO figure out why this?
        public static float GetHydrationValue(CollectibleObject collectible, string type)
        {
            try
            {
                if (collectible?.Attributes?.Token[Attributes.HydrationByType] is JObject hydrationByType)
                {
                    if (hydrationByType.TryGetValue(type, out var token)) return token.ToObject<float>();
                    if (hydrationByType.TryGetValue("*", out var wildcard)) return wildcard.ToObject<float>();
                }
            }
            catch (Exception)
            {
            }

            return 0f;
        }

        public static bool IsBlockBoiling(CollectibleObject collectible) => collectible?.Attributes?.Token.Value<bool>(Attributes.IsBoiling) ?? false;

        public static int GetBlockHungerReduction(CollectibleObject collectible) => collectible?.Attributes?.Token.Value<int>(Attributes.HungerReduction) ?? 0;

        public static int GetBlockHealth(CollectibleObject collectible) => collectible?.Attributes?.Token.Value<int>(Attributes.Healing) ?? 0;
    }
}