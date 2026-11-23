using Game.Core;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] [Min(0.1f)] private float _structureDamageMultiplier = 1f;
        [Tooltip("The structure damage factor: creatures bite the base, gates, towers and the hero at this share of the structure damage multiplier. A lever of its own: the wave solver keeps sizing creatures against the multiplier alone, so softer bites are not answered with tougher creatures.")]
        [SerializeField] [Range(0.1f, 1f)] private float _structureDamageFactor = 1f;
        [Tooltip("Round 88f, the tower damage factor: a gun beside the road (tower, base turret) takes this share of the structure damage in play; gates, the base and the hero take it in full. Below one on Psycho: its creatures are more and live longer, so at the full bite the boss volleys wrecked upgraded guns faster than a repair crew or a purse could answer.")]
        [SerializeField] [Range(0.1f, 1f)] private float _towerDamageFactor = 1f;
        [SerializeField] [Min(0.1f)] private float _incomeMultiplier = 1f;
        [SerializeField] [Min(0.1f)] private float _startingMoneyMultiplier = 1f;
        [Tooltip("Extra health of a continent boss on this difficulty, on top of the health multiplier every creature gets: the boss carries its share of the finale threat (Docs/BALANCE.md). Level bosses in campaign waves are sized by the wave solver instead.")]
        [SerializeField] [Min(0.1f)] private float _bossHealthShare = 1f;
        [Tooltip("Share of a continent boss's escort call that comes out on this difficulty (round 30: Psycho carries Hard's boss health share, so its escorts are lighter instead of the boss).")]
        [SerializeField] [Range(0f, 1f)] private float _bossEscortShare = 1f;
        [Tooltip("Round 88b, level bosses: the health of the brood a level boss sends out of its body over an unhindered walk from spawn to the base, as a share of the boss's own bar (body plus armour) as spawned. The brood grows along the walk (BalanceRules.EscortStartWeight, EscortEndWeight). Zero sends none.")]
        [SerializeField] [Range(0f, 2f)] private float _escortWorth = 0.4f;
        [Tooltip("Round 88d: a level whose waves on this difficulty hold an air raid starts with extra money worth this many of the sky gun the level sells (StartMoneyRule). Zero: no extra (Easy and Medium, where the first-flyer rule already waits for the bounties).")]
        [FormerlySerializedAs("_firstRaidPurseGuns")]
        [SerializeField] [Range(0f, 4f)] private float _raidStartGuns;

        public string DisplayName => Loc.Get(_displayName);

        /// <summary>Sky guns' worth of extra start money on a level with an air raid (<see cref="StartMoneyRule"/>).</summary>
        public float RaidStartGuns => _raidStartGuns;
        public float HealthMultiplier => _healthMultiplier;
        public float CountMultiplier => _countMultiplier;
        public float SpawnIntervalMultiplier => _spawnIntervalMultiplier;

        /// <summary>Scales the research points of the level reward. Higher difficulties pay more so levels are worth replaying.</summary>
        public float RewardMultiplier => _rewardMultiplier;

        /// <summary>Scales the power currency reward. Zero on easy, so drones need harder difficulties.</summary>
        public float PowerCurrencyMultiplier => _powerCurrencyMultiplier;

        /// <summary>
        /// Scales what creatures do to the base, gates, towers and the hero in play, so a tail sting bites harder on psycho:
        /// the calibrated multiplier times the structure damage factor (<see cref="StructureDamageFactor"/>).
        /// </summary>
        public float StructureDamageMultiplier => _structureDamageMultiplier * _structureDamageFactor;

        /// <summary>
        /// The structure damage the siege model and the wave solver were calibrated with, without the factor. The balance
        /// model reads this one, so the factor stays a lever of its own, as the money shares are.
        /// </summary>
        public float CalibratedStructureDamage => _structureDamageMultiplier;

        /// <summary>
        /// The structure damage factor (Remco, round 77): below one, a creature that reaches the base keeps biting, it only
        /// bites softer, so one unshot creature costs health instead of the level.
        /// </summary>
        public float StructureDamageFactor => _structureDamageFactor;

        /// <summary>
        /// The share of the structure damage in play that lands on a gun (<see cref="Game.Combat.StructureRegistry.GunDamageShare"/>):
        /// a damaged tower is something to answer with a repair, not a sudden loss.
        /// </summary>
        public float TowerDamageFactor => _towerDamageFactor;

        /// <summary>
        /// Scales the money earned inside a level (bounties and harvested ore). Below one on Easy: the waves stay soft, but a
        /// good player no longer buys two or three times the defence they need, so the hero still matters on the hard waves.
        /// </summary>
        public float IncomeMultiplier => _incomeMultiplier;

        /// <summary>Scales the level's starting money.</summary>
        public float StartingMoneyMultiplier => _startingMoneyMultiplier;

        /// <summary>Extra health of a continent boss (one with boss data) on this difficulty, over <see cref="HealthMultiplier"/>.</summary>
        public float BossHealthShare => _bossHealthShare;

        /// <summary>Share of a continent boss's escort call (its Spawn power) released on this difficulty; splits are untouched.</summary>
        public float BossEscortShare => _bossEscortShare;

        /// <summary>
        /// The brood of a level boss (one without boss data) over a whole unhindered walk, as a share of the boss's bar as
        /// spawned (round 88b). A continent boss's call does not read it; it keeps <see cref="BossEscortShare"/>.
        /// </summary>
        public float EscortWorth => _escortWorth;

        // Past this a single bite reads as a different game: the siege model and the grace windows were measured well inside it.
        private const float MaxStructureDamage = 2f;

        private void OnValidate()
        {
            if (_structureDamageFactor <= 0f || _structureDamageFactor > 1f)
            {
                Debug.LogWarning($"DifficultyProfile '{name}': structure damage factor {_structureDamageFactor} must be above 0 and at most 1 (it only softens; harder bites belong in the multiplier, which the wave solver reads).", this);
            }

            if (StructureDamageMultiplier > MaxStructureDamage)
            {
                Debug.LogWarning($"DifficultyProfile '{name}': creatures bite at {StructureDamageMultiplier} times their damage, above {MaxStructureDamage}; they would break a gate or the base in a few bites.", this);
            }
        }
    }
}
