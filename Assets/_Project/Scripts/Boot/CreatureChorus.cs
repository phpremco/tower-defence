using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// The voices of every creature on the field, played from the species' CreatureVoice asset. Kept deliberately
    /// quiet and sparse: each species shares one movement loop whose level grows gently with the size of the crowd,
    /// steps are rationed per species, and only bosses get to be loud. Attacks and powers are hooked per enemy
    /// instance once, because pooled enemies come back with the same component.
    /// </summary>
    public sealed class CreatureChorus
    {
        private const float MoveLoopCap = 0.3f;
        private const float BossMoveLoopCap = 0.5f;
        private const float CrowdForFullLoop = 4f;
        private const float StepVolume = 0.2f;
        private const float BossStepVolume = 0.55f;
        private const float StepMinInterval = 0.11f;
        private const float BossStepScale = 1.6f;
        private const float ScreechVolume = 0.45f;
        // Round 60 (Remco: "the ants that keep dying get irritating"): two death cries of one species are at least this far apart
        // (a species' voice may ask for more); the deaths in between stay silent, so a crowd dying is a few cries, not a chatter.
        private const float DeathMinInterval = 0.12f;
        private const float BossScreechVolume = 0.8f;
        private const float AttackVolume = 0.4f;
        private const float BossAttackVolume = 0.7f;
        private const float ImpactVolume = 0.32f;
        private const float BoltSpeed = 7f;
        private const float FreezeVolume = 0.25f;
        private const float FreezeMinInterval = 0.3f;
        private const float GrowlVolume = 0.8f;
        private const float PowerVolume = 3f;
        private const float VolleyVolume = 0.5f;
        private const float VolleyFlightSeconds = 1.1f;
        private const float RegrowVolume = 0.7f;
        private const float SpawnVolume = 2f;
        private const float LaserStartVolume = 0.7f;
        private const float SteamVolume = 0.3f;

        private sealed class Tracker
        {
            public float Progress;
            public float SinceStep;
            public bool Hooked;
        }

        // One voice asset's families and loops, shared by every creature of that species.
        private sealed class Voicing
        {
            public SoundFamily Steps;
            public SoundFamily Screeches;
            public SoundFamily Growls;
            public SoundFamily Attacks;
            public SoundFamily Impacts;
            public SoundFamily Pulse;
            public SoundFamily VolleyLaunch;
            public SoundFamily VolleyImpact;
            public SoundFamily Rally;
            public SoundFamily Regrow;
            public SoundFamily Spawn;
            public SoundFamily LaserStart;
            public LoopVoice Move;
            public LoopVoice Roll;
            public float StepSpacing;
            public float Presence = 1f;
            public bool IsBoss;
            public float MoveWeight;
            public float RollWeight;
            public float LastStep = float.NegativeInfinity;
            public float DeathVolume = 1f;
            public float DeathInterval;
            public float LastDeath = float.NegativeInfinity;
        }

        private readonly SfxPlayer _player;
        private readonly GameObject _host;
        private readonly IReadOnlyList<Enemy> _living;
        private readonly SfxLibrary _library;
        private readonly Dictionary<Enemy, Tracker> _trackers = new Dictionary<Enemy, Tracker>(128);
        private readonly Dictionary<CreatureVoice, Voicing> _voicings = new Dictionary<CreatureVoice, Voicing>();
        private readonly List<Voicing> _active = new List<Voicing>();
        private readonly Voicing _genericWalker;
        private readonly Voicing _genericFlyer;
        private readonly Voicing _genericBoss;
        private readonly Voicing _genericBossFlyer;
        private readonly SoundFamily _steam;
        private readonly AudioClip _bossRoar;
        private readonly AudioClip _bossPulse;
        private readonly AudioClip _minionBurst;
        private float _lastFreeze = float.NegativeInfinity;

        public CreatureChorus(SfxPlayer player, GameObject host, IReadOnlyList<Enemy> living, SfxLibrary library)
        {
            _player = player;
            _host = host;
            _living = living;
            _library = library;
            _bossRoar = ProceduralSfx.BossRoar();
            _bossPulse = ProceduralSfx.BossPulse();
            _minionBurst = ProceduralSfx.MinionBurst();
            _steam = new SoundFamily(library?.SteamBursts);

            // Creatures without a voice asset: a generic death cry, wings for flyers and heavy steps for bosses.
            _genericWalker = Generic(library?.EnemyDeaths, null, null);
            _genericFlyer = Generic(library?.EnemyDeaths, library?.WingLoops, null);
            _genericBoss = Generic(library?.EnemyDeaths, null, library?.HeavySteps);
            _genericBossFlyer = Generic(library?.EnemyDeaths, library?.LargeWingLoops, null);
        }

        private Voicing Generic(SoundSet deaths, SoundSet wings, SoundSet steps)
        {
            return new Voicing
            {
                Steps = new SoundFamily(steps),
                Screeches = new SoundFamily(deaths, ProceduralSfx.EnemyDeath()),
                Growls = new SoundFamily(null, _bossRoar),
                Attacks = new SoundFamily(null),
                Impacts = new SoundFamily(null),
                Pulse = new SoundFamily(null, _bossPulse),
                VolleyLaunch = new SoundFamily(null, _bossPulse),
                VolleyImpact = new SoundFamily(null),
                Rally = new SoundFamily(null, _bossRoar),
                Regrow = new SoundFamily(null),
                Spawn = new SoundFamily(null, _minionBurst),
                LaserStart = new SoundFamily(null),
                Move = LoopVoice.Create(_host, wings),
                StepSpacing = 1f,
            };
        }

        private Voicing VoicingFor(Enemy enemy)
        {
            CreatureVoice voice = enemy.Definition.Voice;
            if (voice == null)
            {
                bool flies = enemy.Definition.Flies;
                return enemy.Definition.IsBoss ? (flies ? _genericBossFlyer : _genericBoss) : (flies ? _genericFlyer : _genericWalker);
            }

            if (_voicings.TryGetValue(voice, out Voicing voicing))
            {
                return voicing;
            }

            voicing = new Voicing
            {
                Steps = new SoundFamily(voice.Steps),
                Screeches = new SoundFamily(voice.Screeches, ProceduralSfx.EnemyDeath()),
                Growls = new SoundFamily(voice.Growls, _bossRoar),
                Attacks = new SoundFamily(voice.Attacks),
                Impacts = new SoundFamily(voice.Impacts),
                Pulse = new SoundFamily(voice.Pulse, _bossPulse),
                VolleyLaunch = new SoundFamily(voice.VolleyLaunch, _bossPulse),
                VolleyImpact = new SoundFamily(voice.VolleyImpact),
                Rally = new SoundFamily(voice.Rally, _bossRoar),
                Regrow = new SoundFamily(voice.Regrow),
                Spawn = new SoundFamily(voice.Spawn, _minionBurst),
                LaserStart = new SoundFamily(voice.LaserStart),
                Move = LoopVoice.Create(_host, voice.MoveLoop),
                Roll = LoopVoice.Create(_host, voice.RollLoop),
                StepSpacing = voice.StepSpacing,
                Presence = voice.Presence,
                DeathVolume = voice.DeathVolume,
                DeathInterval = voice.DeathInterval,
                IsBoss = enemy.Definition.IsBoss,
            };
            _voicings[voice] = voicing;
            return voicing;
        }

        /// <summary>Called for every spawn, pooled or fresh: resets the stride and hooks the instance's events once.</summary>
        public void OnSpawned(Enemy enemy)
        {
            if (!_trackers.TryGetValue(enemy, out Tracker tracker))
            {
                tracker = new Tracker();
                _trackers[enemy] = tracker;
            }

            tracker.Progress = enemy.PathProgress;
            tracker.SinceStep = 0f;
            if (!tracker.Hooked)
            {
                tracker.Hooked = true;
                Hook(enemy);
            }

            if (enemy.Definition.IsBoss)
            {
                _player.Play(VoicingFor(enemy).Growls, GrowlVolume, SfxKind.Priority);
            }
        }

        // One subscription per instance for its whole life; the definition never changes under a pooled enemy.
        private void Hook(Enemy enemy)
        {
            enemy.Attacked += _ => OnAttacked(enemy);
            enemy.Slowed += OnSlowed;
            if (!enemy.Definition.IsBoss)
            {
                return;
            }

            var abilities = enemy.GetComponent<BossAbilities>();
            if (abilities == null)
            {
                return;
            }

            abilities.Pulsed += (centre, radius, color) => _player.Play(VoicingFor(enemy).Pulse, PowerVolume, SfxKind.Priority);
            abilities.Rallied += (centre, color) => _player.Play(VoicingFor(enemy).Rally, GrowlVolume, SfxKind.Priority);
            abilities.Regrown += color => _player.Play(VoicingFor(enemy).Regrow, RegrowVolume, SfxKind.Priority);
            abilities.MinionsReleased += (centre, color) => _player.Play(VoicingFor(enemy).Spawn, SpawnVolume, SfxKind.Priority);
            abilities.LaserChanged += on =>
            {
                if (on)
                {
                    _player.Play(VoicingFor(enemy).LaserStart, LaserStartVolume, SfxKind.Priority);
                }
            };
            abilities.Volleyed += (origin, points, color) =>
            {
                Voicing voicing = VoicingFor(enemy);
                _player.Play(voicing.VolleyLaunch, VolleyVolume, SfxKind.Priority);
                _player.Schedule(voicing.VolleyImpact, VolleyVolume, SfxKind.Priority, VolleyFlightSeconds);
            };
        }

        private void OnAttacked(Enemy enemy)
        {
            Voicing voicing = VoicingFor(enemy);
            bool boss = enemy.Definition.IsBoss;
            _player.Play(voicing.Attacks, boss ? BossAttackVolume : AttackVolume, boss ? SfxKind.Hit : SfxKind.Creature);
            if (voicing.Impacts.HasSound)
            {
                // The sting or bolt lands after its flight; the attack range is as far as it ever flies.
                _player.Schedule(voicing.Impacts, ImpactVolume, SfxKind.Creature, enemy.Definition.AttackRange / BoltSpeed);
            }
        }

        private void OnSlowed()
        {
            float now = Time.unscaledTime;
            if (now - _lastFreeze < FreezeMinInterval)
            {
                return;
            }

            _lastFreeze = now;
            _player.Play(_player.Freezes, FreezeVolume, SfxKind.Hit);
        }

        public void OnKilled(Enemy enemy)
        {
            Voicing voicing = VoicingFor(enemy);
            bool boss = enemy.Definition.IsBoss;
            if (!boss && !TakeDeathTurn(voicing, Time.unscaledTime))
            {
                return;
            }

            _player.Play(voicing.Screeches, (boss ? BossScreechVolume : ScreechVolume) * voicing.DeathVolume, boss ? SfxKind.Priority : SfxKind.Kill);
            if (enemy.Definition.Element == Element.Water)
            {
                _player.Play(_steam, SteamVolume, SfxKind.Kill);
            }
        }

        /// <summary>Whether a species whose last death cry sounded at <paramref name="last"/> may cry again at <paramref name="now"/>.</summary>
        public static bool DeathCryDue(float now, float last, float speciesInterval) => now - last >= Mathf.Max(DeathMinInterval, speciesInterval);

        private static bool TakeDeathTurn(Voicing voicing, float now)
        {
            if (!DeathCryDue(now, voicing.LastDeath, voicing.DeathInterval))
            {
                return false;
            }

            voicing.LastDeath = now;
            return true;
        }

        /// <summary>Movement loops and steps for this frame. The filler gain already holds zoom, duck and mix.</summary>
        public void Update(float unscaledDeltaTime, float fillerGain)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].MoveWeight = 0f;
                _active[i].RollWeight = 0f;
            }

            _active.Clear();
            for (int i = 0; i < _living.Count; i++)
            {
                Enemy enemy = _living[i];
                if (!enemy.IsAlive || !_trackers.TryGetValue(enemy, out Tracker tracker))
                {
                    continue;
                }

                float moved = enemy.PathProgress - tracker.Progress;
                tracker.Progress = enemy.PathProgress;
                if (moved <= 0f)
                {
                    continue;
                }

                Voicing voicing = VoicingFor(enemy);
                bool boss = enemy.Definition.IsBoss;
                float weight = boss ? CrowdForFullLoop : 1f;
                if (voicing.MoveWeight <= 0f && voicing.RollWeight <= 0f)
                {
                    _active.Add(voicing);
                }

                if (enemy.IsRolling && voicing.Roll != null)
                {
                    voicing.RollWeight += weight;
                    continue;
                }

                voicing.MoveWeight += weight;
                if (!voicing.Steps.HasSound)
                {
                    continue;
                }

                tracker.SinceStep += moved;
                float spacing = voicing.StepSpacing * (boss ? BossStepScale : 1f);
                if (tracker.SinceStep < spacing)
                {
                    continue;
                }

                tracker.SinceStep = 0f;
                // A crowd of one species shares a stride: no more than one step of it every tenth of a second.
                if (now - voicing.LastStep < StepMinInterval)
                {
                    continue;
                }

                voicing.LastStep = now;
                _player.Play(voicing.Steps, boss ? BossStepVolume : StepVolume, SfxKind.Creature);
            }

            foreach (KeyValuePair<CreatureVoice, Voicing> pair in _voicings)
            {
                TickLoops(pair.Value, unscaledDeltaTime, fillerGain, pair.Value.IsBoss);
            }

            TickLoops(_genericFlyer, unscaledDeltaTime, fillerGain, false);
            TickLoops(_genericBossFlyer, unscaledDeltaTime, fillerGain, true);
        }

        private static void TickLoops(Voicing voicing, float dt, float fillerGain, bool boss)
        {
            if (voicing.Move != null)
            {
                voicing.Move.Target = Level(voicing.MoveWeight, boss) * voicing.Presence;
                voicing.Move.Tick(dt, fillerGain);
            }

            if (voicing.Roll != null)
            {
                voicing.Roll.Target = Level(voicing.RollWeight, boss) * voicing.Presence;
                voicing.Roll.Tick(dt, fillerGain);
            }
        }

        // One creature is heard at half the cap; a crowd of four fills it and more never gets louder.
        private static float Level(float weight, bool boss)
        {
            if (weight <= 0f)
            {
                return 0f;
            }

            float cap = boss ? BossMoveLoopCap : MoveLoopCap;
            return cap * Mathf.Min(1f, Mathf.Sqrt(weight / CrowdForFullLoop));
        }
    }
}
