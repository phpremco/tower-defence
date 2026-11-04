using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Editor;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Crystal Plains levels 1 to 6 as the S4 spec and its curator decisions describe them: four towers, no armour, no
    /// flyers before level 3, newcomer wave counts, the new display names and the credits on the result screen.
    /// </summary>
    public sealed class Continent1ContentTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string DataRoot = "Assets/_Project/Data";
        private const int SliceLevels = 6;
        private const int FirstAirLevel = 2;
        private const float FastTeaserShare = 2.5f;

        private static readonly string[] SliceTowers = { "PulseRifle", "Cannon", "Laser", "Rocket" };

        private static List<LevelDefinition> Slice()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var levels = new List<LevelDefinition>();
            for (int i = 0; i < catalog.Nodes.Count && levels.Count < SliceLevels; i++)
            {
                if (catalog.Nodes[i].Level != null)
                {
                    levels.Add(catalog.Nodes[i].Level);
                }
            }

            Assert.AreEqual(SliceLevels, levels.Count);
            return levels;
        }

        private static IEnumerable<SpawnGroup> Groups(LevelDefinition level)
        {
            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                foreach (WaveDefinition wave in set.Waves)
                {
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        yield return group;
                    }
                }
            }
        }

        [Test]
        public void OnlyTheFourTowers_AreEverOwnedInTheSlice()
        {
            var owned = new HashSet<string>(AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath).StartingTowerIds);
            foreach (LevelDefinition level in Slice())
            {
                if (level.Reward.NewTower != null)
                {
                    owned.Add(level.Reward.NewTower.Id);
                }
            }

            CollectionAssert.AreEquivalent(SliceTowers, owned);
        }

        [Test]
        public void NoCreature_InTheSlice_WearsArmour()
        {
            foreach (LevelDefinition level in Slice())
            {
                foreach (SpawnGroup group in Groups(level))
                {
                    Assert.AreEqual(0f, group.Enemy.Armor, $"{level.name}: {group.Enemy.name} has armour, which waits for level 11");
                }
            }
        }

        [Test]
        public void NoFlyers_BeforeLevelThree_OnAnyDifficulty()
        {
            List<LevelDefinition> slice = Slice();
            for (int i = 0; i < FirstAirLevel; i++)
            {
                foreach (SpawnGroup group in Groups(slice[i]))
                {
                    Assert.IsFalse(group.Enemy.Flies, $"{slice[i].name}: {group.Enemy.name} flies before the Flak exists");
                }
            }
        }

        [Test]
        public void WaveCounts_SuitNewcomers()
        {
            List<LevelDefinition> slice = Slice();
            for (int i = 0; i < slice.Count; i++)
            {
                foreach (DifficultyWaves set in slice[i].DifficultyWaves)
                {
                    int count = set.Waves.Length;
                    if (i < FirstAirLevel)
                    {
                        Assert.That(count, Is.InRange(10, 12), $"{slice[i].name} {set.Difficulty.name}");
                    }
                    else
                    {
                        Assert.That(count, Is.InRange(14, 18), $"{slice[i].name} {set.Difficulty.name}");
                    }
                }
            }
        }

        [Test]
        public void TheFirstBoss_IsTheBroodMother()
        {
            CampaignLevelSpec first = LevelSpecs.Crystal[0];
            foreach (Game.Balance.BossSpec boss in first.Waves.Bosses)
            {
                Assert.AreEqual("BroodMother", boss.Enemy);
            }
        }

        [Test]
        public void EveryUnlock_CarriesItsCredit()
        {
            foreach (LevelDefinition level in Slice())
            {
                LevelReward reward = level.Reward;
                bool unlocks = reward.NewTower != null || reward.HeroPerk != null || (reward.NewPathObjects != null && reward.NewPathObjects.Length > 0);
                if (unlocks)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(reward.Credit), $"{level.name} unlocks something without saying who made it");
                }
            }
        }

        [Test]
        public void DisplayNames_FollowTheScript()
        {
            Assert.AreEqual("Gun Turret", Load<TowerDefinition>("Towers/PulseRifle").DisplayName);
            Assert.AreEqual("Laser Turret", Load<TowerDefinition>("Towers/Laser").DisplayName);
            Assert.AreEqual("Flak Turret", Load<TowerDefinition>("Towers/Rocket").DisplayName);
            Assert.AreEqual("Land Mine", Load<PathObjectDefinition>("PathObjects/Mine").DisplayName);
            Assert.AreEqual("Chrono Field", Load<PathObjectDefinition>("PathObjects/SlowField").DisplayName);
            Assert.AreEqual("Mercer", Load<HeroDefinition>("Hero/Commander").DisplayName);
        }

        [Test]
        public void Flak_IsAnAirOnlyFusedSplashGun()
        {
            TowerDefinition flak = Load<TowerDefinition>("Towers/Rocket");
            Assert.AreEqual(TargetLayers.Air, flak.Targets);
            Assert.Greater(flak.FuseRadius, 0f);
            Assert.Greater(flak.SplashRadius, 0f);
            Assert.AreEqual(ProjectileStyle.Flak, flak.ProjectileStyle);
            Assert.AreEqual(TowerShape.Flak, flak.Shape);
        }

        [Test]
        public void ChronoField_IsTunedForWideRoads()
        {
            PathObjectDefinition chrono = Load<PathObjectDefinition>("PathObjects/SlowField");
            Assert.AreEqual(0.45f, chrono.SlowFactor, 0.001f);
            Assert.AreEqual(1.6f, chrono.Radius, 0.001f);
            Assert.AreEqual(20f, chrono.Lifetime, 0.001f);
            Assert.AreEqual(45, chrono.Cost);
            Assert.AreEqual(TargetLayers.Ground, chrono.Targets);
            Assert.AreEqual(PathObjectLook.Chrono, chrono.Look);
        }

        // Curator decision 3: the teaser must read as "three times the average", at least 2.5 times the ant swarm.
        [Test]
        public void Roller_IsMuchFasterThanTheSwarm_OverAWholeCycle()
        {
            EnemyDefinition roller = Load<EnemyDefinition>("Enemies/Roller");
            EnemyDefinition skitter = Load<EnemyDefinition>("Enemies/Skitter");
            Assert.GreaterOrEqual(roller.AverageSpeed, skitter.Speed * FastTeaserShare, $"Roller averages {roller.AverageSpeed:0.00} against the Skitter's {skitter.Speed:0.00}");
            Assert.AreEqual(0f, roller.Armor);
            StringAssert.DoesNotContain("rmour", roller.Description);
        }

        [Test]
        public void StoneCreatures_AreHeavyAndUnarmoured()
        {
            EnemyDefinition crag = Load<EnemyDefinition>("Enemies/Cragback");
            EnemyDefinition bat = Load<EnemyDefinition>("Enemies/Shalebat");
            Assert.AreEqual(Element.Stone, crag.Element);
            Assert.AreEqual(Element.Stone, bat.Element);
            Assert.AreEqual(0f, crag.Armor);
            Assert.AreEqual(160f, crag.MaxHealth, 0.001f);
            Assert.AreEqual(0.6f, crag.Speed, 0.001f);
            Assert.IsTrue(bat.Flies);
            Assert.IsNotNull(crag.Voice, "the Cragback borrows a voice until Remco records its own");
            Assert.IsNotNull(bat.Voice);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DataRoot}/{path}.asset");
            Assert.IsNotNull(asset, $"{path}: run Tools/Generate/Scenes and Starter Data");
            return asset;
        }
    }
}
