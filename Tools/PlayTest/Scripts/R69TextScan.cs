using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 69 discovery: lists every string property in the project's ScriptableObject assets that holds words (a space
/// or a sentence), grouped by type and field path (array indices folded), with a count and one example. Writes
/// Tools/PlayTest/r69_text_scan.txt. Read only. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/R69TextScan.cs --entry R69TextScan.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class R69TextScan
{
    public static string Run()
    {
        var groups = new SortedDictionary<string, (int count, string example)>();
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is ScriptableObject so))
                {
                    continue;
                }

                var serialized = new SerializedObject(so);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.String)
                    {
                        continue;
                    }

                    string value = property.stringValue;
                    if (string.IsNullOrEmpty(value) || value.IndexOf(' ') < 0)
                    {
                        continue;
                    }

                    string key = so.GetType().Name + " :: " + System.Text.RegularExpressions.Regex.Replace(property.propertyPath, @"\[\d+\]", "[]");
                    groups.TryGetValue(key, out (int count, string example) entry);
                    groups[key] = (entry.count + 1, entry.example ?? (so.name + ": " + (value.Length > 90 ? value.Substring(0, 90) : value)));
                }
            }
        }

        var text = new StringBuilder();
        foreach (KeyValuePair<string, (int count, string example)> group in groups)
        {
            text.Append(group.Value.count.ToString().PadLeft(5)).Append("  ").Append(group.Key).Append("   e.g. ").AppendLine(group.Value.example.Replace("\n", " "));
        }

        System.IO.File.WriteAllText("Tools/PlayTest/r69_text_scan.txt", text.ToString());
        return groups.Count + " groups";
    }
}
