using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// How tough the creatures of a wave are, as a formula (round 87; until then a solve against the paper ledger):
    /// <c>walkIn = DifficultyScale[d] x levelScalar x min(Ramp(phase) x Kind x Peak, WaveCeiling) x boss relief</c>, and a level's boss as a bar of
    /// its own, <c>BossBase x BossDifficulty[d] x (1 + BossLevelGrowth x level) x (BossEarlyShare + (1 - BossEarlyShare) x
    /// phase) x (flies ? FlyingBossShare : 1)</c>. Nothing here reads a purse, a defence or another wave, so a wave is
    /// what its place in the level says and the closed loop's one scalar per level moves every wave alike (and no boss).
    /// Pure numbers: <see cref="WaveSolver"/> applies them to a level's script.
    /// </summary>
    public static class WaveFormula
    {
        /// <summary>Where a wave stands in its level: 0 for the first wave, 1 for the last.</summary>
        public static float Phase(int number, int waves) => waves > 1 ? (number - 1) / (float)(waves - 1) : 0f;

        /// <summary>The rise over a level: geometric in the wave's phase, from <see cref="BalanceRules.RampStart"/> to <see cref="BalanceRules.RampEnd"/>.</summary>
        public static float Ramp(BalanceRules rules, float phase) =>
            (float)(Math.Pow(rules.RampStart, 1f - phase) * Math.Pow(rules.RampEnd, phase));

        /// <summary>What a wave's kind, a boss in it and a boss just before it do to its creatures.</summary>
        public static float KindFactor(BalanceRules rules, WaveComposer.Kind kind, bool bossWave, bool afterBoss) =>
            KindOnly(rules, kind) * Relief(rules, bossWave, afterBoss);

        /// <summary>The factor of the wave's kind alone: a swarm, a heavy wave, a raid.</summary>
        public static float KindOnly(BalanceRules rules, WaveComposer.Kind kind) =>
            kind == WaveComposer.Kind.Rush ? rules.RushFactor : kind == WaveComposer.Kind.Heavy ? rules.HeavyFactor : kind == WaveComposer.Kind.Air ? rules.AirFactor : 1f;

        /// <summary>What a boss in the wave and a boss just before it take off its creatures.</summary>
        public static float Relief(BalanceRules rules, bool bossWave, bool afterBoss) =>
            (bossWave ? rules.BossCrowdFactor : 1f) * (afterBoss ? rules.AfterBossFactor : 1f);

        /// <summary>
        /// Place, kind and peak together, under the ceiling (<see cref="BalanceRules.WaveCeiling"/>, round 88c): a late
        /// peak on a heavy wave is cut to it, everything below it is the plain product.
        /// </summary>
        public static float Shape(BalanceRules rules, float phase, WaveComposer.Kind kind, bool peak)
        {
            float shape = Ramp(rules, phase) * KindOnly(rules, kind) * (peak ? rules.PeakFactor : 1f);
            return rules.WaveCeiling > 0f ? Math.Min(shape, rules.WaveCeiling) : shape;
        }

        /// <summary>The multiplier the creatures of a wave walk in with.</summary>
        public static float WalkIn(BalanceRules rules, int difficulty, float levelScalar, float phase, WaveComposer.Kind kind, bool bossWave, bool afterBoss, bool peak) =>
            rules.DifficultyScaleFor(difficulty) * levelScalar * Shape(rules, phase, kind, peak) * Relief(rules, bossWave, afterBoss);

        /// <summary>
        /// The bar (body plus armour, as spawned) of a level's boss: by the level's index on its continent, the
        /// difficulty and how late in the level it comes. Not by the level's scalar and not by the creatures around it.
        /// </summary>
        public static float BossBar(BalanceRules rules, int continentIndex, int difficulty, float phase, bool flies) =>
            rules.BossBase * rules.BossDifficultyFor(difficulty) * (1f + rules.BossLevelGrowth * Math.Max(0, continentIndex))
            * (rules.BossEarlyShare + (1f - rules.BossEarlyShare) * phase) * (flies ? rules.FlyingBossShare : 1f);

        /// <summary>The phases a level of this many waves peaks at: the rule's, and one more for a long level.</summary>
        public static List<float> PeakPhases(BalanceRules rules, int waves)
        {
            var phases = new List<float>(rules.PeakPhases);
            if (waves >= rules.LongLevelWaves)
            {
                phases.Add(rules.LongLevelPeakPhase);
            }

            phases.Sort();
            return phases;
        }

        /// <summary>
        /// The peak waves of a level (1-based numbers, ascending). Every peak first lands on the wave nearest its phase
        /// that may carry one (<paramref name="open"/>: a ground wave without a boss, and not the breather after one), no
        /// two side by side: the wave after a peak is an ordinary one. Then a peak moves to a neighbour the player
        /// already reads as a threat (<paramref name="marked"/>: a surprise or a heavy wave) when that keeps it clear of
        /// the other peaks. In two steps, so that a surprise can draw a peak onto its own wave and never pushes one onto
        /// another wave: a surprise makes no other wave tougher.
        /// </summary>
        public static List<int> Peaks(BalanceRules rules, int waves, Func<int, bool> open, Func<int, bool> marked)
        {
            var peaks = new List<int>();
            var places = new List<float>();
            foreach (float phase in PeakPhases(rules, waves))
            {
                float at = 1f + phase * (waves - 1);
                int nearest = Nearest(waves, at, number => open(number) && !peaks.Exists(p => Math.Abs(p - number) <= 1));
                if (nearest != 0)
                {
                    peaks.Add(nearest);
                    places.Add(at);
                }
            }

            for (int i = 0; i < peaks.Count; i++)
            {
                int own = peaks[i];
                bool Clear(int number) => peaks.FindIndex(p => p != own && Math.Abs(p - number) <= 1) < 0;
                int chosen = Nearest(waves, places[i], number => Math.Abs(number - own) <= 1 && open(number) && marked(number) && Clear(number));
                peaks[i] = chosen != 0 ? chosen : own;
            }

            peaks.Sort();
            return peaks;
        }

        // The wave nearest a place in the level that passes the test (the earlier of two equally near); 0 for none.
        private static int Nearest(int waves, float at, Func<int, bool> passes)
        {
            int best = 0;
            for (int number = 1; number <= waves; number++)
            {
                if (passes(number) && (best == 0 || Math.Abs(number - at) < Math.Abs(best - at)))
                {
                    best = number;
                }
            }

            return best;
        }
    }
}
