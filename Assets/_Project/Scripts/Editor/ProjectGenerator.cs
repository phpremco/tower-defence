using System;
using System.Collections.Generic;
using Game.Boot;
using Game.Core;
using Game.Economy;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.Towers;
using Game.UI;
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
        private const string ModelFolder = "Assets/_Project/Art/Models/SpaceKit/";
        private const string UiFolder = "Assets/_Project/Art/UI/";
        private const string SkinAssetPath = "Assets/_Project/Settings/UiSkin.asset";
        private const string DebugAssetPath = "Assets/_Project/Settings/PlaytestSettings.asset";
        private const string UnlockAssetPath = DataRoot + "/Unlocks/UnlockTable.asset";
        private const string LevelAssetPath = DataRoot + "/Levels/Level_Landing.asset";
        private const string ThemeAssetPath = DataRoot + "/Levels/Theme_Landing.asset";

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

        [MenuItem("Tools/Generate/Scenes and Starter Data")]
        public static void GenerateAll()
        {
            CustomModelBuilder.BuildAll();

            TowerDefinition pulse = CreateTower("PulseRifle", "Pulse Rifle", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.3f, 0.7f, 1f),
                new[] { Lvl(50, 4f, 3f, 0.35f), Lvl(60, 6f, 3.3f, 0.3f), Lvl(90, 9f, 3.6f, 0.25f) }, 80f);
            TowerDefinition cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, 9f, 1.2f, new Color(1f, 0.8f, 0.3f),
                new[] { Lvl(100, 25f, 3.5f, 1.4f), Lvl(120, 40f, 3.8f, 1.2f), Lvl(180, 60f, 4.1f, 1f) }, 120f);
            TowerDefinition rocket = CreateTower("Rocket", "Rocket", TargetLayers.Air, 8f, 1.5f, new Color(0.9f, 0.9f, 1f),
                new[] { Lvl(120, 30f, 5f, 1.6f), Lvl(140, 45f, 5.4f, 1.4f), Lvl(200, 70f, 5.8f, 1.2f) }, 70f);

            // Level 0 cost is the price to rebuild a wrecked blockade. The gun arrives with the first upgrade.
            TowerDefinition blockade = CreateTower("Blockade", "Blockade", TargetLayers.Ground, 12f, 0f, new Color(0.55f, 0.65f, 0.8f),
                new[] { Lvl(60, 0f, 3.2f, 0.5f), Lvl(90, 5f, 3.4f, 0.45f, 300f), Lvl(150, 9f, 3.7f, 0.35f, 500f) }, 500f, blocksGround: true);

            EnemyDefinition scout = CreateEnemy("Scout", TargetLayers.Ground, 20f, 3f, 5, 1, new Color(0.9f, 0.6f, 0.15f), 0.45f, blockadeDamage: 3f);
            EnemyDefinition grunt = CreateEnemy("Grunt", TargetLayers.Ground, 50f, 2f, 8, 2, new Color(0.85f, 0.25f, 0.15f), 0.6f, blockadeDamage: 5f);
            EnemyDefinition brute = CreateEnemy("Brute", TargetLayers.Ground, 250f, 1.2f, 25, 5, new Color(0.55f, 0.1f, 0.15f), 0.95f, blockadeDamage: 20f);
            EnemyDefinition flyer = CreateEnemy("Flyer", TargetLayers.Air, 30f, 2.5f, 10, 2, new Color(0.5f, 0.9f, 0.3f), 0.5f, 6f);

            var difficulties = new[]
            {
                CreateDifficulty("Easy", 0.8f, 0.8f, 1.2f, 1f, 0f),
                CreateDifficulty("Medium", 1f, 1f, 1f, 1.25f, 0.5f),
                CreateDifficulty("Hard", 1.4f, 1.3f, 0.85f, 1.6f, 1f),
                CreateDifficulty("Psycho", 2f, 1.7f, 0.7f, 2.5f, 2f),
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
            ConfigureTheme(theme);
            LevelDefinition level = LoadOrCreate<LevelDefinition>(LevelAssetPath);
            HeroDefinition hero = LoadOrCreate<HeroDefinition>($"{DataRoot}/Hero/Commander.asset");
            var heroProps = new SerializedObject(hero);
            heroProps.FindProperty("_damage").floatValue = 18f;
            heroProps.FindProperty("_reach").floatValue = 1.5f;
            heroProps.FindProperty("_strikeInterval").floatValue = 0.7f;
            heroProps.FindProperty("_repairCost").intValue = 30;
            heroProps.FindProperty("_color").colorValue = new Color(1f, 0.85f, 0.3f);
            heroProps.ApplyModifiedPropertiesWithoutUndo();
            ConfigureLevel(level, theme, new[] { pulse, cannon, rocket }, waveSets, cannon, blockade, hero);

            var unlocks = LoadOrCreate<UnlockTable>(UnlockAssetPath);
            var unlockProps = new SerializedObject(unlocks);
            SerializedProperty ids = unlockProps.FindProperty("_startingTowerIds");
            ids.arraySize = 1;
            ids.GetArrayElementAtIndex(0).stringValue = pulse.Id;
            unlockProps.ApplyModifiedPropertiesWithoutUndo();

            var debug = LoadOrCreate<PlaytestSettings>(DebugAssetPath);
            // The pulse rifle uses the hand-made gatling model, coloured by its recipe in CustomModelBuilder.
            SetCustomModel(pulse, "machinegun", 1.15f);
            SetModel(cannon, "turret_double", 1.4f);
            SetModel(rocket, "satelliteDish_large", 1.5f);
            SetModel(scout, "alien", 0.9f);
            SetModel(grunt, "alien", 1.2f);
            SetModel(brute, "alien", 1.9f);
            SetModel(flyer, "craft_speederA", 0.5f);

            UpgradeCatalog upgradeCatalog = CreateUpgrades(pulse, cannon, rocket);
            UiSkin skin = CreateSkin();
            CampaignCatalog campaign = CreateCampaign(level, difficulties);

            CreateBootScene(campaign, upgradeCatalog, unlocks, debug, skin);
            CreateLevelScene();
            CreateMenuScene<MainMenuScreen>(SceneNames.MainMenu);
            CreateMenuScene<WorldMapScreen>(SceneNames.WorldMap);
            CreateMenuScene<UpgradeScreen>(SceneNames.Base);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Boot}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.MainMenu}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.WorldMap}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Level}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Base}.unity", true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("Generated level 1 data and the Boot and Level scenes.");
        }

        private static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}{name}.fbx");

        private static void SetCustomModel(Object asset, string modelName, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CustomModelBuilder.OutputFolder}/{modelName}.prefab");
            var so = new SerializedObject(asset);
            so.FindProperty("_model").objectReferenceValue = prefab;
            so.FindProperty("_modelScale").floatValue = scale;
            so.FindProperty("_keepModelColours").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetModel(Object asset, string modelName, float scale)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("_model").objectReferenceValue = Model(modelName);
            so.FindProperty("_modelScale").floatValue = scale;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TowerLevel Lvl(int cost, float damage, float range, float interval, float healthBonus = 0f) =>
            new TowerLevel { Cost = cost, Damage = damage, Range = range, FireInterval = interval, HealthBonus = healthBonus };

        private static TowerDefinition CreateTower(string id, string displayName, TargetLayers targets, float projectileSpeed, float splash, Color color, TowerLevel[] levels, float health, bool blocksGround = false)
        {
            var tower = LoadOrCreate<TowerDefinition>($"{DataRoot}/Towers/{id}.asset");
            var so = new SerializedObject(tower);
            so.FindProperty("_blocksGroundEnemies").boolValue = blocksGround;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            so.FindProperty("_splashRadius").floatValue = splash;
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_maxHealth").floatValue = health;
            SerializedProperty array = so.FindProperty("_levels");
            array.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cost").intValue = levels[i].Cost;
                element.FindPropertyRelative("Damage").floatValue = levels[i].Damage;
                element.FindPropertyRelative("Range").floatValue = levels[i].Range;
                element.FindPropertyRelative("FireInterval").floatValue = levels[i].FireInterval;
                element.FindPropertyRelative("HealthBonus").floatValue = levels[i].HealthBonus;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return tower;
        }

        private static EnemyDefinition CreateEnemy(string id, TargetLayers layer, float health, float speed, int reward, int baseDamage, Color color, float scale, float attackDamage = 0f, float blockadeDamage = 4f)
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
            so.FindProperty("_attackDamage").floatValue = attackDamage;
            so.FindProperty("_blockadeDamage").floatValue = blockadeDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        private static DifficultyProfile CreateDifficulty(string id, float health, float count, float interval, float reward, float power)
        {
            var profile = LoadOrCreate<DifficultyProfile>($"{DataRoot}/Difficulty/{id}.asset");
            var so = new SerializedObject(profile);
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_healthMultiplier").floatValue = health;
            so.FindProperty("_countMultiplier").floatValue = count;
            so.FindProperty("_spawnIntervalMultiplier").floatValue = interval;
            so.FindProperty("_rewardMultiplier").floatValue = reward;
            so.FindProperty("_powerCurrencyMultiplier").floatValue = power;
            so.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static WaveDefinition[] CreateWaves(string difficultyName, int count, EnemyDefinition scout, EnemyDefinition grunt, EnemyDefinition brute, EnemyDefinition flyer)
        {
            // Level 1 teaches the game: ground waves first, the first flyers only from wave 5.
            var templates = new List<SpawnGroup[]>
            {
                new[] { Group(scout, 6, 1f, 0f) },
                new[] { Group(grunt, 8, 1.2f, 0f) },
                new[] { Group(scout, 8, 0.8f, 0f), Group(grunt, 4, 1.2f, 4f) },
                new[] { Group(grunt, 10, 1f, 0f), Group(brute, 1, 3f, 6f) },
                new[] { Group(grunt, 10, 0.9f, 0f), Group(brute, 2, 3f, 5f), Group(flyer, 2, 2.5f, 8f, NorthAirRoute), Group(flyer, 2, 2.5f, 10f, Road) },
                new[] { Group(brute, 3, 2.5f, 0f), Group(grunt, 12, 0.8f, 2f), Group(flyer, 3, 2f, 5f, NorthAirRoute), Group(flyer, 2, 2.5f, 8f, SouthAirRoute) },
                new[] { Group(brute, 5, 2f, 0f), Group(grunt, 16, 0.6f, 4f), Group(flyer, 4, 1.5f, 6f, NorthAirRoute), Group(flyer, 4, 1.5f, 6.5f, SouthAirRoute) },
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
                    element.FindPropertyRelative("PathIndex").intValue = template[g].PathIndex;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                waves[i] = wave;
            }

            return waves;
        }

        private const int Road = 0;
        private const int NorthAirRoute = 1;
        private const int SouthAirRoute = 2;

        private static SpawnGroup Group(EnemyDefinition enemy, int count, float interval, float delay, int path = Road) =>
            new SpawnGroup { Enemy = enemy, Count = count, Interval = interval, Delay = delay, PathIndex = path };

        private static void ConfigureLevel(LevelDefinition level, VisualTheme theme, TowerDefinition[] towers, List<DifficultyWaves> waveSets, TowerDefinition rewardTower, TowerDefinition blockade, HeroDefinition hero)
        {
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = "Landing Zone";
            so.FindProperty("_gridSize").vector2IntValue = new Vector2Int(16, 10);
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_theme").objectReferenceValue = theme;
            SerializedProperty camera = so.FindProperty("_camera");
            camera.FindPropertyRelative("PitchDegrees").floatValue = 55f;
            camera.FindPropertyRelative("YawDegrees").floatValue = 0f;
            camera.FindPropertyRelative("FitMargin").floatValue = 0.04f;
            so.FindProperty("_startingMoney").intValue = 150;
            so.FindProperty("_baseHealth").intValue = 20;
            SerializedProperty stars = so.FindProperty("_starBaseHealthFractions");
            stars.arraySize = 2;
            stars.GetArrayElementAtIndex(0).floatValue = 0.5f;
            stars.GetArrayElementAtIndex(1).floatValue = 0.9f;

            SerializedProperty reward = so.FindProperty("_reward");
            reward.FindPropertyRelative("ResearchPoints").intValue = 10;
            reward.FindPropertyRelative("PowerCurrency").intValue = 4;
            reward.FindPropertyRelative("NewTower").objectReferenceValue = rewardTower;
            reward.FindPropertyRelative("Source").intValue = (int)UnlockSource.HomePlanetIntel;
            reward.FindPropertyRelative("Story").stringValue = "The home planet sent schematics for a heavy cannon.";

            WritePaths(so.FindProperty("_paths"), new[]
            {
                (
                    "Road",
                    false,
                    new[]
                    {
                        new Vector2Int(0, 8), new Vector2Int(12, 8), new Vector2Int(12, 5),
                        new Vector2Int(3, 5), new Vector2Int(3, 2), new Vector2Int(15, 2),
                    }),
                ("North air route", true, new[] { new Vector2Int(0, 9), new Vector2Int(15, 2) }),
                ("South air route", true, new[] { new Vector2Int(0, 0), new Vector2Int(8, 5), new Vector2Int(15, 2) }),
            });
            WriteCells(so.FindProperty("_blockedCells"), new[]
            {
                new Vector2Int(6, 6), new Vector2Int(7, 6), new Vector2Int(7, 7), new Vector2Int(9, 7),
                new Vector2Int(13, 4), new Vector2Int(14, 4), new Vector2Int(14, 5), new Vector2Int(1, 0),
                new Vector2Int(2, 0), new Vector2Int(8, 0), new Vector2Int(5, 3), new Vector2Int(0, 4),
                new Vector2Int(1, 3), new Vector2Int(10, 4),
            });
            so.FindProperty("_hero").objectReferenceValue = hero;

            // One blockade across the middle leg of the road and one on the last leg before the base.
            var blockadeCells = new[] { new Vector2Int(7, 5), new Vector2Int(10, 2) };
            SerializedProperty blockades = so.FindProperty("_blockades");
            blockades.arraySize = blockadeCells.Length;
            for (int i = 0; i < blockadeCells.Length; i++)
            {
                SerializedProperty element = blockades.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cell").vector2IntValue = blockadeCells[i];
                element.FindPropertyRelative("Blockade").objectReferenceValue = blockade;
            }

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

        private static UiSkin CreateSkin()
        {
            var skin = LoadOrCreate<UiSkin>(SkinAssetPath);
            var so = new SerializedObject(skin);
            so.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(UiFolder + "Fonts/KenneyFuture.ttf");
            so.FindProperty("_narrowFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(UiFolder + "Fonts/KenneyFutureNarrow.ttf");

            var sprites = new (string Field, string File)[]
            {
                ("_panel", "panel"), ("_panelSquare", "panel_square"), ("_button", "button"), ("_buttonSquare", "button_square"),
                ("_barBack", "bar_back"), ("_barBlue", "bar_blue"), ("_barGreen", "bar_green"), ("_barYellow", "bar_yellow"), ("_barRed", "bar_red"),
                ("_cardBlue", "card_blue"), ("_cardGreen", "card_green"), ("_cardYellow", "card_yellow"), ("_cardRed", "card_red"),
            };

            foreach ((string field, string file) in sprites)
            {
                so.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiFolder}Sprites/{file}.png");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return skin;
        }

        // Landing Zone palette: grey and blue on an open plain, glowing cyan accents.
        private static void ConfigureTheme(VisualTheme theme)
        {
            var so = new SerializedObject(theme);
            so.FindProperty("_backgroundColor").colorValue = new Color(0.02f, 0.03f, 0.06f);
            so.FindProperty("_groundColor").colorValue = new Color(0.16f, 0.26f, 0.4f);
            so.FindProperty("_pathColor").colorValue = new Color(0.55f, 0.8f, 1f);
            so.FindProperty("_groundTile").objectReferenceValue = Model("terrain");
            so.FindProperty("_roadStraight").objectReferenceValue = Model("terrain_roadStraight");
            so.FindProperty("_roadCorner").objectReferenceValue = Model("terrain_roadCorner");
            so.FindProperty("_roadEnd").objectReferenceValue = Model("terrain_roadEnd");
            so.FindProperty("_roadCross").objectReferenceValue = Model("terrain_roadCross");
            so.FindProperty("_baseModel").objectReferenceValue = Model("machine_generatorLarge");
            so.FindProperty("_spawnModel").objectReferenceValue = Model("gate_simple");
            SetObjects(so.FindProperty("_blockedProps"), "rock_largeA", "rock_largeB", "rock_crystals", "meteor");
            SetObjects(so.FindProperty("_scenery"), "rocks_smallA", "rocks_smallB", "crater", "craterLarge", "rock_crystalsLargeA", "rock_largeA", "meteor");
            so.FindProperty("_slotColor").colorValue = new Color(0.2f, 0.55f, 1f);
            so.FindProperty("_baseColor").colorValue = new Color(0.3f, 0.85f, 1f);
            so.FindProperty("_lightColor").colorValue = new Color(0.75f, 0.85f, 1f);
            so.FindProperty("_lightIntensity").floatValue = 1.1f;
            so.FindProperty("_fogColor").colorValue = new Color(0.03f, 0.04f, 0.08f);
            so.FindProperty("_fogDensity").floatValue = 0.01f;
            so.FindProperty("_bloomIntensity").floatValue = 1.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WritePaths(SerializedProperty array, (string Name, bool Flying, Vector2Int[] Waypoints)[] paths)
        {
            array.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Name").stringValue = paths[i].Name;
                element.FindPropertyRelative("Flying").boolValue = paths[i].Flying;
                WriteCells(element.FindPropertyRelative("Waypoints"), paths[i].Waypoints);
            }
        }

        private static void SetObjects(SerializedProperty array, params string[] modelNames)
        {
            array.arraySize = modelNames.Length;
            for (int i = 0; i < modelNames.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = Model(modelNames[i]);
            }
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

        private static UpgradeCatalog CreateUpgrades(params TowerDefinition[] towers)
        {
            var all = new List<UpgradeDefinition>();
            foreach (TowerDefinition tower in towers)
            {
                all.Add(CreateUpgrade($"{tower.Id}_Damage", "Damage +10% per level", UpgradeTarget.TowerDamage, new[] { 5, 10, 15 }, 0.1f, tower.Id, tower.DisplayName));
                all.Add(CreateUpgrade($"{tower.Id}_Range", "Range +5% per level", UpgradeTarget.TowerRange, new[] { 5, 10, 15 }, 0.05f, tower.Id, tower.DisplayName));
                all.Add(CreateUpgrade($"{tower.Id}_FireRate", "Fire rate +10% per level", UpgradeTarget.TowerFireRate, new[] { 6, 12, 18 }, 0.1f, tower.Id, tower.DisplayName));
            }

            all.Add(CreateUpgrade("BaseHealth", "Base armour +5 HP per level", UpgradeTarget.BaseHealth, new[] { 4, 8, 12 }, 5f, string.Empty, "Base"));
            all.Add(CreateUpgrade("BlockadeHealth", "Blockade strength +20% per level", UpgradeTarget.BlockadeHealth, new[] { 4, 8, 12 }, 0.2f, string.Empty, "Base"));

            all.Add(CreateUpgrade("Hero_Damage", "Blade damage +15% per level", UpgradeTarget.HeroDamage, new[] { 5, 10, 15 }, 0.15f, string.Empty, "Hero"));
            all.Add(CreateUpgrade("Hero_Health", "Armour +20% per level", UpgradeTarget.HeroHealth, new[] { 5, 10, 15 }, 0.2f, string.Empty, "Hero"));

            var catalog = LoadOrCreate<UpgradeCatalog>($"{DataRoot}/Upgrades/UpgradeCatalog.asset");
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("_upgrades");
            list.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }
        private static UpgradeDefinition CreateUpgrade(string id, string displayName, UpgradeTarget target, int[] costs, float bonus, string towerId, string groupName)
        {
            var upgrade = LoadOrCreate<UpgradeDefinition>($"{DataRoot}/Upgrades/{id}.asset");
            var so = new SerializedObject(upgrade);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_target").enumValueIndex = (int)target;
            so.FindProperty("_bonusPerLevel").floatValue = bonus;
            so.FindProperty("_towerId").stringValue = towerId;
            so.FindProperty("_groupName").stringValue = groupName;
            SerializedProperty array = so.FindProperty("_costs");
            array.arraySize = costs.Length;
            for (int i = 0; i < costs.Length; i++)
            {
                array.GetArrayElementAtIndex(i).intValue = costs[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return upgrade;
        }

        private static CampaignCatalog CreateCampaign(LevelDefinition level, DifficultyProfile[] difficulties)
        {
            var catalog = LoadOrCreate<CampaignCatalog>($"{DataRoot}/Levels/CampaignCatalog.asset");
            var so = new SerializedObject(catalog);

            var nodes = new (string Name, Vector2 Position, LevelDefinition Level)[]
            {
                ("Landing Zone", new Vector2(0.1f, 0.25f), level),
                ("Glowing Forest", new Vector2(0.3f, 0.7f), null),
                ("Lava Caves", new Vector2(0.5f, 0.3f), null),
                ("Ancient Ruins", new Vector2(0.7f, 0.75f), null),
                ("The Nest", new Vector2(0.9f, 0.35f), null),
            };

            SerializedProperty nodeArray = so.FindProperty("_nodes");
            nodeArray.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                SerializedProperty element = nodeArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("DisplayName").stringValue = nodes[i].Name;
                element.FindPropertyRelative("MapPosition").vector2Value = nodes[i].Position;
                element.FindPropertyRelative("Level").objectReferenceValue = nodes[i].Level;
            }

            SerializedProperty difficultyArray = so.FindProperty("_difficulties");
            difficultyArray.arraySize = difficulties.Length;
            for (int i = 0; i < difficulties.Length; i++)
            {
                difficultyArray.GetArrayElementAtIndex(i).objectReferenceValue = difficulties[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static void CreateBootScene(CampaignCatalog campaign, UpgradeCatalog upgrades, UnlockTable unlocks, PlaytestSettings playtest, UiSkin skin)
        {
            // NewScene unloads assets nobody references yet, so remember the paths and load them again afterwards.
            string campaignPath = AssetDatabase.GetAssetPath(campaign);
            string upgradesPath = AssetDatabase.GetAssetPath(upgrades);
            string unlocksPath = AssetDatabase.GetAssetPath(unlocks);
            string playtestPath = AssetDatabase.GetAssetPath(playtest);
            string skinPath = AssetDatabase.GetAssetPath(skin);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrapper");
            var bootstrapper = go.AddComponent<Bootstrapper>();
            var so = new SerializedObject(bootstrapper);
            so.FindProperty("_inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            so.FindProperty("_campaign").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(campaignPath);
            so.FindProperty("_upgrades").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(upgradesPath);
            so.FindProperty("_unlockTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnlockTable>(unlocksPath);
            so.FindProperty("_playtestSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlaytestSettings>(playtestPath);
            so.FindProperty("_uiSkin").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UiSkin>(skinPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.Boot}.unity");
        }

        private static void CreateMenuScene<T>(string sceneName) where T : Component
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{sceneName}.unity");
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










