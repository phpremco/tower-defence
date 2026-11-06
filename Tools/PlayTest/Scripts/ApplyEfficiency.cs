using System.Globalization;
using System.IO;
using System.Text;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 21a3: writes the ledger's in-play efficiency per tower type (the "factor" lines of Tools/PlayTest/tower_efficiency.txt,
/// measured by MonocultureProbeTests.Measure_TowerEfficiency_OnMedium) into each TowerDefinition's _ledgerEfficiency. Types
/// the measurement did not see keep their value. Re-measure after a tower, wave or bot change that moves where guns stand or
/// what they hit, then run ApplyWaves.cs so the waves are solved against the new numbers. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyEfficiency.cs --entry ApplyEfficiency.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class ApplyEfficiency
{
    private const string TablePath = "Tools/PlayTest/tower_efficiency.txt";

    public static string Run()
    {
        var report = new StringBuilder();
        foreach (string line in File.ReadAllLines(TablePath))
        {
            string[] parts = line.Split(' ');
            if (parts.Length != 3 || parts[0] != "factor")
            {
                continue;
            }

            var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{parts[1]}.asset");
            if (tower == null)
            {
                report.Append($"{parts[1]}: no asset; ");
                continue;
            }

            var so = new SerializedObject(tower);
            so.FindProperty("_ledgerEfficiency").floatValue = float.Parse(parts[2], CultureInfo.InvariantCulture);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tower);
            report.Append($"{parts[1]} {parts[2]}; ");
        }

        AssetDatabase.SaveAssets();
        return report.ToString();
    }
}
