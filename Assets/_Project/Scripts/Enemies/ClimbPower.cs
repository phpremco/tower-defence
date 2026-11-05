using System;
using Game.Core;
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
        [Tooltip("What it does at a living gate on its road: climb onto it, ram it with its own clip, or bite it like any creature.")]
        [SerializeField] private GateBehaviour _gateBehaviour = GateBehaviour.Climb;
        [Tooltip("Playback rate of the climb clips: the authored climbs take 6 to 11 seconds, too slow for play.")]
        [SerializeField] [Min(0.5f)] private float _clipRate = 3f;
        [Tooltip("Playback rate of its own gate mount and dismount (Gate_Mount, Gate_Dismount). The straddle loop always plays at 1, so its blows land on the authored weight frames.")]
        [SerializeField] [Min(0.5f)] private float _gateCrossRate = 1.5f;
        [Tooltip("Batter: the clip it rams a gate with; its moment is the blow.")]
        [SerializeField] private ModelClipRole _ramClip = ModelClipRole.Roar;
        [Tooltip("Batter: playback rate of the ram clip.")]
        [SerializeField] [Min(0.25f)] private float _ramRate = 1.2f;
        [Tooltip("Batter: world units between its body and the gate when it stops to ram.")]
        [SerializeField] [Min(0f)] private float _ramClearance = 0.5f;
        [Tooltip("A step up lower than this (world units) is walked with a tilt instead of climbed.")]
        [SerializeField] [Min(0.1f)] private float _minLedge = 0.75f;
        [Tooltip("Damage per second its weight does to a gate it stands on, on top of its own blows.")]
        [SerializeField] [Min(0f)] private float _gateWeightDamage = 18f;
        [Tooltip("Most the body tilts to follow the surface, in degrees.")]
        [SerializeField] [Range(0f, 45f)] private float _maxTilt = 22f;

        public bool ClimbsRocks => _climbsRocks;
        public GateBehaviour GateBehaviour => _gateBehaviour;
        public bool ClimbsGates => _gateBehaviour == GateBehaviour.Climb;
        public bool BattersGates => _gateBehaviour == GateBehaviour.Batter;
        public float GateCrossRate => _gateCrossRate;
        public ModelClipRole RamClip => _ramClip;
        public float RamRate => _ramRate;
        public float RamClearance => _ramClearance;
        public float ClipRate => _clipRate;
        public float MinLedge => _minLedge;
        public float GateWeightDamage => _gateWeightDamage;
        public float MaxTilt => _maxTilt;
    }
}
