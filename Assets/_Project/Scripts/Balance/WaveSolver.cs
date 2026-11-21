using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Sets how tough the creatures of every wave of a level are, and the health of a level's boss (round 87: by
    /// <see cref="WaveFormula"/>, no solve). The script fixes who comes and how many; a wave's place in the level, its
    /// kind and whether it is a peak give its multiplier, and the level's index and the difficulty give its boss. No
    /// purse, no defence and no other wave is read, so a surprise or a scalar moves exactly the waves it is on. Rounds
    /// 83 to 86 solved every wave against the paper ledger's player (a target, a band, a median cap, a hero cap, three
    /// boss regimes); the measured correction then did the real work (Docs/BALANCE_CRITIC_2026-11-19.md).
    /// The level as played is one scalar per level found against the probe (Docs/WAVE_GRAMMAR.md).
    /// </summary>
    public static class WaveSolver
    {
        /// <summary>The difficulty whose multipliers a level's asset records as its own (Medium).</summary>
        public const int SolveDifficulty = 1;

        /// <summary>The multipliers of one difficulty and, per wave, the health of a level's boss on top of its wave's.</summary>
        public sealed class Solution
        {
            /// <summary>Per wave the multiplier the composer is given: <see cref="WalkIn"/> without what a capped troop could not buy.</summary>
            public float[] Healths;
            public float[] BossScales;

            /// <summary>Per wave the multiplier its creatures walk in with.</summary>
            public float[] WalkIn;

            /// <summary>The peak waves, 1-based.</summary>
            public List<int> Peaks;
        }

        /// <summary>
        /// The peak waves of a difficulty of a level: ground waves without a boss and not the breather after one, by
        /// preference a wave that carries a surprise or is a heavy one (<see cref="WaveFormula.Peaks"/>). A swarm that
        /// comes between two air raids gives its peak up and no other wave takes it
        /// (<see cref="BalanceRules.PeakOnARushBetweenRaids"/>, round 88b).
        /// </summary>
        public static List<int> PeaksOf(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int secondRoad, BalanceRules rules)
        {
            bool Open(int number) => WaveComposer.KindOf(script, difficulty, number) != WaveComposer.Kind.Air
                && WaveComposer.BossOn(script, difficulty, number) == null && WaveComposer.BossOn(script, difficulty, number - 1) == null;
            bool Marked(int number) => WaveComposer.KindOf(script, difficulty, number) == WaveComposer.Kind.Heavy
                || WaveGrammar.SurpriseOn(script, enemies, difficulty, number, secondRoad, rules) != SurpriseKind.None;
            List<int> peaks = WaveFormula.Peaks(rules, script.Waves, Open, Marked);
            if (!rules.PeakOnARushBetweenRaids)
            {
                // Dropped after the placing, so every other peak stays on the wave it had: the rule softens one wave.
                peaks.RemoveAll(number => IsRushBetweenRaids(script, difficulty, number));
            }

            return peaks;
        }

        /// <summary>
        /// True for a swarm whose two neighbours are both air raids: the purse of the wave before and of the wave after
        /// belongs to the sky guns, and the swarm is the wave that asks for the most ground guns.
        /// </summary>
        public static bool IsRushBetweenRaids(WaveScriptSpec script, int difficulty, int number) =>
            number > 1 && number < script.Waves
            && WaveComposer.KindOf(script, difficulty, number) == WaveComposer.Kind.Rush
            && WaveComposer.KindOf(script, difficulty, number - 1) == WaveComposer.Kind.Air
            && WaveComposer.KindOf(script, difficulty, number + 1) == WaveComposer.Kind.Air;

        /// <summary>
        /// One difficulty of a level. <paramref name="levelScalar"/> is the level's own factor from the closed loop,
        /// <paramref name="continentIndex"/> the level's index on its continent, <paramref name="spawnScale"/> what the
        /// game multiplies every creature's book health with besides the wave's multiplier (the difficulty's health
        /// factor times the level's health scale): a boss's bar is a number of hit points as spawned.
        /// </summary>
        public static Solution Solve(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int secondRoad, BalanceRules rules, float levelScalar, int continentIndex, float spawnScale)
        {
            var solution = new Solution { Healths = new float[script.Waves], BossScales = new float[script.Waves], WalkIn = new float[script.Waves], Peaks = PeaksOf(script, enemies, difficulty, secondRoad, rules) };
            var yardstick = new BossYardstick();
            for (int i = 0; i < script.Waves; i++)
            {
                int number = i + 1;
                float phase = WaveFormula.Phase(number, script.Waves);
                BossSpec boss = WaveComposer.BossOn(script, difficulty, number);
                WaveComposer.Kind kind = WaveComposer.KindOf(script, difficulty, number);
                bool raid = kind == WaveComposer.Kind.Air && boss == null;
                float walkIn = WaveFormula.WalkIn(rules, difficulty, levelScalar, phase, kind, boss != null, WaveComposer.BossOn(script, difficulty, number - 1) != null, solution.Peaks.Contains(number));

                // A troop capped at the largest group size makes every creature of its wave tougher by what it could not
                // buy (the composer's lift): the formula is the multiplier the creatures walk in with, so the lift comes off.
                float lift = WaveComposer.Compose(script, enemies, difficulty, number, 1f, secondRoad, rules).HealthMultiplier;
                float health = walkIn / lift;
                if (raid)
                {
                    // A flyer in a raid is never softer than the rule says, whatever the level's scalar (BalanceRules.RaidHealthFloor).
                    health = Math.Max(health, rules.RaidHealthFloorFor(difficulty));
                }

                // Rounded so the editor and the BalanceLab tool land on the same numbers.
                health = (float)Math.Round(health, 3);
                WaveSpec wave = WaveComposer.Compose(script, enemies, difficulty, number, health, secondRoad, rules);
                solution.Healths[i] = health;
                solution.WalkIn[i] = wave.HealthMultiplier;
                solution.BossScales[i] = boss != null && enemies.TryGetValue(boss.Enemy, out EnemyStats stats) && stats.IsBoss && !stats.IsContinentBoss
                    ? (float)Math.Round(BossMultiple(stats, yardstick, rules, continentIndex, difficulty, phase, spawnScale) / wave.HealthMultiplier, 3)
                    : 1f;
                if (!raid)
                {
                    yardstick.Meet(wave, wave.HealthMultiplier);
                }
            }

            return solution;
        }

        /// <summary>
        /// The multiple of its book health and armour a level's boss walks in with: its formula bar over its book bar as
        /// the game spawns it, and never under the floor of <see cref="BossYardstick"/>.
        /// </summary>
        public static float BossMultiple(EnemyStats boss, BossYardstick met, BalanceRules rules, int continentIndex, int difficulty, float phase, float spawnScale)
        {
            float book = BossYardstick.Bar(boss) * Math.Max(0.001f, spawnScale);
            float multiple = book > 0f ? WaveFormula.BossBar(rules, continentIndex, difficulty, phase, boss.Flies) / book : 1f;
            // Round 88b: a flying boss takes the flying share of the floor too, as it does of the formula (few guns reach it).
            // Without it the floor sized Fault Line's flying Medium boss above its Hard one (tougher Medium walkers).
            float floorShare = boss.Flies ? rules.FlyingBossShare : 1f;
            return met.HasMet ? Math.Max(multiple, met.Multiple(boss, rules.BossOverRegularFor(difficulty)) * floorShare) : multiple;
        }
    }
}
