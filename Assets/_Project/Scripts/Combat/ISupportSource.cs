using UnityEngine;

namespace Game.Combat
{
    /// <summary>A structure that boosts the towers around it, such as the support tower.</summary>
    public interface ISupportSource
    {
        Vector3 Position { get; }

        bool IsAlive { get; }

        /// <summary>Distance within which towers are boosted. Zero when this is not a support structure.</summary>
        float SupportRadius { get; }

        /// <summary>Extra range as a fraction, for example 0.15 for +15%.</summary>
        float RangeBonus { get; }

        /// <summary>Extra fire rate as a fraction.</summary>
        float RateBonus { get; }
    }
}
