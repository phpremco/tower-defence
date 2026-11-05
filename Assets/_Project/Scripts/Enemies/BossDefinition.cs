using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// What makes a continent boss more than a big creature (Docs/BOSSES.md): its phases on the HUD bar, how it enters,
    /// its signature mechanic and the numbers of it, how it uses rocks, gates and pools, and what the Codex says about it.
    /// Hung on the boss's <see cref="EnemyDefinition"/>; the boss framework reads it, the view and the HUD read it too.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss Definition", fileName = "Boss")]
    public sealed class BossDefinition : ScriptableObject
    {
        // The share of the field one boss effect may hit at most (BOSSES.md: "never more than about a quarter").
        public const float MaxFieldShare = 0.25f;

        [SerializeField] private BossPhase[] _phases = { new BossPhase("Surface", 1f) };
        [SerializeField] private BossArrival _arrival;
        [SerializeField] private BossSignature _signature;
        [SerializeField] private BurrowPower _burrow = new BurrowPower();
        [SerializeField] private ClimbPower _climb = new ClimbPower();
        [SerializeField] private DivePower _dive = new DivePower();
        [Tooltip("How hard the camera kicks on its real impacts (breaking out, a throw, a burst from a pool). Never sustained.")]
        [SerializeField] [Range(0f, 1f)] private float _impactKick = 0.35f;
        [Tooltip("Playback rate of its burrow, throw, dive and burst clips (the climbs have their own).")]
        [SerializeField] [Min(0.5f)] private float _mechanicClipRate = 1.4f;

        [Header("Codex (plain words, shown once the boss has been seen)")]
        [SerializeField] [TextArea(2, 4)] private string _signatureText = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _telegraphText = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _counterText = string.Empty;

        public BossPhase[] Phases => _phases;
        public BossArrival Arrival => _arrival;
        public BossSignature Signature => _signature;
        public BurrowPower Burrow => _burrow;
        public ClimbPower Climb => _climb;
        public DivePower Dive => _dive;
        public float ImpactKick => _impactKick;
        public float MechanicClipRate => _mechanicClipRate;

        /// <summary>What its mechanic does, for the Codex.</summary>
        public string SignatureText => _signatureText;

        /// <summary>How to see it coming, for the Codex.</summary>
        public string TelegraphText => _telegraphText;

        /// <summary>How to beat it, for the Codex.</summary>
        public string CounterText => _counterText;

        /// <summary>The phase a boss is in at a health share: the last whose start it has dropped to.</summary>
        public int PhaseAt(float healthFraction)
        {
            int phase = 0;
            for (int i = 1; i < _phases.Length; i++)
            {
                if (healthFraction <= _phases[i].StartsAt)
                {
                    phase = i;
                }
            }

            return phase;
        }

        public string PhaseName(int phase) => phase >= 0 && phase < _phases.Length ? _phases[phase].Name : string.Empty;

        /// <summary>
        /// Seconds of the whole defence's firepower its mechanic takes away over one fight, for the balance ledger: each
        /// burrow hides it from every tower, and each throw silences one tower, counted as the quarter of the field the
        /// rules let one effect reach.
        /// </summary>
        public float FirepowerLossSeconds()
        {
            if (_signature != BossSignature.Burrow)
            {
                return 0f;
            }

            int burrows = _burrow.Thresholds.Length;
            return burrows * (_burrow.ExpectedHiddenSeconds + MaxFieldShare * _burrow.ThrowSilenceSeconds);
        }

        private void OnValidate()
        {
            if (_phases.Length == 0 || !Mathf.Approximately(_phases[0].StartsAt, 1f))
            {
                Debug.LogWarning($"BossDefinition '{name}': the first phase must start at full health.", this);
            }

            for (int i = 1; i < _phases.Length; i++)
            {
                if (_phases[i].StartsAt >= _phases[i - 1].StartsAt)
                {
                    Debug.LogWarning($"BossDefinition '{name}': phase {i} must start below phase {i - 1}.", this);
                }
            }

            float[] thresholds = _burrow.Thresholds;
            for (int i = 1; i < thresholds.Length; i++)
            {
                if (thresholds[i] >= thresholds[i - 1])
                {
                    Debug.LogWarning($"BossDefinition '{name}': burrow thresholds must fall, highest first.", this);
                }
            }

            if (_signature == BossSignature.Burrow && _burrow.MinionsPerCrack > 0 && _burrow.CrackMinion == null)
            {
                Debug.LogWarning($"BossDefinition '{name}': the crack line spawns minions but names no creature.", this);
            }

            if (string.IsNullOrEmpty(_signatureText) || string.IsNullOrEmpty(_telegraphText) || string.IsNullOrEmpty(_counterText))
            {
                Debug.LogWarning($"BossDefinition '{name}': the Codex needs its signature, telegraph and counter text.", this);
            }
        }
    }
}
