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
        public static Verdict Judge(string level, int difficulty, IReadOnlyList<RunRecord> obvious, IReadOnlyList<RunRecord> plans, IReadOnlyDictionary<string, PlanSpec> specs)
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

            string planName = best.Count > 0 ? best[0].Strategy.Replace("plan ", string.Empty) : string.Empty;
            if (specs.TryGetValue(planName, out PlanSpec spec))
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
                foreach (string key in byPlan.Keys)
                {
                    if (specs.TryGetValue(key.Replace("plan ", string.Empty), out PlanSpec other))
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
