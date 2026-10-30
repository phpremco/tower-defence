using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// Plays the generated sound effects for what happens in a level: shots in character with the weapon, kills,
    /// explosions, creatures climbing out of pools, boss powers, hits on the base and the player's own actions.
    /// Keeps the field from turning into noise: each kind of filler sound has a budget of voices, shots that land
    /// within a few milliseconds of each other bundle into one, big moments duck the filler, and the further the
    /// camera is zoomed out the quieter and duller the filler gets. Lasers hum as a loop instead of stuttering.
    /// Listens to events only; gameplay does not know it exists.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        private enum Kind
        {
            Shot = 0,
            Kill = 1,
            Hit = 2,
            Spawn = 3,
            Priority = 4,
        }

        private const int VoiceCount = 18;
        private const float PitchJitter = 0.08f;
        private const float ShotVolume = 0.12f;
        private const float HeavyShotVolume = 0.2f;
        private const float KillVolume = 0.65f;
        private const float HitVolume = 0.45f;
        private const float SpawnVolume = 0.6f;
        private const float HeavySplashRadius = 1f;
        private const float BundleSeconds = 0.06f;
        // Rapid fire is heard as a volley rather than as every bullet: while a rapid shot sound is on cooldown, the
        // same sound only nudges the voice already playing. Slow guns keep a sound per shot.
        private const float RapidCooldownSeconds = 0.22f;
        private const float BundleBoost = 1.18f;
        private const float BundleCap = 1.2f;
        private const float DuckSeconds = 0.6f;
        private const float DuckDepth = 0.45f;
        private const float FarGain = 0.6f;
        private const float FarCutoffHz = 2600f;
        private const float NearCutoffHz = 22000f;
        private const float MaxZoom = 3f;
        private const float LaserHoldSeconds = 0.25f;
        private const float LaserVolume = 0.15f;
        private const float LaserFadeSeconds = 0.15f;
        private const float BossLaserVolume = 0.6f;
        private const float ApproachVolume = 0.8f;
        private const float ApproachFirstDelay = 2.5f;
        private const float ApproachInterval = 2.2f;
        private const float ApproachMaxSeconds = 40f;
        private const float KillMaxSeconds = 0.7f;
        private const float CutFadeSeconds = 0.12f;
        private static readonly int[] Budgets = { 3, 2, 2, 2, VoiceCount };

        private AudioSource[] _voices;
        private AudioLowPassFilter[] _filters;
        private Kind[] _voiceKinds;
        private float[] _voiceEnds;
        private float[] _voiceVolumes;
        private float[] _voiceStarts;
        private AudioSource _laserLoop;
        private AudioSource _bossLaserLoop;
        private float _laserUntil = float.NegativeInfinity;
        private float _duckUntil = float.NegativeInfinity;
        private float _approachUntil = float.NegativeInfinity;
        private float _nextApproach = float.NegativeInfinity;
        private AudioClip _approach;
        private readonly Dictionary<Family, float> _shotTimes = new Dictionary<Family, float>();
        private readonly Dictionary<Family, int> _shotVoices = new Dictionary<Family, int>();
        private int _bossLasers;
        private int _droneCount;
        private Family _pulse;
        private Family _heavy;
        private Family _rocket;
        private Family _cryo;
        private AudioClip _laser;
        private Family _tesla;
        private Family _rail;
        private Family _explosion;
        private Family _bigExplosion;
        private Family _death;
        private AudioClip _bossDeath;
        private AudioClip _bossRoar;
        private Family _baseHit;
        private AudioClip _horn;
        private AudioClip _build;
        private AudioClip _upgrade;
        private AudioClip _sell;
        private AudioClip _repair;
        private AudioClip _strike;
        private Family _blaster;
        private AudioClip _heroDown;
        private AudioClip _heroRevive;
        private AudioClip _shockwave;
        private AudioClip _drone;
        private Family _structureHit;
        private AudioClip _baseExplosion;
        private AudioClip _bossPulse;
        private AudioClip _minionBurst;
        private AudioClip _rollerCurl;
        private AudioClip _emergeWater;
        private AudioClip _emergeLava;
        private AudioClip _emergeSand;
        private AudioClip _emergeGround;
        private LevelContext _context;
        private AudioMix _mix;
        private FixedCameraRig _rig;
        private float _lastBaseHealth;

        public void Initialize(LevelContext context, AudioMix mix, FixedCameraRig rig)
        {
            _context = context;
            _mix = mix;
            _rig = rig;
            _voices = new AudioSource[VoiceCount];
            _filters = new AudioLowPassFilter[VoiceCount];
            _voiceKinds = new Kind[VoiceCount];
            _voiceEnds = new float[VoiceCount];
            _voiceVolumes = new float[VoiceCount];
            _voiceStarts = new float[VoiceCount];
            // A filter works on everything on its object, so every voice gets an object of its own.
            for (int i = 0; i < VoiceCount; i++)
            {
                var voice = new GameObject($"Voice{i}");
                voice.transform.SetParent(transform, false);
                _voices[i] = voice.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].spatialBlend = 0f;
                _filters[i] = voice.AddComponent<AudioLowPassFilter>();
                _filters[i].cutoffFrequency = NearCutoffHz;
                _voiceEnds[i] = float.NegativeInfinity;
            }

            _laserLoop = Loop(ProceduralSfx.LaserHum());
            // Recorded clips when the library has them, the generated sound otherwise.
            Services.TryGet(out SfxLibrary library);
            _bossLaserLoop = Loop(library != null && library.BossLaserLoops.Count > 0 ? library.BossLaserLoops[0] : ProceduralSfx.BossLaserHum());

            _pulse = new Family(ProceduralSfx.PulseShot(), library?.PulseShots);
            _heavy = new Family(ProceduralSfx.HeavyShot(), library?.HeavyShots);
            _rocket = new Family(ProceduralSfx.RocketLaunch(), library?.RocketLaunches);
            _cryo = new Family(ProceduralSfx.CryoShot(), library?.CryoShots);
            _laser = ProceduralSfx.LaserShot();
            _tesla = new Family(ProceduralSfx.TeslaShot(), library?.TeslaShots);
            _rail = new Family(ProceduralSfx.RailShot(), library?.RailShots);
            _explosion = new Family(ProceduralSfx.Explosion(), library?.Explosions);
            _bigExplosion = new Family(ProceduralSfx.BigExplosion(), library?.BigExplosions);
            _death = new Family(ProceduralSfx.EnemyDeath(), library?.EnemyDeaths);
            _bossDeath = ProceduralSfx.BossDeath();
            _bossRoar = ProceduralSfx.BossRoar();
            _baseHit = new Family(ProceduralSfx.BaseHit(), library?.BaseHits);
            _horn = ProceduralSfx.WaveHorn();
            _build = ProceduralSfx.Build();
            _upgrade = ProceduralSfx.Upgrade();
            _sell = ProceduralSfx.Sell();
            _repair = ProceduralSfx.Repair();
            _strike = ProceduralSfx.HeroStrike();
            _blaster = new Family(ProceduralSfx.HeroBlaster(), library?.HeroBlasters);
            _heroDown = ProceduralSfx.HeroDown();
            _heroRevive = ProceduralSfx.HeroRevive();
            _shockwave = ProceduralSfx.Shockwave();
            _drone = ProceduralSfx.DroneBuy();
            _structureHit = new Family(ProceduralSfx.StructureHit(), library?.StructureHits);
            _baseExplosion = ProceduralSfx.BaseExplosion();
            _bossPulse = ProceduralSfx.BossPulse();
            _minionBurst = ProceduralSfx.MinionBurst();
            _rollerCurl = ProceduralSfx.RollerCurl();
            _emergeWater = ProceduralSfx.EmergeWater();
            _emergeLava = ProceduralSfx.EmergeLava();
            _emergeSand = ProceduralSfx.EmergeSand();
            _emergeGround = ProceduralSfx.EmergeGround();
            _approach = ApproachClipFor(context.Level);

            _lastBaseHealth = context.BaseHealth.Current;
            _droneCount = TotalDrones();
            context.Projectiles.Shot += OnShot;
            context.Projectiles.Exploded += OnExploded;
            context.EnemyKilled.Subscribe(OnKilled);
            context.EnemySpawned.Subscribe(OnSpawned);
            context.StructureHit.Subscribe(OnStructureHit);
            context.StructureWrecked.Subscribe(OnStructureWrecked);
            context.BaseHealth.Changed += OnBaseChanged;
            context.BaseHealth.Died += OnBaseDied;
            context.WaveStarted.Subscribe(OnWaveStarted);
            context.TowerBuilt.Subscribe(OnBuilt);
            context.PathObjectPlaced.Subscribe(OnBuilt);
            context.TowerUpgraded.Subscribe(OnUpgraded);
            context.TowerRepaired.Subscribe(OnRepaired);
            context.TowerSold.Subscribe(OnSold);
            context.DronesChanged.Subscribe(OnDronesChanged);
            context.BossPulsed.Subscribe(OnBossPulsed);
            context.BossLaserChanged.Subscribe(OnBossLaser);
            context.BossMinionsReleased.Subscribe(OnMinions);
            context.BossVolleyed.Subscribe(OnBossPulsed);
            context.BossRallied.Subscribe(OnBossRallied);
            context.BossWaveStarted.Subscribe(OnBossWave);
            if (context.Hero != null)
            {
                context.Hero.Struck += OnStruck;
                context.Hero.ShockwaveReleased += OnShockwave;
                context.Hero.Downed += OnHeroDown;
                context.Hero.Revived += OnHeroRevive;
            }
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Projectiles.Shot -= OnShot;
            _context.Projectiles.Exploded -= OnExploded;
            _context.EnemyKilled.Unsubscribe(OnKilled);
            _context.EnemySpawned.Unsubscribe(OnSpawned);
            _context.StructureHit.Unsubscribe(OnStructureHit);
            _context.StructureWrecked.Unsubscribe(OnStructureWrecked);
            _context.BaseHealth.Changed -= OnBaseChanged;
            _context.BaseHealth.Died -= OnBaseDied;
            _context.WaveStarted.Unsubscribe(OnWaveStarted);
            _context.TowerBuilt.Unsubscribe(OnBuilt);
            _context.PathObjectPlaced.Unsubscribe(OnBuilt);
            _context.TowerUpgraded.Unsubscribe(OnUpgraded);
            _context.TowerRepaired.Unsubscribe(OnRepaired);
            _context.TowerSold.Unsubscribe(OnSold);
            _context.DronesChanged.Unsubscribe(OnDronesChanged);
            _context.BossPulsed.Unsubscribe(OnBossPulsed);
            _context.BossLaserChanged.Unsubscribe(OnBossLaser);
            _context.BossMinionsReleased.Unsubscribe(OnMinions);
            _context.BossVolleyed.Unsubscribe(OnBossPulsed);
            _context.BossRallied.Unsubscribe(OnBossRallied);
            _context.BossWaveStarted.Unsubscribe(OnBossWave);
            if (_context.Hero != null)
            {
                _context.Hero.Struck -= OnStruck;
                _context.Hero.ShockwaveReleased -= OnShockwave;
                _context.Hero.Downed -= OnHeroDown;
                _context.Hero.Revived -= OnHeroRevive;
            }
        }

        private AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.Play();
            return source;
        }

        // Filler voices follow the duck, the zoom and the mix every frame; the loops fade in and out.
        private void Update()
        {
            float now = Time.unscaledTime;
            UpdateApproach(now);
            float zoom = Mathf.Clamp01((_rig.Zoom - 1f) / (MaxZoom - 1f));
            float distance = Mathf.Lerp(FarGain, 1f, zoom);
            float cutoff = Mathf.Lerp(FarCutoffHz, NearCutoffHz, zoom);
            float duck = now < _duckUntil ? DuckDepth : 1f;
            float effects = _mix.EffectsGain;

            for (int i = 0; i < VoiceCount; i++)
            {
                if (now >= _voiceEnds[i])
                {
                    // A voice past its end that is still sounding was cut short on purpose: fade it out and stop it.
                    if (_voices[i].isPlaying)
                    {
                        _voices[i].volume = Mathf.MoveTowards(_voices[i].volume, 0f, Time.unscaledDeltaTime / CutFadeSeconds);
                        if (_voices[i].volume <= 0f)
                        {
                            _voices[i].Stop();
                        }
                    }

                    continue;
                }

                bool filler = _voiceKinds[i] != Kind.Priority;
                _voices[i].volume = _voiceVolumes[i] * effects * (filler ? distance * duck : 1f);
                _filters[i].cutoffFrequency = filler ? cutoff : NearCutoffHz;
            }

            float dt = Time.unscaledDeltaTime / LaserFadeSeconds;
            _laserLoop.volume = Mathf.MoveTowards(_laserLoop.volume, now < _laserUntil ? LaserVolume * effects * distance * duck : 0f, dt);
            _bossLaserLoop.volume = Mathf.MoveTowards(_bossLaserLoop.volume, _bossLasers > 0 ? BossLaserVolume * effects : 0f, dt);
        }

        // The weapon speaks through its shot: chilling shots ring, splash shots thump, flak at the sky whooshes.
        // Lasers keep a hum going instead of a click per tick.
        private void OnShot(ShotInfo shot)
        {
            if (shot.Style == ProjectileStyle.Laser)
            {
                _laserUntil = Time.unscaledTime + LaserHoldSeconds;
                return;
            }

            if (shot.Style == ProjectileStyle.Lightning)
            {
                Shot(_tesla, ShotVolume, RapidCooldownSeconds);
            }
            else if (shot.Style == ProjectileStyle.Rail)
            {
                Shot(_rail, HeavyShotVolume, BundleSeconds);
            }
            else if (shot.Slows)
            {
                Shot(_cryo, ShotVolume, RapidCooldownSeconds);
            }
            else if (shot.SplashRadius > 0f && shot.Targets == TargetLayers.Air)
            {
                Shot(_rocket, HeavyShotVolume, BundleSeconds);
            }
            else if (shot.SplashRadius > 0f)
            {
                Shot(_heavy, HeavyShotVolume, BundleSeconds);
            }
            else
            {
                Shot(_pulse, ShotVolume, RapidCooldownSeconds);
            }
        }

        // The same shot again within its cooldown plays once, a little louder, instead of piling up: a few milliseconds
        // for heavy guns so a volley lands as one, a fifth of a second for rapid fire so a row of rifles sounds like one burst.
        private void Shot(Family family, float volume, float cooldown)
        {
            float now = Time.unscaledTime;
            if (_shotTimes.TryGetValue(family, out float last) && now - last < cooldown && _shotVoices.TryGetValue(family, out int voice) && _voiceKinds[voice] == Kind.Shot && _voiceEnds[voice] > now)
            {
                _voiceVolumes[voice] = Mathf.Min(volume * BundleCap, _voiceVolumes[voice] * BundleBoost);
                return;
            }

            _shotTimes[family] = now;
            _shotVoices[family] = Play(family.Next(), volume, Kind.Shot);
        }

        // One sound of the game with its recorded variants, handed out in turn, or the generated clip when there are none.
        private sealed class Family
        {
            private readonly AudioClip _fallback;
            private readonly IReadOnlyList<AudioClip> _variants;
            private int _next;

            public Family(AudioClip fallback, IReadOnlyList<AudioClip> variants)
            {
                _fallback = fallback;
                _variants = variants;
            }

            public AudioClip Next()
            {
                if (_variants == null || _variants.Count == 0)
                {
                    return _fallback;
                }

                AudioClip clip = _variants[_next % _variants.Count];
                _next++;
                return clip != null ? clip : _fallback;
            }
        }

        private void OnKilled(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                Play(_bossDeath, 1f, Kind.Priority);
                return;
            }

            Play(_death.Next(), KillVolume, Kind.Kill);
        }

        // From the alarm until the boss shows itself, its entrance gives it away: growls from the gate or the ground,
        // bubbles from a pond, lava churning, sand pouring. The player can tell where to look before it is there.
        private void OnBossWave()
        {
            _approachUntil = Time.unscaledTime + ApproachMaxSeconds;
            _nextApproach = Time.unscaledTime + ApproachFirstDelay;
        }

        private void UpdateApproach(float now)
        {
            if (now >= _approachUntil || now < _nextApproach || _context.BossesAlive > 0 || _context.Paused)
            {
                return;
            }

            Play(_approach, ApproachVolume, Kind.Priority);
            _nextApproach = now + ApproachInterval;
        }

        private static AudioClip ApproachClipFor(LevelDefinition level)
        {
            BossEntrance entrance = level.BossEntrance;
            if (entrance.Kind != EntranceKind.Pool)
            {
                return ProceduralSfx.BossGrowl();
            }

            switch (level.Terrain.KindAt(entrance.Cell))
            {
                case TerrainKind.Lava:
                    return ProceduralSfx.BossLavaChurn();
                case TerrainKind.Quicksand:
                    return ProceduralSfx.BossSandShift();
                default:
                    return ProceduralSfx.BossBubbles();
            }
        }

        // Bosses roar; creatures from a pool or the ground splash and crack their way out; rollers clack as they curl.
        private void OnSpawned(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                Play(_bossRoar, 1f, Kind.Priority);
                _approachUntil = float.NegativeInfinity;
            }

            switch (enemy.Origin)
            {
                case EnemyOrigin.Water:
                    Play(_emergeWater, SpawnVolume, enemy.Definition.IsBoss ? Kind.Priority : Kind.Spawn);
                    break;
                case EnemyOrigin.Lava:
                    Play(_emergeLava, SpawnVolume, enemy.Definition.IsBoss ? Kind.Priority : Kind.Spawn);
                    break;
                case EnemyOrigin.Quicksand:
                    Play(_emergeSand, SpawnVolume, enemy.Definition.IsBoss ? Kind.Priority : Kind.Spawn);
                    break;
                case EnemyOrigin.Ground:
                    Play(_emergeGround, 1f, Kind.Priority);
                    break;
                case EnemyOrigin.Nest:
                    Play(NestClipFor(enemy.Definition.Element), SpawnVolume, Kind.Spawn);
                    break;
            }

            if (enemy.Definition.Rolls)
            {
                // Pooled enemies fire Initialized again on reuse; the handler is per instance, so it is added once.
                enemy.RollChanged -= OnRollChanged;
                enemy.RollChanged += OnRollChanged;
            }
        }

        private void OnRollChanged(bool rolling)
        {
            if (rolling)
            {
                Play(_rollerCurl, SpawnVolume, Kind.Spawn);
            }
        }

        private void OnStructureHit(Vector3 position) => Play(_structureHit.Next(), HitVolume, Kind.Hit);

        private void OnStructureWrecked(Vector3 position) => Play(_bigExplosion.Next(), 1f, Kind.Priority);

        private void OnExploded(Vector3 position, float radius) =>
            Play((radius >= HeavySplashRadius ? _bigExplosion : _explosion).Next(), Mathf.Clamp(0.5f + radius * 0.25f, 0.5f, 1f), radius >= HeavySplashRadius ? Kind.Priority : Kind.Hit);

        private void OnBaseChanged(float current)
        {
            if (current < _lastBaseHealth)
            {
                Play(_baseHit.Next(), 1f, Kind.Priority);
            }

            _lastBaseHealth = current;
        }

        private void OnBaseDied() => Play(_baseExplosion, 1f, Kind.Priority);

        private void OnWaveStarted() => Play(_horn, 0.8f, Kind.Priority);

        private void OnBuilt() => Play(_build, 0.7f, Kind.Priority);

        private void OnUpgraded() => Play(_upgrade, 0.7f, Kind.Priority);

        private void OnRepaired() => Play(_repair, 0.6f, Kind.Priority);

        private void OnSold() => Play(_sell, 0.6f, Kind.Priority);

        private void OnStruck(Vector3 target, Game.Hero.HeroWeaponDefinition weapon) =>
            Play(weapon.Kind == Game.Hero.HeroWeaponKind.Ranged ? _blaster.Next() : _strike, 0.55f, Kind.Hit);

        private void OnHeroDown() => Play(_heroDown, 1f, Kind.Priority);

        private void OnHeroRevive() => Play(_heroRevive, 0.8f, Kind.Priority);

        private void OnShockwave(Vector3 centre, float radius) => Play(_shockwave, 0.9f, Kind.Priority);

        private void OnBossPulsed(Vector3 centre) => Play(_bossPulse, 1f, Kind.Priority);

        // A rally is a roar: the same voice the boss arrives with.
        private void OnBossRallied(Vector3 centre) => Play(_bossRoar, 1f, Kind.Priority);

        // An element creature leaves its nest with the voice of its element.
        private AudioClip NestClipFor(Element element)
        {
            switch (element)
            {
                case Element.Fire:
                    return _emergeLava;
                case Element.Water:
                    return _emergeWater;
                case Element.Sand:
                    return _emergeSand;
                default:
                    return _emergeGround;
            }
        }

        private void OnBossLaser(bool on) => _bossLasers = Mathf.Max(0, _bossLasers + (on ? 1 : -1));

        private void OnMinions(Vector3 centre) => Play(_minionBurst, 0.8f, Kind.Priority);

        // Drones announce only when one is bought; losing one to a flyer stays quiet here.
        private void OnDronesChanged()
        {
            int count = TotalDrones();
            if (count > _droneCount)
            {
                Play(_drone, 0.6f, Kind.Priority);
            }

            _droneCount = count;
        }

        private int TotalDrones()
        {
            LevelDefinition level = _context.Level;
            int total = 0;
            foreach (Game.Drones.DroneDefinition drone in level.Drones)
            {
                total += _context.ActiveDrones(drone);
            }

            return total;
        }

        // Takes a free voice, or the oldest voice of the same kind when that kind has used up its budget, so a
        // crowd of towers can never drown out the base taking a hit. Priority sounds also duck the filler for a moment.
        private int Play(AudioClip clip, float volume, Kind kind)
        {
            float now = Time.unscaledTime;
            int index = PickVoice(kind, now);
            AudioSource voice = _voices[index];
            voice.Stop();
            voice.pitch = 1f + Random.Range(-PitchJitter, PitchJitter);
            voice.volume = volume * _mix.EffectsGain;
            voice.PlayOneShot(clip, 1f);
            _voiceKinds[index] = kind;
            _voiceVolumes[index] = volume;
            _voiceStarts[index] = now;
            // A death is a moment, not a mood: a long recorded clip is cut so a swarm dying never leaves a sound behind.
            float length = kind == Kind.Kill ? Mathf.Min(clip.length, KillMaxSeconds) : clip.length;
            _voiceEnds[index] = now + length / voice.pitch;
            if (kind == Kind.Priority)
            {
                _duckUntil = now + DuckSeconds;
            }

            return index;
        }

        private int PickVoice(Kind kind, float now)
        {
            int playing = 0;
            int oldest = -1;
            int free = -1;
            for (int i = 0; i < VoiceCount; i++)
            {
                if (now >= _voiceEnds[i])
                {
                    if (free < 0)
                    {
                        free = i;
                    }

                    continue;
                }

                if (_voiceKinds[i] == kind)
                {
                    playing++;
                    if (oldest < 0 || _voiceStarts[i] < _voiceStarts[oldest])
                    {
                        oldest = i;
                    }
                }
            }

            if (playing >= Budgets[(int)kind] && oldest >= 0)
            {
                return oldest;
            }

            if (free >= 0)
            {
                return free;
            }

            // Every voice is busy: a priority sound takes the oldest filler, filler takes its own oldest.
            int victim = oldest >= 0 ? oldest : 0;
            for (int i = 0; i < VoiceCount; i++)
            {
                if (_voiceKinds[i] != Kind.Priority && _voiceStarts[i] < _voiceStarts[victim])
                {
                    victim = i;
                }
            }

            return victim;
        }
    }
}
