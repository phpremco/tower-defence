using System;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>Stats of one upgrade level (one to three stars). Cost is what it takes to reach this level.</summary>
    [Serializable]
    public struct TowerLevel
    {
        [Min(0)] public int Cost;
        [Min(0f)] public float Damage;
        [Min(0.5f)] public float Range;
        [Min(0.05f)] public float FireInterval;

        [Tooltip("Extra maximum health gained by reaching this level, before upgrade multipliers.")]
        [Min(0f)] public float HealthBonus;

        [Tooltip("Speed factor a hit leaves on the target at this level; zero keeps the tower's own value.")]
        [Range(0f, 1f)] public float SlowFactor;

        [Tooltip("How long that slow lasts at this level; zero keeps the tower's own value.")]
        [Min(0f)] public float SlowSeconds;

        [Tooltip("Optional look for this level. Empty keeps the model of the level before it.")]
        public GameObject Model;
    }
}
