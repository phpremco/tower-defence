using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>One phase of a boss: its name on the HUD boss bar and the health share at which it begins.</summary>
    [Serializable]
    public struct BossPhase
    {
        public string Name;

        /// <summary>The phase begins once the boss's health share drops to this or below; the first phase starts at 1.</summary>
        [Range(0f, 1f)] public float StartsAt;

        public BossPhase(string name, float startsAt)
        {
            Name = name;
            StartsAt = startsAt;
        }
    }
}
