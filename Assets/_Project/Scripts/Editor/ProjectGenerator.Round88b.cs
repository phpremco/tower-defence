using System.Text;
using Game.Balance;
using Game.Core;
using Game.Enemies;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 88b, the tower threat (Remco: "my towers are never damaged because I place them between the roads"). Three
    /// rules, all data: an attack style reaches a set distance to the guns beside the road (<see cref="ReachRules"/>, in
    /// the combat rules asset); the common heavy walker bites what it passes (<see cref="CragbackAttackDamage"/>); and
    /// every level boss throws the same volley at the nearest few guns (<see cref="BossVolley"/>), which leaves gates, the
    /// base and the hero to its own blows. The generator's "Combat rules" and "Bestiary" steps write all of it;
    /// <see cref="ApplyTowerThreat"/> is the targeted step for a project the full generator is never run on.
    /// </summary>
    public static partial class ProjectGenerator
    {
        /// <summary>
        /// How far each attack style carries to a structure beside the road, in cells. A style without a row is melee and
        /// reaches as far as the creature's own attack range (1.6 to 2.5 cells): the front row only.
        /// </summary>
        public static readonly ReachRule[] ReachRules =
        {
            new ReachRule
            {
                Style = nameof(AttackStyle.Slam), Cells = 3.2f,
                Reason = "A slam shakes the ground: the blow carries a cell past the body, to the second row beside the road.",
            },
            new ReachRule
            {
                Style = nameof(AttackStyle.Sting), Cells = 4f,
                Reason = "A sting sprays its venom in an arc, as far as a short gun shoots.",
            },
            new ReachRule
            {
                Style = nameof(AttackStyle.Bolt), Cells = 4.5f,
                Reason = "A spat bolt flies as far as a gun's shot: a tower between two roads is in reach of both.",
            },
        };

        /// <summary>Cells every boss of a level must hurt a tower from (CombatRules.BossReach); the tests hold the data to it.</summary>
        public const float BossReach = 4f;

        /// <summary>
        /// What the Cragback's slam does to a structure in its reach. It is the one heavy walker of the first levels, so a
        /// tower has something to fear before the boss; the swarm kinds (Skitter, Prowler, Roller) keep leaving towers alone.
        /// </summary>
        public const float CragbackAttackDamage = 1.5f;

        // The level bosses' volley: one bolt at each of the nearest few guns in reach, the same for every boss, so a held
        // boss costs the same whichever it is and a fast one costs less in passing. Sized on paper (Round88bThreatLab): the
        // slowest boss passing a tower three cells off its road takes about a third of a level-1 Cannon on Medium when that
        // tower is among its three nearest guns all the way, and half of that in a defence of six guns.
        public const float BossVolleyRange = 5f;
        public const int BossVolleyTargets = 3;
        public const float BossVolleyInterval = 3f;
        public const float BossVolleyFirstDelay = 3f;
        // Round 88f: 14 -> 8. Three bolts every three seconds is then what one repair drone mends a second (8), so a boss
        // held at a gate is answered by a crew or a repair instead of wrecking the three guns beside it in under a minute
        // (the volleys were two thirds of all tower wear in the bots' Shard Ridge runs).
        public const float BossVolleyDamage = 8f;

        /// <summary>The level bosses that got the volley this round, with its asset name and colour.</summary>
        private static readonly (string Boss, string Ability, Color Color)[] BossVolleys =
        {
            ("BroodMother", "VenomSpit", new Color(0.75f, 1f, 0.25f)),
            ("CoreWisp", "ArcDischarge", new Color(0.85f, 0.9f, 1f)),
            ("PackAlpha", "SpineVolley", new Color(1f, 0.5f, 0.15f)),
        };

        /// <summary>A level boss's volley at the guns beside its road (never at a gate, the base or the hero).</summary>
        private static BossAbilityDefinition BossVolley(string id)
        {
            Color color = Color.white;
            foreach ((string _, string listedId, Color listed) in BossVolleys)
            {
                if (listedId == id)
                {
                    color = listed;
                }
            }

            BossAbilityDefinition ability = CreateAbility(id, BossAbilityKind.Volley, BossVolleyInterval, BossVolleyFirstDelay, BossVolleyDamage, 1f, 1f, BossVolleyRange, null, 1, color, targets: BossVolleyTargets);
            var so = new SerializedObject(ability);
            so.FindProperty("_sparesBlockers").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static void WriteReach(SerializedObject rules)
        {
            SerializedProperty reach = rules.FindProperty("_reach");
            reach.arraySize = ReachRules.Length;
            for (int i = 0; i < ReachRules.Length; i++)
            {
                SerializedProperty row = reach.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Style").stringValue = ReachRules[i].Style;
                row.FindPropertyRelative("Cells").floatValue = ReachRules[i].Cells;
                row.FindPropertyRelative("Reason").stringValue = ReachRules[i].Reason;
            }

            rules.FindProperty("_bossReach").floatValue = BossReach;
        }

        private static CombatRules RulesAsset()
        {
            CombatRules rules = AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath);
            return rules != null ? rules : CreateCombatRules();
        }

        /// <summary>
        /// The targeted step: the reach table in the rules asset, the rules on every creature, the Cragback's slam with its
        /// kill money (threat prices a creature that hurts structures), and the three volleys on their bosses (appended,
        /// so whatever else a boss carries stays). Safe to run again.
        /// </summary>
        public static string ApplyTowerThreat()
        {
            var report = new StringBuilder();
            CombatRules rules = CreateCombatRules();
            report.Append("Reach:");
            foreach (ReachRule rule in rules.Reach)
            {
                report.Append(System.FormattableString.Invariant($" {rule.Style} {rule.Cells:0.##};"));
            }

            report.Append(System.FormattableString.Invariant($" boss reach {rules.BossReach:0.##}\n"));
            int stamped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { $"{DataRoot}/Enemies" }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(enemy);
                so.FindProperty("_rules").objectReferenceValue = rules;
                if (so.ApplyModifiedPropertiesWithoutUndo())
                {
                    EditorUtility.SetDirty(enemy);
                    stamped++;
                }
            }

            report.Append($"rules handed to {stamped} creatures\n");
            report.Append(ApplyCragbackSlam());
            foreach ((string boss, string id, Color _) in BossVolleys)
            {
                report.Append(ApplyBossVolley(boss, id));
            }

            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static string ApplyCragbackSlam()
        {
            var cragback = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/Cragback.asset");
            if (cragback == null)
            {
                return "MISSING Cragback\n";
            }

            // Kill money follows threat as the generator writes it; a reward that was not on that formula keeps its number.
            BalanceRules sheet = BalanceSheet.Rules;
            bool onFormula = Threat.Reward(Priced(cragback, cragback.AttackDamage), sheet) == cragback.Reward;
            int reward = onFormula ? Threat.Reward(Priced(cragback, CragbackAttackDamage), sheet) : cragback.Reward;
            string line = System.FormattableString.Invariant(
                $"Cragback: attack {cragback.AttackDamage:0.#} -> {CragbackAttackDamage:0.#}, reward {cragback.Reward} -> {reward}{(onFormula ? string.Empty : " (kept: it was not on the formula)")}\n");
            var so = new SerializedObject(cragback);
            so.FindProperty("_attackDamage").floatValue = CragbackAttackDamage;
            so.FindProperty("_reward").intValue = reward;
            if (so.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(cragback);
            }

            return line;
        }

        private static EnemyStats Priced(EnemyDefinition enemy, float attackDamage) => new EnemyStats
        {
            Id = enemy.name,
            Layer = (int)enemy.Layer,
            Flies = enemy.Flies,
            Health = enemy.MaxHealth,
            Armor = enemy.Armor,
            Speed = enemy.Speed,
            BaseDamage = enemy.BaseDamage,
            AttackDamage = attackDamage,
            AttackInterval = enemy.AttackInterval,
            BlockadeDamage = enemy.BlockadeDamage,
            IsBoss = enemy.IsBoss,
        };

        private static string ApplyBossVolley(string bossId, string abilityId)
        {
            var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/{bossId}.asset");
            if (boss == null)
            {
                return $"MISSING {bossId}\n";
            }

            BossAbilityDefinition volley = BossVolley(abilityId);
            // The asset carries the key of its name once the table knows the text (en.data.txt), as after a full generator run.
            var named = new SerializedObject(volley);
            SerializedProperty displayName = named.FindProperty("_displayName");
            displayName.stringValue = LocalisationTables.KeyOrText(volley, "_displayName", displayName.stringValue);
            named.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(boss);
            SerializedProperty abilities = so.FindProperty("_abilities");
            bool carried = false;
            for (int i = 0; i < abilities.arraySize; i++)
            {
                carried |= abilities.GetArrayElementAtIndex(i).objectReferenceValue == volley;
            }

            if (!carried)
            {
                abilities.arraySize++;
                abilities.GetArrayElementAtIndex(abilities.arraySize - 1).objectReferenceValue = volley;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(boss);
            }

            return System.FormattableString.Invariant(
                $"{bossId}: {abilityId} {volley.Damage:0.#} x{volley.TargetCount} every {volley.Interval:0.#} s within {volley.Range:0.#} cells{(carried ? " (already carried)" : " (added)")}\n");
        }
    }
}
