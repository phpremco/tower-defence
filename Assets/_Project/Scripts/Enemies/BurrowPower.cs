using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The numbers of a burrowing boss (Gravemaw, Docs/BOSSES.md): when it dives, how fast its crack line runs, how long
    /// the target is warned, what the throw costs the tower, what a mine on the line does, and its rage.
    /// </summary>
    [Serializable]
    public sealed class BurrowPower
    {
        [Tooltip("Health shares at which it dives under, highest first.")]
        [SerializeField] private float[] _thresholds = { 0.75f, 0.5f, 0.25f };
        [Tooltip("World units per second under the ground, along the crack line.")]
        [SerializeField] [Min(0.2f)] private float _travelSpeed = 2.6f;
        [Tooltip("Speed factor through quicksand: its shortcut.")]
        [SerializeField] [Min(1f)] private float _quicksandFactor = 2f;
        [Tooltip("Seconds the target tower is ringed red before the boss breaks out beside it.")]
        [SerializeField] [Min(0.5f)] private float _telegraphSeconds = 3f;
        [Tooltip("Share of its full health a thrown tower loses (it is never wrecked by the throw).")]
        [SerializeField] [Range(0f, 0.9f)] private float _throwHealthShare = 0.4f;
        [Tooltip("Seconds a thrown tower lies tilted after it lands, before it rights itself.")]
        [SerializeField] [Min(0f)] private float _upsetSeconds = 4f;
        [Tooltip("Seconds the tower is in the air before it lands.")]
        [SerializeField] [Min(0.1f)] private float _airSeconds = 1.1f;
        [Tooltip("Seconds the boss stands dazed on the surface when a mine went off on it while it burrowed.")]
        [SerializeField] [Min(0f)] private float _stunSeconds = 4f;
        [Tooltip("Speed factor once enraged (below the last threshold).")]
        [SerializeField] [Min(1f)] private float _enragedSpeed = 1.35f;
        [Tooltip("What climbs out of an enraged crack line.")]
        [SerializeField] private EnemyDefinition _crackMinion;
        [SerializeField] [Min(0)] private int _minionsPerCrack = 4;
        [Tooltip("After its entrance it burrows back to this share of the main road from its start, so the fight runs the road.")]
        [SerializeField] [Range(0f, 0.5f)] private float _returnToRoadShare = 0.08f;
        [Tooltip("Speed factor of that trip back to the road under the ground.")]
        [SerializeField] [Min(1f)] private float _returnSpeedFactor = 2.5f;
        [Tooltip("Expected seconds under the ground per burrow, for the balance ledger.")]
        [SerializeField] [Min(0f)] private float _expectedHiddenSeconds = 6f;

        public float[] Thresholds => _thresholds;
        public float TravelSpeed => _travelSpeed;
        public float QuicksandFactor => _quicksandFactor;
        public float TelegraphSeconds => _telegraphSeconds;
        public float ThrowHealthShare => _throwHealthShare;
        public float UpsetSeconds => _upsetSeconds;
        public float AirSeconds => _airSeconds;
        public float StunSeconds => _stunSeconds;
        public float EnragedSpeed => _enragedSpeed;
        public EnemyDefinition CrackMinion => _crackMinion;
        public int MinionsPerCrack => _minionsPerCrack;
        public float ExpectedHiddenSeconds => _expectedHiddenSeconds;
        public float ReturnToRoadShare => _returnToRoadShare;
        public float ReturnSpeedFactor => _returnSpeedFactor;

        /// <summary>A thrown tower is silent from the throw until it stands again.</summary>
        public float ThrowSilenceSeconds => _airSeconds + _upsetSeconds;
    }
}
