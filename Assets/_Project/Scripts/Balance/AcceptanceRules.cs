using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Balance
{
    /// <summary>
    /// Remco's strategy rule per difficulty, encoded (round 22; Docs/BALANCE.md "Acceptance per difficulty"): on every
    /// difficulty a thought-out plan beats the obvious plans (a row of one type, the careless near-gate mix, the ledger-less
    /// affordable buyer), and only the margin grows. Judged on recorded PlayMode runs over several seeds: the obvious family
    /// from the yardstick, the searched plans from the replays. The one exception is explicit: <see cref="TutorialCells"/>.
    /// </summary>
    public static class AcceptanceRules
    {
        public const int Easy = 0;
        public const int Medium = 1;
        public const int Hard = 2;
        public const int Psycho = 3;

        /// <summary>
        /// Cells that may stay forgiving as a tutorial (Remco 2026-11-06: C1_01 Medium): the obvious family may win there, which
        /// is reported, not failed. Nothing else is exempt.
        /// </summary>
        public static readonly HashSet<string> TutorialCells = new HashSet<string> { "Level_C1_01 1" };

        /// <summary>
        /// Cells that may stay forgiving on Hard (Remco 2026-11-07: C1_01 Hard): the obvious family may still win there, but
        /// clearly worse than on Medium (fewer wins, fewer stars or less base left), a thought-out plan does visibly better, and the
        /// level is measurably harder than its Medium cell for the same plan. Judged with the Medium cell's obvious runs beside.
        /// </summary>
        public static readonly HashSet<string> ForgivingHardCells = new HashSet<string> { "Level_C1_01 2" };

        // The forgiving Hard cell: how much less base the same obvious plan must keep than on Medium when it still wins with the same stars.
        private const float ForgivingHardLessBase = 0.85f;

        // Win-rate bars per difficulty (critic of round 21, section 6).
        private const float EasyBestWinRate = 1f;
        private const float MediumBestWinRate = 0.8f;
        private const float HardBestWinRate = 0.6f;
        private const float PsychoBestWinRate = 0.4f;
        private const int MediumBestStars = 2;
        private const int MediumDistinctWinners = 3;
        private const float MediumNaiveMaxWinRate = 0.2f;
        private const float EasyMoreBase = 1.25f;
        private const float EasyMoreCoins = 1.2f;
        private const float LastThird = 2f / 3f;
        private const int HardFeatures = 2;
        private const int PsychoDistinct = 2;

        public sealed class Verdict
        {
            public string Cell = string.Empty;
            public bool Tutorial;
            public readonly List<string> Failures = new List<string>();
            public readonly List<string> Notes = new List<string>();
            public bool Passed => Failures.Count == 0;
        }

        /// <summary>
        /// Judges one cell. <paramref name="obvious"/> are the yardstick runs of the cell (strategy names "mono T", "naive",
        /// "affordable"; "creative" is a reference and ignored), <paramref name="plans"/> the replayed searched plans (strategy
        /// "plan NAME"), <paramref name="specs"/> the plans by name for the feature checks.
        /// </summary>
        public static Verdict Judge(string level, int difficulty, IReadOnlyList<RunRecord> obvious, IReadOnlyList<RunRecord> plans, IReadOnlyDictionary<string, PlanSpec> specs) =>
            Judge(level, difficulty, obvious, plans, specs, null);

        /// <summary>As above; <paramref name="mediumObvious"/> are the same level's Medium yardstick runs, which the forgiving Hard cell is judged against.</summary>
        public static Verdict Judge(string level, int difficulty, IReadOnlyList<RunRecord> obvious, IReadOnlyList<RunRecord> plans, IReadOnlyDictionary<string, PlanSpec> specs, IReadOnlyList<RunRecord> mediumObvious)
        {
            var verdict = new Verdict { Cell = level + " " + difficulty.ToString(CultureInfo.InvariantCulture) };
            verdict.Tutorial = TutorialCells.Contains(verdict.Cell);
            Dictionary<string, List<RunRecord>> families = Group(obvious, r => !r.Strategy.StartsWith("creative", StringComparison.Ordinal));
            Dictionary<string, List<RunRecord>> byPlan = Group(plans, r => true);
            if (byPlan.Count == 0)
            {
                verdict.Failures.Add("no replayed plans (run the plan search and the replay)");
                return verdict;
            }

            // The best plan: highest win rate, then mean realised score.
            string best = null;
            float bestRate = -1f;
            float bestScore = float.MinValue;
            foreach (KeyValuePair<string, List<RunRecord>> plan in byPlan)
            {
                float rate = WinRate(plan.Value);
                float score = MeanScore(plan.Value);
                if (rate > bestRate || (rate == bestRate && score > bestScore))
                {
                    best = plan.Key;
                    bestRate = rate;
                    bestScore = score;
                }
            }

            List<RunRecord> bestRuns = byPlan[best];
            verdict.Notes.Add(Invariant($"best plan {best}: wins {Wins(bestRuns)}/{bestRuns.Count}, stars {MeanStars(bestRuns):0.0}"));
            int winningPlans = 0;
            foreach (KeyValuePair<string, List<RunRecord>> plan in byPlan)
            {
                winningPlans += WinRate(plan.Value) > 0.5f ? 1 : 0;
            }

            verdict.Notes.Add(Invariant($"{winningPlans} of {byPlan.Count} replayed plans win on most seeds"));

            switch (difficulty)
            {
                case Easy:
                    JudgeEasy(verdict, families, bestRuns);
                    break;
                case Medium:
                    JudgeMedium(verdict, families, byPlan, bestRuns, winningPlans);
                    break;
                case Hard when ForgivingHardCells.Contains(verdict.Cell):
                    JudgeForgivingHard(verdict, families, Group(mediumObvious ?? new List<RunRecord>(), r => !r.Strategy.StartsWith("creative", StringComparison.Ordinal)), bestRuns);
                    break;
                default:
                    JudgeHard(verdict, families, byPlan, bestRuns, specs, difficulty);
                    break;
            }

            return verdict;
        }

        // Easy: a reasonable plan wins (two of the three obvious kinds), nobody dies before the last third, the best plan wins
        // every seed with three stars and beats every obvious plan on score on every seed.
        private static void JudgeEasy(Verdict verdict, Dictionary<string, List<RunRecord>> families, List<RunRecord> best)
        {
            int kindsWinning = 0;
            int kinds = 0;
            foreach (KeyValuePair<string, List<RunRecord>> family in Kinds(families))
            {
                kinds++;
                kindsWinning += WinRate(family.Value) > 0.5f ? 1 : 0;
            }

            if (kinds > 0 && kindsWinning < Math.Min(2, kinds))
            {
                verdict.Failures.Add(Invariant($"Easy: only {kindsWinning} of {kinds} obvious kinds win on most seeds (a reasonable plan must win)"));
            }

            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                foreach (RunRecord run in family.Value)
                {
                    if (!run.Won && run.WaveReached < run.WaveCount * LastThird)
                    {
                        verdict.Failures.Add(Invariant($"Easy: {run.Strategy} seed {run.Seed} dies at wave {run.WaveReached}/{run.WaveCount}, before the last third"));
                    }
                }
            }

            if (WinRate(best) < EasyBestWinRate || MinStars(best) < 3)
            {
                verdict.Failures.Add(Invariant($"Easy: the best plan wins {Wins(best)}/{best.Count} with {MinStars(best)} stars at least (needs every seed, 3 stars)"));
            }

            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                foreach (RunRecord run in family.Value)
                {
                    RunRecord same = best.Find(b => b.Seed == run.Seed);
                    if (same == null || !same.Won)
                    {
                        continue;
                    }

                    bool beats = !run.Won || same.Stars > run.Stars || (same.BaseHealth >= run.BaseHealth * EasyMoreBase && same.Coins >= run.Coins * EasyMoreCoins);
                    if (!beats)
                    {
                        verdict.Failures.Add(Invariant($"Easy: the best plan does not clearly beat {run.Strategy} on seed {run.Seed} ({same.Stars}* base {same.BaseHealth:0} coins {same.Coins} vs {run.Stars}* base {run.BaseHealth:0} coins {run.Coins})"));
                    }
                }
            }
        }

        // Medium: the obvious plans lose (mono rows never, the careless mix at most one seed in five with one star, the
        // affordable buyer on most seeds), the best plan wins most seeds with two stars, and several distinct plans win.
        private static void JudgeMedium(Verdict verdict, Dictionary<string, List<RunRecord>> families, Dictionary<string, List<RunRecord>> byPlan, List<RunRecord> best, int winningPlans)
        {
            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                string kind = KindOf(family.Key);
                int wins = Wins(family.Value);
                string note = null;
                if (kind == "mono" && wins > 0)
                {
                    note = Invariant($"Medium: {family.Key} wins {wins}/{family.Value.Count} (a row of one type must lose)");
                }
                else if (kind == "naive" && (WinRate(family.Value) > MediumNaiveMaxWinRate || MaxStars(family.Value) > 1))
                {
                    note = Invariant($"Medium: naive wins {wins}/{family.Value.Count} with up to {MaxStars(family.Value)} stars (at most one in five, one star)");
                }
                else if (kind == "affordable" && WinRate(family.Value) > 0.5f)
                {
                    note = Invariant($"Medium: the affordable buyer wins {wins}/{family.Value.Count} (must lose on most seeds)");
                }

                if (note != null)
                {
                    (verdict.Tutorial ? verdict.Notes : verdict.Failures).Add(verdict.Tutorial ? "tutorial, forgiven: " + note : note);
                }
            }

            if (WinRate(best) < MediumBestWinRate || (Wins(best) > 0 && MeanStars(best) < MediumBestStars))
            {
                verdict.Failures.Add(Invariant($"Medium: the best plan wins {Wins(best)}/{best.Count} with {MeanStars(best):0.0} stars (needs 4 of 5, 2 stars)"));
            }

            if (winningPlans < Math.Min(MediumDistinctWinners, byPlan.Count))
            {
                verdict.Failures.Add(Invariant($"Medium: {winningPlans} replayed plans win (at least {MediumDistinctWinners} distinct answers wanted)"));
            }

            int obviousBest = 0;
            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                obviousBest = Math.Max(obviousBest, MaxStars(family.Value));
            }

            if (!verdict.Tutorial && Wins(best) > 0 && MeanStars(best) - obviousBest < 2f)
            {
                verdict.Failures.Add(Invariant($"Medium: the obvious plans reach {obviousBest} stars, the best plan {MeanStars(best):0.0} (two stars apart wanted)"));
            }
        }

        // Hard and Psycho: every obvious plan loses on every seed and falls before the last third; the best plan wins its share
        // of seeds using at least two of the game's features; Psycho plans differ among themselves and one reaches the finale on
        // every seed.
        private static void JudgeHard(Verdict verdict, Dictionary<string, List<RunRecord>> families, Dictionary<string, List<RunRecord>> byPlan, List<RunRecord> best, IReadOnlyDictionary<string, PlanSpec> specs, int difficulty)
        {
            string name = difficulty == Psycho ? "Psycho" : "Hard";
            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                foreach (RunRecord run in family.Value)
                {
                    if (run.Won)
                    {
                        verdict.Failures.Add(Invariant($"{name}: {run.Strategy} wins on seed {run.Seed} (every obvious plan must lose)"));
                    }
                    else if (run.WaveReached >= run.WaveCount * LastThird)
                    {
                        verdict.Failures.Add(Invariant($"{name}: {run.Strategy} seed {run.Seed} reaches wave {run.WaveReached}/{run.WaveCount} (obvious plans fall before the last third)"));
                    }
                }
            }

            float bar = difficulty == Psycho ? PsychoBestWinRate : HardBestWinRate;
            if (WinRate(best) < bar)
            {
                verdict.Failures.Add(Invariant($"{name}: the best plan wins {Wins(best)}/{best.Count} (needs {bar:P0})"));
            }

            PlanSpec spec = SpecOf(best, specs);
            if (spec != null)
            {
                int features = (spec.Uses(PlanStep.Mine) || spec.Uses(PlanStep.Field) ? 1 : 0) + (spec.Uses(PlanStep.Drone) ? 1 : 0) + (spec.Uses(PlanStep.Sell) ? 1 : 0) + (spec.Upgrades >= 3 ? 1 : 0);
                if (features < HardFeatures)
                {
                    verdict.Failures.Add(Invariant($"{name}: the best plan uses {features} of the game's features (gadgets, a drone, a sale, three or more upgrades; needs {HardFeatures})"));
                }

                if (difficulty == Psycho && !spec.Uses(PlanStep.Drone))
                {
                    verdict.Failures.Add("Psycho: the best plan buys no drone (no plan without drones should win)");
                }
            }

            if (difficulty == Psycho)
            {
                foreach (RunRecord run in best)
                {
                    bool reachedFinale = run.Won || run.WaveReached >= run.WaveCount;
                    if (!reachedFinale)
                    {
                        bool anyReached = false;
                        foreach (List<RunRecord> plan in byPlan.Values)
                        {
                            RunRecord same = plan.Find(p => p.Seed == run.Seed);
                            anyReached |= same != null && (same.Won || same.WaveReached >= same.WaveCount);
                        }

                        if (!anyReached)
                        {
                            verdict.Failures.Add(Invariant($"Psycho: no plan reaches the finale on seed {run.Seed}"));
                        }
                    }
                }

                var top = new List<PlanSpec>();
                foreach (List<RunRecord> plan in byPlan.Values)
                {
                    PlanSpec other = SpecOf(plan, specs);
                    if (other != null)
                    {
                        top.Add(other);
                    }
                }

                int distinct = 0;
                for (int i = 0; i < top.Count; i++)
                {
                    for (int j = i + 1; j < top.Count; j++)
                    {
                        distinct += top[i].DistinctFrom(top[j]) ? 1 : 0;
                    }
                }

                if (top.Count >= 2 && distinct < PsychoDistinct)
                {
                    verdict.Failures.Add(Invariant($"Psycho: the replayed plans are alike ({distinct} distinct pairs; the fight must be a puzzle, not one trick)"));
                }
            }
        }

        // The forgiving Hard cell (C1_01 Hard): obvious plans may win, but worse than on Medium for the same plan; the best plan
        // wins every seed and visibly beats every obvious run; the level is measurably harder than Medium for the same plan.
        private static void JudgeForgivingHard(Verdict verdict, Dictionary<string, List<RunRecord>> families, Dictionary<string, List<RunRecord>> medium, List<RunRecord> best)
        {
            verdict.Notes.Add("forgiving Hard cell (Remco 2026-11-07): obvious plans may win, but worse than on Medium");
            int compared = 0;
            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                if (!medium.TryGetValue(family.Key, out List<RunRecord> onMedium) || onMedium.Count == 0)
                {
                    continue;
                }

                compared++;
                float hardScore = MeanScore(family.Value);
                float mediumScore = MeanScore(onMedium);
                if (hardScore >= mediumScore)
                {
                    verdict.Failures.Add(Invariant($"Hard (forgiving): {family.Key} scores {hardScore:0.00} on Hard against {mediumScore:0.00} on Medium (Hard must be measurably harder for the same plan)"));
                }

                bool worse = Wins(family.Value) < Wins(onMedium)
                    || MeanStars(family.Value) < MeanStars(onMedium)
                    || MeanBase(family.Value) <= MeanBase(onMedium) * ForgivingHardLessBase;
                if (!worse)
                {
                    verdict.Failures.Add(Invariant($"Hard (forgiving): {family.Key} does as well as on Medium ({Wins(family.Value)}/{family.Value.Count} wins, {MeanStars(family.Value):0.0} stars, base {MeanBase(family.Value):0} vs {Wins(onMedium)}/{onMedium.Count}, {MeanStars(onMedium):0.0}, base {MeanBase(onMedium):0}); it must win less, with fewer stars or less base"));
                }
            }

            if (compared == 0)
            {
                verdict.Failures.Add("Hard (forgiving): no Medium runs of the same obvious plans to compare with (run the yardstick on Medium too)");
            }

            if (WinRate(best) < EasyBestWinRate)
            {
                verdict.Failures.Add(Invariant($"Hard (forgiving): the best plan wins {Wins(best)}/{best.Count} (needs every seed)"));
            }

            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                foreach (RunRecord run in family.Value)
                {
                    RunRecord same = best.Find(b => b.Seed == run.Seed);
                    if (same == null || !same.Won)
                    {
                        continue;
                    }

                    bool beats = !run.Won || same.Stars > run.Stars || same.BaseHealth >= run.BaseHealth * EasyMoreBase;
                    if (!beats)
                    {
                        verdict.Failures.Add(Invariant($"Hard (forgiving): the best plan does not visibly beat {run.Strategy} on seed {run.Seed} ({same.Stars}* base {same.BaseHealth:0} vs {run.Stars}* base {run.BaseHealth:0})"));
                    }
                }
            }
        }

        /// <summary>
        /// The plan a group of runs followed: the written spec by name, else (an adaptive strategy, round 22e) the moves the run with the most
        /// moves really took, so the feature checks judge what happened in play.
        /// </summary>
        public static PlanSpec SpecOf(List<RunRecord> runs, IReadOnlyDictionary<string, PlanSpec> specs)
        {
            if (runs.Count == 0)
            {
                return null;
            }

            if (specs != null && specs.TryGetValue(runs[0].PlanName, out PlanSpec spec))
            {
                return spec;
            }

            RunRecord most = runs[0];
            foreach (RunRecord run in runs)
            {
                most = run.Moves.Count > most.Moves.Count ? run : most;
            }

            return most.Moves.Count > 0 ? most.MovesAsPlan() : null;
        }

        /// <summary>
        /// The in-play margin of a group of runs (round 22e, the curator: play judges the ramp), 0 to 1: a win counts 0.5 plus half its
        /// stars share, a loss half of how far it got. Averaged over the runs.
        /// </summary>
        public static float PlayMargin(IReadOnlyList<RunRecord> runs)
        {
            if (runs.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (RunRecord run in runs)
            {
                sum += run.Won ? 0.5f + 0.5f * run.Stars / 3f : 0.5f * run.WaveReached / Math.Max(1, run.WaveCount);
            }

            return sum / runs.Count;
        }

        /// <summary>A cell's in-play margin: the mean of the best replayed plan's margin and the creative reference's (whichever exist).</summary>
        public static float CellMargin(IReadOnlyList<RunRecord> obvious, IReadOnlyList<RunRecord> plans, out string best)
        {
            best = string.Empty;
            var creative = new List<RunRecord>();
            foreach (RunRecord run in obvious)
            {
                if (run.Strategy.StartsWith("creative", StringComparison.Ordinal))
                {
                    creative.Add(run);
                }
            }

            List<RunRecord> bestRuns = null;
            foreach (KeyValuePair<string, List<RunRecord>> plan in Group(plans, r => true))
            {
                if (bestRuns == null || PlayMargin(plan.Value) > PlayMargin(bestRuns))
                {
                    bestRuns = plan.Value;
                    best = plan.Key;
                }
            }

            float sum = 0f;
            int parts = 0;
            if (bestRuns != null)
            {
                sum += PlayMargin(bestRuns);
                parts++;
            }

            if (creative.Count > 0)
            {
                sum += PlayMargin(creative);
                parts++;
            }

            return parts > 0 ? sum / parts : 0f;
        }

        /// <summary>
        /// The in-play ramp within a continent: per level, in play order, the cell margins per difficulty; a level whose margin is above the
        /// level before's by more than the tolerance is a dip (easier in play than the one before). Returns the dips as "Level difficulty".
        /// </summary>
        public static List<(string Level, int Difficulty, string Reason)> InPlayRamp(IReadOnlyList<string> levels, IReadOnlyList<float[]> margins, float tolerance)
        {
            var dips = new List<(string, int, string)>();
            for (int i = 1; i < margins.Count; i++)
            {
                for (int d = 0; d < margins[i].Length && d < margins[i - 1].Length; d++)
                {
                    if (margins[i][d] > 0f && margins[i - 1][d] > 0f && margins[i][d] > margins[i - 1][d] + tolerance)
                    {
                        dips.Add((levels[i], d, Invariant($"easier in play than {levels[i - 1]} (margin {margins[i][d]:0.00} against {margins[i - 1][d]:0.00})")));
                    }
                }
            }

            return dips;
        }

        // Mean base health left over the wins (0 when none won).
        private static float MeanBase(List<RunRecord> runs)
        {
            int wins = Wins(runs);
            if (wins == 0)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (RunRecord run in runs)
            {
                sum += run.Won ? run.BaseHealth : 0f;
            }

            return sum / wins;
        }

        /// <summary>Which way the builder's one lever (the cell's target scale) should move after a stage-B verdict.</summary>
        public enum Lever
        {
            None = 0,
            Harder = 1,
            Easier = 2,
        }

        /// <summary>
        /// The builder's reading of a cell's recorded runs (round 22d: the builder moves on stage-B verdicts, not on paper). Too easy
        /// when the obvious family wins where the rule says it must lose (or, on Easy, wins so comfortably that no plan can visibly
        /// beat it); too hard when the best replayed plan misses its win-rate bar or, on Easy, fewer than two obvious kinds win.
        /// Both at once is a design question, not a lever move. The tutorial and forgiving cells keep their exceptions.
        /// </summary>
        public static Lever LeverFor(string level, int difficulty, IReadOnlyList<RunRecord> obvious, IReadOnlyList<RunRecord> plans, IReadOnlyList<RunRecord> mediumObvious, out string reason)
        {
            string cell = level + " " + difficulty.ToString(CultureInfo.InvariantCulture);
            Dictionary<string, List<RunRecord>> families = Group(obvious, r => !r.Strategy.StartsWith("creative", StringComparison.Ordinal));
            Dictionary<string, List<RunRecord>> byPlan = Group(plans, r => true);
            float bestRate = 0f;
            List<RunRecord> best = null;
            foreach (List<RunRecord> plan in byPlan.Values)
            {
                float rate = WinRate(plan);
                if (best == null || rate > bestRate || (rate == bestRate && MeanScore(plan) > MeanScore(best)))
                {
                    best = plan;
                    bestRate = rate;
                }
            }

            bool tooEasy = false;
            bool tooHard = false;
            var why = new List<string>();
            if (difficulty == Easy)
            {
                int kindsWinning = 0;
                int kinds = 0;
                bool allComfortable = families.Count > 0;
                foreach (KeyValuePair<string, List<RunRecord>> kind in Kinds(families))
                {
                    kinds++;
                    kindsWinning += WinRate(kind.Value) > 0.5f ? 1 : 0;
                }

                foreach (List<RunRecord> family in families.Values)
                {
                    foreach (RunRecord run in family)
                    {
                        allComfortable &= run.Won && run.Stars >= 3;
                    }
                }

                if (kinds > 0 && kindsWinning < Math.Min(2, kinds))
                {
                    tooHard = true;
                    why.Add(Invariant($"only {kindsWinning} of {kinds} obvious kinds win"));
                }

                if (best != null && (bestRate < EasyBestWinRate || MinStars(best) < 3))
                {
                    tooHard = true;
                    why.Add(Invariant($"the best plan wins {Wins(best)}/{best.Count} with {MinStars(best)} stars at least"));
                }

                if (!tooHard && allComfortable)
                {
                    tooEasy = true;
                    why.Add("every obvious run wins with three stars (no plan can visibly beat that)");
                }
            }
            else if (difficulty == Medium && TutorialCells.Contains(cell))
            {
                if (best != null && bestRate < MediumBestWinRate)
                {
                    tooHard = true;
                    why.Add(Invariant($"the best plan wins {Wins(best)}/{best.Count}"));
                }
            }
            else if (difficulty == Medium)
            {
                foreach (KeyValuePair<string, List<RunRecord>> family in families)
                {
                    string kind = KindOf(family.Key);
                    bool wins = (kind == "mono" && Wins(family.Value) > 0) || (kind == "naive" && WinRate(family.Value) > MediumNaiveMaxWinRate) || (kind == "affordable" && WinRate(family.Value) > 0.5f);
                    if (wins)
                    {
                        tooEasy = true;
                        why.Add(Invariant($"{family.Key} wins {Wins(family.Value)}/{family.Value.Count}"));
                    }
                }

                if (best != null && bestRate < MediumBestWinRate)
                {
                    tooHard = true;
                    why.Add(Invariant($"the best plan wins {Wins(best)}/{best.Count}"));
                }
            }
            else if (difficulty == Hard && ForgivingHardCells.Contains(cell))
            {
                Dictionary<string, List<RunRecord>> medium = Group(mediumObvious ?? new List<RunRecord>(), r => !r.Strategy.StartsWith("creative", StringComparison.Ordinal));
                foreach (KeyValuePair<string, List<RunRecord>> family in families)
                {
                    if (!medium.TryGetValue(family.Key, out List<RunRecord> onMedium) || onMedium.Count == 0)
                    {
                        continue;
                    }

                    bool worse = Wins(family.Value) < Wins(onMedium) || MeanStars(family.Value) < MeanStars(onMedium) || MeanBase(family.Value) <= MeanBase(onMedium) * ForgivingHardLessBase;
                    if (!worse || MeanScore(family.Value) >= MeanScore(onMedium))
                    {
                        tooEasy = true;
                        why.Add(Invariant($"{family.Key} does as well as on Medium"));
                    }
                }

                if (best != null && bestRate < EasyBestWinRate)
                {
                    tooHard = true;
                    why.Add(Invariant($"the best plan wins {Wins(best)}/{best.Count}"));
                }
            }
            else
            {
                foreach (KeyValuePair<string, List<RunRecord>> family in families)
                {
                    foreach (RunRecord run in family.Value)
                    {
                        if (run.Won || run.WaveReached >= run.WaveCount * LastThird)
                        {
                            tooEasy = true;
                            why.Add(Invariant($"{run.Strategy} seed {run.Seed} {(run.Won ? "wins" : "reaches wave " + run.WaveReached.ToString(CultureInfo.InvariantCulture))}"));
                            break;
                        }
                    }
                }

                float bar = difficulty == Psycho ? PsychoBestWinRate : HardBestWinRate;
                if (best == null || bestRate < bar)
                {
                    tooHard = true;
                    why.Add(best == null ? "no plan replayed" : Invariant($"the best plan wins {Wins(best)}/{best.Count} (bar {bar:P0})"));
                }
            }

            reason = string.Join("; ", why);
            if (tooEasy && tooHard)
            {
                return Lever.None;
            }

            return tooEasy ? Lever.Harder : tooHard ? Lever.Easier : Lever.None;
        }

        private static Dictionary<string, List<RunRecord>> Group(IReadOnlyList<RunRecord> runs, Predicate<RunRecord> keep)
        {
            var groups = new Dictionary<string, List<RunRecord>>();
            foreach (RunRecord run in runs)
            {
                if (!keep(run))
                {
                    continue;
                }

                if (!groups.TryGetValue(run.Strategy, out List<RunRecord> list))
                {
                    groups[run.Strategy] = list = new List<RunRecord>();
                }

                list.Add(run);
            }

            return groups;
        }

        // The obvious kinds: all mono rows as one kind, the naive mix, the affordable buyer.
        private static Dictionary<string, List<RunRecord>> Kinds(Dictionary<string, List<RunRecord>> families)
        {
            var kinds = new Dictionary<string, List<RunRecord>>();
            foreach (KeyValuePair<string, List<RunRecord>> family in families)
            {
                string kind = KindOf(family.Key);
                if (!kinds.TryGetValue(kind, out List<RunRecord> list))
                {
                    kinds[kind] = list = new List<RunRecord>();
                }

                list.AddRange(family.Value);
            }

            return kinds;
        }

        private static string KindOf(string strategy) => strategy.StartsWith("mono", StringComparison.Ordinal) ? "mono" : strategy;

        private static int Wins(List<RunRecord> runs)
        {
            int wins = 0;
            foreach (RunRecord run in runs)
            {
                wins += run.Won ? 1 : 0;
            }

            return wins;
        }

        private static float WinRate(List<RunRecord> runs) => runs.Count > 0 ? Wins(runs) / (float)runs.Count : 0f;

        private static float MeanStars(List<RunRecord> runs)
        {
            int wins = Wins(runs);
            if (wins == 0)
            {
                return 0f;
            }

            float stars = 0f;
            foreach (RunRecord run in runs)
            {
                stars += run.Won ? run.Stars : 0;
            }

            return stars / wins;
        }

        private static int MinStars(List<RunRecord> runs)
        {
            int min = int.MaxValue;
            foreach (RunRecord run in runs)
            {
                min = Math.Min(min, run.Won ? run.Stars : 0);
            }

            return min == int.MaxValue ? 0 : min;
        }

        private static int MaxStars(List<RunRecord> runs)
        {
            int max = 0;
            foreach (RunRecord run in runs)
            {
                max = Math.Max(max, run.Won ? run.Stars : 0);
            }

            return max;
        }

        private static float MeanScore(List<RunRecord> runs)
        {
            float sum = 0f;
            foreach (RunRecord run in runs)
            {
                sum += run.RealisedScore;
            }

            return runs.Count > 0 ? sum / runs.Count : 0f;
        }

        private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
    }
}
