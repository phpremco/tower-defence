using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// How a boss uses the climb layer (Docs/BOSSES.md, "Use the terrain"): whether it clambers over rocks and onto gates,
    /// how much faster than authored it plays its long climb clips, and what its weight does to a gate it stands on.
    /// </summary>
    [Serializable]
    public sealed class ClimbPower
    {
        [SerializeField] private bool _climbsRocks = true;
        [SerializeField] private bool _climbsGates = true;
        [Tooltip("Playback rate of the climb clips: the authored climbs take 6 to 11 seconds, too slow for play.")]
        [SerializeField] [Min(0.5f)] private float _clipRate = 3f;
        [Tooltip("A step up lower than this (world units) is walked with a tilt instead of climbed.")]
        [SerializeField] [Min(0.1f)] private float _minLedge = 0.75f;
        [Tooltip("Damage per second its weight does to a gate it stands on, on top of its own blows.")]
        [SerializeField] [Min(0f)] private float _gateWeightDamage = 18f;
        [Tooltip("Most the body tilts to follow the surface, in degrees.")]
        [SerializeField] [Range(0f, 45f)] private float _maxTilt = 22f;

        public bool ClimbsRocks => _climbsRocks;
        public bool ClimbsGates => _climbsGates;
        public float ClipRate => _clipRate;
        public float MinLedge => _minLedge;
        public float GateWeightDamage => _gateWeightDamage;
        public float MaxTilt => _maxTilt;
    }
}
