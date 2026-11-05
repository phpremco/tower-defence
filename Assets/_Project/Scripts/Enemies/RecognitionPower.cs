using System;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The numbers of a boss that learns the weapon that hurt it most (the Sealed One, Docs/BOSSES.md "Recognition"): how
    /// much of that weapon's damage still lands, the gate it drains each phase, what ORA says, its glow per phase, and
    /// how it hauls itself onto the base.
    /// </summary>
    [Serializable]
    public sealed class RecognitionPower
    {
        [Tooltip("Share of the learned tower type's damage that still lands in the next phase: resistance, never immunity.")]
        [SerializeField] [Range(0.1f, 1f)] private float _dampenedShare = 0.5f;
        [Tooltip("Each phase drains one gate's gun until the phase ends (the gate keeps blocking).")]
        [SerializeField] private bool _drainsGates = true;
        [Tooltip("ORA's line when it has learned a weapon; {0} is the tower's name.")]
        [SerializeField] private string _adaptLine = "Adaptation detected: {0} signature dampened";
        [Tooltip("Glow look of the model per phase, by the art's emission set names (README round 28: Phase1..Phase4).")]
        [SerializeField] private string[] _phaseLooks = { "Phase1", "Phase2", "Phase3", "Phase4" };

        [Header("The map comes to it")]
        [Tooltip("Seconds the ground rumbles and cracks before the new road rises and it comes up.")]
        [SerializeField] [Min(0.5f)] private float _riftSeconds = 2.5f;
        [Tooltip("Seconds the new road takes to light up from the centre to the base.")]
        [SerializeField] [Min(0.2f)] private float _roadRiseSeconds = 2f;
        [Tooltip("Damage the base takes when it crushes it from above (x the difficulty's structure damage), then per blow.")]
        [SerializeField] [Min(0f)] private float _crushDamage = 120f;
        [Tooltip("Seconds between its blows on the base once it lies on it: the last threat, with time for an answer.")]
        [SerializeField] [Min(0.5f)] private float _baseBlowSeconds = 3f;
        [Tooltip("The loop it plays on the base while its art has no OnBaseLoop clip (README round 28 lists Idle, Roar, Adapt): it stands on the roof in it.")]
        [SerializeField] private ModelClipRole _onBaseStandIn = ModelClipRole.Idle;
        [Tooltip("Playback rate of the loop on the base (its own OnBaseLoop clip or the stand-in).")]
        [SerializeField] [Min(0.1f)] private float _onBaseLoopRate = 0.8f;

        [Header("Balance ledger")]
        [Tooltip("Share of the defence's damage the top tower type deals in a mixed loadout.")]
        [SerializeField] [Range(0f, 1f)] private float _expectedTopShare = 0.3f;
        [Tooltip("Share of the defence one gate's gun is, for the drained gate.")]
        [SerializeField] [Range(0f, 0.25f)] private float _gateGunShare = 0.08f;
        [Tooltip("Seconds of one phase, for the drained gate.")]
        [SerializeField] [Min(0f)] private float _expectedPhaseSeconds = 15f;

        public float DampenedShare => _dampenedShare;
        public bool DrainsGates => _drainsGates;
        public string AdaptLine => _adaptLine;
        public string[] PhaseLooks => _phaseLooks ?? new string[0];
        public float RiftSeconds => _riftSeconds;
        public float RoadRiseSeconds => _roadRiseSeconds;
        public float CrushDamage => _crushDamage;
        public float BaseBlowSeconds => _baseBlowSeconds;
        public ModelClipRole OnBaseStandIn => _onBaseStandIn;
        public float OnBaseLoopRate => _onBaseLoopRate;
        public float ExpectedTopShare => _expectedTopShare;
        public float GateGunShare => _gateGunShare;
        public float ExpectedPhaseSeconds => _expectedPhaseSeconds;

        /// <summary>The glow look of a phase, or empty.</summary>
        public string LookFor(int phase)
        {
            string[] looks = PhaseLooks;
            return looks.Length > 0 ? looks[Mathf.Clamp(phase, 0, looks.Length - 1)] : string.Empty;
        }
    }
}
