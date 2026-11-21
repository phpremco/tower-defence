using System.Text;
using Game.Core;
using UnityEditor;

/// <summary>
/// Round 88b without the full generator: the beam socket of the three level bosses that gained a volley (the bone the
/// bolts leave from), on the plain and the corrupted art row, as BlenderArtCatalog names them. Safe to run again.
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88bSockets.cs ApplyRound88bSockets.Run
/// </summary>
public static class ApplyRound88bSockets
{
    public static string Run()
    {
        var report = new StringBuilder();
        Set("BroodMother", "Head", report);
        Set("PackAlpha", "Head", report);
        Set("CoreWisp", "Core", report);
        AssetDatabase.SaveAssets();
        return report.ToString();
    }

    private static void Set(string row, string bone, StringBuilder report)
    {
        foreach (string name in new[] { row, row + "_Corrupted" })
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>("Assets/_Project/Data/Art/" + name + ".asset");
            if (art == null)
            {
                report.Append("MISSING ").Append(name).Append('\n');
                continue;
            }

            var so = new SerializedObject(art);
            SerializedProperty sockets = so.FindProperty("_beamSockets");
            sockets.arraySize = 1;
            sockets.GetArrayElementAtIndex(0).stringValue = bone;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(art);
            report.Append(name).Append(": ").Append(bone).Append('\n');
        }
    }
}
