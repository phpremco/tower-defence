using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Recorded sound effects, a few variants per sound so a volley does not repeat one sample. Any list left empty
    /// falls back to the generated sound, so the game plays with or without the audio files.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Sfx Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip[] _pulseShots = new AudioClip[0];
        [SerializeField] private AudioClip[] _heavyShots = new AudioClip[0];
        [SerializeField] private AudioClip[] _rocketLaunches = new AudioClip[0];
        [SerializeField] private AudioClip[] _cryoShots = new AudioClip[0];
        [SerializeField] private AudioClip[] _teslaShots = new AudioClip[0];
        [SerializeField] private AudioClip[] _railShots = new AudioClip[0];
        [SerializeField] private AudioClip[] _explosions = new AudioClip[0];
        [SerializeField] private AudioClip[] _bigExplosions = new AudioClip[0];
        [SerializeField] private AudioClip[] _structureHits = new AudioClip[0];
        [SerializeField] private AudioClip[] _enemyDeaths = new AudioClip[0];
        [SerializeField] private AudioClip[] _baseHits = new AudioClip[0];
        [SerializeField] private AudioClip[] _heroBlasters = new AudioClip[0];

        /// <summary>Looping hum of a boss laser; only the first clip is used.</summary>
        [SerializeField] private AudioClip[] _bossLaserLoops = new AudioClip[0];

        /// <summary>The siren that opens a boss wave; only the first clip is used.</summary>
        [SerializeField] private AudioClip[] _bossAlarms = new AudioClip[0];

        public IReadOnlyList<AudioClip> PulseShots => _pulseShots;
        public IReadOnlyList<AudioClip> HeavyShots => _heavyShots;
        public IReadOnlyList<AudioClip> RocketLaunches => _rocketLaunches;
        public IReadOnlyList<AudioClip> CryoShots => _cryoShots;
        public IReadOnlyList<AudioClip> TeslaShots => _teslaShots;
        public IReadOnlyList<AudioClip> RailShots => _railShots;
        public IReadOnlyList<AudioClip> Explosions => _explosions;
        public IReadOnlyList<AudioClip> BigExplosions => _bigExplosions;
        public IReadOnlyList<AudioClip> StructureHits => _structureHits;
        public IReadOnlyList<AudioClip> EnemyDeaths => _enemyDeaths;
        public IReadOnlyList<AudioClip> BaseHits => _baseHits;
        public IReadOnlyList<AudioClip> HeroBlasters => _heroBlasters;
        public IReadOnlyList<AudioClip> BossLaserLoops => _bossLaserLoops;
        public IReadOnlyList<AudioClip> BossAlarms => _bossAlarms;
    }
}
