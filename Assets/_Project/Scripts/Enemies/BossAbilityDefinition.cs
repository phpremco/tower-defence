using UnityEngine;

namespace Game.Enemies
{
    /// <summary>One special power of a boss, with its timing and strength. A boss lists the abilities it carries.</summary>
    [CreateAssetMenu(menuName = "Game/Boss Ability", fileName = "BossAbility")]
    public sealed class BossAbilityDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Ability";
        [SerializeField] private BossAbilityKind _kind;
        [SerializeField] [Min(0.5f)] private float _interval = 8f;
        [SerializeField] [Min(0f)] private float _firstDelay = 4f;
        [SerializeField] [Min(0f)] private float _damage = 20f;
        [SerializeField] [Min(0.1f)] private float _radius = 3f;
        [SerializeField] [Min(0.1f)] private float _sweepSeconds = 3f;
        [SerializeField] [Min(0.5f)] private float _range = 6f;
        [SerializeField] private EnemyDefinition _minion;
        [SerializeField] [Min(1)] private int _minionCount = 3;
        [SerializeField] [Min(1)] private int _targetCount = 3;
        [SerializeField] [Min(1f)] private float _speedFactor = 1.5f;
        [SerializeField] [Range(0.05f, 1f)] private float _triggerFraction = 0.25f;
        [SerializeField] private Color _color = new Color(1f, 0.4f, 0.3f);

        public string DisplayName => _displayName;
        public BossAbilityKind Kind => _kind;

        /// <summary>Seconds between two uses.</summary>
        public float Interval => _interval;

        /// <summary>Seconds after the boss appears before the first use.</summary>
        public float FirstDelay => _firstDelay;

        /// <summary>Damage per pulse, per bolt, or per second while a laser touches a structure; armour per regrow.</summary>
        public float Damage => _damage;

        /// <summary>Reach of a pulse.</summary>
        public float Radius => _radius;

        /// <summary>How long one laser burn, one rally or one ward lasts.</summary>
        public float SweepSeconds => _sweepSeconds;

        /// <summary>
        /// For the balance ledger: the share of a fight a ward keeps the boss sealed (its duration over its interval), and the
        /// creatures a spawn power sends out per second of the fight. Both read only the timing fields.
        /// </summary>
        public float WardShare => _kind == BossAbilityKind.Ward && _interval > 0f ? Mathf.Clamp01(_sweepSeconds / _interval) : 0f;

        public float MinionsPerSecond => _kind == BossAbilityKind.Spawn && _minion != null && _interval > 0f ? _minionCount / _interval : 0f;

        /// <summary>How far a laser or a volley reaches.</summary>
        public float Range => _range;

        public EnemyDefinition Minion => _minion;
        public int MinionCount => _minionCount;

        /// <summary>How many structures one volley hits at most.</summary>
        public int TargetCount => _targetCount;

        /// <summary>Speed factor a rally gives.</summary>
        public float SpeedFactor => _speedFactor;

        /// <summary>Share of health lost between two splits: a quarter means minions at 75, 50 and 25 percent.</summary>
        public float TriggerFraction => _triggerFraction;

        /// <summary>Colour of the effect; audio and views read it, logic does not.</summary>
        public Color Color => _color;

        private void OnValidate()
        {
            if ((_kind == BossAbilityKind.Spawn || _kind == BossAbilityKind.Split) && _minion == null)
            {
                Debug.LogWarning($"BossAbility '{name}' spawns minions but names no enemy.", this);
            }

            if (_kind == BossAbilityKind.Ward && _sweepSeconds >= _interval)
            {
                Debug.LogWarning($"BossAbility '{name}': a ward must be down longer than it is up (duration {_sweepSeconds} s, interval {_interval} s).", this);
            }
        }
    }
}
