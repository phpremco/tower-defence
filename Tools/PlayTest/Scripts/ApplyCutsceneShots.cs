using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Enemies;
using UnityEditor;

/// <summary>
/// Round 35: runs only the generator's "Cutscene" step (dialogues, cast, shots of the opening cutscene), without the
/// Blender "Story art" and "Cutscene art" hand-outs (the cast keeps the art it has), and saves, for a change to the shots
/// or cues alone. Same code as that step of Tools/Generate/Scenes and Starter Data. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyCutsceneShots.cs --entry ApplyCutsceneShots.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyCutsceneShots
{
    public static string Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type generator = typeof(Game.Editor.ProjectGenerator);
        Type contextType = generator.GetNestedType("GeneratorContext", BindingFlags.NonPublic);
        object context = Activator.CreateInstance(contextType, true);
        contextType.GetProperty("Hero").SetValue(context, AssetDatabase.LoadAssetAtPath<Game.Hero.HeroDefinition>("Assets/_Project/Data/Hero/Commander.asset"));
        contextType.GetProperty("HarvestDrone").SetValue(context, AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>("Assets/_Project/Data/Drones/HarvestDrone.asset"));
        contextType.GetProperty("RepairDrone").SetValue(context, AssetDatabase.LoadAssetAtPath<Game.Drones.DroneDefinition>("Assets/_Project/Data/Drones/RepairDrone.asset"));
        var difficulties = new List<Game.Levels.DifficultyProfile>();
        foreach (string name in new[] { "Easy", "Medium", "Hard", "Psycho" })
        {
            difficulties.Add(AssetDatabase.LoadAssetAtPath<Game.Levels.DifficultyProfile>($"Assets/_Project/Data/Difficulty/{name}.asset"));
        }

        contextType.GetProperty("Difficulties").SetValue(context, difficulties.ToArray());

        // The bestiary the generator would hold: every creature asset by its field's name (the Queen is HiveQueen).
        Type bestiaryType = generator.GetNestedType("Bestiary", BindingFlags.NonPublic);
        object bestiary = Activator.CreateInstance(bestiaryType, true);
        var byName = new Dictionary<string, EnemyDefinition>();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            byName[enemy.name] = enemy;
        }

        foreach (FieldInfo field in bestiaryType.GetFields())
        {
            if (field.FieldType != typeof(EnemyDefinition))
            {
                continue;
            }

            if (!byName.TryGetValue(field.Name, out EnemyDefinition found))
            {
                foreach (KeyValuePair<string, EnemyDefinition> pair in byName)
                {
                    if (pair.Key.EndsWith(field.Name, StringComparison.Ordinal))
                    {
                        found = pair.Value;
                    }
                }
            }

            field.SetValue(bestiary, found);
        }

        contextType.GetProperty("Bestiary").SetValue(context, bestiary);
        generator.GetMethod("GenerateCutscene", flags).Invoke(null, new[] { context });
        AssetDatabase.SaveAssets();
        return "cutscene shots, cues and cast regenerated";
    }
}
