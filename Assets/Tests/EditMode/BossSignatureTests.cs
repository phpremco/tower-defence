using System.Collections.Generic;
using Game.Balance;
using Game.Bosses;
using Game.Combat;
using Game.Economy;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The round-4b signatures (Docs/BOSSES.md): the Cinder Matriarch's choice of cold tower and her road fallback, her
    /// smash limit, her off-road route round the pads and her egg clock; the Frost Warden's freeze, thaw and shatter of a
    /// gate, its rage, and its ice armour with the fire window; and the ledger counting both mechanics inside the cap.
    /// </summary>
    public sealed class BossSignatureTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";

        [Test]
        public void ColdHunt_GoesForTheNearestStandingColdTower()
        {
            var towers = new List<ColdHunt.Candidate>
            {
                new ColdHunt.Candidate(new Vector3(1f, 0f, 0f), false, true),
                new ColdHunt.Candidate(new Vector3(9f, 0f, 0f), true, true),
                new ColdHunt.Candidate(new Vector3(3f, 0f, 0f), true, false),
                new ColdHunt.Candidate(new Vector3(5f, 0f, 4f), true, true),
            };

            Assert.AreEqual(3, ColdHunt.Choose(Vector3.zero, towers), "the nearest cold tower that still stands");
        }

        [Test]
        public void ColdHunt_WithoutColdTowers_TakesTheRoad()
        {
            var towers = new List<ColdHunt.Candidate>
            {
                new ColdHunt.Candidate(new Vector3(1f, 0f, 0f), false, true),
                new ColdHunt.Candidate(new Vector3(4f, 0f, 0f), true, false),
            };

            Assert.AreEqual(-1, ColdHunt.Choose(Vector3.zero, towers), "no cold standing: she walks the road");
            Assert.AreEqual(-1, ColdHunt.Choose(Vector3.zero, new List<ColdHunt.Candidate>()));
        }

        [Test]
        public void ColdHunt_KnowsTheColdTowersFromTheirData()
        {
            var cryo = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cryo.asset");
            var cannon = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cannon.asset");
            Assert.IsTrue(ColdHunt.IsCold(cryo.SlowFactor, LevelSlows(cryo)), "a Cryo chills");
            Assert.IsFalse(ColdHunt.IsCold(cannon.SlowFactor, LevelSlows(cannon)), "a Cannon does not");
        }

        [Test]
        public void ColdHunt_SmashLimit_OnePerApproach_AndAQuarterOfTheField()
        {
            Assert.AreEqual(1, ColdHunt.SmashCap(2), "a small field still loses at most one");
            Assert.AreEqual(2, ColdHunt.SmashCap(8));
            Assert.AreEqual(3, ColdHunt.SmashCap(12));
            Assert.IsTrue(ColdHunt.MayHunt(0, 8, 99f, 14f));
            Assert.IsFalse(ColdHunt.MayHunt(1, 8, 3f, 14f), "right after a smash she walks the road first");
            Assert.IsTrue(ColdHunt.MayHunt(1, 8, 14f, 14f));
            Assert.IsFalse(ColdHunt.MayHunt(2, 8, 99f, 14f), "never more than a quarter of the towers");
        }

        [Test]
        public void OffRoadRoute_WithClearance_KeepsACellFromEveryTower()
        {
            ClimbLayer layer = Wall(out List<Vector2Int> towers);
            var points = new List<Vector3>();
            var goal = new Vector2Int(4, 9);
            Assert.IsTrue(BossRoutes.WalkTo(layer, layer.Grid.CellToWorld(new Vector2Int(4, 2)), goal, true, 1, points), "no route through the gap");
            foreach (Vector3 point in points)
            {
                Vector2Int cell = layer.Grid.WorldToCell(point);
                foreach (Vector2Int tower in towers)
                {
                    Assert.Greater(Mathf.Max(Mathf.Abs(cell.x - tower.x), Mathf.Abs(cell.y - tower.y)), 1, $"the route passes {cell} beside the tower at {tower}");
                }
            }
        }

        [Test]
        public void EggClutch_HatchesAfterItsTime_UnlessShot()
        {
            var clutch = new EggClutch<string>(10f);
            var hatched = new List<string>();
            clutch.Lay("a");
            clutch.Lay("b");
            clutch.Step(6f, hatched);
            Assert.IsEmpty(hatched);
            Assert.AreEqual(4f, clutch.SecondsLeft("a"), 0.001f);
            Assert.IsTrue(clutch.Remove("b"), "shot in time");
            clutch.Step(3.9f, hatched);
            Assert.IsEmpty(hatched);
            clutch.Step(0.2f, hatched);
            CollectionAssert.AreEqual(new[] { "a" }, hatched, "only the egg nobody shot hatches, after ten seconds");
            Assert.AreEqual(0, clutch.Count);
        }

        [Test]
        public void LavaTrail_BurnsWhereItWalked_ForItsLifetime()
        {
            var trail = new LavaTrail(6f, 0.5f, 8);
            trail.Walk(Vector3.zero);
            trail.Walk(new Vector3(0.2f, 0f, 0f));
            trail.Walk(new Vector3(1f, 0f, 0f));
            Assert.IsTrue(trail.Touches(new Vector3(1.3f, 0f, 0f), 0.5f));
            Assert.IsFalse(trail.Touches(new Vector3(3f, 0f, 0f), 0.5f));
            trail.Step(6.1f);
            Assert.IsFalse(trail.Touches(new Vector3(1f, 0f, 0f), 0.5f), "the lava cools after six seconds");
        }

        [Test]
        public void LavaTrail_KeepsItsPatchesInLaidOrder_AndMarksWhereAStretchStarts()
        {
            // The ribbon view walks the ring oldest first and breaks it where the boss lifted (dived, climbed).
            var trail = new LavaTrail(6f, 0.5f, 4);
            trail.Walk(new Vector3(0f, 0f, 0f));
            trail.Walk(new Vector3(1f, 0f, 0f));
            trail.Lift();
            Assert.IsFalse(trail.IsWalking);
            trail.Walk(new Vector3(5f, 0f, 0f));
            trail.Walk(new Vector3(6f, 0f, 0f));
            trail.Walk(new Vector3(7f, 0f, 0f));
            Assert.AreEqual(4, trail.Count, "the ring holds four");
            float[] xs = new float[4];
            bool[] starts = new bool[4];
            for (int k = 0; k < 4; k++)
            {
                int slot = trail.SlotInOrder(k);
                xs[k] = trail.PointAt(slot).x;
                starts[k] = trail.StartsRun(slot);
            }

            CollectionAssert.AreEqual(new[] { 1f, 5f, 6f, 7f }, xs, "oldest first; the first patch was overwritten");
            CollectionAssert.AreEqual(new[] { false, true, false, false }, starts, "a new stretch after the lift");
            Assert.IsTrue(trail.IsWalking);
            Assert.AreEqual(7f, trail.Head.x, 0.001f);
        }

        [Test]
        public void Gate_FreezesIntoABlock_ThawsOnRepair_AndShattersIntoAWreck()
        {
            Tower gate = Gate(out GameObject go);
            try
            {
                Assert.IsTrue(gate.BlocksGround);
                gate.Freeze(8f);
                Assert.IsTrue(gate.IsFrozen);
                Assert.IsTrue(gate.BlocksGround, "an ice block still blocks");
                float health = gate.Health.Current;
                gate.TakeDamage(500f);
                Assert.AreEqual(health, gate.Health.Current, "the ice block shrugs off blows");
                Assert.IsTrue(gate.CanRestore, "the hero can thaw it");
                gate.Restore(0.1f);
                Assert.IsFalse(gate.IsFrozen, "the hero's repair thaws it");
                Assert.IsFalse(gate.IsWrecked, "a thawed gate is not lost");

                gate.Freeze(8f);
                gate.SetBurden(1f);
                Assert.IsTrue(gate.IsBridged, "with the boss on top the walkers follow over");
                Assert.IsFalse(gate.BlocksGround);
                gate.Smash(Vector3.zero);
                Assert.IsTrue(gate.IsWrecked, "it shatters into a wreck");
                Assert.IsFalse(gate.IsFrozen);
                Assert.IsTrue(gate.NeedsRepair, "the wreck can be rebuilt");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GateRage_StacksPerBrokenGate_UpToItsCeiling()
        {
            var rage = new GateRage(0.15f, 3);
            Assert.AreEqual(1f, rage.Pace, 0.001f);
            Assert.AreEqual(0.2f, rage.BoneScale(new Vector2(0.2f, 1f)), 0.001f, "calm icicles are small");
            Assert.IsTrue(rage.GateBroken());
            Assert.IsTrue(rage.GateBroken());
            Assert.AreEqual(1.3f, rage.Pace, 0.001f);
            Assert.IsTrue(rage.GateBroken());
            Assert.IsFalse(rage.GateBroken(), "the rage has a ceiling");
            Assert.AreEqual(1.45f, rage.Pace, 0.001f);
            Assert.AreEqual(1f, rage.BoneScale(new Vector2(0.2f, 1f)), 0.001f, "full rage, full icicles");
        }

        [Test]
        public void IceArmour_SoaksHits_FireOpensADoubleDamageWindow_AndItBreaksForGood()
        {
            var armour = new IceArmour(0.5f, 2f, 5f);
            Assert.AreEqual(0.5f, armour.DamageFactor, 0.001f, "resistance, not immunity");
            Assert.IsTrue(armour.Burn());
            Assert.AreEqual(2f, armour.DamageFactor, 0.001f, "fire: double damage");
            Assert.IsFalse(armour.Step(4.9f));
            Assert.AreEqual(2f, armour.DamageFactor, 0.001f);
            Assert.IsTrue(armour.Step(0.2f), "the crack closes after five seconds");
            Assert.AreEqual(0.5f, armour.DamageFactor, 0.001f);
            Assert.IsTrue(armour.Break());
            Assert.AreEqual(1f, armour.DamageFactor, 0.001f, "exposed: every hit lands in full");
            Assert.IsFalse(armour.Burn(), "nothing left to crack");
            Assert.AreEqual(IceArmour.Stage.Exposed, armour.Current);
        }

        [Test]
        public void Enemy_DamageScale_ShapesEveryHit()
        {
            var go = new GameObject("Boss");
            try
            {
                var enemy = go.AddComponent<Enemy>();
                enemy.enabled = false;
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/FrostWarden.asset");
                enemy.Initialize(definition, new[] { Vector3.zero, Vector3.forward }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
                enemy.SetDamageScale(0.5f);
                enemy.TakeDamageFrom(100f, null, false);
                Assert.AreEqual(definition.MaxHealth - 50f, enemy.Health.Current, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BossData_CinderAndWarden_RunTheirSignatures_FromPoolsWithTheirBrood()
        {
            var cinder = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/CinderMatriarch.asset");
            var warden = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/FrostWarden.asset");
            Assert.AreEqual(BossSignature.HuntTheCold, cinder.Boss.Signature);
            Assert.AreEqual(BossArrival.Pool, cinder.Boss.Arrival);
            Assert.IsNotNull(cinder.Boss.Hunt.Egg, "she lays eggs");
            Assert.IsNotNull(cinder.Boss.Hunt.Hatchling, "they hatch fire larvae");
            Assert.IsNotNull(cinder.Boss.Hunt.Egg.Art, "the egg wears its Blender model");
            Assert.GreaterOrEqual(cinder.Art.IndexOf(ModelClipRole.Smash), 0);
            Assert.Greater(cinder.Art.Clips[cinder.Art.IndexOf(ModelClipRole.Smash)].AnchorForward, 0f, "the smash reaches a tower ahead");
            Assert.GreaterOrEqual(cinder.Art.IndexOf(ModelClipRole.LayEggs), 0);
            Assert.GreaterOrEqual(cinder.Art.IndexOf(ModelClipRole.HuntMove), 0);
            Assert.AreEqual(BossSignature.GateBreaker, warden.Boss.Signature);
            Assert.AreEqual(BossArrival.Pool, warden.Boss.Arrival);
            Assert.AreEqual(1, warden.Art.Parts.Length, "the ice armour is a part of its own");
            Assert.AreEqual(warden.Boss.GateBreaker.ArmourPart, warden.Art.Parts[0].Object);
            foreach (ModelClipRole role in new[] { ModelClipRole.FreezeGate, ModelClipRole.ShatterThrough, ModelClipRole.ArmourBreak })
            {
                Assert.GreaterOrEqual(warden.Art.IndexOf(role), 0, $"no {role} clip");
            }

            Assert.Greater(warden.Art.Clips[warden.Art.IndexOf(ModelClipRole.FreezeGate)].AnchorForward, 0f, "the breath reaches the gate ahead");
            var lavaBall = AssetDatabase.LoadAssetAtPath<Game.PathObjects.PathObjectDefinition>("Assets/_Project/Data/PathObjects/LavaBall.asset");
            Assert.IsTrue(lavaBall.Burns, "the lava ball is the Warden's counter");
        }

        [Test]
        public void Ledger_CountsBothMechanics_InsideTheCap()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
            BalanceRules withHero = rules.HumanWithHero();
            foreach (string id in new[] { "CinderMatriarch", "FrostWarden" })
            {
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
                EnemyStats stats = BalanceLedger.Stats(boss);
                Assert.Greater(stats.MechanicLossSeconds, 0f, $"{id}: its mechanic costs firepower");
                int last = LastLevelIndex(catalog);
                var specs = new List<WaveSpec>();
                foreach (WaveDefinition wave in catalog.Difficulties.Count > 0 ? LastLevel(catalog).GetWaves(catalog.Difficulties[1]) : new WaveDefinition[0])
                {
                    specs.Add(BalanceLedger.Spec(wave, model.Enemies));
                }

                int finale = specs.FindLastIndex(s => s.Groups.Exists(g => g.Enemy.IsBoss));
                Assert.GreaterOrEqual(finale, 0);
                WaveSpec spec = specs[finale];
                for (int i = 0; i < spec.Groups.Count; i++)
                {
                    GroupSpec group = spec.Groups[i];
                    if (group.Enemy.IsBoss)
                    {
                        group.Enemy = stats;
                        group.Count = 1;
                        spec.Groups[i] = group;
                    }
                }

                var loads = new List<WaveLoad>();
                Ledger.Evaluate(model.InputFor(last, 1), specs, model.Difficulties[1], withHero, loads);
                Assert.IsNull(BalanceChecks.CheckBossMechanic(id, stats.MechanicLossSeconds, loads[finale].BossFight, withHero));
            }

            var warden = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/FrostWarden.asset");
            Assert.Greater(BalanceLedger.Stats(warden).Health, warden.MaxHealth, "its ice armour counts as more health to burn through");
        }

        private static int LastLevelIndex(CampaignCatalog catalog)
        {
            int count = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                count += node.Level != null ? 1 : 0;
            }

            return count - 1;
        }

        private static LevelDefinition LastLevel(CampaignCatalog catalog)
        {
            LevelDefinition last = null;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    last = node.Level;
                }
            }

            return last;
        }

        private static List<float> LevelSlows(TowerDefinition tower)
        {
            var slows = new List<float>();
            for (int i = 0; i <= tower.MaxLevelIndex; i++)
            {
                slows.Add(tower.GetLevel(i).SlowFactor);
            }

            return slows;
        }

        private static Tower Gate(out GameObject go)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Blockade.asset");
            Assert.IsNotNull(definition);
            go = new GameObject("Gate");
            var gate = go.AddComponent<Tower>();
            gate.enabled = false;
            var targets = new TargetRegistry();
            gate.Initialize(definition, targets, new ProjectilePool(targets, go.transform), PlayerModifiers.None);
            return gate;
        }

        // A 16 x 10 field, the road along row 2, a wall of towers across row 5 with a three-cell gap: a walk with clearance
        // must take the middle of the gap.
        private static ClimbLayer Wall(out List<Vector2Int> towers)
        {
            var road = new List<Vector2Int>();
            for (int x = 0; x < 16; x++)
            {
                road.Add(new Vector2Int(x, 2));
            }

            var layer = new ClimbLayer(new GridMap(new Vector2Int(16, 10), 1f), road, new TerrainMap(new TerrainPatch[0]), new DecorFormation[0]);
            towers = new List<Vector2Int>();
            for (int x = 0; x < 16; x++)
            {
                if (x < 10 || x > 12)
                {
                    towers.Add(new Vector2Int(x, 5));
                    layer.MarkTower(new Vector2Int(x, 5));
                }
            }

            return layer;
        }
    }
}
