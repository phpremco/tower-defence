using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.Platform;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 66 (Remco after playtest 2): ten upgrade tiers opened per continent, the research economy over five
    /// continents, Spencer's gear (the companion repair drone, the armour, the blade at its top tier) and an old save
    /// in the new model.
    /// </summary>
    public sealed class Round66Tests
    {
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string HeroPath = "Assets/_Project/Data/Hero/Commander.asset";

        /// <summary>A copy of a real save put here for the migration check (never the save itself); absent, that test is skipped.</summary>
        private const string SaveCopyPath = "Tools/PlayTest/r66_save_copy.json";
        private const string EconomyReportPath = "Tools/PlayTest/r66_upgrade_economy.txt";

        // What the roadmap plans per later continent, as the first-tier research price of its own new upgrades: two new
        // towers (damage 5 + range 5 + fire rate 6 each). An assumption for the projection, not built content.
        private static readonly int[] PlannedFirstTierCost = { 0, 32, 32, 32, 32 };

        private static readonly BalanceRules Rules = BalanceRules.Default;

        private sealed class JsonSave : ISaveService
        {
            private readonly Dictionary<string, string> _store = new Dictionary<string, string>();

            public JsonSave(string json = null)
            {
                if (json != null)
                {
                    _store["progress"] = json;
                }
            }

            public void Save<T>(string key, T data) => _store[key] = JsonUtility.ToJson(data);

            public bool TryLoad<T>(string key, out T data)
            {
                if (_store.TryGetValue(key, out string json))
                {
                    data = JsonUtility.FromJson<T>(json);
                    return true;
                }

                data = default;
                return false;
            }
        }

        private sealed class FakeGuarded : IGuardable
        {
            public Vector3 Position { get; set; }

            public IDamageGuard Guard { get; set; }

            public bool CanRestore => false;

            public void Restore(float fraction)
            {
            }
        }

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private static UpgradeCatalog Catalog() => AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);

        private static UpgradeDefinition Find(UpgradeCatalog catalog, string id)
        {
            foreach (UpgradeDefinition upgrade in catalog.Upgrades)
            {
                if (upgrade.Id == id)
                {
                    return upgrade;
                }
            }

            Assert.Fail("No upgrade " + id);
            return null;
        }

        private static ProgressService ProgressWith(params (string Id, int Level)[] bought)
        {
            var progress = new ProgressService(new JsonSave(), new string[0]);
            foreach ((string Id, int Level) entry in bought)
            {
                progress.Data.Upgrades.Add(new UpgradeLevel { Id = entry.Id, Level = entry.Level });
            }

            return progress;
        }

        // ---- tier gating ---------------------------------------------------------------------------------------------

        [Test]
        public void TierCaps_ReadTheListPerContinent()
        {
            int[] caps = { 4, 6, 8, 9, 10 };
            Assert.AreEqual(4, UpgradeTiers.Cap(caps, 0));
            Assert.AreEqual(6, UpgradeTiers.Cap(caps, 1));
            Assert.AreEqual(10, UpgradeTiers.Cap(caps, 4));
            Assert.AreEqual(10, UpgradeTiers.Cap(caps, 7), "a continent past the list keeps the last cap");
            Assert.AreEqual(4, UpgradeTiers.Cap(caps, -1));
            Assert.AreEqual(UpgradeTiers.Unlimited, UpgradeTiers.Cap(new int[0], 0), "no list caps nothing");
            Assert.AreEqual(0, UpgradeTiers.ContinentOpening(caps, 4));
            Assert.AreEqual(1, UpgradeTiers.ContinentOpening(caps, 5));
            Assert.AreEqual(4, UpgradeTiers.ContinentOpening(caps, 10));
            Assert.AreEqual(-1, UpgradeTiers.ContinentOpening(caps, 11));
        }

        [Test]
        public void Catalog_OpensFourTiersOnTheFirstContinent_AndTenOnTheFifth()
        {
            UpgradeCatalog catalog = Catalog();
            CollectionAssert.AreEqual(Rules.UpgradeTiersPerContinent, catalog.TiersPerContinent, "the catalogue's caps are the rules' caps (the model shops under the same)");
            Assert.AreEqual(4, catalog.TierCap(0));
            Assert.AreEqual(10, catalog.TierCap(4));
            for (int c = 1; c < catalog.TiersPerContinent.Count; c++)
            {
                int added = catalog.TiersPerContinent[c] - catalog.TiersPerContinent[c - 1];
                Assert.That(added, Is.InRange(1, 2), $"continent {c + 1} opens one or two tiers more");
            }
        }

        [Test]
        public void Catalog_EveryPercentUpgradeHasTenTiers_PricedUpwards()
        {
            foreach (UpgradeDefinition upgrade in Catalog().Upgrades)
            {
                // The base turrets and the extra harvest drone are built out in steps of their own.
                bool stepped = upgrade.Target == UpgradeTarget.BaseWeapon || upgrade.Target == UpgradeTarget.DroneCount;
                Assert.AreEqual(stepped ? upgrade.MaxLevel : 10, upgrade.MaxLevel, upgrade.Id);
                for (int tier = 1; tier < upgrade.MaxLevel; tier++)
                {
                    Assert.Greater(upgrade.CostForNextLevel(tier), upgrade.CostForNextLevel(tier - 1), $"{upgrade.Id} tier {tier + 1} costs more than tier {tier}");
                }
            }
        }

        [Test]
        public void Buying_StopsAtTheTierCap_AndGoesOnWhenTheNextContinentOpens()
        {
            UpgradeCatalog catalog = Catalog();
            UpgradeDefinition damage = Find(catalog, "PulseRifle_Damage");
            var progress = new ProgressService(new JsonSave(), new string[0]);
            progress.AddRewards(1000, 0);
            int cap = catalog.TierCap(0);
            for (int tier = 0; tier < cap; tier++)
            {
                Assert.IsTrue(progress.TryBuyUpgrade(damage, cap), $"tier {tier + 1} is open on the first continent");
            }

            int held = progress.Data.ResearchPoints;
            Assert.IsFalse(progress.TryBuyUpgrade(damage, cap), "tier 5 waits for the second continent");
            Assert.AreEqual(held, progress.Data.ResearchPoints, "a refused purchase costs nothing");
            Assert.AreEqual(cap, progress.GetUpgradeLevel(damage.Id));
            Assert.IsTrue(progress.TryBuyUpgrade(damage, catalog.TierCap(1)), "the second continent opens tier 5");
        }

        [Test]
        public void FreshSave_HasTheFirstContinentsCap_FromTheCampaign()
        {
            var campaign = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            UpgradeCatalog catalog = Catalog();
            var progress = new ProgressService(new JsonSave(), new string[0]);
            Assert.AreEqual(0, CampaignRules.ReachedRegion(campaign, progress));
            Assert.AreEqual(catalog.TierCap(0), CampaignRules.UpgradeTierCap(campaign, catalog, progress));
        }

        [Test]
        public void Model_ShopsUnderTheContinentsCap_AndLeavesTheHerosGearOnTheShelf()
        {
            var upgrades = new List<UpgradeStats>
            {
                new UpgradeStats { Id = "Rifle_Damage", Kind = UpgradeKind.TowerDamage, TowerId = "Rifle", BonusPerLevel = 0.1f, Costs = UpgradeEconomy.TierCosts(5f, 10, Rules), Continent = 0 },
                new UpgradeStats { Id = "Hero_Armour", Kind = UpgradeKind.HeroGear, BonusPerLevel = 0.05f, Costs = UpgradeEconomy.TierCosts(5f, 10, Rules), Continent = 0 },
            };
            var campaign = new List<CampaignLevel>
            {
                new CampaignLevel { Key = "A", Continent = 0, Research = 5000 },
                new CampaignLevel { Key = "B", Continent = 0, Research = 5000 },
                new CampaignLevel { Key = "C", Continent = 1, Research = 5000 },
            };

            PlayerState first = Progression.StateAt(campaign, 1, 1, new[] { "Rifle" }, upgrades, Rules);
            Assert.AreEqual(Rules.UpgradeTierCap(0), first.UpgradeLevels["Rifle_Damage"], "a rich player on the first continent stops at its cap");
            Assert.IsFalse(first.UpgradeLevels.ContainsKey("Hero_Armour"), "the model's shopper does not buy the hero's gear");

            PlayerState second = Progression.StateAt(campaign, 2, 1, new[] { "Rifle" }, upgrades, Rules);
            Assert.AreEqual(Rules.UpgradeTierCap(1), second.UpgradeLevels["Rifle_Damage"], "the second continent opens its tiers to the model too");
        }

        // ---- cost curve and economy ----------------------------------------------------------------------------------

        [Test]
        public void TierCosts_KeepTheOldThreeTiers_AndClimbToTwentyTimesTheFirst()
        {
            CollectionAssert.AreEqual(new[] { 5, 10, 15, 22, 30, 40, 52, 66, 82, 100 }, UpgradeEconomy.TierCosts(5f, 10, Rules));
            UpgradeCatalog catalog = Catalog();
            AssertFirstThree(catalog, "PulseRifle_Damage", 5, 10, 15);
            AssertFirstThree(catalog, "PulseRifle_FireRate", 6, 12, 18);
            AssertFirstThree(catalog, "BaseHealth", 4, 8, 12);
            AssertFirstThree(catalog, "BlockadeHealth", 4, 8, 12);
            AssertFirstThree(catalog, "Hero_Damage", 5, 10, 15);
            AssertFirstThree(catalog, "Hero_Health", 5, 10, 15);
            AssertFirstThree(catalog, "RepairDrone_Speed", 3, 6, 9);
            AssertFirstThree(catalog, "Base_Weapon", 12, 20, 32);
            Assert.AreEqual(0.1f, Find(catalog, "PulseRifle_Damage").BonusPerLevel, 1e-5f, "a tier gives what it gave");
            Assert.AreEqual(0.2f, Find(catalog, "Hero_Health").BonusPerLevel, 1e-5f);
        }

        private static void AssertFirstThree(UpgradeCatalog catalog, string id, int first, int second, int third)
        {
            UpgradeDefinition upgrade = Find(catalog, id);
            Assert.AreEqual(first, upgrade.CostForNextLevel(0), id);
            Assert.AreEqual(second, upgrade.CostForNextLevel(1), id);
            Assert.AreEqual(third, upgrade.CostForNextLevel(2), id);
        }

        [Test]
        public void CrystalPlains_BudgetIsItsShareOfWhatItOpens_AndEveryDifficultyTogetherPaysItAll()
        {
            List<UpgradeStats> upgrades = Stats();
            int opened = UpgradeEconomy.Opened(upgrades, 0, power: false, Rules);
            int budget = FinalePlan.CrystalPlainsResearchBudget;
            Assert.AreEqual(Rules.ResearchBudgetShare, budget / (float)opened, 0.02f, $"748 is the budget share of the {opened} research the first four tiers cost");

            int all = 0;
            for (int d = 0; d < Rules.ResearchMultipliers.Length; d++)
            {
                all += UpgradeEconomy.Earned(budget, d, Rules);
            }

            Assert.Less(UpgradeEconomy.Earned(budget, 3, Rules), opened, "Psycho alone does not buy everything");
            Assert.GreaterOrEqual(all, opened, "every level on every difficulty pays for all four tiers");

            // The first three tiers of what the model shops for are the old budget: nothing moved under the calibration.
            int core = 0;
            foreach (UpgradeStats upgrade in upgrades)
            {
                if (!upgrade.PaidInPower && upgrade.Continent == 0 && upgrade.Kind != UpgradeKind.HeroGear)
                {
                    core += UpgradeEconomy.Cost(upgrade, 0, 3);
                }
            }

            Assert.AreEqual(budget, core, "tiers one to three of the towers, the base, the gates and the commander still cost the budget");
        }

        [Test]
        public void Projection_ReachesTierTenOnTheFifthContinent_AndWritesTheReport()
        {
            var budgets = new[] { FinalePlan.CrystalPlainsResearchBudget };
            List<UpgradeEconomy.Line> lines = UpgradeEconomy.Project(Stats(), budgets, PlannedFirstTierCost, Rules);
            Assert.AreEqual(5, lines.Count);
            Assert.AreEqual(10, lines[4].TierCap);

            var text = new StringBuilder();
            text.Append("Round 66: research over five continents (every level of a continent once per difficulty; later continents assume two new towers each)\n");
            text.Append("continent | cap | opens | budget | Easy | Medium | Hard | Psycho | all four | opened so far | earned so far (all four) | even tier: Easy / Medium / Medium+Hard / all four\n");
            for (int c = 0; c < lines.Count; c++)
            {
                UpgradeEconomy.Line line = lines[c];
                Assert.Greater(line.Opened, 0);
                Assert.GreaterOrEqual(line.EarnedSoFarOnEveryDifficulty, line.OpenedSoFar, $"continent {c + 1}: all four difficulties keep up with everything opened");
                Assert.AreEqual(line.TierCap, line.EvenTier[3], $"continent {c + 1}: the player of every difficulty holds every open tier");
                Assert.Less(line.EvenTier[0], line.TierCap, $"continent {c + 1}: Easy alone does not");
                if (c > 0)
                {
                    Assert.Greater(line.Budget, lines[0].Budget, "every later continent pays more than the first");
                }

                text.Append($"C{c + 1} | {line.TierCap} | {line.Opened} | {line.Budget} | {line.Earned[0]} | {line.Earned[1]} | {line.Earned[2]} | {line.Earned[3]} | {line.EarnedOnEveryDifficulty} | {line.OpenedSoFar} | {line.EarnedSoFarOnEveryDifficulty} | {line.EvenTier[0]} / {line.EvenTier[1]} / {line.EvenTier[2]} / {line.EvenTier[3]}\n");
            }

            File.WriteAllText(EconomyReportPath, text.ToString());
        }

        private static List<UpgradeStats> Stats()
        {
            var stats = new List<UpgradeStats>();
            foreach (UpgradeDefinition upgrade in Catalog().Upgrades)
            {
                stats.Add(BalanceLedger.Stats(upgrade));
            }

            return stats;
        }

        // ---- an old save in the new model ----------------------------------------------------------------------------

        // A version-2 save shaped like Remco's after playtest 2: three bought upgrades in the three-tier model.
        private const string OldSave =
            "{\"Version\":2,\"ResearchPoints\":9,\"PowerCurrency\":7,\"IntroSeen\":true,\"UnlockedTowerIds\":[\"PulseRifle\",\"Cannon\",\"Laser\",\"Rocket\"]," +
            "\"CompletedLevels\":[\"Level_C1_01/Easy\",\"Level_C1_02/Medium\",\"Level_C1_03/Easy\",\"Level_C1_03/Hard\",\"Level_C1_04/Easy\"]," +
            "\"Upgrades\":[{\"Id\":\"Base_Weapon\",\"Level\":1},{\"Id\":\"BlockadeHealth\",\"Level\":2},{\"Id\":\"PulseRifle_Damage\",\"Level\":2}]," +
            "\"UnlockedHeroPerkIds\":[\"Perk_QuickReload\"]}";

        [Test]
        public void OldSave_LoadsIntoTheTenTierModel_WithoutLoss()
        {
            AssertSaveCarriesOver(OldSave);
        }

        [Test]
        public void SaveCopy_LoadsIntoTheTenTierModel_WithoutLoss()
        {
            if (!File.Exists(SaveCopyPath))
            {
                Assert.Ignore("No save copy at " + SaveCopyPath + " (put a COPY of a real save there to check it).");
            }

            AssertSaveCarriesOver(File.ReadAllText(SaveCopyPath));
        }

        // The three old tiers are the first three of the ten, at the same price and worth, so a save's levels carry over as
        // they are: no format change, no migration step. This asserts exactly that, upgrade by upgrade.
        private static void AssertSaveCarriesOver(string json)
        {
            ProgressData before = JsonUtility.FromJson<ProgressData>(json);
            var progress = new ProgressService(new JsonSave(json), new string[0]);
            UpgradeCatalog catalog = Catalog();
            var campaign = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            int cap = CampaignRules.UpgradeTierCap(campaign, catalog, progress);

            Assert.AreEqual(ProgressMigrator.CurrentVersion, progress.Data.Version);
            Assert.AreEqual(before.ResearchPoints, progress.Data.ResearchPoints, "research kept");
            Assert.AreEqual(before.PowerCurrency, progress.Data.PowerCurrency, "power kept");
            CollectionAssert.AreEqual(before.CompletedLevels, progress.Data.CompletedLevels);
            CollectionAssert.AreEqual(before.UnlockedHeroPerkIds, progress.Data.UnlockedHeroPerkIds);
            Assert.AreEqual(before.Upgrades.Count, progress.Data.Upgrades.Count);
            foreach (UpgradeLevel bought in before.Upgrades)
            {
                Assert.AreEqual(bought.Level, progress.GetUpgradeLevel(bought.Id), bought.Id + " keeps its level");
                UpgradeDefinition upgrade = Find(catalog, bought.Id);
                Assert.LessOrEqual(bought.Level, UpgradeTiers.Open(upgrade, cap), bought.Id + " holds no tier that is locked now");
                Assert.LessOrEqual(bought.Level, 3, "a three-tier save");
            }

            // What the bought levels do is what they did: level times the unchanged bonus per level.
            PlayerModifiers global = UpgradeEffects.ComputeGlobal(progress, catalog, cap);
            Assert.AreEqual(1f + 0.2f * progress.GetUpgradeLevel("BlockadeHealth"), global.BlockadeHealthMultiplier, 1e-4f);
            Assert.AreEqual(progress.GetUpgradeLevel("Base_Weapon"), global.BaseWeaponLevel);
            Assert.AreEqual(1f + 0.1f * progress.GetUpgradeLevel("PulseRifle_Damage"), UpgradeEffects.ComputeForTower(progress, catalog, "PulseRifle").TowerDamageMultiplier, 1e-4f);
            Assert.AreEqual(0, global.HeroGear.ArmourTier, "the old 'Armour' (his health) did not become the new armour");
            Assert.AreEqual(1f + 0.2f * progress.GetUpgradeLevel("Hero_Health"), global.HeroHealthMultiplier, 1e-4f);

            // A reload of what the service saves is the same again.
            var store = new JsonSave(json);
            var first = new ProgressService(store, new string[0]);
            first.Save();
            var second = new ProgressService(store, new string[0]);
            foreach (UpgradeLevel bought in before.Upgrades)
            {
                Assert.AreEqual(bought.Level, second.GetUpgradeLevel(bought.Id));
            }
        }

        // ---- Spencer's gear -------------------------------------------------------------------------------------------

        [Test]
        public void Gear_ComesFromTheBoughtTiers()
        {
            UpgradeCatalog catalog = Catalog();
            UpgradeDefinition drone = Find(catalog, "Hero_RepairDrone");
            UpgradeDefinition armour = Find(catalog, "Hero_Armour");
            Assert.AreEqual(UpgradeTarget.HeroRegen, drone.Target);
            Assert.AreEqual(UpgradeTarget.HeroArmour, armour.Target);
            Assert.AreEqual("Spencer", drone.GroupName);
            Assert.IsTrue(drone.IsForHero && armour.IsForHero);
            Assert.AreEqual(0, drone.Continent, "offered from the first continent");

            HeroGear none = UpgradeEffects.ComputeGlobal(ProgressWith(), catalog, 4).HeroGear;
            Assert.AreEqual(1f, none.RegenMultiplier);
            Assert.AreEqual(1f, none.DamageTakenMultiplier);
            Assert.AreEqual(0, none.RepairDroneTier);
            Assert.IsFalse(none.BladeAtTop);

            HeroGear gear = UpgradeEffects.ComputeGlobal(ProgressWith(("Hero_RepairDrone", 2), ("Hero_Armour", 3)), catalog, 4).HeroGear;
            Assert.AreEqual(1f + 2 * drone.BonusPerLevel, gear.RegenMultiplier, 1e-4f);
            Assert.AreEqual(1f - 3 * armour.BonusPerLevel, gear.DamageTakenMultiplier, 1e-4f);
            Assert.AreEqual(2, gear.RepairDroneTier);
            Assert.AreEqual(3, gear.ArmourTier);
            Assert.AreEqual(armour.MaxLevel, gear.ArmourTiers);
            Assert.Less(armour.BonusPerLevel * armour.MaxLevel, HeroGear.MaxDamageReduction, "all ten tiers still leave a hit to take");
        }

        [Test]
        public void Blade_IsAtItsTop_OnlyAtTheHighestOpenTier()
        {
            UpgradeCatalog catalog = Catalog();
            Assert.IsFalse(UpgradeEffects.ComputeGlobal(ProgressWith(("Hero_Damage", 3)), catalog, 4).HeroGear.BladeAtTop);
            Assert.IsTrue(UpgradeEffects.ComputeGlobal(ProgressWith(("Hero_Damage", 4)), catalog, 4).HeroGear.BladeAtTop, "tier 4 is the top on the first continent");
            Assert.IsFalse(UpgradeEffects.ComputeGlobal(ProgressWith(("Hero_Damage", 4)), catalog, 6).HeroGear.BladeAtTop, "the second continent opens more: the blade cools until it is topped again");
            Assert.IsTrue(UpgradeEffects.ComputeGlobal(ProgressWith(("Hero_Damage", 10)), catalog, UpgradeTiers.Unlimited).HeroGear.BladeAtTop);
            Assert.IsFalse(UpgradeEffects.ComputeGlobal(ProgressWith(), catalog, 0).HeroGear.BladeAtTop, "an unbought blade is never at its top");
        }

        [Test]
        public void HeroGear_ClampsItsNumbers()
        {
            Assert.AreEqual(1f - HeroGear.MaxDamageReduction, new HeroGear(0f, 0, 5f, 10, 10, false).DamageTakenMultiplier, 1e-5f);
            Assert.AreEqual(1f, new HeroGear(-1f, 0, -1f, 0, 10, false).RegenMultiplier);
            Assert.AreEqual(1f, default(HeroGear).DamageTakenMultiplier, "no gear changes nothing");
            Assert.AreEqual(0.4f, new HeroGear(0f, 0, 0f, 4, 10, false).ArmourGrowth, 1e-5f);
        }

        [Test]
        public void Armour_TakesItsShareOffEveryHit_AndTheGateStillGetsItsOwn()
        {
            Commander plain = Hero(default, out HeroDefinition definition);
            Commander armoured = Hero(new HeroGear(0f, 0, 0.2f, 4, 10, false), out _);

            plain.TakeHazardDamage(100f);
            armoured.TakeHazardDamage(100f);
            Assert.AreEqual(100f, plain.Health.Max - plain.Health.Current, 1e-3f);
            Assert.AreEqual(80f, armoured.Health.Max - armoured.Health.Current, 1e-3f, "a fifth of the hit is gone");

            float through = armoured.Absorb(50f);
            Assert.AreEqual(50f * (1f - definition.GuardShare), through, 1e-3f, "the gate takes what it took before");
            Assert.AreEqual(80f + 50f * definition.GuardShare * 0.8f, armoured.Health.Max - armoured.Health.Current, 1e-3f, "his share of it is lighter");
        }

        [Test]
        public void RepairDrone_SpeedsUpHisMending_NotItsStart()
        {
            Commander plain = Hero(default, out HeroDefinition definition);
            Commander helped = Hero(new HeroGear(0.5f, 2, 0f, 0, 10, false), out _);
            MethodInfo recover = typeof(Commander).GetMethod("Recover", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (Commander hero in new[] { plain, helped })
            {
                hero.TakeHazardDamage(200f);
                recover.Invoke(hero, new object[] { definition.RegenDelaySeconds * 0.5f });
                Assert.IsFalse(hero.IsMending, "nothing mends inside the pause after a hit");
                Assert.AreEqual(200f, hero.Health.Max - hero.Health.Current, 1e-3f);
                recover.Invoke(hero, new object[] { definition.RegenDelaySeconds });
                Assert.IsTrue(hero.IsMending);
            }

            float plainHealed = 200f - (plain.Health.Max - plain.Health.Current);
            float helpedHealed = 200f - (helped.Health.Max - helped.Health.Current);
            Assert.AreEqual(definition.RegenPerSecond * definition.RegenDelaySeconds, plainHealed, 1e-3f);
            Assert.AreEqual(plainHealed * 1.5f, helpedHealed, 1e-3f, "half as fast again with the drone");

            recover.Invoke(helped, new object[] { 1000f });
            recover.Invoke(helped, new object[] { 1f });
            Assert.IsFalse(helped.IsMending, "whole again: the drone's light goes out");
        }

        private Commander Hero(HeroGear gear, out HeroDefinition definition)
        {
            definition = ScriptableObject.CreateInstance<HeroDefinition>();
            var weapon = ScriptableObject.CreateInstance<HeroWeaponDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("_weapon").objectReferenceValue = weapon;
            so.FindProperty("_maxHealth").floatValue = 300f;
            so.FindProperty("_regenPerSecond").floatValue = 6f;
            so.FindProperty("_regenDelaySeconds").floatValue = 4f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var go = new GameObject("Hero");
            _created.Add(go);
            _created.Add(definition);
            _created.Add(weapon);
            var hero = go.AddComponent<Commander>();
            var posts = new List<HeroPost> { new HeroPost(new FakeGuarded(), new Vector3(-0.65f, 0f, 0f), Vector3.right) };
            hero.Initialize(definition, new TargetRegistry(), posts, 0, new PlayerModifiers(1f, 1f, 1f, 1f, 0, 1f, heroGear: gear));
            return hero;
        }

        [Test]
        public void ArmourPlates_AppearTierByTier_OnBonesTheBodyHas()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(HeroPath);
            HeroGearArtDefinition art = hero.Art != null ? hero.Art.Gear : null;
            Assert.IsNotNull(art, "the hero's art carries its gear art");
            Assert.IsNotNull(art.DroneBody, "the companion has a model");
            Assert.Greater(art.Plates.Length, 0);

            var root = new GameObject("Round66Plates");
            _created.Add(root);
            int before = -1;
            for (int tier = 0; tier <= 6; tier++)
            {
                GameObject figure = HeroView.BuildDisplayFigure(root.transform, hero, new HeroGear(0f, 0, 0f, tier, 10, false));
                int plates = 0;
                foreach (MeshRenderer renderer in figure.GetComponentsInChildren<MeshRenderer>())
                {
                    if (renderer.name == "ArmourPlate")
                    {
                        plates++;
                        Assert.AreEqual(3, renderer.sharedMaterials.Length, "face, trim and frame");
                    }
                }

                int expected = 0;
                foreach (ArmourPlateSpec plate in art.Plates)
                {
                    expected += plate.FromTier <= tier ? 1 : 0;
                }

                Assert.AreEqual(expected, plates, $"tier {tier}: every plate of the tier found its bone");
                Assert.GreaterOrEqual(plates, before, "a tier never takes a plate away");
                if (tier >= 1 && tier <= 5)
                {
                    Assert.Greater(plates, before, $"tier {tier} adds a piece");
                }

                before = plates;
                Object.DestroyImmediate(figure);
            }

            Assert.AreEqual(0, HeroArmourPlates.Build(_ => null, art, default), "no armour, no plates");
            var top = new HeroGear(0f, 0, 0f, 10, 10, false);
            Assert.AreEqual(art.PlateGrowth, HeroArmourPlates.Growth(1, top, art.PlateGrowth), 1e-4f, "the first plates are full grown at the last tier");
            Assert.AreEqual(1f, HeroArmourPlates.Growth(4, new HeroGear(0f, 0, 0f, 4, 10, false), art.PlateGrowth), 1e-4f, "a new plate starts at its own size");
        }

        // ---- Dr. Voss explains the screen ---------------------------------------------------------------------------

        [Test]
        public void UpgradeScreenExplanation_PlaysOncePerSave_AlsoWhenSkipped()
        {
            var campaign = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            DialogueDefinition tip = campaign.Story.UpgradeScreenTip;
            Assert.IsNotNull(tip, "the story carries the explanation");
            Assert.That(tip.Lines.Count, Is.InRange(2, 4), "two to four short lines");
            StringAssert.Contains(Rules.UpgradeTierCap(0).ToString(), tip.Lines[2].Segments[0].Text, "the line names the first continent's tier cap from the rules");
            foreach (DialogueLine line in tip.Lines)
            {
                Assert.AreEqual("Speaker_Voss", line.Speaker.name);
                Assert.LessOrEqual(line.CharacterCount, 150, "short enough for the box");
            }

            var store = new JsonSave();
            var progress = new ProgressService(store, new string[0]);
            Assert.AreSame(tip, Game.UI.UpgradeScreenGuide.Pending(campaign, progress), "a fresh save is told");

            // Skipped on its first line: seen all the same, and saved.
            var player = new DialoguePlayer(progress.MarkStorySeen);
            player.Enqueue(tip);
            Assert.IsTrue(player.TryStart());
            Assert.IsFalse(progress.HasSeenStory(tip.Id), "not seen while it still plays");
            player.SkipConversation();
            Assert.IsFalse(player.IsBusy);
            Assert.IsTrue(progress.HasSeenStory(tip.Id));
            Assert.IsNull(Game.UI.UpgradeScreenGuide.Pending(campaign, progress), "never again in this save");
            Assert.IsNull(Game.UI.UpgradeScreenGuide.Pending(campaign, new ProgressService(store, new string[0])), "nor after a reload");

            // Read to the end: seen as well.
            var other = new ProgressService(new JsonSave(), new string[0]);
            var reader = new DialoguePlayer(other.MarkStorySeen) { AutoAdvance = true };
            reader.Enqueue(tip);
            reader.TryStart();
            for (int i = 0; i < 20 && reader.IsBusy; i++)
            {
                reader.Tick(0.1f);
            }

            Assert.IsTrue(other.HasSeenStory(tip.Id));
        }

        [Test]
        public void Companion_StandsBesideTheFigure_OnlyOnceBought()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(HeroPath);
            var root = new GameObject("Round66Companion");
            _created.Add(root);
            GameObject without = HeroView.BuildDisplayFigure(root.transform, hero, default);
            Assert.IsNull(without.transform.Find("CompanionDrone"));
            GameObject with = HeroView.BuildDisplayFigure(root.transform, hero, new HeroGear(0.25f, 1, 0f, 0, 10, false));
            Transform drone = with.transform.Find("CompanionDrone");
            Assert.IsNotNull(drone, "the drone hovers at his shoulder on the upgrade screen");
            Assert.Less(drone.localScale.x, 1f, "a small drone");
            Assert.Greater(drone.localPosition.y, 1.2f, "above his shoulders");
        }
    }
}
