using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the rest of the first world: more towers, bosses, path objects, drones, recipes and levels 2 to 5.</summary>
    public static partial class ProjectGenerator
    {
        private const float StartingMoneyStep = 30f;

        private struct Palette
        {
            public Color Background;
            public Color Ground;
            public Color Path;
            public Color Slot;
            public Color Base;
            public Color Light;
            public float LightIntensity;
            public Color Fog;
            public float FogDensity;
            public float Bloom;
            public Color Rock;
            public Color Accent;
            public Color Water;
            public Color Lava;
            public Color Sand;
            public PropKind[] Props;
            public float Vein;
            public float AmbientRate;
            public float AmbientRise;
        }

        private sealed class LevelSpec
        {
            public string Key;
            public string Name;
            public Palette Palette;
            public Vector2Int[] Road;
            public Vector2Int[] SecondRoad;
            public Vector2Int[] NorthAir;
            public Vector2Int[] SouthAir;
                        public Vector2Int[] BlockadeCells;
            public TerrainPatch[] Terrain;
            public Vector2Int Size;
            public int BlockedSeed;
            public int Money;
            public int BaseHealth;
            public float CountScale;
            public float HealthScale;
            public EnemyDefinition Boss;
            public TowerDefinition RewardTower;
            public UnlockSource Source;
            public string Story;
            public int Research;
            public int Power;
            public PathObjectDefinition[] PathObjects;
            public CombineRecipe[] Recipes;
        }

        private sealed class Roster
        {
            public TowerDefinition Pulse;
            public TowerDefinition Cannon;
            public TowerDefinition Laser;
            public TowerDefinition Cryo;
            public TowerDefinition Rocket;
            public TowerDefinition Tesla;
            public TowerDefinition Railgun;
            public TowerDefinition Support;
            public TowerDefinition Artillery;
            public TowerDefinition StormRifle;
            public TowerDefinition CryoBeam;

            public TowerDefinition[] Standard => new[] { Pulse, Cannon, Laser, Cryo, Rocket, Tesla, Railgun, Support };
        }

        private static Roster CreateRoster(TowerDefinition pulse, TowerDefinition cannon, TowerDefinition rocket)
        {
            var roster = new Roster { Pulse = pulse, Cannon = cannon, Rocket = rocket };

            // Fast single-target damage on the ground; a stand-in for a continuous beam until it has its own effect.
            roster.Laser = CreateTower("Laser", "Laser", TargetLayers.Ground, 40f, 0f, new Color(1f, 0.35f, 0.45f),
                new[] { Lvl(110, 6f, 3.4f, 0.2f), Lvl(130, 9f, 3.6f, 0.18f), Lvl(190, 13f, 3.9f, 0.15f) }, 90f);

            roster.Cryo = CreateTower("Cryo", "Cryo", TargetLayers.Ground | TargetLayers.Air, 12f, 0f, new Color(0.55f, 0.95f, 1f),
                new[] { Lvl(90, 2f, 3.2f, 0.5f), Lvl(110, 3f, 3.5f, 0.45f), Lvl(160, 4f, 3.8f, 0.4f) }, 80f, slowFactor: 0.5f, slowSeconds: 2f);

            // Lightning is approximated by a small splash, so it works well on groups.
            roster.Tesla = CreateTower("Tesla", "Tesla", TargetLayers.Ground, 40f, 1.3f, new Color(0.7f, 0.5f, 1f),
                new[] { Lvl(140, 10f, 3f, 0.5f), Lvl(160, 15f, 3.2f, 0.45f), Lvl(230, 22f, 3.5f, 0.4f) }, 90f);

            roster.Railgun = CreateTower("Railgun", "Railgun", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(1f, 1f, 1f),
                new[] { Lvl(220, 80f, 5.5f, 2.2f), Lvl(260, 120f, 6f, 2f), Lvl(360, 180f, 6.5f, 1.8f) }, 100f);

            roster.Support = CreateTower("Support", "Support", TargetLayers.Ground, 1f, 0f, new Color(0.4f, 1f, 0.6f),
                new[] { Lvl(120, 0f, 2.5f, 1f), Lvl(140, 0f, 3f, 1f), Lvl(200, 0f, 3.5f, 1f) }, 80f, support: true, rangeBonus: 0.15f, rateBonus: 0.2f);

            // Hybrids come from recipes, so their first level costs nothing to build: the recipe has the price.
            roster.Artillery = CreateTower("Artillery", "Artillery", TargetLayers.Ground | TargetLayers.Air, 8f, 1.8f, new Color(1f, 0.6f, 0.2f),
                new[] { Lvl(0, 60f, 4.6f, 1.6f), Lvl(120, 90f, 5f, 1.4f), Lvl(180, 130f, 5.4f, 1.2f) }, 140f);

            roster.StormRifle = CreateTower("StormRifle", "Storm Rifle", TargetLayers.Ground | TargetLayers.Air, 20f, 0.8f, new Color(0.6f, 0.8f, 1f),
                new[] { Lvl(0, 14f, 3.6f, 0.2f), Lvl(110, 20f, 3.9f, 0.18f), Lvl(170, 28f, 4.2f, 0.15f) }, 110f);

            roster.CryoBeam = CreateTower("CryoBeam", "Cryo Beam", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(0.7f, 1f, 1f),
                new[] { Lvl(0, 8f, 3.8f, 0.15f), Lvl(110, 12f, 4.1f, 0.13f), Lvl(170, 17f, 4.4f, 0.11f) }, 100f, slowFactor: 0.4f, slowSeconds: 2.5f);

            SetModel(roster.Laser, "turret_single", 1.35f);
            SetModel(roster.Cryo, "satelliteDish", 1.4f);
            SetModel(roster.Tesla, "machine_wireless", 1.4f);
            SetModel(roster.Railgun, "rocket_topA", 1.6f);
            SetModel(roster.Support, "machine_generatorLarge", 0.9f);
            SetModel(roster.Artillery, "turret_double", 1.7f);
            SetModel(roster.StormRifle, "turret_single", 1.6f);
            SetModel(roster.CryoBeam, "satelliteDish_large", 1.5f);
            return roster;
        }

        private static PathObjectDefinition CreatePathObject(
            string id,
            string displayName,
            PathObjectKind kind,
            PathObjectPlacement placement,
            TargetLayers targets,
            int cost,
            float radius,
            float triggerRadius,
            float damage,
            float slowFactor,
            float speed,
            float lifetime,
            Color color)
        {
            var asset = LoadOrCreate<PathObjectDefinition>($"{DataRoot}/PathObjects/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_placement").enumValueIndex = (int)placement;
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_cost").intValue = cost;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_triggerRadius").floatValue = triggerRadius;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_slowFactor").floatValue = slowFactor;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_lifetime").floatValue = lifetime;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static DroneDefinition CreateDrone(string id, string displayName, DroneKind kind, float speed, float health, float repair, float energyCost, Color color)
        {
            var asset = LoadOrCreate<DroneDefinition>($"{DataRoot}/Drones/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_repairPerSecond").floatValue = repair;
            so.FindProperty("_energyCost").floatValue = energyCost;
            so.FindProperty("_color").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static CombineRecipe CreateRecipe(string id, TowerDefinition first, TowerDefinition second, TowerDefinition result, int cost)
        {
            var asset = LoadOrCreate<CombineRecipe>($"{DataRoot}/Recipes/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_first").objectReferenceValue = first;
            so.FindProperty("_second").objectReferenceValue = second;
            so.FindProperty("_result").objectReferenceValue = result;
            so.FindProperty("_cost").intValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        // Path objects, drones and recipes are shared by all levels; each level only chooses which it sells.
        private static void ConfigureLevelExtras(
            LevelDefinition level,
            PathObjectDefinition[] pathObjects,
            CombineRecipe[] recipes,
            DroneDefinition repairDrone,
            DroneDefinition collectorDrone)
        {
            var so = new SerializedObject(level);
            SetReferences(so.FindProperty("_pathObjects"), pathObjects);
            SetReferences(so.FindProperty("_recipes"), recipes);
            so.FindProperty("_repairDrone").objectReferenceValue = repairDrone;
            so.FindProperty("_collectorDrone").objectReferenceValue = collectorDrone;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferences(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static List<DifficultyWaves> BuildWaveSets(
            string levelKey,
            DifficultyProfile[] difficulties,
            float countScale,
            EnemyDefinition scout,
            EnemyDefinition grunt,
            EnemyDefinition brute,
            EnemyDefinition flyer,
            EnemyDefinition boss,
            int secondRoad)
        {
            var sets = new List<DifficultyWaves>();
            for (int d = 0; d < difficulties.Length; d++)
            {
                int count = d == 0 ? 5 : d == 3 ? 7 : 6;
                sets.Add(new DifficultyWaves
                {
                    Difficulty = difficulties[d],
                    Waves = CreateWaves(levelKey, DifficultyNames[d], count, scout, grunt, brute, flyer, countScale, boss, secondRoad),
                });
            }

            return sets;
        }

        private static LevelDefinition[] CreateWorldLevels(
            LevelDefinition landing,
            Roster roster,
            DifficultyProfile[] difficulties,
            EnemyDefinition scout,
            EnemyDefinition grunt,
            EnemyDefinition brute,
            EnemyDefinition flyer,
            EnemyDefinition overseer,
            EnemyDefinition warden,
            EnemyDefinition queen,
            TowerDefinition blockade,
            HeroDefinition hero,
            PathObjectDefinition slowField,
            PathObjectDefinition mine,
            PathObjectDefinition lavaBall,
            PathObjectDefinition barrel,
            PathObjectDefinition groundDrone,
            PathObjectDefinition antiAir,
            CombineRecipe[] recipes,
            DroneDefinition repairDrone,
            DroneDefinition collectorDrone)
        {
            var specs = new[]
            {
                new LevelSpec
                {
                    Key = "Forest",
                    Name = "Glowing Forest",
                    Palette = new Palette
                    {
                        Background = new Color(0.03f, 0.02f, 0.07f),
                        Ground = new Color(0.1f, 0.3f, 0.32f),
                        Path = new Color(0.65f, 0.45f, 0.95f),
                        Slot = new Color(0.2f, 0.9f, 0.8f),
                        Base = new Color(0.4f, 1f, 0.8f),
                        Light = new Color(0.7f, 0.85f, 1f),
                        LightIntensity = 0.95f,
                        Fog = new Color(0.04f, 0.02f, 0.09f),
                        FogDensity = 0.018f,
                        Bloom = 1.5f,
                        Rock = new Color(0.24f, 0.3f, 0.46f),
                        Accent = new Color(0.35f, 1f, 0.8f),
                        Water = new Color(0.06f, 0.5f, 0.6f),
                        Lava = new Color(1f, 0.4f, 0.1f),
                        Sand = new Color(0.45f, 0.5f, 0.4f),
                        Props = new[] { PropKind.Mushroom, PropKind.Mushroom, PropKind.GlowTree, PropKind.GlowTree, PropKind.Frond, PropKind.Frond, PropKind.Crystal, PropKind.Rock },
                        Vein = 0.85f,
                        AmbientRate = 30f,
                        AmbientRise = 0.18f,
                    },
                    Size = new Vector2Int(26, 14),
                    Road = Cells(0, 12, 8, 12, 8, 8, 16, 8, 16, 3, 25, 3),
                    SecondRoad = Cells(0, 1, 8, 1, 8, 8),
                    NorthAir = Cells(0, 13, 25, 3),
                    SouthAir = Cells(0, 0, 13, 7, 25, 3),
                    BlockadeCells = Cells(12, 8, 16, 5),
                    BlockedSeed = 21,
                    Terrain = Terrain(
                        Area(TerrainKind.Water, 21, 13, 22, 17), Area(TerrainKind.Water, 20, 10, 21, 12), Area(TerrainKind.Water, 19, 8, 20, 9),
                        Area(TerrainKind.Water, 18, 5, 19, 7), Area(TerrainKind.Water, 3, 5, 5, 6),
                        Area(TerrainKind.Raised, 0, 8, 3, 10), Area(TerrainKind.Raised, 10, 10, 14, 12), Area(TerrainKind.Raised, 21, 5, 25, 7),
                        Area(TerrainKind.Raised, 12, 0, 18, 1)),
                    Money = 180,
                    BaseHealth = 20,
                    CountScale = 1.1f,
                    HealthScale = 1.05f,
                    Boss = overseer,
                    RewardTower = roster.Cryo,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "Alien crystals in the forest turned out to hold a cold-field emitter. Our engineers adapted it.",
                    Research = 14,
                    Power = 6,
                    PathObjects = new[] { slowField, mine },
                    Recipes = new CombineRecipe[0],
                },
                new LevelSpec
                {
                    Key = "Lava",
                    Name = "Lava Caves",
                    Palette = new Palette
                    {
                        Background = new Color(0.07f, 0.02f, 0.01f),
                        Ground = new Color(0.42f, 0.17f, 0.09f),
                        Path = new Color(1f, 0.55f, 0.2f),
                        Slot = new Color(1f, 0.4f, 0.15f),
                        Base = new Color(1f, 0.7f, 0.3f),
                        Light = new Color(1f, 0.7f, 0.5f),
                        LightIntensity = 1.0f,
                        Fog = new Color(0.1f, 0.03f, 0.01f),
                        FogDensity = 0.022f,
                        Bloom = 1.8f,
                        Rock = new Color(0.34f, 0.2f, 0.16f),
                        Accent = new Color(1f, 0.5f, 0.15f),
                        Water = new Color(0.1f, 0.2f, 0.3f),
                        Lava = new Color(1f, 0.4f, 0.08f),
                        Sand = new Color(0.4f, 0.25f, 0.15f),
                        Props = new[] { PropKind.Spike, PropKind.Spike, PropKind.Spike, PropKind.Rock, PropKind.Boulder, PropKind.Crystal },
                        Vein = 0.9f,
                        AmbientRate = 26f,
                        AmbientRise = 0.5f,
                    },
                    Size = new Vector2Int(22, 16),
                    Road = Cells(0, 14, 18, 14, 18, 11, 3, 11, 3, 8, 18, 8, 18, 5, 3, 5, 3, 2, 21, 2),
                    SecondRoad = null,
                    NorthAir = Cells(0, 15, 21, 2),
                    SouthAir = Cells(0, 0, 11, 7, 21, 2),
                    BlockadeCells = Cells(10, 11, 10, 5),
                    BlockedSeed = 33,
                    Terrain = Terrain(
                        Area(TerrainKind.Lava, 5, 12, 8, 13), Area(TerrainKind.Lava, 13, 12, 16, 13), Area(TerrainKind.Lava, 6, 9, 9, 10),
                        Area(TerrainKind.Lava, 12, 6, 15, 7), Area(TerrainKind.Lava, 4, 0, 12, 1), Area(TerrainKind.Lava, 20, 7, 26, 13),
                        Area(TerrainKind.Raised, 0, 12, 2, 13), Area(TerrainKind.Raised, 0, 6, 1, 7), Area(TerrainKind.Raised, 19, 3, 20, 4),
                        Area(TerrainKind.Raised, 10, 9, 14, 10)),
                    Money = 210,
                    BaseHealth = 20,
                    CountScale = 1.3f,
                    HealthScale = 1.3f,
                    Boss = null,
                    RewardTower = roster.Laser,
                    Source = UnlockSource.HomePlanetIntel,
                    Story = "Home sent the focusing crystals they refined from our first samples: a laser design.",
                    Research = 18,
                    Power = 9,
                    PathObjects = new[] { lavaBall, barrel },
                    Recipes = new CombineRecipe[0],
                },
                new LevelSpec
                {
                    Key = "Ruins",
                    Name = "Ancient Ruins",
                    Palette = new Palette
                    {
                        Background = new Color(0.05f, 0.05f, 0.02f),
                        Ground = new Color(0.26f, 0.27f, 0.12f),
                        Path = new Color(0.95f, 0.8f, 0.35f),
                        Slot = new Color(0.85f, 0.75f, 0.3f),
                        Base = new Color(1f, 0.9f, 0.45f),
                        Light = new Color(1f, 0.95f, 0.75f),
                        LightIntensity = 1.0f,
                        Fog = new Color(0.07f, 0.07f, 0.03f),
                        FogDensity = 0.014f,
                        Bloom = 1.3f,
                        Rock = new Color(0.5f, 0.46f, 0.28f),
                        Accent = new Color(1f, 0.85f, 0.4f),
                        Water = new Color(0.1f, 0.4f, 0.3f),
                        Lava = new Color(1f, 0.4f, 0.1f),
                        Sand = new Color(0.72f, 0.6f, 0.32f),
                        Props = new[] { PropKind.Column, PropKind.Column, PropKind.Column, PropKind.Rock, PropKind.Boulder, PropKind.Frond, PropKind.Crystal },
                        Vein = 0.3f,
                        AmbientRate = 14f,
                        AmbientRise = 0.05f,
                    },
                    Size = new Vector2Int(28, 16),
                    Road = Cells(0, 6, 12, 6, 12, 12, 18, 12, 18, 3, 8, 3, 8, 9, 3, 9),
                    SecondRoad = null,
                    NorthAir = Cells(27, 15, 3, 9),
                    SouthAir = Cells(27, 0, 3, 9),
                    BlockadeCells = Cells(5, 6, 18, 8),
                    BlockedSeed = 45,
                    Terrain = Terrain(
                        Area(TerrainKind.Quicksand, 20, 5, 23, 8), Area(TerrainKind.Quicksand, 9, 4, 11, 5), Area(TerrainKind.Quicksand, 2, 11, 4, 13),
                        Area(TerrainKind.Water, 22, 0, 26, 2),
                        Area(TerrainKind.Raised, 14, 7, 16, 9), Area(TerrainKind.Raised, 0, 12, 6, 14), Area(TerrainKind.Raised, 24, 6, 27, 12),
                        Area(TerrainKind.Raised, 1, 0, 6, 2)),
                    Money = 240,
                    BaseHealth = 20,
                    CountScale = 1.45f,
                    HealthScale = 1.5f,
                    Boss = warden,
                    RewardTower = roster.Rocket,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "The ruin vaults held guidance gear. With it our rocket launchers can finally lock on to flyers.",
                    Research = 22,
                    Power = 12,
                    PathObjects = new[] { groundDrone, antiAir },
                    Recipes = new CombineRecipe[0],
                },
                new LevelSpec
                {
                    Key = "Nest",
                    Name = "The Nest",
                    Palette = new Palette
                    {
                        Background = new Color(0.05f, 0.0f, 0.01f),
                        Ground = new Color(0.34f, 0.09f, 0.12f),
                        Path = new Color(0.95f, 0.3f, 0.35f),
                        Slot = new Color(0.9f, 0.25f, 0.3f),
                        Base = new Color(1f, 0.4f, 0.35f),
                        Light = new Color(1f, 0.6f, 0.6f),
                        LightIntensity = 1.05f,
                        Fog = new Color(0.1f, 0.01f, 0.03f),
                        FogDensity = 0.016f,
                        Bloom = 1.7f,
                        Rock = new Color(0.46f, 0.16f, 0.2f),
                        Accent = new Color(1f, 0.3f, 0.4f),
                        Water = new Color(0.3f, 0.65f, 0.1f),
                        Lava = new Color(1f, 0.35f, 0.1f),
                        Sand = new Color(0.14f, 0.05f, 0.08f),
                        Props = new[] { PropKind.Tendril, PropKind.Tendril, PropKind.Tendril, PropKind.Egg, PropKind.Egg, PropKind.Spike, PropKind.Rock },
                        Vein = 0.9f,
                        AmbientRate = 22f,
                        AmbientRise = -0.12f,
                    },
                    Size = new Vector2Int(24, 14),
                    Road = Cells(0, 11, 11, 11, 11, 6, 23, 6),
                    SecondRoad = Cells(0, 2, 11, 2, 11, 6),
                    NorthAir = Cells(0, 13, 23, 6),
                    SouthAir = Cells(0, 0, 23, 6),
                    BlockadeCells = Cells(6, 11, 6, 2, 17, 6),
                    BlockedSeed = 57,
                    Terrain = Terrain(
                        Area(TerrainKind.Water, 18, 9, 23, 12), Area(TerrainKind.Water, 15, 0, 20, 3), Area(TerrainKind.Water, 21, 13, 23, 16),
                        Area(TerrainKind.Quicksand, 2, 6, 3, 7), Area(TerrainKind.Quicksand, 7, 4, 8, 5),
                        Area(TerrainKind.Raised, 0, 8, 1, 9), Area(TerrainKind.Raised, 13, 8, 16, 9), Area(TerrainKind.Raised, 5, 7, 9, 9)),
                    Money = 270,
                    BaseHealth = 20,
                    CountScale = 1.35f,
                    HealthScale = 1.4f,
                    Boss = queen,
                    RewardTower = roster.Tesla,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "The queen's nest was powered by living coils. We took one, and with it the first hybrid designs.",
                    Research = 30,
                    Power = 18,
                    PathObjects = new[] { slowField, mine, lavaBall, barrel, groundDrone, antiAir },
                    Recipes = recipes,
                },
            };

            var levels = new LevelDefinition[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                levels[i] = ConfigureWorldLevel(specs[i], roster, difficulties, scout, grunt, brute, flyer, blockade, hero, repairDrone, collectorDrone);
            }

            return levels;
        }

        // A filled rectangle of one terrain kind. Coordinates may reach past the grid so a river can flow out of view.
        private static IEnumerable<TerrainPatch> Area(TerrainKind kind, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    yield return new TerrainPatch { Cell = new Vector2Int(x, y), Kind = kind };
                }
            }
        }

        private static TerrainPatch[] Terrain(params IEnumerable<TerrainPatch>[] areas) => areas.SelectMany(a => a).ToArray();

        // Rectangles look like building plots. Cells on the outline are dropped now and then (the same ones every
        // run), which turns them into rough natural shapes; interior cells are never touched.
        private static TerrainPatch[] Erode(TerrainPatch[] patches)
        {
            var byCell = patches.GroupBy(p => p.Cell).ToDictionary(g => g.Key, g => g.Last().Kind);
            var kept = new List<TerrainPatch>();
            var random = new System.Random(7);
            Vector2Int[] steps = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

            foreach (var pair in byCell)
            {
                bool outline = steps.Any(s => !byCell.TryGetValue(pair.Key + s, out TerrainKind other) || other != pair.Value);
                if (outline && random.NextDouble() < ErodeChance)
                {
                    continue;
                }

                kept.Add(new TerrainPatch { Cell = pair.Key, Kind = pair.Value });
            }

            return kept.ToArray();
        }

        private const double ErodeChance = 0.4;

        // Terrain never covers the road, and liquid keeps clear of the blockades where the hero stands.
        private static void WriteTerrain(SerializedProperty array, TerrainPatch[] patches, HashSet<Vector2Int> road, Vector2Int[] blockades)
        {
            patches = Erode(patches);
            TerrainPatch[] kept = patches
                .Where(p => !road.Contains(p.Cell))
                .Where(p => !TerrainMap.IsLiquid(p.Kind) || !blockades.Any(b => Mathf.Abs(b.x - p.Cell.x) <= 1 && Mathf.Abs(b.y - p.Cell.y) <= 1))
                .GroupBy(p => p.Cell)
                .Select(g => g.Last())
                .ToArray();

            array.arraySize = kept.Length;
            for (int i = 0; i < kept.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cell").vector2IntValue = kept[i].Cell;
                element.FindPropertyRelative("Kind").enumValueIndex = (int)kept[i].Kind;
            }
        }

        private static Vector2Int[] Cells(params int[] xy)
        {
            var cells = new Vector2Int[xy.Length / 2];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new Vector2Int(xy[i * 2], xy[i * 2 + 1]);
            }

            return cells;
        }

        private static LevelDefinition ConfigureWorldLevel(
            LevelSpec spec,
            Roster roster,
            DifficultyProfile[] difficulties,
            EnemyDefinition scout,
            EnemyDefinition grunt,
            EnemyDefinition brute,
            EnemyDefinition flyer,
            TowerDefinition blockade,
            HeroDefinition hero,
            DroneDefinition repairDrone,
            DroneDefinition collectorDrone)
        {
            var theme = LoadOrCreate<VisualTheme>($"{DataRoot}/Levels/Theme_{spec.Key}.asset");
            ConfigureThemePalette(theme, spec.Palette);

            var level = LoadOrCreate<LevelDefinition>($"{DataRoot}/Levels/Level_{spec.Key}.asset");
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = spec.Name;
            so.FindProperty("_gridSize").vector2IntValue = spec.Size;
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_theme").objectReferenceValue = theme;
            SerializedProperty camera = so.FindProperty("_camera");
            camera.FindPropertyRelative("PitchDegrees").floatValue = 55f;
            camera.FindPropertyRelative("YawDegrees").floatValue = 0f;
            camera.FindPropertyRelative("FitMargin").floatValue = 0.04f;
            so.FindProperty("_startingMoney").intValue = spec.Money;
            so.FindProperty("_enemyHealthMultiplier").floatValue = spec.HealthScale;
            so.FindProperty("_baseHealth").intValue = spec.BaseHealth;
            so.FindProperty("_hero").objectReferenceValue = hero;
            SerializedProperty stars = so.FindProperty("_starBaseHealthFractions");
            stars.arraySize = 2;
            stars.GetArrayElementAtIndex(0).floatValue = 0.5f;
            stars.GetArrayElementAtIndex(1).floatValue = 0.9f;

            SerializedProperty reward = so.FindProperty("_reward");
            reward.FindPropertyRelative("ResearchPoints").intValue = spec.Research;
            reward.FindPropertyRelative("PowerCurrency").intValue = spec.Power;
            reward.FindPropertyRelative("NewTower").objectReferenceValue = spec.RewardTower;
            reward.FindPropertyRelative("Source").intValue = (int)spec.Source;
            reward.FindPropertyRelative("Story").stringValue = spec.Story;

            // Road 0, flying routes 1 and 2, and an optional second road as path 3, which the waves use by index.
            var paths = new List<(string Name, bool Flying, Vector2Int[] Waypoints)>
            {
                ("Road", false, spec.Road),
                ("North air route", true, spec.NorthAir),
                ("South air route", true, spec.SouthAir),
            };
            if (spec.SecondRoad != null)
            {
                paths.Add(("Second road", false, spec.SecondRoad));
            }

            WritePaths(so.FindProperty("_paths"), paths.ToArray());

            var road = new HashSet<Vector2Int>(LevelValidator.LandPathCells(spec.Road));
            if (spec.SecondRoad != null)
            {
                road.UnionWith(LevelValidator.LandPathCells(spec.SecondRoad));
            }

            TerrainPatch[] terrain = spec.Terrain ?? new TerrainPatch[0];
            WriteCells(so.FindProperty("_blockedCells"), ScatterBlocked(spec.Size, road, terrain, spec.BlockadeCells, spec.BlockedSeed, BlockedFraction));
            WriteTerrain(so.FindProperty("_terrain"), terrain, road, spec.BlockadeCells);

            SerializedProperty blockades = so.FindProperty("_blockades");
            blockades.arraySize = spec.BlockadeCells.Length;
            for (int i = 0; i < spec.BlockadeCells.Length; i++)
            {
                SerializedProperty element = blockades.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cell").vector2IntValue = spec.BlockadeCells[i];
                element.FindPropertyRelative("Blockade").objectReferenceValue = blockade;
            }

            SetReferences(so.FindProperty("_availableTowers"), roster.Standard);

            int secondRoadIndex = spec.SecondRoad != null ? 3 : -1;
            List<DifficultyWaves> waveSets = BuildWaveSets(spec.Key, difficulties, spec.CountScale, scout, grunt, brute, flyer, spec.Boss, secondRoadIndex);
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
            ConfigureLevelExtras(level, spec.PathObjects, spec.Recipes, repairDrone, collectorDrone);
            return level;
        }
    }
}
