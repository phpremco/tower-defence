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
        [SerializeField] private HuntPower _hunt = new HuntPower();
        [SerializeField] private GateBreakerPower _gateBreaker = new GateBreakerPower();
        [SerializeField] private OvergrowthPower _overgrowth = new OvergrowthPower();
        [SerializeField] private SplitPower _split = new SplitPower();
        [SerializeField] private RecognitionPower _recognition = new RecognitionPower();
        [SerializeField] private ClimbPower _climb = new ClimbPower();
        [SerializeField] private DivePower _dive = new DivePower();
        [Tooltip("Half the width of its body, legs included, in world units: its routes keep this clear of every tower.")]
        [SerializeField] [Min(0.3f)] private float _bodyHalfWidth = 1.1f;
        [Tooltip("How hard the camera kicks on its real impacts (breaking out, a throw, a burst from a pool). Never sustained.")]
        [SerializeField] [Range(0f, 1f)] private float _impactKick = 0.35f;
        [Tooltip("Playback rate of its burrow, throw, dive and burst clips (the climbs have their own).")]
        [SerializeField] [Min(0.5f)] private float _mechanicClipRate = 1.4f;
        [Tooltip("Seconds the victory panel waits after it falls, so its end plays first (the Sealed One's white Release: its death clip's length). 0 shows the panel at once.")]
        [SerializeField] [Min(0f)] private float _resultHoldSeconds;
        [Tooltip("This boss's own health factor per difficulty (by profile name), on top of the profile's boss share; a difficulty not listed takes 1.")]
        [SerializeField] private BossDifficultyHealth[] _difficultyHealth = new BossDifficultyHealth[0];

        [Header("Codex (plain words, shown once the boss has been seen)")]
        [SerializeField] [TextArea(2, 4)] private string _signatureText = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _telegraphText = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _counterText = string.Empty;

        public BossPhase[] Phases => _phases;
        public BossArrival Arrival => _arrival;
        public BossSignature Signature => _signature;
        public BurrowPower Burrow => _burrow;
        public HuntPower Hunt => _hunt;
        public GateBreakerPower GateBreaker => _gateBreaker;
        public OvergrowthPower Overgrowth => _overgrowth;
        public SplitPower Split => _split;
        public RecognitionPower Recognition => _recognition;
        public ClimbPower Climb => _climb;
        public DivePower Dive => _dive;
        public float ImpactKick => _impactKick;
        public float BodyHalfWidth => _bodyHalfWidth;
        public float MechanicClipRate => _mechanicClipRate;
        public float ResultHoldSeconds => _resultHoldSeconds;
        public BossDifficultyHealth[] DifficultyHealth => _difficultyHealth ?? new BossDifficultyHealth[0];

        /// <summary>Its own health factor on a difficulty (the profile's asset name), on top of the boss share; 1 when not listed.</summary>
        public float HealthOn(string difficulty)
        {
            if (_difficultyHealth == null)
            {
                return 1f;
            }

            foreach (BossDifficultyHealth entry in _difficultyHealth)
            {
                if (entry.Difficulty == difficulty)
                {
                    return entry.Factor;
                }
            }

            return 1f;
        }

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
            switch (_signature)
            {
                case BossSignature.Burrow:
                    int burrows = _burrow.Thresholds.Length;
                    return burrows * (_burrow.ExpectedHiddenSeconds + MaxFieldShare * _burrow.ThrowSilenceSeconds);
                case BossSignature.HuntTheCold:
                    // One smashed tower per phase, out until it is rebuilt, counted as the quarter the rules let one effect reach.
                    return _phases.Length * MaxFieldShare * _hunt.RebuildSeconds;
                case BossSignature.GateBreaker:
                    // Each frozen gate's gun is silent for the freeze, counted as the quarter of the field.
                    return _gateBreaker.ExpectedGates * MaxFieldShare * _gateBreaker.FreezeSeconds;
                case BossSignature.Overgrowth:
                    // Each volley slows up to a quarter of the field to its fire share until the vines are burned off.
                    return _overgrowth.ExpectedVolleys * MaxFieldShare * (1f - _overgrowth.FireRate) * _overgrowth.ExpectedOvergrownSeconds;
                case BossSignature.Splitting:
                    // While it is split, the towers on the routes a shard does not take fire at nothing: a third of the field.
                    return _split.ExpectedSplitSeconds / Mathf.Max(1, _split.Shards.Length);
                case BossSignature.Recognition:
                    // Each phase one gate's gun is drained for the phase.
                    return _recognition.DrainsGates ? _phases.Length * _recognition.GateGunShare * _recognition.ExpectedPhaseSeconds : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// How much tougher its own defence makes it than its health says, for the balance ledger: an ice armour that lets
        /// only a share of each hit through over the armoured phases costs that much more firepower per point of health.
        /// </summary>
        public float EffectiveHealthFactor()
        {
            if (_signature == BossSignature.Recognition)
            {
                // From its second phase on the tower type that hurt it most lands only a share of its hits.
                float learnedShare = _phases.Length > 1 ? _phases[1].StartsAt : 0f;
                return 1f + learnedShare * _recognition.ExpectedTopShare * (1f / _recognition.DampenedShare - 1f);
            }

            if (_signature != BossSignature.GateBreaker)
            {
                return 1f;
            }

            int exposed = Mathf.Clamp(_gateBreaker.ExposedPhase, 0, _phases.Length - 1);
            float armouredShare = exposed > 0 ? 1f - _phases[exposed].StartsAt : 0f;
            return 1f + armouredShare * (1f / _gateBreaker.ArmourSoak - 1f);
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

            if (_signature == BossSignature.HuntTheCold && _hunt.Clutches > 0 && (_hunt.Egg == null || _hunt.Hatchling == null))
            {
                Debug.LogWarning($"BossDefinition '{name}': it lays eggs but names no egg or hatchling.", this);
            }

            if (_signature == BossSignature.Overgrowth && _overgrowth.Pod == null)
            {
                Debug.LogWarning($"BossDefinition '{name}': it spores the pads but names no pod.", this);
            }

            if (_signature == BossSignature.Splitting && (_split.Shards.Length == 0 || _split.RejoinPhase <= _split.SplitPhase || _split.RejoinPhase >= _phases.Length))
            {
                Debug.LogWarning($"BossDefinition '{name}': it splits but has no shards, or its rejoin phase does not follow its split.", this);
            }

            if (_signature == BossSignature.GateBreaker && _gateBreaker.ExposedPhase >= _phases.Length)
            {
                Debug.LogWarning($"BossDefinition '{name}': the exposed phase lies past its last phase.", this);
            }

            for (int i = 0; _difficultyHealth != null && i < _difficultyHealth.Length; i++)
            {
                if (string.IsNullOrEmpty(_difficultyHealth[i].Difficulty) || _difficultyHealth[i].Factor <= 0f)
                {
                    Debug.LogWarning($"BossDefinition '{name}': difficulty health entry {i} needs a difficulty name and a factor above zero.", this);
                }

                for (int j = 0; j < i; j++)
                {
                    if (_difficultyHealth[j].Difficulty == _difficultyHealth[i].Difficulty)
                    {
                        Debug.LogWarning($"BossDefinition '{name}': difficulty {_difficultyHealth[i].Difficulty} has two health factors.", this);
                    }
                }
            }

            if (string.IsNullOrEmpty(_signatureText) || string.IsNullOrEmpty(_telegraphText) || string.IsNullOrEmpty(_counterText))
            {
                Debug.LogWarning($"BossDefinition '{name}': the Codex needs its signature, telegraph and counter text.", this);
            }
        }
    }
}
