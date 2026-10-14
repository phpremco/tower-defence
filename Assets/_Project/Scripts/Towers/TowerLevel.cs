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
    }
}
