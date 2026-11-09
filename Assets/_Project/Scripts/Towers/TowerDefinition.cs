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

        /// <summary>
        /// Share of the damage that lands on a target of each layer (a creature that is air only takes the air share). Ground
        /// guns that can also aim up do less up there, so the sky asks for an air gun (Docs/BALANCE.md, "Roles by enemy").
        /// </summary>
        [SerializeField] [Range(0f, 1f)] private float _groundDamageFactor = 1f;
        [SerializeField] [Range(0f, 1f)] private float _airDamageFactor = 1f;

        [Tooltip("Share of a hit that works on a creature's armour while it has any: one for a heavy shell that strips it point for point, a fraction for light fire that only chips it (round 24).")]
        [SerializeField] [Range(0.05f, 2f)] private float _armourFactor = 1f;

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

        /// <summary>The imported model per level; when set it replaces the procedural shape.</summary>
        [SerializeField] private TowerArtDefinition _art;
        [Tooltip("A gate's imported look (pylons, beam, panels, wreck); empty keeps the procedural gate.")]
        [SerializeField] private GateArtDefinition _gateArt;

        public string Id => name;
        public string DisplayName => _displayName;
        public TargetLayers Targets => _targets;
        public float ProjectileSpeed => _projectileSpeed;
        public float SplashRadius => _splashRadius;
        public int SplashTargets => _splashTargets;
        public float GroundDamageFactor => _groundDamageFactor;
        public float AirDamageFactor => _airDamageFactor;

        /// <summary>Share of a hit that works on armour while the creature has any (Health.TakeDamage); light fire sits well under one.</summary>
        public float ArmourFactor => _armourFactor;

        /// <summary>The damage share against a target of this layer: air only takes the air share, anything on the ground the ground share.</summary>
        public float DamageFactorAgainst(TargetLayers layer) => (layer & TargetLayers.Ground) != 0 ? _groundDamageFactor : _airDamageFactor;

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

            if (((_targets & TargetLayers.Ground) != 0 && _groundDamageFactor <= 0f) || ((_targets & TargetLayers.Air) != 0 && _airDamageFactor <= 0f))
            {
                Debug.LogWarning($"TowerDefinition '{name}' aims at a layer it deals no damage to: drop the layer from its targets instead.", this);
            }
        }
    }
}


