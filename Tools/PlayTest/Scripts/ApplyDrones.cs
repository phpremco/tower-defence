using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Enemies;
using UnityEditor;

/// <summary>
/// Writes which drone kinds each campaign level offers (the generator's ConfigureLevelDrones, part of "Campaign levels")
/// and runs the "Level story" step (speakers, dialogues, story triggers), on the existing assets, for a drone or story
/// change while the full generator must not run (it syncs the Blender exports from ArtSource). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyDrones.cs --entry ApplyDrones.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyDrones
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
    private const string Data = "Assets/_Project/Data";

    public static string Run()
    {
        Type generator = typeof(Game.Editor.ProjectGenerator);
        Type contextType = generator.GetNestedType("GeneratorContext", BindingFlags.NonPublic);
        object context = Activator.CreateInstance(contextType, true);
        void Set(string property, object value) => contextType.GetProperty(property).SetValue(context, value);

        Set("RepairDrone", Load<Game.Drones.DroneDefinition>("Drones/RepairDrone"));
        Set("HarvestDrone", Load<Game.Drones.DroneDefinition>("Drones/HarvestDrone"));
        Set("AttackDrone", Load<Game.Drones.DroneDefinition>("Drones/AttackDrone"));
        Set("SlowField", Load<Game.PathObjects.PathObjectDefinition>("PathObjects/SlowField"));
        Set("Mine", Load<Game.PathObjects.PathObjectDefinition>("PathObjects/Mine"));
        Set("LavaBall", Load<Game.PathObjects.PathObjectDefinition>("PathObjects/LavaBall"));
        Set("Barrel", Load<Game.PathObjects.PathObjectDefinition>("PathObjects/ExplosiveBarrel"));
        Set("GroundDrone", Load<Game.PathObjects.PathObjectDefinition>("PathObjects/GroundDrone"));
        Set("Bestiary", Bestiary(generator));

        // The campaign levels with their specs, in campaign order, as the "Campaign levels" step leaves them.
        Type specLevel = generator.GetNestedType("SpecLevel", BindingFlags.NonPublic);
        var levels = (IList)contextType.GetProperty("CampaignLevels").GetValue(context);
        MethodInfo configure = generator.GetMethod("ConfigureLevelDrones", Flags);
        var report = new System.Text.StringBuilder();
        Game.Editor.CampaignLevelSpec[][] continents = Game.Editor.LevelSpecs.Continents;
        for (int c = 0; c < continents.Length; c++)
        {
            for (int i = 0; i < continents[c].Length; i++)
            {
                Game.Editor.CampaignLevelSpec spec = continents[c][i];
                var level = Load<Game.Levels.LevelDefinition>($"Levels/Level_{spec.Key}");
                if (level == null)
                {
                    throw new InvalidOperationException("No level asset for " + spec.Key);
                }

                configure.Invoke(null, new object[] { level, spec.Key, context });
                EditorUtility.SetDirty(level);
                levels.Add(Activator.CreateInstance(specLevel, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { spec, level, c, i }, null));
                report.Append($"{level.name}: {level.Drones.Length} drones\n");
            }
        }

        generator.GetMethod("GenerateLevelStory", Flags).Invoke(null, new[] { context });
        AssetDatabase.SaveAssets();
        return report.ToString();
    }

    private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>($"{Data}/{path}.asset");

    // The bestiary the generator would hold: every creature asset by its field's name (as ApplyWaves.cs).
    private static object Bestiary(Type generator)
    {
        Type bestiaryType = generator.GetNestedType("Bestiary", BindingFlags.NonPublic);
        object bestiary = Activator.CreateInstance(bestiaryType, true);
        var byName = new Dictionary<string, EnemyDefinition>();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { Data + "/Enemies" }))
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

            field.SetValue(bestiary, found ?? throw new InvalidOperationException("No creature asset for " + field.Name));
        }

        return bestiary;
    }
}
