using System;
using Game.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Levels
{
    /// <summary>One line the hero calls out when a boss shows one of its signature moments.</summary>
    [Serializable]
    public struct BossBark
    {
        public BossMoment Moment;

        [FormerlySerializedAs("Line")] [TextArea(1, 2)] [SerializeField] private string _line;

        public BossBark(BossMoment moment, string line)
        {
            Moment = moment;
            _line = line;
        }

        /// <summary>The stored value: a localisation key once the tables are written (Docs/LOCALISATION.md).</summary>
        public string Key => _line;

        public string Line => Loc.Get(_line);
    }
}
