using System.Collections.Generic;
using System.Reflection;
using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88b, the tower package. Remco's playtest of 2026-11-20: the Cannon is needed but a Cannon-heavy build was the
    /// best way to play every level, so its blast hurts two creatures as built and three and four with its stars (a cap per
    /// tower level, read by the game, the balance model and the texts through one accessor). And two things the model did
    /// not know of round 88a: an air-only gun reaches a creature of both layers (the Wisp), and a slow field lasts as long as
    /// its data says.
    /// </summary>
    public sealed class Round88bTowersTests
    {
        private const string TowerFolder = "Assets/_Project/Data/Towers";
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";
        private const int PackedCreatures = 5;
        private const float PackedSpacing = 0.1f;
        private const int RoadLength = 40;
        private const float Tolerance = 0.0001f;
        private static readonly int[] CannonCaps = { 2, 3, 4 };
        private static readonly int[] FlakCaps = { 4, 4, 4 };

        private static TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerFolder}/{id}.asset");

        private static EnemyDefinition Enemy(string id) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/{id}.asset");

        private static IEnumerable<TowerDefinition> Towers()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { TowerFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // ---- the data --------------------------------------------------------------------------------------------

        [Test]
        public void TheCannonsBlast_HurtsTwoThreeFour_ByLevel_AndTheFlaksFourAtEveryLevel()
        {
            TowerDefinition cannon = Tower("Cannon");
            TowerDefinition flak = Tower("Rocket");
            for (int level = 0; level < TowerDefinition.LevelCount; level++)
            {
                Assert.AreEqual(CannonCaps[level], cannon.SplashTargetsAt(level), $"Cannon level {level + 1} (run ApplyRound88bTowers)");
                Assert.AreEqual(FlakCaps[level], flak.SplashTargetsAt(level), $"Flak level {level + 1}");
            }

            Assert.IsTrue(cannon.SplashTargetsGrow, "the Cannon's blast grows with its stars");
            Assert.IsFalse(flak.SplashTargetsGrow, "the Flak's blast is the same at every level");
        }

        [Test]
        public void EveryTowersBlastCaps_MakeSense_AndItsStepsStillSayTheOneThingTheyImprove()
        {
            foreach (TowerDefinition tower in Towers())
            {
                Assert.IsNull(tower.SplashTargetsProblem(), tower.name);
                Assert.IsNull(tower.StepProblem(), tower.name);
            }
        }

        [Test]
        public void ACapThatShrinksWithTheLevel_OrACapWithoutABlast_IsABrokenTower()
        {
            var tower = ScriptableObject.CreateInstance<TowerDefinition>();
            try
            {
                var so = new SerializedObject(tower);
                so.FindProperty("_splashRadius").floatValue = 1f;
                SerializedProperty levels = so.FindProperty("_levels");
                levels.arraySize = TowerDefinition.LevelCount;
                WriteCaps(so, levels, 3, 2, 4);
                StringAssert.Contains("fewer", tower.SplashTargetsProblem());
                WriteCaps(so, levels, 0, 2, 4);
                Assert.IsNotNull(tower.SplashTargetsProblem(), "the whole radius at level 1 and two creatures at level 2 is a step back");
                WriteCaps(so, levels, 2, 3, 4);
                Assert.IsNull(tower.SplashTargetsProblem());
                WriteCaps(so, levels, 0, 0, 0);
                Assert.IsNull(tower.SplashTargetsProblem(), "no cap at all is the whole radius at every level");
                so.FindProperty("_splashRadius").floatValue = 0f;
                WriteCaps(so, levels, 2, 3, 4);
                StringAssert.Contains("no splash radius", tower.SplashTargetsProblem());
            }
            finally
            {
                Object.DestroyImmediate(tower);
            }
        }

        private static void WriteCaps(SerializedObject so, SerializedProperty levels, params int[] caps)
        {
            for (int i = 0; i < caps.Length; i++)
            {
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("SplashTargets").intValue = caps[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ALevelWithoutItsOwnCap_TakesTheTowers()
        {
            var tower = ScriptableObject.CreateInstance<TowerDefinition>();
            try
            {
                var so = new SerializedObject(tower);
                so.FindProperty("_splashRadius").floatValue = 1f;
                so.FindProperty("_splashTargets").intValue = 3;
                SerializedProperty levels = so.FindProperty("_levels");
                levels.arraySize = TowerDefinition.LevelCount;
                WriteCaps(so, levels, 0, 0, 5);
                Assert.AreEqual(3, tower.SplashTargetsAt(0));
                Assert.AreEqual(3, tower.SplashTargetsAt(1));
                Assert.AreEqual(5, tower.SplashTargetsAt(2));
            }
            finally
            {
                Object.DestroyImmediate(tower);
            }
        }

        // ---- the game: a shell fired by a tower of each level ------------------------------------------------------

        [Test]
        public void ACannonShellOnFivePackedCreatures_HurtsTwoThreeFour_ByTheTowersLevel()
        {
            TowerDefinition cannon = Tower("Cannon");
            MethodInfo shoot = typeof(Tower).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo fly = typeof(Projectile).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(shoot, "Tower.Shoot");
            Assert.IsNotNull(fly, "Projectile.Update");
            for (int level = 0; level <= cannon.MaxLevelIndex; level++)
            {
                var go = new GameObject("Tower");
                try
                {
                    var registry = new TargetRegistry();
                    var pack = new List<PackedTarget>();
                    for (int i = 0; i < PackedCreatures; i++)
                    {
                        // A column on the road, a tenth of a cell apart: all five well inside the blast.
                        var creature = new PackedTarget(new Vector3(i * PackedSpacing, 0f, 0f));
                        pack.Add(creature);
                        registry.Add(creature);
                    }

                    var tower = go.AddComponent<Tower>();
                    tower.enabled = false;
                    tower.Initialize(cannon, registry, new ProjectilePool(registry, go.transform), PlayerModifiers.None);
                    tower.SetLevel(level);
                    Assert.AreEqual(level, tower.LevelIndex);
                    // The shell leaves from where it lands, so its first frame is its impact whatever the frame time.
                    tower.MuzzleProvider = target => target;
                    shoot.Invoke(tower, new object[] { cannon.GetLevel(level), pack[0] });
                    Projectile shell = go.GetComponentInChildren<Projectile>(true);
                    Assert.IsNotNull(shell, "the tower fired no shell");
                    fly.Invoke(shell, null);

                    int hurt = 0;
                    for (int i = 0; i < pack.Count; i++)
                    {
                        hurt += pack[i].Taken > 0f ? 1 : 0;
                        Assert.AreEqual(i < CannonCaps[level], pack[i].Taken > 0f, $"level {level + 1}: the blast takes the {CannonCaps[level]} nearest to the impact (creature {i})");
                    }

                    Assert.AreEqual(CannonCaps[level], hurt, $"a level {level + 1} Cannon shell on five packed creatures");
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        // ---- the model reads the game's number ---------------------------------------------------------------------

        [Test]
        public void TheModel_ReadsTheSameCapPerLevel_AsTheGameFiresWith()
        {
            BalanceRules rules = BalanceRules.Default;
            CombatRules combat = RoleMatrix.Rules();
            foreach (TowerDefinition tower in Towers())
            {
                if (tower.SplashRadius <= 0f)
                {
                    continue;
                }

                TowerStats stats = BalanceLedger.Stats(tower);
                for (int level = 0; level <= tower.MaxLevelIndex; level++)
                {
                    int game = tower.SplashTargetsAt(level);
                    Assert.AreEqual(game, stats.Levels[level].SplashTargets, $"{tower.name} level {level + 1}: the model's level stats");
                    // A gun without a cap hurts the whole radius in the game; the ledger then takes the rules' cap, as it always did.
                    Assert.AreEqual(game > 0 ? game : rules.MaxSplashTargets, Ledger.SplashCap(stats, level, rules), Tolerance, $"{tower.name} level {level + 1}: the ledger's cap");
                    RoleMatrix.Source source = RoleMatrix.Towers(combat, level).Find(s => s.Tower == tower);
                    if (source != null && source.Level == level)
                    {
                        Assert.AreEqual(game, source.SplashTargets, $"{tower.name} level {level + 1}: the role matrix");
                    }
                }
            }
        }

        [Test]
        public void TheLedgersBlast_GrowsWithTheCannonsLevel_OnADenseStream_AndNotOnALooseOne()
        {
            BalanceRules rules = BalanceRules.Default;
            TowerStats cannon = BalanceLedger.Stats(Tower("Cannon"));
            const float dense = 5f;
            const float loose = 0.1f;
            for (int level = 0; level < cannon.Levels.Length; level++)
            {
                Assert.AreEqual(CannonCaps[level], Ledger.Splash(cannon.Splash, dense, Ledger.SplashCap(cannon, level, rules)), Tolerance, $"dense, level {level + 1}");
                Assert.AreEqual(1f + 2f * cannon.Splash * loose, Ledger.Splash(cannon.Splash, loose, Ledger.SplashCap(cannon, level, rules)), Tolerance, $"loose, level {level + 1}");
            }
        }

        [Test]
        public void AHandBuiltTower_WithOnlyItsOwnCap_KeepsItAtEveryLevel_AndOneWithoutTakesTheRules()
        {
            BalanceRules rules = BalanceRules.Default;
            var capped = new TowerStats { Splash = 1f, SplashTargets = 3, Levels = new[] { new TowerLevelStats(50, 10f, 3f, 1f), new TowerLevelStats(50, 20f, 3f, 1f, splashTargets: 5) } };
            Assert.AreEqual(3f, Ledger.SplashCap(capped, 0, rules));
            Assert.AreEqual(5f, Ledger.SplashCap(capped, 1, rules));
            var open = new TowerStats { Splash = 1f, Levels = new[] { new TowerLevelStats(50, 10f, 3f, 1f) } };
            Assert.AreEqual(rules.MaxSplashTargets, Ledger.SplashCap(open, 0, rules));
        }

        [Test]
        public void TheInputHash_MovesWithACapOfOneLevel()
        {
            string Hash(int levelTwoCap)
            {
                var model = new CampaignModel();
                TowerStats cannon = BalanceLedger.Stats(Tower("Cannon"));
                cannon.Levels[1].SplashTargets = levelTwoCap;
                model.Towers[cannon.Id] = cannon;
                return InputHash.Of(model);
            }

            Assert.AreEqual(Hash(3), Hash(3));
            Assert.AreNotEqual(Hash(3), Hash(4), "a calibration stamped on other blast caps must read as stale");
        }

        // ---- the player can see it ----------------------------------------------------------------------------------

        [Test]
        public void TheTowerMenu_SaysHowManyTheBlastHurts_AndThatTheNextStepRaisesIt()
        {
            TowerDefinition cannon = Tower("Cannon");
            TowerDefinition flak = Tower("Rocket");
            TowerDefinition gun = Tower("PulseRifle");
            for (int level = 0; level < cannon.MaxLevelIndex; level++)
            {
                string step = Loc.Format("tower.step.blast", CannonCaps[level], CannonCaps[level + 1]);
                Assert.AreEqual(step, BlastRead.Step(cannon, level));
                StringAssert.Contains(step, TowerStepText.Next(cannon, level), "the upgrade preview names the bigger blast beside the step's own thing");
                StringAssert.Contains(Loc.Format("tower.step.damage", cannon.GetLevel(level).Damage, cannon.GetLevel(level + 1).Damage), TowerStepText.Next(cannon, level));
            }

            Assert.AreEqual(Loc.Format("tower.blast.up_to", CannonCaps[0]), BlastRead.Label(cannon, 0));
            Assert.AreEqual(Loc.Format("tower.blast.up_to", CannonCaps[2]), BlastRead.Label(cannon, 2));
            Assert.AreEqual(string.Empty, BlastRead.Step(cannon, cannon.MaxLevelIndex), "nothing follows the top level");
            Assert.AreEqual(Loc.Format("tower.blast.up_to", FlakCaps[0]), BlastRead.Label(flak, 0));
            Assert.AreEqual(string.Empty, BlastRead.Step(flak, 0), "the Flak's blast does not grow");
            Assert.AreEqual(string.Empty, BlastRead.Label(gun, 0), "a gun without a blast says nothing");
            StringAssert.Contains("2", Loc.Format("tower.blast.up_to", 2), "the string table has the line");
            StringAssert.Contains("3", Loc.Format("tower.step.blast", 2, 3));
        }

        // ---- an air-only gun and the creature of both layers: the model follows the game ----------------------------

        [Test]
        public void TheModelsCanHit_IsTheGames_ForEveryPairOfLayers()
        {
            const int all = (int)(TargetLayers.Ground | TargetLayers.Air);
            for (int mask = 0; mask <= all; mask++)
            {
                for (int layer = 0; layer <= all; layer++)
                {
                    Assert.AreEqual(((TargetLayers)mask).CanHit((TargetLayers)layer), Layer.CanHit(mask, layer), $"mask {(TargetLayers)mask}, creature on {(TargetLayers)layer}");
                }
            }
        }

        [Test]
        public void ModelAndGame_Agree_OnTheFlakAndTheWisp_TheFlakAndTheSkitter_TheCannonAndAFlyer()
        {
            Pair("Rocket", "Wisp", true);
            Pair("Rocket", "CoreWisp", true);
            Pair("Rocket", "Skitter", false);
            Pair("Cannon", "Dustbat", false);
            Pair("Cannon", "Wisp", true);
            Pair("PulseRifle", "Dustbat", true);
        }

        private static void Pair(string towerId, string enemyId, bool hits)
        {
            TowerDefinition tower = Tower(towerId);
            EnemyDefinition enemy = Enemy(enemyId);
            Assert.IsNotNull(enemy, enemyId);
            Assert.AreEqual(hits, tower.Targets.CanHit(enemy.Layer), $"the game: {towerId} on {enemyId}");
            Assert.AreEqual(hits, Layer.CanHit(BalanceLedger.Stats(tower).Targets, BalanceLedger.Stats(enemy).Layer), $"the model: {towerId} on {enemyId}");
        }

        private static WaveLoad Load(params (EnemyStats Enemy, int Count)[] groups)
        {
            var level = new LevelInput();
            level.Shape.Lanes.Add(new LevelShape.Lane { PathIndex = 0, Length = RoadLength, TrunkShare = 1f });
            var wave = new WaveSpec();
            foreach ((EnemyStats enemy, int count) in groups)
            {
                wave.Groups.Add(new GroupSpec { Enemy = enemy, Count = count, Interval = 1f });
            }

            return WaveLoad.Of(level, wave, new DifficultyStats(), BalanceRules.Default);
        }

        [Test]
        public void TheWaveLoad_KnowsWhatOfTheGroundAnAirOnlyGunReaches()
        {
            EnemyStats wisp = BalanceLedger.Stats(Enemy("Wisp"));
            EnemyStats skitter = BalanceLedger.Stats(Enemy("Skitter"));
            WaveLoad wisps = Load((wisp, 10));
            Assert.Greater(wisps.StreamHealth, 0f, "the Wisp walks the road: it is in the ground stream");
            Assert.AreEqual(0f, wisps.AirHealth, "and not among the flyers");
            Assert.AreEqual(1f, wisps.StreamShareFor(Layer.Air), Tolerance);
            Assert.AreEqual(1f, wisps.StreamShareFor(Layer.Ground), Tolerance);
            Assert.AreEqual(0f, wisps.StreamShareFor(0));

            WaveLoad skitters = Load((skitter, 10));
            Assert.AreEqual(0f, skitters.StreamShareFor(Layer.Air), "an air-only gun reaches no plain walker");
            Assert.AreEqual(1f, skitters.StreamShareFor(Layer.Ground | Layer.Air), Tolerance);

            WaveLoad mixed = Load((wisp, 10), (skitter, 10));
            Assert.AreEqual(wisps.StreamHealth / (wisps.StreamHealth + skitters.StreamHealth), mixed.StreamShareFor(Layer.Air), Tolerance, "the Wisps' share of the stream's health");
            foreach (WaveGroup group in mixed.Groups)
            {
                Assert.IsFalse(group.Flies, "both walk the road");
            }

            Assert.AreEqual(Layer.Ground | Layer.Air, mixed.Groups[0].Layer);
            Assert.AreEqual(Layer.Ground, mixed.Groups[1].Layer);
        }

        [Test]
        public void TheLedger_CreditsTheFlakForWisps_AndForNoPlainWalker_AndTheCannonForNoFlyer()
        {
            BalanceRules rules = BalanceRules.Default;
            TowerStats flak = BalanceLedger.Stats(Tower("Rocket"));
            TowerStats cannon = BalanceLedger.Stats(Tower("Cannon"));
            var level = new LevelInput();
            SpotCover cover = SpotCover.Ideal(flak.Levels[0].Range);
            WaveLoad wisps = Load((BalanceLedger.Stats(Enemy("Wisp")), 10));
            WaveLoad skitters = Load((BalanceLedger.Stats(Enemy("Skitter")), 10));
            WaveLoad flyers = Load((BalanceLedger.Stats(Enemy("Dustbat")), 10));

            Assert.Greater(Ledger.Capacity(level, wisps, rules, flak, 0, cover).Stream, 0f, "the Flak fires at Wisps on the road");
            Assert.AreEqual(0f, Ledger.Capacity(level, skitters, rules, flak, 0, cover).Stream, "the Flak cannot touch a Skitter");
            Assert.Greater(Ledger.Capacity(level, flyers, rules, flak, 0, cover).Air, 0f);
            Assert.IsTrue(Ledger.Capacity(level, wisps, rules, flak, 0, cover).AirOnly, "it stays a sky gun to the purse");
            Assert.AreEqual(0f, Ledger.Capacity(level, flyers, rules, cannon, 0, cover).Air, "the Cannon cannot aim up");
            Assert.Greater(Ledger.Capacity(level, wisps, rules, cannon, 0, cover).Stream, 0f);
            Assert.Greater(flyers.AirHealth, 0f);
            Assert.AreEqual(0f, flyers.StreamHealth);
        }

        [Test]
        public void TheRouteWalk_LetsAnAirOnlyGunKillWisps_AndNoPlainWalker_AndWalkersDoNotThinItsFire()
        {
            var flak = new TowerStats { Id = "Sky", Targets = Layer.Air, Levels = new[] { new TowerLevelStats(50, 10f, 2.5f, 0.5f) } };
            var waypoints = new[] { new Cell(0, 5), new Cell(RoadLength, 5) };
            var roads = new List<RoadInput> { new RoadInput { PathIndex = 0, Waypoints = waypoints } };
            var gate = new Cell(30, 5);
            var spots = new List<Cell> { new Cell(10, 7) };
            var level = new LevelInput { BaseHealth = 600f, GateHealth = 500f, Towers = new List<TowerStats> { flak } };
            level.Geometry = LevelGeometry.Build(roads, new List<RoadInput>(), new List<Cell> { gate }, gate, spots, 1f);
            level.Shape.Gates.Add(new LevelShape.Gate { OnTrunk = true, Progress = 0.75f });
            level.Shape.Lanes.Add(new LevelShape.Lane { PathIndex = 0, Length = RoadLength, TrunkShare = 1f });
            BalanceRules rules = BalanceRules.Default.HumanWithHero();
            rules.Utilisation = 1f;
            var guns = new List<RouteGun> { new RouteGun { Tower = flak, Level = 0, Spot = new Cell(10, 7), Efficiency = 1f, Slow = 1f } };

            WaveLoad Wave(bool withWalkers)
            {
                // Ten creatures of each kind, one every nine seconds, a cell a second.
                var load = new WaveLoad { StreamHealth = 1000f * (withWalkers ? 2f : 1f), GroundCount = withWalkers ? 20 : 10, MeanHealth = 100f, GroundSpeed = 1f, Span = 90f, Window = 90f, GroundStrike = 2f };
                load.Groups.Add(new WaveGroup { Count = 10, Health = 100f, Speed = 1f, Path = 0, Layer = Layer.Ground | Layer.Air });
                if (withWalkers)
                {
                    load.Groups.Add(new WaveGroup { Count = 10, Health = 100f, Speed = 1f, Path = 0, Layer = Layer.Ground });
                }

                return load;
            }

            RouteResult alone = RouteKill.Walk(level, Wave(false), rules, guns);
            Assert.Greater(alone.WalkStream, 0f, "the air-only gun hurts the creatures of both layers on the road");
            RouteResult mixed = RouteKill.Walk(level, Wave(true), rules, guns);
            Assert.AreEqual(alone.WalkStream, mixed.WalkStream, 1f, "plain walkers take nothing from it and none of its time");

            var walkers = new WaveLoad { StreamHealth = 1000f, GroundCount = 10, MeanHealth = 100f, GroundSpeed = 1f, Span = 90f, Window = 90f, GroundStrike = 2f };
            walkers.Groups.Add(new WaveGroup { Count = 10, Health = 100f, Speed = 1f, Path = 0 });
            Assert.AreEqual(0f, RouteKill.Walk(level, walkers, rules, guns).WalkStream, "an air-only gun kills no plain walker");
        }

        // ---- the slow field lasts what its data says -----------------------------------------------------------------

        [Test]
        public void TheModel_CreditsAFieldForTheSecondsItsDataGivesIt()
        {
            var minute = new GadgetStats { Kind = GadgetKind.SlowField, SlowFactor = 0.5f, Lifetime = 60f };
            var brief = new GadgetStats { Kind = GadgetKind.SlowField, SlowFactor = 0.5f, Lifetime = 20f };
            var lasting = new GadgetStats { Kind = GadgetKind.SlowField, SlowFactor = 0.5f };
            var longWave = new WaveLoad { Span = 55f };
            var shortWave = new WaveLoad { Span = 16f };
            Assert.AreEqual(1f, PlanEvaluator.FieldWaveShare(minute, longWave), Tolerance, "a minute holds the longest wave of the first continent");
            Assert.AreEqual(20f / 55f, PlanEvaluator.FieldWaveShare(brief, longWave), Tolerance, "a 20 s field held a third of a long wave");
            Assert.AreEqual(1f, PlanEvaluator.FieldWaveShare(brief, shortWave), Tolerance);
            Assert.AreEqual(1f, PlanEvaluator.FieldWaveShare(lasting, longWave), Tolerance, "no lifetime: it stays");
        }

        [Test]
        public void TheModelsSlowField_CarriesTheAssetsLifetime()
        {
            var field = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>("Assets/_Project/Data/PathObjects/SlowField.asset");
            Assert.IsNotNull(field);
            Assert.Greater(field.Lifetime, 0f);
            int levels = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                GadgetStats gadget = BalanceLedger.Gadgets(level).Find(g => g.Kind == GadgetKind.SlowField);
                if (gadget == null)
                {
                    continue;
                }

                levels++;
                Assert.AreEqual(field.Lifetime, gadget.Lifetime, Tolerance, level.name);
            }

            Assert.Greater(levels, 0, "no level sells the slow field: the rule is held against nothing");
        }

        private sealed class PackedTarget : ITargetable
        {
            public PackedTarget(Vector3 position)
            {
                Position = position;
            }

            public Vector3 Position { get; }
            public TargetLayers Layer => TargetLayers.Ground;
            public float PathProgress => 0f;
            public bool IsAlive => true;
            public float Taken { get; private set; }

            public void TakeDamage(float amount) => Taken += amount;

            public void ApplySlow(float speedFactor, float seconds)
            {
            }
        }
    }
}
