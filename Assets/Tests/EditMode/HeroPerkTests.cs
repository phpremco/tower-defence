using System.Collections.Generic;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.Platform;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Hero perks (STORY.md, hero upgrades): every continent grants its pistol, blade and ability perks in that order, each
    /// once, and a first clear unlocks the perk in the save.
    /// </summary>
    public sealed class HeroPerkTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string PerkFolder = "Assets/_Project/Data/Hero/Perks";

        private sealed class MemorySave : ISaveService
        {
            public void Save<T>(string key, T data)
            {
            }

            public bool TryLoad<T>(string key, out T data)
            {
                data = default;
                return false;
            }
        }

        [Test]
        public void ContinentOnesPerks_ExistWithTheirSlots()
        {
            var expected = new Dictionary<string, HeroPerkSlot>
            {
                ["Perk_QuickReload"] = HeroPerkSlot.Pistol,
                ["Perk_WideSlash"] = HeroPerkSlot.Blade,
                ["Perk_FieldRepair"] = HeroPerkSlot.Ability,
            };
            foreach (KeyValuePair<string, HeroPerkSlot> pair in expected)
            {
                var perk = AssetDatabase.LoadAssetAtPath<HeroPerkDefinition>($"{PerkFolder}/{pair.Key}.asset");
                Assert.IsNotNull(perk, $"{pair.Key}: run Tools/Generate/Scenes and Starter Data");
                Assert.AreEqual(pair.Value, perk.Slot);
                Assert.IsFalse(string.IsNullOrWhiteSpace(perk.DisplayName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(perk.Description));
            }
        }

        [Test]
        public void EveryContinent_GrantsPistolThenBladeThenAbility()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            for (int r = 0; r < catalog.Regions.Count; r++)
            {
                var slots = new List<HeroPerkSlot>();
                foreach (MapNode node in catalog.Nodes)
                {
                    if (node.Region == r && node.Level != null && node.Level.Reward.HeroPerk != null)
                    {
                        slots.Add(node.Level.Reward.HeroPerk.Slot);
                    }
                }

                for (int i = 0; i < slots.Count; i++)
                {
                    Assert.AreEqual((HeroPerkSlot)i, slots[i], $"{catalog.Regions[r].DisplayName}: perk {i + 1}");
                }
            }
        }

        [Test]
        public void FirstClear_UnlocksThePerkOnce()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            LevelDefinition level = null;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null && node.Level.Reward.HeroPerk != null)
                {
                    level = node.Level;
                    break;
                }
            }

            Assert.IsNotNull(level, "some level grants a perk");
            var progress = new ProgressService(new MemorySave(), new string[0]);
            DifficultyProfile easy = catalog.Difficulties[0];

            LevelResult first = RewardCalculator.Compute(level, easy, progress, true, 1f);
            RewardCalculator.Apply(first, level, easy, progress);
            LevelResult again = RewardCalculator.Compute(level, catalog.Difficulties[1], progress, true, 1f);

            Assert.AreSame(level.Reward.HeroPerk, first.HeroPerk);
            Assert.IsTrue(progress.IsHeroPerkUnlocked(level.Reward.HeroPerk.Id));
            Assert.IsNull(again.HeroPerk, "a perk comes with the first clear only");
        }
        [Test]
        public void NoPerk_GivesTheBaseValues()
        {
            HeroPerkModifiers none = HeroPerkModifiers.From(new HeroPerkDefinition[0]);
            Assert.AreEqual(1f, none.RangedIntervalMultiplier, 0.0001f);
            Assert.AreEqual(1f, none.ReloadMultiplier, 0.0001f);
            Assert.AreEqual(1f, HeroPerkModifiers.From(null).ReloadMultiplier, 0.0001f);
        }

        // Curator decision 2: without the perk the magazine and reload keep the old damage per second (0.35 s a shot);
        // Quick Reload adds 25 to 30 percent.
        [Test]
        public void QuickReload_KeepsTheBaseDpsAndAddsAQuarter()
        {
            var perk = AssetDatabase.LoadAssetAtPath<HeroPerkDefinition>($"{PerkFolder}/Perk_QuickReload.asset");
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>("Assets/_Project/Data/Hero/Commander.asset");
            Assert.IsNotNull(perk, "run Tools/Generate/Scenes and Starter Data");
            HeroWeaponDefinition gun = hero.RangedWeapon;
            Assert.Greater(gun.Magazine, 0, "the pistol has a magazine to reload");

            float plain = HeroPerkModifiers.EffectiveInterval(gun.AttackInterval, gun.Magazine, gun.ReloadSeconds);
            Assert.AreEqual(0.35f, plain, 0.001f, "the old pace, reload included");

            HeroPerkModifiers quick = HeroPerkModifiers.From(new[] { perk });
            Assert.Less(quick.ReloadMultiplier, 1f);
            float fast = HeroPerkModifiers.EffectiveInterval(gun.AttackInterval * quick.RangedIntervalMultiplier, gun.Magazine, gun.ReloadSeconds * quick.ReloadMultiplier);
            float gain = plain / fast;
            Assert.That(gain, Is.InRange(1.25f, 1.30f), $"Quick Reload gives x{gain:0.00} damage per second");
            CollectionAssert.Contains(hero.Perks, perk, "the hero can earn Quick Reload");
        }
    }
}
