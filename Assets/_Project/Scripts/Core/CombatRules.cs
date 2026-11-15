using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The combat rules, one table for every damage source in the game (round 67, Remco: "one set of general combat rules
    /// that every damage source follows, with only explicit, named exceptions"). A weapon carries its damage type and
    /// nothing else; what a hit is worth against armour and in the air comes from here (<see cref="ProfileOf"/>), and a
    /// weapon that must differ is listed as an exception with its reason. A creature carries its numbers and nothing else;
    /// its traits come from here as well (<see cref="TraitsOf"/>). The reference fights per trait and their margins are
    /// what the role tests enforce, and the grace windows are what the level balance checks (Docs/COMBAT_RULES.md).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Combat Rules", fileName = "CombatRules")]
    public sealed class CombatRules : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Balance/CombatRules.asset";

        public const int DifficultyCount = 4;

        [Header("Damage type against trait")]
        [SerializeField] private DamageRule[] _damage = new DamageRule[0];

        [Header("Named exceptions (the only place a weapon differs from its type)")]
        [SerializeField] private CombatException[] _exceptions = new CombatException[0];

        [Header("Creature traits from their numbers")]
        [Tooltip("Body plus armour at or under this is a swarm creature.")]
        [SerializeField] [Min(1f)] private float _swarmHealth = 40f;

        [Tooltip("Body plus armour at or over this is a tough creature; every boss is tough.")]
        [SerializeField] [Min(1f)] private float _toughHealth = 250f;

        [Tooltip("Average speed at or over this (cells a second) is fast.")]
        [SerializeField] [Min(0.1f)] private float _fastSpeed = 1.3f;

        [Header("Roles: the reference fight per trait")]
        [SerializeField] private RoleScenario[] _scenarios = new RoleScenario[0];

        [Tooltip("Every reference fight is played at each of these health scales and the results averaged, so a role never rests on one lucky number (a hit that just kills, or just does not).")]
        [SerializeField] private float[] _healthLadder = { 0.75f, 1f, 1.5f, 2.25f };

        [Tooltip("Health scale of the reference creatures when the towers stand at their top level with every permanent upgrade: late waves are that much tougher.")]
        [SerializeField] [Min(1f)] private float _lateHealthScale = 6f;

        [Tooltip("A helper (hero, drone, path object, gate or base gun) is weighed over this many seconds of a tower's fire: what it does in its whole life against what the same coins of the on-role tower do in this time.")]
        [SerializeField] [Min(1f)] private float _helperHorizonSeconds = 120f;

        [Tooltip("The coins a free helper (the hero, the base's turret, a gate's gun) is weighed as.")]
        [SerializeField] [Min(1)] private int _freeHelperCoins = 100;

        [Header("Growth: grace after a new creature trait appears")]
        [Tooltip("Per difficulty (Easy to Psycho): waves a defence without the counter tower may still hold after a trait first appears.")]
        [SerializeField] private int[] _graceWaves = { 2, 1, 1, 0 };

        [Tooltip("Per difficulty: waves after the grace window within which that defence must have lost.")]
        [SerializeField] private int[] _fallWithinWaves = { 4, 4, 3, 3 };

        /// <summary>Raised by one whenever a rules asset loads or changes, so a definition's cached profile is read again.</summary>
        public static int Version { get; private set; }

        public DamageRule[] Damage => _damage;
        public CombatException[] Exceptions => _exceptions;
        public RoleScenario[] Scenarios => _scenarios;
        public float SwarmHealth => _swarmHealth;
        public float ToughHealth => _toughHealth;
        public float FastSpeed => _fastSpeed;
        public float[] HealthLadder => _healthLadder;
        public float LateHealthScale => _lateHealthScale;
        public float HelperHorizonSeconds => _helperHorizonSeconds;
        public int FreeHelperCoins => _freeHelperCoins;

        /// <summary>Waves a defence without the counter tower may hold after a trait first appears, on a difficulty (0 Easy to 3 Psycho).</summary>
        public int GraceWaves(int difficulty) => _graceWaves[Mathf.Clamp(difficulty, 0, _graceWaves.Length - 1)];

        /// <summary>Waves after the grace window within which a defence without the counter tower must have lost.</summary>
        public int FallWithinWaves(int difficulty) => _fallWithinWaves[Mathf.Clamp(difficulty, 0, _fallWithinWaves.Length - 1)];

        /// <summary>The row of a damage type; a type without a row lands in full and says so when the asset is validated.</summary>
        public DamageRule RuleOf(DamageType type)
        {
            for (int i = 0; i < _damage.Length; i++)
            {
                if (_damage[i].Type == type)
                {
                    return _damage[i];
                }
            }

            return new DamageRule { Type = type, Armour = 1f, Air = 1f };
        }

        /// <summary>What a weapon's hits are worth: its type's row, with the weapon's own exceptions over it.</summary>
        /// <param name="weapon">The weapon's asset name, as the exceptions list it.</param>
        public DamageProfile ProfileOf(DamageType type, string weapon)
        {
            DamageRule rule = RuleOf(type);
            float armour = rule.Armour;
            float air = rule.Air;
            for (int i = 0; i < _exceptions.Length; i++)
            {
                if (!string.Equals(_exceptions[i].Weapon, weapon, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_exceptions[i].Trait == EnemyTraits.Armoured)
                {
                    armour = _exceptions[i].Factor;
                }
                else if (_exceptions[i].Trait == EnemyTraits.Airborne)
                {
                    air = _exceptions[i].Factor;
                }
            }

            return new DamageProfile(armour, air);
        }

        /// <summary>A creature's traits from its numbers: one size (swarm, mob, tough) and the modifiers that apply.</summary>
        /// <param name="body">Hit points under the armour.</param>
        /// <param name="speed">Its average speed over a walk-and-roll cycle, in cells a second.</param>
        public EnemyTraits TraitsOf(float body, float armour, float speed, TargetLayers layer, bool boss)
        {
            float toughness = body + armour;
            EnemyTraits traits = boss || toughness >= _toughHealth ? EnemyTraits.Tough : toughness <= _swarmHealth ? EnemyTraits.Swarm : EnemyTraits.Mob;
            if (armour > 0f)
            {
                traits |= EnemyTraits.Armoured;
            }

            if ((layer & TargetLayers.Ground) == 0)
            {
                traits |= EnemyTraits.Airborne;
            }

            if (speed >= _fastSpeed)
            {
                traits |= EnemyTraits.Fast;
            }

            return traits;
        }

        /// <summary>The creature traits a tower of this role is bought against; none for the roles that do not fight a trait.</summary>
        public static EnemyTraits Answers(TowerRole role)
        {
            switch (role)
            {
                case TowerRole.LightFire:
                    return EnemyTraits.Swarm | EnemyTraits.Fast | EnemyTraits.Tough;
                case TowerRole.Splash:
                    return EnemyTraits.Mob;
                case TowerRole.AntiArmour:
                    return EnemyTraits.Armoured;
                case TowerRole.AntiAir:
                    return EnemyTraits.Airborne;
                default:
                    return EnemyTraits.None;
            }
        }

        /// <summary>The reference fight of a trait, or false when the rules have none for it.</summary>
        public bool TryScenario(EnemyTraits trait, out RoleScenario scenario)
        {
            for (int i = 0; i < _scenarios.Length; i++)
            {
                if (_scenarios[i].Trait == trait)
                {
                    scenario = _scenarios[i];
                    return true;
                }
            }

            scenario = default;
            return false;
        }

        private void OnEnable() => Version++;

        private void OnValidate()
        {
            Version++;
            foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
            {
                bool found = false;
                for (int i = 0; i < _damage.Length; i++)
                {
                    found |= _damage[i].Type == type;
                }

                if (!found)
                {
                    Debug.LogWarning($"CombatRules '{name}' has no row for {type} damage: it would land in full on everything.", this);
                }
            }

            for (int i = 0; i < _exceptions.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(_exceptions[i].Reason) || (_exceptions[i].Trait != EnemyTraits.Armoured && _exceptions[i].Trait != EnemyTraits.Airborne))
                {
                    Debug.LogWarning($"CombatRules '{name}': the exception for '{_exceptions[i].Weapon}' needs a reason and the trait Armoured or Airborne.", this);
                }
            }

            if (_graceWaves.Length != DifficultyCount || _fallWithinWaves.Length != DifficultyCount)
            {
                Debug.LogWarning($"CombatRules '{name}' needs a grace window and a fall window for each of the {DifficultyCount} difficulties.", this);
            }
        }
    }
}
