using System.Text;
using Game.Balance;
using Game.Enemies;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 88b without the full generator: BalanceRules.BossArmourShare came down (1 -> 0.5), and the armour stage of
/// round 85 only ever raises a pool (BossArmourFor takes the larger of the boss's own and the rule's). Every boss whose
/// pool stands on the old share (armour = body) gets the new share of its body; a boss with another pool keeps it and
/// is reported. Armour is in the calibration's hash and in the kill money: run ApplyRound88bEconomy, re-stamp, solve and
/// the finale stage after it. Safe to run again.
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88bArmour.cs ApplyRound88bArmour.Run
/// </summary>
public static class ApplyRound88bArmour
{
    private const float ShareBefore = 1f;

    public static string Run()
    {
        BalanceRules rules = Game.Editor.BalanceSheet.Rules;
        var report = new StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (enemy == null || !enemy.IsBoss)
            {
                continue;
            }

            float now = Mathf.Round(enemy.MaxHealth * rules.BossArmourShare);
            bool onOldShare = Mathf.Approximately(enemy.Armor, Mathf.Round(enemy.MaxHealth * ShareBefore));
            bool onNewShare = Mathf.Approximately(enemy.Armor, now);
            report.Append(enemy.name).Append(": body ").Append(enemy.MaxHealth).Append(", armour ").Append(enemy.Armor).Append(" -> ")
                .Append(onOldShare || onNewShare ? now : enemy.Armor).Append(onOldShare || onNewShare ? string.Empty : " (kept: its own pool)").Append('\n');
            if (onOldShare && !onNewShare)
            {
                var so = new SerializedObject(enemy);
                so.FindProperty("_armor").floatValue = now;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(enemy);
            }
        }

        AssetDatabase.SaveAssets();
        return report.ToString();
    }
}
