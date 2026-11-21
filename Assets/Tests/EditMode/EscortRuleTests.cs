using System.Collections.Generic;
using Game.Balance;
using Game.Combat;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88b: the escort rule of a level boss. Its brood is worth the difficulty's share of its bar over a whole walk,
    /// grows along the road, is as tough as the creatures of the boss's own wave, pays the rule's bounty share (nothing),
    /// and is in the data of every level boss. The continent bosses keep their own calls, to the number.
    /// </summary>
    public sealed class EscortRuleTests
    {
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";
        private const string DifficultyFolder = "Assets/_Project/Data/Difficulty";
        private static readonly string[] DifficultyIds = { "Easy", "Medium", "Hard", "Psycho" };

        private static readonly EscortSettings Plain = new EscortSettings(0.5f, 0.4f, 1.6f, 0f, 1.5f, 0f);

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

        [Test]
        public void TheBrood_GrowsAlongTheWalk_AndAWholeWalkSendsItsWorth()
        {
            var brood = new EscortBrood();
            brood.Begin(Plain, 2000f, 200f, 10f, 0f);
            int first = 0;
            int last = 0;
            int previous = 0;
            float secondHalf = 0f;
            for (int call = 0; call < 20; call++)
            {
                float progress = (call + 0.5f) / 20f;
                float before = brood.Sent;
                brood.Cast(10f, progress, out int light, out int heavy);
                Assert.AreEqual(0, heavy, "no plated creature without one to send");
                Assert.GreaterOrEqual(light, previous - 1, "a call is never clearly smaller than the one before it");
                previous = light;
                first = call == 0 ? light : first;
                last = light;
                secondHalf += progress > 0.5f ? brood.Sent - before : 0f;
            }

            Assert.AreEqual(1000f, brood.FullWorth, 0.01f);
            Assert.AreEqual(1000f, brood.Sent, 10f, "a whole unhindered walk sends the worth, to a creature");
            Assert.Greater(last, first * 3, "the last call is several times the first (weights 0.4 to 1.6)");
            Assert.Greater(secondHalf, 0.6f * brood.Sent, "most of the brood comes in the second half of the walk");
        }

        [Test]
        public void ABossKilledAtTheFirstDefence_HasSentLittle()
        {
            EscortSettings rule = EscortRule.For(Difficulty("Medium"), BalanceRules.Default);
            Assert.Less(EscortBrood.ShareBy(rule, 0.15f), 0.1f, "by a gate at 15 % of the road under a tenth of the brood is out");
            Assert.Less(EscortBrood.ShareBy(rule, 0.5f), 0.4f);
            Assert.AreEqual(1f, EscortBrood.ShareBy(rule, 1f), 0.0001f);
            Assert.Less(EscortBrood.Weight(rule, 0f), 1f);
            Assert.Greater(EscortBrood.Weight(rule, 1f), 1f);
        }

        [Test]
        public void ABossHeldForLong_NeverSendsMoreThanTheCap()
        {
            var brood = new EscortBrood();
            brood.Begin(Plain, 2000f, 200f, 10f, 0f);
            for (int call = 0; call < 500; call++)
            {
                brood.Cast(10f, 0.9f, out _, out _);
            }

            Assert.AreEqual(1000f * Plain.MostOfWorth, brood.Sent, 10f);
        }

        [Test]
        public void APlatedShare_WalksInOnlyWhenTheWalkPaysForOne_AndTheWorthStands()
        {
            var rule = new EscortSettings(0.5f, 1f, 1f, 0.25f, 1.5f, 0f);
            var poor = new EscortBrood();
            poor.Begin(rule, 2000f, 200f, 10f, 300f);
            Assert.IsFalse(poor.SendsHeavy, "a quarter of 1000 does not pay for a plated creature of 300");

            var rich = new EscortBrood();
            rich.Begin(rule, 2000f, 200f, 10f, 100f);
            Assert.IsTrue(rich.SendsHeavy);
            int plated = 0;
            for (int call = 0; call < 20; call++)
            {
                rich.Cast(10f, (call + 0.5f) / 20f, out _, out int heavy);
                plated += heavy;
            }

            Assert.AreEqual(2, plated, "a quarter of 1000 buys two of 100 (the third needs the last grant in full)");
            Assert.AreEqual(1000f, rich.Sent, 100f, "the plated share is part of the worth, not on top of it");
        }

        [Test]
        public void EscortWorth_IsSmallOnEasy_AndNeverFallsWithDifficulty()
        {
            float previous = 0f;
            for (int d = 0; d < DifficultyIds.Length; d++)
            {
                DifficultyProfile profile = Difficulty(DifficultyIds[d]);
                Assert.AreEqual(Game.Editor.BalanceSheet.Difficulties[d].EscortWorth, profile.EscortWorth, 0.0001f, DifficultyIds[d] + ": the asset holds the sheet's worth");
                Assert.Greater(profile.EscortWorth, 0f);
                Assert.GreaterOrEqual(profile.EscortWorth, previous, DifficultyIds[d]);
                previous = profile.EscortWorth;
            }

            Assert.LessOrEqual(Difficulty("Easy").EscortWorth, 0.5f * Difficulty("Medium").EscortWorth, "Easy: a small brood");
        }

        [Test]
        public void TheRulesGrowTheBrood_AndPayNoBounty()
        {
            BalanceRules rules = BalanceRules.Default;
            Assert.Greater(rules.EscortEndWeight, rules.EscortStartWeight * 2f, "the road behind the first defence gets worse, not better");
            Assert.AreEqual(0f, rules.EscortBountyShare, "a brood that grows with the walk must not pay for letting the boss walk");
            Assert.Greater(rules.EscortStartShare, 0.5f, "the ledger's brood joins past halfway");
            Assert.GreaterOrEqual(rules.EscortMostOfWorth, 1f);
        }

        [Test]
        public void AStagedBoss_SendsCreaturesAsToughAsItsWave_AtTheRulesBounty_AndMoreAsItWalks()
        {
            EnemyDefinition minion = Definition(10f, 1f, false, null);
            BossAbilityDefinition call = Spawn(minion, 3, 10f, 10f);
            EnemyDefinition boss = Definition(1000f, 1f, true, new[] { call });
            var go = new GameObject("Boss");
            _made.Add(go);
            var enemy = go.AddComponent<Enemy>();
            var powers = go.AddComponent<BossAbilities>();
            var sent = new List<int>();
            int inCall = 0;
            int plain = 0;
            powers.Initialize(new StructureRegistry(), (d, b) => plain++, (origin, range) => null);
            powers.SetEscortRule(new EscortSettings(0.5f, 0.4f, 1.6f, 0f, 1.5f, 0f), (definition, from, health, bounty) =>
            {
                Assert.AreSame(minion, definition);
                Assert.AreSame(enemy, from);
                Assert.AreEqual(2f, health, 0.0001f, "the multiplier of the boss's own wave, as staged");
                Assert.AreEqual(0f, bounty, 0.0001f);
                inCall++;
            });
            enemy.Initialize(boss, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
            powers.StageEscort(100f, 2f, null);
            Assert.IsTrue(powers.Brood.Active);
            Assert.AreEqual(500f, powers.Brood.FullWorth, 0.01f, "half of a bar of 1000");

            for (int call10 = 0; call10 < 9; call10++)
            {
                enemy.Step(10f);
                inCall = 0;
                powers.Step(10f);
                sent.Add(inCall);
            }

            Assert.AreEqual(0, plain, "under the rule the plain count is not used");
            Assert.Greater(sent[8], sent[0] * 2, "the ninth call is far larger than the first");
            Assert.AreEqual(powers.Brood.Sent, Sum(sent) * 20f, 0.01f, "each creature costs its book health times the wave's multiplier");

            // Sent out again from the pool without a stage (a test, a menu guardian): the plain count is back.
            enemy.Deactivate();
            enemy.Initialize(boss, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
            Assert.IsFalse(powers.Brood.Active);
            powers.Step(10f);
            Assert.AreEqual(3, plain);
        }

        [Test]
        public void AnEscortCreature_PaysTheBountyShare()
        {
            EnemyDefinition minion = Definition(10f, 1f, false, null);
            var so = new SerializedObject(minion);
            so.FindProperty("_reward").intValue = 7;
            so.ApplyModifiedPropertiesWithoutUndo();
            var go = new GameObject("Escort");
            _made.Add(go);
            var enemy = go.AddComponent<Enemy>();
            enemy.Initialize(minion, new[] { Vector3.zero, Vector3.forward }, 1f, BalanceRules.Default.EscortBountyShare, 0f, new TargetRegistry(), new StructureRegistry());
            Assert.AreEqual(0, enemy.Reward);
        }

        [Test]
        public void EveryLevelBoss_CarriesOneEscortPower_WithACreatureThatTravelsAsItDoes()
        {
            int bosses = 0;
            foreach (EnemyDefinition enemy in Enemies())
            {
                if (!EscortRule.Binds(enemy))
                {
                    continue;
                }

                bosses++;
                int powers = 0;
                foreach (BossAbilityDefinition ability in enemy.Abilities)
                {
                    powers += ability != null && ability.Kind == BossAbilityKind.Spawn ? 1 : 0;
                }

                Assert.AreEqual(1, powers, enemy.name + ": one Spawn power (the rule gives one brood a budget)");
                EnemyDefinition creature = EscortRule.CreatureOf(enemy);
                Assert.IsNotNull(creature, enemy.name);
                Assert.IsFalse(creature.IsBoss, enemy.name);
                Assert.AreEqual(enemy.Flies, creature.Flies, enemy.name + ": its brood takes the rest of its own route");
            }

            Assert.GreaterOrEqual(bosses, 10);
        }

        // The six continent bosses stage their own calls: creature, count and interval as round 87 stored them.
        [TestCase("Gravemaw", "Skitter", 5, 30f)]
        [TestCase("CinderMatriarch", "FireLarva", 4, 20f)]
        [TestCase("FrostWarden", "Cragback", 2, 25f)]
        [TestCase("Rotbloom", "Oozer", 2, 60f)]
        [TestCase("Shattered", "ShardCrawler", 3, 22f)]
        [TestCase("SealedOne", "Bulwark", 2, 30f)]
        public void AContinentBoss_KeepsItsOwnCall(string id, string creature, int count, float interval)
        {
            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/{id}.asset");
            Assert.IsNotNull(boss, id);
            Assert.IsNotNull(boss.Boss, id + " is a continent boss");
            Assert.IsFalse(EscortRule.Binds(boss), id + ": the escort rule never binds a continent boss");

            BossAbilityDefinition call = null;
            foreach (BossAbilityDefinition ability in boss.Abilities)
            {
                if (ability != null && ability.Kind == BossAbilityKind.Spawn)
                {
                    Assert.IsNull(call, id + ": one call");
                    call = ability;
                }
            }

            Assert.IsNotNull(call, id);
            Assert.AreEqual(id + "Call", call.name);
            Assert.AreEqual(creature, call.Minion.name);
            Assert.AreEqual(count, call.MinionCount);
            Assert.AreEqual(interval, call.Interval, 0.0001f);
            Assert.AreEqual(interval * 0.5f, call.FirstDelay, 0.0001f);

            EnemyStats stats = BalanceLedger.Stats(boss);
            Assert.IsFalse(stats.RuledEscort);
            Assert.AreEqual(count / interval, stats.SummonsPerSecond, 0.0001f);
        }

        [Test]
        public void TheContinentBossEscortShare_IsUntouched()
        {
            float[] shares = { 1f, 1f, 1f, 0.5f };
            for (int d = 0; d < DifficultyIds.Length; d++)
            {
                Assert.AreEqual(shares[d], Difficulty(DifficultyIds[d]).BossEscortShare, 0.0001f, DifficultyIds[d]);
            }
        }

        [Test]
        public void AnUnstagedBoss_ReleasesItsPlainCount_TimesTheBossEscortShare()
        {
            EnemyDefinition minion = Definition(10f, 1f, false, null);
            BossAbilityDefinition call = Spawn(minion, 4, 10f, 10f);
            EnemyDefinition boss = Definition(1000f, 1f, true, new[] { call });
            var go = new GameObject("Boss");
            _made.Add(go);
            var enemy = go.AddComponent<Enemy>();
            var powers = go.AddComponent<BossAbilities>();
            int plain = 0;
            int ruled = 0;
            powers.Initialize(new StructureRegistry(), (d, b) => plain++, (origin, range) => null);
            powers.SetEscortShare(0.5f);
            powers.SetEscortRule(Plain, (definition, from, health, bounty) => ruled++);
            enemy.Initialize(boss, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
            powers.Step(10f);
            powers.Step(10f);
            Assert.AreEqual(4, plain, "two calls of half of four, as before round 88b");
            Assert.AreEqual(0, ruled);
        }

        [Test]
        public void TheLedger_CountsALevelBossesBrood_AndLeavesAContinentBossAlone()
        {
            BalanceRules rules = BalanceRules.Default;
            var level = new LevelInput();
            level.Shape.Lanes.Add(new LevelShape.Lane { PathIndex = 0, Length = 100f, TrunkShare = 1f });

            WaveLoad none = WaveLoad.Of(level, Wave(LedgerBoss(true, false)), new DifficultyStats { EscortWorth = 0f }, rules);
            WaveLoad brood = WaveLoad.Of(level, Wave(LedgerBoss(true, false)), new DifficultyStats { EscortWorth = 0.5f }, rules);
            Assert.AreEqual(0, none.GroundCount);
            Assert.AreEqual(33, brood.GroundCount, "half of a bar of 2000 in creatures of 30");
            Assert.AreEqual(33 * 30f, brood.StreamHealth, 0.01f);
            Assert.AreEqual(none.BossHealth, brood.BossHealth, 0.01f, "the brood never counts as the boss");
            WaveGroup escorts = brood.Groups.Find(g => !g.IsBoss);
            Assert.AreEqual(rules.EscortStartShare, escorts.StartShare, 0.001f);

            WaveLoad continent = WaveLoad.Of(level, Wave(LedgerBoss(false, true)), new DifficultyStats { EscortWorth = 0f }, rules);
            WaveLoad continentWithWorth = WaveLoad.Of(level, Wave(LedgerBoss(false, true)), new DifficultyStats { EscortWorth = 0.9f }, rules);
            Assert.Greater(continent.GroundCount, 0);
            Assert.AreEqual(continent.GroundCount, continentWithWorth.GroundCount, "the escort worth moves no continent boss");
            Assert.AreEqual(continent.StreamHealth, continentWithWorth.StreamHealth, 0.001f);
            Assert.AreEqual(WaveLoad.SummonStartShare, continent.Groups.Find(g => !g.IsBoss).StartShare, 0.001f);
        }

        [Test]
        public void TheLedgersStats_TakeTheBroodCreature_OfEveryLevelBoss()
        {
            foreach (EnemyDefinition enemy in Enemies())
            {
                if (!EscortRule.Binds(enemy))
                {
                    continue;
                }

                EnemyStats stats = BalanceLedger.Stats(enemy);
                EnemyDefinition creature = EscortRule.CreatureOf(enemy);
                Assert.IsTrue(stats.RuledEscort, enemy.name);
                Assert.AreEqual(0f, stats.SummonsPerSecond, enemy.name + ": the worth decides how many, not a rate");
                Assert.AreEqual(creature.MaxHealth + creature.Armor, stats.SummonHealth + stats.SummonArmor, 0.001f, enemy.name);
            }
        }

        private static WaveSpec Wave(EnemyStats boss)
        {
            var wave = new WaveSpec();
            wave.Groups.Add(new GroupSpec { Enemy = boss, Count = 1 });
            return wave;
        }

        private static EnemyStats LedgerBoss(bool ruled, bool continent) => new EnemyStats
        {
            Id = "Titan",
            Health = 2000f,
            Speed = 0.5f,
            IsBoss = true,
            IsContinentBoss = continent,
            RuledEscort = ruled,
            BlockadeDamage = 100f,
            AttackInterval = 1f,
            SummonsPerSecond = continent ? 0.1f : 0f,
            SummonHealth = 30f,
            SummonSpeed = 1.5f,
            SummonBlockadeDamage = 4f,
            SummonAttackInterval = 1f,
        };

        private static int Sum(List<int> values)
        {
            int sum = 0;
            foreach (int value in values)
            {
                sum += value;
            }

            return sum;
        }

        private static DifficultyProfile Difficulty(string id)
        {
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DifficultyFolder}/{id}.asset");
            Assert.IsNotNull(profile, id);
            return profile;
        }

        private static IEnumerable<EnemyDefinition> Enemies()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy != null)
                {
                    yield return enemy;
                }
            }
        }

        private EnemyDefinition Definition(float health, float speed, bool boss, BossAbilityDefinition[] abilities)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _made.Add(definition);
            var so = new SerializedObject(definition);
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_armor").floatValue = 0f;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_isBoss").boolValue = boss;
            SerializedProperty list = so.FindProperty("_abilities");
            list.arraySize = abilities != null ? abilities.Length : 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = abilities[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private BossAbilityDefinition Spawn(EnemyDefinition minion, int count, float interval, float firstDelay)
        {
            var ability = ScriptableObject.CreateInstance<BossAbilityDefinition>();
            _made.Add(ability);
            var so = new SerializedObject(ability);
            so.FindProperty("_kind").enumValueIndex = (int)BossAbilityKind.Spawn;
            so.FindProperty("_interval").floatValue = interval;
            so.FindProperty("_firstDelay").floatValue = firstDelay;
            so.FindProperty("_minion").objectReferenceValue = minion;
            so.FindProperty("_minionCount").intValue = count;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ability;
        }
    }
}
