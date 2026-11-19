using System;
using System.Text;
using Game.Balance;
using Game.Enemies;
using UnityEditor;
using static System.FormattableString;

namespace Game.Editor
{
    /// <summary>
    /// Round 85: the boss armour rule on the creature data without the full generator. The generator writes the same
    /// (ProjectGenerator.ArmourOf, through <see cref="BalanceRules.BossArmourFor"/>), and the finale stage keeps a
    /// continent boss on the rule when it moves its health.
    /// </summary>
    public static partial class BalanceStages
    {
        /// <summary>
        /// Gives every boss creature the armour pool the rule asks (never less than its own) and the kill money that
        /// follows from it. Safe to run again: a boss already on the rule is left alone. Armour is an input of the
        /// calibration hash, so the calibration reads stale afterwards (apply-keep, Docs/COMMANDS.md "Round 83").
        /// </summary>
        public static string ApplyBossArmour()
        {
            BalanceRules rules = BalanceSheet.Rules;
            var text = new StringBuilder();
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder.TrimEnd('/') }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy == null || !enemy.IsBoss)
                {
                    continue;
                }

                float armour = rules.BossArmourFor(enemy.MaxHealth, enemy.Armor);
                if (Math.Abs(armour - enemy.Armor) < 0.5f)
                {
                    text.Append(Invariant($"  {enemy.name,-16} body {enemy.MaxHealth:0}, armour {enemy.Armor:0}: on the rule\n"));
                    continue;
                }

                // Kill money follows threat as the generator writes it; a reward that was not on that formula before
                // (a boss whose health a later stage moved) keeps its number.
                bool onFormula = Threat.Reward(Priced(enemy, enemy.Armor), rules) == enemy.Reward;
                int reward = onFormula ? Threat.Reward(Priced(enemy, armour), rules) : enemy.Reward;
                text.Append(Invariant($"  {enemy.name,-16} body {enemy.MaxHealth:0}, armour {enemy.Armor:0} -> {armour:0}, reward {enemy.Reward} -> {reward}{(onFormula ? string.Empty : " (kept: it was not on the formula)")}\n"));
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("_armor").floatValue = armour;
                serialized.FindProperty("_reward").intValue = reward;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
                changed++;
            }

            if (changed > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return Invariant($"boss armour (share {rules.BossArmourShare:0.##} of the body at least): {changed} creatures changed\n{text}");
        }

        // The creature as the generator prices it (ProjectGenerator.StatsOf): its book numbers, no boss mechanics.
        private static EnemyStats Priced(EnemyDefinition enemy, float armour) => new EnemyStats
        {
            Id = enemy.name,
            Layer = (int)enemy.Layer,
            Flies = enemy.Flies,
            Health = enemy.MaxHealth,
            Armor = armour,
            Speed = enemy.Speed,
            BaseDamage = enemy.BaseDamage,
            AttackDamage = enemy.AttackDamage,
            AttackInterval = enemy.AttackInterval,
            BlockadeDamage = enemy.BlockadeDamage,
            IsBoss = enemy.IsBoss,
        };

        [MenuItem("Tools/Generate/Balance/12 Boss armour (the rule's share on every boss)")]
        private static void MenuApplyBossArmour() => UnityEngine.Debug.Log("[Balance] " + ApplyBossArmour());
    }
}
