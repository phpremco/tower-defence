using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Economy;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Runs the balance model over every level, difficulty and wave as they are stored, writes the table to
    /// Tools/PlayTest/balance_ledger.txt and fails when a wave leaves its difficulty's band, jumps far above the waves
    /// before it, or late Easy asks nothing (<see cref="BalanceChecks"/>). Since round 83b the band is held on the
    /// solver's own waves: the stored ones with play's correction taken off (the difficulty's factor and the cell's
    /// scalar, both plain factors on creature health). How tough a cell is with that correction is measured, not read
    /// on paper (ClosedLoopAcceptanceTests). The player is the one the progression model
    /// expects there: owned towers only, the research of the levels before spent on upgrades. Per level it also applies the
    /// human lens (<see cref="BalanceChecks.CheckHuman"/>): Easy may not hand a competent player far more firepower than
    /// it needs, and each difficulty must ask more than the one below. Since 2026-11-04 it also checks the human target curve
    /// (<see cref="BalanceChecks.CheckHumanTarget"/>), the campaign ramp within a continent (<see cref="BalanceChecks.CheckRamp"/>)
    /// and that every level stays beatable with the hero and the expected upgrades (<see cref="BalanceChecks.CheckBeatable"/>).
    /// </summary>
    public sealed class BalanceLedgerTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string LedgerPath = "Tools/PlayTest/balance_ledger.txt";

        [Test]
        public void EveryWave_StaysInsideItsDifficultyBand()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
            BalanceRules human = rules.HumanTowersOnly();
            BalanceRules withHero = rules.HumanWithHero();

            var report = new StringBuilder();
            var curve = new StringBuilder("Human lens per level (towers alone, plain waves; Easy Medium Hard Psycho), with the hero, and the research the campaign expects:\n");
            var failures = new List<string>();
            // Round 83: how tough a cell is as stored is play's to say (the closed loop's scalar on the solver's waves), so the
            // paper's own reading of the stored level (the hero cap wave by wave, the order of the difficulties) is a report,
            // as the paper ramp has been since round 22e. Round 83b: what the paper does answer for is asserted again, on the
            // solver's own waves (the stored multipliers over the difficulty's factor and the cell's scalar): the band, the
            // cliff and late Easy of BalanceChecks.Check, the beatable floor on the plain waves, and the hero cap on every
            // wave whose own health is what asks (CheckCap names the ones the solver cannot lower).
            var paperWarnings = new List<string>();
            var held = new List<string>();
            // Round 86: a level's boss is sized by one rule, a multiple of the toughest ordinary creature before it
            // (BalanceRules.BossOverRegular; Round86Tests asserts the rule itself on every stored boss). Only the boss's own
            // share of its wave is the rule's: a boss wave over its band is reported here when its crowd without the boss
            // is inside the band of a plain wave, and one over the hero cap when its crowd stands on the lower edge of its
            // band (the solver gave way all it may) and the wave stays under ByRuleCeiling times the cap. Round 85 excused
            // the whole wave from every check whenever the boss was at or under its multiple.
            var ruling = new List<string>();
            var bossHeld = new List<string>();
            int judged = 0;
            // Per continent, in play order: the level names and their human-lens surplus per difficulty, for the ramp check.
            var rampLevels = new Dictionary<int, List<string>>();
            var rampSurplus = new Dictionary<int, List<float[]>>();
            int index = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                var plainMeans = new List<float>();
                // The level's script, solved once so that it carries the flyer floors the stored waves were made with: the
                // calm reference of a surprise wave is read with it (CampaignModel.CountsAs).
                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                WaveScriptSpec script = model.IndexOf(key) == index ? Game.Editor.WaveScripts.ByKey(key) : null;
                if (script != null)
                {
                    model.BuildWaves(index, script, out _);
                }

                var surplus = new float[catalog.Difficulties.Count];
                var heroLine = new StringBuilder();
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    DifficultyProfile difficulty = catalog.Difficulties[d];
                    List<WaveLine> lines = BalanceLedger.Evaluate(model, index, node.Level, difficulty, d);
                    report.Append(node.Level.name).Append(' ').Append(difficulty.name).Append('\n');
                    foreach (WaveLine line in lines)
                    {
                        report.Append("  ").Append(line).Append('\n');
                    }

                    paperWarnings.AddRange(BalanceChecks.Check(node.Level.name, difficulty.name, d, lines, rules));
                    float correction = rules.DifficultyScaleFor(d) * model.Levels[index].TargetScaleFor(d);
                    List<WaveSpec> own = OwnWaves(model, node.Level, difficulty, correction);
                    string cell = difficulty.name + " (the solver's own waves)";
                    List<WaveLine> ownLines = Ledger.Evaluate(model.InputFor(index, d), own, model.Difficulties[d], rules);
                    foreach (string finding in BalanceChecks.Check(node.Level.name, cell, d, ownLines, rules))
                    {
                        int wave = WaveOf(finding, node.Level.name + " " + cell + " wave ");
                        float crowd = wave > 0 && ownLines[wave - 1].Boss ? CrowdAlone(model.InputFor(index, d), own, wave - 1, model.Difficulties[d], rules) : float.PositiveInfinity;
                        // The band of a boss wave (BossAllowance over the plain band) is what such a wave was always held to;
                        // the crowd alone must fit it now, and what is over it is the boss's own share. Measured in round 86:
                        // the 26 crowds that are named here ask 0.17 to 0.94 of that band;
                        // the plain band is no measure for them, because a boss wave's crowd comes in two or three clusters
                        // on top of what the waves before left on the road.
                        float bossBand = rules.MaxPressure(d) * rules.BossAllowance;
                        if (crowd <= bossBand)
                        {
                            ruling.Add(finding + $"; its crowd without the boss asks {crowd:0.00}, inside the band of a boss wave {bossBand:0.00}");
                        }
                        else
                        {
                            failures.Add(finding + (float.IsInfinity(crowd) ? string.Empty : $"; its crowd without the boss asks {crowd:0.00}, over the band of a boss wave {bossBand:0.00}"));
                        }
                    }

                    // What each wave counts as for the band of the waves after it, on the solver's own measure: a surprise
                    // wave as the calm wave it would have been.
                    var countsAs = new float[own.Count];
                    float[] stored = script != null ? model.CountsAs(index, script, d, StoredWaves(model, node.Level, difficulty)) : null;
                    for (int i = 0; i < countsAs.Length; i++)
                    {
                        countsAs[i] = stored != null ? stored[i] / correction : own[i].HealthMultiplier;
                    }

                    failures.AddRange(CheckCap(node.Level.name, cell, d, own, countsAs, Ledger.Evaluate(model.InputFor(index, d), own, model.Difficulties[d], withHero), rules, held, bossHeld));
                    judged += own.Count;
                    float mean = BalanceChecks.PlainMean(BalanceLedger.Evaluate(model, index, node.Level, difficulty, d, null, human));
                    plainMeans.Add(mean);
                    surplus[d] = mean > 0f ? 1f / mean : 0f;
                    List<WaveLine> beatable = BalanceLedger.Evaluate(model, index, node.Level, difficulty, d, null, withHero);
                    paperWarnings.AddRange(BalanceChecks.CheckBeatable(node.Level.name, difficulty.name, d, beatable, rules));
                    PlayerState player = model.PlayerAt(index, d);
                    heroLine.Append($" {1f / BalanceChecks.PlainMean(beatable):0.00}x (research {player.ResearchSpent})");
                }

                report.Append(BalanceChecks.DescribeHuman(node.Level.name, plainMeans)).Append('\n');
                paperWarnings.AddRange(BalanceChecks.CheckHuman(node.Level.name, plainMeans, rules, model.Levels[index]));
                curve.Append($"  {node.Level.name}: {surplus[0]:0.00}x {surplus[1]:0.00}x {surplus[2]:0.00}x {surplus[3]:0.00}x | with hero{heroLine}\n");
                if (!rampLevels.ContainsKey(node.Region))
                {
                    rampLevels[node.Region] = new List<string>();
                    rampSurplus[node.Region] = new List<float[]>();
                }

                rampLevels[node.Region].Add(node.Level.name);
                rampSurplus[node.Region].Add(surplus);
                index++;
            }

            // The paper ramp is a warning, not a failure (the curator, round 22e): play is the judge of the ramp. The builder moves a
            // cell's scale on stage-B verdicts, and where that pulls against the paper ramp (C1_03-C1_06 eased, C1_01 hardened) stage B
            // wins; the in-play ramp is checked by the acceptance stage on the recorded runs (AcceptanceRules.InPlayRamp).
            var rampWarnings = new List<string>();
            foreach (int region in rampLevels.Keys)
            {
                rampWarnings.AddRange(BalanceChecks.CheckRamp(rampLevels[region], rampSurplus[region], rules));
            }

            report.Append(curve);
            report.Append("Paper ramp (a report check since round 22e; play judges the ramp): ").Append(rampWarnings.Count == 0 ? "monotone within tolerance\n" : "\n  " + string.Join("\n  ", rampWarnings) + "\n");
            if (rampWarnings.Count > 0)
            {
                UnityEngine.Debug.Log("[Balance] paper ramp warnings (not failures): " + string.Join("; ", rampWarnings));
            }

            report.Append("Paper reading of the stored waves (a report check since round 83; the closed loop judges the level): ").Append(paperWarnings.Count == 0 ? "nothing\n" : "\n  " + string.Join("\n  ", paperWarnings) + "\n");
            report.Append($"Boss waves over the boss band whose crowd alone is inside the plain band (round 86, {ruling.Count} findings; the boss's own share, by the boss rule): ").Append(ruling.Count == 0 ? "none\n" : "\n  " + string.Join("\n  ", ruling) + "\n");
            report.Append($"Boss waves over the hero cap with the crowd on the lower edge of its band ({bossHeld.Count} boss waves; asserted: each under {ByRuleCeiling:0.0#} times the cap): ").Append(bossHeld.Count == 0 ? "none\n" : "\n  " + string.Join("\n  ", bossHeld) + "\n");
            report.Append($"Waves over the hero cap that the solver cannot lower ({held.Count} of {judged}; asserted: no other wave is over it, and these stay under {HeldShareMost:P0} of all waves): ").Append(held.Count == 0 ? "none\n" : "\n  " + string.Join("\n  ", held) + "\n");
            System.IO.File.WriteAllText(LedgerPath, report.ToString());
            Assert.Greater(judged, 0);
            Assert.LessOrEqual(held.Count, judged * HeldShareMost, "too many waves sit over the hero cap where the solver cannot lower them:\n" + string.Join("\n", held));
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\nSee " + LedgerPath);
        }

        /// <summary>The lower edge of the solver's band: a ground wave keeps this share of the toughest of the three ground waves before it (WaveSolver.MaxDrop).</summary>
        private const float BandLowerEdge = 0.7f;
        private const int BandMemory = 3;

        /// <summary>Rounding of the stored multipliers (three decimals) against an edge or a floor.</summary>
        private const float EdgeSlack = 1.01f;

        /// <summary>The held waves are the exception: more than this share of all waves and the cap is no rule any more (round 83b: 4 of 364).</summary>
        private const float HeldShareMost = 0.03f;

        /// <summary>
        /// A boss wave that is over the hero cap by the boss rule (its crowd on the lower edge of its band) is still never
        /// more than this many times the cap, on the human lens with the hero. Measured in round 86 on the 42 boss waves of
        /// the first six levels: 32 are over the cap, at 1.04 to 2.50 times it (the median 1.61; Glass Meadows Hard wave 9
        /// the most), so the ceiling stands a notch over the most. They get no budget of their own: how many there are is the boss rule's doing, and the solver's own
        /// waves do not move with a cell's scalar, so the ceiling is what guards a change of the rules.
        /// </summary>
        private const float ByRuleCeiling = 2.6f;

        /// <summary>The solver's floor under every multiplier as its creatures walk in (WaveSolver.MinHealth).</summary>
        private const float SolverFloor = 0.4f;

        // The stored waves of a cell with play's correction taken off: what the solver itself made.
        private static List<WaveSpec> OwnWaves(CampaignModel model, LevelDefinition level, DifficultyProfile difficulty, float correction)
        {
            List<WaveSpec> waves = StoredWaves(model, level, difficulty);
            foreach (WaveSpec spec in waves)
            {
                spec.HealthMultiplier /= correction;
            }

            return waves;
        }

        private static List<WaveSpec> StoredWaves(CampaignModel model, LevelDefinition level, DifficultyProfile difficulty)
        {
            var waves = new List<WaveSpec>();
            foreach (WaveDefinition wave in level.GetWaves(difficulty))
            {
                waves.Add(BalanceLedger.Spec(wave, model.Enemies));
            }

            return waves;
        }

        // BalanceChecks.CheckBeatable on the solver's own waves, with the two cases the solver itself leaves over the cap named
        // instead of failed (WaveSolver.Solve): a plain wave whose pressure is carried in by earlier waves keeps the lower
        // edge of its band (softer creatures would be paper and still not bring it under the cap), and a boss wave whose
        // boss the boss rule sizes keeps its crowd on that edge. A boss wave is so excused only with its crowd on the edge
        // and under ByRuleCeiling times the cap. Every other wave over the cap is a failure: there the wave's own health is
        // what asks too much.
        private static List<string> CheckCap(string level, string difficultyName, int difficulty, List<WaveSpec> waves, float[] countsAs, List<WaveLine> withHero, BalanceRules rules, List<string> held, List<string> bossHeld)
        {
            var failures = new List<string>();
            float mean = BalanceChecks.PlainMean(withHero);
            float floor = rules.MinBeatableSurplusFor(difficulty);
            if (mean > 0f && 1f / mean < floor)
            {
                failures.Add($"{level} {difficultyName}: with the hero a competent player has only {1f / mean:0.00}x the firepower on plain waves (at least {floor:0.00}x)");
            }

            float cap = rules.HeroCapFor(difficulty);
            float most = cap * (1f + rules.BeatableTolerance);
            var recent = new List<float>();
            for (int i = 0; i < waves.Count; i++)
            {
                WaveSpec wave = waves[i];
                bool boss = withHero[i].Boss;
                float trend = 0f;
                foreach (float value in recent)
                {
                    trend = System.Math.Max(trend, value);
                }

                if (withHero[i].Pressure > most)
                {
                    string where = $"{level} {difficultyName} wave {i + 1}{(boss ? " (boss)" : string.Empty)}: pressure {withHero[i].Pressure:0.00} with the hero (most {most:0.00})";
                    bool onEdge = (boss || !wave.AirRaid) && wave.HealthMultiplier <= System.Math.Max(trend * BandLowerEdge, SolverFloor) * EdgeSlack;
                    if (boss && onEdge && withHero[i].Pressure <= cap * ByRuleCeiling)
                    {
                        bossHeld.Add(where + $", its crowd on the lower edge of its band (x{wave.HealthMultiplier:0.00} against x{System.Math.Max(trend * BandLowerEdge, SolverFloor):0.00}), {withHero[i].Pressure / cap:0.00} times the cap");
                    }
                    else if (boss)
                    {
                        failures.Add(where + (onEdge ? $", over {ByRuleCeiling:0.0#} times the cap" : $", its crowd above the lower edge of its band (x{wave.HealthMultiplier:0.00} against x{System.Math.Max(trend * BandLowerEdge, SolverFloor):0.00})"));
                    }
                    else if (onEdge && trend > 0f)
                    {
                        held.Add(where + ", on the lower edge of its band (carried in by earlier waves)");
                    }
                    else
                    {
                        failures.Add(where);
                    }
                }

                if (!wave.AirRaid && !boss)
                {
                    recent.Add(countsAs[i]);
                    if (recent.Count > BandMemory)
                    {
                        recent.RemoveAt(0);
                    }
                }
            }

            return failures;
        }

        // What the crowd of a boss wave asks without its boss, the waves before it as they are: the wave with its boss
        // groups taken out, on the same lens.
        private static float CrowdAlone(LevelInput input, List<WaveSpec> waves, int index, DifficultyStats difficulty, BalanceRules rules)
        {
            var copy = new List<WaveSpec>(waves);
            WaveSpec whole = waves[index];
            var crowd = new WaveSpec { StartDelay = whole.StartDelay, HealthMultiplier = whole.HealthMultiplier, BossHealthScale = whole.BossHealthScale, CounterCheck = whole.CounterCheck, AirRaid = whole.AirRaid };
            crowd.Groups.AddRange(whole.Groups.FindAll(g => g.Enemy != null && !g.Enemy.IsBoss));
            copy[index] = crowd;
            return crowd.Groups.Count == 0 ? 0f : Ledger.Evaluate(input, copy, difficulty, rules)[index].Pressure;
        }

        // The wave number of a finding of BalanceChecks.Check ("<level> <cell> wave <n>: ..." or "... wave <n> (boss): ..."); 0 if none.
        private static int WaveOf(string finding, string prefix)
        {
            if (!finding.StartsWith(prefix))
            {
                return 0;
            }

            int end = prefix.Length;
            while (end < finding.Length && char.IsDigit(finding[end]))
            {
                end++;
            }

            return int.TryParse(finding.Substring(prefix.Length, end - prefix.Length), out int wave) ? wave : 0;
        }

        // GAME_DESIGN, Economy, per continent: its levels on Easy pay about a fifth of the research upgrades tagged to it,
        // on Psycho all of them; power is never paid on Easy. Only some of a continent's twenty levels exist yet, so the
        // stored payouts are checked against the curve, and the curve over all twenty against the upgrades' cost.
        [Test]
        public void MetaEconomy_FollowsTheDesign()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));

            for (int region = 0; region < catalog.Regions.Count; region++)
            {
                if (catalog.NodeCount(region) == 0)
                {
                    continue;
                }

                model.MetaTotals(region, out int researchCost, out int powerCost, out _, out int[] power);
                int easy = 0;
                for (int i = 0; i < rules.LevelsPerContinent; i++)
                {
                    easy += Game.Editor.BalanceSheet.Research(region, i);
                }

                string name = catalog.Regions[region].DisplayName;
                Assert.Greater(researchCost, 0, $"{name} has research upgrades tagged to it");
                // Round 66: a continent's research upgrades are the tiers it opens; its budget (Psycho alone) is the rules' share of them.
                Assert.AreEqual(rules.EasyResearchShare * rules.ResearchBudgetShare, easy / (float)researchCost, 0.02f, $"{name}: all of Easy pays a fifth of the budget share of the tiers it opens");
                Assert.AreEqual(rules.ResearchBudgetShare, easy * rules.ResearchMultipliers[3] / researchCost, 0.03f, $"{name}: all of Psycho pays the budget share of the tiers it opens");
                Assert.AreEqual(0, power[0], $"{name}: Easy pays no power");
                Assert.Greater(powerCost, 0, $"{name} has drone upgrades tagged to it");

                int first = catalog.FirstNode(region);
                for (int i = first; i < first + catalog.NodeCount(region); i++)
                {
                    LevelDefinition level = catalog.Nodes[i].Level;
                    if (level != null)
                    {
                        Assert.AreEqual(Game.Editor.BalanceSheet.Research(region, i - first), level.Reward.ResearchPoints, $"{level.name} pays the curve");
                    }
                }
            }
        }
    }
}
