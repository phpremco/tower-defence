using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Writes the round-12 body widths (the same numbers as ProjectGenerator.Bestiary) into the existing enemy assets, without
/// regenerating anything else: half the body across its heading, and whether the body lies on the ground (the slugs).
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBodyWidths.cs --entry ApplyBodyWidths.Run --timeout_ms 120000
/// </summary>
public static class ApplyBodyWidths
{
    private static readonly Dictionary<string, (float Half, bool Ground)> Widths = new Dictionary<string, (float, bool)>
    {
        { "Oozer", (0.55f, true) },
        { "SlimeMonarch", (0.95f, true) },
        { "Cragback", (0.65f, false) },
        { "Bulwark", (0.9f, false) },
        { "Roller", (0.6f, false) },
        { "PackAlpha", (1.2f, false) },
        { "BroodMother", (1.3f, false) },
        { "RollerKing", (1.4f, false) },
        { "AcidEmpress", (1.6f, false) },
        { "CrystalWarlord", (1.8f, false) },
        { "HiveQueen", (2.2f, false) },
    };

    public static string Run()
    {
        string report = string.Empty;
        foreach (KeyValuePair<string, (float Half, bool Ground)> entry in Widths)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>($"Assets/_Project/Data/Enemies/{entry.Key}.asset");
            if (enemy == null)
            {
                report += $"{entry.Key}: missing; ";
                continue;
            }

            var so = new SerializedObject(enemy);
            so.FindProperty("_bodyHalfWidth").floatValue = entry.Value.Half;
            so.FindProperty("_groundBody").boolValue = entry.Value.Ground;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            report += $"{entry.Key} {enemy.BodyHalfWidth}{(enemy.GroundBody ? " ground" : string.Empty)}; ";
        }

        AssetDatabase.SaveAssets();
        return report;
    }
}
