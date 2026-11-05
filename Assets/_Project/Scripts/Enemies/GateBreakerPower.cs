using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The numbers of a boss that freezes the gates and breaks through them (the Frost Warden, Docs/BOSSES.md "Gate
    /// Breaker"): the ice block, the warning, the rage that grows with each broken gate, and its ice armour.
    /// </summary>
    [Serializable]
    public sealed class GateBreakerPower
    {
        [Tooltip("Seconds a gate stays an ice block before the boss breaks through it.")]
        [SerializeField] [Min(1f)] private float _freezeSeconds = 8f;
        [Tooltip("How far ahead along the road a gate gets its blue haze and crack sound (world units).")]
        [SerializeField] [Min(1f)] private float _warnDistance = 7f;
        [Tooltip("Seconds before it may freeze a gate the hero thawed again.")]
        [SerializeField] [Min(0f)] private float _refreezeSeconds = 12f;

        [Header("Rage")]
        [Tooltip("Speed factor it gains per broken gate.")]
        [SerializeField] [Min(0f)] private float _ragePerGate = 0.15f;
        [SerializeField] [Min(1)] private int _maxRage = 4;
        [Tooltip("Bones that grow with its rage (README round 28: Icicle_1..5).")]
        [SerializeField] private string[] _rageBones = { "Icicle_1", "Icicle_2", "Icicle_3", "Icicle_4", "Icicle_5" };
        [Tooltip("Scale of the rage bones calm (x) and at full rage (y).")]
        [SerializeField] private Vector2 _rageScale = new Vector2(0.2f, 1f);

        [Header("Ice armour")]
        [Tooltip("The model's armour object, shown while it is armoured.")]
        [SerializeField] private string _armourPart = string.Empty;
        [Tooltip("Bones that carry the armour plates: held at full size while it is armoured (the older clips key them at 0).")]
        [SerializeField] private string[] _armourBones = { "Armour_1", "Armour_2", "Armour_3", "Armour_4", "Armour_5", "Armour_6", "Armour_7", "Armour_8" };
        [Tooltip("Share of each hit that lands while the armour holds: resistance, never immunity.")]
        [SerializeField] [Range(0.2f, 1f)] private float _armourSoak = 0.5f;
        [Tooltip("Damage factor while fire has cracked the armour.")]
        [SerializeField] [Min(1f)] private float _crackedFactor = 2f;
        [Tooltip("Seconds the cracked armour lets the damage through.")]
        [SerializeField] [Min(0.5f)] private float _crackSeconds = 5f;
        [Tooltip("The phase in which the armour is gone for good and it runs (Exposed, below 40%).")]
        [SerializeField] [Min(0)] private int _exposedPhase = 1;
        [Tooltip("Speed factor once exposed.")]
        [SerializeField] [Min(1f)] private float _exposedPace = 1.35f;

        [Header("Balance ledger")]
        [Tooltip("Gates it is expected to freeze in one fight.")]
        [SerializeField] [Min(0)] private int _expectedGates = 3;

        public float FreezeSeconds => _freezeSeconds;
        public float WarnDistance => _warnDistance;
        public float RefreezeSeconds => _refreezeSeconds;
        public float RagePerGate => _ragePerGate;
        public int MaxRage => _maxRage;
        public string[] RageBones => _rageBones;
        public Vector2 RageScale => _rageScale;
        public string ArmourPart => _armourPart;
        public string[] ArmourBones => _armourBones;
        public float ArmourSoak => _armourSoak;
        public float CrackedFactor => _crackedFactor;
        public float CrackSeconds => _crackSeconds;
        public int ExposedPhase => _exposedPhase;
        public float ExposedPace => _exposedPace;
        public int ExpectedGates => _expectedGates;
    }
}
