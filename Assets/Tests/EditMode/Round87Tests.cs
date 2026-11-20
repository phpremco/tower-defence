using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 87 (Remco: the Laser is the answer to big, fast swarms of light creatures, with a small pulse at a high
    /// rate, and weak on armour and on one big target; the Gun Turret stays the all-rounder; "a bonus against certain
    /// enemies, e.g. energy enemies" is a perk). Three things are held here:
    /// (1) the family table of the combat rules: a damage type is worth one on every creature family the table does not
    /// list, the factor is taken on the creature that is hit, before its air share and its armour, and every weapon reads
    /// it from the table by its damage type (no weapon has a bonus of its own);
    /// (2) the retarget time: a turret pays a swing for a new target, a tower that was idle or stays on its target does
    /// not, and the same comes out at the game's frame length and at the probe's;
    /// (3) the Laser's role, played through the firing rules (StreamDuel) per level and difficulty: on a swarm that
    /// overloads it a coin in the Laser buys at least the specialist premium over a coin in the Gun Turret (the price
    /// rule), on one big plain body a little less, on armour and on a boss far less, and far less than the Cannon.
    /// </summary>
    public sealed class Round87Tests
    {
        private const string DataRoot = "Assets/_Project/Data";

        // The Laser beside the Gun Turret per coin on one big plain body (the lead's band for Remco's corrected role).
        private const float BigBodyLeast = 0.85f;
        private const float BigBodyMost = 1f;

        // On armour the table gives a beam half a bullet's share; on a boss (an armour pool as big as its body) the
        // Laser stays clearly under the Gun Turret and far under the Cannon.
        private const float ArmourMostOfGun = 0.6f;
        private const float BossMostOfGun = 0.65f;
        private const float BossMostOfCannon = 0.5f;

        // The swing is modest where a turret can keep up: a level 1 Gun Turret on a body a second loses at most this
        // share of its fire to it.
        private const float LooseSwingLossMost = 0.2f;

        // A rule that is paid like a reload gives the same at sixty frames a second and at the probe's 0.2 s steps:
        // within a few percent on an overloaded stream, the most (4.6 percent) where the bodies come nearly a step apart.
        private const float FrameTolerance = 0.06f;

        private const int StreamBodies = 160;
        private const float BigBodyScale = 12f;
        private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        private static CombatRules Rules()
        {
            CombatRules rules = RoleMatrix.Rules();
            Assert.IsNotNull(rules, "no combat rules asset: run Tools/PlayTest/Scripts/ApplyRound87Laser.cs");
            return rules;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DataRoot}/{path}.asset");
            Assert.IsNotNull(asset, path);
            return asset;
        }

        private static IEnumerable<T> All<T>(string folder) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { DataRoot + "/" + folder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        private static CombatRules RulesWith(params FamilyRule[] families)
        {
            var rules = ScriptableObject.CreateInstance<CombatRules>();
            var so = new SerializedObject(rules);
            SerializedProperty array = so.FindProperty("_families");
            array.arraySize = families.Length;
            for (int i = 0; i < families.Length; i++)
            {
                SerializedProperty row = array.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Type").enumValueIndex = (int)families[i].Type;
                row.FindPropertyRelative("Family").enumValueIndex = (int)families[i].Family;
                row.FindPropertyRelative("Factor").floatValue = families[i].Factor;
                row.FindPropertyRelative("Reason").stringValue = families[i].Reason;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return rules;
        }

        // ---- (1) the family table --------------------------------------------------------------------------------

        [Test]
        public void APairTheTableDoesNotList_IsWorthOne()
        {
            CombatRules empty = RulesWith();
            CombatRules one = RulesWith(new FamilyRule { Type = DamageType.Beam, Family = CreatureFamily.Energy, Factor = 2f, Reason = "test" });
            try
            {
                foreach (DamageType type in System.Enum.GetValues(typeof(DamageType)))
                {
                    foreach (CreatureFamily family in System.Enum.GetValues(typeof(CreatureFamily)))
                    {
                        Assert.AreEqual(1f, empty.FamilyFactor(type, family), 1e-6f, $"{type} on {family}: an empty table");
                        Assert.AreEqual(1f, empty.ProfileOf(type, string.Empty).Against(family), 1e-6f, $"{type} on {family}: the profile of an empty table");
                        float expected = type == DamageType.Beam && family == CreatureFamily.Energy ? 2f : 1f;
                        Assert.AreEqual(expected, one.FamilyFactor(type, family), 1e-6f, $"{type} on {family}");
                        Assert.AreEqual(expected, one.ProfileOf(type, string.Empty).Against(family), 1e-6f, $"{type} on {family}: the profile says what the table says");
                    }
                }

                Assert.AreEqual(1f, DamageProfile.Full.Against(CreatureFamily.Energy), 1e-6f, "a weapon without rules lands in full, no more");
            }
            finally
            {
                Object.DestroyImmediate(empty);
                Object.DestroyImmediate(one);
            }
        }

        [Test]
        public void ACreatureOfNoFamily_TakesEveryDamageTypeAsItsRowSays()
        {
            // A row on None would be a second damage table under another name: the lookup and the profile both ignore it.
            CombatRules rules = RulesWith(new FamilyRule { Type = DamageType.Bullet, Family = CreatureFamily.None, Factor = 3f, Reason = "test" });
            try
            {
                Assert.AreEqual(1f, rules.FamilyFactor(DamageType.Bullet, CreatureFamily.None), 1e-6f);
                Assert.AreEqual(1f, rules.ProfileOf(DamageType.Bullet, string.Empty).Against(CreatureFamily.None), 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(rules);
            }
        }

        [Test]
        public void TheTable_CarriesRemcosBeamOnEnergy_AndEveryEntrySaysWhy()
        {
            CombatRules rules = Rules();
            Assert.AreEqual(1.5f, rules.FamilyFactor(DamageType.Beam, CreatureFamily.Energy), 1e-4f, "a beam overloads an energy body: half again the hit (round 87, a perk beside the role)");
            Assert.AreEqual(1f, rules.FamilyFactor(DamageType.Bullet, CreatureFamily.Energy), 1e-6f, "the Gun Turret has no bonus");
            Assert.AreEqual(0.1f, rules.RuleOf(DamageType.Beam).Armour, 1e-4f, "a beam barely marks armour: a tenth (round 87), half a bullet's share");
            Assert.AreEqual(0.2f, rules.RuleOf(DamageType.Bullet).Armour, 1e-4f);
            var seen = new HashSet<(DamageType, CreatureFamily)>();
            foreach (FamilyRule rule in rules.Families)
            {
                Assert.AreNotEqual(CreatureFamily.None, rule.Family, $"{rule.Type}: an entry names a family");
                Assert.IsNotEmpty(rule.Reason, $"{rule.Type} on {rule.Family}: an entry says why");
                Assert.Greater(rule.Factor, 0f, $"{rule.Type} on {rule.Family}");
                Assert.IsTrue(seen.Add((rule.Type, rule.Family)), $"{rule.Type} on {rule.Family}: one entry per pair");
            }
        }

        [Test]
        public void TheFamilyFactor_IsTakenOnTheCreatureHit_BeforeItsAirShareAndItsArmour()
        {
            var profile = new DamageProfile(0.2f, 0.25f, new[] { 1f, 2f });
            var walker = new Dummy(TargetLayers.Ground, CreatureFamily.None);
            var energyWalker = new Dummy(TargetLayers.Ground, CreatureFamily.Energy);
            var energyFlyer = new Dummy(TargetLayers.Air, CreatureFamily.Energy);
            Hits.Deal(walker, 100f, profile);
            Hits.Deal(energyWalker, 100f, profile);
            Hits.Deal(energyFlyer, 100f, profile);
            Assert.AreEqual(100f, walker.Taken, 1e-4f, "an ordinary body takes the hit as it is");
            Assert.AreEqual(200f, energyWalker.Taken, 1e-4f, "an energy body takes the family factor");
            Assert.AreEqual(50f, energyFlyer.Taken, 1e-4f, "in the air the family factor and the air share both apply");
            Assert.AreEqual(0.2f, energyWalker.ArmourFactor, 1e-6f, "the armour share travels on unchanged");
            Assert.AreEqual(profile.Against(CreatureFamily.Energy) * profile.Against(TargetLayers.Air), profile.Against(TargetLayers.Air, CreatureFamily.Energy), 1e-6f);

            // On an energy body's armour: twice the hit, of which a fifth works on the pool (Beam on Energy armour is 2 x 0.2).
            var plated = new Health(50f, 100f);
            plated.TakeDamage(100f * profile.Against(TargetLayers.Ground, CreatureFamily.Energy), profile.Armour);
            Assert.AreEqual(60f, plated.Armor, 1e-3f);
            Assert.AreEqual(50f, plated.Current, 1e-3f, "nothing reaches the body while armour is left");
        }

        [Test]
        public void ATargetThatSaysNoFamily_IsHitAsBefore()
        {
            var profile = new DamageProfile(0.2f, 0.25f, new[] { 1f, 2f });
            var gate = new Plain();
            Hits.Deal(gate, 100f, profile);
            Assert.AreEqual(100f, gate.Taken, 1e-4f);
        }

        [Test]
        public void EveryWeapon_ReadsItsFamilyFactorsFromTheTable_ByItsDamageType()
        {
            CombatRules rules = Rules();
            int beams = 0;
            foreach (TowerDefinition tower in All<TowerDefinition>("Towers"))
            {
                foreach (CreatureFamily family in System.Enum.GetValues(typeof(CreatureFamily)))
                {
                    Assert.AreEqual(rules.FamilyFactor(tower.DamageType, family), tower.Profile.Against(family), 1e-6f, $"{tower.name} on {family}: no tower has a factor of its own");
                }

                beams += tower.DamageType == DamageType.Beam ? 1 : 0;
            }

            // The rule is the beam's, not the Laser's: the Cryo and the Cryo Beam overload an energy body too.
            Assert.GreaterOrEqual(beams, 3, "Laser, Cryo and Cryo Beam are beams");
            Assert.AreEqual(rules.FamilyFactor(DamageType.Beam, CreatureFamily.Energy), Load<TowerDefinition>("Towers/CryoBeam").Profile.Against(CreatureFamily.Energy), 1e-4f);
        }

        [Test]
        public void TheEnergyCreatures_AreTheOnesTheGeneratorLists()
        {
            foreach (EnemyDefinition enemy in All<EnemyDefinition>("Enemies"))
            {
                CreatureFamily expected = ProjectGenerator.CreatureFamilies.TryGetValue(enemy.name, out CreatureFamily listed) ? listed : CreatureFamily.None;
                Assert.AreEqual(expected, enemy.Family, enemy.name + ": run Tools/PlayTest/Scripts/ApplyRound87Laser.cs");
            }

            Assert.AreEqual(CreatureFamily.Energy, Load<EnemyDefinition>("Enemies/Wisp").Family, "a ball of energy");
            Assert.AreEqual(CreatureFamily.Energy, Load<EnemyDefinition>("Enemies/CoreWisp").Family, "a core of raw energy");
            Assert.AreEqual(CreatureFamily.None, Load<EnemyDefinition>("Enemies/Skitter").Family);
        }

        [Test]
        public void ACreature_IsHitAndPricedAtItsFamilysFactor()
        {
            var go = new GameObject("Round87Enemy");
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            try
            {
                var so = new SerializedObject(definition);
                so.FindProperty("_maxHealth").floatValue = 40f;
                so.FindProperty("_armor").floatValue = 0f;
                so.FindProperty("_family").enumValueIndex = (int)CreatureFamily.Energy;
                so.ApplyModifiedPropertiesWithoutUndo();
                var enemy = go.AddComponent<Enemy>();
                enemy.Initialize(definition, new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
                var beam = new DamageProfile(0.2f, 0.2f, new[] { 1f, 2f });
                var bullet = new DamageProfile(0.2f, 0.2f);

                Assert.AreEqual(CreatureFamily.Energy, enemy.Family);
                // The beam's kill pulse (BeamBudget) is priced on what the creature really takes: half the hit kills it.
                Assert.AreEqual(20f, enemy.HitToKill(beam), 1e-3f);
                Assert.AreEqual(40f, enemy.HitToKill(bullet), 1e-3f);

                Hits.Deal(enemy, 5f, bullet, null, false);
                Assert.AreEqual(35f, enemy.Health.Current, 1e-3f, "a bullet lands as it is");
                Hits.Deal(enemy, 5f, beam, null, false);
                Assert.AreEqual(25f, enemy.Health.Current, 1e-3f, "a beam pulse lands twice");
                Hits.Deal(enemy, enemy.HitToKill(beam), beam, null, false);
                Assert.IsTrue(enemy.Health.IsDead, "the hit it asked for kills it");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(definition);
            }
        }

        // ---- (2) the retarget time -------------------------------------------------------------------------------

        [Test]
        public void ATurret_PaysItsSwingForANewTarget_AndNothingOtherwise()
        {
            Assert.AreEqual(0.15f, Retarget.Seconds(true, false, 0.15f), 1e-6f, "another creature than the last shot went to: the swing");
            Assert.AreEqual(0f, Retarget.Seconds(true, true, 0.15f), 1e-6f, "the same creature: no swing");
            Assert.AreEqual(0f, Retarget.Seconds(false, false, 0.15f), 1e-6f, "an idle tower has had the time to turn");
            Assert.AreEqual(0f, Retarget.Seconds(true, false, 0f), 1e-6f, "a tower that does not swing");
        }

        [Test]
        public void TheGunTurretSwings_AndTheBeamDoesNot()
        {
            Assert.Greater(Load<TowerDefinition>("Towers/PulseRifle").RetargetSeconds, 0f, "a turret swings to a new target");
            Assert.AreEqual(Load<TowerDefinition>("Towers/PulseRifle").RetargetSeconds, Load<TowerDefinition>("Towers/StormRifle").RetargetSeconds, 1e-6f, "its hybrid keeps the turret");
            Assert.AreEqual(0f, Load<TowerDefinition>("Towers/Laser").RetargetSeconds, 1e-6f, "a beam is on the next creature at once");
            Assert.AreEqual(0f, Load<TowerDefinition>("Towers/Cannon").RetargetSeconds, 1e-6f);
        }

        [Test]
        public void TheSwing_IsModestWhereATurretKeepsUp_AndTheSameAtEveryFrameLength()
        {
            var gun = Load<TowerDefinition>("Towers/PulseRifle");
            TowerDefinition still = Object.Instantiate(gun);
            try
            {
                var so = new SerializedObject(still);
                so.FindProperty("_retargetSeconds").floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
                ForEachDifficulty((difficulty, swarm) =>
                {
                    StreamDuel.Stream loose = Swarm(swarm, 1f);
                    float with = StreamDuel.Dps(gun, 0, loose, StreamDuel.GameStep);
                    float without = StreamDuel.Dps(still, 0, loose, StreamDuel.GameStep);
                    Assert.Less(with, without, $"{difficulty.name}: the swing costs something");
                    Assert.GreaterOrEqual(with, without * (1f - LooseSwingLossMost), $"{difficulty.name}: a level 1 Gun Turret on a body a second lands {with:0.0} of {without:0.0} without a swing");
                    for (int level = 0; level <= gun.MaxLevelIndex; level++)
                    {
                        // More bodies than the turret can kill, so it fires without a pause and what is compared is its fire.
                        // A step of the probe sees one change of target at most: denser streams than that are not its to judge.
                        float spacing = StreamDuel.OverloadSpacing(gun, level, swarm.Body);
                        if (spacing < StreamDuel.ProbeStep)
                        {
                            continue;
                        }

                        float game = StreamDuel.Dps(gun, level, Swarm(swarm, spacing), StreamDuel.GameStep);
                        float probe = StreamDuel.Dps(gun, level, Swarm(swarm, spacing), StreamDuel.ProbeStep);
                        Assert.AreEqual(1f, probe / game, FrameTolerance, $"{difficulty.name} L{level + 1}, a body every {spacing:0.00} s: {game:0.0} at sixty frames, {probe:0.0} at 0.2 s steps");
                    }
                });
            }
            finally
            {
                Object.DestroyImmediate(still);
            }
        }

        // ---- (3) the Laser's role --------------------------------------------------------------------------------

        [Test]
        public void TheLaser_FiresASmallerPulseFasterThanTheGunTurret_AtEveryLevel()
        {
            var laser = Load<TowerDefinition>("Towers/Laser");
            var gun = Load<TowerDefinition>("Towers/PulseRifle");
            Assert.AreEqual(gun.MaxLevelIndex, laser.MaxLevelIndex);
            for (int i = 0; i <= laser.MaxLevelIndex; i++)
            {
                Assert.Less(laser.GetLevel(i).Damage, gun.GetLevel(i).Damage, $"level {i + 1}: the pulse is smaller than the round");
                Assert.Less(laser.GetLevel(i).FireInterval, gun.GetLevel(i).FireInterval, $"level {i + 1}: the beam pulses faster than the gun fires");
            }

            Assert.IsNull(laser.StepProblem(), "every step still improves the one thing it names");
        }

        [Test]
        public void OnASwarmThatOverloadsIt_TheLaserCostsNoMoreThanThePriceRuleAllows()
        {
            var laser = Load<TowerDefinition>("Towers/Laser");
            var gun = Load<TowerDefinition>("Towers/PulseRifle");
            ForEachDifficulty((difficulty, swarm) =>
            {
                for (int level = 0; level <= laser.MaxLevelIndex; level++)
                {
                    // The Laser's own target mix: more light fast bodies than its data can kill, so both guns fire without a pause.
                    StreamDuel.Stream stream = Swarm(swarm, StreamDuel.OverloadSpacing(laser, level, swarm.Body));
                    float laserValue = StreamDuel.Dps(laser, level, stream, StreamDuel.GameStep);
                    float gunValue = StreamDuel.Dps(gun, level, stream, StreamDuel.GameStep);
                    int cap = BalanceSheet.SpecialistPriceCap(laserValue, gunValue, StreamDuel.Coins(gun, level));
                    Assert.LessOrEqual(StreamDuel.Coins(laser, level), cap, $"{difficulty.name} L{level + 1}: the Laser takes off {laserValue:0.0} a second where the Gun Turret takes {gunValue:0.0} for {StreamDuel.Coins(gun, level)} coins; at the premium x{BalanceSheet.SpecialistPremium} it may cost {cap}");
                }
            });
        }

        [Test]
        public void ThePriceRule_IsThePremiumOverTheAllRoundersCoin()
        {
            // The all-rounder takes off 20 a second for 100 coins; a specialist that takes off 50 there may cost 50 over the
            // premium times 0.2, rounded down to the price step of five coins.
            int expected = Mathf.FloorToInt(50f / (BalanceSheet.SpecialistPremium * 0.2f) / 5f) * 5;
            Assert.AreEqual(expected, BalanceSheet.SpecialistPriceCap(50f, 20f, 100));
            Assert.Greater(BalanceSheet.SpecialistPremium, 1f, "a specialist that buys no more than the all-rounder on its own job is a trap");
            Assert.AreEqual(0, BalanceSheet.SpecialistPriceCap(50f, 0f, 100), "nothing to compare with: no price");
        }

        [Test]
        public void OnOneBigPlainBody_TheLaserBuysALittleLessThanTheGunTurret()
        {
            var laser = Load<TowerDefinition>("Towers/Laser");
            var gun = Load<TowerDefinition>("Towers/PulseRifle");
            var prowler = Load<EnemyDefinition>("Enemies/Prowler");
            ForEachDifficulty((difficulty, swarm) =>
            {
                StreamDuel.Stream big = StreamDuel.Stream.Of(prowler, Scale(difficulty, prowler) * BigBodyScale, 1f, 1);
                for (int level = 0; level <= laser.MaxLevelIndex; level++)
                {
                    float ratio = PerCoin(laser, level, big) / PerCoin(gun, level, big);
                    Assert.That(ratio, Is.InRange(BigBodyLeast, BigBodyMost), $"{difficulty.name} L{level + 1}: x{ratio:0.00} of the Gun Turret per coin on one big plain body");
                }
            });
        }

        [Test]
        public void OnArmourAndOnABoss_TheLaserIsFarUnderTheGunTurretAndTheCannon()
        {
            var laser = Load<TowerDefinition>("Towers/Laser");
            var gun = Load<TowerDefinition>("Towers/PulseRifle");
            var cannon = Load<TowerDefinition>("Towers/Cannon");
            var prowler = Load<EnemyDefinition>("Enemies/Prowler");
            ForEachDifficulty((difficulty, swarm) =>
            {
                StreamDuel.Stream boss = StreamDuel.Stream.Of(prowler, Scale(difficulty, prowler) * BigBodyScale, 1f, 1);
                boss.Armour = boss.Body;
                StreamDuel.Stream plate = boss;
                plate.Body = 1f;
                for (int level = 0; level <= laser.MaxLevelIndex; level++)
                {
                    float onPlate = PerCoin(laser, level, plate) / PerCoin(gun, level, plate);
                    float onBoss = PerCoin(laser, level, boss) / PerCoin(gun, level, boss);
                    float underCannon = PerCoin(laser, level, boss) / PerCoin(cannon, level, boss);
                    Assert.LessOrEqual(onPlate, ArmourMostOfGun, $"{difficulty.name} L{level + 1}: x{onPlate:0.00} of the Gun Turret per coin on armour");
                    Assert.LessOrEqual(onBoss, BossMostOfGun, $"{difficulty.name} L{level + 1}: x{onBoss:0.00} of the Gun Turret per coin on a boss");
                    Assert.LessOrEqual(underCannon, BossMostOfCannon, $"{difficulty.name} L{level + 1}: x{underCannon:0.00} of the Cannon per coin on a boss");
                }
            });
        }

        [Test]
        public void ABeamKillsAnEnergyBodyInFewerPulses()
        {
            var laser = Load<TowerDefinition>("Towers/Laser");
            var wisp = Load<EnemyDefinition>("Enemies/Wisp");
            float pulse = laser.GetLevel(0).Damage;
            int plain = Pulses(new Health(wisp.MaxHealth, wisp.Armor), pulse * laser.Profile.Against(TargetLayers.Ground, CreatureFamily.None), laser.Profile.Armour);
            int energy = Pulses(new Health(wisp.MaxHealth, wisp.Armor), pulse * laser.Profile.Against(wisp.Layer, wisp.Family), laser.Profile.Armour);
            Assert.AreEqual(Mathf.CeilToInt(wisp.MaxHealth / (pulse * laser.Profile.Against(CreatureFamily.Energy))), energy);
            Assert.Less(energy, plain, $"{plain} pulses on an ordinary body of a Wisp's health, {energy} on the Wisp");
        }

        private static int Pulses(Health health, float hit, float armourFactor)
        {
            int pulses = 0;
            while (!health.IsDead && pulses < 10000)
            {
                health.TakeDamage(hit, armourFactor);
                pulses++;
            }

            return pulses;
        }

        // Each difficulty with its Skitter as the stored waves send it (the mean over every wave, by head count).
        private static void ForEachDifficulty(System.Action<Game.Levels.DifficultyProfile, StreamDuel.Stream> check)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath);
            var skitter = Load<EnemyDefinition>("Enemies/Skitter");
            foreach (Game.Levels.DifficultyProfile difficulty in catalog.Difficulties)
            {
                float scale = StreamDuel.MeanHealthScale(catalog, difficulty, skitter);
                Assert.Greater(scale, 0f, difficulty.name + " sends no Skitter");
                check(difficulty, StreamDuel.Stream.Of(skitter, scale, 1f, StreamBodies));
            }
        }

        private static float Scale(Game.Levels.DifficultyProfile difficulty, EnemyDefinition enemy) =>
            StreamDuel.MeanHealthScale(AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>(CatalogPath), difficulty, enemy);

        private static StreamDuel.Stream Swarm(StreamDuel.Stream swarm, float spacing)
        {
            swarm.Spacing = spacing;
            return swarm;
        }

        // Bar taken off per second something was in reach, per coin of the tower's whole price at that level.
        private static float PerCoin(TowerDefinition tower, int level, StreamDuel.Stream stream) =>
            StreamDuel.Dps(tower, level, stream, StreamDuel.GameStep) / StreamDuel.Coins(tower, level);

        private sealed class Dummy : ITargetable, ISourcedTarget, IFamilyMember
        {
            public Dummy(TargetLayers layer, CreatureFamily family)
            {
                Layer = layer;
                Family = family;
            }

            public Vector3 Position => Vector3.zero;
            public TargetLayers Layer { get; }
            public CreatureFamily Family { get; }
            public float PathProgress => 0f;
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

        private sealed class Plain : ITargetable
        {
            public Vector3 Position => Vector3.zero;
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
