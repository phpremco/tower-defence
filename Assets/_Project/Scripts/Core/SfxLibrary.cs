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
        [SerializeField] private SoundSet _railShots = new SoundSet();
        [SerializeField] private SoundSet _flakShots = new SoundSet();

        [Header("Explosions")]
        [SerializeField] private SoundSet _cannonExplosions = new SoundSet();
        [SerializeField] private SoundSet _rocketExplosions = new SoundSet();
        [SerializeField] private SoundSet _flakBursts = new SoundSet();
        [SerializeField] private SoundSet _mineExplosions = new SoundSet();
        [SerializeField] private SoundSet _gateExplosions = new SoundSet();
        [SerializeField] private SoundSet _baseExplosions = new SoundSet();
        [SerializeField] private SoundSet _towerBreaks = new SoundSet();

        [Header("Structures and base")]
        [SerializeField] private SoundSet _structureHits = new SoundSet();
        [Tooltip("A boss smashing a crystal, plant or rock off the road.")]
        [SerializeField] private SoundSet _decorSmashes = new SoundSet();
        [SerializeField] private SoundSet _baseAlerts = new SoundSet();
        [SerializeField] private SoundSet _upgrades = new SoundSet();
        [SerializeField] private SoundSet _scrapPickups = new SoundSet();
        [SerializeField] private SoundSet _cash = new SoundSet();
        [SerializeField] private SoundSet _freezes = new SoundSet();
        [SerializeField] private SoundSet _acidSizzles = new SoundSet();
        [SerializeField] private SoundSet _steamBursts = new SoundSet();
        [SerializeField] private SoundSet _earthquakes = new SoundSet();
        [SerializeField] private SoundSet _distantBlasts = new SoundSet();

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

        [Header("HUD")]
        [Tooltip("The radar's ping when a surprise comes out of a spawn point (round 88a).")]
        [SerializeField] private SoundSet _radarPings = new SoundSet();

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
        [SerializeField] private SoundSet _riftLoops = new SoundSet();
        [SerializeField] private SoundSet _steamLoops = new SoundSet();
        [SerializeField] private SoundSet _leavesLoops = new SoundSet();
        [SerializeField] private SoundSet _icyWindLoops = new SoundSet();

        [Header("Boss wave")]
        [SerializeField] private SoundSet _bossLaserLoops = new SoundSet();
        [SerializeField] private SoundSet _beamLoops = new SoundSet();
        [SerializeField] private SoundSet _bossAlarms = new SoundSet();
        [Tooltip("The drums that open every wave.")]
        [SerializeField] private SoundSet _waveDrums = new SoundSet();

        [Header("Music")]
        [Tooltip("Seamless loops: the menu and planet map, the level while ordinary waves run, and the level while a boss is on the field.")]
        [SerializeField] private SoundSet _menuMusic = new SoundSet();
        [SerializeField] private SoundSet _gameMusic = new SoundSet();
        [SerializeField] private SoundSet _bossMusic = new SoundSet();

        [SerializeField] private CreatureVoice[] _voices = new CreatureVoice[0];

        [Header("Dialogue")]
        [Tooltip("The dialogue box's opener, typing cadence and default blips (the speakers carry their own blips).")]
        [SerializeField] private DialogueSounds _dialogue;

        public SoundSet PulseShots => _pulseShots;
        public SoundSet HeavyShots => _heavyShots;
        public SoundSet RocketLaunches => _rocketLaunches;
        public SoundSet CryoShots => _cryoShots;
        public SoundSet TeslaShots => _teslaShots;
        public SoundSet RailShots => _railShots;
        public SoundSet FlakShots => _flakShots;
        public SoundSet FlakBursts => _flakBursts;
        public SoundSet CannonExplosions => _cannonExplosions;
        public SoundSet RocketExplosions => _rocketExplosions;
        public SoundSet MineExplosions => _mineExplosions;
        public SoundSet GateExplosions => _gateExplosions;
        public SoundSet BaseExplosions => _baseExplosions;
        public SoundSet TowerBreaks => _towerBreaks;
        public SoundSet StructureHits => _structureHits;
        public SoundSet DecorSmashes => _decorSmashes;
        public SoundSet BaseAlerts => _baseAlerts;
        public SoundSet Upgrades => _upgrades;
        public SoundSet ScrapPickups => _scrapPickups;
        public SoundSet Cash => _cash;
        public SoundSet Freezes => _freezes;
        public SoundSet AcidSizzles => _acidSizzles;
        public SoundSet SteamBursts => _steamBursts;
        public SoundSet Earthquakes => _earthquakes;
        public SoundSet DistantBlasts => _distantBlasts;
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
        public SoundSet RadarPings => _radarPings;
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
        public SoundSet RiftLoops => _riftLoops;
        public SoundSet SteamLoops => _steamLoops;
        public SoundSet LeavesLoops => _leavesLoops;
        public SoundSet IcyWindLoops => _icyWindLoops;
        public SoundSet BossLaserLoops => _bossLaserLoops;

        /// <summary>The hum a beam tower holds for as long as it fires.</summary>
        public SoundSet BeamLoops => _beamLoops;
        public SoundSet BossAlarms => _bossAlarms;
        public SoundSet WaveDrums => _waveDrums;
        public SoundSet MenuMusic => _menuMusic;
        public SoundSet GameMusic => _gameMusic;
        public SoundSet BossMusic => _bossMusic;
        public CreatureVoice[] Voices => _voices;
        public DialogueSounds Dialogue => _dialogue;

        /// <summary>Every set with its label, for the sound board and the content tests.</summary>
        public (string Label, SoundSet Set)[] Sets() => new[]
        {
            (Loc.Get("sound_set.pulse_shot"), _pulseShots), (Loc.Get("sound_set.heavy_shot"), _heavyShots), (Loc.Get("sound_set.rocket_launch"), _rocketLaunches), (Loc.Get("sound_set.cryo_shot"), _cryoShots),
            (Loc.Get("sound_set.tesla_shot"), _teslaShots), (Loc.Get("sound_set.rail_shot"), _railShots), (Loc.Get("sound_set.flak_shot"), _flakShots), (Loc.Get("sound_set.flak_burst"), _flakBursts), (Loc.Get("sound_set.cannon_explosion"), _cannonExplosions),
            (Loc.Get("sound_set.rocket_explosion"), _rocketExplosions), (Loc.Get("sound_set.mine_explosion"), _mineExplosions), (Loc.Get("sound_set.gate_explosion"), _gateExplosions),
            (Loc.Get("sound_set.base_explosion"), _baseExplosions), (Loc.Get("sound_set.tower_break"), _towerBreaks), (Loc.Get("sound_set.structure_hit"), _structureHits), (Loc.Get("sound_set.decor_smash"), _decorSmashes),
            (Loc.Get("sound_set.base_alert"), _baseAlerts), (Loc.Get("sound_set.upgrade"), _upgrades), (Loc.Get("sound_set.scrap_pickup"), _scrapPickups), (Loc.Get("sound_set.cash"), _cash),
            (Loc.Get("sound_set.freeze"), _freezes), (Loc.Get("sound_set.acid_sizzle"), _acidSizzles), (Loc.Get("sound_set.steam_burst"), _steamBursts),
            (Loc.Get("sound_set.earthquake"), _earthquakes), (Loc.Get("sound_set.distant_blast"), _distantBlasts), (Loc.Get("sound_set.cart_loop"), _cartLoops), (Loc.Get("sound_set.fireball_loop"), _fireballLoops),
            (Loc.Get("sound_set.hero_blaster"), _heroBlasters), (Loc.Get("sound_set.saber_swing"), _saberSwings), (Loc.Get("sound_set.saber_hit"), _saberHits), (Loc.Get("sound_set.saber_hum"), _saberHums),
            (Loc.Get("sound_set.hero_pulse"), _heroPulses), (Loc.Get("sound_set.hero_step"), _heroSteps), (Loc.Get("sound_set.hero_death"), _heroDeaths), (Loc.Get("sound_set.hero_fall"), _heroFalls),
            (Loc.Get("sound_set.drone_fan"), _droneFanLoops), (Loc.Get("sound_set.drone_repair"), _droneRepairLoops), (Loc.Get("sound_set.drone_harvest"), _droneHarvestLoops),
            (Loc.Get("sound_set.drone_laser"), _droneLasers), (Loc.Get("sound_set.enemy_death"), _enemyDeaths), (Loc.Get("sound_set.heavy_steps"), _heavySteps), (Loc.Get("sound_set.wings"), _wingLoops),
            (Loc.Get("sound_set.large_wings"), _largeWingLoops), (Loc.Get("sound_set.water"), _waterLoops), (Loc.Get("sound_set.lava"), _lavaLoops), (Loc.Get("sound_set.rift"), _riftLoops), (Loc.Get("sound_set.steam"), _steamLoops),
            (Loc.Get("sound_set.leaves"), _leavesLoops), (Loc.Get("sound_set.icy_wind"), _icyWindLoops), (Loc.Get("sound_set.boss_laser"), _bossLaserLoops), (Loc.Get("sound_set.beam_loop"), _beamLoops), (Loc.Get("sound_set.boss_alarm"), _bossAlarms), (Loc.Get("sound_set.wave_drums"), _waveDrums),
            (Loc.Get("sound_set.radar_ping"), _radarPings), (Loc.Get("sound_set.menu_music"), _menuMusic), (Loc.Get("sound_set.game_music"), _gameMusic), (Loc.Get("sound_set.boss_music"), _bossMusic),
        };
    }
}
