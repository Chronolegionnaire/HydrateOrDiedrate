using System;

namespace HydrateOrDiedrate.Thirst;

/// <summary>
/// Interface for EntityBehaviors that allowes influencing the thirst rate of an entity.
/// </summary>
[Obsolete("System has been reworked to work more stat centered")]
public interface IThirstRateModifier
{
    public float OnThirstRateCalculate(float currentModifier);
}
