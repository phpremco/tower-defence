using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Every sound one species makes: how it moves, steps, cries, attacks and, for a boss, uses its powers. An
    /// enemy definition points at one voice, so a boss is simply a creature with a heavier voice asset. Any set left
    /// empty falls back to the generated sound of that kind, or stays silent where there is nothing to fall back on.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Creature Voice", fileName = "Voice")]
    public sealed class CreatureVoice : ScriptableObject
    {
        [Header("Moving")]
        [Tooltip("A seamless loop that plays while the creature travels: slime, wings, skittering legs.")]
        [SerializeField] private SoundSet _moveLoop = new SoundSet();

        [Tooltip("One-shot footsteps, one per stride. Leave empty for creatures that glide or fly.")]
        [SerializeField] private SoundSet _steps = new SoundSet();

        [Tooltip("World units travelled between two steps. Bigger creatures stride further.")]
        [SerializeField] [Min(0.1f)] private float _stepSpacing = 0.5f;

        [Tooltip("How loud this species' movement loop sits against the others at the same crowd size: a quiet hum needs more than skittering legs.")]
        [SerializeField] [Min(0f)] private float _presence = 1f;

        [Tooltip("The loop of a creature that has curled up and rolls instead of walking.")]
        [SerializeField] private SoundSet _rollLoop = new SoundSet();

        [Header("Cries")]
        [Tooltip("The cry on dying.")]
        [SerializeField] private SoundSet _screeches = new SoundSet();

        [Tooltip("A boss announces itself with this when it enters the field.")]
        [SerializeField] private SoundSet _growls = new SoundSet();

        [Header("Attacking")]
        [Tooltip("Played on every attack: a bite, a sting, a bolt leaving the mouth.")]
        [SerializeField] private SoundSet _attacks = new SoundSet();

        [Tooltip("For ranged attacks, the sound where the shot lands, after its flight.")]
        [SerializeField] private SoundSet _impacts = new SoundSet();

        [Header("Boss powers")]
        [SerializeField] private SoundSet _pulse = new SoundSet();
        [SerializeField] private SoundSet _volleyLaunch = new SoundSet();
        [SerializeField] private SoundSet _volleyImpact = new SoundSet();
        [SerializeField] private SoundSet _rally = new SoundSet();
        [SerializeField] private SoundSet _regrow = new SoundSet();
        [SerializeField] private SoundSet _spawn = new SoundSet();
        [SerializeField] private SoundSet _laserStart = new SoundSet();

        public SoundSet MoveLoop => _moveLoop;
        public SoundSet Steps => _steps;
        public float StepSpacing => _stepSpacing;
        public float Presence => _presence;
        public SoundSet RollLoop => _rollLoop;
        public SoundSet Screeches => _screeches;
        public SoundSet Growls => _growls;
        public SoundSet Attacks => _attacks;
        public SoundSet Impacts => _impacts;
        public SoundSet Pulse => _pulse;
        public SoundSet VolleyLaunch => _volleyLaunch;
        public SoundSet VolleyImpact => _volleyImpact;
        public SoundSet Rally => _rally;
        public SoundSet Regrow => _regrow;
        public SoundSet Spawn => _spawn;
        public SoundSet LaserStart => _laserStart;

        /// <summary>Every set with its label, for the sound board and the content tests.</summary>
        public (string Label, SoundSet Set)[] Sets() => new[]
        {
            ("move", _moveLoop), ("steps", _steps), ("roll", _rollLoop), ("screech", _screeches), ("growl", _growls),
            ("attack", _attacks), ("impact", _impacts), ("pulse", _pulse), ("volley", _volleyLaunch),
            ("volley impact", _volleyImpact), ("rally", _rally), ("regrow", _regrow), ("spawn", _spawn), ("laser", _laserStart),
        };
    }
}
