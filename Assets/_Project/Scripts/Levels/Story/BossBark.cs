using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One line the hero calls out when a boss shows one of its signature moments.</summary>
    [Serializable]
    public struct BossBark
    {
        public BossMoment Moment;

        [TextArea(1, 2)] public string Line;
    }
}
