using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>Stats, upgrade levels and look of one tower type.</summary>
    [CreateAssetMenu(menuName = "Game/Tower Definition", fileName = "TowerDefinition")]
    public sealed class TowerDefinition : ScriptableObject
    {
        public const int LevelCount = 3;

        [SerializeField] private string _displayName = "Tower";
        [SerializeField] private TargetLayers _targets = TargetLayers.Ground;
        [SerializeField] private TowerLevel[] _levels = new TowerLevel[LevelCount];
        [SerializeField] [Min(1f)] private float _projectileSpeed = 10f;
        [SerializeField] [Min(0f)] private float _splashRadius;

        /// <summary>
        /// Most creatures one blast hurts (the nearest to the impact); zero hurts everything in the radius. The Cannon's cap keeps
        /// its shell a group weapon, not a swarm weapon: a dense ant crowd asks for rate of fire (Docs/BALANCE.md, round 22).
        /// </summary>
        [SerializeField] [Min(0)] private int _splashTargets;

        [Tooltip("What the damage is made of. What a hit is worth against armour and in the air follows from it through the combat rules; nothing is typed here (round 67).")]
        [SerializeField] private DamageType _damageType = DamageType.Explosive;

        [Tooltip("The one rule table every damage source reads (Data/Balance/CombatRules).")]
        [SerializeField] private CombatRules _rules;

        [Tooltip("The job this tower is bought for, first; then the few other jobs it also does (the Cannon also strips armour, a hybrid has its parents' jobs). The role tests hold every tower to its list per coin.")]
        [SerializeField] private TowerRole[] _roles = new TowerRole[0];

        [Tooltip("The role in one line, for the rules page and the Codex.")]
        [SerializeField] private string _roleLine = string.Empty;

        [System.NonSerialized] private ProfileCache _profileCache;

        /// <summary>
        /// A guided shot whose creature dies in flight turns to the nearest live creature of the same layer this close to where
        /// it was heading (cells); zero lets it land on nothing. Remco (2026-11-06), the genre's convention: guided missiles (the
        /// Flak) turn; bullets (the Gun Turret, fast enough to be near instant), ballistic shells (the Cannon) and instant beams,
        /// chains and rails (Laser, Tesla, Railgun) do not. Only homing shots can turn.
        /// </summary>
        [SerializeField] [Min(0f)] private float _retargetRadius;

        /// <summary>
        /// True: the shot follows its creature and lands on it. False (splash shells only): it is fired at the spot the creature
        /// stood on and bursts there after its flight time (distance over projectile speed), so a fast runner can be out of the
        /// blast while a queue or a pack is not.
        /// </summary>
        [SerializeField] private bool _homing = true;

        /// <summary>A proximity fuse: the shell bursts this close to its target. Zero bursts on impact.</summary>
        [SerializeField] [Min(0f)] private float _fuseRadius;
        [SerializeField] [Min(1f)] private float _maxHealth = 100f;

        /// <summary>Share of everything spent on the tower that selling gives back.</summary>
        [SerializeField] [Range(0f, 1f)] private float _sellRefundFraction = 0.7f;

        /// <summary>Share of everything spent that scrapping a wreck gives back.</summary>
        [SerializeField] [Range(0f, 1f)] private float _scrapFraction = 0.3f;

        [SerializeField] [Min(0f)] private float _repairCostPerHealth = 0.4f;

        /// <summary>Power of a nearly destroyed tower: 1 is no penalty, 0.5 means half damage and half fire rate.</summary>
        [SerializeField] [Range(0f, 1f)] private float _damagedPowerFloor = 0.5f;

        /// <summary>Speed share enemies hit by this tower are slowed to. One means no slow.</summary>
        [SerializeField] [Range(0.1f, 1f)] private float _slowFactor = 1f;

        [SerializeField] [Min(0f)] private float _slowSeconds;

        /// <summary>A support tower does no damage. It boosts the range and fire rate of towers within its range.</summary>
        [SerializeField] private bool _isSupport;

        [SerializeField] [Min(0f)] private float _supportRangeBonus;

        [SerializeField] [Min(0f)] private float _supportRateBonus;

        /// <summary>A blockade stops ground enemies until it is destroyed. It is set in the level and cannot be sold.</summary>
        [SerializeField] private bool _blocksGroundEnemies;

        /// <summary>A gun that belongs to the base: built from the permanent base upgrades, never sold.</summary>
        [SerializeField] private bool _isBaseDefence;

        [SerializeField] private Color _color = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private TowerShape _shape;
        [SerializeField] private ProjectileStyle _projectileStyle;

        [Tooltip("Beam only: seconds the beam charges before its first pulse when it was idle. A live beam swings on to the next creature without charging.")]
        [SerializeField] [Min(0f)] private float _beamLockSeconds = 0.35f;

        [Tooltip("Beam only: after a kill the beam sweeps on to the next creature this soon, instead of waiting out its pulse interval. Zero waits the full interval.")]
        [SerializeField] [Min(0f)] private float _beamSweepSeconds;

        [Tooltip("Seconds a turret needs to swing to a new target: its first shot at a creature other than the one it shot last comes this much later. Zero for a tower that does not swing (a beam has its sweep, a blast its shell).")]
        [SerializeField] [Min(0f)] private float _retargetSeconds;

        /// <summary>The imported model per level; when set it replaces the procedural shape.</summary>
        [SerializeField] private TowerArtDefinition _art;
        [Tooltip("A gate's imported look (pylons, beam, panels, wreck); empty keeps the procedural gate.")]
        [SerializeField] private GateArtDefinition _gateArt;

        public string Id => name;
        public string DisplayName => Loc.Get(_displayName);
        public TargetLayers Targets => _targets;
        public float ProjectileSpeed => _projectileSpeed;
        public float SplashRadius => _splashRadius;

        /// <summary>
        /// Most creatures one blast hurts at a level (zero-based): the level's own cap, else the tower's; zero hurts everything
        /// in the radius. The one reader for the game (Tower), the balance model (BalanceLedger.Stats) and the texts, so they
        /// cannot disagree (round 88b, Remco: the Cannon's shell hurts two creatures and more with every star).
        /// </summary>
        public int SplashTargetsAt(int levelIndex)
        {
            int own = _levels != null && levelIndex >= 0 && levelIndex < _levels.Length ? _levels[levelIndex].SplashTargets : 0;
            return own > 0 ? own : _splashTargets;
        }

        /// <summary>True when the blast's cap is not the same at every level: an upgrade then also widens what one blast hurts.</summary>
        public bool SplashTargetsGrow => _splashRadius > 0f && SplashTargetsAt(0) != SplashTargetsAt(MaxLevelIndex);

        /// <summary>What the damage is made of: the combat rules turn it into what a hit is worth against armour and in the air.</summary>
        public DamageType DamageType => _damageType;

        /// <summary>What this tower's hits are worth against armour and in the air, from the rule table (and its named exceptions).</summary>
        public DamageProfile Profile => _profileCache.Get(_rules, _damageType, this);

        /// <summary>The job the tower is bought for (its first role); a tower without roles only helps.</summary>
        public TowerRole Role => _roles.Length > 0 ? _roles[0] : TowerRole.Helper;

        /// <summary>Every job it does, the one it is bought for first.</summary>
        public System.Collections.Generic.IReadOnlyList<TowerRole> Roles => _roles;

        /// <summary>The role in one line.</summary>
        public string RoleLine => Loc.Get(_roleLine);

        /// <summary>True when any of its roles answers creatures of this trait.</summary>
        public bool Answers(EnemyTraits trait)
        {
            for (int i = 0; i < _roles.Length; i++)
            {
                if ((CombatRules.Answers(_roles[i]) & trait) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public float FuseRadius => _fuseRadius;
        public float RetargetRadius => _retargetRadius;
        public bool Homing => _homing;
        public float MaxHealth => _maxHealth;
        public float SellRefundFraction => _sellRefundFraction;
        public float ScrapFraction => _scrapFraction;
        public float RepairCostPerHealth => _repairCostPerHealth;
        public float DamagedPowerFloor => _damagedPowerFloor;
        public float SlowFactor => _slowFactor;
        public float SlowSeconds => _slowSeconds;
        public bool IsSupport => _isSupport;
        public float SupportRangeBonus => _supportRangeBonus;
        public float SupportRateBonus => _supportRateBonus;
        public bool BlocksGroundEnemies => _blocksGroundEnemies;
        public bool IsBaseDefence => _isBaseDefence;
        public Color Color => _color;

        /// <summary>The imported model shown for this tower, or null to use placeholder shapes.</summary>
        /// <summary>The procedural model this tower wears, or None to use <see cref="Model"/> or the placeholder.</summary>
        public TowerShape Shape => _shape;

        /// <summary>How this tower's shots look and sound.</summary>
        public ProjectileStyle ProjectileStyle => _projectileStyle;

        /// <summary>A beam holds one creature until it dies or leaves reach, instead of picking a target every pulse.</summary>
        public bool IsBeam => _projectileStyle == ProjectileStyle.Laser;

        public float BeamLockSeconds => _beamLockSeconds;

        public float BeamSweepSeconds => _beamSweepSeconds;

        /// <summary>Seconds before the first shot at a new target, on top of the reload (<see cref="Retarget"/>); zero for most.</summary>
        public float RetargetSeconds => _retargetSeconds;


        /// <summary>The imported (Blender) look, or null for the procedural shape.</summary>
        public TowerArtDefinition Art => _art;
        public GateArtDefinition GateArt => _gateArt;
        public int MaxLevelIndex => _levels.Length - 1;

        /// <summary>Price to build the tower, which is the cost of its first level.</summary>
        public int BuildCost => _levels[0].Cost;

        public TowerLevel GetLevel(int index) => _levels[index];

        private void OnValidate()
        {
            if (_levels == null || _levels.Length != LevelCount)
            {
                Debug.LogWarning($"TowerDefinition '{name}' must have exactly {LevelCount} levels.", this);
            }

            if (!_homing && _splashRadius <= 0f)
            {
                Debug.LogWarning($"TowerDefinition '{name}' fires unaimed single shots: only a splash shell may be fired at a spot.", this);
            }

            _profileCache.Clear();
            if ((_targets & TargetLayers.Air) != 0 && !_isSupport && _rules != null && Profile.Air <= 0f)
            {
                Debug.LogWarning($"TowerDefinition '{name}' aims at the sky with {_damageType} damage, which never reaches it: drop the layer from its targets.", this);
            }

            string blast = SplashTargetsProblem();
            if (blast != null)
            {
                Debug.LogWarning($"TowerDefinition '{name}': {blast}", this);
            }

            string step = StepProblem();
            if (step != null)
            {
                Debug.LogWarning($"TowerDefinition '{name}': {step}", this);
            }
        }

        /// <summary>
        /// Null when the blast caps per level make sense, otherwise what is wrong: only a splash gun has a cap, and an upgrade
        /// never makes a blast hurt fewer creatures (zero, the whole radius, counts as the most).
        /// </summary>
        public string SplashTargetsProblem()
        {
            for (int i = 0; _levels != null && i < _levels.Length; i++)
            {
                if (_levels[i].SplashTargets > 0 && _splashRadius <= 0f)
                {
                    return $"level {i + 1} caps a blast at {_levels[i].SplashTargets} creatures, but the tower has no splash radius.";
                }

                int before = i > 0 ? SplashTargetsAt(i - 1) : 0;
                int now = SplashTargetsAt(i);
                if (i > 0 && now > 0 && (before == 0 || now < before))
                {
                    return $"the blast hurts fewer creatures at level {i + 1} ({now}) than at level {i} ({(before == 0 ? "all" : before.ToString())}).";
                }
            }

            return null;
        }

        /// <summary>
        /// Null when every upgrade step has a name and changes the one thing it says (round 67), otherwise what is wrong.
        /// The blast's cap per level (SplashTargets) is not a step of its own: it grows with the stars beside the named thing
        /// and the tower menu lists it next to it (TowerStepText).
        /// A refit (gates, the base's turret) may change several things.
        /// </summary>
        public string StepProblem()
        {
            for (int i = 1; _levels != null && i < _levels.Length; i++)
            {
                TowerLevel from = _levels[i - 1];
                TowerLevel to = _levels[i];
                if (string.IsNullOrWhiteSpace(to.StepName) || to.Step == TowerStep.None)
                {
                    return $"the step to level {i + 1} has no name or does not say what it improves.";
                }

                if (to.Step == TowerStep.Refit)
                {
                    continue;
                }

                bool damage = !Mathf.Approximately(from.Damage, to.Damage);
                bool rate = !Mathf.Approximately(from.FireInterval, to.FireInterval);
                bool range = !Mathf.Approximately(from.Range, to.Range);
                bool slow = !Mathf.Approximately(from.SlowFactor, to.SlowFactor) || !Mathf.Approximately(from.SlowSeconds, to.SlowSeconds);
                bool support = !Mathf.Approximately(from.SupportScale, to.SupportScale);
                int changed = (damage ? 1 : 0) + (rate ? 1 : 0) + (range ? 1 : 0) + (slow ? 1 : 0) + (support ? 1 : 0) + (to.HealthBonus > 0f ? 1 : 0);
                bool named = (to.Step == TowerStep.Damage && damage) || (to.Step == TowerStep.FireRate && rate) || (to.Step == TowerStep.Range && range)
                    || (to.Step == TowerStep.Slow && slow) || (to.Step == TowerStep.Support && support);
                if (changed != 1 || !named)
                {
                    return $"the step '{to.StepName}' to level {i + 1} must improve its {to.Step} and nothing else (it changes {changed} things).";
                }
            }

            return null;
        }
    }
}


