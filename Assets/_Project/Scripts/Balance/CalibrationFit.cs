using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Balance
{
    /// <summary>One wave of a recorded probe run: what stood, what the wave brought, what each tower type really took off it.</summary>
    public sealed class WaveRecord
    {
        public int Wave;

        /// <summary>Health the wave spawned (its creatures' maximum health, by the wave they spawned in).</summary>
        public float Health;

        /// <summary>Damage the towers of each type really dealt to this wave's creatures, after plating and overkill.</summary>
        public readonly Dictionary<string, float> Dealt = new Dictionary<string, float>();

        /// <summary>Damage by anything that is not a tower the player built: the hero, mines, the base turret, drones.</summary>
        public float OtherDealt;

        /// <summary>Health the base and the gates lost during the wave (repairs not counted back): what the leak really cost (round 22b).</summary>
        public float BaseLost;
        public float GateLost;

        /// <summary>Seconds a repair drone was on the field during the wave, and the seconds it spent repairing: its busy share (round 22b).</summary>
        public float RepairDroneSeconds;
        public float RepairDroneBusy;

        /// <summary>The towers standing when the wave started: type, level and cell.</summary>
        public readonly List<(string Id, int Level, Cell Cell)> Standing = new List<(string, int, Cell)>();

        public float TowerDealt
        {
            get
            {
                float sum = 0f;
                foreach (float dealt in Dealt.Values)
                {
                    sum += dealt;
                }

                return sum;
            }
        }
    }

    /// <summary>One recorded probe run (a bot or a replayed plan) with its outcome and its waves.</summary>
    public sealed class RunRecord
    {
        public string Level = string.Empty;
        public int Difficulty;
        public string Strategy = string.Empty;
        public int Seed;
        public float Step;
        public bool Won;
        public int Stars;
        public int WaveReached;
        public int WaveCount;
        public float BaseHealth;
        public int Coins;

        /// <summary>Played on a fresh save (no permanent upgrades, perks or base turrets) instead of the campaign's expected player for the cell (round 22d).</summary>
        public bool FreshPlayer;
        public readonly List<WaveRecord> Waves = new List<WaveRecord>();

        /// <summary>The build order the run really took (builds, upgrades, gadgets, drones, sales with their wave; round 22e), for the README and the feature checks.</summary>
        public readonly List<PlanStep> Moves = new List<PlanStep>();

        /// <summary>The plan or strategy name without its "plan " or "strategy " prefix.</summary>
        public string PlanName => PlanNameOf(Strategy);

        public static string PlanNameOf(string strategy) =>
            strategy.StartsWith("plan ", StringComparison.Ordinal) ? strategy.Substring(5) : strategy.StartsWith("strategy ", StringComparison.Ordinal) ? strategy.Substring(9) : strategy;

        /// <summary>The moves as a plan (what was really built, in order), so the acceptance rules judge the features a run used.</summary>
        public PlanSpec MovesAsPlan() => new PlanSpec { Level = Level, Difficulty = Difficulty, Name = PlanName, Steps = Moves.ToArray() };

        /// <summary>Stars first, then base health, then coins; a loss ranks by how far it got.</summary>
        public float RealisedScore => Leak.Score(Won, Stars, BaseHealth, Coins, WaveCount, WaveReached);

        /// <summary>A replayed plan (no decisions at run time: gates are repaired only where the plan says so), not a bot.</summary>
        public bool Scripted => Strategy.StartsWith("plan", StringComparison.Ordinal);

        public string Cell => Level + " " + Difficulty.ToString(CultureInfo.InvariantCulture) + (FreshPlayer ? " fresh" : string.Empty);
    }

    /// <summary>What the fit needs of a level and difficulty: the ledger input and the loads of its waves, as the probe met them.</summary>
    public sealed class RecordedCell
    {
        public LevelInput Input;
        public List<WaveLoad> Loads = new List<WaveLoad>();

        /// <summary>The rules the run is judged by (the human lens with the hero) and the difficulty's hero cap.</summary>
        public BalanceRules Judge;
        public float Cap;
    }

    /// <summary>
    /// Fits the ledger's per-tower efficiency on recorded runs (round 22): for every tower type, the damage its towers really
    /// dealt over the paper capacity of the same towers from their own spots, summed over all recorded waves. A wave the
    /// defence out-gunned (more capacity than health) saturates: its capacity counts only up to the health there was to take,
    /// shared by the types in proportion to their capacity, which is why the fit iterates. Absolute, per type: no mean
    /// normalisation, no field ease. Then the leak factor (round 22b): the base health the runs really lost over the leak model's
    /// paper damage from the same standing sets. Then it checks itself: with the fitted numbers, does the paper verdict on each run
    /// (the base survives every wave) agree with what happened, and does its ranking of the runs in a cell match theirs.
    /// </summary>
    public static class CalibrationFit
    {
        private const int Iterations = 16;
        private const float MinEfficiency = 0.05f;
        private const float MaxEfficiency = 4f;

        /// <summary>Bounds of the siege factor the fit may return (<see cref="BalanceRules.SiegeFactor"/>).</summary>
        private const float MinSiegeFactor = 0.1f;
        private const float MaxSiegeFactor = 10f;

        public sealed class Result
        {
            public readonly SortedDictionary<string, float> Efficiency = new SortedDictionary<string, float>(StringComparer.Ordinal);
            public readonly SortedDictionary<string, (float Dealt, float Capacity, int Waves)> Sums = new SortedDictionary<string, (float, float, int)>(StringComparer.Ordinal);
            public int Runs;

            /// <summary>Structure health (gates and base) the runs really lost over the siege model's paper damage at factor one (<see cref="BalanceRules.SiegeFactor"/>).</summary>
            public float SiegeFactor = 1f;
            public float StructureLost;
            public float PaperStructureDamage;
            public int SiegeWaves;

            /// <summary>Share of its field time a repair drone spent repairing, over every recorded wave with one (<see cref="BalanceRules.RepairDroneBusyShare"/>); -1 when none was seen.</summary>
            public float RepairDroneBusyShare = -1f;

            /// <summary>The queue and doorstep guards' share of their damage per second with the best verdict agreement (<see cref="BalanceRules.QueueGuardShare"/>).</summary>
            public float QueueGuardShare = 1f;
            public float RepairDroneSeconds;
        }

        /// <summary>What the leak model predicts for one recorded run from its realised standing sets: the base left, the fall wave, the stars.</summary>
        public struct Prediction
        {
            public float BaseLeft;
            public int FallWave;
            public int Stars;
            public float Margin;
            public int FirstBreak;
            public bool Holds => FallWave == 0 && BaseLeft > 0f;
        }

        public sealed class Agreement
        {
            public int Runs;
            public int Agreed;
            public int MonoRuns;
            public int MonoAgreed;
            public readonly List<string> Disagreements = new List<string>();
            public readonly List<(string Cell, float Spearman, int Plans)> Rankings = new List<(string, float, int)>();

            public float Share => Runs > 0 ? Agreed / (float)Runs : 0f;
            public float MonoShare => MonoRuns > 0 ? MonoAgreed / (float)MonoRuns : 1f;

            public float MeanSpearman
            {
                get
                {
                    float sum = 0f;
                    int n = 0;
                    foreach ((string _, float rho, int plans) in Rankings)
                    {
                        if (plans >= 4)
                        {
                            sum += rho;
                            n++;
                        }
                    }

                    return n > 0 ? sum / n : 1f;
                }
            }
        }

        /// <summary>Fits one efficiency per tower type on the runs whose cells are given; towers never seen stay at one.</summary>
        public static Result Fit(IReadOnlyList<RunRecord> runs, Func<RunRecord, RecordedCell> cellOf)
        {
            var result = new Result();
            var efficiency = new Dictionary<string, float>();
            var waves = new List<(WaveRecord Record, Dictionary<string, float> Capacity, float Hero)>();
            foreach (RunRecord run in runs)
            {
                RecordedCell cell = cellOf(run);
                if (cell == null)
                {
                    continue;
                }

                result.Runs++;
                foreach (WaveRecord wave in run.Waves)
                {
                    // The wave a lost run ends in is cut short (the base dies while creatures still walk): it says nothing about the guns.
                    bool cutShort = !run.Won && wave.Wave >= run.WaveReached;
                    if (wave.Wave < 1 || wave.Wave > cell.Loads.Count || wave.Health <= 0f || cutShort)
                    {
                        continue;
                    }

                    WaveLoad load = cell.Loads[wave.Wave - 1];
                    Dictionary<string, float> capacity = PlanEvaluator.CapacityByType(cell.Input, load, JudgeOf(cell), wave.Standing, 1f);
                    waves.Add((wave, capacity, wave.OtherDealt));
                    foreach (string id in capacity.Keys)
                    {
                        if (!efficiency.ContainsKey(id))
                        {
                            efficiency[id] = 1f;
                        }
                    }
                }
            }

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                var dealt = new Dictionary<string, float>();
                var capacity = new Dictionary<string, float>();
                var seen = new Dictionary<string, int>();
                foreach ((WaveRecord record, Dictionary<string, float> paper, float other) in waves)
                {
                    // The defence could take at most the health there was, less what the hero and the gadgets took.
                    float total = 0f;
                    foreach (KeyValuePair<string, float> pair in paper)
                    {
                        total += pair.Value * efficiency[pair.Key];
                    }

                    float available = Math.Max(0f, record.Health - other);
                    float saturation = total > 0f ? Math.Min(1f, available / total) : 1f;
                    foreach (KeyValuePair<string, float> pair in paper)
                    {
                        capacity[pair.Key] = (capacity.TryGetValue(pair.Key, out float c) ? c : 0f) + pair.Value * saturation;
                        dealt[pair.Key] = (dealt.TryGetValue(pair.Key, out float d) ? d : 0f) + (record.Dealt.TryGetValue(pair.Key, out float real) ? real : 0f);
                        seen[pair.Key] = (seen.TryGetValue(pair.Key, out int n) ? n : 0) + 1;
                    }
                }

                foreach (string id in new List<string>(efficiency.Keys))
                {
                    float paper = capacity.TryGetValue(id, out float c) ? c : 0f;
                    efficiency[id] = paper > 0f ? Math.Min(MaxEfficiency, Math.Max(MinEfficiency, dealt[id] / paper)) : 1f;
                    result.Sums[id] = (dealt.TryGetValue(id, out float d) ? d : 0f, paper, seen.TryGetValue(id, out int n) ? n : 0);
                }
            }

            foreach (KeyValuePair<string, float> pair in efficiency)
            {
                result.Efficiency[pair.Key] = (float)Math.Round(pair.Value, 3);
            }

            FitGuards(runs, cellOf, result);
            FitRepairDrone(runs, result);
            return result;
        }

        // The guard shares the fit tries (round 22d): the siege factor is refitted for each, and the share whose verdicts agree best with
        // the recorded outcomes (ties by the mean ranking) is kept. Bots and plans count alike: the thin plans are where full rate failed.
        private static readonly float[] GuardShares = { 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };

        private static void FitGuards(IReadOnlyList<RunRecord> runs, Func<RunRecord, RecordedCell> cellOf, Result result)
        {
            float bestShare = 1f;
            float bestAgreement = -1f;
            float bestRanking = -2f;
            foreach (float share in GuardShares)
            {
                result.QueueGuardShare = share;
                FitSiege(runs, cellOf, result);
                Agreement check = Check(runs, cellOf, result.Efficiency, result.SiegeFactor, share);
                float agreement = check.Runs > 0 ? check.Agreed / (float)check.Runs : 0f;
                float ranking = check.MeanSpearman;
                if (agreement > bestAgreement + 1e-6f || (Math.Abs(agreement - bestAgreement) <= 1e-6f && ranking > bestRanking))
                {
                    bestShare = share;
                    bestAgreement = agreement;
                    bestRanking = ranking;
                }
            }

            result.QueueGuardShare = bestShare;
            FitSiege(runs, cellOf, result);
        }

        // The siege factor: over every recorded run, the gate and base health really lost against the siege model's paper damage
        // with the fitted efficiencies at factor one, each run capped at the structures it had (the recorder's gate sum can jump
        // when a gate is rebuilt, and a run cannot lose more than stood).
        private static void FitSiege(IReadOnlyList<RunRecord> runs, Func<RunRecord, RecordedCell> cellOf, Result result)
        {
            foreach (RunRecord run in runs)
            {
                RecordedCell cell = cellOf(run);
                if (cell == null)
                {
                    continue;
                }

                BalanceRules judge = JudgeOf(cell).Copy();
                judge.SiegeFactor = 1f;
                judge.QueueGuardShare = result.QueueGuardShare;
                int trunkGates = PlanEvaluator.TrunkGates(cell.Input);
                float structures = cell.Input.BaseHealth + cell.Input.GateHealth * trunkGates;
                SiegeState state = SiegeState.Start(cell.Input, trunkGates);
                float paper = 0f;
                float lost = 0f;
                foreach (WaveRecord wave in run.Waves)
                {
                    if (wave.Wave < 1 || wave.Wave > cell.Loads.Count)
                    {
                        continue;
                    }

                    SiegeLine siege = SiegeOf(wave, cell, judge, result.Efficiency, state);
                    paper += siege.GateDamage + siege.BaseDamage;
                    lost += Math.Min(wave.GateLost, cell.Input.GateHealth * trunkGates) + Math.Min(wave.BaseLost, cell.Input.BaseHealth);
                    result.SiegeWaves += siege.GateDamage + siege.BaseDamage > 0f || wave.GateLost + wave.BaseLost > 0f ? 1 : 0;
                }

                result.PaperStructureDamage += Math.Min(paper, structures);
                result.StructureLost += Math.Min(lost, structures);
            }

            result.SiegeFactor = result.PaperStructureDamage > 0f && result.StructureLost > 0f
                ? (float)Math.Round(Math.Max(MinSiegeFactor, Math.Min(MaxSiegeFactor, result.StructureLost / result.PaperStructureDamage)), 3)
                : new BalanceRules().SiegeFactor;
        }

        // The repair drone's busy share: over every recorded wave a repair drone was on the field, the seconds it repaired over the seconds it was there.
        private static void FitRepairDrone(IReadOnlyList<RunRecord> runs, Result result)
        {
            float busy = 0f;
            foreach (RunRecord run in runs)
            {
                foreach (WaveRecord wave in run.Waves)
                {
                    result.RepairDroneSeconds += wave.RepairDroneSeconds;
                    busy += wave.RepairDroneBusy;
                }
            }

            result.RepairDroneBusyShare = result.RepairDroneSeconds > 0f ? (float)Math.Round(Math.Min(1f, busy / result.RepairDroneSeconds), 3) : -1f;
        }

        // The siege of one recorded wave from its realised standing set (the paper guards at the given efficiencies), wearing the state.
        private static SiegeLine SiegeOf(WaveRecord wave, RecordedCell cell, BalanceRules judge, IReadOnlyDictionary<string, float> efficiency, SiegeState state)
        {
            WaveLoad load = cell.Loads[wave.Wave - 1];
            Pressures(wave, cell, judge, efficiency, out _, out _, out SiegeGuards guards);
            return Leak.Resolve(cell.Input, load, judge, guards, state, judge.WaveDelay + load.Window);
        }

        // Ground and air pressure of a recorded wave against its standing towers and the hero, and the same towers as siege guards.
        private static void Pressures(WaveRecord wave, RecordedCell cell, BalanceRules judge, IReadOnlyDictionary<string, float> efficiency, out float ground, out float air, out SiegeGuards guards)
        {
            WaveLoad load = cell.Loads[wave.Wave - 1];
            float stream = Ledger.HeroStream(cell.Input, load, judge);
            float single = Ledger.HeroSingle(cell.Input, load, judge);
            float sky = 0f;
            guards = new SiegeGuards();
            Leak.AddBaseTurrets(ref guards, cell.Input, load, judge);
            foreach ((string id, int level, Cell spot) in wave.Standing)
            {
                int index = cell.Input.Towers.FindIndex(t => t.Id == id);
                if (index < 0 || level < 0 || level >= cell.Input.Towers[index].Levels.Length)
                {
                    continue;
                }

                TowerStats stats = cell.Input.Towers[index];
                float range = stats.Levels[level].Range * cell.Input.BoostOf(id).Range;
                float own = efficiency != null && efficiency.TryGetValue(id, out float e) ? e : 1f;
                SpotCover cover = PlanEvaluator.CoverOf(cell.Input, spot, range);
                Ledger.Option option = Ledger.Capacity(cell.Input, load, judge, index, level, cover, own);
                stream += option.Stream;
                single += option.Single;
                sky += option.Air;
                Leak.AddTower(ref guards, cell.Input, load, judge, index, level, cover, own, 1f);
            }

            ground = load.GroundPressure(stream, single);
            air = load.AirPressure(sky);
        }

        /// <summary>
        /// With these efficiencies, the ledger's verdict on every run (its standing towers hold every wave inside the cap)
        /// against what happened, and per cell the Spearman correlation of the analytic and the realised ranking of the runs.
        /// </summary>
        public static Agreement Check(IReadOnlyList<RunRecord> runs, Func<RunRecord, RecordedCell> cellOf, IReadOnlyDictionary<string, float> efficiency, float siegeFactor = 1f, float queueGuardShare = 1f)
        {
            var agreement = new Agreement();
            var cells = new Dictionary<string, List<(float Analytic, float Realised)>>();
            foreach (RunRecord run in runs)
            {
                RecordedCell cell = cellOf(run);
                if (cell == null)
                {
                    continue;
                }

                Prediction predicted = Predict(run, cell, efficiency, siegeFactor, queueGuardShare);
                bool holds = predicted.Holds;
                bool mono = run.Strategy.StartsWith("mono", StringComparison.Ordinal);
                agreement.Runs++;
                agreement.MonoRuns += mono ? 1 : 0;
                if (holds == run.Won)
                {
                    agreement.Agreed++;
                    agreement.MonoAgreed += mono ? 1 : 0;
                }
                else
                {
                    agreement.Disagreements.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} seed {2}: paper {3} (base {4:0}{5}, margin {6:0.00}{7}), game {8} at wave {9}/{10} base {11:0}",
                        run.Cell, run.Strategy, run.Seed, holds ? "survives" : "falls", predicted.BaseLeft, predicted.FallWave > 0 ? " at wave " + predicted.FallWave : string.Empty, predicted.Margin,
                        predicted.FirstBreak > 0 ? " leaking from wave " + predicted.FirstBreak : string.Empty, run.Won ? "won " + run.Stars + "*" : "lost", run.WaveReached, run.WaveCount, run.BaseHealth));
                }

                if (!cells.TryGetValue(run.Cell, out List<(float, float)> list))
                {
                    cells[run.Cell] = list = new List<(float, float)>();
                }

                float analytic = Leak.Score(holds, predicted.Stars, predicted.BaseLeft, run.Coins, run.WaveCount, predicted.FallWave > 0 ? predicted.FallWave : run.WaveCount);
                list.Add((analytic, run.RealisedScore));
            }

            foreach (KeyValuePair<string, List<(float Analytic, float Realised)>> cell in cells)
            {
                agreement.Rankings.Add((cell.Key, Spearman(cell.Value), cell.Value.Count));
            }

            return agreement;
        }

        /// <summary>
        /// The siege model's verdict on a recorded run from its realised standing sets, with the given efficiencies (1 for types not
        /// listed) and siege factor: the base left after every wave, the wave it falls in, the stars, and the worst wave's cap over pressure.
        /// </summary>
        public static Prediction Predict(RunRecord run, RecordedCell cell, IReadOnlyDictionary<string, float> efficiency, float siegeFactor, float queueGuardShare = 1f)
        {
            var prediction = new Prediction { BaseLeft = cell.Input.BaseHealth, Margin = float.MaxValue };
            BalanceRules judge = JudgeOf(cell).Copy();
            judge.SiegeFactor = siegeFactor;
            judge.QueueGuardShare = queueGuardShare;
            SiegeState state = SiegeState.Start(cell.Input, PlanEvaluator.TrunkGates(cell.Input));
            int cheapestGun = PlanEvaluator.CheapestGun(cell.Input);
            foreach (WaveRecord wave in run.Waves)
            {
                if (wave.Wave < 1 || wave.Wave > cell.Loads.Count)
                {
                    continue;
                }

                WaveLoad load = cell.Loads[wave.Wave - 1];
                // The bots repair their gates by their rule (a gate at risk, or cheaper than a gun); a scripted plan only where it says so.
                if (!run.Scripted && PlanEvaluator.RepairPrice(cell.Input, load, state, cheapestGun, cell.Loads.Count - wave.Wave + 1) >= 0)
                {
                    state.RepairGates(cell.Input);
                }

                Pressures(wave, cell, judge, efficiency, out float ground, out float air, out SiegeGuards guards);
                Leak.Resolve(cell.Input, load, judge, guards, state, judge.WaveDelay + load.Window);
                prediction.BaseLeft = state.BaseLeft;
                if (prediction.BaseLeft <= 0f && prediction.FallWave == 0)
                {
                    prediction.FallWave = wave.Wave;
                }

                float pressure = Math.Max(ground, air);
                if (pressure <= 0f)
                {
                    continue;
                }

                float waveMargin = float.IsInfinity(pressure) ? 0f : judge.CapFor(cell.Cap, load.HasBoss) / pressure;
                prediction.Margin = Math.Min(prediction.Margin, waveMargin);
                if (waveMargin < 1f && prediction.FirstBreak == 0)
                {
                    prediction.FirstBreak = wave.Wave;
                }
            }

            prediction.Margin = prediction.Margin == float.MaxValue ? 0f : prediction.Margin;
            prediction.Stars = Leak.Stars(prediction.BaseLeft, cell.Input.BaseHealth, cell.Input.StarFractions);
            return prediction;
        }

        /// <summary>Spearman's rank correlation of two scores over the same items (1 = same order).</summary>
        public static float Spearman(List<(float Analytic, float Realised)> pairs)
        {
            int n = pairs.Count;
            if (n < 2)
            {
                return 1f;
            }

            float[] a = Ranks(pairs, true);
            float[] b = Ranks(pairs, false);
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                float d = a[i] - b[i];
                sum += d * d;
            }

            return 1f - 6f * sum / (n * ((float)n * n - 1f));
        }

        private static float[] Ranks(List<(float Analytic, float Realised)> pairs, bool analytic)
        {
            int n = pairs.Count;
            var order = new int[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (x, y) => (analytic ? pairs[y].Analytic : pairs[y].Realised).CompareTo(analytic ? pairs[x].Analytic : pairs[x].Realised));
            var ranks = new float[n];
            int at = 0;
            while (at < n)
            {
                int end = at;
                float value = analytic ? pairs[order[at]].Analytic : pairs[order[at]].Realised;
                while (end + 1 < n && (analytic ? pairs[order[end + 1]].Analytic : pairs[order[end + 1]].Realised) == value)
                {
                    end++;
                }

                float rank = (at + end) * 0.5f + 1f;
                for (int i = at; i <= end; i++)
                {
                    ranks[order[i]] = rank;
                }

                at = end + 1;
            }

            return ranks;
        }

        private static BalanceRules JudgeOf(RecordedCell cell) => cell.Judge ?? BalanceRules.Default.HumanWithHero();

        // ---- Wave table: paper beside play for every recorded wave, so a model term can be checked offline -------------------

        public const string WaveTableHeader = "level|d|strategy|seed|won|reached|count|wave|boss|recHealth|paperStream|paperBoss|paperAir|walkStream|walkSingle|walkAir|queueStream|queueSingle|doorStream|doorAir|heroDps|gPressure|aPressure|towerDealt|otherDealt|baseLost|gateLost|towers|paperGroundLeak|paperRestLeak|paperAirLeak|paperGateDamage|paperBaseDamage|heroFell|gatesLeft|baseLeft|groundCount|bossCount|airCount|groundStrike|bossStrike|airStrike|window|baseHealth|gateHealth";

        /// <summary>
        /// One line per recorded wave with the paper side at the given efficiencies and siege factor (pressures, walk kills, the
        /// queue and doorstep guards, the siege's gate and base damage and the state after it) beside what the game recorded
        /// (damage by type, base and gate losses): the fit's working table, written by the fit stage.
        /// </summary>
        public static string WaveTable(IReadOnlyList<RunRecord> runs, Func<RunRecord, RecordedCell> cellOf, IReadOnlyDictionary<string, float> efficiency, float siegeFactor)
        {
            var text = new StringBuilder(WaveTableHeader).Append('\n');
            foreach (RunRecord run in runs)
            {
                RecordedCell cell = cellOf(run);
                if (cell == null)
                {
                    continue;
                }

                BalanceRules judge = JudgeOf(cell).Copy();
                judge.SiegeFactor = siegeFactor;
                SiegeState state = SiegeState.Start(cell.Input, PlanEvaluator.TrunkGates(cell.Input));
                foreach (WaveRecord wave in run.Waves)
                {
                    if (wave.Wave < 1 || wave.Wave > cell.Loads.Count)
                    {
                        continue;
                    }

                    WaveLoad load = cell.Loads[wave.Wave - 1];
                    Pressures(wave, cell, judge, efficiency, out float ground, out float air, out SiegeGuards guards);
                    SiegeLine siege = Leak.Resolve(cell.Input, load, judge, guards, state, judge.WaveDelay + load.Window);
                    text.Append(string.Join("|", run.Level, I(run.Difficulty), run.Strategy, I(run.Seed), run.Won ? "1" : "0", I(run.WaveReached), I(run.WaveCount), I(wave.Wave), load.HasBoss ? "1" : "0",
                        F(wave.Health), F(load.StreamHealth), F(load.BossHealth), F(load.AirHealth), F(guards.WalkStream), F(guards.WalkSingle), F(guards.WalkAir),
                        F(guards.QueueStream), F(guards.QueueSingle), F(guards.DoorStream), F(guards.DoorAir), F(Ledger.HeroDps(cell.Input)), F(ground), F(air),
                        F(wave.TowerDealt), F(wave.OtherDealt), F(wave.BaseLost), F(wave.GateLost), I(wave.Standing.Count),
                        F(siege.GroundLeak), F(siege.RestLeak), F(siege.AirLeak), F(siege.GateDamage), F(siege.BaseDamage), siege.HeroFell ? "1" : "0", F(state.GatesLeft), F(state.BaseLeft),
                        I(load.GroundCount), I(load.BossCount), I(load.AirCount), F(load.GroundStrike), F(load.BossStrike), F(load.AirStrike), F(load.Window),
                        F(cell.Input.BaseHealth), F(cell.Input.GateHealth))).Append('\n');
                }
            }

            return text.ToString();
        }

        // ---- Text form, so stage B can add its runs and the fit can be re-run offline ----------------------------------------

        public static string Write(IEnumerable<RunRecord> runs)
        {
            var text = new StringBuilder();
            foreach (RunRecord run in runs)
            {
                text.Append(string.Join("|", "run", run.Level, I(run.Difficulty), run.Strategy, I(run.Seed), F(run.Step), run.Won ? "won" : "lost", I(run.Stars), I(run.WaveReached), I(run.WaveCount), F(run.BaseHealth), I(run.Coins), run.FreshPlayer ? "fresh" : "expected")).Append('\n');
                foreach (WaveRecord wave in run.Waves)
                {
                    var dealt = new List<string>();
                    foreach (KeyValuePair<string, float> pair in wave.Dealt)
                    {
                        dealt.Add(pair.Key + "=" + F(pair.Value));
                    }

                    var standing = new List<string>();
                    foreach ((string id, int level, Cell cell) in wave.Standing)
                    {
                        standing.Add(id + ":" + I(level) + ":" + I(cell.X) + ":" + I(cell.Y));
                    }

                    text.Append(string.Join("|", "wave", I(wave.Wave), F(wave.Health), F(wave.OtherDealt), string.Join(";", dealt), string.Join(",", standing), F(wave.BaseLost), F(wave.GateLost), F(wave.RepairDroneSeconds), F(wave.RepairDroneBusy))).Append('\n');
                }

                foreach (PlanStep move in run.Moves)
                {
                    text.Append(string.Join("|", "move", I(move.Wave), move.Kind, move.Item, I(move.X), I(move.Y))).Append('\n');
                }
            }

            return text.ToString();
        }

        public static List<RunRecord> Read(IEnumerable<string> lines)
        {
            var runs = new List<RunRecord>();
            RunRecord current = null;
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length >= 12 && parts[0] == "run")
                {
                    current = new RunRecord
                    {
                        Level = parts[1],
                        Difficulty = int.Parse(parts[2], CultureInfo.InvariantCulture),
                        Strategy = parts[3],
                        Seed = int.Parse(parts[4], CultureInfo.InvariantCulture),
                        Step = float.Parse(parts[5], CultureInfo.InvariantCulture),
                        Won = parts[6] == "won",
                        Stars = int.Parse(parts[7], CultureInfo.InvariantCulture),
                        WaveReached = int.Parse(parts[8], CultureInfo.InvariantCulture),
                        WaveCount = int.Parse(parts[9], CultureInfo.InvariantCulture),
                        BaseHealth = float.Parse(parts[10], CultureInfo.InvariantCulture),
                        Coins = int.Parse(parts[11], CultureInfo.InvariantCulture),
                        // Records before round 22d carry no player field; they were played on a fresh save.
                        FreshPlayer = parts.Length < 13 || parts[12] == "fresh",
                    };
                    runs.Add(current);
                }
                else if (parts.Length >= 6 && parts[0] == "wave" && current != null)
                {
                    var wave = new WaveRecord
                    {
                        Wave = int.Parse(parts[1], CultureInfo.InvariantCulture),
                        Health = float.Parse(parts[2], CultureInfo.InvariantCulture),
                        OtherDealt = float.Parse(parts[3], CultureInfo.InvariantCulture),
                        BaseLost = parts.Length > 6 ? float.Parse(parts[6], CultureInfo.InvariantCulture) : 0f,
                        GateLost = parts.Length > 7 ? float.Parse(parts[7], CultureInfo.InvariantCulture) : 0f,
                        RepairDroneSeconds = parts.Length > 8 ? float.Parse(parts[8], CultureInfo.InvariantCulture) : 0f,
                        RepairDroneBusy = parts.Length > 9 ? float.Parse(parts[9], CultureInfo.InvariantCulture) : 0f,
                    };
                    foreach (string item in parts[4].Split(';'))
                    {
                        string[] kv = item.Split('=');
                        if (kv.Length == 2)
                        {
                            wave.Dealt[kv[0]] = float.Parse(kv[1], CultureInfo.InvariantCulture);
                        }
                    }

                    foreach (string item in parts[5].Split(','))
                    {
                        string[] s = item.Split(':');
                        if (s.Length == 4)
                        {
                            wave.Standing.Add((s[0], int.Parse(s[1], CultureInfo.InvariantCulture), new Cell(int.Parse(s[2], CultureInfo.InvariantCulture), int.Parse(s[3], CultureInfo.InvariantCulture))));
                        }
                    }

                    current.Waves.Add(wave);
                }
                else if (parts.Length >= 6 && parts[0] == "move" && current != null)
                {
                    current.Moves.Add(new PlanStep { Wave = int.Parse(parts[1], CultureInfo.InvariantCulture), Kind = parts[2], Item = parts[3], X = int.Parse(parts[4], CultureInfo.InvariantCulture), Y = int.Parse(parts[5], CultureInfo.InvariantCulture) });
                }
            }

            return runs;
        }

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
