using System.Collections.Generic;
using Game.Balance;
using Game.Bosses;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The round-4c signatures (Docs/BOSSES.md): Rotbloom's choice of spore targets within the quarter cap, burning
    /// overgrown pads clean and pods shot down in the air; the Shattered's split into thirds and the dead-shard penalty on
    /// the rejoined giant; the Sealed One's choice of the weapon it learns (half damage, never immunity) and the gate it
    /// drains for a phase and gives back; their data, art and arena entries; and the ledger keeping all three inside the cap.
    /// </summary>
    public sealed class BossRound4cTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string EnemiesFolder = "Assets/_Project/Data/Enemies";

        [Test]
        public void SporeTargets_GoForTheFullestClusterInReach_NeverPastAQuarter()
        {
            var towers = new List<Vector3>();
            var eligible = new List<bool>();
            // A cluster of four, a pair, one tower alone, and a big cluster out of reach: twelve towers, a cap of three.
            foreach (Vector3 at in new[] { new Vector3(10f, 0f, 10f), new Vector3(11f, 0f, 10f), new Vector3(10f, 0f, 11f), new Vector3(11f, 0f, 11f) })
            {
                towers.Add(at);
            }

            towers.Add(new Vector3(3f, 0f, 3f));
            towers.Add(new Vector3(4f, 0f, 3f));
            towers.Add(new Vector3(6f, 0f, 14f));
            for (int i = 0; i < 5; i++)
            {
                towers.Add(new Vector3(60f + i * 0.5f, 0f, 60f));
            }

            foreach (Vector3 _ in towers)
            {
                eligible.Add(true);
            }

            var picked = new List<int>();
            OvergrowthTargets.Choose(towers, eligible, new Vector3(8f, 0f, 8f), 16f, 2.6f, 3, 0, picked);
            Assert.AreEqual(3, picked.Count, "three pads, the volley's most");
            foreach (int index in picked)
            {
                Assert.Less(index, 4, "it goes for the fullest cluster within its reach");
            }

            OvergrowthTargets.Choose(towers, eligible, new Vector3(8f, 0f, 8f), 16f, 2.6f, 3, 2, picked);
            Assert.AreEqual(1, picked.Count, "two already overgrown: one more reaches the quarter of twelve");
            OvergrowthTargets.Choose(towers, eligible, new Vector3(8f, 0f, 8f), 16f, 2.6f, 3, 3, picked);
            Assert.IsEmpty(picked, "a quarter of the towers overgrown: no volley");
            Assert.AreEqual(1, OvergrowthTargets.Cap(2), "a small field still gets one pad");

            for (int i = 0; i < 4; i++)
            {
                eligible[i] = false;
            }

            OvergrowthTargets.Choose(towers, eligible, new Vector3(8f, 0f, 8f), 16f, 2.6f, 3, 0, picked);
            CollectionAssert.AreEquivalent(new[] { 4, 5 }, picked, "overgrown towers are left out: the pair is next");
        }

        [Test]
        public void OvergrownPads_BurnClean_WhereABlastReachesThem()
        {
            var pads = new OvergrowthPads();
            Assert.IsTrue(pads.Add(new Vector2Int(2, 2), new Vector3(2.5f, 0f, 2.5f)));
            Assert.IsTrue(pads.Add(new Vector2Int(6, 2), new Vector3(6.5f, 0f, 2.5f)));
            Assert.IsFalse(pads.Add(new Vector2Int(2, 2), new Vector3(2.5f, 0f, 2.5f)), "a pad overgrows once");
            var cleaned = new List<Vector2Int>();
            pads.Burn(new Vector3(3.6f, 0f, 2.5f), 0.7f, cleaned);
            CollectionAssert.AreEqual(new[] { new Vector2Int(2, 2) }, cleaned, "a blast at the pad's edge burns it clean");
            Assert.IsFalse(pads.Contains(new Vector2Int(2, 2)));
            Assert.IsTrue(pads.Contains(new Vector2Int(6, 2)), "a pad out of the blast stays overgrown");
            Assert.IsTrue(pads.Remove(new Vector2Int(6, 2)), "the hero burns a pad clean");
            Assert.AreEqual(0, pads.Count);
        }

        [Test]
        public void SporePod_ShotDownInTheAir_LeavesItsPadClean()
        {
            var volley = new SporeVolley();
            int shot = volley.Launch(new Vector2Int(1, 1));
            int landed = volley.Launch(new Vector2Int(2, 1));
            Assert.AreEqual(2, volley.Flying);
            volley.Shoot(shot);
            Assert.IsFalse(volley.Land(shot), "a pod shot down never overgrows its pad");
            Assert.IsTrue(volley.Land(landed), "a whole pod overgrows its pad");
            Assert.IsFalse(volley.Land(landed), "a pod lands once");
            Assert.AreEqual(1, volley.ShotDown);
            Assert.AreEqual(1, volley.Landed);
            Assert.AreEqual(0, volley.Flying);
            Vector3 from = new Vector3(0f, 2f, 0f);
            Vector3 to = new Vector3(4f, 0f, 0f);
            Assert.AreEqual(from, SporeVolley.Arc(from, to, 3f, 0f));
            Assert.AreEqual(to, SporeVolley.Arc(from, to, 3f, 1f));
            Assert.AreEqual(4f, SporeVolley.Arc(from, to, 3f, 0.5f).y, 0.001f, "the arc tops out its height above the line");
        }

        [Test]
        public void Roots_ShowAStagePerWave_InTheThreeWavesBeforeTheBoss()
        {
            var power = new OvergrowthPower();
            Assert.AreEqual(-1, power.StageFor(4), "four waves before: nothing yet");
            Assert.AreEqual(0, power.StageFor(3));
            Assert.AreEqual(1, power.StageFor(2));
            Assert.AreEqual(2, power.StageFor(1));
            Assert.AreEqual(2, power.StageFor(0), "the boss wave keeps the last stage");
        }

        [Test]
        public void Overgrowth_SlowsTheTower_AndTheHerosRepairBurnsItOff()
        {
            Tower tower = Gate(out GameObject go);
            try
            {
                bool cleared = false;
                tower.OvergrowthChanged += (t, on) => cleared |= !on;
                tower.Overgrow(0.7f);
                Assert.IsTrue(tower.IsOvergrown);
                Assert.IsTrue(tower.CanRestore, "the hero's repair has something to do on an overgrown tower");
                tower.Restore(0.1f);
                Assert.IsFalse(tower.IsOvergrown, "the repair burns the vines off");
                Assert.IsTrue(cleared);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Split_GivesEachShardAThird_AndEachDeadShardTakesItsShareOffTheGiant()
        {
            Assert.AreEqual(1000f, ShardSplit.ShardHealth(3000f, 3), 0.001f);
            Assert.AreEqual(0f, ShardSplit.ShardHealth(3000f, 0), 0.001f);
            Assert.AreEqual(900f, ShardSplit.RejoinHealth(900f, 0, 0.3f), 0.001f, "no shard died: the survivors' health");
            Assert.AreEqual(420f, ShardSplit.RejoinHealth(600f, 1, 0.3f), 0.001f, "one dead shard: 30% off");
            Assert.AreEqual(120f, ShardSplit.RejoinHealth(300f, 2, 0.3f), 0.001f, "two dead shards: 60% off");
            Assert.AreEqual(0f, ShardSplit.RejoinHealth(0f, 3, 0.3f), 0.001f);
        }

        [Test]
        public void Recognition_LearnsTheTypeThatHurtMost_AndOnlyThatTypeLandsHalf()
        {
            var tally = new RecognitionTally<string>();
            Assert.IsNull(tally.Learn(), "nothing hit it: nothing learned");
            tally.Add("Cannon", 300f);
            tally.Add("Rifle", 200f);
            tally.Add("Rifle", 150f);
            tally.Add("Laser", 100f);
            Assert.AreEqual("Rifle", tally.Learn(), "the type with the most damage in the phase");
            Assert.AreEqual(0.5f, tally.ScaleFor("Rifle", 0.5f), "half damage, never immunity");
            Assert.AreEqual(1f, tally.ScaleFor("Cannon", 0.5f), "every other type in full");
            Assert.AreEqual(0f, tally.Of("Rifle"), "each phase counts afresh");
            tally.Add("Cannon", 50f);
            Assert.AreEqual("Cannon", tally.Learn());
            Assert.AreEqual(1f, tally.ScaleFor("Rifle", 0.5f), "the type it learned before lands in full again");
        }

        [Test]
        public void GateDrain_SilencesOneGateForAPhase_AndGivesItBack()
        {
            Tower first = Gate(out GameObject a);
            Tower second = Gate(out GameObject b);
            try
            {
                var drainer = new GateDrainer();
                drainer.Drain(first);
                Assert.IsTrue(first.IsDrained);
                Assert.IsTrue(first.BlocksGround, "a drained gate still blocks the road");
                drainer.Drain(second);
                Assert.IsFalse(first.IsDrained, "the phase ended: the first gate has its gun back");
                Assert.IsTrue(second.IsDrained, "one gate at a time");
                drainer.Release();
                Assert.IsFalse(second.IsDrained, "the fight ended: every gate has its gun back");
                Assert.IsNull(drainer.Drained);
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void NewBosses_HaveTheirSignatureData_ArtAndArenaFights()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            BossArenaDefinition arena = catalog.BossArena;
            var rotbloom = Enemy("Rotbloom");
            var shattered = Enemy("Shattered");
            var sealed1 = Enemy("SealedOne");
            Assert.AreEqual(BossSignature.Overgrowth, rotbloom.Boss.Signature);
            Assert.IsNotNull(rotbloom.Boss.Overgrowth.Pod, "Rotbloom fires no pod");
            Assert.IsTrue(rotbloom.Boss.Overgrowth.Pod.Flies, "its pods are air targets");
            Assert.IsNotNull(rotbloom.Boss.Overgrowth.Vines);
            Assert.IsNotNull(rotbloom.Boss.Overgrowth.Roots);
            Assert.AreEqual(3, shattered.Boss.Split.Shards.Length);
            var routes = new HashSet<ShardRoute>();
            foreach (ShardSpec shard in shattered.Boss.Split.Shards)
            {
                routes.Add(shard.Route);
                Assert.IsNotNull(shard.Enemy.Art, $"{shard.Enemy.name} wears no model");
            }

            Assert.AreEqual(3, routes.Count, "each shard takes another route");
            Assert.AreEqual(BossArrival.Rift, sealed1.Boss.Arrival);
            ModelArtDefinition art = sealed1.Art;
            Assert.IsNotNull(art, "the Sealed One wears its model");
            Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.Move), 0, "it hauls itself along");
            Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.RiseOntoBase), 0);
            Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.Adapt), 0);
            Assert.AreEqual("Release", art.Clips[art.IndexOf(ModelClipRole.Death)].Clip.name, "its end is the release");
            Assert.AreEqual("Release", art.DeathEmissionSet);
            float last = 0f;
            foreach (string look in sealed1.Boss.Recognition.PhaseLooks)
            {
                ModelEmissionSet set = art.EmissionSet(look);
                Assert.IsNotNull(set, $"no glow look {look}");
                Assert.IsNotNull(set.BodyFor(false), $"{look} has no body map");
                Assert.Greater(set.Strength, last - 0.0001f, "the glow grows phase by phase");
                last = set.Strength;
            }

            Assert.IsNotNull(art.EmissionSet("Release").BodyFor(true), "the release has a Low map");
            foreach (string id in new[] { "Rotbloom", "Shattered" })
            {
                Assert.GreaterOrEqual(Enemy(id).Art.IndexOf(ModelClipRole.ClimbUp), 0, $"{id} cannot climb");
            }

            Assert.GreaterOrEqual(rotbloom.Art.IndexOf(ModelClipRole.SporeVolley), 0);
            Assert.GreaterOrEqual(shattered.Art.IndexOf(ModelClipRole.Split), 0);
            Assert.GreaterOrEqual(shattered.Art.IndexOf(ModelClipRole.Rejoin), 0);

            CollectionAssert.IsSubsetOf(new[] { rotbloom, shattered, sealed1 }, new List<EnemyDefinition>(arena.Bosses), "the arena lists all three");
            Assert.AreEqual(4, arena.WavesFor(rotbloom).Length, "Rotbloom's roots show over three short waves before it");
            LevelDefinition bog = arena.LevelFor(rotbloom);
            Assert.IsTrue(new LiquidPools(bog).Count(TerrainKind.Bog) > 0, "Rotbloom's arena has a bog");
            LevelDefinition ruins = arena.LevelFor(shattered);
            int roads = 0;
            int air = 0;
            foreach (EnemyPath path in ruins.Paths)
            {
                roads += path.Flying ? 0 : 1;
                air += path.Flying ? 1 : 0;
            }

            Assert.GreaterOrEqual(roads, 2, "the Shattered's arena has a second road");
            Assert.GreaterOrEqual(air, 1, "the Shattered's arena has an air route");
            Assert.AreEqual(EntranceKind.Ground, ruins.BossEntrance.Kind, "it comes out of the ruin gate");
            foreach (LevelDefinition level in new[] { bog, ruins })
            {
                var errors = new List<string>();
                LevelValidator.Validate(level, errors);
                Assert.IsEmpty(errors, string.Join("\n", errors));
            }
        }

        [Test]
        public void Ledger_CountsTheNewMechanics_InsideTheCap()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            BalanceRules rules = BalanceRules.Default;
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules);
            BalanceRules withHero = rules.HumanWithHero();
            LevelDefinition last = null;
            int levels = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    last = node.Level;
                    levels++;
                }
            }

            foreach (string id in new[] { "Rotbloom", "Shattered", "SealedOne" })
            {
                EnemyStats stats = BalanceLedger.Stats(Enemy(id));
                Assert.Greater(stats.MechanicLossSeconds, 0f, $"{id}: its mechanic costs firepower");
                var specs = new List<WaveSpec>();
                foreach (WaveDefinition wave in last.GetWaves(catalog.Difficulties[1]))
                {
                    specs.Add(BalanceLedger.Spec(wave, model.Enemies));
                }

                int finale = specs.FindLastIndex(s => s.Groups.Exists(g => g.Enemy.IsBoss));
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
                Ledger.Evaluate(model.InputFor(levels - 1, 1), specs, model.Difficulties[1], withHero, loads);
                Assert.IsNull(BalanceChecks.CheckBossMechanic(id, stats.MechanicLossSeconds, loads[finale].BossFight, withHero));
            }

            Assert.Greater(BalanceLedger.Stats(Enemy("SealedOne")).Health, Enemy("SealedOne").MaxHealth, "the learned weapon's lost half counts as more health");
        }

        private static EnemyDefinition Enemy(string id)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemiesFolder}/{id}.asset");
            Assert.IsNotNull(enemy, $"no {id}");
            return enemy;
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
    }
}
