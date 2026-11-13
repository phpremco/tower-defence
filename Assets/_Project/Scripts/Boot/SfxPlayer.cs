using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// Plays the sound effects of a level: shots in character with the weapon, explosions by what exploded,
    /// creatures through their species' voice (see CreatureChorus), the hero's weapon and stride, the drones' quiet
    /// work, path objects on the road, hits on the base and the player's own actions.
    /// Keeps the field from turning into noise: each kind of filler sound has a budget of voices, shots that land
    /// within a few milliseconds of each other bundle into one, big moments duck the filler, and the further the
    /// camera is zoomed out the quieter and duller the filler gets.
    /// Listens to events only; gameplay does not know it exists.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        private const int VoiceCount = 24;
        private const float PitchJitter = 0.08f;
        private const float ShotVolume = 0.4f;
        // A beam tower holds a hum instead of zapping per tick: one voice per firing tower, released a moment after
        // its last tick, so the sound stops and starts with the beam itself.
        private const int BeamVoiceCount = 4;
        private const float BeamVolume = 0.55f;
        private const float BeamHoldSeconds = 0.3f;
        private const float BeamSameTowerDistance = 1.5f;
        private const float BeamFallbackGain = 0.5f;
        private const float HeavyShotVolume = 0.6f;
        private const float HitVolume = 0.4f;
        private const float DecorSmashVolume = 0.45f;
        private const float SpawnVolume = 0.6f;
        private const float BundleSeconds = 0.06f;
        // Rapid fire is heard as a volley rather than as every bullet: while a rapid shot sound is on cooldown, the
        // same sound only nudges the voice already playing. Slow guns keep a sound per shot.
        private const float RapidCooldownSeconds = 0.22f;
        private const float BundleBoost = 1.18f;
        private const float BundleCap = 1.2f;
        private const float DuckSeconds = 0.6f;
        private const float DuckDepth = 0.45f;
        private const float FarGain = 0.85f;
        private const float FarCutoffHz = 7000f;
        private const float NearCutoffHz = 22000f;
        private const float MaxZoom = 3f;
        private const float LaserFadeSeconds = 0.15f;
        private const float BossLaserVolume = 0.6f;
        private const float ApproachVolume = 0.5f;
        private const float BaseHitMinInterval = 1.5f;
        private const float ApproachFirstDelay = 2.5f;
        private const float ApproachInterval = 3f;
        private const float ApproachMaxSeconds = 40f;
        private const float KillMaxSeconds = 0.7f;
        private const float CutFadeSeconds = 0.12f;
        private const float CannonExplosionVolume = 0.6f;
        private const float RocketExplosionVolume = 0.7f;
        private const float FlakShotVolume = 0.55f;
        private const float FlakBurstVolume = 0.35f;
        private const float MineExplosionVolume = 1f;
        private const float GateExplosionVolume = 1.6f;
        private const float BaseExplosionVolume = 2f;
        private const float WaveDrumsVolume = 1.5f;
        private const float TowerBreakVolume = 1.2f;
        private const float BaseAlertVolume = 0.7f;
        private const float UpgradeVolume = 0.6f;
        // Round 60 (Remco: "tone down the building sounds"): the procedural build blip is mastered hot (p90 RMS 0.256, 2.5x the
        // library's target); at 0.7 it sat 9 dB over an upgrade. At 0.3 it lands about level with one.
        private const float BuildVolume = 0.3f;
        private const float CashVolume = 0.4f;
        private const float ScrapVolume = 0.5f;
        private const float AcidVolume = 0.15f;
        private const float HeroGunVolume = 0.5f;
        private const float SaberVolume = 0.45f;
        private const float SaberHitDelay = 0.12f;
        private const float SaberHumVolume = 0.05f;
        private const float HeroPulseVolume = 2.5f;
        private const float RepairAbilityVolume = 0.9f;
        private const float HeroStepVolume = 0.12f;
        private const float HeroStepSpacing = 0.55f;
        private const float HeroDeathVolume = 1.4f;
        private const float HeroFallVolume = 0.6f;
        private const float HeroFallDelay = 0.5f;
        private const float DroneFanVolume = 0.05f;
        private const float DronesForFullFan = 3f;
        private const float DroneWorkVolume = 0.07f;
        private const float DroneLaserVolume = 0.07f;
        private const float CartVolume = 0.1f;
        // The ground rumbling under a burrowing continent boss swells to this as it nears the surface.
        private const float RumbleVolume = 0.6f;
        private const float FireballVolume = 0.14f;
        private const float FreezeFieldVolume = 0.4f;
        private const float BossGateCrackVolume = 0.7f;
        private const float BossGateFreezeVolume = 0.9f;
        private const float BossBloomVolume = 0.75f;
        private const float BossSplitVolume = 0.9f;
        private const float OraAlertVolume = 0.8f;
        private const float RollersForFullLoop = 2f;
        private const int ScheduleCapacity = 32;
        private static readonly int[] Budgets = { 3, 2, 2, 2, 4, 3, VoiceCount };

        private struct Pending
        {
            public float At;
            public SoundFamily Family;
            public float Volume;
            public SfxKind Kind;
        }

        private AudioSource[] _voices;
        private AudioLowPassFilter[] _filters;
        private SfxKind[] _voiceKinds;
        private float[] _voiceEnds;
        private float[] _voiceVolumes;
        private float[] _voiceStarts;
        private AudioSource _bossLaserLoop;
        private float _duckUntil = float.NegativeInfinity;
        private float _approachUntil = float.NegativeInfinity;
        private float _nextApproach = float.NegativeInfinity;
        private SoundFamily _approach;
        private readonly Dictionary<SoundFamily, float> _shotTimes = new Dictionary<SoundFamily, float>();
        private readonly Dictionary<SoundFamily, int> _shotVoices = new Dictionary<SoundFamily, int>();
        private readonly List<Pending> _pending = new List<Pending>(ScheduleCapacity);
        private readonly List<PathObject> _rollers = new List<PathObject>();
        private readonly HashSet<Drone> _hookedDrones = new HashSet<Drone>();
        // One family per ability, kept for the level, so its recordings rotate and a use allocates nothing.
        private readonly Dictionary<HeroAbilityDefinition, SoundFamily> _abilitySounds = new Dictionary<HeroAbilityDefinition, SoundFamily>();
        private int _bossLasers;
        private int _droneCount;
        private SoundFamily _pulse;
        private SoundFamily _heavy;
        private SoundFamily _rocket;
        private SoundFamily _flak;
        private SoundFamily _flakBurst;
        private SoundFamily _cryo;
        private SoundFamily _tesla;
        private SoundFamily _rail;
        private SoundFamily _cannonExplosion;
        private SoundFamily _rocketExplosion;
        private SoundFamily _mineExplosion;
        private SoundFamily _gateExplosion;
        private SoundFamily _baseExplosion;
        private SoundFamily _towerBreak;
        private AudioClip _bossDeath;
        private SoundFamily _baseAlert;
        private SoundFamily _horn;
        private AudioClip _build;
        private SoundFamily _upgrade;
        private SoundFamily _cash;
        private SoundFamily _scrap;
        private AudioClip _repair;
        private SoundFamily _blaster;
        private SoundFamily _saberSwing;
        private SoundFamily _saberHit;
        private SoundFamily _heroPulse;
        private SoundFamily _heroStep;
        private SoundFamily _heroDeath;
        private SoundFamily _heroFall;
        private AudioClip _heroRevive;
        private AudioClip _droneBuy;
        private SoundFamily _droneLaser;
        private SoundFamily _structureHit;
        private SoundFamily _decorSmash;
        private SoundFamily _freezes;
        private SoundFamily _acid;
        private LoopVoice _saberHum;
        private readonly LoopVoice[] _beamVoices = new LoopVoice[BeamVoiceCount];
        private readonly Vector3[] _beamOrigins = new Vector3[BeamVoiceCount];
        private readonly float[] _beamLastTick = new float[BeamVoiceCount];
        private LoopVoice _droneFan;
        private LoopVoice _droneRepair;
        private LoopVoice _droneHarvest;
        private LoopVoice _cart;
        private LoopVoice _rumble;
        private LoopVoice _fireball;
        private AudioClip _rollerCurl;
        private AudioClip _emergeWater;
        private AudioClip _emergeLava;
        private AudioClip _emergeSand;
        private AudioClip _emergeGround;
        private AudioClip _emergeIce;
        private AudioClip _emergeBog;
        private CreatureChorus _chorus;
        private IReadOnlyList<Drone> _drones;
        private LevelContext _context;
        private AudioMix _mix;
        private FixedCameraRig _rig;
        private float _lastBaseHealth;
        private float _nextBaseHitSound;
        private Vector3 _heroLast;
        private float _heroSinceStep;
        private bool _heroHasSaber;

        /// <summary>The chill of a slow taking hold; the chorus plays it when a creature is first slowed.</summary>
        public SoundFamily Freezes => _freezes;

        public void Initialize(LevelContext context, AudioMix mix, FixedCameraRig rig, IReadOnlyList<Enemy> living, IReadOnlyList<Drone> drones)
        {
            _context = context;
            _mix = mix;
            _rig = rig;
            _drones = drones;
            _voices = new AudioSource[VoiceCount];
            _filters = new AudioLowPassFilter[VoiceCount];
            _voiceKinds = new SfxKind[VoiceCount];
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

            // Recorded clips when the library has them, the generated sound otherwise.
            Services.TryGet(out SfxLibrary library);
            _bossLaserLoop = Loop(library != null && !library.BossLaserLoops.IsEmpty ? library.BossLaserLoops[0] : ProceduralSfx.BossLaserHum());
            CreateFamilies(library);
            _chorus = new CreatureChorus(this, gameObject, living, library);

            _lastBaseHealth = context.BaseHealth.Current;
            _droneCount = TotalDrones();
            context.Projectiles.Shot += OnShot;
            context.Projectiles.Exploded += OnExploded;
            context.EnemyKilled.Subscribe(OnKilled);
            context.EnemySpawned.Subscribe(OnSpawned);
            context.StructureHit.Subscribe(OnStructureHit);
            context.DecorSmashed.Subscribe(OnDecorSmashed);
            context.TowerWrecked.Subscribe(OnTowerWrecked);
            context.BaseHealth.Changed += OnBaseChanged;
            context.BaseHealth.Died += OnBaseDied;
            context.WaveStarted.Subscribe(OnWaveStarted);
            context.TowerBuilt.Subscribe(OnBuilt);
            context.TowerUpgraded.Subscribe(OnUpgraded);
            context.TowerRepaired.Subscribe(OnRepaired);
            context.TowerSold.Subscribe(OnSold);
            context.TowerScrapped.Subscribe(OnScrapped);
            context.PathObjectSpawned.Subscribe(OnPathObjectSpawned);
            context.PathObjectTriggered.Subscribe(OnPathObjectTriggered);
            context.DronesChanged.Subscribe(OnDronesChanged);
            context.BossLaserChanged.Subscribe(OnBossLaser);
            context.BossWaveStarted.Subscribe(OnBossWave);
            context.BossGateWarned.Subscribe(OnBossGateWarned);
            context.BossGateFrozen.Subscribe(OnBossGateFrozen);
            context.BossBloomed.Subscribe(OnBossBloomed);
            context.BossSplit.Subscribe(OnBossSplit);
            context.BossAdapted.Subscribe(OnBossAdapted);
            if (context.Hero != null)
            {
                context.Hero.Struck += OnStruck;
                context.Hero.AbilityUsed += OnAbilityUsed;
                context.Hero.Downed += OnHeroDown;
                context.Hero.Revived += OnHeroRevive;
                _heroLast = context.Hero.transform.position;
                _heroHasSaber = context.Hero.Definition.Weapon != null && context.Hero.Definition.Weapon.Style == HeroWeaponStyle.EnergySword;
            }
        }

        private void CreateFamilies(SfxLibrary library)
        {
            _pulse = new SoundFamily(library?.PulseShots, ProceduralSfx.PulseShot());
            _heavy = new SoundFamily(library?.HeavyShots, ProceduralSfx.HeavyShot());
            _rocket = new SoundFamily(library?.RocketLaunches, ProceduralSfx.RocketLaunch());
            _flak = new SoundFamily(library?.FlakShots, ProceduralSfx.HeavyShot());
            _flakBurst = new SoundFamily(library?.FlakBursts, ProceduralSfx.Explosion());
            _cryo = new SoundFamily(library?.CryoShots, ProceduralSfx.CryoShot());
            _tesla = new SoundFamily(library?.TeslaShots, ProceduralSfx.TeslaShot());
            for (int i = 0; i < BeamVoiceCount; i++)
            {
                _beamVoices[i] = library != null && !library.BeamLoops.IsEmpty ? LoopVoice.Create(gameObject, library.BeamLoops) : LoopVoice.Create(gameObject, ProceduralSfx.BossLaserHum(), BeamFallbackGain);
            }
            _rail = new SoundFamily(library?.RailShots, ProceduralSfx.RailShot());
            _cannonExplosion = new SoundFamily(library?.CannonExplosions, ProceduralSfx.Explosion());
            _rocketExplosion = new SoundFamily(library?.RocketExplosions, ProceduralSfx.Explosion());
            _mineExplosion = new SoundFamily(library?.MineExplosions, ProceduralSfx.BigExplosion());
            _gateExplosion = new SoundFamily(library?.GateExplosions, ProceduralSfx.BigExplosion());
            _baseExplosion = new SoundFamily(library?.BaseExplosions, ProceduralSfx.BaseExplosion());
            _towerBreak = new SoundFamily(library?.TowerBreaks, ProceduralSfx.BigExplosion());
            _bossDeath = ProceduralSfx.BossDeath();
            _baseAlert = new SoundFamily(library?.BaseAlerts, ProceduralSfx.BaseHit());
            _horn = new SoundFamily(library?.WaveDrums, ProceduralSfx.WaveHorn());
            _build = ProceduralSfx.Build();
            _upgrade = new SoundFamily(library?.Upgrades, ProceduralSfx.Upgrade());
            _cash = new SoundFamily(library?.Cash, ProceduralSfx.Sell());
            _scrap = new SoundFamily(library?.ScrapPickups, ProceduralSfx.Sell());
            _repair = ProceduralSfx.Repair();
            _blaster = new SoundFamily(library?.HeroBlasters, ProceduralSfx.HeroBlaster());
            _saberSwing = new SoundFamily(library?.SaberSwings, ProceduralSfx.HeroStrike());
            _saberHit = new SoundFamily(library?.SaberHits);
            _heroPulse = new SoundFamily(library?.HeroPulses, ProceduralSfx.Shockwave());
            _heroStep = new SoundFamily(library?.HeroSteps);
            _heroDeath = new SoundFamily(library?.HeroDeaths, ProceduralSfx.HeroDown());
            _heroFall = new SoundFamily(library?.HeroFalls);
            _heroRevive = ProceduralSfx.HeroRevive();
            _droneBuy = ProceduralSfx.DroneBuy();
            _droneLaser = new SoundFamily(library?.DroneLasers, ProceduralSfx.PulseShot());
            _structureHit = new SoundFamily(library?.StructureHits, ProceduralSfx.StructureHit());
            _decorSmash = new SoundFamily(library?.DecorSmashes, ProceduralSfx.StructureHit());
            _freezes = new SoundFamily(library?.Freezes);
            _acid = new SoundFamily(library?.AcidSizzles);
            _saberHum = LoopVoice.Create(gameObject, library?.SaberHums);
            _droneFan = LoopVoice.Create(gameObject, library?.DroneFanLoops);
            _droneRepair = LoopVoice.Create(gameObject, library?.DroneRepairLoops);
            _droneHarvest = LoopVoice.Create(gameObject, library?.DroneHarvestLoops);
            _cart = LoopVoice.Create(gameObject, library?.CartLoops);
            _rumble = LoopVoice.Create(gameObject, library?.Earthquakes);
            _fireball = LoopVoice.Create(gameObject, library?.FireballLoops);
            _rollerCurl = ProceduralSfx.RollerCurl();
            _emergeWater = ProceduralSfx.EmergeWater();
            _emergeLava = ProceduralSfx.EmergeLava();
            _emergeSand = ProceduralSfx.EmergeSand();
            _emergeGround = ProceduralSfx.EmergeGround();
            _emergeIce = ProceduralSfx.EmergeIce();
            _emergeBog = ProceduralSfx.EmergeBog();
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
            _context.DecorSmashed.Unsubscribe(OnDecorSmashed);
            _context.TowerWrecked.Unsubscribe(OnTowerWrecked);
            _context.BaseHealth.Changed -= OnBaseChanged;
            _context.BaseHealth.Died -= OnBaseDied;
            _context.WaveStarted.Unsubscribe(OnWaveStarted);
            _context.TowerBuilt.Unsubscribe(OnBuilt);
            _context.TowerUpgraded.Unsubscribe(OnUpgraded);
            _context.TowerRepaired.Unsubscribe(OnRepaired);
            _context.TowerSold.Unsubscribe(OnSold);
            _context.TowerScrapped.Unsubscribe(OnScrapped);
            _context.PathObjectSpawned.Unsubscribe(OnPathObjectSpawned);
            _context.PathObjectTriggered.Unsubscribe(OnPathObjectTriggered);
            _context.DronesChanged.Unsubscribe(OnDronesChanged);
            _context.BossLaserChanged.Unsubscribe(OnBossLaser);
            _context.BossWaveStarted.Unsubscribe(OnBossWave);
            _context.BossGateWarned.Unsubscribe(OnBossGateWarned);
            _context.BossGateFrozen.Unsubscribe(OnBossGateFrozen);
            _context.BossBloomed.Unsubscribe(OnBossBloomed);
            _context.BossSplit.Unsubscribe(OnBossSplit);
            _context.BossAdapted.Unsubscribe(OnBossAdapted);
            if (_context.Hero != null)
            {
                _context.Hero.Struck -= OnStruck;
                _context.Hero.AbilityUsed -= OnAbilityUsed;
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
            float dt = Time.unscaledDeltaTime;
            UpdateApproach(now);
            RunPending(now);
            float zoom = Mathf.Clamp01((_rig.Zoom - 1f) / (MaxZoom - 1f));
            float distance = Mathf.Lerp(FarGain, 1f, zoom);
            float cutoff = Mathf.Lerp(FarCutoffHz, NearCutoffHz, zoom);
            float duck = now < _duckUntil ? DuckDepth : 1f;
            float effects = _mix.EffectsGain;
            // When the level is decided every running sound stops; only one-shots of the ending itself still play.
            float running = _context.Outcome == LevelOutcome.Running ? 1f : 0f;
            float filler = effects * distance * duck * running;

            for (int i = 0; i < VoiceCount; i++)
            {
                if (now >= _voiceEnds[i])
                {
                    // A voice past its end that is still sounding was cut short on purpose: fade it out and stop it.
                    if (_voices[i].isPlaying)
                    {
                        _voices[i].volume = Mathf.MoveTowards(_voices[i].volume, 0f, dt / CutFadeSeconds);
                        if (_voices[i].volume <= 0f)
                        {
                            _voices[i].Stop();
                        }
                    }

                    continue;
                }

                bool isFiller = _voiceKinds[i] != SfxKind.Priority;
                _voices[i].volume = _voiceVolumes[i] * (isFiller ? filler : effects);
                _filters[i].cutoffFrequency = isFiller ? cutoff : NearCutoffHz;
            }

            float fade = dt / LaserFadeSeconds;
            _bossLaserLoop.volume = Mathf.MoveTowards(_bossLaserLoop.volume, _bossLasers > 0 ? BossLaserVolume * effects * running : 0f, fade);
            _chorus.Update(dt, filler);
            if (_rumble != null)
            {
                _rumble.Target = RumbleVolume * _context.BossRumble * running;
                _rumble.Tick(dt, effects);
            }

            UpdateHero(dt, filler);
            UpdateBeams(dt, filler);
            UpdateDrones(dt, filler);
            UpdateRollers(dt, filler);
        }

        // A sound booked for later: the landing of a shot, the fall after a death cry.
        public void Schedule(SoundFamily family, float volume, SfxKind kind, float delay)
        {
            if (!family.HasSound || _pending.Count >= ScheduleCapacity)
            {
                return;
            }

            _pending.Add(new Pending { At = Time.unscaledTime + delay, Family = family, Volume = volume, Kind = kind });
        }

        private void RunPending(float now)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (now < _pending[i].At)
                {
                    continue;
                }

                Pending pending = _pending[i];
                _pending.RemoveAt(i);
                Play(pending.Family, pending.Volume, pending.Kind);
            }
        }

        // The weapon speaks through its shot: chilling shots ring, splash shots thump, flak at the sky whooshes.
        // A laser zaps once per shot, bundled like the tesla so a fast tower does not stutter.
        private void OnShot(ShotInfo shot)
        {
            if (shot.Style == ProjectileStyle.Laser)
            {
                TouchBeam(shot.Position);
            }
            else if (shot.Style == ProjectileStyle.Lightning)
            {
                Shot(_tesla, ShotVolume, RapidCooldownSeconds);
            }
            else if (shot.Style == ProjectileStyle.Flak)
            {
                Shot(_flak, FlakShotVolume, BundleSeconds);
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
        private void Shot(SoundFamily family, float volume, float cooldown)
        {
            float now = Time.unscaledTime;
            if (_shotTimes.TryGetValue(family, out float last) && now - last < cooldown && _shotVoices.TryGetValue(family, out int voice) && _voiceKinds[voice] == SfxKind.Shot && _voiceEnds[voice] > now)
            {
                _voiceVolumes[voice] = Mathf.Min(volume * family.Gain * BundleCap, _voiceVolumes[voice] * BundleBoost);
                return;
            }

            _shotTimes[family] = now;
            _shotVoices[family] = Play(family, volume, SfxKind.Shot);
        }

        // The splash of a shell is a different bang from a rocket bursting in the air.
        private void OnExploded(Vector3 position, float radius, ProjectileStyle style)
        {
            if (style == ProjectileStyle.Rocket)
            {
                Play(_rocketExplosion, RocketExplosionVolume, SfxKind.Hit);
            }
            else if (style == ProjectileStyle.Flak)
            {
                Play(_flakBurst, FlakBurstVolume, SfxKind.Hit);
            }
            else
            {
                Play(_cannonExplosion, CannonExplosionVolume, SfxKind.Hit);
            }
        }

        private void OnKilled(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                Play(_bossDeath, 1f, SfxKind.Priority);
            }

            _chorus.OnKilled(enemy);
        }

        // From the alarm until the boss shows itself, its entrance gives it away: the boss's own recorded growl from
        // the gate, or bubbles, churning lava, pouring sand from a pool. A boss without a recorded growl arrives
        // unannounced rather than behind a generated noise; the alarm, the rumble and the marker still point the way.
        private void OnBossWave()
        {
            _approach = ApproachFor(_context.ComingBoss, _context.Level);
            if (_approach == null)
            {
                return;
            }

            _approachUntil = Time.unscaledTime + ApproachMaxSeconds;
            _nextApproach = Time.unscaledTime + ApproachFirstDelay;
        }

        private void UpdateApproach(float now)
        {
            if (_approach == null || now >= _approachUntil || now < _nextApproach || _context.BossesAlive > 0 || _context.Paused)
            {
                return;
            }

            Play(_approach, ApproachVolume, SfxKind.Priority);
            _nextApproach = now + ApproachInterval;
        }

        private static SoundFamily ApproachFor(EnemyDefinition boss, LevelDefinition level)
        {
            BossEntrance entrance = level.BossEntrance;
            if (entrance.Kind != EntranceKind.Pool)
            {
                CreatureVoice voice = boss != null ? boss.Voice : null;
                return voice != null && !voice.Growls.IsEmpty ? new SoundFamily(voice.Growls) : null;
            }

            return new SoundFamily(null, PoolApproachClip(level, entrance));
        }

        private static AudioClip PoolApproachClip(LevelDefinition level, BossEntrance entrance)
        {

            switch (level.Terrain.KindAt(entrance.Cell))
            {
                case TerrainKind.Lava:
                    return ProceduralSfx.BossLavaChurn();
                case TerrainKind.Quicksand:
                    return ProceduralSfx.BossSandShift();
                case TerrainKind.Ice:
                    return ProceduralSfx.BossIceCrack();
                default:
                    return ProceduralSfx.BossBubbles();
            }
        }

        // Creatures from a pool or the ground splash and crack their way out; rollers clack as they curl; the chorus
        // takes over the creature's own voice from here.
        private void OnSpawned(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                _approachUntil = float.NegativeInfinity;
            }

            _chorus.OnSpawned(enemy);
            SfxKind kind = enemy.Definition.IsBoss ? SfxKind.Priority : SfxKind.Spawn;
            switch (enemy.Origin)
            {
                case EnemyOrigin.Water:
                    Play(_emergeWater, SpawnVolume, kind);
                    break;
                case EnemyOrigin.Lava:
                    Play(_emergeLava, SpawnVolume, kind);
                    break;
                case EnemyOrigin.Quicksand:
                    Play(_emergeSand, SpawnVolume, kind);
                    break;
                case EnemyOrigin.Ice:
                    Play(_emergeIce, SpawnVolume, kind);
                    break;
                case EnemyOrigin.Bog:
                    Play(_emergeBog, SpawnVolume, kind);
                    break;
                case EnemyOrigin.Rift:
                    // Gravel and cracking rock, like the ground opening; the stone voice follows from the chorus.
                    Play(_emergeGround, SpawnVolume, kind);
                    break;
                case EnemyOrigin.Ground:
                    Play(_emergeGround, 1f, SfxKind.Priority);
                    break;
                case EnemyOrigin.Nest:
                    Play(NestClipFor(enemy.Definition.Element), SpawnVolume, SfxKind.Spawn);
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
                Play(_rollerCurl, SpawnVolume, SfxKind.Spawn);
            }
        }

        private void OnStructureHit(Vector3 position) => Play(_structureHit, HitVolume, SfxKind.Hit);

        private void OnDecorSmashed(Vector3 position) => Play(_decorSmash, DecorSmashVolume, SfxKind.Hit);

        // A gate goes with a blast; a weapon tower tears itself apart.
        private void OnTowerWrecked(Tower tower)
        {
            if (tower.Definition.BlocksGroundEnemies)
            {
                Play(_gateExplosion, GateExplosionVolume, SfxKind.Priority);
            }
            else
            {
                Play(_towerBreak, TowerBreakVolume, SfxKind.Priority);
            }
        }

        private void OnBaseChanged(float current)
        {
            // A besieged base is hit every second by every attacker; one alarm every so often says enough.
            if (current < _lastBaseHealth && Time.unscaledTime >= _nextBaseHitSound)
            {
                Play(_baseAlert, BaseAlertVolume, SfxKind.Priority);
                _nextBaseHitSound = Time.unscaledTime + BaseHitMinInterval;
            }

            _lastBaseHealth = current;
        }

        private void OnBaseDied() => Play(_baseExplosion, BaseExplosionVolume, SfxKind.Priority);

        private void OnWaveStarted() => Play(_horn, WaveDrumsVolume, SfxKind.Priority);

        private void OnBuilt() => Play(_build, BuildVolume, SfxKind.Priority);

        private void OnUpgraded() => Play(_upgrade, UpgradeVolume, SfxKind.Priority);

        private void OnRepaired() => Play(_repair, 0.6f, SfxKind.Priority);

        private void OnSold() => Play(_cash, CashVolume, SfxKind.Priority);

        private void OnScrapped()
        {
            Play(_scrap, ScrapVolume, SfxKind.Priority);
            Schedule(_cash, CashVolume, SfxKind.Priority, 0.35f);
        }

        // A mine or barrel is a blast; cold is laid down; a roller rumbles until it is done.
        private void OnPathObjectSpawned(PathObject pathObject)
        {
            switch (pathObject.Definition.Effect)
            {
                case PathObjectEffect.Slow:
                    Play(_freezes, FreezeFieldVolume, SfxKind.Priority);
                    break;
                case PathObjectEffect.Roll:
                case PathObjectEffect.Patrol:
                    _rollers.Add(pathObject);
                    pathObject.Finished += finished => _rollers.Remove(finished);
                    Play(_build, BuildVolume, SfxKind.Priority);
                    break;
                default:
                    Play(_build, BuildVolume, SfxKind.Priority);
                    break;
            }
        }

        private void OnPathObjectTriggered(PathObjectDefinition definition) => Play(_mineExplosion, MineExplosionVolume, SfxKind.Priority);

        private void UpdateRollers(float dt, float filler)
        {
            float fire = 0f;
            float cart = 0f;
            for (int i = 0; i < _rollers.Count; i++)
            {
                if (_rollers[i].Definition.Effect == PathObjectEffect.Roll)
                {
                    fire += 1f;
                }
                else
                {
                    cart += 1f;
                }
            }

            if (_fireball != null)
            {
                _fireball.Target = FireballVolume * Mathf.Min(1f, fire / RollersForFullLoop);
                _fireball.Tick(dt, filler);
            }

            if (_cart != null)
            {
                _cart.Target = CartVolume * Mathf.Min(1f, cart / RollersForFullLoop);
                _cart.Tick(dt, filler);
            }
        }

        // The hero's blade swings and then bites; the gun just fires.
        private void OnStruck(Vector3 target, HeroWeaponDefinition weapon)
        {
            if (weapon.Kind == HeroWeaponKind.Ranged)
            {
                Play(_blaster, HeroGunVolume, SfxKind.Crew);
                return;
            }

            Play(_saberSwing, SaberVolume, SfxKind.Crew);
            Schedule(_saberHit, SaberVolume, SfxKind.Crew, SaberHitDelay);
        }

        private void OnHeroDown()
        {
            Play(_heroDeath, HeroDeathVolume, SfxKind.Priority);
            Schedule(_heroFall, HeroFallVolume, SfxKind.Priority, HeroFallDelay);
        }

        private void OnHeroRevive() => Play(_heroRevive, 0.8f, SfxKind.Priority);

        // An ability with its own recording plays that; otherwise the kind picks the stand-in.
        private void OnAbilityUsed(int slot, HeroAbilityDefinition ability, Vector3 position)
        {
            if (!ability.Sound.IsEmpty)
            {
                Play(AbilitySound(ability), HeroPulseVolume, SfxKind.Priority);
            }
            else if (ability.Kind == HeroAbilityKind.Shockwave)
            {
                Play(_heroPulse, HeroPulseVolume, SfxKind.Priority);
            }
            else
            {
                Play(_repair, RepairAbilityVolume, SfxKind.Priority);
            }
        }

        private SoundFamily AbilitySound(HeroAbilityDefinition ability)
        {
            if (!_abilitySounds.TryGetValue(ability, out SoundFamily family))
            {
                family = new SoundFamily(ability.Sound);
                _abilitySounds.Add(ability, family);
            }

            return family;
        }

        // A beam tick keeps the voice of the tower it came from alive, or claims a free voice for a tower that just
        // opened fire. With every voice busy the tick goes unheard; four beams already sound like a wall of them.
        private void TouchBeam(Vector3 origin)
        {
            float now = Time.unscaledTime;
            int free = -1;
            for (int i = 0; i < BeamVoiceCount; i++)
            {
                bool live = now - _beamLastTick[i] < BeamHoldSeconds;
                if (live && (origin - _beamOrigins[i]).sqrMagnitude < BeamSameTowerDistance * BeamSameTowerDistance)
                {
                    _beamLastTick[i] = now;
                    return;
                }

                if (!live && free < 0)
                {
                    free = i;
                }
            }

            if (free >= 0)
            {
                _beamOrigins[free] = origin;
                _beamLastTick[free] = now;
            }
        }

        private void UpdateBeams(float dt, float filler)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < BeamVoiceCount; i++)
            {
                LoopVoice voice = _beamVoices[i];
                if (voice == null)
                {
                    continue;
                }

                voice.Target = now - _beamLastTick[i] < BeamHoldSeconds ? BeamVolume : 0f;
                voice.Tick(dt, filler);
            }
        }

        // Footsteps by distance walked, and the blade's hum while the hero is on their feet.
        private void UpdateHero(float dt, float filler)
        {
            Commander hero = _context.Hero;
            if (hero == null)
            {
                return;
            }

            if (_saberHum != null)
            {
                _saberHum.Target = _heroHasSaber && !hero.IsDown ? SaberHumVolume : 0f;
                _saberHum.Tick(dt, filler);
            }

            Vector3 position = hero.transform.position;
            float moved = Vector3.Distance(position, _heroLast);
            _heroLast = position;
            if (!hero.IsWalking || hero.IsFlying || moved <= 0f)
            {
                _heroSinceStep = 0f;
                return;
            }

            _heroSinceStep += moved;
            if (_heroSinceStep >= HeroStepSpacing)
            {
                _heroSinceStep = 0f;
                Play(_heroStep, HeroStepVolume, SfxKind.Crew);
            }
        }

        // Drones hum as a quiet crowd; welding and drilling sound while any drone is at it; the gunship's shots are small.
        private void UpdateDrones(float dt, float filler)
        {
            float flying = 0f;
            bool repairing = false;
            bool mining = false;
            for (int i = 0; i < _drones.Count; i++)
            {
                Drone drone = _drones[i];
                if (!drone.IsAlive)
                {
                    continue;
                }

                flying += 1f;
                repairing |= drone.Activity == DroneActivity.Repairing;
                mining |= drone.Activity == DroneActivity.Mining;
                if (_hookedDrones.Add(drone))
                {
                    drone.Fired += OnDroneFired;
                    drone.Delivered += OnDelivered;
                }
            }

            if (_droneFan != null)
            {
                _droneFan.Target = DroneFanVolume * Mathf.Min(1f, flying / DronesForFullFan);
                _droneFan.Tick(dt, filler);
            }

            if (_droneRepair != null)
            {
                _droneRepair.Target = repairing ? DroneWorkVolume : 0f;
                _droneRepair.Tick(dt, filler);
            }

            if (_droneHarvest != null)
            {
                _droneHarvest.Target = mining ? DroneWorkVolume : 0f;
                _droneHarvest.Tick(dt, filler);
            }
        }

        private void OnDroneFired(Vector3 target) => Shot(_droneLaser, DroneLaserVolume, RapidCooldownSeconds);

        private void OnDelivered(int value) => Play(_cash, CashVolume, SfxKind.Crew);

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
                case Element.Ice:
                    return _emergeIce;
                case Element.Bog:
                    return _emergeBog;
                case Element.Stone:
                    return _emergeGround;
                default:
                    return _emergeGround;
            }
        }

        // The ice cracking at the gate an ice boss is heading for, and the freeze itself when its breath lands.
        private void OnBossGateWarned(Vector3 gate) => Play(ProceduralSfx.BossIceCrack(), BossGateCrackVolume, SfxKind.Priority);

        private void OnBossGateFrozen(Vector3 gate) => Play(_freezes, BossGateFreezeVolume, SfxKind.Priority);

        private void OnBossBloomed(Vector3 at) => Play(ProceduralSfx.BossBloom(), BossBloomVolume, SfxKind.Priority);

        private void OnBossSplit(Vector3 at) => Play(ProceduralSfx.BossShatter(), BossSplitVolume, SfxKind.Priority);

        private void OnBossAdapted(string line) => Play(ProceduralSfx.OraAlert(), OraAlertVolume, SfxKind.Priority);

        private void OnBossLaser(bool on) => _bossLasers = Mathf.Max(0, _bossLasers + (on ? 1 : -1));

        // Drones announce only when one is bought; losing one to a flyer stays quiet here.
        private void OnDronesChanged()
        {
            int count = TotalDrones();
            if (count > _droneCount)
            {
                Play(_droneBuy, 0.6f, SfxKind.Priority);
            }

            _droneCount = count;
            _hookedDrones.RemoveWhere(drone => drone == null);
        }

        private int TotalDrones()
        {
            LevelDefinition level = _context.Level;
            int total = 0;
            foreach (DroneDefinition drone in level.Drones)
            {
                total += _context.ActiveDrones(drone);
            }

            return total;
        }

        /// <summary>Plays the next variant of a family at the given level times the family's loudness gain.</summary>
        public int Play(SoundFamily family, float volume, SfxKind kind)
        {
            AudioClip clip = family.Next();
            return clip == null ? -1 : Play(clip, volume * family.Gain, kind, family.Pitch);
        }

        // Takes a free voice, or the oldest voice of the same kind when that kind has used up its budget, so a
        // crowd of towers can never drown out the base taking a hit. Priority sounds also duck the filler for a moment.
        private int Play(AudioClip clip, float volume, SfxKind kind, float pitch = 1f)
        {
            float now = Time.unscaledTime;
            int index = PickVoice(kind, now);
            AudioSource voice = _voices[index];
            voice.Stop();
            voice.pitch = pitch * (1f + Random.Range(-PitchJitter, PitchJitter));
            voice.volume = volume * _mix.EffectsGain;
            // PlayOneShot leaves the clip slot empty; keeping the clip there lets probes read what each voice last played.
            voice.clip = clip;
            voice.PlayOneShot(clip, 1f);
            _voiceKinds[index] = kind;
            _voiceVolumes[index] = volume;
            _voiceStarts[index] = now;
            // A death is a moment, not a mood: a long recorded clip is cut so a swarm dying never leaves a sound behind.
            float length = kind == SfxKind.Kill ? Mathf.Min(clip.length, KillMaxSeconds) : clip.length;
            _voiceEnds[index] = now + length / voice.pitch;
            if (kind == SfxKind.Priority)
            {
                _duckUntil = now + DuckSeconds;
            }

            return index;
        }

        private int PickVoice(SfxKind kind, float now)
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
                if (_voiceKinds[i] != SfxKind.Priority && _voiceStarts[i] < _voiceStarts[victim])
                {
                    victim = i;
                }
            }

            return victim;
        }
    }
}
