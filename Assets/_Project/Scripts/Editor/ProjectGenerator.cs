using System;
using System.Collections.Generic;
using System.Linq;
using Game.Boot;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
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
    public static partial class ProjectGenerator
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

        // Money a mining node holds; with three nodes a level, the harvest drones earn a bounded sum, not a stream.
        private const int MineStock = 140;

        [MenuItem("Tools/Generate/Scenes and Starter Data")]
        public static void GenerateAll()
        {
            CustomModelBuilder.BuildAll();

            TowerDefinition pulse = CreateTower("PulseRifle", "Pulse Rifle", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.3f, 0.7f, 1f),
                new[] { Lvl(50, 4f, 3.5f, 0.35f), Lvl(60, 6f, 3.8f, 0.3f), Lvl(90, 9f, 4.1f, 0.25f) }, 80f);
            TowerDefinition cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, 9f, 1.2f, new Color(1f, 0.8f, 0.3f),
                new[] { Lvl(100, 25f, 4f, 1.4f), Lvl(120, 40f, 4.3f, 1.2f), Lvl(180, 60f, 4.6f, 1f) }, 120f);
            TowerDefinition rocket = CreateTower("Rocket", "Rocket", TargetLayers.Air, 8f, 1.5f, new Color(0.9f, 0.9f, 1f),
                new[] { Lvl(120, 30f, 5.5f, 1.6f), Lvl(140, 45f, 5.9f, 1.4f), Lvl(200, 70f, 6.3f, 1.2f) }, 70f);

            // Level 0 cost is the price to rebuild a wrecked blockade. The gun arrives with the first upgrade.
            TowerDefinition blockade = CreateTower("Blockade", "Blockade", TargetLayers.Ground, 12f, 0f, new Color(0.55f, 0.65f, 0.8f),
                new[] { Lvl(60, 0f, 3.7f, 0.5f), Lvl(90, 5f, 3.9f, 0.45f, 300f), Lvl(150, 9f, 4.2f, 0.35f, 500f) }, 500f, blocksGround: true);

            Bestiary bestiary = CreateBestiary();

            var difficulties = new[]
            {
                CreateDifficulty("Easy", 0.8f, 0.8f, 1.2f, 1f, 0f, 0.7f),
                CreateDifficulty("Medium", 1.1f, 1.05f, 1f, 1.25f, 0.5f, 1f),
                CreateDifficulty("Hard", 1.6f, 1.4f, 0.85f, 1.6f, 1f, 1.35f),
                CreateDifficulty("Psycho", 2.4f, 1.9f, 0.7f, 2.5f, 2f, 1.8f),
            };

            List<DifficultyWaves> waveSets = BuildWaves("Landing", difficulties, LandingScript(bestiary), -1);

            VisualTheme theme = LoadOrCreate<VisualTheme>(ThemeAssetPath);
            ConfigureTheme(theme);
            LevelDefinition level = LoadOrCreate<LevelDefinition>(LevelAssetPath);
            HeroWeaponDefinition sword = CreateHeroWeapon("EnergySword", "Energy sword", HeroWeaponKind.Melee, HeroWeaponStyle.EnergySword, 18f, 1.5f, 0.7f, TargetLayers.Ground, new Color(0.3f, 0.95f, 1f));
            // Not carried yet: the ranged rig is ready for when guns become a reward. Point the hero at it to try it.
            CreateHeroWeapon("Blaster", "Blaster", HeroWeaponKind.Ranged, HeroWeaponStyle.Blaster, 9f, 3.2f, 0.35f, TargetLayers.Ground | TargetLayers.Air, new Color(1f, 0.6f, 0.25f));
            HeroDefinition hero = LoadOrCreate<HeroDefinition>($"{DataRoot}/Hero/Commander.asset");
            var heroProps = new SerializedObject(hero);
            heroProps.FindProperty("_weapon").objectReferenceValue = sword;
            heroProps.FindProperty("_repairCost").intValue = 30;
            heroProps.FindProperty("_color").colorValue = new Color(1f, 0.85f, 0.3f);
            heroProps.ApplyModifiedPropertiesWithoutUndo();
            ConfigureLevel(level, theme, new[] { pulse, cannon, rocket }, waveSets, cannon, blockade, hero);

            Roster roster = CreateRoster(pulse, cannon, rocket);
            PathObjectDefinition slowField = CreatePathObject("SlowField", "Slow field", PathObjectKind.SlowField, PathObjectPlacement.Road, TargetLayers.Ground, 40, 1.3f, 0.5f, 0f, 0.5f, 0f, 25f, new Color(0.4f, 0.8f, 1f));
            PathObjectDefinition mine = CreatePathObject("Mine", "Mine", PathObjectKind.Mine, PathObjectPlacement.Road, TargetLayers.Ground, 35, 1.4f, 0.6f, 90f, 1f, 0f, 0f, new Color(1f, 0.3f, 0.2f));
            PathObjectDefinition lavaBall = CreatePathObject("LavaBall", "Lava ball", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 70, 0.9f, 0.5f, 55f, 1f, 2.2f, 0f, new Color(1f, 0.45f, 0.1f));
            PathObjectDefinition barrel = CreatePathObject("ExplosiveBarrel", "Barrel", PathObjectKind.Mine, PathObjectPlacement.Beside, TargetLayers.Ground, 50, 2f, 1.1f, 140f, 1f, 0f, 0f, new Color(0.9f, 0.5f, 0.1f));
            PathObjectDefinition groundDrone = CreatePathObject("GroundDrone", "Drone", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 80, 1.1f, 0.5f, 30f, 1f, 1.1f, 0f, new Color(0.4f, 0.9f, 1f));
            PathObjectDefinition antiAir = CreatePathObject("AntiAirNet", "Air net", PathObjectKind.SlowField, PathObjectPlacement.Beside, TargetLayers.Air, 60, 2.2f, 0.5f, 0f, 0.4f, 0f, 30f, new Color(0.9f, 0.9f, 0.4f));
            // Three drones, one per job, bought with money in the level, so a drone competes with a tower for the same
            // purse: a harvester earns its price back in a few minutes of trips.
            DroneDefinition repairDrone = CreateDrone("RepairDrone", "Repair drone", DroneKind.Repair, 3.5f, 40f, 120, 3, new Color(0.4f, 0.9f, 1f), repair: 8f);
            DroneDefinition harvestDrone = CreateDrone("HarvestDrone", "Harvest drone", DroneKind.Harvest, 4f, 30f, 150, 2, new Color(1f, 0.85f, 0.3f), harvestSeconds: 4f, harvestValue: 10);
            DroneDefinition attackDrone = CreateDrone("AttackDrone", "Attack drone", DroneKind.Attack, 4.5f, 35f, 180, 3, new Color(1f, 0.45f, 0.35f), damage: 6f, fireInterval: 0.5f, range: 3f);
            CombineRecipe[] recipes =
            {
                CreateRecipe("Artillery", roster.Cannon, roster.Rocket, roster.Artillery, 150),
                CreateRecipe("StormRifle", roster.Pulse, roster.Tesla, roster.StormRifle, 120),
                CreateRecipe("CryoBeam", roster.Laser, roster.Cryo, roster.CryoBeam, 140),
            };
            ConfigureLevelExtras(level, new[] { slowField }, new CombineRecipe[0], repairDrone, harvestDrone, attackDrone);
            LevelDefinition[] worldLevels = CreateWorldLevels(
                level, roster, difficulties,
                bestiary,
                blockade, hero,
                slowField, mine, lavaBall, barrel, groundDrone, antiAir, recipes, repairDrone, harvestDrone, attackDrone);
            var allLevels = new LevelDefinition[worldLevels.Length + 1];
            allLevels[0] = level;
            worldLevels.CopyTo(allLevels, 1);
            foreach (LevelDefinition baked in allLevels)
            {
                BakeTerrain(baked);
            }


            var unlocks = LoadOrCreate<UnlockTable>(UnlockAssetPath);
            var unlockProps = new SerializedObject(unlocks);
            SerializedProperty ids = unlockProps.FindProperty("_startingTowerIds");
            ids.arraySize = 1;
            ids.GetArrayElementAtIndex(0).stringValue = pulse.Id;
            unlockProps.ApplyModifiedPropertiesWithoutUndo();

            var debug = LoadOrCreate<PlaytestSettings>(DebugAssetPath);
            // Every tower wears a procedural model; the hand-made gatling stays on disk but is no longer used.
            ClearModel(pulse);
            SetModel(cannon, "turret_double", 1.4f);
            SetModel(rocket, "satelliteDish_large", 1.5f);
            SetShapes(roster, cannon, rocket, blockade);

            UpgradeCatalog upgradeCatalog = CreateUpgrades(roster.Standard);
            UiSkin skin = CreateSkin();
            CampaignCatalog campaign = CreateCampaign(allLevels, difficulties);
            CreateMenuStage(worldLevels, bestiary);

            CreateStory(campaign);
            CreateSfxLibrary();
            CreateBootScene(campaign, upgradeCatalog, unlocks, debug, skin, CreateShaderKeepers());
            CreateLevelScene();
            CreateMainMenuScene();
            CreateMenuScene<IntroScreen>(SceneNames.Intro);
            CreateMenuScene<WorldMapScreen>(SceneNames.WorldMap);
            CreateMenuScene<UpgradeScreen>(SceneNames.Base);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Boot}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Intro}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.MainMenu}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.WorldMap}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Level}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Base}.unity", true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("Generated level 1 data and the Boot and Level scenes.");
        }

        private static GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}{name}.fbx");

        private static void ClearModel(Object asset)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("_model").objectReferenceValue = null;
            so.FindProperty("_keepModelColours").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HeroWeaponDefinition CreateHeroWeapon(string id, string displayName, HeroWeaponKind kind, HeroWeaponStyle style, float damage, float reach, float interval, TargetLayers targets, Color color)
        {
            var weapon = LoadOrCreate<HeroWeaponDefinition>($"{DataRoot}/Hero/Weapon_{id}.asset");
            var so = new SerializedObject(weapon);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").intValue = (int)kind;
            so.FindProperty("_style").intValue = (int)style;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_reach").floatValue = reach;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
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

        private static TowerDefinition CreateTower(string id, string displayName, TargetLayers targets, float projectileSpeed, float splash, Color color, TowerLevel[] levels, float health, bool blocksGround = false, float slowFactor = 1f, float slowSeconds = 0f, bool support = false, float rangeBonus = 0f, float rateBonus = 0f)
        {
            var tower = LoadOrCreate<TowerDefinition>($"{DataRoot}/Towers/{id}.asset");
            var so = new SerializedObject(tower);
            so.FindProperty("_blocksGroundEnemies").boolValue = blocksGround;
            so.FindProperty("_slowFactor").floatValue = slowFactor;
            so.FindProperty("_slowSeconds").floatValue = slowSeconds;
            so.FindProperty("_isSupport").boolValue = support;
            so.FindProperty("_supportRangeBonus").floatValue = rangeBonus;
            so.FindProperty("_supportRateBonus").floatValue = rateBonus;
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

        private static DifficultyProfile CreateDifficulty(string id, float health, float count, float interval, float reward, float power, float structureDamage)
        {
            var profile = LoadOrCreate<DifficultyProfile>($"{DataRoot}/Difficulty/{id}.asset");
            var so = new SerializedObject(profile);
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_healthMultiplier").floatValue = health;
            so.FindProperty("_countMultiplier").floatValue = count;
            so.FindProperty("_spawnIntervalMultiplier").floatValue = interval;
            so.FindProperty("_rewardMultiplier").floatValue = reward;
            so.FindProperty("_powerCurrencyMultiplier").floatValue = power;
            so.FindProperty("_structureDamageMultiplier").floatValue = structureDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static void ConfigureLevel(LevelDefinition level, VisualTheme theme, TowerDefinition[] towers, List<DifficultyWaves> waveSets, TowerDefinition rewardTower, TowerDefinition blockade, HeroDefinition hero)
        {
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = "Landing Zone";
            so.FindProperty("_regionName").stringValue = "Northern Plain";
            so.FindProperty("_briefing").stringValue = "Our first foothold: the lander sits on open grey plain and the scouts are already testing the fence. Hold the base. Everything else comes later.";
            so.FindProperty("_gridSize").vector2IntValue = LandingSize;
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_theme").objectReferenceValue = theme;
            SerializedProperty camera = so.FindProperty("_camera");
            camera.FindPropertyRelative("PitchDegrees").floatValue = 55f;
            camera.FindPropertyRelative("YawDegrees").floatValue = 0f;
            camera.FindPropertyRelative("FitMargin").floatValue = 0.04f;
            so.FindProperty("_startingMoney").intValue = 150;
            so.FindProperty("_baseHealth").intValue = 20;
            so.FindProperty("_mineStock").intValue = MineStock;
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

            var landingPaths = new List<(string Name, bool Flying, Vector2Int[] Waypoints)>
            {
                ("Road", false, LandingRoad),
                ("North air route", true, Cells(0, 14, 21, 3)),
                ("South air route", true, Cells(0, 0, 10, 5, 21, 3)),
            };
            WritePaths(so.FindProperty("_paths"), landingPaths.ToArray());
            so.FindProperty("_roadWidth").intValue = LandingRoadWidth;
            WriteCells(so.FindProperty("_plazaCells"), new Vector2Int[0]);
            so.FindProperty("_hero").objectReferenceValue = hero;

            // One blockade on the first leg of the road and one across the last leg.
            var blockadeCells = Cells(9, 12, 11, 3);

            // A lake that drains off the right edge, a pond below the road, and low ridges on the free corners.
            HashSet<Vector2Int> landingRoad = RoadCells(LandingSize, landingPaths, LandingRoadWidth, new Vector2Int[0]);
            TerrainPatch[] landingTerrain = Terrain(
                Area(TerrainKind.Water, 18, 5, 23, 9), Area(TerrainKind.Water, 7, 0, 10, 1), Area(TerrainKind.Water, 8, -4, 9, -1),
                Area(TerrainKind.Raised, 0, 4, 2, 6), Area(TerrainKind.Raised, 6, 9, 9, 10), Area(TerrainKind.Raised, 18, 13, 21, 14), Area(TerrainKind.Raised, 0, 0, 3, 1));
            // Tide bats rise out of the lake and the pond; the nests keep rocks and mines off their cells.
            ElementNest[] landingNests = { Nest(Element.Water, 20, 7), Nest(Element.Water, 8, 0) };
            Vector2Int[] landingReserved = blockadeCells.Concat(NestCells(landingNests)).ToArray();
            Vector2Int[] landingBlocked = ScatterBlocked(LandingSize, landingRoad, landingTerrain, landingReserved, 11, BlockedFraction);
            WriteCells(so.FindProperty("_blockedCells"), landingBlocked);
            WriteCells(so.FindProperty("_mineCells"), PickMineCells(LandingSize, landingRoad, landingTerrain, landingBlocked, landingReserved, 16));
            TerrainPatch[] landingKept = WriteTerrain(so.FindProperty("_terrain"), landingTerrain, landingRoad, blockadeCells);
            WriteNests(so.FindProperty("_nests"), landingNests, landingKept);
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
            ConfigureThemePalette(theme, new Palette
            {
                Background = new Color(0.02f, 0.03f, 0.06f),
                Ground = new Color(0.27f, 0.33f, 0.42f),
                Path = new Color(0.55f, 0.8f, 1f),
                Slot = new Color(0.2f, 0.55f, 1f),
                Base = new Color(0.3f, 0.85f, 1f),
                Light = new Color(0.75f, 0.85f, 1f),
                LightIntensity = 1.1f,
                Fog = new Color(0.03f, 0.04f, 0.08f),
                FogDensity = 0.01f,
                Bloom = 1.2f,
                Rock = new Color(0.42f, 0.48f, 0.6f),
                Accent = new Color(0.35f, 0.85f, 1f),
                Water = new Color(0.07f, 0.38f, 0.62f),
                Lava = new Color(1f, 0.4f, 0.1f),
                Sand = new Color(0.5f, 0.5f, 0.56f),
                Props = new[] { PropKind.Rock, PropKind.Boulder, PropKind.Crystal, PropKind.Frond, PropKind.Rock, PropKind.Crystal },
                Vein = 0.4f,
                AmbientRate = 9f,
                AmbientRise = 0.1f,
                Ambience = AmbienceKind.Wind,
                Plant = new Color(0.36f, 0.55f, 0.62f),
            });
        }

        private static void ConfigureThemePalette(VisualTheme theme, Palette p)
        {
            var so = new SerializedObject(theme);
            so.FindProperty("_backgroundColor").colorValue = p.Background;
            so.FindProperty("_groundColor").colorValue = p.Ground;
            so.FindProperty("_pathColor").colorValue = p.Path;
            so.FindProperty("_groundTile").objectReferenceValue = Model("terrain");
            so.FindProperty("_roadStraight").objectReferenceValue = Model("terrain_roadStraight");
            so.FindProperty("_roadCorner").objectReferenceValue = Model("terrain_roadCorner");
            so.FindProperty("_roadEnd").objectReferenceValue = Model("terrain_roadEnd");
            so.FindProperty("_roadCross").objectReferenceValue = Model("terrain_roadCross");
            so.FindProperty("_baseModel").objectReferenceValue = Model("machine_generatorLarge");
            so.FindProperty("_spawnModel").objectReferenceValue = Model("gate_simple");
            SetObjects(so.FindProperty("_blockedProps"), "rock_largeA", "rock_largeB", "rock_crystals", "meteor");
            SetObjects(so.FindProperty("_scenery"), "rocks_smallA", "rocks_smallB", "crater", "craterLarge", "rock_crystalsLargeA", "rock_largeA", "meteor");
            so.FindProperty("_slotColor").colorValue = p.Slot;
            so.FindProperty("_baseColor").colorValue = p.Base;
            so.FindProperty("_lightColor").colorValue = p.Light;
            so.FindProperty("_lightIntensity").floatValue = p.LightIntensity;
            so.FindProperty("_fogColor").colorValue = p.Fog;
            so.FindProperty("_fogDensity").floatValue = p.FogDensity;
            so.FindProperty("_bloomIntensity").floatValue = p.Bloom;
            so.FindProperty("_rockColor").colorValue = p.Rock;
            so.FindProperty("_accentColor").colorValue = p.Accent;
            so.FindProperty("_waterColor").colorValue = p.Water;
            so.FindProperty("_lavaColor").colorValue = p.Lava;
            so.FindProperty("_sandColor").colorValue = p.Sand;
            so.FindProperty("_veinStrength").floatValue = p.Vein;
            so.FindProperty("_ambientRate").floatValue = p.AmbientRate;
            so.FindProperty("_ambientRise").floatValue = p.AmbientRise;
            so.FindProperty("_ambience").enumValueIndex = (int)p.Ambience;
            so.FindProperty("_plantColor").colorValue = p.Plant;
            SerializedProperty props = so.FindProperty("_propKinds");
            props.arraySize = p.Props.Length;
            for (int i = 0; i < p.Props.Length; i++)
            {
                props.GetArrayElementAtIndex(i).enumValueIndex = (int)p.Props[i];
            }

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

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
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

            const string HeroText = "Improvements for your commander";
            all.Add(CreateUpgrade("Hero_Damage", "Blade damage +15% per level", UpgradeTarget.HeroDamage, new[] { 5, 10, 15 }, 0.15f, string.Empty, "Hero", UpgradeCurrency.ResearchPoints, HeroText));
            all.Add(CreateUpgrade("Hero_Health", "Armour +20% per level", UpgradeTarget.HeroHealth, new[] { 5, 10, 15 }, 0.2f, string.Empty, "Hero", UpgradeCurrency.ResearchPoints, HeroText));

            // Drones are bought with money inside a level; only their speed is a permanent upgrade, paid in power currency.
            const string DroneText = "Drones are bought from the base with money and work on their own: repair, harvest and attack. Their speed costs power.";
            all.Add(CreateUpgrade("Drone_Speed", "Drone speed +25% per level", UpgradeTarget.DroneSpeed, new[] { 4, 8, 12 }, 0.25f, string.Empty, "Drones", UpgradeCurrency.PowerCurrency, DroneText));

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
        private static UpgradeDefinition CreateUpgrade(string id, string displayName, UpgradeTarget target, int[] costs, float bonus, string towerId, string groupName, UpgradeCurrency currency = UpgradeCurrency.ResearchPoints, string description = "")
        {
            var upgrade = LoadOrCreate<UpgradeDefinition>($"{DataRoot}/Upgrades/{id}.asset");
            var so = new SerializedObject(upgrade);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_target").enumValueIndex = (int)target;
            so.FindProperty("_bonusPerLevel").floatValue = bonus;
            so.FindProperty("_towerId").stringValue = towerId;
            so.FindProperty("_groupName").stringValue = groupName;
            so.FindProperty("_currency").enumValueIndex = (int)currency;
            so.FindProperty("_groupDescription").stringValue = description;
            SerializedProperty array = so.FindProperty("_costs");
            array.arraySize = costs.Length;
            for (int i = 0; i < costs.Length; i++)
            {
                array.GetArrayElementAtIndex(i).intValue = costs[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return upgrade;
        }

        private const string SfxLibraryPath = DataRoot + "/Audio/SfxLibrary.asset";
        private const string KenneyAudio = "Assets/_Project/Audio/Kenney_SciFi";
        private const string LentikulaAudio = "Assets/_Project/Audio/Lentikula";
        private const string FreesoundAudio = "Assets/_Project/Audio/Freesound";

        // Which recorded clips (CC0, see CREDITS.md) stand in for which generated sound. Missing files leave a list empty.
        private static readonly (string Field, string Folder, string Pattern, int Count)[] SfxClips =
        {
            ("_pulseShots", LentikulaAudio, "Laser Rifle {0}.wav", 5),
            ("_heavyShots", KenneyAudio, "laserLarge_00{0}.ogg", 5),
            ("_rocketLaunches", KenneyAudio, "thrusterFire_00{0}.ogg", 5),
            ("_cryoShots", KenneyAudio, "laserRetro_00{0}.ogg", 5),
            ("_teslaShots", KenneyAudio, "forceField_00{0}.ogg", 5),
            ("_railShots", LentikulaAudio, "Laser Beam {0}.wav", 5),
            ("_explosions", KenneyAudio, "explosionCrunch_00{0}.ogg", 5),
            ("_bigExplosions", KenneyAudio, "lowFrequency_explosion_00{0}.ogg", 2),
            ("_structureHits", KenneyAudio, "impactMetal_00{0}.ogg", 5),
            ("_enemyDeaths", KenneyAudio, "slime_00{0}.ogg", 2),
            ("_baseHits", KenneyAudio, "explosionCrunch_00{0}.ogg", 5),
            ("_heroBlasters", LentikulaAudio, "Laser Pistol {0}.wav", 5),
            ("_bossLaserLoops", FreesoundAudio, "freesound_community-laser-weld-103309.mp3", 1),
            ("_bossAlarms", FreesoundAudio, "warning.mp3", 1),
        };

        // The library of recorded sounds: every list filled from the audio folders, numbered from 0 (Kenney) or 1 (Lentikula).
        private static void CreateSfxLibrary()
        {
            AssetDatabase.Refresh();
            var library = LoadOrCreate<SfxLibrary>(SfxLibraryPath);
            var so = new SerializedObject(library);
            foreach (var set in SfxClips)
            {
                var clips = new List<Object>();
                int first = set.Pattern.Contains("00{0}") ? 0 : 1;
                for (int i = 0; i < set.Count; i++)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{set.Folder}/{string.Format(set.Pattern, first + i)}");
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                }

                SetReferences(so.FindProperty(set.Field), clips.ToArray());
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }

        private const string StoryAssetPath = DataRoot + "/Story/Story.asset";

        // The intro told on first launch, as beats over the scenes the intro screen draws. Placeholder prose: edit the asset.
        private static void CreateStory(CampaignCatalog campaign)
        {
            var story = LoadOrCreate<StoryDefinition>(StoryAssetPath);
            var beats = new (string Text, StoryScene Scene, float Seconds)[]
            {
                ("Earth is running dry. Eleven light-years out, the survey ships found a world with everything we need.", StoryScene.Space, 6f),
                ("Metal in the crust, water in the valleys, energy in the lava fields. Nobody asked what else was down there.", StoryScene.Approach, 7f),
                ("The planet is alive. Every valley, every cave, every nest holds something, and all of it has noticed us.", StoryScene.Surface, 7f),
                ("We have landed. Hold the ground, take it one region at a time, and find out what this world is hiding.", StoryScene.Landing, 7f),
            };
            var so = new SerializedObject(story);
            SerializedProperty array = so.FindProperty("_introBeats");
            array.arraySize = beats.Length;
            for (int i = 0; i < beats.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Text").stringValue = beats[i].Text;
                element.FindPropertyRelative("Scene").enumValueIndex = (int)beats[i].Scene;
                element.FindPropertyRelative("Seconds").floatValue = beats[i].Seconds;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var catalog = new SerializedObject(campaign);
            catalog.FindProperty("_story").objectReferenceValue = story;
            catalog.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CampaignCatalog CreateCampaign(LevelDefinition[] levels, DifficultyProfile[] difficulties)
        {
            var catalog = LoadOrCreate<CampaignCatalog>($"{DataRoot}/Levels/CampaignCatalog.asset");
            var so = new SerializedObject(catalog);

            var nodes = new (string Name, Vector2 Position, LevelDefinition Level)[]
            {
                ("Landing Zone", new Vector2(0.1f, 0.25f), levels[0]),
                ("Glowing Forest", new Vector2(0.3f, 0.7f), levels[1]),
                ("Lava Caves", new Vector2(0.5f, 0.3f), levels[2]),
                ("Ancient Ruins", new Vector2(0.7f, 0.75f), levels[3]),
                ("The Nest", new Vector2(0.9f, 0.35f), levels[4]),
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

        private const string KeeperFolder = "Assets/_Project/Art/Materials/ShaderKeepers";

        // Materials saved with the keyword combinations the runtime materials use, so a player build keeps those
        // shader variants. They are referenced by the Boot scene and never drawn.
        private static string[] CreateShaderKeepers()
        {
            EnsureFolder(KeeperFolder);
            return new[]
            {
                KeeperMaterial("Lit_EmissionNormal", false, "_EMISSION", "_NORMALMAP"),
                KeeperMaterial("Lit_Transparent", true, "_EMISSION", "_SURFACE_TYPE_TRANSPARENT"),
                KeeperMaterial("Lit_TransparentFlat", true, "_EMISSION", "_SURFACE_TYPE_TRANSPARENT", "_SPECULARHIGHLIGHTS_OFF"),
                KeeperShader("ImpactRipple", "Game/ImpactRipple"),
            };
        }

        // A material on a hand-written shader, so the build keeps the shader the ripples find by name at runtime.
        private static string KeeperShader(string name, string shaderName)
        {
            string path = $"{KeeperFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            return path;
        }

        private static string KeeperMaterial(string name, bool transparent, params string[] keywords)
        {
            string path = $"{KeeperFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial);
                AssetDatabase.CreateAsset(material, path);
            }

            foreach (string keyword in keywords)
            {
                material.EnableKeyword(keyword);
            }

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);
            return path;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static void CreateBootScene(CampaignCatalog campaign, UpgradeCatalog upgrades, UnlockTable unlocks, PlaytestSettings playtest, UiSkin skin, string[] keeperPaths)
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
            so.FindProperty("_sfxLibrary").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SfxLibraryPath);
            so.FindProperty("_uiSkin").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UiSkin>(skinPath);
            SerializedProperty keepers = so.FindProperty("_shaderKeepers");
            keepers.arraySize = keeperPaths.Length;
            for (int i = 0; i < keeperPaths.Length; i++)
            {
                keepers.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(keeperPaths[i]);
            }

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

        private const string MenuStageAssetPath = DataRoot + "/Menu/MainMenuStage.asset";

        // The menu scene shows the themes of the world levels and a handful of its creatures.
        private static void CreateMenuStage(LevelDefinition[] worldLevels, Bestiary bestiary)
        {
            var stage = LoadOrCreate<MenuStageDefinition>(MenuStageAssetPath);
            var so = new SerializedObject(stage);
            so.FindProperty("_forestTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Forest);
            so.FindProperty("_lavaTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Lava);
            so.FindProperty("_ruinsTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Ruins);
            so.FindProperty("_nestTheme").objectReferenceValue = ThemeWith(worldLevels, AmbienceKind.Hive);
            SerializedProperty walkers = so.FindProperty("_nestCreatures");
            EnemyDefinition[] crawlers = { bestiary.Skitter, bestiary.Prowler, bestiary.Roller, bestiary.Stinger };
            walkers.arraySize = crawlers.Length;
            for (int i = 0; i < crawlers.Length; i++)
            {
                walkers.GetArrayElementAtIndex(i).objectReferenceValue = crawlers[i];
            }

            so.FindProperty("_nestGuardian").objectReferenceValue = bestiary.Queen;
            so.FindProperty("_flyer").objectReferenceValue = bestiary.Skyray;
            so.FindProperty("_waterCreature").objectReferenceValue = bestiary.Tidebat;
            so.FindProperty("_lavaCreature").objectReferenceValue = bestiary.Emberbat;
            so.FindProperty("_sandCreature").objectReferenceValue = bestiary.Dustbat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static VisualTheme ThemeWith(LevelDefinition[] levels, AmbienceKind ambience)
        {
            foreach (LevelDefinition level in levels)
            {
                if (level.Theme != null && level.Theme.Ambience == ambience)
                {
                    return level.Theme;
                }
            }

            return levels[0].Theme;
        }

        // Loaded by path: the earlier NewScene calls unload assets nothing references yet, which breaks a held reference.
        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(nameof(MainMenuScreen)).AddComponent<MainMenuScreen>();
            var stageGo = new GameObject(nameof(MenuStage));
            var menuStage = stageGo.AddComponent<MenuStage>();
            var so = new SerializedObject(menuStage);
            so.FindProperty("_stage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MenuStageDefinition>(MenuStageAssetPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.MainMenu}.unity");
        }        private static void CreateLevelScene()
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










