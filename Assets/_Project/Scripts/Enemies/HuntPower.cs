using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The numbers of a boss that hunts one kind of tower off the road (the Cinder Matriarch, Docs/BOSSES.md "Hatred of
    /// the Cold"): how often it sets out, how its heat line warns, its trail of lava, and the brood it lays in the pools.
    /// </summary>
    [Serializable]
    public sealed class HuntPower
    {
        [Tooltip("Seconds it walks the road after a smash before it sets out again: one tower per approach.")]
        [SerializeField] [Min(0f)] private float _cooldownSeconds = 14f;
        [Tooltip("Seconds the heat line takes to run from it to the hunted tower.")]
        [SerializeField] [Min(0.2f)] private float _heatLineSeconds = 1.5f;
        [Tooltip("Speed factor of its hunting stride off the road.")]
        [SerializeField] [Min(0.5f)] private float _huntPace = 1.25f;
        [Tooltip("A lava pool is a shortcut when it lets it come up at least this much nearer its target (world units).")]
        [SerializeField] [Min(0f)] private float _diveGain = 4f;
        [Tooltip("Seconds a smashed tower is out until the player has rebuilt it, for the balance ledger.")]
        [SerializeField] [Min(0f)] private float _rebuildSeconds = 12f;

        [Header("Lava trail")]
        [Tooltip("Seconds a patch of its trail glows and burns.")]
        [SerializeField] [Min(0f)] private float _trailSeconds = 6f;
        [SerializeField] [Min(0.1f)] private float _trailSpacing = 0.6f;
        [SerializeField] [Min(0.1f)] private float _trailRadius = 0.7f;
        [Tooltip("World units behind the pivot where the trail drips from its sac.")]
        [SerializeField] [Min(0f)] private float _trailBehind = 1.6f;
        [Tooltip("Damage per second to a structure a patch touches.")]
        [SerializeField] [Min(0f)] private float _towerBurn = 14f;
        [Tooltip("Damage per second to a walker on a patch: less than to the towers.")]
        [SerializeField] [Min(0f)] private float _walkerBurn = 4f;

        [Header("Brood")]
        [Tooltip("The phase from which it lays eggs (Brood, below half its health).")]
        [SerializeField] [Min(0)] private int _broodPhase = 1;
        [SerializeField] private EnemyDefinition _egg;
        [SerializeField] private EnemyDefinition _hatchling;
        [SerializeField] [Min(1)] private int _eggsPerClutch = 3;
        [SerializeField] [Min(1)] private int _hatchlingsPerEgg = 2;
        [Tooltip("Seconds until an egg nobody shot hatches.")]
        [SerializeField] [Min(1f)] private float _hatchSeconds = 10f;
        [SerializeField] [Min(0)] private int _clutches = 2;
        [Tooltip("Seconds between two clutches.")]
        [SerializeField] [Min(1f)] private float _clutchInterval = 30f;
        [Tooltip("World units behind the pivot where the eggs leave its sac (README round 28: the tip is ~2.8 model units back).")]
        [SerializeField] [Min(0f)] private float _sacBehind = 2.2f;

        public float CooldownSeconds => _cooldownSeconds;
        public float HeatLineSeconds => _heatLineSeconds;
        public float HuntPace => _huntPace;
        public float DiveGain => _diveGain;
        public float RebuildSeconds => _rebuildSeconds;
        public float TrailSeconds => _trailSeconds;
        public float TrailSpacing => _trailSpacing;
        public float TrailRadius => _trailRadius;
        public float TrailBehind => _trailBehind;
        public float TowerBurn => _towerBurn;
        public float WalkerBurn => _walkerBurn;
        public int BroodPhase => _broodPhase;
        public EnemyDefinition Egg => _egg;
        public EnemyDefinition Hatchling => _hatchling;
        public int EggsPerClutch => _eggsPerClutch;
        public int HatchlingsPerEgg => _hatchlingsPerEgg;
        public float HatchSeconds => _hatchSeconds;
        public int Clutches => _clutches;
        public float ClutchInterval => _clutchInterval;
        public float SacBehind => _sacBehind;
    }
}
