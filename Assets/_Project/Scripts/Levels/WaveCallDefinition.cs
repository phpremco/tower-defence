using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Calling the next wave early (round 73): the player sends the next wave in while the countdown still runs, on top of the
    /// creatures already on the field, and is paid for the seconds the countdown had left. The balance model assumes the player
    /// waits; calling early is an optional risk, so the bonus stays modest.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Wave Call", fileName = "WaveCall")]
    public sealed class WaveCallDefinition : ScriptableObject
    {
        [Tooltip("Coins paid per second still left on the countdown when the next wave is called.")]
        [SerializeField] [Min(0f)] private float _coinsPerSecond = 1f;

        [Tooltip("The most one call pays, however long the countdown still had to run.")]
        [SerializeField] [Min(0)] private int _maxBonusPerCall = 8;

        [Tooltip("Game seconds after a call before the next call is accepted, so mashing the button cannot send several waves at once.")]
        [SerializeField] [Min(0f)] private float _cooldownSeconds = 4f;

        [Tooltip("Whether the first wave may be called before its countdown ends. Off: the opening countdown is the player's build time.")]
        [SerializeField] private bool _allowBeforeFirstWave;

        public float CoinsPerSecond => _coinsPerSecond;

        public int MaxBonusPerCall => _maxBonusPerCall;

        public float CooldownSeconds => _cooldownSeconds;

        public bool AllowBeforeFirstWave => _allowBeforeFirstWave;

        /// <summary>The coins a call pays when the countdown still had this many seconds to run (whole coins, rounded down, capped).</summary>
        public int BonusFor(float secondsSaved)
        {
            if (secondsSaved <= 0f)
            {
                return 0;
            }

            return Mathf.Min(_maxBonusPerCall, Mathf.FloorToInt(secondsSaved * _coinsPerSecond));
        }

        private static WaveCallDefinition _defaults;

        /// <summary>The field defaults, for a level played without a campaign (a bare test level); the campaign's asset is the real data.</summary>
        public static WaveCallDefinition Defaults
        {
            get
            {
                if (_defaults == null)
                {
                    _defaults = CreateInstance<WaveCallDefinition>();
                    _defaults.hideFlags = HideFlags.DontSave;
                }

                return _defaults;
            }
        }

        /// <summary>Settings for a test or tool that builds them in code.</summary>
        public static WaveCallDefinition Create(float coinsPerSecond, int maxBonusPerCall, float cooldownSeconds, bool allowBeforeFirstWave)
        {
            var definition = CreateInstance<WaveCallDefinition>();
            definition._coinsPerSecond = coinsPerSecond;
            definition._maxBonusPerCall = maxBonusPerCall;
            definition._cooldownSeconds = cooldownSeconds;
            definition._allowBeforeFirstWave = allowBeforeFirstWave;
            return definition;
        }

        private void OnValidate()
        {
            if (_maxBonusPerCall > 0 && _coinsPerSecond <= 0f)
            {
                Debug.LogWarning($"{name}: a bonus cap without coins per second pays nothing.", this);
            }
        }
    }
}
