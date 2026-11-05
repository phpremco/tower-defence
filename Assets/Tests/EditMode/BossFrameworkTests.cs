using System.Collections.Generic;
using Game.Balance;
using Game.Bosses;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The continent boss framework (Docs/BOSSES.md): crack lines and walks keep off every footprint and take quicksand as a
    /// shortcut, thresholds and phases fire in order, the pool limits hold, the boss goes for the tower that hurt it most,
    /// the data and the Codex text are complete, the art carries every mechanic clip, and the ledger counts Gravemaw's
    /// mechanic as lost firepower while its fight stays inside the HeroCap.
    /// </summary>
    public sealed class BossFrameworkTests
    {
        private const string GravemawPath = "Assets/_Project/Data/Enemies/Gravemaw.asset";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string CodexPath = "Assets/_Project/Data/Levels/CodexCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private static readonly string[] ContinentBosses = { "Gravemaw", "CinderMatriarch", "FrostWarden", "Rotbloom", "Shattered", "SealedOne" };

        // A 16 x 10 field with a road along row 2 and a row of towers between the start and the goal, one gap in it.
        private static ClimbLayer Field(IEnumerable<TerrainPatch> terrain, out List<Vector2Int> towers)
        {
            var road = new List<Vector2Int>();
            for (int x = 0; x < 16; x++)
            {
                road.Add(new Vector2Int(x, 2));
            }

            var layer = new ClimbLayer(new GridMap(new Vector2Int(16, 10), 1f), road, new TerrainMap(terrain), new DecorFormation[0]);
            towers = new List<Vector2Int>();
            for (int x = 2; x < 14; x++)
            {
                if (x != 11)
                {
                    towers.Add(new Vector2Int(x, 5));
                    layer.MarkTower(new Vector2Int(x, 5));
                }
            }

            layer.MarkGate(new Vector2Int(6, 2));
            return layer;
        }

        [Test]
        public void CrackLine_RunsRoundTowersAndGates_NeverUnderThem()
        {
            ClimbLayer layer = Field(new TerrainPatch[0], out List<Vector2Int> towers);
            var path = new List<Vector2Int>();
            Assert.IsTrue(GridRouter.TryFind(new Vector2Int(16, 10), new Vector2Int(4, 1), new Vector2Int(4, 8), c => layer.CrackCost(c, 2f), path), "no crack line found");
            for (int i = 0; i < path.Count; i++)
            {
                Assert.IsFalse(layer.IsFootprint(path[i]), $"the crack runs under a footprint at {path[i]}");
                if (i == 0)
                {
                    continue;
                }

                Vector2Int step = path[i] - path[i - 1];
                if (step.x != 0 && step.y != 0)
                {
                    // A diagonal step past a tower's corner would draw the crack across the tower's edge.
                    Assert.IsFalse(layer.IsFootprint(path[i - 1] + new Vector2Int(step.x, 0)), $"the crack cuts a corner at {path[i]}");
                    Assert.IsFalse(layer.IsFootprint(path[i - 1] + new Vector2Int(0, step.y)), $"the crack cuts a corner at {path[i]}");
                }
            }

        }

        [Test]
        public void CrackLine_TakesQuicksandAsItsShortcut()
        {
            var sand = new List<TerrainPatch>();
            for (int x = 1; x < 15; x++)
            {
                sand.Add(new TerrainPatch { Cell = new Vector2Int(x, 8), Kind = TerrainKind.Quicksand });
            }

            var road = new List<Vector2Int>();
            var layer = new ClimbLayer(new GridMap(new Vector2Int(16, 10), 1f), road, new TerrainMap(sand), new DecorFormation[0]);
            var path = new List<Vector2Int>();
            Assert.IsTrue(GridRouter.TryFind(new Vector2Int(16, 10), new Vector2Int(0, 6), new Vector2Int(15, 6), c => layer.CrackCost(c, 3f), path, false, 1f / 3f));
            int throughSand = path.FindAll(c => layer.KindAt(c) == SurfaceKind.Quicksand).Count;
            Assert.Greater(throughSand, 6, "the crack should detour through the cheap quicksand");
            Assert.IsTrue(float.IsPositiveInfinity(layer.CrackCost(new Vector2Int(20, 20), 3f)), "outside the grid is closed");
        }

        [Test]
        public void Walk_ClimbsRocks_OnlyWhenTheBossClimbs_AndNeverCrossesAFootprint()
        {
            var rock = new DecorFormation(new Vector3(-0.5f, 0.6f, -0.5f), new Vector3(0.5f, 0.6f, 0.5f), false);
            var crystal = new DecorFormation(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.4f, 0.5f, 0.4f), true);
            var layer = new ClimbLayer(new GridMap(new Vector2Int(4, 4), 1f), new List<Vector2Int>(), new TerrainMap(new TerrainPatch[0]), new[] { rock, crystal });
            Vector2Int rockCell = layer.Grid.WorldToCell(rock.Centre);
            Assert.AreEqual(SurfaceKind.Rock, layer.KindAt(rockCell));
            Assert.AreEqual(rock.Top, layer.HeightAt(rockCell), 0.001f, "the climb layer takes the rock's drawn top");
            Assert.AreEqual(SurfaceKind.Fragile, layer.KindAt(layer.Grid.WorldToCell(crystal.Centre)));
            Assert.Less(layer.WalkCost(rockCell, true), float.PositiveInfinity);
            Assert.IsTrue(float.IsPositiveInfinity(layer.WalkCost(rockCell, false)), "a boss that does not climb goes round rocks");
            layer.MarkTower(new Vector2Int(0, 0));
            Assert.IsTrue(float.IsPositiveInfinity(layer.WalkCost(new Vector2Int(0, 0), true)), "a tower is never walked over");
            Assert.Greater(layer.WalkCost(new Vector2Int(0, 1), true), 1f, "beside a tower costs extra, to keep clear");
        }

        [Test]
        public void Thresholds_FireOnceEach_InOrder_EvenAfterOneBigHit()
        {
            var tracker = new ThresholdTracker(new[] { 0.75f, 0.5f, 0.25f });
            Assert.IsFalse(tracker.TryCross(0.8f));
            Assert.IsTrue(tracker.IsDue(0.1f));
            Assert.IsTrue(tracker.TryCross(0.1f));
            Assert.IsTrue(tracker.TryCross(0.1f));
            Assert.IsTrue(tracker.TryCross(0.1f));
            Assert.IsFalse(tracker.TryCross(0.05f));
            Assert.IsTrue(tracker.Done);
            Assert.AreEqual(0.25f, tracker.Last);
        }

        [Test]
        public void Gravemaw_Phases_StartAtTheirShares()
        {
            BossDefinition boss = Gravemaw().Boss;
            Assert.AreEqual(0, boss.PhaseAt(1f));
            Assert.AreEqual(0, boss.PhaseAt(0.76f));
            Assert.AreEqual(1, boss.PhaseAt(0.75f));
            Assert.AreEqual(2, boss.PhaseAt(0.5f));
            Assert.AreEqual(3, boss.PhaseAt(0.2f));
            Assert.AreEqual("Enraged", boss.PhaseName(3));
            CollectionAssert.AreEqual(new[] { 0.75f, 0.5f, 0.25f }, boss.Burrow.Thresholds);
            Assert.AreEqual(BossSignature.Burrow, boss.Signature);
            Assert.AreEqual(BossArrival.ColonyRig, boss.Arrival);
            Assert.AreEqual(0.4f, boss.Burrow.ThrowHealthShare, 0.001f);
            Assert.AreEqual(4f, boss.Burrow.UpsetSeconds, 0.001f);
            Assert.AreEqual(3f, boss.Burrow.TelegraphSeconds, 0.001f);
        }

        [Test]
        public void PoolDives_EachPoolOncePerPhase_AndNeverLongerThanSixSeconds()
        {
            var rules = new PoolDiveRules(10f);
            Assert.AreEqual(PoolDiveRules.AbsoluteMaxSeconds, rules.MaxSeconds, "the data may not stretch the limit");
            Assert.IsTrue(rules.CanUse(1, 0));
            rules.MarkUsed(1, 0);
            Assert.IsFalse(rules.CanUse(1, 0), "a pool once per phase");
            Assert.IsTrue(rules.CanUse(1, 1), "the next phase may use it again");
            Assert.IsTrue(rules.CanUse(2, 0), "another pool is free");
            Assert.LessOrEqual(rules.SecondsFor(100f, 1f), PoolDiveRules.AbsoluteMaxSeconds, "a long swim goes faster, not longer");
            Assert.AreEqual(2f, rules.SecondsFor(4f, 2f), 0.001f);
            Assert.LessOrEqual(Gravemaw().Boss.Dive.MaxSeconds, PoolDiveRules.AbsoluteMaxSeconds);
            Assert.Less(Gravemaw().Boss.Dive.SplashShare, 1f, "under the surface only part of a blast reaches it");
        }

        [Test]
        public void TopDamage_IsTheSourceThatHurtMost_AmongTheEligible()
        {
            var tally = new DamageTally<string>();
            tally.Add("gun", 40f);
            tally.Add("cannon", 90f);
            tally.Add("laser", 60f);
            tally.Add("gun", 30f);
            tally.Add(null, 500f);
            Assert.AreEqual("cannon", tally.Top(null));
            Assert.AreEqual("gun", tally.Top(s => s != "cannon"), "a sold or upset tower is skipped: the next one is the target");
            Assert.AreEqual(70f, tally.Of("gun"), 0.001f);
            tally.Clear();
            Assert.IsNull(tally.Top(null));
        }

        [Test]
        public void EveryContinentBoss_HasCodexText_AndIsListedInTheCodex_OnlyGravemawInTheArena()
        {
            var codex = AssetDatabase.LoadAssetAtPath<CodexCatalog>(CodexPath);
            foreach (string id in ContinentBosses)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
                Assert.IsNotNull(enemy, id);
                Assert.IsTrue(enemy.IsBoss, id);
                Assert.IsNotNull(enemy.Boss, $"{id} has no boss data");
                Assert.IsNotEmpty(enemy.Boss.SignatureText, id);
                Assert.IsNotEmpty(enemy.Boss.TelegraphText, id);
                Assert.IsNotEmpty(enemy.Boss.CounterText, id);
                Assert.Greater(enemy.Boss.Phases.Length, 1, id);
                Assert.IsTrue(enemy.Art != null || id == "SealedOne", $"{id} wears no model");
                CollectionAssert.Contains(new List<EnemyDefinition>(codex.Enemies), enemy, $"{id} is not in the Codex");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog.BossArena, "the developer boss arena is not on the campaign");
            CollectionAssert.AreEqual(new[] { Gravemaw() }, catalog.BossArena.Bosses);
            Assert.IsNotNull(catalog.BossArena.Level);
            WaveDefinition[] waves = catalog.BossArena.WavesFor(Gravemaw());
            Assert.AreEqual(1, waves.Length);
            Assert.IsTrue(new List<SpawnGroup>(waves[0].Groups).Exists(g => g.Enemy == Gravemaw() && g.Source == SpawnSource.Entrance));
            var index = new StoryIndex(catalog);
            Assert.IsNotNull(index.Arena, "the dev story viewer lists no boss arena");
        }

        [Test]
        public void Gravemaw_Art_CarriesEveryMechanicClip_WithItsReportedOffsets()
        {
            ModelArtDefinition art = Gravemaw().Art;
            ModelClipRole[] travel = { ModelClipRole.ClimbUp, ModelClipRole.ClimbDown, ModelClipRole.DiveIn, ModelClipRole.SurfaceBurst, ModelClipRole.BurrowDown, ModelClipRole.SurfaceThrow };
            foreach (ModelClipRole role in travel)
            {
                int index = art.IndexOf(role);
                Assert.GreaterOrEqual(index, 0, $"no {role} clip");
                Assert.Greater(art.Clips[index].EndOffset.x, 0f, $"{role} has no end offset from the report");
                Assert.Greater(art.Clips[index].AnchorForward, 0f, $"{role} has no hole, bank or ledge distance");
            }

            foreach (ModelClipRole role in new[] { ModelClipRole.ClimbLoop, ModelClipRole.Submerged, ModelClipRole.BurrowTravel })
            {
                int index = art.IndexOf(role);
                Assert.GreaterOrEqual(index, 0, $"no {role} cycle");
                Assert.IsTrue(art.Clips[index].Clip.isLooping, $"{role} does not loop");
            }

            ModelClip up = art.Clips[art.IndexOf(ModelClipRole.ClimbUp)];
            Assert.AreEqual(1.2f, up.EndOffset.y, 0.01f, "Climb_Up ends a ledge higher (README round 28)");
            ModelClip toss = art.Clips[art.IndexOf(ModelClipRole.SurfaceThrow)];
            Assert.Greater(toss.EventSeconds, toss.CueSeconds, "the earth bursts before the throw");
            Assert.AreEqual(180f, art.Yaw, "creatures face -Z once imported");
        }

        [Test]
        public void Ledger_CountsGravemawsMechanic_AndItsFightStaysInsideTheHeroCap()
        {
            EnemyDefinition gravemaw = Gravemaw();
            EnemyStats stats = BalanceLedger.Stats(gravemaw);
            Assert.Greater(stats.MechanicLossSeconds, 0f, "the burrows and the throws cost firepower");
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules);
            BalanceRules withHero = rules.HumanWithHero();
            (int levelIndex, LevelDefinition level) = LastLevel(catalog);
            var failures = new List<string>();
            var withoutMechanic = BalanceLedger.Stats(gravemaw);
            withoutMechanic.MechanicLossSeconds = 0f;
            for (int d = 0; d < catalog.Difficulties.Count; d++)
            {
                // The finale as it will be: the last level's last boss wave, its boss swapped for Gravemaw.
                List<WaveSpec> waves = SpecsWithFinale(model, level, catalog.Difficulties[d], stats, out int finale);
                var loads = new List<WaveLoad>();
                List<WaveLine> lines = Ledger.Evaluate(model.InputFor(levelIndex, d), waves, model.Difficulties[d], withHero, loads);
                float most = withHero.HeroCapFor(d) * (1f + withHero.BeatableTolerance);
                if (lines[finale].Pressure > most)
                {
                    failures.Add($"{catalog.Difficulties[d].name}: Gravemaw's finale asks {lines[finale].Pressure:0.00} with the hero (most {most:0.00})");
                }

                string mechanic = BalanceChecks.CheckBossMechanic(gravemaw.name, stats.MechanicLossSeconds, loads[finale].BossFight, withHero);
                if (mechanic != null)
                {
                    failures.Add(mechanic);
                }

                List<WaveSpec> plain = SpecsWithFinale(model, level, catalog.Difficulties[d], withoutMechanic, out _);
                List<WaveLine> plainLines = Ledger.Evaluate(model.InputFor(levelIndex, d), plain, model.Difficulties[d], withHero);
                Assert.Greater(lines[finale].Pressure, plainLines[finale].Pressure, "the ledger must count the mechanic as lost firepower");
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private static List<WaveSpec> SpecsWithFinale(CampaignModel model, LevelDefinition level, DifficultyProfile profile, EnemyStats boss, out int finale)
        {
            var waves = new List<WaveSpec>();
            finale = -1;
            foreach (WaveDefinition wave in level.GetWaves(profile))
            {
                WaveSpec spec = BalanceLedger.Spec(wave, model.Enemies);
                waves.Add(spec);
                if (spec.Groups.Exists(g => g.Enemy.IsBoss))
                {
                    finale = waves.Count - 1;
                }
            }

            Assert.GreaterOrEqual(finale, 0, $"{level.name} has no boss wave");
            WaveSpec last = waves[finale];
            for (int i = 0; i < last.Groups.Count; i++)
            {
                GroupSpec group = last.Groups[i];
                if (group.Enemy.IsBoss)
                {
                    group.Enemy = boss;
                    group.Count = 1;
                    last.Groups[i] = group;
                }
            }

            return waves;
        }

        private static (int Index, LevelDefinition Level) LastLevel(CampaignCatalog catalog)
        {
            int index = -1;
            LevelDefinition last = null;
            int counted = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    index = counted;
                    last = node.Level;
                    counted++;
                }
            }

            return (index, last);
        }

        private static EnemyDefinition Gravemaw()
        {
            var gravemaw = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(GravemawPath);
            Assert.IsNotNull(gravemaw, "run the Continent bosses generator step (Tools/PlayTest/Scripts/ApplyBosses.cs)");
            return gravemaw;
        }
    }
}
