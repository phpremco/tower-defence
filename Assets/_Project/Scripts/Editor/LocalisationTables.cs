using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Writes the generated English table (<c>Resources/Localization/en.data.txt</c>) from the player-facing text fields
    /// of the definition assets, and puts a key in each field in place of the text. The generator writes English
    /// literals into the definitions as before and runs this as its last data step, so the generator's spec stays the
    /// source of the English text while the assets carry keys only. Safe to run again: a field that already holds its
    /// key keeps it, and its text is taken over from the existing table. Keys are <c>data.&lt;kind&gt;.&lt;asset&gt;.&lt;field&gt;</c>.
    /// </summary>
    public static class LocalisationTables
    {
        public const string Folder = "Assets/_Project/Resources/Localization";
        public const string DataTablePath = Folder + "/en.data.txt";
        public const string KeyPrefix = "data.";

        private const string SearchRoot = "Assets/_Project";
        private const string Header =
            "# English: generated from the definition assets by Game.Editor.LocalisationTables (generator step \"Localisation\").\n" +
            "# Change the English text in the generator spec (or here for a quick fix: a full generator run writes the spec's text again).\n";

        private static readonly Regex Index = new Regex(@"\.Array\.data\[(\d+)\]", RegexOptions.Compiled);

        /// <summary>
        /// The localised text fields per definition type, as serialized property paths with array indices written as
        /// "[]". Only these fields are converted: names of bones, sockets and assets live in string fields too.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Fields = new Dictionary<string, string[]>
        {
            { "BossAbilityDefinition", new[] { "_displayName" } },
            { "BossDefinition", new[] { "_signatureText", "_telegraphText", "_counterText", "_phases[].Name", "_recognition._adaptLine" } },
            { "BossIntroDefinition", new[] { "_heroLine", "_repeatBarks[]", "_barks[]._line" } },
            { "CampaignCatalog", new[] { "_nodes[]._displayName", "_regions[]._displayName" } },
            { "CutsceneDefinition", new[] { "_shots[].TitleCard" } },
            { "DialogueDefinition", new[] { "_lines[].Segments[]._text", "_loadingTip" } },
            { "DifficultyProfile", new[] { "_displayName" } },
            { "DroneDefinition", new[] { "_displayName", "_role", "_summary" } },
            { "EnemyDefinition", new[] { "_displayName", "_description" } },
            { "HeroAbilityDefinition", new[] { "_displayName", "_description" } },
            { "HeroDefinition", new[] { "_displayName" } },
            { "HeroPerkDefinition", new[] { "_displayName", "_description" } },
            { "HeroWeaponDefinition", new[] { "_displayName" } },
            { "LevelDefinition", new[] { "_displayName", "_regionName", "_briefing", "_reward.Credit", "_reward.Story" } },
            { "PathObjectDefinition", new[] { "_displayName", "_description" } },
            { "SpeakerDefinition", new[] { "_displayName" } },
            { "TowerDefinition", new[] { "_displayName", "_roleLine", "_levels[].StepName" } },
            { "UpgradeDefinition", new[] { "_displayName", "_groupName", "_groupDescription" } },
        };

        [MenuItem("Tools/Generate/Localisation Tables")]
        public static void RunFromMenu()
        {
            Debug.Log(Run());
        }

        /// <summary>Converts every definition, writes the table and returns a one-line report.</summary>
        public static string Run()
        {
            Dictionary<string, string> existing = ReadTable(DataTablePath);
            var table = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var pending = new List<(ScriptableObject Asset, string Path, string Key)>();
            var owners = new Dictionary<string, string>();
            var errors = new List<string>();
            int converted = 0;

            foreach (ScriptableObject asset in Definitions())
            {
                string kind = KindOf(asset.GetType().Name);
                var serialized = new SerializedObject(asset);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.String || !IsLocalised(asset.GetType().Name, property.propertyPath))
                    {
                        continue;
                    }

                    string value = property.stringValue;
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    string key = KeyFor(kind, asset.name, property.propertyPath);
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    if (owners.TryGetValue(key, out string owner) && owner != assetPath)
                    {
                        errors.Add($"key '{key}' is claimed by '{owner}' and '{assetPath}': give the assets distinct names");
                        continue;
                    }

                    owners[key] = assetPath;
                    if (value == key)
                    {
                        if (existing.TryGetValue(key, out string text))
                        {
                            table[key] = text;
                        }
                        else
                        {
                            errors.Add($"'{assetPath}' holds the key '{key}' but the table has no text for it");
                        }

                        continue;
                    }

                    table[key] = value;
                    pending.Add((asset, property.propertyPath, key));
                    converted++;
                }
            }

            // The table goes in first: definitions check their texts when they change, and must find them by key.
            WriteTable(DataTablePath, table);
            AssetDatabase.ImportAsset(DataTablePath, ImportAssetOptions.ForceUpdate);
            Loc.ResetFallback();
            ApplyKeys(pending);
            AssetDatabase.SaveAssets();

            string report = $"Localisation: {table.Count} data texts, {converted} fields converted to keys.";
            if (errors.Count > 0)
            {
                Debug.LogError("Localisation tables:\n" + string.Join("\n", errors));
                report += $" {errors.Count} errors (see the console).";
            }

            return report;
        }

        /// <summary>
        /// What a partial generator step writes into a localised field: the field's key when the table already holds
        /// exactly this text under it, and the text itself otherwise (the next "Localisation" step converts it). A step
        /// that runs without that last step, such as the solve stage, would otherwise put the English text back over the key.
        /// </summary>
        public static string KeyOrText(ScriptableObject asset, string propertyPath, string text)
        {
            return KeyOrText(ReadTable(DataTablePath), asset.GetType().Name, asset.name, propertyPath, text);
        }

        /// <summary>The same choice against a given table (pure, for the tests).</summary>
        public static string KeyOrText(IReadOnlyDictionary<string, string> table, string typeName, string assetName, string propertyPath, string text)
        {
            if (!IsLocalised(typeName, propertyPath))
            {
                return text;
            }

            string key = KeyFor(KindOf(typeName), assetName, propertyPath);
            return table.TryGetValue(key, out string known) && known == text ? key : text;
        }

        /// <summary>True when the serialized path (with indices) is one of the localised fields of the type.</summary>
        public static bool IsLocalised(string typeName, string propertyPath)
        {
            if (!Fields.TryGetValue(typeName, out string[] paths))
            {
                return false;
            }

            string folded = Index.Replace(propertyPath, "[]");
            return Array.IndexOf(paths, folded) >= 0;
        }

        /// <summary>The key of one field: <c>data.tower.PulseRifle.displayName</c>, <c>data.dialogue.Dlg_L01_Start.lines.0.segments.1.text</c>.</summary>
        public static string KeyFor(string kind, string assetName, string propertyPath)
        {
            string path = Index.Replace(propertyPath, ".$1");
            string[] parts = path.Split('.');
            var builder = new StringBuilder(KeyPrefix).Append(kind).Append('.').Append(assetName.Replace(' ', '_'));
            foreach (string part in parts)
            {
                string trimmed = part.TrimStart('_');
                if (trimmed.Length == 0)
                {
                    continue;
                }

                builder.Append('.').Append(char.ToLowerInvariant(trimmed[0])).Append(trimmed, 1, trimmed.Length - 1);
            }

            return builder.ToString();
        }

        /// <summary>Every definition asset with localised fields, in a stable order.</summary>
        public static IEnumerable<ScriptableObject> Definitions()
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { SearchRoot }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(StringComparer.Ordinal);
            foreach (string path in paths)
            {
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is ScriptableObject definition && Fields.ContainsKey(definition.GetType().Name))
                    {
                        yield return definition;
                    }
                }
            }
        }

        /// <summary>Reads a table file into a dictionary (empty when the file does not exist).</summary>
        public static Dictionary<string, string> ReadTable(string path)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            if (File.Exists(path))
            {
                StringTableParser.Parse(File.ReadAllText(path), Path.GetFileName(path), table, null);
            }

            return table;
        }

        // "TowerDefinition" -> "tower", "DifficultyProfile" -> "difficultyProfile", "CampaignCatalog" -> "campaignCatalog".
        private static string KindOf(string typeName)
        {
            const string Suffix = "Definition";
            string kind = typeName.EndsWith(Suffix, StringComparison.Ordinal) ? typeName.Substring(0, typeName.Length - Suffix.Length) : typeName;
            return char.ToLowerInvariant(kind[0]) + kind.Substring(1);
        }

        private static void WriteTable(string path, SortedDictionary<string, string> table)
        {
            var text = new StringBuilder(Header);
            foreach (KeyValuePair<string, string> entry in table)
            {
                text.Append(entry.Key).Append(" = ").Append(StringTableParser.Escape(entry.Value)).Append('\n');
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        }

        private static void ApplyKeys(List<(ScriptableObject Asset, string Path, string Key)> pending)
        {
            var byAsset = new Dictionary<ScriptableObject, SerializedObject>();
            foreach ((ScriptableObject asset, string path, string key) in pending)
            {
                if (!byAsset.TryGetValue(asset, out SerializedObject serialized))
                {
                    serialized = new SerializedObject(asset);
                    byAsset.Add(asset, serialized);
                }

                serialized.FindProperty(path).stringValue = key;
            }

            foreach (KeyValuePair<ScriptableObject, SerializedObject> entry in byAsset)
            {
                entry.Value.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(entry.Key);
            }
        }
    }
}
