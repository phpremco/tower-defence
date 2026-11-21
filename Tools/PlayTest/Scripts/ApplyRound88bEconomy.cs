using System.Text;
using Game.Balance;
using Game.Enemies;
using UnityEditor;

/// <summary>
/// Round 88b without the full generator: every boss's kill money on the rule again after BalanceRules.BossRewardFactor
/// moved (3 -> 1.5). A boss whose stored reward is not the rule's at any factor this round knew (3 or 1.5) keeps its
/// number and is reported. Rewards are in the calibration's input hash: re-stamp after it. Safe to run again.
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88bEconomy.cs ApplyRound88bEconomy.Run
/// </summary>
public static class ApplyRound88bEconomy
{
    private const float FactorBefore = 3f;

    public static string Run()
    {
        BalanceRules now = Game.Editor.BalanceSheet.Rules;
        BalanceRules before = now.Copy();
        before.BossRewardFactor = FactorBefore;
        var report = new StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (enemy == null || !enemy.IsBoss)
            {
                continue;
            }

            var stats = new EnemyStats
            {
                Id = enemy.name,
                Layer = (int)enemy.Layer,
                Flies = enemy.Flies,
                Health = enemy.MaxHealth,
                Armor = enemy.Armor,
                Speed = enemy.Speed,
                BaseDamage = enemy.BaseDamage,
                AttackDamage = enemy.AttackDamage,
                AttackInterval = enemy.AttackInterval,
                BlockadeDamage = enemy.BlockadeDamage,
                IsBoss = true,
            };
            int rule = Threat.Reward(stats, now);
            // A level boss (no staging of its own) is always on the rule; a continent boss keeps the number its stage gave it.
            bool known = enemy.Boss == null || enemy.Reward == rule || enemy.Reward == Threat.Reward(stats, before);
            report.Append(enemy.name).Append(": ").Append(enemy.Reward).Append(" -> ").Append(known ? rule : enemy.Reward).Append(known ? string.Empty : " (kept: not on the rule)").Append('\n');
            if (known && enemy.Reward != rule)
            {
                var so = new SerializedObject(enemy);
                so.FindProperty("_reward").intValue = rule;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }
        }

        AssetDatabase.SaveAssets();
        return report.ToString();
    }
}
