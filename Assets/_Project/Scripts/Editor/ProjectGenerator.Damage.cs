using System.Collections.Generic;
using System.Text;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the combat rules (round 67; the armour rule of round 64 is its first column). One table for
    /// every damage source: a weapon is listed once with what its damage is made of, the rules asset
    /// (<see cref="CombatRules"/>) says what each damage type is worth against armour, in the air and on a creature
    /// family (round 87), and the few weapons that differ from their type are named exceptions with a reason. No weapon carries a hand-typed factor, so a new
    /// weapon cannot forget one (the attack drone's shots ignored armour until round 64; the base turret, the hero's
    /// blaster and the attack drone hit flyers in full until round 67 while the Gun Turret, the same bullets, did not).
    /// </summary>
    public static partial class ProjectGenerator
    {
        /// <summary>Asset path under the data root, without the extension, to the weapon's damage type.</summary>
        public static readonly IReadOnlyDictionary<string, DamageType> WeaponDamageTypes = new Dictionary<string, DamageType>
        {
            { "Towers/PulseRifle", DamageType.Bullet },
            { "Towers/BaseTurret", DamageType.Bullet },
            { "Towers/Blockade", DamageType.Bullet },
            { "Towers/Cannon", DamageType.Explosive },
            { "Towers/Artillery", DamageType.Explosive },
            // The Flak's shells burst (its type), but against armour it is a named exception: see CombatExceptions.
            { "Towers/Rocket", DamageType.Explosive },
            { "Towers/Laser", DamageType.Beam },
            { "Towers/Cryo", DamageType.Beam },
            { "Towers/CryoBeam", DamageType.Beam },
            { "Towers/Tesla", DamageType.Arc },
            { "Towers/StormRifle", DamageType.Arc },
            { "Towers/Railgun", DamageType.Piercing },
            { "Hero/Weapon_Blaster", DamageType.Bullet },
            { "Hero/Weapon_EnergySword", DamageType.Blade },
            { "Hero/Abilities/Ability_Shockwave", DamageType.Explosive },
            { "Drones/AttackDrone", DamageType.Bullet },
            { "PathObjects/Mine", DamageType.Explosive },
            { "PathObjects/ExplosiveBarrel", DamageType.Explosive },
            { "PathObjects/LavaBall", DamageType.Burn },
            { "PathObjects/GroundDrone", DamageType.Ram },
        };

        // The rule table: damage type against the two traits a factor exists for. Armour (round 64, Remco: "reduced damage
        // from non-exploding things"; light fire 0.2 and the middle 0.5 are his numbers of 2026-11-09, the blade's 0.5 his
        // of 2026-11-15): a pool in front of the body that a weapon works on at this share. Air (round 61, "air needs the
        // sky gun"): the share that lands on a creature only the sky can be aimed at. Zero in the air column means the
        // damage never reaches the sky; no weapon of that type may aim up.
        private static readonly DamageRule[] DamageRules =
        {
            Rule(DamageType.Explosive, 1f, 1f, "A blast strips armour point for point and bursts among flyers. (A shell still cannot be aimed up: that is the Cannon's targets, not its damage.)"),
            Rule(DamageType.Bullet, 0.2f, 0.2f, "Light fire only chips armour, and a gun not made for the sky lands a fifth up there: enough to finish a straggler, never an air defence."),
            // Round 87 (Remco: the Laser is "weak against armour and against single big targets/bosses"): a tenth, half the bullet's share.
            Rule(DamageType.Beam, 0.1f, 0.2f, "A beam glances off plates, and a pulse this small barely marks them; it spreads out over the distance to a flyer."),
            Rule(DamageType.Blade, 0.5f, 0f, "A blade cuts armour at half. It never reaches the sky."),
            Rule(DamageType.Arc, 0.2f, 0.4f, "Lightning runs over plates instead of through them, like the other light fire, and thins out on its way up."),
            Rule(DamageType.Burn, 0.5f, 0f, "Fire heats armour at half. It burns on the ground."),
            Rule(DamageType.Ram, 0.5f, 0f, "A ram dents armour at half. It drives on the ground."),
            Rule(DamageType.Piercing, 1.5f, 0.5f, "A slug is made for armour: it works on plates better than on the body. One slow shot is a poor way to hunt flyers."),
        };

        // The family table (round 87, Remco: "a bonus against certain enemies, e.g. energy enemies"): what a damage type is
        // worth on what a creature is made of. Only the pairs that differ from one are listed; the factor multiplies the
        // hit before the armour and air shares, for every weapon of the type (the Cryo and the Cryo Beam are beams too, and
        // that is meant: the rule is the beam's, not the Laser's). A new pairing is a line here and nothing else.
        private static readonly FamilyRule[] FamilyRules =
        {
            new FamilyRule
            {
                Type = DamageType.Beam, Family = CreatureFamily.Energy, Factor = 1.5f,
                Reason = "A beam feeds straight into an energy body and overloads it.",
            },
        };

        /// <summary>
        /// The creatures that are made of something the family table has a row for, by asset name; every other creature is
        /// <see cref="CreatureFamily.None"/>. Listed only where a creature's own description says so: the Wisp is a ball of
        /// energy and the Core Wisp a core of it.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, CreatureFamily> CreatureFamilies = new Dictionary<string, CreatureFamily>
        {
            { "Wisp", CreatureFamily.Energy },
            { "CoreWisp", CreatureFamily.Energy },
        };

        private static CreatureFamily FamilyOf(string creature) =>
            CreatureFamilies.TryGetValue(creature, out CreatureFamily family) ? family : CreatureFamily.None;

        // The named exceptions: the only weapons that differ from their damage type, each with its reason.
        private static readonly CombatException[] CombatExceptions =
        {
            new CombatException
            {
                Weapon = "Rocket", Trait = EnemyTraits.Armoured, Factor = 0.5f,
                Reason = "The Flak's airburst is fragments, not a shell: it is the air specialist and no anti-armour gun (Remco 2026-11-15). No armoured flyer exists today.",
            },
        };

        // The reference fight per creature trait and the margin the towers made for it must keep per coin over every other
        // source (the role tests play these; Docs/COMBAT_RULES.md shows the matrix). Air and armour are Remco's "at least
        // twice"; the size traits are held by mechanics (hit size, blast cap, unguided shells), so their margins are what
        // those mechanics give today with a little room.
        private static readonly RoleScenario[] RoleScenarios =
        {
            Scenario(EnemyTraits.Swarm, "Skitter", 12, 1.5f, true, 1.15f, "A stream of a dozen Skitters, a blast apart: every hit bigger than one of them is wasted, so light fire wins."),
            Scenario(EnemyTraits.Mob, "Oozer", 5, 0.5f, false, 1.25f, "Five Oozers queued at a gate: one blast takes three."),
            Scenario(EnemyTraits.Tough, "ShardCrawler", 1, 1f, false, 1.15f, "One Shard Crawler: a big soft body asks for light fire held on one target."),
            Scenario(EnemyTraits.Armoured, "Cragback", 3, 0.6f, false, 2f, "Three Cragbacks at a gate: light fire chips the plates, shells and slugs break them."),
            Scenario(EnemyTraits.Airborne, "Skyray", 4, 0.8f, true, 2f, "A wing of four Skyrays: only the sky guns land in full."),
            Scenario(EnemyTraits.Fast, "Roller", 1, 1f, true, 1.15f, "One Roller at speed: an unguided shell lands where it was."),
        };

        private const float SwarmHealthMost = 40f;
        private const float ToughHealthLeast = 250f;
        private const float FastSpeedLeast = 1.3f;

        // Late waves at the top tower level with every permanent upgrade bought: the reference creatures are this much tougher.
        private const float LateHealthScale = 6f;

        // Every reference fight is played at these health scales and averaged (waves run from under one to a few times a creature's book health).
        private static readonly float[] HealthLadder = { 0.75f, 1f, 1.5f, 2.25f };
        private const float HelperHorizonSeconds = 120f;
        private const int FreeHelperCoins = 100;

        // Growth (Remco 2026-11-15): after a creature trait first appears, a defence without its counter tower may hold a few
        // waves and must have lost a few waves after that. The table is Game.Balance.GrowthWindow: the wave scripts read it too.
        private static int[] GraceWaves => Game.Balance.GrowthWindow.GraceWaves;
        private static int[] FallWithinWaves => Game.Balance.GrowthWindow.FallWithinWaves;

        private static DamageRule Rule(DamageType type, float armour, float air, string reason) =>
            new DamageRule { Type = type, Armour = armour, Air = air, Reason = reason };

        private static RoleScenario Scenario(EnemyTraits trait, string enemy, int count, float spacing, bool moving, float margin, string reason) =>
            new RoleScenario { Trait = trait, Enemy = enemy, Count = count, Spacing = spacing, Moving = moving, Margin = margin, Reason = reason };

        /// <summary>Writes the rules asset from the tables above; safe to run again.</summary>
        public static CombatRules CreateCombatRules()
        {
            EnsureFolder($"{DataRoot}/Balance");
            var rules = LoadOrCreate<CombatRules>(CombatRules.AssetPath);
            var so = new SerializedObject(rules);
            SerializedProperty damage = so.FindProperty("_damage");
            damage.arraySize = DamageRules.Length;
            for (int i = 0; i < DamageRules.Length; i++)
            {
                SerializedProperty row = damage.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Type").enumValueIndex = (int)DamageRules[i].Type;
                row.FindPropertyRelative("Armour").floatValue = DamageRules[i].Armour;
                row.FindPropertyRelative("Air").floatValue = DamageRules[i].Air;
                row.FindPropertyRelative("Reason").stringValue = DamageRules[i].Reason;
            }

            SerializedProperty families = so.FindProperty("_families");
            families.arraySize = FamilyRules.Length;
            for (int i = 0; i < FamilyRules.Length; i++)
            {
                SerializedProperty row = families.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Type").enumValueIndex = (int)FamilyRules[i].Type;
                row.FindPropertyRelative("Family").enumValueIndex = (int)FamilyRules[i].Family;
                row.FindPropertyRelative("Factor").floatValue = FamilyRules[i].Factor;
                row.FindPropertyRelative("Reason").stringValue = FamilyRules[i].Reason;
            }

            SerializedProperty exceptions = so.FindProperty("_exceptions");
            exceptions.arraySize = CombatExceptions.Length;
            for (int i = 0; i < CombatExceptions.Length; i++)
            {
                SerializedProperty row = exceptions.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Weapon").stringValue = CombatExceptions[i].Weapon;
                row.FindPropertyRelative("Trait").intValue = (int)CombatExceptions[i].Trait;
                row.FindPropertyRelative("Factor").floatValue = CombatExceptions[i].Factor;
                row.FindPropertyRelative("Reason").stringValue = CombatExceptions[i].Reason;
            }

            SerializedProperty scenarios = so.FindProperty("_scenarios");
            scenarios.arraySize = RoleScenarios.Length;
            for (int i = 0; i < RoleScenarios.Length; i++)
            {
                SerializedProperty row = scenarios.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Trait").intValue = (int)RoleScenarios[i].Trait;
                row.FindPropertyRelative("Enemy").stringValue = RoleScenarios[i].Enemy;
                row.FindPropertyRelative("Count").intValue = RoleScenarios[i].Count;
                row.FindPropertyRelative("Spacing").floatValue = RoleScenarios[i].Spacing;
                row.FindPropertyRelative("Moving").boolValue = RoleScenarios[i].Moving;
                row.FindPropertyRelative("Margin").floatValue = RoleScenarios[i].Margin;
                row.FindPropertyRelative("Reason").stringValue = RoleScenarios[i].Reason;
            }

            so.FindProperty("_swarmHealth").floatValue = SwarmHealthMost;
            so.FindProperty("_toughHealth").floatValue = ToughHealthLeast;
            so.FindProperty("_fastSpeed").floatValue = FastSpeedLeast;
            so.FindProperty("_lateHealthScale").floatValue = LateHealthScale;
            SerializedProperty ladder = so.FindProperty("_healthLadder");
            ladder.arraySize = HealthLadder.Length;
            for (int i = 0; i < HealthLadder.Length; i++)
            {
                ladder.GetArrayElementAtIndex(i).floatValue = HealthLadder[i];
            }

            so.FindProperty("_helperHorizonSeconds").floatValue = HelperHorizonSeconds;
            so.FindProperty("_freeHelperCoins").intValue = FreeHelperCoins;
            WriteInts(so.FindProperty("_graceWaves"), GraceWaves);
            WriteInts(so.FindProperty("_fallWithinWaves"), FallWithinWaves);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rules);
            return rules;
        }

        private static void WriteInts(SerializedProperty array, int[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).intValue = values[i];
            }
        }

        /// <summary>
        /// Writes every creature's family from <see cref="CreatureFamilies"/> and nothing else: the targeted step, safe to
        /// run again. Returns the creatures that have one.
        /// </summary>
        public static string ApplyCreatureFamilies()
        {
            var report = new StringBuilder("Families:");
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { $"{DataRoot}/Enemies" }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                CreatureFamily family = FamilyOf(enemy.name);
                var so = new SerializedObject(enemy);
                so.FindProperty("_family").enumValueIndex = (int)family;
                if (so.ApplyModifiedPropertiesWithoutUndo())
                {
                    EditorUtility.SetDirty(enemy);
                }

                if (family != CreatureFamily.None)
                {
                    report.Append($" {enemy.name} {family};");
                }
            }

            return report.Append('\n').ToString();
        }

        /// <summary>Writes a weapon's damage type and hands it the rule table; an asset outside the list is a blast.</summary>
        private static void StampDamage(SerializedObject so, string key)
        {
            DamageType type = WeaponDamageTypes.TryGetValue(key, out DamageType listed) ? listed : DamageType.Explosive;
            so.FindProperty("_damageType").enumValueIndex = (int)type;
            CombatRules rules = AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath);
            so.FindProperty("_rules").objectReferenceValue = rules != null ? rules : CreateCombatRules();
        }

        /// <summary>
        /// Stamps every weapon asset in the list and nothing else: the targeted step, safe to run again. Returns one line
        /// per weapon with what the rules make of it.
        /// </summary>
        public static string ApplyDamageTypes()
        {
            var report = new StringBuilder();
            CombatRules rules = CreateCombatRules();
            foreach (KeyValuePair<string, DamageType> entry in WeaponDamageTypes)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{DataRoot}/{entry.Key}.asset");
                if (asset == null)
                {
                    report.Append($"MISSING {entry.Key}\n");
                    continue;
                }

                var so = new SerializedObject(asset);
                StampDamage(so, entry.Key);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                DamageProfile profile = rules.ProfileOf(entry.Value, asset.name);
                report.Append($"{entry.Key}: {entry.Value}, armour {profile.Armour:0.##}, air {profile.Air:0.##}\n");
            }

            return report.ToString();
        }
    }
}
