using System.Text;
using Game.Enemies;
using UnityEditor;
using UnityEngine;

/// <summary>Round 88b: the bones of the three level bosses that gained a volley, to name a beam socket for each. Reads assets only.</summary>
public static class Round88bBones
{
    public static string Run()
    {
        var report = new StringBuilder();
        foreach (string id in new[] { "BroodMother", "CoreWisp", "PackAlpha" })
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            report.Append(id).Append(" art ").Append(enemy.Art != null ? AssetDatabase.GetAssetPath(enemy.Art) : "none").Append(": ");
            if (enemy.Art != null && enemy.Art.Model != null)
            {
                foreach (Transform bone in enemy.Art.Model.GetComponentsInChildren<Transform>(true))
                {
                    report.Append(bone.name).Append(' ');
                }
            }

            report.Append('\n');
        }

        return report.ToString();
    }
}
