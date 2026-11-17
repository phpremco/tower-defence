using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// The affordability check of the first counter tower (round 76): the arithmetic on written waves, and the table of the
    /// first continent, whose warnings are reported and do not fail the suite.
    /// </summary>
    public sealed class CounterAffordabilityTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const int FirstContinentCells = 24;
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        private EnemyDefinition Enemy(int reward, bool flies = false, float armour = 0f, bool boss = false)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            _made.Add(enemy);
            var data = new SerializedObject(enemy);
            data.FindProperty("_reward").intValue = reward;
            data.FindProperty("_flies").boolValue = flies;
            data.FindProperty("_armor").floatValue = armour;
            data.FindProperty("_isBoss").boolValue = boss;
            data.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        private DifficultyProfile Difficulty(float count, float income)
        {
            var difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            _made.Add(difficulty);
            var data = new SerializedObject(difficulty);
            data.FindProperty("_countMultiplier").floatValue = count;
            data.FindProperty("_incomeMultiplier").floatValue = income;
            data.ApplyModifiedPropertiesWithoutUndo();
            return difficulty;
        }

        private static WaveDefinition Wave(params (EnemyDefinition Enemy, int Count)[] groups)
        {
            var list = new List<SpawnGroup>();
            foreach ((EnemyDefinition enemy, int count) in groups)
            {
                list.Add(new SpawnGroup { Enemy = enemy, Count = count, Interval = 1f });
            }

            return new WaveDefinition(list);
        }

        [Test]
        public void KillCoins_SumsTheWavesBefore_ThroughCountAndIncome()
        {
            EnemyDefinition walker = Enemy(7);
            WaveDefinition[] waves = { Wave((walker, 4)), Wave((walker, 3)), Wave((walker, 100)) };
            DifficultyProfile difficulty = Difficulty(1.3f, 0.95f);

            // Counts round up per group (6 and 4 creatures), the wallet floors the scaled total: 10 x 7 x 0.95 = 66.5.
            Assert.AreEqual(66, CounterAffordability.KillCoins(waves, difficulty, 2));
            Assert.AreEqual(0, CounterAffordability.KillCoins(waves, difficulty, 0));
        }

        [Test]
        public void FirstWave_FindsTheTrait_AndSkipsBosses()
        {
            EnemyDefinition walker = Enemy(5);
            EnemyDefinition flyer = Enemy(5, flies: true);
            EnemyDefinition plated = Enemy(5, armour: 20f);
            EnemyDefinition platedBoss = Enemy(50, armour: 100f, boss: true);
            WaveDefinition[] waves = { Wave((walker, 3)), Wave((platedBoss, 1)), Wave((flyer, 2)), Wave((plated, 1)) };

            Assert.AreEqual(3, CounterAffordability.FirstWave(waves, EnemyTraits.Airborne));
            Assert.AreEqual(4, CounterAffordability.FirstWave(waves, EnemyTraits.Armoured));
            Assert.AreEqual(0, CounterAffordability.FirstWave(new[] { Wave((walker, 3)) }, EnemyTraits.Airborne));
        }

        [Test]
        public void Verdict_TellsBountiesFromStartingMoneyFromOutOfReach()
        {
            CounterAffordability.Row Row(int start, int kills, string counter = "Rocket") =>
                new CounterAffordability.Row("Level", 1, EnemyTraits.Airborne, 3, 4, start, kills, counter, counter.Length > 0 ? 120 : 0);

            Assert.AreEqual(CounterAffordability.Verdict.Payable, Row(0, 120).Verdict);
            Assert.IsNull(CounterAffordability.Warning(Row(0, 120)));
            Assert.AreEqual(CounterAffordability.Verdict.NeedsStartingMoney, Row(260, 60).Verdict);
            Assert.AreEqual(60f / 260f, Row(260, 60).StartingMoneyShare, 1e-4f);
            Assert.AreEqual(CounterAffordability.Verdict.OutOfReach, Row(50, 60).Verdict);
            Assert.AreEqual(CounterAffordability.Verdict.NoCounterOwned, Row(500, 500, string.Empty).Verdict);
            StringAssert.Contains("only from the starting money", CounterAffordability.Warning(Row(260, 60)));
        }

        [Test]
        public void FirstContinent_TableIsWritten_WarningsDoNotFail()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run the generator first");

            List<string> warnings = AffordabilityReport.Write();

            var cells = new HashSet<(string, int)>();
            foreach (CounterAffordability.Row row in CounterAffordability.Rows(catalog, AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset").StartingTowerIds, AffordabilityReport.FirstContinentNodes))
            {
                cells.Add((row.Level, row.Difficulty));
                Assert.Greater(row.DeadlineWave, 0);
            }

            Assert.AreEqual(FirstContinentCells, cells.Count, "every cell of the first continent sends a counter trait");
            TestContext.WriteLine($"{warnings.Count} affordability warnings, see {AffordabilityReport.ReportPath}");
        }

        [Test]
        public void FirstFlyers_WhereTheRuleHolds_ArePayableFromBounties()
        {
            // The first-flyer rule (BalanceRules.FlyersAfterBounties, round 77): on the difficulties it names the wave solver moves
            // the first flyers until the bounties pay for a sky gun. This reads the stored waves, so it fails when the rule or
            // the data went out of step (run the solve stage).
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run the generator first");
            Game.Balance.BalanceRules rules = Game.Balance.BalanceRules.Default;
            var failures = new List<string>();
            int judged = 0;
            foreach (CounterAffordability.Row row in CounterAffordability.Rows(catalog, AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset").StartingTowerIds, AffordabilityReport.FirstContinentNodes))
            {
                if (row.Trait != EnemyTraits.Airborne || !rules.FlyersAfterBountiesFor(row.Difficulty) || row.Verdict == CounterAffordability.Verdict.NoCounterOwned)
                {
                    continue;
                }

                judged++;
                if (row.Verdict != CounterAffordability.Verdict.Payable)
                {
                    failures.Add(CounterAffordability.Warning(row));
                }
            }

            Assert.Greater(judged, 0, "the first continent sends flyers on Easy and Medium");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
