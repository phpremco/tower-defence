using System;
using System.Collections.Generic;
using Game.Boot;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// Generates the data assets and scenes for the first playable level. Safe to run repeatedly:
    /// existing assets are reused (and their values refreshed) and scenes are rebuilt in place.
    /// </summary>
    public static class ProjectGenerator
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string InputAssetPath = "Assets/_Project/Settings/Input/GameInput.inputactions";
        private const string DebugAssetPath = "Assets/_Project/Settings/PlaytestSettings.asset";
        private const string UnlockAssetPath = DataRoot + "/Unlocks/UnlockTable.asset";
        private const string LevelAssetPath = DataRoot + "/Levels/Level_Landing.asset";
        private const string ThemeAssetPath = DataRoot + "/Levels/Theme_Landing.asset";

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

        [MenuItem("Tools/Generate/Scenes and Starter Data")]
        public static void GenerateAll()
        {
            TowerDefinition pulse = CreateTower("PulseRifle", "Pulse Rifle", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.3f, 0.7f, 1f),
                new[] { Lvl(50, 4f, 3f, 0.35f), Lvl(60, 6f, 3.3f, 0.3f), Lvl(90, 9f, 3.6f, 0.25f) });
            TowerDefinition cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, 9f, 1.2f, new Color(1f, 0.8f, 0.3f),
                new[] { Lvl(100, 25f, 3.5f, 1.4f), Lvl(120, 40f, 3.8f, 1.2f), Lvl(180, 60f, 4.1f, 1f) });
            TowerDefinition rocket = CreateTower("Rocket", "Rocket", TargetLayers.Air, 8f, 1.5f, new Color(0.9f, 0.9f, 1f),
                new[] { Lvl(120, 30f, 5f, 1.6f), Lvl(140, 45f, 5.4f, 1.4f), Lvl(200, 70f, 5.8f, 1.2f) });

            EnemyDefinition scout = CreateEnemy("Scout", TargetLayers.Ground, 20f, 3f, 5, 1, new Color(0.9f, 0.6f, 0.15f), 0.45f);
            EnemyDefinition grunt = CreateEnemy("Grunt", TargetLayers.Ground, 50f, 2f, 8, 2, new Color(0.85f, 0.25f, 0.15f), 0.6f);
            EnemyDefinition brute = CreateEnemy("Brute", TargetLayers.Ground, 250f, 1.2f, 25, 5, new Color(0.55f, 0.1f, 0.15f), 0.95f);
            EnemyDefinition flyer = CreateEnemy("Flyer", TargetLayers.Air, 30f, 2.5f, 10, 2, new Color(0.5f, 0.9f, 0.3f), 0.5f);

            var difficulties = new[]
            {
                CreateDifficulty("Easy", 0.8f, 0.8f, 1.2f, 1f),
                CreateDifficulty("Medium", 1f, 1f, 1f, 1.25f),
                CreateDifficulty("Hard", 1.4f, 1.3f, 0.85f, 1.6f),
                CreateDifficulty("Psycho", 2f, 1.7f, 0.7f, 2.5f),
            };

            var waveSets = new List<DifficultyWaves>();
            for (int d = 0; d < difficulties.Length; d++)
            {
                waveSets.Add(new DifficultyWaves
                {
                    Difficulty = difficulties[d],
                    Waves = CreateWaves(DifficultyNames[d], d == 0 ? 5 : d == 3 ? 7 : 6, scout, grunt, brute, flyer),
                });
            }

            VisualTheme theme = LoadOrCreate<VisualTheme>(ThemeAssetPath);
            LevelDefinition level = LoadOrCreate<LevelDefinition>(LevelAssetPath);
            ConfigureLevel(level, theme, new[] { pulse, cannon, rocket }, waveSets);

            var unlocks = LoadOrCreate<UnlockTable>(UnlockAssetPath);
            var unlockProps = new SerializedObject(unlocks);
            SerializedProperty ids = unlockProps.FindProperty("_startingTowerIds");
            ids.arraySize = 1;
            ids.GetArrayElementAtIndex(0).stringValue = pulse.Id;
            unlockProps.ApplyModifiedPropertiesWithoutUndo();

            var debug = LoadOrCreate<PlaytestSettings>(DebugAssetPath);

            CreateBootScene(level, difficulties[1], unlocks, debug);
            CreateLevelScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Boot}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Level}.unity", true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("Generated level 1 data and the Boot and Level scenes.");
        }

        private static TowerLevel Lvl(int cost, float damage, float range, float interval) =>
            new TowerLevel { Cost = cost, Damage = damage, Range = range, FireInterval = interval };

        private static TowerDefinition CreateTower(string id, string displayName, TargetLayers targets, float projectileSpeed, float splash, Color color, TowerLevel[] levels)
        {
            var tower = LoadOrCreate<TowerDefinition>($"{DataRoot}/Towers/{id}.asset");
            var so = new SerializedObject(tower);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("_splashRadius").floatValue = splash;
            so.FindProperty("_color").colorValue = color;
            SerializedProperty array = so.FindProperty("_levels");
            array.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cost").intValue = levels[i].Cost;
                element.FindPropertyRelative("Damage").floatValue = levels[i].Damage;
                element.FindPropertyRelative("Range").floatValue = levels[i].Range;
                element.FindPropertyRelative("FireInterval").floatValue = levels[i].FireInterval;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return tower;
        }

        private static EnemyDefinition CreateEnemy(string id, TargetLayers layer, float health, float speed, int reward, int baseDamage, Color color, float scale)
        {
            var enemy = LoadOrCreate<EnemyDefinition>($"{DataRoot}/Enemies/{id}.asset");
            var so = new SerializedObject(enemy);
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_layer").intValue = (int)layer;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_reward").intValue = reward;
            so.FindProperty("_baseDamage").intValue = baseDamage;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_scale").floatValue = scale;
            so.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        private static DifficultyProfile CreateDifficulty(string id, float health, float count, float interval, float reward)
        {
            var profile = LoadOrCreate<DifficultyProfile>($"{DataRoot}/Difficulty/{id}.asset");
            var so = new SerializedObject(profile);
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_healthMultiplier").floatValue = health;
            so.FindProperty("_countMultiplier").floatValue = count;
            so.FindProperty("_spawnIntervalMultiplier").floatValue = interval;
            so.FindProperty("_rewardMultiplier").floatValue = reward;
            so.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static WaveDefinition[] CreateWaves(string difficultyName, int count, EnemyDefinition scout, EnemyDefinition grunt, EnemyDefinition brute, EnemyDefinition flyer)
        {
            var templates = new List<SpawnGroup[]>
            {
                new[] { Group(scout, 6, 1f, 0f) },
                new[] { Group(grunt, 8, 1.2f, 0f) },
                new[] { Group(scout, 8, 0.8f, 0f), Group(flyer, 4, 1.5f, 3f) },
                new[] { Group(grunt, 10, 1f, 0f), Group(brute, 2, 3f, 5f) },
                new[] { Group(flyer, 8, 1f, 0f), Group(scout, 10, 0.6f, 2f) },
                new[] { Group(brute, 5, 2.5f, 0f), Group(grunt, 12, 0.8f, 2f), Group(flyer, 6, 1.2f, 4f) },
                new[] { Group(brute, 8, 2f, 0f), Group(flyer, 10, 0.8f, 3f), Group(grunt, 16, 0.6f, 6f) },
            };

            var waves = new WaveDefinition[count];
            for (int i = 0; i < count; i++)
            {
                var wave = LoadOrCreate<WaveDefinition>($"{DataRoot}/Waves/Landing_{difficultyName}_{i + 1}.asset");
                var so = new SerializedObject(wave);
                so.FindProperty("_startDelay").floatValue = i == 0 ? 10f : 8f;
                SerializedProperty groups = so.FindProperty("_groups");
                SpawnGroup[] template = templates[i];
                groups.arraySize = template.Length;
                for (int g = 0; g < template.Length; g++)
                {
                    SerializedProperty element = groups.GetArrayElementAtIndex(g);
                    element.FindPropertyRelative("Enemy").objectReferenceValue = template[g].Enemy;
                    element.FindPropertyRelative("Count").intValue = template[g].Count;
                    element.FindPropertyRelative("Interval").floatValue = template[g].Interval;
                    element.FindPropertyRelative("Delay").floatValue = template[g].Delay;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                waves[i] = wave;
            }

            return waves;
        }

        private static SpawnGroup Group(EnemyDefinition enemy, int count, float interval, float delay) =>
            new SpawnGroup { Enemy = enemy, Count = count, Interval = interval, Delay = delay };

        private static void ConfigureLevel(LevelDefinition level, VisualTheme theme, TowerDefinition[] towers, List<DifficultyWaves> waveSets)
        {
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = "Landing Zone";
            so.FindProperty("_gridSize").vector2IntValue = new Vector2Int(16, 10);
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_theme").objectReferenceValue = theme;
            so.FindProperty("_startingMoney").intValue = 150;
            so.FindProperty("_baseHealth").intValue = 20;

            WriteCells(so.FindProperty("_landPath"), new[]
            {
                new Vector2Int(0, 8), new Vector2Int(12, 8), new Vector2Int(12, 5),
                new Vector2Int(3, 5), new Vector2Int(3, 2), new Vector2Int(15, 2),
            });
            WriteCells(so.FindProperty("_airPath"), new[] { new Vector2Int(0, 9), new Vector2Int(15, 2) });
            WriteCells(so.FindProperty("_buildSlots"), new[]
            {
                new Vector2Int(2, 7), new Vector2Int(5, 7), new Vector2Int(8, 7), new Vector2Int(11, 7),
                new Vector2Int(11, 6), new Vector2Int(13, 6), new Vector2Int(14, 7),
                new Vector2Int(5, 4), new Vector2Int(8, 4), new Vector2Int(11, 4),
                new Vector2Int(2, 3), new Vector2Int(4, 3), new Vector2Int(6, 3), new Vector2Int(9, 3), new Vector2Int(12, 3),
                new Vector2Int(5, 1), new Vector2Int(8, 1), new Vector2Int(11, 1), new Vector2Int(14, 1),
            });

            SerializedProperty towerArray = so.FindProperty("_availableTowers");
            towerArray.arraySize = towers.Length;
            for (int i = 0; i < towers.Length; i++)
            {
                towerArray.GetArrayElementAtIndex(i).objectReferenceValue = towers[i];
            }

            SerializedProperty waveArray = so.FindProperty("_difficultyWaves");
            waveArray.arraySize = waveSets.Count;
            for (int i = 0; i < waveSets.Count; i++)
            {
                SerializedProperty element = waveArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Difficulty").objectReferenceValue = waveSets[i].Difficulty;
                SerializedProperty waves = element.FindPropertyRelative("Waves");
                waves.arraySize = waveSets[i].Waves.Length;
                for (int w = 0; w < waveSets[i].Waves.Length; w++)
                {
                    waves.GetArrayElementAtIndex(w).objectReferenceValue = waveSets[i].Waves[w];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteCells(SerializedProperty array, Vector2Int[] cells)
        {
            array.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
            {
                array.GetArrayElementAtIndex(i).vector2IntValue = cells[i];
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void CreateBootScene(LevelDefinition level, DifficultyProfile difficulty, UnlockTable unlocks, PlaytestSettings debug)
        {
            // NewScene unloads assets nobody references yet, so remember the paths and load them again afterwards.
            string levelPath = AssetDatabase.GetAssetPath(level);
            string difficultyPath = AssetDatabase.GetAssetPath(difficulty);
            string unlocksPath = AssetDatabase.GetAssetPath(unlocks);
            string debugPath = AssetDatabase.GetAssetPath(debug);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrapper");
            var bootstrapper = go.AddComponent<Bootstrapper>();
            var so = new SerializedObject(bootstrapper);
            so.FindProperty("_inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            so.FindProperty("_startLevel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>(levelPath);
            so.FindProperty("_startDifficulty").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(difficultyPath);
            so.FindProperty("_unlockTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnlockTable>(unlocksPath);
            so.FindProperty("_playtestSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlaytestSettings>(debugPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.Boot}.unity");
        }

        private static void CreateLevelScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            var rig = cameraGo.AddComponent<FixedCameraRig>();

            var root = new GameObject("LevelBootstrapper");
            var bootstrapper = root.AddComponent<LevelBootstrapper>();
            var so = new SerializedObject(bootstrapper);
            so.FindProperty("_cameraRig").objectReferenceValue = rig;
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.Level}.unity");
        }
    }
}




