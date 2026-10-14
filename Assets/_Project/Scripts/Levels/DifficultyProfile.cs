using UnityEngine;

namespace Game.Levels
{
    /// <summary>Multipliers that turn one level into easy, medium, hard or psycho.</summary>
    [CreateAssetMenu(menuName = "Game/Difficulty Profile", fileName = "DifficultyProfile")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [SerializeField] private string _displayName = "Medium";
        [SerializeField] [Min(0.1f)] private float _healthMultiplier = 1f;
        [SerializeField] [Min(0.1f)] private float _countMultiplier = 1f;
        [SerializeField] [Min(0.1f)] private float _spawnIntervalMultiplier = 1f;
        [SerializeField] [Min(0.1f)] private float _rewardMultiplier = 1f;
        [SerializeField] [Min(0f)] private float _powerCurrencyMultiplier = 1f;

        public string DisplayName => _displayName;
        public float HealthMultiplier => _healthMultiplier;
        public float CountMultiplier => _countMultiplier;
        public float SpawnIntervalMultiplier => _spawnIntervalMultiplier;

        /// <summary>Scales kill rewards. Higher difficulties pay more so levels are worth replaying.</summary>
        public float RewardMultiplier => _rewardMultiplier;

        /// <summary>Scales the power currency reward. Zero on easy, so drones need harder difficulties.</summary>
        public float PowerCurrencyMultiplier => _powerCurrencyMultiplier;
    }
}

