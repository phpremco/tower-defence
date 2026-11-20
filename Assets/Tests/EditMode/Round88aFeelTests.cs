using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88a, the findings of Remco's playtest of 2026-11-20 on sound, the radar and the Chrono Field: a sound with a
    /// swell is timed to the moment on screen (the shockwave), the beam towers hum as a choir that does not grow with their
    /// number (the laser), a surprise pings once per group, and a Chrono Field lasts a wave and pays against a gun over it.
    /// </summary>
    public sealed class Round88aFeelTests
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const float Tolerance = 0.0001f;

        // ---- a sound's blow lands on the moment on screen ---------------------------------------------------------

        [Test]
        public void TheBlowOfASwellingSound_IsFoundWhereItReachesItsLevel()
        {
            const int frequency = 1000;
            // Silence for 0.1 s, a ramp to full over 0.2 s, then full for 0.2 s.
            var samples = new float[500];
            for (int i = 0; i < samples.Length; i++)
            {
                float level = i < 100 ? 0f : i < 300 ? (i - 100) / 200f : 1f;
                samples[i] = (i % 2 == 0 ? 1f : -1f) * level;
            }

            float blow = SoundImpact.Seconds(samples, 1, frequency);
            // 70 % of the level lies 0.14 s up the ramp, read in windows of 0.01 s.
            Assert.AreEqual(0.24f, blow, 0.011f);
            Assert.AreEqual(blow, SoundImpact.Seconds(Interleave(samples), 2, frequency), Tolerance, "two channels read as one");

            var bang = new float[200];
            bang[0] = 1f;
            bang[1] = -1f;
            Assert.AreEqual(0f, SoundImpact.Seconds(bang, 1, frequency), Tolerance, "a sound that hits at once has no swell");
            Assert.AreEqual(0f, SoundImpact.Seconds(new float[200], 1, frequency), Tolerance, "silence has no blow");
        }

        private static float[] Interleave(float[] mono)
        {
            var stereo = new float[mono.Length * 2];
            for (int i = 0; i < mono.Length; i++)
            {
                stereo[i * 2] = mono[i];
                stereo[i * 2 + 1] = mono[i];
            }

            return stereo;
        }

        [Test]
        public void ASwellingSound_StartsAheadOfItsMoment_OrPartWayIn_NeverLate()
        {
            // Lead to spare: the start waits, nothing is skipped; the blow lands on the moment minus the device's delay.
            SoundImpact.Plan(0.2f, 1f, 0.5f, 0.05f, out float delay, out float skip);
            Assert.AreEqual(0.25f, delay, Tolerance);
            Assert.AreEqual(0f, skip, Tolerance);

            // The hero's pulse: 0.19 s of swell, the wave leaves after 0.1 s: the recording starts 0.14 s in.
            SoundImpact.Plan(0.19f, 1f, 0.1f, 0.05f, out delay, out skip);
            Assert.AreEqual(0f, delay, Tolerance);
            Assert.AreEqual(0.14f, skip, Tolerance);
            // What is left of the swell plus the device's delay is exactly the lead: the blow is heard with the wave.
            Assert.AreEqual(0.1f, (0.19f - skip) + 0.05f, Tolerance);

            // No lead at all: it starts at its blow, never past it.
            SoundImpact.Plan(0.35f, 1f, 0f, 0.05f, out delay, out skip);
            Assert.AreEqual(0f, delay, Tolerance);
            Assert.AreEqual(0.35f, skip, Tolerance);

            // A sound played slower swells longer in real time.
            SoundImpact.Plan(0.2f, 0.5f, 1f, 0f, out delay, out skip);
            Assert.AreEqual(0.6f, delay, Tolerance);

            // A sound that hits at once is never delayed past its moment and never skipped.
            SoundImpact.Plan(0f, 1f, 0f, 0.05f, out delay, out skip);
            Assert.AreEqual(0f, delay, Tolerance);
            Assert.AreEqual(0f, skip, Tolerance);
        }

        [Test]
        public void ThePowerPulses_CarryTheirMeasuredSwell_InTheData()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>($"{DataRoot}/Audio/SfxLibrary.asset");
            Assert.IsNotNull(library, "no sound library");
            Assert.IsFalse(library.HeroPulses.IsEmpty, "no hero pulse recording");
            Assert.Greater(library.HeroPulses.Impact, 0.05f, "the hero pulse swells before it hits; its blow must be measured (ApplyRound88)");
            Assert.Less(library.HeroPulses.Impact, 0.5f);
            Assert.IsFalse(library.RadarPings.IsEmpty, "the radar ping has no recording");
            Assert.Less(library.RadarPings.Impact, 0.05f, "a ping hits at once");

            var voice = AssetDatabase.LoadAssetAtPath<CreatureVoice>($"{DataRoot}/Audio/Voices/Voice_CoreWisp.asset");
            Assert.IsNotNull(voice, "no Core Wisp voice");
            Assert.Greater(voice.Pulse.Impact, 0.2f, "the Core Wisp's pulse swells for a third of a second; its blow must be measured");
        }

        // ---- the hum of the beam towers --------------------------------------------------------------------------

        [Test]
        public void TheBeamChoir_KeepsThePowerOfOneBeam_HoweverManyHum()
        {
            var hum = new BeamHum(4, 0.3f, 1.5f);
            Assert.AreEqual(0, hum.Active);
            hum.Touch(new Vector3(0f, 0f, 0f), 10f);
            Assert.AreEqual(1, hum.Active);
            Assert.AreEqual(1f, hum.Level(0), Tolerance, "one beam opens at its full level");

            for (int towers = 2; towers <= 4; towers++)
            {
                hum.Touch(new Vector3(towers * 5f, 0f, 0f), 10f);
                Assert.AreEqual(towers, hum.Active);
                float power = 0f;
                for (int v = 0; v < hum.Voices; v++)
                {
                    power += hum.Level(v) * hum.Level(v);
                }

                Assert.AreEqual(1f, power, 0.001f, $"{towers} beams must carry the power of one");
            }

            // A fifth tower finds every voice taken and stays silent; the four keep their voices.
            hum.Touch(new Vector3(100f, 0f, 0f), 10f);
            Assert.AreEqual(4, hum.Active);
        }

        [Test]
        public void ABeamThatKeepsFiring_SettlesToABed_AndOpensAtFullLevelAgainAfterASilence()
        {
            var hum = new BeamHum(4, 0.3f, 1.5f);
            var tower = new Vector3(3f, 0f, 3f);
            float now = 0f;
            hum.Touch(tower, now);
            // Fifteen pulses a second for ten seconds, pulses a little apart in space (the muzzle moves as the gun turns).
            while (now < 10f)
            {
                now += 1f / 15f;
                hum.Touch(tower + new Vector3(0.2f, 0f, 0f), now);
            }

            Assert.AreEqual(1, hum.Active, "pulses of one tower hold one voice");
            Assert.AreEqual(BeamHum.SustainShare, hum.Level(0), Tolerance, "a long burst settles to the bed");

            // Half a second in it is still at the opening level, two seconds in on its way down.
            var fresh = new BeamHum(4, 0.3f, 1.5f);
            for (float t = 0f; t <= 2f; t += 0.1f)
            {
                fresh.Touch(tower, t);
                if (t <= BeamHum.OpeningSeconds)
                {
                    Assert.AreEqual(1f, fresh.Level(0), Tolerance);
                }
            }

            Assert.Less(fresh.Level(0), 1f);
            Assert.Greater(fresh.Level(0), BeamHum.SustainShare);

            // The tower stops: the voice is released after the hold; firing again opens at the full level.
            hum.Step(now + 0.31f);
            Assert.AreEqual(0, hum.Active);
            Assert.AreEqual(0f, hum.Level(0), Tolerance);
            hum.Touch(tower, now + 2f);
            Assert.AreEqual(1f, hum.Level(0), Tolerance);
        }

        [Test]
        public void TheVoicesOfTheChoir_AreTunedApart_AndStartApartInTheLoop()
        {
            var hum = new BeamHum(4, 0.3f, 1.5f);
            var pitches = new HashSet<int>();
            var phases = new HashSet<int>();
            for (int v = 0; v < hum.Voices; v++)
            {
                for (float t = 0f; t < 8f; t += 0.25f)
                {
                    hum.Step(t);
                    float pitch = hum.Pitch(v);
                    Assert.Greater(pitch, 0.9f, "a voice must stay the same sound");
                    Assert.Less(pitch, 1.1f, "a voice must stay the same sound");
                }

                hum.Step(0f);
                pitches.Add(Mathf.RoundToInt((hum.Pitch(v) - Mathf.Sin(v * 0.37f * Mathf.PI * 2f) * BeamHum.DriftDepth) * 1000f));
                phases.Add(Mathf.RoundToInt(hum.Phase(v) * 1000f));
            }

            Assert.AreEqual(hum.Voices, pitches.Count, "every voice has its own tuning");
            Assert.AreEqual(hum.Voices, phases.Count, "every voice starts elsewhere in the loop");
        }

        // ---- the radar's ping for a surprise ---------------------------------------------------------------------

        [Test]
        public void ASurprisePoint_PingsOncePerGroup_AndAMainPointNever()
        {
            var watch = new SurpriseWatch(new[] { false, true }, new[] { true, false });
            Assert.IsFalse(watch.PathSpawned(0, 0f), "the main path never pings");
            Assert.IsTrue(watch.PathSpawned(1, 0f), "the first creature down a surprise path pings");
            for (float t = 1f; t <= 20f; t += 1f)
            {
                Assert.IsFalse(watch.PathSpawned(1, t), "a stream that keeps coming pings once");
            }

            Assert.IsTrue(watch.PathSpawned(1, 20f + SurpriseWatch.QuietSeconds), "the next group after a silence pings again");
            Assert.IsTrue(watch.NestSpawned(0, 3f), "a surprise nest pings on its own clock");
            Assert.IsFalse(watch.NestSpawned(1, 3f), "a main nest never pings");
            Assert.IsFalse(watch.NestSpawned(7, 3f), "a nest the level does not have never pings");
            Assert.IsFalse(watch.PathSpawned(-1, 3f));
        }

        [Test]
        public void EveryLevel_WatchesItsSurprisePointsAndTheNestsBehindItsGate()
        {
            int watched = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { DataRoot + "/Levels" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                SurpriseWatch watch = SurpriseWatch.For(level);
                bool[] behind = RearNests.BehindGate(level);
                for (int p = 0; p < level.Paths.Length; p++)
                {
                    bool surprise = !level.Paths[p].Flying && level.RoleOfPath(p) == SpawnRole.Surprise;
                    Assert.AreEqual(surprise, watch.WatchesPath(p), $"{level.name}: path {p}");
                    watched += surprise ? 1 : 0;
                }

                for (int n = 0; n < level.Nests.Length; n++)
                {
                    bool surprise = level.RoleOfNest(n + 1) == SpawnRole.Surprise || behind[n];
                    Assert.AreEqual(surprise, watch.WatchesNest(n), $"{level.name}: nest {n + 1}");
                    watched += surprise ? 1 : 0;
                }
            }

            Assert.Greater(watched, 0, "no level has a surprise point to watch");
        }

        // ---- the Chrono Field lasts a wave and pays against a gun over it ------------------------------------------

        [Test]
        public void TheChronoField_OutlastsTheLongestGroundWave_AndPaysAgainstTheCheapestGunOverIt()
        {
            var field = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>($"{DataRoot}/PathObjects/SlowField.asset");
            var gun = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{DataRoot}/Towers/PulseRifle.asset");
            Assert.IsNotNull(field, "no Chrono Field");
            Assert.IsNotNull(gun, "no Gun Turret");
            Assert.AreEqual(PathObjectKind.SlowField, field.Kind);
            Assert.AreEqual(Game.Editor.ProjectGenerator.ChronoFieldSeconds, field.Lifetime, Tolerance, "the asset and the generator disagree");

            // It lasts as long as the longest ground wave of the campaign keeps coming.
            float longest = 0f;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { DataRoot + "/Levels" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (!level.name.StartsWith("Level_"))
                {
                    continue;
                }

                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    foreach (WaveDefinition wave in set.Waves)
                    {
                        longest = Mathf.Max(longest, GroundSpan(wave, set.Difficulty.SpawnIntervalMultiplier));
                    }
                }
            }

            TestContext.WriteLine($"longest ground wave {longest:0.0} s, field {field.Lifetime:0} s");
            Assert.Greater(longest, 0f, "no ground wave found");
            Assert.GreaterOrEqual(field.Lifetime, longest, "a field laid for a wave must last that wave");

            // Over its life, in reach of two of the cheapest gun: each gun fires on a creature for the stretch it reaches
            // (two ranges); the field adds the extra seconds of the slowed stretch (its diameter). That gain, in guns, per
            // coin must be at least what a coin in the gun itself gives over the same time.
            float stretch = 2f * gun.GetLevel(0).Range;
            float gainPerGun = (1f / field.SlowFactor - 1f) * Mathf.Min(1f, 2f * field.Radius / stretch);
            float fieldGunsPerCoin = 2f * gainPerGun / field.Cost;
            float gunGunsPerCoin = 1f / gun.BuildCost;
            TestContext.WriteLine($"gain per gun x{1f + gainPerGun:0.00}; field {fieldGunsPerCoin * 50f:0.00} guns per 50 coins at two guns, a gun {gunGunsPerCoin * 50f:0.00}");
            Assert.GreaterOrEqual(fieldGunsPerCoin, gunGunsPerCoin, "at a two-gun defence the field must give at least what its coins in guns give while it lasts");
            // And it is no replacement for towers: at a three-gun defence it stays under two guns' worth per gun's price.
            Assert.Less(3f * gainPerGun / field.Cost, 2f * gunGunsPerCoin, "the field is too strong for its price");
        }

        private static float GroundSpan(WaveDefinition wave, float tempo)
        {
            float first = float.MaxValue;
            float last = 0f;
            foreach (SpawnGroup group in wave.Groups)
            {
                if (group.Enemy == null || group.Enemy.Flies)
                {
                    continue;
                }

                float start = group.Delay + group.Stagger * tempo;
                first = Mathf.Min(first, start);
                last = Mathf.Max(last, start + (group.Count - 1) * group.Interval * tempo);
            }

            return first < float.MaxValue ? last - first : 0f;
        }
    }
}
