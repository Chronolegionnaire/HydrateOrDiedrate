using HydrateOrDiedrate.Configuration;
using HydrateOrDiedrate.EntityBehavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace HydrateOrDiedrate
{
    public class WaterInteractionHandler
    {
        private readonly ICoreAPI _api;
        private readonly Config _config;

        public WaterInteractionHandler(ICoreAPI api, Config config)
        {
            _api = api;
            _config = config;
        }

        public void HandleWaterInteraction(IPlayer byPlayer, Block block)
        {
            string liquidCode = block.LiquidCode;
            if (liquidCode.StartsWith("boilingwater"))
            {
                _api.Logger.Debug("Boiling water detected.");
                if (_config.EnableBoilingWaterDamage)
                {
                    ApplyHeatDamage(byPlayer);
                }
            }
            else if (liquidCode.StartsWith("saltwater"))
            {
                _api.Logger.Debug("Salt water detected.");
                if (_config.EnableSaltWaterThirstIncrease)
                {
                    IncreaseThirst(byPlayer);
                }
                else
                {
                    QuenchThirst(byPlayer);
                }
            }
            else if (liquidCode.StartsWith("water"))
            {
                _api.Logger.Debug("Regular water detected.");
                QuenchThirst(byPlayer);
            }
        }

        private void QuenchThirst(IPlayer player)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.CurrentThirst += _config.RegularWaterThirstDecrease;
                _api.Logger.Debug("Thirst quenched by {0}. Current thirst: {1}", _config.RegularWaterThirstDecrease, thirstBehavior.CurrentThirst);
            }
        }

        private void IncreaseThirst(IPlayer player)
        {
            var thirstBehavior = player.Entity.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior != null)
            {
                thirstBehavior.CurrentThirst -= _config.SaltWaterThirstIncrease;
                _api.Logger.Debug("Thirst increased by {0}. Current thirst: {1}", _config.SaltWaterThirstIncrease, thirstBehavior.CurrentThirst);
            }
        }

        private void ApplyHeatDamage(IPlayer player)
        {
            player.Entity.ReceiveDamage(new DamageSource()
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.Heat
            }, _config.BoilingWaterDamage);
            _api.Logger.Debug("Heat damage applied: {0}", _config.BoilingWaterDamage);
        }
    }
}
