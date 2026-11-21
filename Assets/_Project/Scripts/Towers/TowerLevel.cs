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

        [Tooltip("Support masts only: multiplier on the mast's boost at this level; zero is one.")]
        [Min(0f)] public float SupportScale;

        [Tooltip("The name of the upgrade step that leads to this level, as the tower menu shows it (\"Faster cycling\"); empty for the level a tower is built at.")]
        public string StepName;

        [Tooltip("The one thing that step improves; the tower's data is checked against it.")]
        public TowerStep Step;

        [Tooltip("Splash guns only: most creatures one blast hurts at this level (the nearest to the impact); zero keeps the tower's own cap. Read it through TowerDefinition.SplashTargetsAt.")]
        [Min(0)] public int SplashTargets;
    }
}
