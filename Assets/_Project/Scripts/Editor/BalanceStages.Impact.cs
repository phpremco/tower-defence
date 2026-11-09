using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The impact calculator (round 25; Docs/BALANCE.md "Impact in minutes"). Remco's wish: change a number and see what it does
    /// without a day of machine time. A baseline snapshot holds the paper verdict of every cell (the searched plans, the creative
    /// reference and the obvious family on the calibrated paper) as it was before the change; the impact stage evaluates the same
    /// plans on the current data and reports per cell what moved, which verdicts could flip within the calibration's own error band,
    /// and the exact replay filter for those cells. The calibration's efficiencies are reused: a number inside the model (a factor,
    /// a price, a health) keeps paper and play in step, so no re-measure is needed; a change of the model's or the combat code's
    /// shape (a new rule or term, a new tower type) does need one, and the report says STALE then.
    /// </summary>
    public static partial class BalanceStages
    {
        public const string ImpactPath = "Tools/PlayTest/impact.txt";
        public const string ImpactBaselineFolder = "Tools/PlayTest/impact_baseline";
        public const string ImpactBaselinePath = ImpactBaselineFolder + "/paper.txt";

        /// <summary>The margin band when no records exist to measure the calibration's error from.</summary>
        public const float DefaultImpactBand = 0.15f;
        private const float MinImpactBand = 0.05f;
        private const float MaxImpactBand = 0.5f;

        /// <summary>Rounding to a price step leaves an upgrade ratio a hair under the margin; that is not a broken rule.</summary>
        private const float UpgradeRuleTolerance = 0.03f;

        // The code whose shape the calibration depends on: the paper model and how a hit lands in play. Numbers in data are
        // inputs (InputHash); these files are rules. BalanceRules.cs holds rule numbers, so only its field names count.
        private static readonly string[] ShapeFolders = { "Assets/_Project/Scripts/Balance", "Assets/_Project/Scripts/Combat" };
        private const string RulesFile = "BalanceRules.cs";

        public sealed class ImpactRow
        {
            public string Level = string.Empty;
            public int Node;
            public int Difficulty;
            public string Kind = string.Empty;
            public string Name = string.Empty;
            public bool Holds;
            public int Stars;
            public float BaseLeft;
            public int FallWave;
            public float Margin;
            public float Score;

            public string Key => Level + "|" + Difficulty.ToString(CultureInfo.InvariantCulture) + "|" + Kind + "|" + Name;

            public string Outcome => Holds ? Invariant($"{Stars}* base {BaseLeft:0}") : Invariant($"falls w{FallWave}");

            public string Serialise() =>
                Invariant($"row|{Level}|{Difficulty}|{Node}|{Kind}|{Name}|{(Holds ? 1 : 0)}|{Stars}|{BaseLeft:R}|{FallWave}|{Margin:R}|{Score:R}");

            public static ImpactRow Parse(string line)
            {
                string[] p = line.Split('|');
                if (p.Length < 12 || p[0] != "row")
                {
                    return null;
                }

                return new ImpactRow
                {
                    Level = p[1],
                    Difficulty = int.Parse(p[2], CultureInfo.InvariantCulture),
                    Node = int.Parse(p[3], CultureInfo.InvariantCulture),
                    Kind = p[4],
                    Name = p[5],
                    Holds = p[6] == "1",
                    Stars = int.Parse(p[7], CultureInfo.InvariantCulture),
                    BaseLeft = float.Parse(p[8], CultureInfo.InvariantCulture),
                    FallWave = int.Parse(p[9], CultureInfo.InvariantCulture),
                    Margin = float.Parse(p[10], CultureInfo.InvariantCulture),
                    Score = float.Parse(p[11], CultureInfo.InvariantCulture),
                };
            }
        }

        public sealed class ImpactBaseline
        {
            public string TakenOn = string.Empty;
            public string Inputs = string.Empty;
            public string Shape = string.Empty;
            public string Calibration = string.Empty;
            public float Band = DefaultImpactBand;
            public string BandNote = string.Empty;
            public readonly List<ImpactRow> Rows = new List<ImpactRow>();
            public readonly Dictionary<string, float> UpgradeRatios = new Dictionary<string, float>(StringComparer.Ordinal);
        }

        public sealed class ImpactReport
        {
            public string Text = string.Empty;
            public bool Stale;
            public bool Changed;
            public readonly List<string> FlaggedCells = new List<string>();

            /// <summary>The probe_arg.txt line that replays only the flagged cells (node:difficulty).</summary>
            public string Filter => FlaggedCells.Count > 0 ? "cells=" + string.Join(",", FlaggedCells) : string.Empty;
        }

        // ---- The shape of the model -----------------------------------------------------------------------------------------

        /// <summary>
        /// A fingerprint of the rules the calibration was fitted under: the paper model's and the combat code's sources (the rule
        /// numbers of BalanceRules only by name), the tower and creature types and the difficulties. A number tweak leaves it alone.
        /// </summary>
        public static string ShapeHash(Loaded loaded)
        {
            var text = new StringBuilder();
            foreach (string folder in ShapeFolders)
            {
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                string[] files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.Ordinal);
                foreach (string file in files)
                {
                    text.Append(Path.GetFileName(file));
                    if (Path.GetFileName(file) != RulesFile)
                    {
                        text.Append(File.ReadAllText(file).Replace("\r\n", "\n"));
                    }
                }
            }

            foreach (FieldInfo field in typeof(BalanceRules).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                text.Append(field.Name).Append(':').Append(field.FieldType.Name).Append(';');
            }

            if (loaded.Model != null)
            {
                foreach (KeyValuePair<string, TowerStats> tower in Sorted(loaded.Model.Towers))
                {
                    text.Append(tower.Key).Append(tower.Value.Levels.Length).Append(';');
                }

                foreach (KeyValuePair<string, EnemyStats> enemy in Sorted(loaded.Model.Enemies))
                {
                    text.Append(enemy.Key).Append(';');
                }

                text.Append(loaded.Model.Difficulties.Count);
                foreach (LevelFacts facts in loaded.Model.Facts)
                {
                    text.Append(facts.Key).Append(';');
                }
            }

            return Fnv(text.ToString());
        }

        // ---- The paper of a cell ---------------------------------------------------------------------------------------------

        /// <summary>The searched plans, the creative reference and the obvious family of a cell on the current calibrated paper.</summary>
        public static List<ImpactRow> PaperRows(Loaded loaded, int node, int index, int difficulty)
        {
            MapNode map = loaded.Catalog.Nodes[node];
            BalanceRules rules = loaded.Model.Rules;
            var input = new PlanSearchInput
            {
                LevelKey = map.Level.name,
                Difficulty = difficulty,
                Level = loaded.Model.InputFor(index, difficulty),
                Waves = Waves(loaded.Model, map.Level, loaded.Catalog.Difficulties[difficulty]),
                Stats = loaded.Model.Difficulties[difficulty],
                Judge = rules.HumanWithHero(),
                Cap = rules.HeroCapFor(difficulty) * (1f + rules.BeatableTolerance),
                AllowSell = difficulty == AcceptanceRules.Psycho,
            };
            var rows = new List<ImpactRow>();
            foreach (KeyValuePair<string, StrategySpec> strategy in new SortedDictionary<string, StrategySpec>(LoadStrategies(map.Level.name, difficulty), StringComparer.Ordinal))
            {
                rows.Add(Row(map.Level.name, node, difficulty, "plan", strategy.Key, StrategyExecutor.Evaluate(input, strategy.Value)));
            }

            foreach (PlanSpec plan in LoadPlans(map.Level.name, difficulty))
            {
                rows.Add(Row(map.Level.name, node, difficulty, "plan", plan.Name, PlanEvaluator.Evaluate(input.Level, input.Waves, input.Stats, input.Judge, plan, input.Cap)));
            }

            rows.Add(Row(map.Level.name, node, difficulty, "creative", "creative", StrategyExecutor.Evaluate(input, StrategySpec.Creative(map.Level.name, difficulty))));
            foreach (PlanSpec plan in PlanSearch.ObviousPlans(input))
            {
                rows.Add(Row(map.Level.name, node, difficulty, "obvious", plan.Name, PlanEvaluator.Evaluate(input.Level, input.Waves, input.Stats, input.Judge, plan, input.Cap)));
            }

            return rows;
        }

        private static ImpactRow Row(string level, int node, int difficulty, string kind, string name, PlanOutcome outcome) => new ImpactRow
        {
            Level = level,
            Node = node,
            Difficulty = difficulty,
            Kind = kind,
            Name = name,
            Holds = outcome.Holds,
            Stars = outcome.Stars,
            BaseLeft = outcome.BaseLeft,
            FallWave = outcome.FallWave,
            Margin = outcome.Margin,
            Score = outcome.AnalyticScore,
        };

        /// <summary>Every cell's rows, in catalog order; the filter file narrows them like every other stage.</summary>
        public static List<ImpactRow> PaperRows(Loaded loaded)
        {
            var rows = new List<ImpactRow>();
            int index = 0;
            for (int node = 0; node < loaded.Catalog.Nodes.Count; node++)
            {
                if (loaded.Catalog.Nodes[node].Level == null)
                {
                    continue;
                }

                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    if (WantedCell(node, d))
                    {
                        rows.AddRange(PaperRows(loaded, node, index, d));
                    }
                }

                index++;
            }

            return rows;
        }

        /// <summary>
        /// Remco's upgrade rule per gun: the coins to reach a level buy at least UpgradeMargin times the single-target DPS per coin of
        /// the level below. The smallest ratio over the steps, per tower (one means the rule is met exactly).
        /// </summary>
        public static Dictionary<string, float> UpgradeRuleRatios(CampaignModel model)
        {
            var ratios = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, TowerStats> pair in Sorted(model.Towers))
            {
                TowerStats tower = pair.Value;
                if (!tower.IsGun || tower.Levels.Length < 2)
                {
                    continue;
                }

                float worst = float.MaxValue;
                int total = tower.Levels[0].Cost;
                float below = Dps(tower.Levels[0]) / Mathf.Max(1, total);
                for (int i = 1; i < tower.Levels.Length; i++)
                {
                    total += tower.Levels[i].Cost;
                    float perCoin = Dps(tower.Levels[i]) / Mathf.Max(1, total);
                    worst = Mathf.Min(worst, below > 0f ? perCoin / below / model.Rules.UpgradeMargin : 0f);
                    below = perCoin;
                }

                ratios[tower.Id] = worst;
            }

            return ratios;
        }

        private static float Dps(TowerLevelStats level) => level.Interval > 0f ? level.Damage / level.Interval : 0f;

        // ---- The baseline ------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Takes the baseline: the paper of every cell on the current data and calibration, the input and shape hashes, and the
        /// calibration's error band measured from the records (the efficiencies fitted on the odd levels against those fitted on the
        /// even ones, weighted by capacity). Take it after a committed pipeline run, before changing a number.
        /// </summary>
        public static string SnapshotBaseline()
        {
            Loaded loaded = Load(true);
            if (loaded.Catalog == null)
            {
                return "No campaign catalog: run Tools/Generate first.";
            }

            float band = MeasureBand(loaded, out string bandNote);
            var text = new StringBuilder();
            text.Append(Invariant($"baseline {DateTime.Now:yyyy-MM-dd HH:mm}\n"));
            text.Append("inputs ").Append(loaded.Hash).Append('\n');
            text.Append("shape ").Append(ShapeHash(loaded)).Append('\n');
            text.Append("calibration ").Append(loaded.Calibration != null ? loaded.Calibration.MeasuredOn : "none").Append('\n');
            text.Append(Invariant($"band {band:0.000}\n"));
            text.Append("bandnote ").Append(bandNote).Append('\n');
            foreach (KeyValuePair<string, float> ratio in UpgradeRuleRatios(loaded.Model))
            {
                text.Append(Invariant($"upgrade|{ratio.Key}|{ratio.Value:R}\n"));
            }

            List<ImpactRow> rows = PaperRows(loaded);
            foreach (ImpactRow row in rows)
            {
                text.Append(row.Serialise()).Append('\n');
            }

            Directory.CreateDirectory(ImpactBaselineFolder);
            File.WriteAllText(ImpactBaselinePath, text.ToString());
            return Invariant($"Baseline written to {ImpactBaselinePath}: {rows.Count} paper rows, inputs {loaded.Hash}, band {band:0.00} ({bandNote}).");
        }

        public static ImpactBaseline LoadBaseline()
        {
            if (!File.Exists(ImpactBaselinePath))
            {
                return null;
            }

            var baseline = new ImpactBaseline();
            foreach (string line in File.ReadAllLines(ImpactBaselinePath))
            {
                if (line.StartsWith("row|", StringComparison.Ordinal))
                {
                    ImpactRow row = ImpactRow.Parse(line);
                    if (row != null)
                    {
                        baseline.Rows.Add(row);
                    }

                    continue;
                }

                if (line.StartsWith("upgrade|", StringComparison.Ordinal))
                {
                    string[] p = line.Split('|');
                    if (p.Length == 3)
                    {
                        baseline.UpgradeRatios[p[1]] = float.Parse(p[2], CultureInfo.InvariantCulture);
                    }

                    continue;
                }

                int space = line.IndexOf(' ');
                if (space < 0)
                {
                    continue;
                }

                string key = line.Substring(0, space);
                string value = line.Substring(space + 1);
                switch (key)
                {
                    case "baseline": baseline.TakenOn = value; break;
                    case "inputs": baseline.Inputs = value; break;
                    case "shape": baseline.Shape = value; break;
                    case "calibration": baseline.Calibration = value; break;
                    case "band": baseline.Band = float.Parse(value, CultureInfo.InvariantCulture); break;
                    case "bandnote": baseline.BandNote = value; break;
                }
            }

            return baseline;
        }

        // The calibration's own error as a margin band: the per-tower efficiencies fitted on the odd levels against those fitted on
        // the even ones, relative to the fit on all, weighted by each type's paper capacity. A margin is capacity over pressure, so
        // a relative error in the efficiencies is the same relative error in the margin.
        private static float MeasureBand(Loaded loaded, out string note)
        {
            if (!File.Exists(RecordsPath))
            {
                note = "no records: the default band";
                return DefaultImpactBand;
            }

            List<RunRecord> runs = CalibrationFit.Read(File.ReadAllLines(RecordsPath));
            if (File.Exists(PlanRecordsPath))
            {
                runs.AddRange(CalibrationFit.Read(File.ReadAllLines(PlanRecordsPath)));
            }

            var cells = new Dictionary<string, RecordedCell>();
            RecordedCell CellOf(RunRecord run) => CellFor(run, loaded, cells);
            List<RunRecord> odd = runs.FindAll(r => LevelIndex(loaded.Catalog, r.Level) % 2 == 0);
            List<RunRecord> even = runs.FindAll(r => LevelIndex(loaded.Catalog, r.Level) % 2 == 1);
            if (odd.Count == 0 || even.Count == 0)
            {
                note = "records on one half of the levels only: the default band";
                return DefaultImpactBand;
            }

            CalibrationFit.Result all = CalibrationFit.Fit(runs, CellOf);
            CalibrationFit.Result first = CalibrationFit.Fit(odd, CellOf);
            CalibrationFit.Result second = CalibrationFit.Fit(even, CellOf);
            float weighted = 0f;
            float weight = 0f;
            var parts = new List<string>();
            foreach (KeyValuePair<string, float> pair in all.Efficiency)
            {
                if (!first.Efficiency.TryGetValue(pair.Key, out float a) || !second.Efficiency.TryGetValue(pair.Key, out float b) || pair.Value <= 0f)
                {
                    continue;
                }

                float relative = Mathf.Abs(a - b) / pair.Value;
                float capacity = all.Sums.TryGetValue(pair.Key, out var sums) ? sums.Capacity : 1f;
                weighted += relative * capacity;
                weight += capacity;
                parts.Add(Invariant($"{pair.Key} {a:0.00}/{b:0.00}"));
            }

            float band = weight > 0f ? Mathf.Clamp(weighted / weight, MinImpactBand, MaxImpactBand) : DefaultImpactBand;
            note = Invariant($"odd/even hold-out efficiencies {string.Join(", ", parts)} on {runs.Count} runs");
            return band;
        }

        // ---- The impact --------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The impact of the current data against the baseline: per cell the plans, the creative reference and the obvious family
        /// before and after, the cells whose verdict flipped or could flip within the band, the Psycho-above-Hard ordering and the
        /// upgrade rule, and the replay filter for the flagged cells. Writes impact.txt. <paramref name="bandOverride"/> replaces
        /// the baseline's band.
        /// </summary>
        public static ImpactReport Impact(float? bandOverride = null)
        {
            var report = new ImpactReport();
            Loaded loaded = Load(true);
            ImpactBaseline baseline = LoadBaseline();
            var text = new StringBuilder();
            if (loaded.Catalog == null)
            {
                report.Text = "No campaign catalog: run Tools/Generate first.";
                return report;
            }

            if (baseline == null)
            {
                report.Text = "No baseline (" + ImpactBaselinePath + "): take it on the data before the change with `balance_pipeline.sh snapshot`.";
                File.WriteAllText(ImpactPath, report.Text + "\n");
                return report;
            }

            string shape = ShapeHash(loaded);
            string calibratedShape = loaded.Calibration != null && !string.IsNullOrEmpty(loaded.Calibration.ShapeHashValue) ? loaded.Calibration.ShapeHashValue : baseline.Shape;
            report.Stale = shape != calibratedShape;
            report.Changed = loaded.Hash != baseline.Inputs;
            float band = bandOverride ?? baseline.Band;
            text.Append(Invariant($"Impact {DateTime.Now:yyyy-MM-dd HH:mm}: data now {loaded.Hash}, baseline {baseline.TakenOn} on {baseline.Inputs} (calibration {baseline.Calibration}). {loaded.CalibrationNote}\n"));
            if (report.Stale)
            {
                text.Append(Invariant($"STALE: the model's shape changed ({calibratedShape} -> {shape}): a rule, a term, a tower or creature type, or the combat code. The calibration's\n"));
                text.Append("efficiencies were fitted under other rules, so the paper below is indicative only: re-measure (measure, fit, apply) before trusting it.\n");
            }
            else if (!report.Changed)
            {
                text.Append("No input changed since the baseline: the paper below is the baseline's.\n");
            }
            else
            {
                text.Append("Numbers changed inside the calibrated model (the rules are the same): the calibration's efficiencies are reused; the paper below is the impact.\n");
            }

            text.Append(Invariant($"Band {band:0.00} ({(bandOverride.HasValue ? "override" : baseline.BandNote)}): a row whose margin moved by at least the band, or whose verdict flipped, flags its cell.\n"));
            text.Append("Margin: the worst wave's hero cap over its pressure (above one every wave sits inside the cap); a plan holds when the base survives the leak model.\n\n");

            var before = new Dictionary<string, ImpactRow>(StringComparer.Ordinal);
            foreach (ImpactRow row in baseline.Rows)
            {
                before[row.Key] = row;
            }

            List<ImpactRow> after = PaperRows(loaded);
            var cells = new SortedDictionary<string, List<ImpactRow>>(StringComparer.Ordinal);
            foreach (ImpactRow row in after)
            {
                string cell = row.Level + " " + row.Difficulty.ToString(CultureInfo.InvariantCulture);
                if (!cells.TryGetValue(cell, out List<ImpactRow> list))
                {
                    cells[cell] = list = new List<ImpactRow>();
                }

                list.Add(row);
            }

            text.Append("level        diff    best plan (before -> after)                 plans hold  obvious hold  creative           flag\n");
            var details = new StringBuilder();
            var bestMargins = new Dictionary<string, (float Before, float After)>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<ImpactRow>> cell in cells)
            {
                var reasons = new List<string>();
                ImpactRow best = null;
                int plansHoldBefore = 0, plansHoldAfter = 0, plans = 0, obviousBefore = 0, obviousAfter = 0, obvious = 0;
                string creative = "-";
                foreach (ImpactRow row in cell.Value)
                {
                    ImpactRow was = before.TryGetValue(row.Key, out ImpactRow found) ? found : null;
                    if (row.Kind == "plan")
                    {
                        plans++;
                        plansHoldAfter += row.Holds ? 1 : 0;
                        plansHoldBefore += was != null && was.Holds ? 1 : 0;
                        if (best == null || row.Score > best.Score)
                        {
                            best = row;
                        }
                    }
                    else if (row.Kind == "obvious")
                    {
                        obvious++;
                        obviousAfter += row.Holds ? 1 : 0;
                        obviousBefore += was != null && was.Holds ? 1 : 0;
                    }
                    else
                    {
                        creative = (was != null ? was.Outcome : "new") + " -> " + row.Outcome;
                    }

                    string reason = FlagReason(was, row, band);
                    if (reason != null)
                    {
                        reasons.Add(reason);
                    }

                    details.Append(Invariant($"    {row.Kind,-8} {row.Name,-28} {(was != null ? was.Outcome : "new"),-16} {(was != null ? was.Margin : 0f),5:0.00} -> {row.Outcome,-16} {row.Margin,5:0.00}{(reason != null ? "   <- " + reason : string.Empty)}\n"));
                }

                ImpactRow bestWas = best != null && before.TryGetValue(best.Key, out ImpactRow b) ? b : null;
                string bestText = best == null ? "-" : Invariant($"{ShortName(best.Name),-10} {(bestWas != null ? bestWas.Outcome : "new"),-14} {(bestWas != null ? bestWas.Margin : 0f),4:0.00} -> {best.Outcome,-14} {best.Margin,4:0.00}");
                if (best != null)
                {
                    bestMargins[cell.Key] = (bestWas != null ? bestWas.Margin : best.Margin, best.Margin);
                }

                string[] parts = cell.Key.Split(' ');
                int difficulty = int.Parse(parts[1], CultureInfo.InvariantCulture);
                bool flagged = reasons.Count > 0;
                if (flagged)
                {
                    report.FlaggedCells.Add(cell.Value[0].Node.ToString(CultureInfo.InvariantCulture) + ":" + parts[1]);
                }

                text.Append(Invariant($"{parts[0],-12} {DifficultyName(difficulty),-7} {bestText,-46} {plansHoldBefore}/{plans}->{plansHoldAfter}/{plans,-4} {obviousBefore}/{obvious}->{obviousAfter}/{obvious,-4} {creative,-18} {(flagged ? "FLAG" : "-")}\n"));
                if (flagged)
                {
                    text.Append("  reasons: ").Append(string.Join("; ", reasons)).Append('\n');
                }
            }

            text.Append('\n').Append(OrderingSection(loaded, bestMargins, report)).Append('\n');
            text.Append(UpgradeRuleSection(loaded, baseline, report)).Append('\n');
            if (report.FlaggedCells.Count > 0)
            {
                text.Append(Invariant($"Flagged cells: {report.FlaggedCells.Count}. Replay only these (parallel workers):\n"));
                text.Append("  bash Tools/PlayTest/replay_cells.sh \"").Append(report.Filter).Append("\"\n");
                text.Append("  (the yardstick too: bash Tools/PlayTest/measure_cells.sh \"").Append(report.Filter).Append("\"; then fit, acceptance)\n");
            }
            else
            {
                text.Append("No cell flagged: no paper verdict moved by more than the band.\n");
            }

            if (report.Stale)
            {
                text.Append("\nSTALE: re-measure before trusting these numbers (the rules changed; see the top of this report).\n");
            }

            text.Append("\nPer cell, every row (before -> after):\n").Append(details);
            report.Text = text.ToString();
            File.WriteAllText(ImpactPath, report.Text);
            return report;
        }

        // Why a row flags its cell: its verdict flipped, its stars moved, or its margin moved by the band or more.
        private static string FlagReason(ImpactRow was, ImpactRow now, float band)
        {
            if (was == null)
            {
                return "new row (not in the baseline)";
            }

            if (was.Holds != now.Holds)
            {
                return Invariant($"{now.Name} {(now.Holds ? "now holds" : "now falls")} (was {was.Outcome})");
            }

            if (was.Holds && was.Stars != now.Stars)
            {
                return Invariant($"{now.Name} stars {was.Stars} -> {now.Stars}");
            }

            float delta = now.Margin - was.Margin;
            if (Mathf.Abs(delta) >= band)
            {
                return Invariant($"{now.Name} margin {was.Margin:0.00} -> {now.Margin:0.00} (moved {delta:+0.00;-0.00}, band {band:0.00})");
            }

            return null;
        }

        // Psycho must sit above Hard on paper: the best plan's margin on Psycho below the one on Hard, per level.
        private static string OrderingSection(Loaded loaded, Dictionary<string, (float Before, float After)> bestMargins, ImpactReport report)
        {
            var text = new StringBuilder("Psycho above Hard (best plan's margin, before -> after; Psycho must be the smaller):\n");
            for (int node = 0; node < loaded.Catalog.Nodes.Count; node++)
            {
                MapNode map = loaded.Catalog.Nodes[node];
                if (map.Level == null)
                {
                    continue;
                }

                string hardKey = map.Level.name + " " + AcceptanceRules.Hard.ToString(CultureInfo.InvariantCulture);
                string psychoKey = map.Level.name + " " + AcceptanceRules.Psycho.ToString(CultureInfo.InvariantCulture);
                if (!bestMargins.TryGetValue(hardKey, out (float Before, float After) hard) || !bestMargins.TryGetValue(psychoKey, out (float Before, float After) psycho))
                {
                    continue;
                }

                bool orderedBefore = psycho.Before < hard.Before;
                bool orderedAfter = psycho.After < hard.After;
                string flag = orderedBefore != orderedAfter ? "   <- ordering flipped" : string.Empty;
                if (orderedBefore != orderedAfter)
                {
                    AddFlag(report, node, AcceptanceRules.Hard);
                    AddFlag(report, node, AcceptanceRules.Psycho);
                }

                text.Append(Invariant($"  {map.Level.name,-12} Hard {hard.Before:0.00} -> {hard.After:0.00}   Psycho {psycho.Before:0.00} -> {psycho.After:0.00}   {(orderedAfter ? "ordered" : "NOT ordered")}{flag}\n"));
            }

            return text.ToString();
        }

        private static void AddFlag(ImpactReport report, int node, int difficulty)
        {
            string cell = node.ToString(CultureInfo.InvariantCulture) + ":" + difficulty.ToString(CultureInfo.InvariantCulture);
            if (!report.FlaggedCells.Contains(cell))
            {
                report.FlaggedCells.Add(cell);
            }
        }

        // The upgrade rule per gun (the smallest step ratio over the margin; one or more is met).
        private static string UpgradeRuleSection(Loaded loaded, ImpactBaseline baseline, ImpactReport report)
        {
            var text = new StringBuilder(Invariant($"Upgrade beats a second tower (DPS per coin of a level over {loaded.Model.Rules.UpgradeMargin:0.00} x the level below; the worst step, 1.00 = met exactly):\n"));
            foreach (KeyValuePair<string, float> ratio in UpgradeRuleRatios(loaded.Model))
            {
                float was = baseline.UpgradeRatios.TryGetValue(ratio.Key, out float b) ? b : ratio.Value;
                bool metBefore = was >= 1f - UpgradeRuleTolerance;
                bool metAfter = ratio.Value >= 1f - UpgradeRuleTolerance;
                text.Append(Invariant($"  {ratio.Key,-14} {was:0.00} -> {ratio.Value:0.00}   {(metAfter ? "met" : "BROKEN")}{(metBefore != metAfter ? "   <- changed" : string.Empty)}\n"));
            }

            return text.ToString();
        }

        private static string ShortName(string name)
        {
            int at = name.LastIndexOf('_');
            return at >= 0 && at < name.Length - 1 ? name.Substring(at + 1) : name;
        }

        private static string Fnv(string text)
        {
            uint hash = 2166136261;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }

        private static SortedDictionary<string, T> Sorted<T>(Dictionary<string, T> source) => new SortedDictionary<string, T>(source, StringComparer.Ordinal);

        [MenuItem("Tools/Generate/Balance/8 Impact (paper deltas against the baseline)")]
        private static void MenuImpact()
        {
            ImpactReport report = Impact();
            Debug.Log($"[Balance] impact: {report.FlaggedCells.Count} cells flagged{(report.Stale ? " (STALE: re-measure)" : string.Empty)}; see {ImpactPath}");
        }

        [MenuItem("Tools/Generate/Balance/9 Impact baseline (snapshot the paper)")]
        private static void MenuSnapshot() => Debug.Log("[Balance] " + SnapshotBaseline());
    }
}
