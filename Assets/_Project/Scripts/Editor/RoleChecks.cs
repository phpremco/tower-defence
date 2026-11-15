using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Towers;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.Editor
{
    /// <summary>
    /// The role rules as checks (round 67): per creature trait, the towers made for it against every other damage
    /// source, per coin, on the reference fight. The EditMode tests assert the failure lists are empty, the report prints
    /// the same numbers as the matrix on the rules page, so the page can never show a different game than the tests hold.
    /// </summary>
    public static class RoleChecks
    {
        public const string ReportPath = "Tools/PlayTest/r67_role_matrix.txt";
        private const string UpgradeFolder = "Assets/_Project/Data/Upgrades";

        /// <summary>A moment of a level the roles must hold at.</summary>
        public readonly struct Moment
        {
            public Moment(string name, int level, bool upgraded)
            {
                Name = name;
                Level = level;
                Upgraded = upgraded;
            }

            public string Name { get; }

            /// <summary>The towers' level index.</summary>
            public int Level { get; }

            /// <summary>True with every permanent tower upgrade bought (all ten tiers) against late-wave creatures.</summary>
            public bool Upgraded { get; }
        }

        public static readonly Moment[] Moments =
        {
            new Moment("level 1", 0, false),
            new Moment("level 3", TowerDefinition.LevelCount - 1, false),
            new Moment("level 3, every permanent upgrade, late waves", TowerDefinition.LevelCount - 1, true),
        };

        /// <summary>What a tower's permanent upgrades give at their top tier (all ten): damage and fire rate.</summary>
        public static RoleMatrix.Boost TopUpgrades(string towerId)
        {
            return new RoleMatrix.Boost(1f + Bonus(towerId + "_Damage"), 1f + Bonus(towerId + "_FireRate"));
        }

        private static float Bonus(string upgradeId)
        {
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>($"{UpgradeFolder}/{upgradeId}.asset");
            return upgrade != null ? upgrade.BonusPerLevel * upgrade.MaxLevel : 0f;
        }

        /// <summary>The group cleared per second by a hundred coins of the source at a moment.</summary>
        public static float Rate(RoleMatrix.Source source, RoleScenario scenario, EnemyDefinition enemy, CombatRules rules, Moment moment)
        {
            RoleMatrix.Boost boost = moment.Upgraded && source.Tower != null ? TopUpgrades(source.Id) : RoleMatrix.Boost.None;
            float late = moment.Upgraded ? rules.LateHealthScale : 1f;
            float sum = 0f;
            // The mean over the rules' health ladder: one health alone lets a hit that just kills (or just does not) decide a role.
            foreach (float scale in rules.HealthLadder)
            {
                sum += RoleMatrix.ClearRate(source, scenario, enemy, scale * late, boost);
            }

            return rules.HealthLadder.Length > 0 ? sum / rules.HealthLadder.Length : 0f;
        }

        /// <summary>The standard tower made for a trait that clears its reference fight fastest per coin at a moment, or null.</summary>
        public static RoleMatrix.Source BestOnRole(List<RoleMatrix.Source> towers, RoleScenario scenario, EnemyDefinition enemy, CombatRules rules, Moment moment)
        {
            RoleMatrix.Source best = null;
            float bestRate = 0f;
            foreach (RoleMatrix.Source tower in towers)
            {
                if (tower.Kind != RoleMatrix.SourceKind.Tower || !tower.Tower.Answers(scenario.Trait))
                {
                    continue;
                }

                float rate = Rate(tower, scenario, enemy, rules, moment);
                if (best == null || rate > bestRate)
                {
                    best = tower;
                    bestRate = rate;
                }
            }

            return best;
        }

        /// <summary>
        /// Every case where a tower without the role for a trait comes within the trait's margin of a tower made for it,
        /// per coin, at a moment. Empty when the roles hold.
        /// </summary>
        public static List<string> TowerFailures(CombatRules rules, Moment moment)
        {
            var failures = new List<string>();
            List<RoleMatrix.Source> towers = RoleMatrix.Towers(rules, moment.Level);
            foreach (RoleScenario scenario in rules.Scenarios)
            {
                EnemyDefinition enemy = RoleMatrix.Enemy(scenario.Enemy);
                if (enemy == null)
                {
                    failures.Add($"{scenario.Trait}: no creature '{scenario.Enemy}' for its reference fight");
                    continue;
                }

                RoleMatrix.Source best = BestOnRole(towers, scenario, enemy, rules, moment);
                if (best == null)
                {
                    failures.Add($"{scenario.Trait}: no tower is made for it");
                    continue;
                }

                float on = Rate(best, scenario, enemy, rules, moment);
                foreach (RoleMatrix.Source other in towers)
                {
                    if (other.Kind == RoleMatrix.SourceKind.Structure || other.Tower.Answers(scenario.Trait))
                    {
                        continue;
                    }

                    float off = Rate(other, scenario, enemy, rules, moment);
                    if (on < off * scenario.Margin)
                    {
                        failures.Add(Invariant($"{scenario.Trait} at {moment.Name}: {other.Name} (off-role) clears {off:0.000} per 100 coins a second, {best.Name} (the best tower made for it) {on:0.000}: {(off > 0f ? on / off : 0f):0.00}x, the rules ask {scenario.Margin:0.00}x"));
                    }
                }
            }

            return failures;
        }

        /// <summary>
        /// Every helper that replaces a role: one that does more per coin against a trait than the best tower made for it.
        /// A free helper (the hero's weapons, the base's turret) is weighed as the rules' reference coins, a gate's gun and
        /// the base's turret at their top level against top-level towers, the hero and the attack drone against level-1
        /// towers, and a path object, spent after one use, over the helper horizon of a tower's fire.
        /// </summary>
        public static List<string> HelperFailures(CombatRules rules)
        {
            var failures = new List<string>();
            var first = new Moment("level 1", 0, false);
            var top = new Moment("level 3", TowerDefinition.LevelCount - 1, false);
            List<RoleMatrix.Source> towers = RoleMatrix.Towers(rules, 0);
            List<RoleMatrix.Source> topTowers = RoleMatrix.Towers(rules, top.Level);
            var helpers = new List<(RoleMatrix.Source Source, Moment At, List<RoleMatrix.Source> Against)>();
            foreach (RoleMatrix.Source helper in RoleMatrix.Helpers(rules))
            {
                helpers.Add((helper, first, towers));
            }

            // A gate's gun arrives with its first refit: the structures are weighed at their top level, against top-level towers.
            foreach (RoleMatrix.Source structure in topTowers.FindAll(t => t.Kind == RoleMatrix.SourceKind.Structure))
            {
                helpers.Add((structure, top, topTowers));
            }

            foreach (RoleScenario scenario in rules.Scenarios)
            {
                EnemyDefinition enemy = RoleMatrix.Enemy(scenario.Enemy);
                if (enemy == null)
                {
                    continue;
                }

                foreach ((RoleMatrix.Source helper, Moment at, List<RoleMatrix.Source> against) in helpers)
                {
                    RoleMatrix.Source best = BestOnRole(against, scenario, enemy, rules, at);
                    if (best == null)
                    {
                        continue;
                    }

                    // A path object is spent after one use, so it is weighed over the horizon; everything else fires on and is weighed by its rate.
                    float help = helper.SingleUse ? RoleMatrix.GroupsOverHorizon(helper, scenario, enemy, rules) : Rate(helper, scenario, enemy, rules, at);
                    float on = helper.SingleUse ? RoleMatrix.GroupsOverHorizon(best, scenario, enemy, rules) : Rate(best, scenario, enemy, rules, at);
                    if (help > on)
                    {
                        failures.Add(Invariant($"{scenario.Trait}: {helper.Name} (weighed as {helper.Coins} coins) does {help:0.000} per 100 coins, more than the {best.Name} L{best.Level + 1} made for it ({on:0.000})"));
                    }
                }
            }

            return failures;
        }

        /// <summary>The matrix and both failure lists as text (Tools/PlayTest/r67_role_matrix.txt); the tables are Markdown, for the rules page.</summary>
        public static string Report(CombatRules rules)
        {
            var text = new StringBuilder();
            text.Append("Role matrix (round 67). Seconds for 100 coins of a source to clear the reference group of a trait; '-' cannot. ");
            text.Append("**Bold**: the source is made for the trait.\n\n");
            foreach (Moment moment in Moments)
            {
                text.Append("### Towers at ").Append(moment.Name).Append("\n\n| Tower | Coins |");
                foreach (RoleScenario scenario in rules.Scenarios)
                {
                    text.Append(' ').Append(scenario.Trait).Append(" |");
                }

                text.Append("\n| --- | --- |");
                foreach (RoleScenario _ in rules.Scenarios)
                {
                    text.Append(" --- |");
                }

                text.Append('\n');
                foreach (RoleMatrix.Source tower in RoleMatrix.Towers(rules, moment.Level))
                {
                    text.Append("| ").Append(tower.Name).Append(" | ").Append(tower.Coins).Append(" |");
                    foreach (RoleScenario scenario in rules.Scenarios)
                    {
                        EnemyDefinition enemy = RoleMatrix.Enemy(scenario.Enemy);
                        float rate = enemy != null ? Rate(tower, scenario, enemy, rules, moment) : 0f;
                        bool made = tower.Kind != RoleMatrix.SourceKind.Structure && tower.Tower.Answers(scenario.Trait);
                        string cell = rate > 0f ? Invariant($"{1f / rate:0.0}") : "-";
                        text.Append(' ').Append(made ? "**" + cell + "**" : cell).Append(" |");
                    }

                    text.Append('\n');
                }

                text.Append('\n');
                List<string> failures = TowerFailures(rules, moment);
                text.Append(failures.Count == 0 ? "Roles hold.\n\n" : "ROLE BREAKS:\n- " + string.Join("\n- ", failures) + "\n\n");
            }

            text.Append(Invariant($"### Helpers: groups cleared per 100 coins over {rules.HelperHorizonSeconds:0} s (a free helper counts as {rules.FreeHelperCoins} coins; a path object is spent after one use)\n\n| Source | Coins |"));
            foreach (RoleScenario scenario in rules.Scenarios)
            {
                text.Append(' ').Append(scenario.Trait).Append(" |");
            }

            text.Append("\n| --- | --- |");
            foreach (RoleScenario _ in rules.Scenarios)
            {
                text.Append(" --- |");
            }

            text.Append('\n');
            var rows = new List<RoleMatrix.Source>(RoleMatrix.Towers(rules, 0).FindAll(t => t.Kind == RoleMatrix.SourceKind.Tower));
            rows.AddRange(RoleMatrix.Towers(rules, TowerDefinition.LevelCount - 1).FindAll(t => t.Kind == RoleMatrix.SourceKind.Structure));
            rows.AddRange(RoleMatrix.Helpers(rules));
            foreach (RoleMatrix.Source row in rows)
            {
                text.Append("| ").Append(row.Name).Append(row.Kind == RoleMatrix.SourceKind.Structure ? " (level 3)" : row.Kind == RoleMatrix.SourceKind.Tower ? " (level 1)" : string.Empty).Append(" | ").Append(row.Coins).Append(" |");
                foreach (RoleScenario scenario in rules.Scenarios)
                {
                    EnemyDefinition enemy = RoleMatrix.Enemy(scenario.Enemy);
                    float groups = enemy != null ? RoleMatrix.GroupsOverHorizon(row, scenario, enemy, rules) : 0f;
                    bool made = row.Kind == RoleMatrix.SourceKind.Tower && row.Tower.Answers(scenario.Trait);
                    string cell = groups > 0f ? Invariant($"{groups:0.00}") : "-";
                    text.Append(' ').Append(made ? "**" + cell + "**" : cell).Append(" |");
                }

                text.Append('\n');
            }

            List<string> helperFailures = HelperFailures(rules);
            text.Append(helperFailures.Count == 0 ? "\nNo helper replaces a role.\n" : "\nHELPER BREAKS:\n- " + string.Join("\n- ", helperFailures) + "\n");
            return text.ToString();
        }

        /// <summary>Every creature in the data with the traits its numbers give it, as a Markdown table.</summary>
        public static string Bestiary(CombatRules rules)
        {
            var text = new StringBuilder("| Creature | Body | Armour | Speed | Traits |\n| --- | --- | --- | --- | --- |\n");
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                text.Append(Invariant($"| {enemy.DisplayName}{(enemy.IsBoss ? " (boss)" : string.Empty)} | {enemy.MaxHealth:0} | {enemy.Armor:0} | {enemy.AverageSpeed:0.0#} | {RoleMatrix.TraitsOf(rules, enemy)} |\n"));
            }

            return text.ToString();
        }
    }
}
