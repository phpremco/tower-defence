using System.Collections.Generic;
using System.IO;
using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Editor;
using Game.Enemies;
using Game.Hero;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The combat rules as a foundation (round 67, Remco: "one set of general combat rules that every damage source
    /// follows, with only explicit, named exceptions"). Three layers are held here:
    /// (1) the table: every damage type has a row, every weapon asset reads it and carries no factor of its own, an
    /// exception names its weapon and its reason, and a hit takes its layer share on the creature it lands on;
    /// (2) the roles: per creature trait, the best tower made for it beats every tower without the role per coin by the
    /// trait's margin, at level 1, at level 3 and at level 3 with every permanent upgrade against late waves, and no
    /// helper (hero, drone, path object, gate or base gun) does more per coin than the tower made for the trait;
    /// (3) the upgrade steps: every in-level step has a name and improves the one thing it says.
    /// The checks read every asset of each kind, so a new weapon or creature that breaks a role fails here.
    /// </summary>
    public sealed class CombatRulesTests
    {
        private const string DataRoot = "Assets/_Project/Data";

        private static CombatRules Rules()
        {
            CombatRules rules = RoleMatrix.Rules();
            Assert.IsNotNull(rules, "no combat rules asset: run Tools/PlayTest/Scripts/ApplyRound67.cs");
            return rules;
        }

        private static IEnumerable<T> All<T>(string folder) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { DataRoot + "/" + folder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // ---- (1) the table ---------------------------------------------------------------------------------------

        [Test]
        public void TheTable_HasARowForEveryDamageType_WithItsReason()
        {
            CombatRules rules = Rules();
            foreach (DamageType type in System.Enum.GetValues(typeof(DamageType)))
            {
                Assert.AreEqual(1, System.Array.FindAll(rules.Damage, r => r.Type == type).Length, type + ": exactly one row");
                Assert.IsNotEmpty(rules.RuleOf(type).Reason, type + ": a row says why");
            }
        }

        [Test]
        public void TheTable_CarriesRemcosNumbers()
        {
            CombatRules rules = Rules();
            // 2026-11-09: light fire 0.2. 2026-11-15: the blade 0.5, the Flak 0.5 (a named exception to its blast).
            // Round 87: a beam 0.1 (the Laser is weak on armour and on bosses; every Beam weapon reads the row).
            Assert.AreEqual(0.2f, rules.RuleOf(DamageType.Bullet).Armour, 1e-4f);
            Assert.AreEqual(0.1f, rules.RuleOf(DamageType.Beam).Armour, 1e-4f);
            Assert.AreEqual(0.5f, rules.RuleOf(DamageType.Blade).Armour, 1e-4f);
            Assert.AreEqual(1f, rules.RuleOf(DamageType.Explosive).Armour, 1e-4f);
            Assert.AreEqual(1.5f, rules.RuleOf(DamageType.Piercing).Armour, 1e-4f);
            Assert.AreEqual(0.5f, rules.ProfileOf(DamageType.Explosive, "Rocket").Armour, 1e-4f, "the Flak is no anti-armour gun");
            Assert.AreEqual(1f, rules.ProfileOf(DamageType.Explosive, "Rocket").Air, 1e-4f, "the Flak is the air specialist");
            Assert.AreEqual(1f, rules.ProfileOf(DamageType.Explosive, "Cannon").Armour, 1e-4f, "an exception touches its own weapon only");
        }

        [Test]
        public void EveryException_NamesAWeaponThatExists_ATraitWithAFactor_AndAReason()
        {
            CombatRules rules = Rules();
            var weapons = new HashSet<string>();
            foreach (string key in ProjectGenerator.WeaponDamageTypes.Keys)
            {
                weapons.Add(Path.GetFileName(key));
            }

            foreach (CombatException exception in rules.Exceptions)
            {
                Assert.IsTrue(weapons.Contains(exception.Weapon), exception.Weapon + " is no weapon in the data");
                Assert.IsTrue(exception.Trait == EnemyTraits.Armoured || exception.Trait == EnemyTraits.Airborne, exception.Weapon + ": only armour and air have a factor");
                Assert.IsNotEmpty(exception.Reason, exception.Weapon + ": an exception says why");
            }
        }

        [Test]
        public void EveryWeaponAsset_ReadsTheTable_AndAimsOnlyWhereItsDamageReaches()
        {
            CombatRules rules = Rules();
            var sources = new List<(Object Asset, TargetLayers Targets, DamageProfile Profile, DamageType Type)>();
            foreach (TowerDefinition tower in All<TowerDefinition>("Towers"))
            {
                if (!tower.IsSupport)
                {
                    sources.Add((tower, tower.Targets, tower.Profile, tower.DamageType));
                }
            }

            foreach (HeroWeaponDefinition weapon in All<HeroWeaponDefinition>("Hero"))
            {
                sources.Add((weapon, weapon.Targets, weapon.Profile, weapon.DamageType));
            }

            foreach (HeroAbilityDefinition ability in All<HeroAbilityDefinition>("Hero"))
            {
                if (ability.Damage > 0f)
                {
                    sources.Add((ability, ability.Targets, ability.Profile, ability.DamageType));
                }
            }

            foreach (DroneDefinition drone in All<DroneDefinition>("Drones"))
            {
                if (drone.Kind == DroneKind.Attack)
                {
                    sources.Add((drone, drone.Targets, drone.Profile, drone.DamageType));
                }
            }

            foreach (PathObjectDefinition gadget in All<PathObjectDefinition>("PathObjects"))
            {
                if (gadget.Damage > 0f)
                {
                    sources.Add((gadget, gadget.Targets, gadget.Profile, gadget.DamageType));
                }
            }

            Assert.Greater(sources.Count, 15, "the weapons were not found");
            foreach ((Object asset, TargetLayers targets, DamageProfile profile, DamageType type) in sources)
            {
                Assert.AreSame(rules, new SerializedObject(asset).FindProperty("_rules").objectReferenceValue, asset.name + " is not wired to the rule table");
                DamageProfile expected = rules.ProfileOf(type, asset.name);
                Assert.AreEqual(expected.Armour, profile.Armour, 1e-5f, asset.name + ": armour share is not the table's");
                Assert.AreEqual(expected.Air, profile.Air, 1e-5f, asset.name + ": air share is not the table's");
                if ((targets & TargetLayers.Air) != 0)
                {
                    Assert.Greater(profile.Air, 0f, $"{asset.name} aims at the sky with {type} damage, which never reaches it");
                }
            }
        }

        [Test]
        public void AHit_TakesItsLayerShareOnTheCreatureItLandsOn_AndHandsTheArmourShareOn()
        {
            var profile = new DamageProfile(0.2f, 0.25f);
            var walker = new Dummy(TargetLayers.Ground);
            var flyer = new Dummy(TargetLayers.Air);
            var wisp = new Dummy(TargetLayers.Ground | TargetLayers.Air);
            Hits.Deal(walker, 100f, profile);
            Hits.Deal(flyer, 100f, profile);
            Hits.Deal(wisp, 100f, profile);
            Assert.AreEqual(100f, walker.Taken, 1e-4f);
            Assert.AreEqual(25f, flyer.Taken, 1e-4f, "a creature only the sky reaches takes the air share");
            Assert.AreEqual(100f, wisp.Taken, 1e-4f, "a creature that also touches the ground takes the hit in full");
            Assert.AreEqual(0.2f, flyer.ArmourFactor, 1e-5f);
            Assert.AreEqual(DamageProfile.Full.Air, 1f);
        }

        // ---- (2) traits and roles ---------------------------------------------------------------------------------

        [Test]
        public void EveryCreature_HasOneSize_AndTheTraitsItsNumbersGive()
        {
            CombatRules rules = Rules();
            EnemyTraits sizes = EnemyTraits.Swarm | EnemyTraits.Mob | EnemyTraits.Tough;
            int count = 0;
            foreach (EnemyDefinition enemy in All<EnemyDefinition>("Enemies"))
            {
                EnemyTraits traits = RoleMatrix.TraitsOf(rules, enemy);
                EnemyTraits size = traits & sizes;
                Assert.IsTrue(size == EnemyTraits.Swarm || size == EnemyTraits.Mob || size == EnemyTraits.Tough, enemy.name + " needs exactly one size, has " + size);
                Assert.AreEqual(enemy.HasArmor, (traits & EnemyTraits.Armoured) != 0, enemy.name);
                Assert.AreEqual(enemy.Flies, (traits & EnemyTraits.Airborne) != 0, enemy.name + ": what flies is airborne and nothing else is");
                count++;
            }

            Assert.Greater(count, 30);
            Assert.AreEqual(EnemyTraits.Swarm | EnemyTraits.Fast, RoleMatrix.TraitsOf(rules, RoleMatrix.Enemy("Skitter")));
            Assert.AreEqual(EnemyTraits.Mob | EnemyTraits.Armoured, RoleMatrix.TraitsOf(rules, RoleMatrix.Enemy("Cragback")));
            Assert.AreEqual(EnemyTraits.Tough, RoleMatrix.TraitsOf(rules, RoleMatrix.Enemy("ShardCrawler")));
        }

        [Test]
        public void EveryTrait_HasAReferenceFight_WithACreatureOfThatTrait()
        {
            CombatRules rules = Rules();
            foreach (EnemyTraits trait in new[] { EnemyTraits.Armoured, EnemyTraits.Airborne, EnemyTraits.Swarm, EnemyTraits.Mob, EnemyTraits.Tough, EnemyTraits.Fast })
            {
                Assert.IsTrue(rules.TryScenario(trait, out RoleScenario scenario), trait + " has no reference fight");
                EnemyDefinition enemy = RoleMatrix.Enemy(scenario.Enemy);
                Assert.IsNotNull(enemy, scenario.Enemy);
                Assert.AreNotEqual(EnemyTraits.None, RoleMatrix.TraitsOf(rules, enemy) & trait, $"{scenario.Enemy} does not have the trait {trait} it stands for");
                Assert.GreaterOrEqual(scenario.Margin, 1.1f, trait + ": a margin under 1.1 is no role");
            }

            // Remco: at least twice for air and armour.
            rules.TryScenario(EnemyTraits.Armoured, out RoleScenario armour);
            rules.TryScenario(EnemyTraits.Airborne, out RoleScenario air);
            Assert.GreaterOrEqual(armour.Margin, 2f);
            Assert.GreaterOrEqual(air.Margin, 2f);
        }

        [Test]
        public void EveryTowerThePlayerBuilds_DeclaresItsRole()
        {
            int towers = 0;
            foreach (TowerDefinition tower in All<TowerDefinition>("Towers"))
            {
                Assert.IsNotEmpty(tower.RoleLine, tower.name + " has no role line");
                bool structure = tower.BlocksGroundEnemies || tower.IsBaseDefence;
                Assert.AreEqual(structure, tower.Role == TowerRole.Helper, tower.name + ": a tower the player builds has a role, a gate or base gun only helps");
                towers++;
            }

            Assert.Greater(towers, 10);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TheRoles_Hold_PerCoin(int moment)
        {
            List<string> failures = RoleChecks.TowerFailures(Rules(), RoleChecks.Moments[moment]);
            Assert.IsEmpty(failures, "A tower without the role comes too close to the towers made for a trait:\n" + string.Join("\n", failures));
        }

        [Test]
        public void NoHelper_ReplacesARole()
        {
            List<string> failures = RoleChecks.HelperFailures(Rules());
            Assert.IsEmpty(failures, "A helper does more per coin than the tower made for the trait:\n" + string.Join("\n", failures));
        }

        [Test]
        public void ABrokenRule_IsCaught()
        {
            // The round 61 and round 64 bugs, put back in memory: bullets that land in full on flyers, a beam that melts armour.
            CombatRules rules = Rules();
            Assert.IsEmpty(RoleChecks.TowerFailures(rules, RoleChecks.Moments[0]));
            CombatRules bullets = Object.Instantiate(rules);
            CombatRules beams = Object.Instantiate(rules);
            try
            {
                SetRule(bullets, DamageType.Bullet, "Air", 1f);
                SetRule(beams, DamageType.Beam, "Armour", 1f);
                StringAssert.Contains("Airborne", string.Join("\n", RoleChecks.TowerFailures(bullets, RoleChecks.Moments[0])), "a Gun Turret that kills flyers must fail the roles");
                StringAssert.Contains("Armoured", string.Join("\n", RoleChecks.TowerFailures(beams, RoleChecks.Moments[0])), "a Laser that melts armour must fail the roles");
            }
            finally
            {
                Object.DestroyImmediate(bullets);
                Object.DestroyImmediate(beams);
            }
        }

        private static void SetRule(CombatRules rules, DamageType type, string column, float value)
        {
            var so = new SerializedObject(rules);
            SerializedProperty rows = so.FindProperty("_damage");
            for (int i = 0; i < rows.arraySize; i++)
            {
                SerializedProperty row = rows.GetArrayElementAtIndex(i);
                if (row.FindPropertyRelative("Type").enumValueIndex == (int)type)
                {
                    row.FindPropertyRelative(column).floatValue = value;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- (3) upgrade steps ------------------------------------------------------------------------------------

        [Test]
        public void EveryUpgradeStep_HasAName_AndImprovesTheOneThingItSays()
        {
            foreach (TowerDefinition tower in All<TowerDefinition>("Towers"))
            {
                Assert.IsNull(tower.StepProblem(), tower.name);
                for (int i = 1; i <= tower.MaxLevelIndex; i++)
                {
                    bool structure = tower.BlocksGroundEnemies || tower.IsBaseDefence;
                    Assert.AreEqual(structure, tower.GetLevel(i).Step == TowerStep.Refit, tower.name + ": only a gate or the base's turret is refitted");
                }

                Assert.AreEqual(TowerStep.None, tower.GetLevel(0).Step, tower.name + ": no step leads to the level it is built at");
            }
        }

        [Test]
        public void AStepThatChangesTwoThings_IsRefused()
        {
            var tower = ScriptableObject.CreateInstance<TowerDefinition>();
            try
            {
                var so = new SerializedObject(tower);
                SerializedProperty levels = so.FindProperty("_levels");
                levels.arraySize = 2;
                Write(levels.GetArrayElementAtIndex(0), 5f, 4f, 0.5f, string.Empty, TowerStep.None);
                Write(levels.GetArrayElementAtIndex(1), 9f, 4.3f, 0.5f, "Heavier rounds", TowerStep.Damage);
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsNotNull(tower.StepProblem(), "damage and range in one step");
                so.Update();
                Write(levels.GetArrayElementAtIndex(1), 9f, 4f, 0.5f, "Heavier rounds", TowerStep.Damage);
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsNull(tower.StepProblem());
                so.Update();
                Write(levels.GetArrayElementAtIndex(1), 9f, 4f, 0.5f, "Faster cycling", TowerStep.FireRate);
                so.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsNotNull(tower.StepProblem(), "a step named for the fire rate that changes the damage");
            }
            finally
            {
                Object.DestroyImmediate(tower);
            }
        }

        private static void Write(SerializedProperty level, float damage, float range, float interval, string step, TowerStep improves)
        {
            level.FindPropertyRelative("Damage").floatValue = damage;
            level.FindPropertyRelative("Range").floatValue = range;
            level.FindPropertyRelative("FireInterval").floatValue = interval;
            level.FindPropertyRelative("StepName").stringValue = step;
            level.FindPropertyRelative("Step").enumValueIndex = (int)improves;
        }

        // ---- growth ----------------------------------------------------------------------------------------------

        [Test]
        public void TheGraceWindow_ShrinksWithTheDifficulty_AndJudgesARow()
        {
            CombatRules rules = Rules();
            for (int d = 1; d < CombatRules.DifficultyCount; d++)
            {
                Assert.LessOrEqual(rules.GraceWaves(d), rules.GraceWaves(d - 1), "a harder difficulty forgives no longer");
            }

            Assert.AreEqual(0, rules.GraceWaves(3), "Psycho gives no grace");
            // A trait from wave 9 of 16, window 2 and 4: the row may hold waves 9 and 10 and must have fallen by wave 14.
            Assert.AreEqual(GraceWindows.Verdict.Early, GraceWindows.Judge(9, 16, 10, 0, 3, 2, 4));
            Assert.AreEqual(GraceWindows.Verdict.InTime, GraceWindows.Judge(9, 16, 11, 0, 3, 2, 4));
            Assert.AreEqual(GraceWindows.Verdict.InTime, GraceWindows.Judge(9, 16, 14, 0, 3, 2, 4));
            Assert.AreEqual(GraceWindows.Verdict.Late, GraceWindows.Judge(9, 16, 15, 0, 3, 2, 4));
            Assert.AreEqual(GraceWindows.Verdict.Late, GraceWindows.Judge(9, 16, 0, 3, 3, 2, 4), "a row that wins did not need the counter");
            Assert.AreEqual(GraceWindows.Verdict.NotJudged, GraceWindows.Judge(9, 16, 7, 0, 3, 2, 4), "it fell before the trait came");
            Assert.AreEqual(GraceWindows.Verdict.NotJudged, GraceWindows.Judge(14, 16, 0, 3, 3, 2, 4), "too close to the end to judge");
        }

        [Test]
        public void NoRowWithoutTheCounter_OutlastsItsWindow_AsPlayed()
        {
            CombatRules rules = Rules();
            if (GraceWindows.Rows(rules).Count == 0)
            {
                Assert.Ignore("No yardstick rows (Tools/PlayTest/calibration_records.txt): run balance_pipeline.sh measure first.");
            }

            List<string> breaks = GraceWindows.Breaks(rules);
            Assert.IsEmpty(breaks, "the growth layer does not hold (Docs/COMBAT_RULES.md section 6): " + string.Join("; ", breaks));
        }

        [Test]
        public void EveryAllowance_HasAReason_AndNoneForgivesARowThatWins()
        {
            foreach (GraceWindows.Allowance allowance in GraceWindows.Allowances)
            {
                Assert.IsNotEmpty(allowance.Reason, $"{allowance.Level} {allowance.Difficulty} {allowance.Trait} needs a reason");
                Assert.Greater(allowance.WavesLate, 0, $"{allowance.Level} {allowance.Difficulty} {allowance.Trait}: an allowance of no waves is no allowance");
            }
        }

        [Test]
        public void PlatedTroops_ComeAsWrittenThroughTheWindow_AndGrowAfterIt()
        {
            var enemies = new Dictionary<string, Game.Balance.EnemyStats>
            {
                ["Soft"] = new Game.Balance.EnemyStats { Id = "Soft", Health = 20f, Speed = 1f },
                ["Plated"] = new Game.Balance.EnemyStats { Id = "Plated", Health = 60f, Armor = 160f, Speed = 0.6f },
            };
            var script = new Game.Balance.WaveScriptSpec
            {
                Key = "Growth", Waves = 12, Budget = 40f, FinalBudget = 40f,
                Troops = new[]
                {
                    new Game.Balance.TroopSpec { Enemy = "Soft", FirstWave = 1, Weight = 1f },
                    new Game.Balance.TroopSpec { Enemy = "Plated", FirstWave = 4, Weight = 1f },
                },
                GraceWaves = new[] { 2 },
                FallWithinWaves = new[] { 3 },
            };
            // The growth is the difficulty's rule (round 84; a script lever until then): one script, read with and without it.
            var plain = new Game.Balance.BalanceRules { ArmourGrowth = new[] { 1f } };
            var grown = new Game.Balance.BalanceRules { ArmourGrowth = new[] { 1.5f } };
            int Plated(Game.Balance.BalanceRules rules, int number)
            {
                int count = 0;
                foreach (Game.Balance.GroupSpec group in Game.Balance.WaveComposer.Compose(script, enemies, 0, number, 1f, -1, rules).Groups)
                {
                    count += group.Enemy.Armor > 0f ? group.Count : 0;
                }

                return count;
            }

            Assert.AreEqual(4, Game.Balance.WaveComposer.FirstTraitWave(script, enemies, 0, -1, grown, false), "the window opens on the first plated wave");
            for (int number = 1; number <= 5; number++)
            {
                Assert.AreEqual(Plated(plain, number), Plated(grown, number), $"wave {number}: inside the grace window the troops come as written");
                Assert.IsFalse(Game.Balance.WaveComposer.Compose(script, enemies, 0, number, 1f, -1, grown).CounterCheck, $"wave {number} asks for no counter yet");
            }

            Assert.Greater(Plated(grown, 6), Plated(plain, 6), "the first wave after the window brings more of them");
            Assert.Greater(Plated(grown, 8), Plated(grown, 6), "and they keep growing through the waves a row must fall in");
            Assert.AreEqual(Mathf.Pow(1.5f, 3f), Game.Balance.WaveComposer.GrowthOn(script, enemies, 0, 8, -1, grown).Armour, 0.001f, "three waves of growth: the waves a row must fall in");
            Assert.AreEqual(Game.Balance.WaveComposer.GrowthOn(script, enemies, 0, 8, -1, grown).Armour, Game.Balance.WaveComposer.GrowthOn(script, enemies, 0, 12, -1, grown).Armour, "then they stay as they are");
            Assert.IsTrue(Game.Balance.WaveComposer.Compose(script, enemies, 0, 8, 1f, -1, grown).CounterCheck, "a grown wave is a counter check: off the mean of the plain waves");
            Assert.IsFalse(Game.Balance.WaveComposer.Compose(script, enemies, 0, 8, 1f, -1, plain).CounterCheck);
        }

        [Test]
        public void ATroopWithAHardestDifficulty_StaysOffTheHarderOnes()
        {
            var enemies = new Dictionary<string, Game.Balance.EnemyStats>
            {
                ["Soft"] = new Game.Balance.EnemyStats { Id = "Soft", Health = 20f, Speed = 1f },
                ["Plated"] = new Game.Balance.EnemyStats { Id = "Plated", Health = 60f, Armor = 160f, Speed = 0.6f },
            };
            var script = new Game.Balance.WaveScriptSpec
            {
                Key = "Late", Waves = 12, Budget = 40f, FinalBudget = 40f,
                Troops = new[]
                {
                    new Game.Balance.TroopSpec { Enemy = "Soft", FirstWave = 1, Weight = 1f },
                    new Game.Balance.TroopSpec { Enemy = "Plated", FirstWave = 8, Weight = 1f, MaxDifficulty = 1 },
                    new Game.Balance.TroopSpec { Enemy = "Plated", FirstWave = 4, Weight = 1f, MinDifficulty = 2 },
                },
            };
            var rules = new Game.Balance.BalanceRules();
            Assert.AreEqual(8, Game.Balance.WaveComposer.FirstTraitWave(script, enemies, 1, -1, rules, false), "Medium: the plated troop comes from wave 8");
            Assert.AreEqual(4, Game.Balance.WaveComposer.FirstTraitWave(script, enemies, 2, -1, rules, false), "Hard: only the Hard troop, from wave 4");
        }

        [Test]
        public void TheReports_AreWritten()
        {
            CombatRules rules = Rules();
            File.WriteAllText(RoleChecks.ReportPath, RoleChecks.Report(rules) + "\n### Creatures and their traits\n\n" + RoleChecks.Bestiary(rules));
            File.WriteAllText(GraceWindows.ReportPath, GraceWindows.Report(rules));
            Debug.Log($"[CombatRules] {RoleChecks.ReportPath} and {GraceWindows.ReportPath} written");
        }

        private sealed class Dummy : ITargetable, ISourcedTarget
        {
            public Dummy(TargetLayers layer)
            {
                Layer = layer;
            }

            public Vector3 Position => Vector3.zero;
            public TargetLayers Layer { get; }
            public float DistanceToGoal => 0f;
            public bool IsAlive => true;
            public float Taken { get; private set; }
            public float ArmourFactor { get; private set; }

            public void TakeDamage(float amount) => Taken += amount;

            public void ApplySlow(float speedFactor, float seconds)
            {
            }

            public void TakeDamageFrom(float amount, object source, bool splash, float armourFactor = 1f)
            {
                Taken += amount;
                ArmourFactor = armourFactor;
            }
        }
    }
}
