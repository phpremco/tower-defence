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

        // A downed hero is gone long enough to hurt: two minutes, shown as a countdown in the HUD.
        private const float HeroReviveSeconds = 120f;

        // The base is besieged like a gate, so it needs gate-sized health: a few gates' worth, grown by the armour upgrade.
        private const int BaseHealth = 600;
        private const float BaseHealthUpgradeStep = 100f;

        // The camera looks down a little less steeply and from a slight angle, so gates, towers and creatures show a
        // side as well as a top and read as things with height instead of symbols on a map.
        private const float CameraPitchDegrees = 44f;
        private const float CameraYawDegrees = 16f;

        // A battered tower still fires at three quarters, and mending a point of health costs less than a third of a coin,
        // so damage is a dent in the defence rather than its collapse.
        private const float DamagedPowerFloor = 0.75f;
        private const float RepairCostPerHealth = 0.3f;

        [MenuItem("Tools/Generate/Scenes and Starter Data")]
        public static void GenerateAll()
        {
            CustomModelBuilder.BuildAll();

            TowerDefinition pulse = CreateTower("PulseRifle", "Pulse Rifle", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.3f, 0.7f, 1f),
                new[] { Lvl(50, 5f, 3.5f, 0.35f), Lvl(60, 9f, 3.8f, 0.3f), Lvl(90, 15f, 4.1f, 0.25f) }, 150f);
            TowerDefinition cannon = CreateTower("Cannon", "Cannon", TargetLayers.Ground, 9f, 1.2f, new Color(1f, 0.8f, 0.3f),
                new[] { Lvl(100, 25f, 4f, 1.4f), Lvl(120, 48f, 4.3f, 1.2f), Lvl(180, 85f, 4.6f, 1f) }, 220f);
            TowerDefinition rocket = CreateTower("Rocket", "Rocket", TargetLayers.Air, 8f, 1.5f, new Color(0.9f, 0.9f, 1f),
                new[] { Lvl(120, 30f, 5.5f, 1.6f), Lvl(140, 55f, 5.9f, 1.4f), Lvl(200, 90f, 6.3f, 1.2f) }, 130f);

            // Level 0 cost is the price to rebuild a wrecked blockade. The gun arrives with the first upgrade.
            TowerDefinition blockade = CreateTower("Blockade", "Blockade", TargetLayers.Ground, 12f, 0f, new Color(0.55f, 0.65f, 0.8f),
                new[] { Lvl(60, 0f, 3.7f, 0.5f), Lvl(90, 6f, 3.9f, 0.45f, 300f), Lvl(150, 11f, 4.2f, 0.35f, 500f) }, 500f, blocksGround: true);

            // The base's own gun: unarmed at base weapon level zero, one or two of these beside the road once bought.
            TowerDefinition baseTurret = CreateTower("BaseTurret", "Base Turret", TargetLayers.Ground | TargetLayers.Air, 14f, 0f, new Color(0.45f, 0.85f, 1f),
                new[] { Lvl(0, 7f, 4.2f, 0.4f), Lvl(70, 12f, 4.5f, 0.35f), Lvl(110, 19f, 4.8f, 0.3f) }, 220f, baseDefence: true);
            SetShape(baseTurret, TowerShape.PulseRifle);

            Bestiary bestiary = CreateBestiary();

            var difficulties = new[]
            {
                CreateDifficulty("Easy", 0.76f, 0.8f, 1.2f, 1f, 0f, 0.6f),
                CreateDifficulty("Medium", 1f, 1f, 1f, 1.45f, 0.5f, 0.75f),
                CreateDifficulty("Hard", 1.3f, 1.2f, 0.85f, 2f, 1f, 1f),
                CreateDifficulty("Psycho", 1.8f, 1.5f, 0.7f, 3.2f, 2f, 1.4f),
            };

            List<DifficultyWaves> waveSets = BuildWaves("Landing", difficulties, LandingScript(bestiary), -1);

            VisualTheme theme = LoadOrCreate<VisualTheme>(ThemeAssetPath);
            ConfigureTheme(theme);
            LevelDefinition level = LoadOrCreate<LevelDefinition>(LevelAssetPath);
            HeroWeaponDefinition sword = CreateHeroWeapon("EnergySword", "Energy sword", HeroWeaponKind.Melee, HeroWeaponStyle.EnergySword, 18f, 1.5f, 0.7f, TargetLayers.Ground, new Color(0.3f, 0.95f, 1f));
            // The sidearm: the stats of a level-one pulse rifle, so the hero is worth one simple turret at range.
            HeroWeaponDefinition blaster = CreateHeroWeapon("Blaster", "Blaster", HeroWeaponKind.Ranged, HeroWeaponStyle.Blaster, 4f, 3.5f, 0.35f, TargetLayers.Ground | TargetLayers.Air, new Color(1f, 0.6f, 0.25f));
            HeroDefinition hero = LoadOrCreate<HeroDefinition>($"{DataRoot}/Hero/Commander.asset");
            var heroProps = new SerializedObject(hero);
            heroProps.FindProperty("_weapon").objectReferenceValue = sword;
            heroProps.FindProperty("_rangedWeapon").objectReferenceValue = blaster;
            heroProps.FindProperty("_repairCost").intValue = 30;
            heroProps.FindProperty("_reviveSeconds").floatValue = HeroReviveSeconds;
            heroProps.FindProperty("_color").colorValue = new Color(1f, 0.85f, 0.3f);
            heroProps.ApplyModifiedPropertiesWithoutUndo();
            ConfigureLevel(level, theme, new[] { pulse, cannon, rocket }, waveSets, cannon, blockade, hero);

            Roster roster = CreateRoster(pulse, cannon, rocket);
            PathObjectDefinition slowField = CreatePathObject("SlowField", "Slow field", PathObjectKind.SlowField, PathObjectPlacement.Road, TargetLayers.Ground, 40, 1.3f, 0.5f, 0f, 0.5f, 0f, 25f, new Color(0.4f, 0.8f, 1f), PathObjectEffect.Slow, "A field of cold laid on the road. Everything that walks through it moves at half speed, for as long as the field lasts.");
            PathObjectDefinition mine = CreatePathObject("Mine", "Mine", PathObjectKind.Mine, PathObjectPlacement.Road, TargetLayers.Ground, 35, 1.4f, 0.6f, 140f, 1f, 0f, 0f, new Color(1f, 0.3f, 0.2f), PathObjectEffect.Trap, "Buried in the road. Goes off once, under the first enemy that steps on it, and hurts everything close by.");
            PathObjectDefinition lavaBall = CreatePathObject("LavaBall", "Lava ball", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 70, 0.9f, 0.5f, 55f, 1f, 2.2f, 0f, new Color(1f, 0.45f, 0.1f), PathObjectEffect.Roll, "Dropped on the road, it rolls on towards the base and burns every walker it touches along the way.");
            PathObjectDefinition barrel = CreatePathObject("ExplosiveBarrel", "Barrel", PathObjectKind.Mine, PathObjectPlacement.Beside, TargetLayers.Ground, 50, 2f, 1.1f, 200f, 1f, 0f, 0f, new Color(0.9f, 0.5f, 0.1f), PathObjectEffect.Blast, "Stands beside the road. When the enemy passes close it bursts, and the blast reaches far across the road.");
            PathObjectDefinition groundDrone = CreatePathObject("GroundDrone", "Ground drone", PathObjectKind.Roller, PathObjectPlacement.Road, TargetLayers.Ground, 80, 1.1f, 0.5f, 30f, 1f, 1.1f, 0f, new Color(0.4f, 0.9f, 1f), PathObjectEffect.Patrol, "Drives down the road at a steady pace and rams every walker it meets, dealing damage as long as it runs.");
            PathObjectDefinition antiAir = CreatePathObject("AntiAirNet", "Air net", PathObjectKind.SlowField, PathObjectPlacement.Beside, TargetLayers.Air, 60, 2.2f, 0.5f, 0f, 0.4f, 0f, 30f, new Color(0.9f, 0.9f, 0.4f), PathObjectEffect.AirNet, "A net of energy beside the road. Flyers passing over it are caught and slowed while it holds.");
            // Three drones, one per job, bought with money in the level, so a drone competes with a tower for the same
            // purse: a harvester earns its price back in a few minutes of trips.
            DroneDefinition repairDrone = CreateDrone("RepairDrone", "Repair drone", DroneKind.Repair, 3.5f, 40f, 120, 3, new Color(0.4f, 0.9f, 1f), repair: 8f, skin: CreatureSkin.Kind.Plate, accent: new Color(1f, 0.22f, 0.28f));
            DroneDefinition harvestDrone = CreateDrone("HarvestDrone", "Harvest drone", DroneKind.Harvest, 4f, 30f, 150, 2, new Color(1f, 0.85f, 0.3f), harvestSeconds: 4f, harvestValue: 10, skin: CreatureSkin.Kind.Alloy, accent: new Color(1f, 0.75f, 0.1f));
            DroneDefinition attackDrone = CreateDrone("AttackDrone", "Attack drone", DroneKind.Attack, 4.5f, 35f, 180, 3, new Color(1f, 0.45f, 0.35f), damage: 6f, fireInterval: 0.5f, range: 3f, skin: CreatureSkin.Kind.Plate, accent: new Color(0.92f, 0.95f, 1f));
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
            foreach (LevelDefinition armed in allLevels)
            {
                var armedProps = new SerializedObject(armed);
                armedProps.FindProperty("_baseTurret").objectReferenceValue = baseTurret;
                armedProps.ApplyModifiedPropertiesWithoutUndo();
            }

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

            UpgradeCatalog upgradeCatalog = CreateUpgrades(roster.Standard, new[] { repairDrone, harvestDrone, attackDrone });
            UiSkin skin = CreateSkin();
            CampaignCatalog campaign = CreateCampaign(allLevels, difficulties);
            CreateMenuStage(worldLevels, bestiary);

            CreateStory(campaign);
            ConfigureRenderPipeline();
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

        private static TowerLevel Lvl(int cost, float damage, float range, float interval, float healthBonus = 0f, float slowFactor = 0f, float slowSeconds = 0f) =>
            new TowerLevel { Cost = cost, Damage = damage, Range = range, FireInterval = interval, HealthBonus = healthBonus, SlowFactor = slowFactor, SlowSeconds = slowSeconds };

        private static void SetShape(TowerDefinition tower, TowerShape shape)
        {
            var so = new SerializedObject(tower);
            so.FindProperty("_shape").enumValueIndex = (int)shape;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TowerDefinition CreateTower(string id, string displayName, TargetLayers targets, float projectileSpeed, float splash, Color color, TowerLevel[] levels, float health, bool blocksGround = false, float slowFactor = 1f, float slowSeconds = 0f, bool support = false, float rangeBonus = 0f, float rateBonus = 0f, bool baseDefence = false)
        {
            var tower = LoadOrCreate<TowerDefinition>($"{DataRoot}/Towers/{id}.asset");
            var so = new SerializedObject(tower);
            so.FindProperty("_blocksGroundEnemies").boolValue = blocksGround;
            so.FindProperty("_isBaseDefence").boolValue = baseDefence;
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
            so.FindProperty("_damagedPowerFloor").floatValue = DamagedPowerFloor;
            so.FindProperty("_repairCostPerHealth").floatValue = RepairCostPerHealth;
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
                element.FindPropertyRelative("SlowFactor").floatValue = levels[i].SlowFactor;
                element.FindPropertyRelative("SlowSeconds").floatValue = levels[i].SlowSeconds;
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
            camera.FindPropertyRelative("PitchDegrees").floatValue = CameraPitchDegrees;
            camera.FindPropertyRelative("YawDegrees").floatValue = CameraYawDegrees;
            camera.FindPropertyRelative("FitMargin").floatValue = 0.04f;
            so.FindProperty("_startingMoney").intValue = 200;
            so.FindProperty("_baseHealth").intValue = BaseHealth;
            so.FindProperty("_mineStock").intValue = MineStock;
            SerializedProperty stars = so.FindProperty("_starBaseHealthFractions");
            stars.arraySize = 2;
            stars.GetArrayElementAtIndex(0).floatValue = 0.5f;
            stars.GetArrayElementAtIndex(1).floatValue = 0.9f;

            SerializedProperty reward = so.FindProperty("_reward");
            reward.FindPropertyRelative("ResearchPoints").intValue = 20;
            reward.FindPropertyRelative("PowerCurrency").intValue = 6;
            reward.FindPropertyRelative("NewTower").objectReferenceValue = rewardTower;
            reward.FindPropertyRelative("Source").intValue = (int)UnlockSource.HomePlanetIntel;
            reward.FindPropertyRelative("Story").stringValue = "The home planet sent schematics for a heavy cannon.";

            var landingPaths = new List<(string Name, bool Flying, Vector2Int[] Waypoints)>
            {
                ("Road", false, LandingRoad),
                ("North air route", true, Cells(0, 17, 16, 14, 31, 3)),
                ("South air route", true, Cells(0, 0, 14, 4, 31, 3)),
            };
            WritePaths(so.FindProperty("_paths"), landingPaths.ToArray());
            so.FindProperty("_roadWidth").intValue = LandingRoadWidth;
            WriteCells(so.FindProperty("_plazaCells"), new Vector2Int[0]);
            so.FindProperty("_hero").objectReferenceValue = hero;

            // One blockade on the first leg of the road and one across the last leg.
            var blockadeCells = Cells(7, 16, 11, 8, 24, 13);

            // Two lakes caught inside the switchbacks, a pond in the north-west corner and ridges on the free corners.
            HashSet<Vector2Int> landingRoad = RoadCells(LandingSize, landingPaths, LandingRoadWidth, new Vector2Int[0]);
            TerrainPatch[] landingTerrain = Terrain(
                Area(TerrainKind.Water, 15, 10, 18, 13), Area(TerrainKind.Water, 5, 13, 8, 14), Area(TerrainKind.Water, 22, 3, 25, 6), Area(TerrainKind.Water, 23, -3, 24, 2),
                Area(TerrainKind.Raised, 0, 0, 2, 2), Area(TerrainKind.Raised, 22, 16, 27, 17), Area(TerrainKind.Raised, 0, 13, 1, 14), Area(TerrainKind.Raised, 30, 8, 31, 11));
            // Tide bats rise out of the lake and the pond; the nests keep rocks and mines off their cells.
            ElementNest[] landingNests = { Nest(Element.Water, 15, 12), Nest(Element.Water, 6, 13) };
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

        // A grading tint shifts hue only: scaled so its brightest channel is one, it never darkens the picture.
        private static Color Hue(Color tint)
        {
            float peak = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b), 0.001f);
            return new Color(tint.r / peak, tint.g / peak, tint.b / peak, 1f);
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
            so.FindProperty("_sunPitch").floatValue = p.SunPitch > 0f ? p.SunPitch : 42f;
            so.FindProperty("_sunYaw").floatValue = p.SunYaw != 0f ? p.SunYaw : -35f;
            so.FindProperty("_skyColor").colorValue = p.Sky == default ? Color.Lerp(p.Fog, p.Light, 0.6f) : p.Sky;
            so.FindProperty("_rimColor").colorValue = p.Rim == default ? p.Accent : p.Rim;
            so.FindProperty("_contrast").floatValue = p.Contrast != 0f ? p.Contrast : 12f;
            so.FindProperty("_saturation").floatValue = p.Saturation != 0f ? p.Saturation : 8f;
            so.FindProperty("_shadowTint").colorValue = Hue(p.ShadowTint == default ? Color.Lerp(Color.white, p.Fog, 0.5f) : p.ShadowTint);
            so.FindProperty("_highlightTint").colorValue = Hue(p.HighlightTint == default ? Color.Lerp(Color.white, p.Light, 0.4f) : p.HighlightTint);
            so.FindProperty("_rockColor").colorValue = p.Rock;
            so.FindProperty("_accentColor").colorValue = p.Accent;
            so.FindProperty("_waterColor").colorValue = p.Water;
            // Older palettes name no ice or bog: they keep a sensible default so a stray patch still draws.
            so.FindProperty("_iceColor").colorValue = p.Ice == default ? new Color(0.75f, 0.9f, 1f) : p.Ice;
            so.FindProperty("_bogColor").colorValue = p.Bog == default ? new Color(0.18f, 0.24f, 0.12f) : p.Bog;
            so.FindProperty("_spawnColor").colorValue = p.Spawn == default ? new Color(1f, 0.4f, 0.25f) : p.Spawn;
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

        private static UpgradeCatalog CreateUpgrades(TowerDefinition[] towers, DroneDefinition[] drones)
        {
            // Display names stay short: the upgrade screen shows the value before and after next to them.
            var all = new List<UpgradeDefinition>();
            foreach (TowerDefinition tower in towers)
            {
                all.Add(CreateUpgrade($"{tower.Id}_Damage", "Damage", UpgradeTarget.TowerDamage, new[] { 5, 10, 15 }, 0.1f, tower.Id, tower.DisplayName));
                all.Add(CreateUpgrade($"{tower.Id}_Range", "Range", UpgradeTarget.TowerRange, new[] { 5, 10, 15 }, 0.05f, tower.Id, tower.DisplayName));
                all.Add(CreateUpgrade($"{tower.Id}_FireRate", "Fire rate", UpgradeTarget.TowerFireRate, new[] { 6, 12, 18 }, 0.1f, tower.Id, tower.DisplayName));
            }

            const string BaseText = "The base and the gates that hold the roads";
            all.Add(CreateUpgrade("BaseHealth", "Base armour", UpgradeTarget.BaseHealth, new[] { 4, 8, 12 }, BaseHealthUpgradeStep, string.Empty, "Base", UpgradeCurrency.ResearchPoints, BaseText));
            all.Add(CreateUpgrade("Base_Weapon", "Base turrets", UpgradeTarget.BaseWeapon, new[] { 12, 20, 32 }, 1f, string.Empty, "Base", UpgradeCurrency.ResearchPoints, BaseText));
            all.Add(CreateUpgrade("BlockadeHealth", "Gate strength", UpgradeTarget.BlockadeHealth, new[] { 4, 8, 12 }, 0.2f, string.Empty, "Base", UpgradeCurrency.ResearchPoints, BaseText));

            const string HeroText = "The commander who holds the gates";
            all.Add(CreateUpgrade("Hero_Damage", "Blade damage", UpgradeTarget.HeroDamage, new[] { 5, 10, 15 }, 0.15f, string.Empty, "Commander", UpgradeCurrency.ResearchPoints, HeroText));
            all.Add(CreateUpgrade("Hero_Health", "Armour", UpgradeTarget.HeroHealth, new[] { 5, 10, 15 }, 0.2f, string.Empty, "Commander", UpgradeCurrency.ResearchPoints, HeroText));

            // Drones are bought with money inside a level; each type has its own permanent path, paid in power currency.
            foreach (DroneDefinition drone in drones)
            {
                string id = drone.name;
                string group = drone.DisplayName;
                all.Add(CreateUpgrade($"{id}_Speed", "Flight speed", UpgradeTarget.DroneSpeed, new[] { 3, 6, 9 }, 0.2f, string.Empty, group, UpgradeCurrency.PowerCurrency, DroneText(drone), id));
                switch (drone.Kind)
                {
                    case DroneKind.Repair:
                        all.Add(CreateUpgrade($"{id}_Output", "Repair rate", UpgradeTarget.DroneOutput, new[] { 4, 8, 12 }, 0.25f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        all.Add(CreateUpgrade($"{id}_Armour", "Armour", UpgradeTarget.DroneArmour, new[] { 3, 6, 9 }, 0.3f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        break;
                    case DroneKind.Harvest:
                        all.Add(CreateUpgrade($"{id}_Output", "Yield per trip", UpgradeTarget.DroneOutput, new[] { 4, 8, 12 }, 0.25f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        all.Add(CreateUpgrade($"{id}_Count", "Extra drone", UpgradeTarget.DroneCount, new[] { 6, 12 }, 1f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        break;
                    default:
                        all.Add(CreateUpgrade($"{id}_Output", "Damage", UpgradeTarget.DroneOutput, new[] { 4, 8, 12 }, 0.15f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        all.Add(CreateUpgrade($"{id}_Tempo", "Fire rate", UpgradeTarget.DroneTempo, new[] { 4, 8, 12 }, 0.15f, string.Empty, group, UpgradeCurrency.PowerCurrency, string.Empty, id));
                        break;
                }
            }

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
        private static string DroneText(DroneDefinition drone)
        {
            switch (drone.Kind)
            {
                case DroneKind.Repair:
                    return "Bought with money in the level. Flies to the most damaged tower, gate or base and repairs it.";
                case DroneKind.Harvest:
                    return "Bought with money in the level. Mines the ore nodes and carries the money home.";
                default:
                    return "Bought with money in the level. Hunts the enemy furthest down the road and shoots it from the air.";
            }
        }

        private static UpgradeDefinition CreateUpgrade(string id, string displayName, UpgradeTarget target, int[] costs, float bonus, string towerId, string groupName, UpgradeCurrency currency = UpgradeCurrency.ResearchPoints, string description = "", string droneId = "")
        {
            var upgrade = LoadOrCreate<UpgradeDefinition>($"{DataRoot}/Upgrades/{id}.asset");
            var so = new SerializedObject(upgrade);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_droneId").stringValue = droneId;
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

            // The continents of the planet. Only the first holds levels for now; the others are land still to be
            // explored, placed so every climate has a home: ice near the poles, swamp and jungle near the equator.
            var regions = new (string Name, float Longitude, float Latitude, float Radius, Color Colour)[]
            {
                ("Landfall", -45f, 30f, 27f, new Color(0.38f, 0.52f, 0.34f)),
                ("Ember Reach", 12f, -8f, 24f, new Color(0.58f, 0.34f, 0.22f)),
                ("Verdant Belt", 64f, 16f, 26f, new Color(0.26f, 0.5f, 0.3f)),
                ("Southern Wastes", -28f, -46f, 24f, new Color(0.6f, 0.54f, 0.36f)),
                ("Polar Crown", 122f, 62f, 22f, new Color(0.7f, 0.8f, 0.88f)),
                ("Deep Mire", 152f, -20f, 24f, new Color(0.24f, 0.38f, 0.26f)),
                ("Shattered Isles", -135f, -6f, 22f, new Color(0.42f, 0.36f, 0.52f)),
            };

            SerializedProperty regionArray = so.FindProperty("_regions");
            regionArray.arraySize = regions.Length;
            for (int i = 0; i < regions.Length; i++)
            {
                SerializedProperty element = regionArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("DisplayName").stringValue = regions[i].Name;
                element.FindPropertyRelative("Longitude").floatValue = regions[i].Longitude;
                element.FindPropertyRelative("Latitude").floatValue = regions[i].Latitude;
                element.FindPropertyRelative("RadiusDegrees").floatValue = regions[i].Radius;
                element.FindPropertyRelative("Colour").colorValue = regions[i].Colour;
            }

            // Longitude and latitude on the globe, all inside Landfall. The campaign winds from the landing site on
            // the south-west coast up to the frozen north and back down into the marsh, so the route reads as one
            // journey across the continent.
            var nodes = new (string Name, float Longitude, float Latitude, int Region, LevelDefinition Level)[]
            {
                ("Landing Zone", -64f, 14f, 0, levels[0]),
                ("Glowing Forest", -54f, 24f, 0, levels[1]),
                ("Lava Caves", -42f, 16f, 0, levels[2]),
                ("Ancient Ruins", -32f, 26f, 0, levels[3]),
                ("The Nest", -30f, 38f, 0, levels[4]),
                ("Frozen Shelf", -42f, 50f, 0, levels[5]),
                ("Black Marsh", -56f, 38f, 0, levels[6]),
            };

            SerializedProperty nodeArray = so.FindProperty("_nodes");
            nodeArray.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                SerializedProperty element = nodeArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("DisplayName").stringValue = nodes[i].Name;
                element.FindPropertyRelative("Longitude").floatValue = nodes[i].Longitude;
                element.FindPropertyRelative("Latitude").floatValue = nodes[i].Latitude;
                element.FindPropertyRelative("Region").intValue = nodes[i].Region;
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
                KeeperMaterial("Lit_EmissionNormalDetail", false, "_EMISSION", "_NORMALMAP", "_DETAIL_MULX2"),
                KeeperMaterial("Lit_Transparent", true, "_EMISSION", "_SURFACE_TYPE_TRANSPARENT"),
                KeeperMaterial("Lit_TransparentFlat", true, "_EMISSION", "_SURFACE_TYPE_TRANSPARENT", "_SPECULARHIGHLIGHTS_OFF"),
                KeeperShader("ImpactRipple", "Game/ImpactRipple"),
                KeeperShader("UnitLit", Game.Core.SurfaceMaterials.UnitShaderName),
                KeeperShader("UnitLit_Emission", Game.Core.SurfaceMaterials.UnitShaderName, "_EMISSION"),
                KeeperShader("UnitLit_Normal", Game.Core.SurfaceMaterials.UnitShaderName, "_NORMALMAP"),
                KeeperShader("UnitLit_EmissionNormal", Game.Core.SurfaceMaterials.UnitShaderName, "_EMISSION", "_NORMALMAP"),
            };
        }

        // A material on a hand-written shader, so the build keeps the shader the ripples find by name at runtime.
        private static string KeeperShader(string name, string shaderName, params string[] keywords)
        {
            string path = $"{KeeperFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            foreach (string keyword in keywords)
            {
                material.EnableKeyword(keyword);
            }

            EditorUtility.SetDirty(material);
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










