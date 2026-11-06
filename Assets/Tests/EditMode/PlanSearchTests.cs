using System.Collections.Generic;
using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The tester's stage A (round 22): the analytic plan search over every level and difficulty, writing the readable plan
    /// files and the replayable JSON plans under Tools/PlayTest/Plans (same code as Tools/Generate/Balance/5). Plus two checks of
    /// the evaluator itself on the real data: an empty plan falls, and a plan's cover follows its spots.
    /// </summary>
    public sealed class PlanSearchTests
    {
        private const int Medium = 1;

        [Test]
        public void Search_WritesPlansForEveryCell()
        {
            BalanceStages.SearchReport report = BalanceStages.SearchPlans();
            Assert.Greater(report.Cells, 0, "no cells searched (is the campaign generated?)");
            Assert.IsTrue(System.IO.File.Exists(BalanceStages.PlansSummaryPath));
            Debug.Log($"[PlanSearch] {report.Cells} cells; see {BalanceStages.PlansSummaryPath}");
            if (report.Stale)
            {
                Assert.Ignore("The calibration is stale for the current data: re-run the measure, fit and apply stages before trusting these plans.");
            }
        }

        [Test]
        public void Evaluator_EmptyPlanFalls_AndSpotsMatter()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            LevelDefinition level = null;
            int index = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level != null)
                {
                    level = node.Level;
                    break;
                }
            }

            Assert.IsNotNull(level);
            LevelInput input = loaded.Model.InputFor(index, Medium);
            List<WaveSpec> waves = BalanceStages.Waves(loaded.Model, level, loaded.Catalog.Difficulties[Medium]);
            BalanceRules judge = loaded.Model.Rules.HumanWithHero();
            float cap = loaded.Model.Rules.HeroCapFor(Medium);
            PlanOutcome empty = PlanEvaluator.Evaluate(input, waves, loaded.Model.Difficulties[Medium], judge, new PlanSpec { Steps = new PlanStep[0] }, cap);
            Assert.IsFalse(empty.Holds, "a plan without towers must fall");

            TowerStats gun = input.Towers.Find(t => t.IsGun);
            Assert.IsNotNull(gun);
            List<Cell> ranked = input.Geometry.Rank(gun.Levels[0].Range, false, gun.Splash > 0f);
            Assert.Greater(ranked.Count, 1);
            Cell best = ranked[0];
            Cell worst = ranked[ranked.Count - 1];
            PlanOutcome good = PlanEvaluator.Evaluate(input, waves, loaded.Model.Difficulties[Medium], judge, Plan(gun.Id, best), cap);
            PlanOutcome poor = PlanEvaluator.Evaluate(input, waves, loaded.Model.Difficulties[Medium], judge, Plan(gun.Id, worst), cap);
            Assert.GreaterOrEqual(good.MeanCover, poor.MeanCover, "the best-ranked spot must cover at least as much as the worst");
        }

        private static PlanSpec Plan(string tower, Cell cell) =>
            new PlanSpec { Steps = new[] { new PlanStep { Kind = PlanStep.Build, Item = tower, X = cell.X, Y = cell.Y, Wave = 1 } } };
    }
}
