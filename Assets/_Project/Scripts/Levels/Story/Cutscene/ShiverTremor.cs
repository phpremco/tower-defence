using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Round 63 (Remco: "the stones move like an extreme earthquake, but there is only a faint rumble"): how the ground's
    /// shiver under the commander looks at full strength. A tremor, not a shaking table: loose stones buzz a few millimetres
    /// and now and then hop once, and the lens trembles a little with them.
    /// </summary>
    [Serializable]
    public sealed class ShiverTremor
    {
        [Tooltip("The lens tremble at full strength (1 = the camera's full rumble, a third of a degree).")]
        [SerializeField] [Range(0f, 1f)] private float _lensRumble = 0.35f;
        [Tooltip("How fast the stones buzz on the ground.")]
        [SerializeField] [Min(0f)] private float _buzzHertz = 11f;
        [Tooltip("How high the buzz lifts a stone, in metres.")]
        [SerializeField] [Min(0f)] private float _buzzHeight = 0.004f;
        [Tooltip("Seconds between two hops of one stone (each stone varies it by a quarter either way).")]
        [SerializeField] [Min(0.1f)] private float _hopEvery = 1.2f;
        [Tooltip("How long one hop is in the air.")]
        [SerializeField] [Min(0.02f)] private float _hopSeconds = 0.2f;
        [Tooltip("The highest a hop lifts a stone, in metres.")]
        [SerializeField] [Min(0f)] private float _hopHeight = 0.025f;
        [Tooltip("How far a stone rocks about a level axis while it rattles.")]
        [SerializeField] [Min(0f)] private float _rockDegrees = 4f;

        public static ShiverTremor Default { get; } = new ShiverTremor();

        public float LensRumble => _lensRumble;
        public float BuzzHertz => _buzzHertz;
        public float BuzzHeight => _buzzHeight;
        public float HopEvery => _hopEvery;
        public float HopSeconds => _hopSeconds;
        public float HopHeight => _hopHeight;
        public float RockDegrees => _rockDegrees;
    }
}
