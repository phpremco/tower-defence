using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The recorded sound effects of the game, grouped per sound with their loudness gain (see SoundSet). The
    /// generator fills it from the audio folders and measures the gains; any set left empty falls back to the
    /// generated sound, so the game plays with or without the audio files. Creature voices live in their own
    /// assets, listed here so the sound board and the content tests can reach them.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Sfx Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Header("Tower shots")]
        [SerializeField] private SoundSet _pulseShots = new SoundSet();
        [SerializeField] private SoundSet _heavyShots = new SoundSet();
        [SerializeField] private SoundSet _rocketLaunches = new SoundSet();
        [SerializeField] private SoundSet _cryoShots = new SoundSet();
        [SerializeField] private SoundSet _teslaShots = new SoundSet();
        [SerializeField] private SoundSet _laserShots = new SoundSet();
        [SerializeField] private SoundSet _railShots = new SoundSet();

        [Header("Explosions")]
        [SerializeField] private SoundSet _cannonExplosions = new SoundSet();
        [SerializeField] private SoundSet _rocketExplosions = new SoundSet();
        [SerializeField] private SoundSet _mineExplosions = new SoundSet();
        [SerializeField] private SoundSet _gateExplosions = new SoundSet();
        [SerializeField] private SoundSet _baseExplosions = new SoundSet();
        [SerializeField] private SoundSet _towerBreaks = new SoundSet();

        [Header("Structures and base")]
        [SerializeField] private SoundSet _structureHits = new SoundSet();
        [SerializeField] private SoundSet _baseAlerts = new SoundSet();
        [SerializeField] private SoundSet _upgrades = new SoundSet();
        [SerializeField] private SoundSet _scrapPickups = new SoundSet();
        [SerializeField] private SoundSet _cash = new SoundSet();
        [SerializeField] private SoundSet _freezes = new SoundSet();
        [SerializeField] private SoundSet _acidSizzles = new SoundSet();
        [SerializeField] private SoundSet _steamBursts = new SoundSet();
        [SerializeField] private SoundSet _netShots = new SoundSet();
        [SerializeField] private SoundSet _earthquakes = new SoundSet();

        [Header("Path objects (loops)")]
        [SerializeField] private SoundSet _cartLoops = new SoundSet();
        [SerializeField] private SoundSet _fireballLoops = new SoundSet();

        [Header("Hero")]
        [SerializeField] private SoundSet _heroBlasters = new SoundSet();
        [SerializeField] private SoundSet _saberSwings = new SoundSet();
        [SerializeField] private SoundSet _saberHits = new SoundSet();
        [SerializeField] private SoundSet _saberHums = new SoundSet();
        [SerializeField] private SoundSet _heroPulses = new SoundSet();
        [SerializeField] private SoundSet _heroSteps = new SoundSet();
        [SerializeField] private SoundSet _heroDeaths = new SoundSet();
        [SerializeField] private SoundSet _heroFalls = new SoundSet();

        [Header("Drones (loops are quiet by design)")]
        [SerializeField] private SoundSet _droneFanLoops = new SoundSet();
        [SerializeField] private SoundSet _droneRepairLoops = new SoundSet();
        [SerializeField] private SoundSet _droneHarvestLoops = new SoundSet();
        [SerializeField] private SoundSet _droneLasers = new SoundSet();

        [Header("Creatures without a voice of their own")]
        [SerializeField] private SoundSet _enemyDeaths = new SoundSet();
        [SerializeField] private SoundSet _heavySteps = new SoundSet();
        [SerializeField] private SoundSet _wingLoops = new SoundSet();
        [SerializeField] private SoundSet _largeWingLoops = new SoundSet();

        [Header("Ambience layers")]
        [SerializeField] private SoundSet _waterLoops = new SoundSet();
        [SerializeField] private SoundSet _lavaLoops = new SoundSet();
        [SerializeField] private SoundSet _steamLoops = new SoundSet();
        [SerializeField] private SoundSet _leavesLoops = new SoundSet();
        [SerializeField] private SoundSet _icyWindLoops = new SoundSet();

        [Header("Boss wave")]
        [SerializeField] private SoundSet _bossLaserLoops = new SoundSet();
        [SerializeField] private SoundSet _bossAlarms = new SoundSet();
        [Tooltip("The drums that open every wave.")]
        [SerializeField] private SoundSet _waveDrums = new SoundSet();

        [Header("Music")]
        [Tooltip("Seamless loops: the menu and planet map, the level while ordinary waves run, and the level while a boss is on the field.")]
        [SerializeField] private SoundSet _menuMusic = new SoundSet();
        [SerializeField] private SoundSet _gameMusic = new SoundSet();
        [SerializeField] private SoundSet _bossMusic = new SoundSet();

        [SerializeField] private CreatureVoice[] _voices = new CreatureVoice[0];

        public SoundSet PulseShots => _pulseShots;
        public SoundSet HeavyShots => _heavyShots;
        public SoundSet RocketLaunches => _rocketLaunches;
        public SoundSet CryoShots => _cryoShots;
        public SoundSet TeslaShots => _teslaShots;
        public SoundSet LaserShots => _laserShots;
        public SoundSet RailShots => _railShots;
        public SoundSet CannonExplosions => _cannonExplosions;
        public SoundSet RocketExplosions => _rocketExplosions;
        public SoundSet MineExplosions => _mineExplosions;
        public SoundSet GateExplosions => _gateExplosions;
        public SoundSet BaseExplosions => _baseExplosions;
        public SoundSet TowerBreaks => _towerBreaks;
        public SoundSet StructureHits => _structureHits;
        public SoundSet BaseAlerts => _baseAlerts;
        public SoundSet Upgrades => _upgrades;
        public SoundSet ScrapPickups => _scrapPickups;
        public SoundSet Cash => _cash;
        public SoundSet Freezes => _freezes;
        public SoundSet AcidSizzles => _acidSizzles;
        public SoundSet SteamBursts => _steamBursts;
        public SoundSet NetShots => _netShots;
        public SoundSet Earthquakes => _earthquakes;
        public SoundSet CartLoops => _cartLoops;
        public SoundSet FireballLoops => _fireballLoops;
        public SoundSet HeroBlasters => _heroBlasters;
        public SoundSet SaberSwings => _saberSwings;
        public SoundSet SaberHits => _saberHits;
        public SoundSet SaberHums => _saberHums;
        public SoundSet HeroPulses => _heroPulses;
        public SoundSet HeroSteps => _heroSteps;
        public SoundSet HeroDeaths => _heroDeaths;
        public SoundSet HeroFalls => _heroFalls;
        public SoundSet DroneFanLoops => _droneFanLoops;
        public SoundSet DroneRepairLoops => _droneRepairLoops;
        public SoundSet DroneHarvestLoops => _droneHarvestLoops;
        public SoundSet DroneLasers => _droneLasers;
        public SoundSet EnemyDeaths => _enemyDeaths;
        public SoundSet HeavySteps => _heavySteps;
        public SoundSet WingLoops => _wingLoops;
        public SoundSet LargeWingLoops => _largeWingLoops;
        public SoundSet WaterLoops => _waterLoops;
        public SoundSet LavaLoops => _lavaLoops;
        public SoundSet SteamLoops => _steamLoops;
        public SoundSet LeavesLoops => _leavesLoops;
        public SoundSet IcyWindLoops => _icyWindLoops;
        public SoundSet BossLaserLoops => _bossLaserLoops;
        public SoundSet BossAlarms => _bossAlarms;
        public SoundSet WaveDrums => _waveDrums;
        public SoundSet MenuMusic => _menuMusic;
        public SoundSet GameMusic => _gameMusic;
        public SoundSet BossMusic => _bossMusic;
        public CreatureVoice[] Voices => _voices;

        /// <summary>Every set with its label, for the sound board and the content tests.</summary>
        public (string Label, SoundSet Set)[] Sets() => new[]
        {
            ("Pulse shot", _pulseShots), ("Heavy shot", _heavyShots), ("Rocket launch", _rocketLaunches), ("Cryo shot", _cryoShots),
            ("Tesla shot", _teslaShots), ("Laser shot", _laserShots), ("Rail shot", _railShots), ("Cannon explosion", _cannonExplosions),
            ("Rocket explosion", _rocketExplosions), ("Mine explosion", _mineExplosions), ("Gate explosion", _gateExplosions),
            ("Base explosion", _baseExplosions), ("Tower break", _towerBreaks), ("Structure hit", _structureHits),
            ("Base alert", _baseAlerts), ("Upgrade", _upgrades), ("Scrap pickup", _scrapPickups), ("Cash", _cash),
            ("Freeze", _freezes), ("Acid sizzle", _acidSizzles), ("Steam burst", _steamBursts), ("Net shot", _netShots),
            ("Earthquake", _earthquakes), ("Cart loop", _cartLoops), ("Fireball loop", _fireballLoops),
            ("Hero blaster", _heroBlasters), ("Saber swing", _saberSwings), ("Saber hit", _saberHits), ("Saber hum", _saberHums),
            ("Hero pulse", _heroPulses), ("Hero step", _heroSteps), ("Hero death", _heroDeaths), ("Hero fall", _heroFalls),
            ("Drone fan", _droneFanLoops), ("Drone repair", _droneRepairLoops), ("Drone harvest", _droneHarvestLoops),
            ("Drone laser", _droneLasers), ("Enemy death", _enemyDeaths), ("Heavy steps", _heavySteps), ("Wings", _wingLoops),
            ("Large wings", _largeWingLoops), ("Water", _waterLoops), ("Lava", _lavaLoops), ("Steam", _steamLoops),
            ("Leaves", _leavesLoops), ("Icy wind", _icyWindLoops), ("Boss laser", _bossLaserLoops), ("Boss alarm", _bossAlarms), ("Wave drums", _waveDrums),
            ("Menu music", _menuMusic), ("Game music", _gameMusic), ("Boss music", _bossMusic),
        };
    }
}
