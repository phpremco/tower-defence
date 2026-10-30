using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Generated sound effects, so the game has punchy audio before real sound files exist. Every clip is built
    /// from a few layers (tones with pitch sweeps, filtered noise, crackle, echo, soft clipping) on a tiny synth
    /// and cached by name. Replace a clip with imported audio later by changing where SfxPlayer gets it.
    /// </summary>
    public static class ProceduralSfx
    {
        public const int SampleRate = Synth.SampleRate;

        private const float AmbienceSeconds = 9f;
        private const float AmbienceCrossfade = 1.2f;

        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip PulseShot() => Cached("PulseShot", s =>
        {
            s.Tone(0f, 0.1f, 1600f, 420f, 0.5f, 0.004f, SynthWave.Square);
            s.Noise(0f, 0.04f, 0.35f, 0.002f, 6000f, 1500f);
            s.Tone(0f, 0.08f, 240f, 120f, 0.25f, 0.004f, SynthWave.Sine);
            s.Distort(1.5f);
            s.Normalize(0.8f);
        }, 0.14f);

        public static AudioClip HeavyShot() => Cached("HeavyShot", s =>
        {
            s.Noise(0f, 0.3f, 0.9f, 0.002f, 3500f, 200f);
            s.Tone(0f, 0.45f, 95f, 38f, 0.9f, 0.003f, SynthWave.Sine);
            s.Crackle(0.02f, 0.25f, 0.4f, 0.02f);
            s.Echo(0.06f, 0.35f, 2);
            s.Distort(2.2f);
            s.Normalize(0.95f);
        }, 0.6f);

        public static AudioClip RocketLaunch() => Cached("RocketLaunch", s =>
        {
            s.Noise(0f, 0.55f, 0.7f, 0.15f, 600f, 4500f);
            s.Tone(0f, 0.5f, 180f, 520f, 0.25f, 0.1f, SynthWave.Saw);
            s.Normalize(0.75f);
        }, 0.6f);

        public static AudioClip CryoShot() => Cached("CryoShot", s =>
        {
            s.Tone(0f, 0.3f, 700f, 1900f, 0.4f, 0.02f, SynthWave.Sine);
            s.Tone(0.02f, 0.25f, 1400f, 2800f, 0.25f, 0.02f, SynthWave.Sine);
            s.Noise(0f, 0.3f, 0.2f, 0.01f, 9000f, 3000f);
            s.Normalize(0.7f);
        }, 0.35f);

        public static AudioClip LaserShot() => Cached("LaserShot", s =>
        {
            s.Tone(0f, 0.22f, 2400f, 900f, 0.45f, 0.004f, SynthWave.Saw);
            s.Tone(0f, 0.2f, 4800f, 1800f, 0.2f, 0.004f, SynthWave.Sine);
            s.Noise(0f, 0.08f, 0.25f, 0.002f, 9000f, 3000f);
            s.Echo(0.04f, 0.3f, 2);
            s.Normalize(0.75f);
        }, 0.3f);

        public static AudioClip TeslaShot() => Cached("TeslaShot", s =>
        {
            s.Crackle(0f, 0.22f, 0.9f, 0.08f);
            s.Noise(0f, 0.18f, 0.5f, 0.002f, 6000f, 1200f);
            s.Tone(0f, 0.2f, 180f, 120f, 0.35f, 0.003f, SynthWave.Square);
            s.Distort(2.5f);
            s.Normalize(0.8f);
        }, 0.3f);

        public static AudioClip RailShot() => Cached("RailShot", s =>
        {
            s.Tone(0f, 0.25f, 300f, 3200f, 0.35f, 0.05f, SynthWave.Sine);
            s.Noise(0.2f, 0.35f, 1f, 0.002f, 4000f, 200f);
            s.Tone(0.2f, 0.5f, 110f, 40f, 0.9f, 0.003f, SynthWave.Sine);
            s.Echo(0.07f, 0.4f, 3);
            s.Distort(2.4f);
            s.Normalize(0.95f);
        }, 0.85f);

        public static AudioClip Explosion() => Cached("Explosion", s => Boom(s, 1f), 1.1f);

        public static AudioClip BigExplosion() => Cached("BigExplosion", s => Boom(s, 1.5f), 1.5f);

        /// <summary>The base going up: a long, deep blast with a second rumble.</summary>
        public static AudioClip BaseExplosion() => Cached("BaseExplosion", s =>
        {
            Boom(s, 2.2f);
            s.Noise(0.9f, 1.4f, 0.5f, 0.1f, 900f, 60f);
            s.Tone(0.8f, 1.6f, 48f, 22f, 0.8f, 0.05f, SynthWave.Sine);
            s.Normalize(1f);
        }, 3f);

        public static AudioClip EnemyDeath() => Cached("EnemyDeath", s =>
        {
            s.Noise(0f, 0.2f, 0.8f, 0.003f, 1800f, 350f);
            s.Tone(0f, 0.25f, 420f, 110f, 0.5f, 0.004f, SynthWave.Sine);
            s.Tone(0.05f, 0.035f, 2200f, 1700f, 0.25f, 0.005f, SynthWave.Sine);
            s.Tone(0.11f, 0.035f, 2400f, 1800f, 0.22f, 0.005f, SynthWave.Sine);
            s.Tone(0.18f, 0.035f, 2000f, 1500f, 0.2f, 0.005f, SynthWave.Sine);
            s.Normalize(0.8f);
        }, 0.45f);

        public static AudioClip BossDeath() => Cached("BossDeath", s =>
        {
            s.Tone(0f, 1.4f, 160f, 45f, 0.8f, 0.05f, SynthWave.Saw);
            s.Tone(0f, 1.3f, 80f, 30f, 0.7f, 0.02f, SynthWave.Sine);
            s.Noise(0f, 1.2f, 0.6f, 0.01f, 1800f, 150f);
            s.Crackle(0.1f, 1f, 0.4f, 0.01f);
            s.Echo(0.12f, 0.45f, 3);
            s.Distort(2.5f);
            s.Normalize(1f);
        }, 1.9f);

        public static AudioClip BossRoar() => Cached("BossRoar", s =>
        {
            s.Tone(0f, 0.6f, 90f, 140f, 0.6f, 0.15f, SynthWave.Saw);
            s.Tone(0.5f, 0.8f, 140f, 60f, 0.6f, 0.05f, SynthWave.Saw);
            s.Tone(0f, 1.2f, 55f, 35f, 0.4f, 0.1f, SynthWave.Square);
            s.Noise(0f, 1.2f, 0.35f, 0.1f, 900f, 300f);
            s.Distort(3f);
            s.Echo(0.11f, 0.4f, 3);
            s.Normalize(0.95f);
        }, 1.7f);

        public static AudioClip BaseHit() => Cached("BaseHit", s =>
        {
            s.Tone(0f, 0.5f, 90f, 40f, 0.9f, 0.003f, SynthWave.Sine);
            s.Noise(0f, 0.3f, 0.8f, 0.002f, 3000f, 200f);
            s.Tone(0.05f, 0.2f, 880f, 880f, 0.3f, 0.01f, SynthWave.Square, 0.3f);
            s.Tone(0.3f, 0.2f, 660f, 660f, 0.3f, 0.01f, SynthWave.Square, 0.3f);
            s.Tone(0.55f, 0.2f, 880f, 880f, 0.25f, 0.01f, SynthWave.Square, 0.3f);
            s.Distort(2f);
            s.Normalize(1f);
        }, 0.9f);

        public static AudioClip WaveHorn() => Cached("WaveHorn", s =>
        {
            s.Tone(0f, 1f, 190f, 200f, 0.45f, 0.12f, SynthWave.Saw, 0.6f);
            s.Tone(0.18f, 0.75f, 290f, 300f, 0.35f, 0.1f, SynthWave.Saw, 0.6f);
            s.Tone(0f, 1f, 98f, 100f, 0.3f, 0.1f, SynthWave.Sine, 0.6f);
            s.Distort(1.6f);
            s.Echo(0.15f, 0.3f, 2);
            s.Normalize(0.75f);
        }, 1.5f);

        public static AudioClip Build() => Cached("Build", s =>
        {
            s.Tone(0f, 0.12f, 520f, 180f, 0.6f, 0.002f, SynthWave.Sine);
            s.Noise(0f, 0.05f, 0.5f, 0.002f, 4000f, 800f);
            s.Tone(0.09f, 0.12f, 380f, 140f, 0.5f, 0.002f, SynthWave.Sine);
            s.Normalize(0.7f);
        }, 0.25f);

        public static AudioClip Upgrade() => Cached("Upgrade", s =>
        {
            Chime(s, 0f, 660f);
            Chime(s, 0.1f, 880f);
            Chime(s, 0.2f, 1320f);
            s.Normalize(0.6f);
        }, 0.55f);

        public static AudioClip Sell() => Cached("Sell", s =>
        {
            s.Tone(0f, 0.1f, 1760f, 1760f, 0.5f, 0.003f, SynthWave.Sine);
            s.Tone(0.07f, 0.12f, 2200f, 2200f, 0.4f, 0.003f, SynthWave.Sine);
            s.Tone(0.15f, 0.15f, 1320f, 1320f, 0.4f, 0.003f, SynthWave.Sine);
            s.Normalize(0.55f);
        }, 0.35f);

        public static AudioClip Repair() => Cached("Repair", s =>
        {
            s.Tone(0f, 0.05f, 900f, 900f, 0.4f, 0.003f, SynthWave.Sine);
            s.Tone(0.08f, 0.05f, 900f, 900f, 0.4f, 0.003f, SynthWave.Sine);
            s.Tone(0.15f, 0.25f, 300f, 700f, 0.35f, 0.05f, SynthWave.Saw, 0.5f);
            s.Normalize(0.6f);
        }, 0.45f);

        public static AudioClip HeroStrike() => Cached("HeroStrike", s =>
        {
            s.Noise(0f, 0.28f, 0.6f, 0.08f, 400f, 3000f);
            s.Tone(0f, 0.2f, 300f, 520f, 0.3f, 0.03f, SynthWave.Sine);
            s.Normalize(0.7f);
        }, 0.3f);

        public static AudioClip HeroBlaster() => Cached("HeroBlaster", s =>
        {
            s.Tone(0f, 0.12f, 1900f, 300f, 0.5f, 0.003f, SynthWave.Square);
            s.Tone(0f, 0.09f, 950f, 150f, 0.3f, 0.003f, SynthWave.Saw);
            s.Noise(0f, 0.05f, 0.3f, 0.002f, 7000f, 2000f);
            s.Distort(1.6f);
            s.Normalize(0.75f);
        }, 0.16f);

        public static AudioClip HeroDown() => Cached("HeroDown", s =>
        {
            s.Tone(0f, 0.9f, 520f, 40f, 0.45f, 0.01f, SynthWave.Saw);
            s.Tone(0f, 0.7f, 260f, 30f, 0.3f, 0.01f, SynthWave.Square);
            s.Noise(0.55f, 0.35f, 0.9f, 0.002f, 2500f, 150f);
            s.Tone(0.55f, 0.4f, 110f, 45f, 0.9f, 0.003f, SynthWave.Sine);
            s.Crackle(0.6f, 0.6f, 0.5f, 0.02f);
            s.Echo(0.08f, 0.35f, 2);
            s.Distort(2f);
            s.Normalize(1f);
        }, 1.3f);

        public static AudioClip HeroRevive() => Cached("HeroRevive", s =>
        {
            s.Tone(0f, 0.7f, 90f, 1400f, 0.4f, 0.05f, SynthWave.Saw);
            s.Noise(0f, 0.5f, 0.25f, 0.1f, 600f, 5000f);
            Chime(s, 0.45f, 880f);
            Chime(s, 0.6f, 1320f);
            Chime(s, 0.75f, 1760f);
            s.Echo(0.1f, 0.3f, 2);
            s.Normalize(0.85f);
        }, 1.2f);

        public static AudioClip Shockwave() => Cached("Shockwave", s =>
        {
            s.Tone(0f, 0.6f, 60f, 30f, 0.9f, 0.003f, SynthWave.Sine);
            s.Tone(0f, 0.4f, 120f, 900f, 0.35f, 0.02f, SynthWave.Sine);
            s.Noise(0f, 0.5f, 0.6f, 0.003f, 1500f, 200f);
            s.Echo(0.08f, 0.35f, 2);
            s.Distort(2f);
            s.Normalize(0.95f);
        }, 0.75f);

        public static AudioClip DroneBuy() => Cached("DroneBuy", s =>
        {
            s.Tone(0f, 0.08f, 880f, 880f, 0.4f, 0.003f, SynthWave.Sine);
            s.Tone(0.1f, 0.1f, 1320f, 1320f, 0.4f, 0.003f, SynthWave.Sine);
            s.Noise(0.05f, 0.25f, 0.2f, 0.1f, 800f, 3000f);
            s.Normalize(0.6f);
        }, 0.32f);

        public static AudioClip StructureHit() => Cached("StructureHit", s =>
        {
            s.Noise(0f, 0.12f, 0.7f, 0.002f, 1500f, 300f);
            s.Tone(0f, 0.15f, 180f, 70f, 0.6f, 0.003f, SynthWave.Sine);
            s.Normalize(0.75f);
        }, 0.2f);

        /// <summary>The looping sound bed of a level, or null for a silent level.</summary>
        /// <summary>Two-tone siren that announces a boss wave.</summary>
        public static AudioClip BossAlarm() => Cached("BossAlarm", s =>
        {
            for (int i = 0; i < 4; i++)
            {
                float at = i * 0.55f;
                s.Tone(at, 0.28f, 520f, 540f, 0.5f, 0.02f, SynthWave.Square, 0.75f);
                s.Tone(at + 0.28f, 0.27f, 390f, 380f, 0.5f, 0.02f, SynthWave.Square, 0.75f);
            }

            s.Echo(0.12f, 0.35f, 2);
            s.LowPass(2600f);
            s.Normalize(0.75f);
        }, 2.6f);

        /// <summary>The ground shuddering under something very large: a long low swell with tremors.</summary>
        public static AudioClip QuakeRumble() => Cached("QuakeRumble", s =>
        {
            s.Rumble(0f, 4.5f, 1f, 70f);
            s.Tone(0.3f, 4f, 38f, 30f, 0.5f, 0.6f, SynthWave.Sine, 0.6f);
            s.Crackle(1f, 3f, 0.3f, 0.004f);
            s.Distort(1.8f);
            s.Normalize(0.95f);
        }, 4.6f);

        /// <summary>A boss shockwave hitting the field.</summary>
        public static AudioClip BossPulse() => Cached("BossPulse", s =>
        {
            s.Tone(0f, 0.9f, 60f, 24f, 1f, 0.004f, SynthWave.Sine);
            s.Tone(0f, 0.35f, 900f, 120f, 0.4f, 0.004f, SynthWave.Saw);
            s.Noise(0f, 0.7f, 0.8f, 0.002f, 1800f, 90f);
            s.Echo(0.1f, 0.45f, 3);
            s.Distort(2.2f);
            s.Normalize(1f);
        }, 1.3f);

        /// <summary>The hum of a boss laser, built to loop while the beam sweeps.</summary>
        public static AudioClip BossLaserHum() => Cached("BossLaserHum", s =>
        {
            s.Drone(110f, 0.35f, 2.5f);
            s.Drone(220f, 0.2f, 3.1f);
            s.Wind(0.25f, 3000f, 0.5f, 4f);
            s.Loopable(0.3f);
            s.Normalize(0.6f);
        }, 1.6f);

        /// <summary>The hum of a laser tower firing, looped as long as the shots keep coming.</summary>
        public static AudioClip LaserHum() => Cached("LaserHum", s =>
        {
            s.Drone(1200f, 0.3f, 6f);
            s.Drone(2400f, 0.12f, 7f);
            s.Wind(0.15f, 6000f, 0.4f, 5f);
            s.Loopable(0.2f);
            s.Normalize(0.5f);
        }, 0.9f);

        /// <summary>A boss pushing fresh creatures out of its body.</summary>
        public static AudioClip MinionBurst() => Cached("MinionBurst", s =>
        {
            s.Noise(0f, 0.4f, 0.6f, 0.01f, 1200f, 300f);
            s.Tone(0.05f, 0.35f, 260f, 110f, 0.4f, 0.01f, SynthWave.Saw);
            s.Crackle(0.02f, 0.3f, 0.5f, 0.02f);
            s.Normalize(0.75f);
        }, 0.5f);

        /// <summary>A creature breaking the surface of water.</summary>
        public static AudioClip EmergeWater() => Cached("EmergeWater", s =>
        {
            s.Noise(0f, 0.5f, 0.7f, 0.03f, 900f, 4000f);
            s.Tone(0.02f, 0.25f, 180f, 90f, 0.4f, 0.01f, SynthWave.Sine);
            s.Crackle(0.1f, 0.5f, 0.25f, 0.01f);
            s.Normalize(0.7f);
        }, 0.6f);

        /// <summary>A creature bursting out of lava: a hiss and a deep splash.</summary>
        public static AudioClip EmergeLava() => Cached("EmergeLava", s =>
        {
            s.Tone(0f, 0.6f, 90f, 40f, 0.7f, 0.01f, SynthWave.Sine);
            s.Hiss(0.05f, 0.7f, 0.5f, 2f);
            s.Crackle(0.1f, 0.6f, 0.5f, 0.012f);
            s.Distort(1.6f);
            s.Normalize(0.8f);
        }, 0.8f);

        /// <summary>A creature digging up through sand.</summary>
        public static AudioClip EmergeSand() => Cached("EmergeSand", s =>
        {
            s.Noise(0f, 0.6f, 0.6f, 0.08f, 500f, 1500f, 1.5f);
            s.Hiss(0.1f, 0.5f, 0.3f, 2f);
            s.Normalize(0.65f);
        }, 0.7f);

        /// <summary>Ground cracking open as something large climbs out.</summary>
        public static AudioClip EmergeGround() => Cached("EmergeGround", s =>
        {
            s.Tone(0f, 0.9f, 55f, 30f, 0.9f, 0.01f, SynthWave.Sine);
            s.Noise(0f, 0.8f, 0.9f, 0.003f, 2000f, 150f);
            s.Crackle(0.05f, 0.9f, 0.7f, 0.02f);
            s.Echo(0.11f, 0.4f, 3);
            s.Distort(2.2f);
            s.Normalize(1f);
        }, 1.4f);

        /// <summary>A boss drawing breath somewhere out of sight: a low, throaty growl that rises and breaks off.</summary>
        public static AudioClip BossGrowl() => Cached("BossGrowl", s =>
        {
            s.Tone(0f, 1.1f, 48f, 70f, 0.7f, 0.25f, SynthWave.Saw, 0.5f);
            s.Tone(0.05f, 1f, 97f, 140f, 0.35f, 0.3f, SynthWave.Square, 0.4f);
            s.Noise(0.1f, 1f, 0.3f, 0.2f, 400f, 900f);
            s.Crackle(0.2f, 0.8f, 0.25f, 0.015f);
            s.Distort(2.6f);
            s.LowPass(1400f);
            s.Echo(0.14f, 0.35f, 2);
            s.Normalize(0.8f);
        }, 1.6f);

        /// <summary>Something large stirring under water: slow bubbles breaking the surface over a deep swell.</summary>
        public static AudioClip BossBubbles() => Cached("BossBubbles", s =>
        {
            float[] times = { 0f, 0.22f, 0.5f, 0.68f, 1.05f, 1.3f };
            for (int i = 0; i < times.Length; i++)
            {
                float hz = 180f + (i * 37) % 90;
                s.Tone(times[i], 0.16f, hz, hz * 1.8f, 0.5f, 0.005f, SynthWave.Sine);
            }

            s.Noise(0f, 1.5f, 0.2f, 0.2f, 600f, 300f);
            s.Rumble(0f, 1.5f, 0.5f, 90f);
            s.Normalize(0.7f);
        }, 1.6f);

        /// <summary>Lava heaving before something climbs out: thick glugs and a hiss of steam.</summary>
        public static AudioClip BossLavaChurn() => Cached("BossLavaChurn", s =>
        {
            float[] times = { 0f, 0.35f, 0.6f, 1f };
            for (int i = 0; i < times.Length; i++)
            {
                s.Tone(times[i], 0.3f, 70f + i * 10f, 40f, 0.6f, 0.01f, SynthWave.Sine);
            }

            s.Hiss(0.2f, 1.2f, 0.25f, 2f);
            s.Crackle(0f, 1.4f, 0.35f, 0.01f);
            s.Rumble(0f, 1.5f, 0.6f, 80f);
            s.Distort(1.5f);
            s.Normalize(0.75f);
        }, 1.6f);

        /// <summary>Sand pouring into a hollow as something burrows up underneath it.</summary>
        public static AudioClip BossSandShift() => Cached("BossSandShift", s =>
        {
            s.Noise(0f, 1.3f, 0.5f, 0.3f, 300f, 1200f, 1.2f);
            s.Hiss(0.3f, 0.9f, 0.25f, 2f);
            s.Rumble(0f, 1.4f, 0.5f, 70f);
            s.Normalize(0.65f);
        }, 1.5f);

        /// <summary>A roller tucking in and taking off, a quick clack and a rumble.</summary>
        public static AudioClip RollerCurl() => Cached("RollerCurl", s =>
        {
            s.Crackle(0f, 0.15f, 0.6f, 0.05f);
            s.Tone(0.05f, 0.4f, 140f, 70f, 0.45f, 0.01f, SynthWave.Saw);
            s.Noise(0.08f, 0.35f, 0.3f, 0.02f, 700f, 200f);
            s.Normalize(0.65f);
        }, 0.5f);

        public static AudioClip Ambience(AmbienceKind kind)
        {
            switch (kind)
            {
                case AmbienceKind.Wind:
                    return Cached("AmbienceWind", s =>
                    {
                        s.Wind(0.6f, 450f, 0.7f, 0.11f);
                        s.Wind(0.3f, 900f, 0.6f, 0.07f);
                        s.Loopable(AmbienceCrossfade);
                        s.Normalize(0.5f);
                    }, AmbienceSeconds + AmbienceCrossfade);
                case AmbienceKind.Forest:
                    return Cached("AmbienceForest", s =>
                    {
                        s.Wind(0.3f, 300f, 0.5f, 0.09f);
                        s.Drone(65f, 0.12f, 0.08f);
                        for (int i = 0; i < 9; i++)
                        {
                            float at = s.Chance() * AmbienceSeconds;
                            float hz = 1800f + s.Chance() * 1400f;
                            s.Tone(at, 0.06f, hz, hz * 1.15f, 0.12f, 0.01f, SynthWave.Sine);
                        }

                        s.Loopable(AmbienceCrossfade);
                        s.Normalize(0.5f);
                    }, AmbienceSeconds + AmbienceCrossfade);
                case AmbienceKind.Lava:
                    return Cached("AmbienceLava", s =>
                    {
                        s.Wind(0.9f, 120f, 0.4f, 0.05f);
                        s.Crackle(0f, AmbienceSeconds + AmbienceCrossfade, 0.25f, 0.0008f);
                        for (int i = 0; i < 12; i++)
                        {
                            float at = s.Chance() * AmbienceSeconds;
                            s.Tone(at, 0.09f, 300f, 90f, 0.3f, 0.004f, SynthWave.Sine);
                        }

                        s.Loopable(AmbienceCrossfade);
                        s.Normalize(0.55f);
                    }, AmbienceSeconds + AmbienceCrossfade);
                case AmbienceKind.Ruins:
                    return Cached("AmbienceRuins", s =>
                    {
                        s.Wind(0.35f, 500f, 0.8f, 0.06f);
                        s.Drone(55f, 0.15f, 0.05f);
                        s.Drone(82.5f, 0.08f, 0.07f);
                        for (int i = 0; i < 4; i++)
                        {
                            float at = s.Chance() * AmbienceSeconds;
                            s.Tone(at, 0.15f, 2400f, 2300f, 0.08f, 0.002f, SynthWave.Sine);
                        }

                        s.Loopable(AmbienceCrossfade);
                        s.Normalize(0.5f);
                    }, AmbienceSeconds + AmbienceCrossfade);
                case AmbienceKind.Hive:
                    return Cached("AmbienceHive", s =>
                    {
                        s.Throb(58f, 1.4f, 0.5f);
                        s.Wind(0.3f, 350f, 0.6f, 0.25f);
                        for (int i = 0; i < 10; i++)
                        {
                            float at = s.Chance() * AmbienceSeconds;
                            s.Noise(at, 0.05f + s.Chance() * 0.06f, 0.2f, 0.01f, 2500f, 1200f);
                        }

                        s.Loopable(AmbienceCrossfade);
                        s.Normalize(0.55f);
                    }, AmbienceSeconds + AmbienceCrossfade);
                default:
                    return null;
            }
        }

        private static void Boom(Synth s, float size)
        {
            s.Tone(0f, 0.7f * size, 75f / size, 28f / size, 1f, 0.004f, SynthWave.Sine);
            s.Noise(0f, 0.6f * size, 1f, 0.002f, 2500f, 120f, 2.5f);
            s.Crackle(0.05f, 0.6f * size, 0.5f, 0.015f);
            s.Noise(0.2f, 0.7f * size, 0.3f, 0.05f, 500f, 100f);
            s.Echo(0.09f, 0.4f, 3);
            s.Distort(2.5f);
            s.Normalize(1f);
        }

        private static void Chime(Synth s, float at, float hz)
        {
            s.Tone(at, 0.3f, hz, hz, 0.45f, 0.005f, SynthWave.Sine);
            s.Tone(at, 0.2f, hz * 2f, hz * 2f, 0.15f, 0.005f, SynthWave.Sine);
        }

        private static AudioClip Cached(string name, System.Action<Synth> build, float seconds)
        {
            if (Cache.TryGetValue(name, out AudioClip clip) && clip != null)
            {
                return clip;
            }

            var synth = new Synth(seconds, name.GetHashCode());
            build(synth);
            clip = synth.ToClip(name);
            Cache[name] = clip;
            return clip;
        }
    }
}
