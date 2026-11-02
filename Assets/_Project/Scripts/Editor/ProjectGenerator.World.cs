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
            public Color Ice;
            public Color Bog;
            public Color Spawn;
            public PropKind[] Props;
            public float Vein;
            public float AmbientRate;
            public float AmbientRise;
            public AmbienceKind Ambience;
            public Color Plant;
            // Light rig and grading; left at default they are derived from the light, fog and accent colours.
            public Color Sky;
            public Color Rim;
            public float SunPitch;
            public float SunYaw;
            public float Contrast;
            public float Saturation;
            public Color ShadowTint;
            public Color HighlightTint;
        }

        // The liquid cell closest to a wanted entrance, because erosion may have dropped the exact one.
        private static Vector2Int NearestLiquid(TerrainPatch[] patches, Vector2Int wanted)
        {
            Vector2Int best = wanted;
            float bestSqr = float.MaxValue;
            foreach (TerrainPatch patch in patches)
            {
                if (!TerrainMap.IsLiquid(patch.Kind))
                {
                    continue;
                }

                float sqr = (patch.Cell - wanted).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = patch.Cell;
                }
            }

            return best;
        }

        private sealed class LevelSpec
        {
            public string Key;
            public string Region;
            public string Briefing;
            public string Name;
            public Palette Palette;
            public int RoadWidth;
            public Vector2Int[] Road;
            public Vector2Int[] SecondRoad;
            public Vector2Int[] Plazas;
            public Vector2Int[] NorthAir;
            public Vector2Int[] SouthAir;
                        public Vector2Int[] BlockadeCells;
            public TerrainPatch[] Terrain;
            public Vector2Int Size;
            public int BlockedSeed;
            public int Money;
            public int BaseHealth;
            public float HealthScale;
            public TowerDefinition RewardTower;
            public UnlockSource Source;
            public string Story;
            public int Research;
            public int Power;

            /// <summary>How much permanent upgrade the player is expected to bring; enemies are scaled up by it.</summary>
            public float UpgradeBonus;
            public PathObjectDefinition[] PathObjects;
            public CombineRecipe[] Recipes;
            public WaveScript Script;
            // Bosses use the main gate unless the level is late, rich or simple; see Docs/LEVEL_GUIDE.md before changing this.
            public EntranceKind Entrance;
            public Vector2Int EntranceCell;
            public ElementNest[] Nests = new ElementNest[0];
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
            public TowerDefinition[] Hybrids => new[] { Artillery, StormRifle, CryoBeam };
        }

        private static Roster CreateRoster(TowerDefinition pulse, TowerDefinition cannon, TowerDefinition rocket)
        {
            var roster = new Roster { Pulse = pulse, Cannon = cannon, Rocket = rocket };

            // Fast single-target damage on the ground; a stand-in for a continuous beam until it has its own effect.
            roster.Laser = CreateTower("Laser", "Laser", TargetLayers.Ground, 40f, 0f, new Color(1f, 0.35f, 0.45f),
                new[] { Lvl(110, 8f, 3.9f, 0.2f), Lvl(130, 12f, 4.1f, 0.18f), Lvl(190, 18f, 4.4f, 0.15f) }, 160f);

            roster.Cryo = CreateTower("Cryo", "Cryo", TargetLayers.Ground | TargetLayers.Air, 12f, 0f, new Color(0.55f, 0.95f, 1f),
                new[] { Lvl(90, 2f, 3.7f, 0.5f, 0f, 0.5f, 2f), Lvl(110, 2f, 4f, 0.45f, 0f, 0.42f, 3f), Lvl(160, 2f, 4.3f, 0.4f, 0f, 0.35f, 4f) }, 140f, slowFactor: 0.5f, slowSeconds: 2f);

            // Lightning is approximated by a small splash, so it works well on groups.
            roster.Tesla = CreateTower("Tesla", "Tesla", TargetLayers.Ground, 40f, 1.3f, new Color(0.7f, 0.5f, 1f),
                new[] { Lvl(140, 16f, 3.5f, 0.5f), Lvl(160, 26f, 3.7f, 0.45f), Lvl(230, 40f, 4f, 0.4f) }, 160f);

            roster.Railgun = CreateTower("Railgun", "Railgun", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(1f, 1f, 1f),
                new[] { Lvl(220, 80f, 6f, 2.2f), Lvl(260, 120f, 6.5f, 2f), Lvl(360, 180f, 7f, 1.8f) }, 180f);

            roster.Support = CreateTower("Support", "Support", TargetLayers.Ground, 1f, 0f, new Color(0.4f, 1f, 0.6f),
                new[] { Lvl(120, 0f, 3f, 1f), Lvl(140, 0f, 3.5f, 1f), Lvl(200, 0f, 4f, 1f) }, 140f, support: true, rangeBonus: 0.15f, rateBonus: 0.2f);

            // Hybrids come from recipes, so their first level costs nothing to build: the recipe has the price. Every
            // level beats the two parents at that level put together (by 15 to 50 percent), in one cell, and the
            // upgrades cost close to what upgrading both parents would.
            roster.Artillery = CreateTower("Artillery", "Artillery", TargetLayers.Ground | TargetLayers.Air, 8f, 1.8f, new Color(1f, 0.6f, 0.2f),
                new[] { Lvl(0, 70f, 5.1f, 1.6f), Lvl(220, 130f, 5.5f, 1.4f), Lvl(320, 220f, 5.9f, 1.2f) }, 240f);

            roster.StormRifle = CreateTower("StormRifle", "Storm Rifle", TargetLayers.Ground | TargetLayers.Air, 20f, 0.8f, new Color(0.6f, 0.8f, 1f),
                new[] { Lvl(0, 14f, 4.1f, 0.2f), Lvl(200, 20f, 4.4f, 0.18f), Lvl(300, 28f, 4.7f, 0.15f) }, 190f);

            roster.CryoBeam = CreateTower("CryoBeam", "Cryo Beam", TargetLayers.Ground | TargetLayers.Air, 40f, 0f, new Color(0.7f, 1f, 1f),
                new[] { Lvl(0, 8f, 4.3f, 0.15f, 0f, 0.4f, 2.5f), Lvl(210, 13f, 4.6f, 0.13f, 0f, 0.33f, 3.5f), Lvl(310, 20f, 4.9f, 0.11f, 0f, 0.27f, 4.5f) }, 180f, slowFactor: 0.4f, slowSeconds: 2.5f);

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
            Color color,
            PathObjectEffect effect,
            string description)
        {
            var asset = LoadOrCreate<PathObjectDefinition>($"{DataRoot}/PathObjects/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_effect").enumValueIndex = (int)effect;
            so.FindProperty("_description").stringValue = description;
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

        private static DroneDefinition CreateDrone(string id, string displayName, DroneKind kind, float speed, float health, int cost, int maxActive, Color color, float repair = 0f, float harvestSeconds = 4f, int harvestValue = 0, float damage = 0f, float fireInterval = 0.5f, float range = 3f, CreatureSkin.Kind skin = CreatureSkin.Kind.Plate, Color accent = default)
        {
            var asset = LoadOrCreate<DroneDefinition>($"{DataRoot}/Drones/{id}.asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_cost").intValue = cost;
            so.FindProperty("_maxActive").intValue = maxActive;
            so.FindProperty("_repairPerSecond").floatValue = repair;
            so.FindProperty("_harvestSeconds").floatValue = harvestSeconds;
            so.FindProperty("_harvestValue").intValue = harvestValue;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_fireInterval").floatValue = fireInterval;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_targets").intValue = (int)(TargetLayers.Ground | TargetLayers.Air);
            so.FindProperty("_color").colorValue = color;
            so.FindProperty("_skin").enumValueIndex = (int)skin;
            so.FindProperty("_accentColor").colorValue = accent == default ? Color.white : accent;
            so.ApplyModifiedPropertiesWithoutUndo();
            // A value equal to the field default leaves the asset clean, so force the save or a stale field lingers on disk.
            EditorUtility.SetDirty(asset);
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
            DroneDefinition harvestDrone,
            DroneDefinition attackDrone)
        {
            var so = new SerializedObject(level);
            SetReferences(so.FindProperty("_pathObjects"), pathObjects);
            SetReferences(so.FindProperty("_recipes"), recipes);
            so.FindProperty("_repairDrone").objectReferenceValue = repairDrone;
            so.FindProperty("_harvestDrone").objectReferenceValue = harvestDrone;
            so.FindProperty("_attackDrone").objectReferenceValue = attackDrone;
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

        private static LevelDefinition[] CreateWorldLevels(
            LevelDefinition landing,
            Roster roster,
            DifficultyProfile[] difficulties,
            Bestiary enemies,
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
            DroneDefinition harvestDrone,
            DroneDefinition attackDrone)
        {
            var specs = new[]
            {
                new LevelSpec
                {
                    Key = "Forest",
                    Script = ForestScript(enemies),
                    Name = "Glowing Forest",
                    Region = "Bioluminescent Woods",
                    Briefing = "The woods glow at night, and so does what lives in them. Two roads meet under the canopy. Rollers come down them fast, and the ponds hide worse.",
                    Palette = new Palette
                    {
                        Background = new Color(0.03f, 0.02f, 0.07f),
                        Ground = new Color(0.07f, 0.16f, 0.17f),
                        Path = new Color(0.78f, 0.62f, 1f),
                        Slot = new Color(0.2f, 0.9f, 0.8f),
                        Base = new Color(0.4f, 1f, 0.8f),
                        Light = new Color(0.86f, 0.92f, 1f),
                        LightIntensity = 1.15f,
                        Sky = new Color(0.2f, 0.3f, 0.36f),
                        Fog = new Color(0.04f, 0.02f, 0.09f),
                        FogDensity = 0.018f,
                        Bloom = 1.5f,
                        Rock = new Color(0.22f, 0.26f, 0.36f),
                        Accent = new Color(0.35f, 1f, 0.8f),
                        Water = new Color(0.06f, 0.5f, 0.6f),
                        Lava = new Color(1f, 0.4f, 0.1f),
                        Sand = new Color(0.45f, 0.5f, 0.4f),
                        Props = new[] { PropKind.Mushroom, PropKind.Mushroom, PropKind.GlowTree, PropKind.GlowTree, PropKind.Frond, PropKind.Frond, PropKind.Crystal, PropKind.Rock },
                        Vein = 0.85f,
                        AmbientRate = 30f,
                        AmbientRise = 0.18f,
                        Ambience = AmbienceKind.Forest,
                        Plant = new Color(0.14f, 0.52f, 0.42f),
                    },
                    // The main road winds through the woods in three switchbacks; a second road comes in from the
                    // south-west and meets it on a clearing before the last stretch east to the base.
                    Size = new Vector2Int(38, 20),
                    RoadWidth = 2,
                    Road = Cells(0, 18, 10, 18, 10, 13, 2, 13, 2, 10, 16, 10, 16, 15, 26, 15, 26, 8, 26, 5, 37, 5),
                    SecondRoad = Cells(0, 2, 21, 2, 21, 8, 26, 8),
                    Plazas = Rect(23, 7, 27, 9),
                    NorthAir = Cells(0, 19, 20, 17, 37, 5),
                    SouthAir = Cells(0, 0, 18, 4, 37, 5),
                    BlockadeCells = Cells(6, 18, 8, 10, 30, 5, 10, 2),
                    BlockedSeed = 21,
                    Terrain = Terrain(
                        Area(TerrainKind.Water, 4, 15, 7, 16), Area(TerrainKind.Water, 12, 14, 14, 17), Area(TerrainKind.Water, 13, 18, 14, 22),
                        Area(TerrainKind.Water, 29, 9, 33, 12), Area(TerrainKind.Water, 34, 11, 38, 13),
                        Area(TerrainKind.Raised, 0, 15, 2, 16), Area(TerrainKind.Raised, 18, 11, 24, 13), Area(TerrainKind.Raised, 30, 0, 36, 1),
                        Area(TerrainKind.Raised, 5, 5, 9, 7)),
                    Money = 240,
                    BaseHealth = BaseHealth,
                    HealthScale = 1.05f,
                    UpgradeBonus = 0f,
                    RewardTower = roster.Cryo,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "Alien crystals in the forest turned out to hold a cold-field emitter. Our engineers adapted it.",
                    Research = 36,
                    Power = 8,
                    PathObjects = new[] { slowField, mine },
                    Recipes = new CombineRecipe[0],
                    // Rollers from the third wave, pool creatures out of the forest ponds from the fourth; the boss surfaces from a pond.
                    Entrance = EntranceKind.Gate,
                    EntranceCell = new Vector2Int(13, 15),
                    // Moss bats roost in hollow trees on both sides of the woods; tide bats rise from the ponds.
                    Nests = new[] { Nest(Element.Wood, 4, 6), Nest(Element.Wood, 18, 12), Nest(Element.Water, 5, 15), Nest(Element.Water, 13, 15) },
                },
                new LevelSpec
                {
                    Key = "Lava",
                    Script = LavaScript(enemies),
                    Name = "Lava Caves",
                    Region = "Lava Fields",
                    Briefing = "The energy we came for lies in these lava fields. The heat does not stop the swarm, and halfway through something climbs out of the lava itself.",
                    Palette = new Palette
                    {
                        Background = new Color(0.07f, 0.02f, 0.01f),
                        Ground = new Color(0.17f, 0.09f, 0.07f),
                        Path = new Color(0.95f, 0.72f, 0.45f),
                        Slot = new Color(1f, 0.4f, 0.15f),
                        Base = new Color(1f, 0.7f, 0.3f),
                        Light = new Color(1f, 0.88f, 0.76f),
                        LightIntensity = 1.2f,
                        Sky = new Color(0.34f, 0.2f, 0.16f),
                        Fog = new Color(0.08f, 0.02f, 0.01f),
                        FogDensity = 0.016f,
                        Bloom = 1.8f,
                        Rock = new Color(0.28f, 0.22f, 0.2f),
                        Accent = new Color(1f, 0.5f, 0.15f),
                        Water = new Color(0.1f, 0.2f, 0.3f),
                        Lava = new Color(1f, 0.4f, 0.08f),
                        Sand = new Color(0.4f, 0.25f, 0.15f),
                        Props = new[] { PropKind.Spike, PropKind.Spike, PropKind.Spike, PropKind.Rock, PropKind.Boulder, PropKind.Crystal },
                        Vein = 0.9f,
                        AmbientRate = 26f,
                        AmbientRise = 0.5f,
                        Ambience = AmbienceKind.Lava,
                        Plant = new Color(0.5f, 0.2f, 0.12f),
                    },
                    // A serpentine through the caves. The second row of a road lies to the right of travel, so the
                    // legs that turn clockwise sit five rows apart and the others three, leaving two build rows each.
                    Size = new Vector2Int(34, 24),
                    RoadWidth = 2,
                    Road = Cells(0, 22, 26, 22, 26, 17, 4, 17, 4, 14, 28, 14, 28, 9, 6, 9, 6, 6, 24, 6, 24, 2, 33, 2),
                    SecondRoad = null,
                    NorthAir = Cells(0, 23, 17, 20, 33, 2),
                    SouthAir = Cells(0, 0, 14, 4, 33, 2),
                    BlockadeCells = Cells(12, 22, 14, 14, 16, 9, 30, 2),
                    BlockedSeed = 33,
                    Terrain = Terrain(
                        Area(TerrainKind.Lava, 8, 19, 12, 20), Area(TerrainKind.Lava, 18, 11, 21, 12), Area(TerrainKind.Lava, 0, 0, 3, 3),
                        Area(TerrainKind.Lava, 29, 6, 33, 12), Area(TerrainKind.Lava, 34, 4, 36, 14), Area(TerrainKind.Lava, 10, 0, 16, 1),
                        Area(TerrainKind.Raised, 0, 19, 2, 20), Area(TerrainKind.Raised, 30, 16, 33, 20), Area(TerrainKind.Raised, 8, 11, 11, 12),
                        Area(TerrainKind.Raised, 14, 3, 17, 4)),
                    Money = 280,
                    BaseHealth = BaseHealth,
                    HealthScale = 1.3f,
                    UpgradeBonus = 0.1f,
                    RewardTower = roster.Laser,
                    Source = UnlockSource.HomePlanetIntel,
                    Story = "Home sent the focusing crystals they refined from our first samples: a laser design.",
                    Research = 32,
                    Power = 11,
                    PathObjects = new[] { lavaBall, barrel },
                    Recipes = new CombineRecipe[0],
                    // No final boss here, but a mind-boss climbs out of the lava halfway through the level.
                    Entrance = EntranceKind.Gate,
                    EntranceCell = new Vector2Int(10, 19),
                    // Ember bats pour out of the lava vents.
                    Nests = new[] { Nest(Element.Fire, 9, 19), Nest(Element.Fire, 19, 12) },
                },
                new LevelSpec
                {
                    Key = "Ruins",
                    Script = RuinsScript(enemies),
                    Name = "Ancient Ruins",
                    Region = "Old Ruins",
                    Briefing = "Someone built here long before us, and lost. Old metal, gold and green, and a walking fortress that still guards it.",
                    Palette = new Palette
                    {
                        Background = new Color(0.05f, 0.05f, 0.02f),
                        Ground = new Color(0.14f, 0.15f, 0.08f),
                        Path = new Color(0.95f, 0.84f, 0.5f),
                        Slot = new Color(0.85f, 0.75f, 0.3f),
                        Base = new Color(1f, 0.9f, 0.45f),
                        Light = new Color(1f, 0.96f, 0.84f),
                        LightIntensity = 1.2f,
                        Sky = new Color(0.3f, 0.3f, 0.2f),
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
                        Ambience = AmbienceKind.Ruins,
                        Plant = new Color(0.48f, 0.5f, 0.2f),
                    },
                    // The main road snakes through the whole ruin: north wall, the old streets, round the cistern and
                    // back west before the long avenue to the base; a second road from the south-west meets it on
                    // the market square.
                    Size = new Vector2Int(40, 22),
                    RoadWidth = 2,
                    Road = Cells(0, 20, 14, 20, 14, 15, 3, 15, 3, 12, 20, 12, 20, 17, 30, 17, 30, 8, 12, 8, 12, 6, 12, 4, 39, 4),
                    SecondRoad = Cells(0, 2, 5, 2, 5, 6, 12, 6),
                    Plazas = Rect(9, 5, 13, 8),
                    NorthAir = Cells(0, 21, 22, 19, 39, 4),
                    SouthAir = Cells(16, 0, 28, 2, 39, 4),
                    BlockadeCells = Cells(7, 20, 10, 12, 25, 17, 33, 4, 3, 2),
                    BlockedSeed = 45,
                    Terrain = Terrain(
                        Area(TerrainKind.Quicksand, 6, 17, 9, 18), Area(TerrainKind.Quicksand, 16, 13, 18, 14), Area(TerrainKind.Quicksand, 34, 6, 37, 7),
                        Area(TerrainKind.Water, 22, 10, 27, 14), Area(TerrainKind.Water, 36, 10, 40, 14),
                        Area(TerrainKind.Raised, 0, 17, 2, 18), Area(TerrainKind.Raised, 33, 9, 35, 14), Area(TerrainKind.Raised, 16, 0, 20, 1),
                        Area(TerrainKind.Raised, 24, 0, 28, 1), Area(TerrainKind.Raised, 0, 8, 1, 10)),
                    Money = 320,
                    BaseHealth = BaseHealth,
                    HealthScale = 1.5f,
                    UpgradeBonus = 0.2f,
                    RewardTower = roster.Rocket,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "The ruin vaults held guidance gear. With it our rocket launchers can finally lock on to flyers.",
                    Research = 40,
                    Power = 14,
                    PathObjects = new[] { groundDrone, antiAir },
                    Recipes = new CombineRecipe[0],
                    // Diggers out of the quicksand, rollers, and eels from the cistern; the fortress breaks out of the ground between the roads.
                    Entrance = EntranceKind.Gate,
                    EntranceCell = new Vector2Int(8, 10),
                    // Dust bats burrow out of the sand drifts and tide bats out of the old cistern.
                    // Only the drifts in the first half of the siege hold nests; the sand by the base stays quiet.
                    Nests = new[] { Nest(Element.Sand, 8, 17), Nest(Element.Sand, 17, 13), Nest(Element.Water, 22, 14) },
                },
                new LevelSpec
                {
                    Key = "Nest",
                    Script = NestScript(enemies),
                    Name = "The Nest",
                    Region = "The Hive",
                    Briefing = "The source. The queen waits under the hive and the whole planet seems to hold its breath. End this, and learn what is under the surface.",
                    Palette = new Palette
                    {
                        Background = new Color(0.05f, 0.0f, 0.01f),
                        Ground = new Color(0.16f, 0.05f, 0.07f),
                        Path = new Color(0.9f, 0.6f, 0.55f),
                        Slot = new Color(0.9f, 0.25f, 0.3f),
                        Base = new Color(1f, 0.4f, 0.35f),
                        Light = new Color(1f, 0.84f, 0.82f),
                        LightIntensity = 1.2f,
                        Sky = new Color(0.32f, 0.16f, 0.18f),
                        Fog = new Color(0.1f, 0.01f, 0.03f),
                        FogDensity = 0.016f,
                        Bloom = 1.7f,
                        Rock = new Color(0.34f, 0.16f, 0.18f),
                        Accent = new Color(1f, 0.3f, 0.4f),
                        Water = new Color(0.3f, 0.65f, 0.1f),
                        Lava = new Color(1f, 0.35f, 0.1f),
                        Sand = new Color(0.14f, 0.05f, 0.08f),
                        Props = new[] { PropKind.Tendril, PropKind.Tendril, PropKind.Tendril, PropKind.Egg, PropKind.Egg, PropKind.Spike, PropKind.Rock },
                        Vein = 0.9f,
                        AmbientRate = 22f,
                        AmbientRise = -0.12f,
                        Ambience = AmbienceKind.Hive,
                        Plant = new Color(0.52f, 0.14f, 0.32f),
                    },
                    // The north road winds down through the hive in switchbacks, the south road comes straight at the
                    // forecourt; both pour onto the square before the last stretch to the base.
                    Size = new Vector2Int(36, 22),
                    RoadWidth = 2,
                    Road = Cells(0, 20, 14, 20, 14, 15, 4, 15, 4, 12, 20, 12, 20, 7, 35, 7),
                    SecondRoad = Cells(0, 2, 10, 2, 10, 7, 20, 7),
                    Plazas = Rect(17, 5, 22, 9),
                    NorthAir = Cells(0, 21, 18, 18, 35, 7),
                    SouthAir = Cells(0, 0, 16, 3, 35, 7),
                    BlockadeCells = Cells(7, 20, 9, 12, 27, 7, 5, 2),
                    BlockedSeed = 57,
                    Terrain = Terrain(
                        Area(TerrainKind.Water, 16, 17, 19, 20), Area(TerrainKind.Water, 0, 8, 2, 10), Area(TerrainKind.Water, 24, 10, 30, 14), Area(TerrainKind.Water, 31, 14, 36, 16),
                        Area(TerrainKind.Quicksand, 6, 8, 8, 9), Area(TerrainKind.Quicksand, 14, 2, 16, 4), Area(TerrainKind.Quicksand, 30, 0, 33, 2),
                        Area(TerrainKind.Raised, 0, 17, 2, 18), Area(TerrainKind.Raised, 23, 15, 28, 17), Area(TerrainKind.Raised, 24, 1, 28, 3),
                        Area(TerrainKind.Raised, 32, 10, 35, 12)),
                    Money = 360,
                    BaseHealth = BaseHealth,
                    HealthScale = 1.4f,
                    UpgradeBonus = 0.3f,
                    RewardTower = roster.Tesla,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "The queen's nest was powered by living coils. We took one, and with it the first hybrid designs.",
                    Research = 50,
                    Power = 20,
                    PathObjects = new[] { slowField, mine, lavaBall, barrel, groundDrone, antiAir },
                    Recipes = recipes,
                    // Everything at once, a fortress halfway, and the queen herself through the hive mouth.
                    Entrance = EntranceKind.Gate,
                    // The hive keeps its own bats: in the drowned corner and in the sand pits.
                    Nests = new[] { Nest(Element.Water, 17, 18), Nest(Element.Water, 1, 9), Nest(Element.Sand, 7, 8), Nest(Element.Sand, 15, 3) },
                },
                new LevelSpec
                {
                    Key = "Ice",
                    Script = IceScript(enemies),
                    Name = "Frozen Shelf",
                    Region = "Polar Shelf",
                    Briefing = "Beyond the hive the planet turns to ice. The lakes are frozen over, and something lives under the ice: the shelf cracks where the frost bats come up, and the road is never far from a lake.",
                    Palette = new Palette
                    {
                        Background = new Color(0.05f, 0.07f, 0.11f),
                        Ground = new Color(0.6f, 0.66f, 0.74f),
                        Path = new Color(0.3f, 0.46f, 0.68f),
                        Slot = new Color(0.4f, 0.75f, 1f),
                        Base = new Color(0.5f, 0.85f, 1f),
                        Light = new Color(0.8f, 0.9f, 1f),
                        LightIntensity = 1.3f,
                        Fog = new Color(0.3f, 0.36f, 0.46f),
                        FogDensity = 0.02f,
                        Bloom = 1.1f,
                        Rock = new Color(0.8f, 0.85f, 0.92f),
                        Accent = new Color(0.6f, 0.9f, 1f),
                        Water = new Color(0.1f, 0.35f, 0.55f),
                        Lava = new Color(1f, 0.4f, 0.1f),
                        Sand = new Color(0.6f, 0.62f, 0.66f),
                        Ice = new Color(0.72f, 0.88f, 1f),
                        Bog = new Color(0.18f, 0.24f, 0.12f),
                        Spawn = new Color(0.55f, 0.85f, 1f),
                        Props = new[] { PropKind.Snowdrift, PropKind.Snowdrift, PropKind.Snowdrift, PropKind.Snowdrift, PropKind.IceShard, PropKind.Boulder, PropKind.Rock, PropKind.Snowdrift },
                        Vein = 0.25f,
                        AmbientRate = 60f,
                        AmbientRise = -0.7f,
                        Ambience = AmbienceKind.Blizzard,
                        Plant = new Color(0.62f, 0.7f, 0.72f),
                    },
                    // The road zigzags between four frozen lakes; a second road comes in along the southern edge.
                    Size = new Vector2Int(38, 20),
                    RoadWidth = 2,
                    Road = Cells(0, 3, 12, 3, 12, 10, 4, 10, 4, 16, 22, 16, 22, 9, 30, 9, 30, 14, 37, 14),
                    SecondRoad = Cells(0, 19, 4, 19, 4, 16),
                    Plazas = Rect(20, 14, 24, 17),
                    NorthAir = Cells(0, 19, 18, 18, 37, 14),
                    SouthAir = Cells(0, 0, 20, 2, 37, 14),
                    BlockadeCells = Cells(6, 3, 4, 13, 15, 16, 30, 11),
                    BlockedSeed = 33,
                    Terrain = Terrain(
                        Area(TerrainKind.Ice, 14, 5, 19, 8), Area(TerrainKind.Ice, 26, 16, 30, 19), Area(TerrainKind.Ice, 0, 6, 2, 8), Area(TerrainKind.Ice, 32, 2, 36, 6),
                        Area(TerrainKind.Raised, 24, 1, 28, 3), Area(TerrainKind.Raised, 8, 13, 10, 14), Area(TerrainKind.Raised, 34, 17, 37, 19)),
                    Money = 320,
                    BaseHealth = BaseHealth,
                    HealthScale = 1.2f,
                    UpgradeBonus = 0.35f,
                    RewardTower = roster.Railgun,
                    Source = UnlockSource.HomePlanetIntel,
                    Story = "The home planet finished the railgun designs. Nothing the ice can grow stops a slug at that speed.",
                    Research = 55,
                    Power = 22,
                    PathObjects = new[] { slowField, mine, barrel, groundDrone, antiAir },
                    Recipes = recipes,
                    // The warlord breaks up through the middle lake.
                    Entrance = EntranceKind.Pool,
                    EntranceCell = new Vector2Int(17, 7),
                    // Frost bats come up through cracks in the first two lakes, near the gates.
                    Nests = new[] { Nest(Element.Ice, 1, 7), Nest(Element.Ice, 16, 6), Nest(Element.Ice, 14, 5) },
                },
                new LevelSpec
                {
                    Key = "Swamp",
                    Script = SwampScript(enemies),
                    Name = "Black Marsh",
                    Region = "Drowned Lowlands",
                    Briefing = "South of the shelf the ice melts into a marsh that never dries. The roads are causeways between the pools, the trees are dead, and the mire itself breathes: leeches, slugs and bats come up out of it.",
                    Palette = new Palette
                    {
                        Background = new Color(0.02f, 0.04f, 0.03f),
                        Ground = new Color(0.1f, 0.13f, 0.08f),
                        Path = new Color(0.7f, 0.64f, 0.42f),
                        Slot = new Color(0.45f, 0.8f, 0.35f),
                        Base = new Color(0.6f, 1f, 0.5f),
                        Light = new Color(0.88f, 0.94f, 0.84f),
                        LightIntensity = 1.15f,
                        Sky = new Color(0.22f, 0.3f, 0.22f),
                        Fog = new Color(0.05f, 0.08f, 0.05f),
                        FogDensity = 0.024f,
                        Bloom = 1.4f,
                        Rock = new Color(0.22f, 0.24f, 0.18f),
                        Accent = new Color(0.65f, 1f, 0.45f),
                        Water = new Color(0.08f, 0.3f, 0.28f),
                        Lava = new Color(1f, 0.4f, 0.1f),
                        Sand = new Color(0.35f, 0.32f, 0.22f),
                        Ice = new Color(0.72f, 0.88f, 1f),
                        Bog = new Color(0.14f, 0.2f, 0.1f),
                        Spawn = new Color(0.6f, 0.95f, 0.35f),
                        Props = new[] { PropKind.Reed, PropKind.Reed, PropKind.DeadTree, PropKind.DeadTree, PropKind.Mushroom, PropKind.Tendril, PropKind.Rock, PropKind.Frond },
                        Vein = 0.6f,
                        AmbientRate = 24f,
                        AmbientRise = 0.12f,
                        Ambience = AmbienceKind.Marsh,
                        Plant = new Color(0.2f, 0.42f, 0.2f),
                    },
                    // Causeways wind between four bogs; a second road comes in from the north-west.
                    Size = new Vector2Int(38, 21),
                    RoadWidth = 2,
                    Road = Cells(0, 17, 9, 17, 9, 6, 17, 6, 17, 13, 27, 13, 27, 4, 33, 4, 33, 10, 37, 10),
                    SecondRoad = Cells(0, 2, 5, 2, 5, 6, 9, 6),
                    Plazas = Rect(26, 11, 29, 14),
                    NorthAir = Cells(0, 20, 20, 19, 37, 10),
                    SouthAir = Cells(0, 0, 22, 1, 37, 10),
                    BlockadeCells = Cells(4, 17, 9, 10, 22, 13, 33, 7),
                    BlockedSeed = 34,
                    Terrain = Terrain(
                        Area(TerrainKind.Bog, 11, 9, 15, 12), Area(TerrainKind.Bog, 20, 8, 25, 11), Area(TerrainKind.Bog, 29, 15, 34, 19), Area(TerrainKind.Bog, 1, 8, 4, 12),
                        Area(TerrainKind.Water, 12, 18, 16, 20),
                        Area(TerrainKind.Raised, 29, 0, 34, 1), Area(TerrainKind.Raised, 0, 14, 3, 15)),
                    Money = 340,
                    BaseHealth = BaseHealth,
                    HealthScale = 1.25f,
                    UpgradeBonus = 0.4f,
                    RewardTower = roster.Support,
                    Source = UnlockSource.StolenAlienTech,
                    Story = "The marsh slugs secrete a resin that carries energy without loss. Wrapped around a mast, it feeds the guns beside it.",
                    Research = 60,
                    Power = 24,
                    PathObjects = new[] { slowField, mine, lavaBall, barrel, groundDrone, antiAir },
                    Recipes = recipes,
                    // The slime monarch heaves itself out of the middle bog.
                    Entrance = EntranceKind.Pool,
                    EntranceCell = new Vector2Int(23, 10),
                    // Mire bats rise from three bogs and tide bats from the drowned corner.
                    Nests = new[] { Nest(Element.Bog, 13, 10), Nest(Element.Bog, 22, 9), Nest(Element.Bog, 31, 17), Nest(Element.Water, 14, 19) },
                },
            };

            var levels = new LevelDefinition[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                levels[i] = ConfigureWorldLevel(specs[i], roster, difficulties, enemies, blockade, hero, repairDrone, harvestDrone, attackDrone);
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
        private static TerrainPatch[] WriteTerrain(SerializedProperty array, TerrainPatch[] patches, HashSet<Vector2Int> road, Vector2Int[] blockades)
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

            return kept;
        }

        /// <summary>Every cell of a rectangle, both corners included: the shape of a plaza.</summary>
        private static Vector2Int[] Rect(int x0, int y0, int x1, int y1)
        {
            var cells = new List<Vector2Int>();
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }

            return cells.ToArray();
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
            Bestiary enemies,
            TowerDefinition blockade,
            HeroDefinition hero,
            DroneDefinition repairDrone,
            DroneDefinition harvestDrone,
            DroneDefinition attackDrone)
        {
            var theme = LoadOrCreate<VisualTheme>($"{DataRoot}/Levels/Theme_{spec.Key}.asset");
            ConfigureThemePalette(theme, spec.Palette);

            var level = LoadOrCreate<LevelDefinition>($"{DataRoot}/Levels/Level_{spec.Key}.asset");
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = spec.Name;
            so.FindProperty("_regionName").stringValue = spec.Region;
            so.FindProperty("_briefing").stringValue = spec.Briefing;
            so.FindProperty("_gridSize").vector2IntValue = spec.Size;
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_theme").objectReferenceValue = theme;
            SerializedProperty camera = so.FindProperty("_camera");
            camera.FindPropertyRelative("PitchDegrees").floatValue = CameraPitchDegrees;
            camera.FindPropertyRelative("YawDegrees").floatValue = CameraYawDegrees;
            camera.FindPropertyRelative("FitMargin").floatValue = 0.04f;
            so.FindProperty("_startingMoney").intValue = spec.Money;
            so.FindProperty("_enemyHealthMultiplier").floatValue = spec.HealthScale;
            so.FindProperty("_expectedUpgradeBonus").floatValue = spec.UpgradeBonus;
            so.FindProperty("_baseHealth").intValue = spec.BaseHealth;
            so.FindProperty("_mineStock").intValue = MineStock;
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
                paths.Add(("Second road", false, MergeIntoMain(spec.SecondRoad, spec.Road)));
            }

            WritePaths(so.FindProperty("_paths"), paths.ToArray());
            so.FindProperty("_roadWidth").intValue = spec.RoadWidth;
            Vector2Int[] plazas = spec.Plazas ?? new Vector2Int[0];
            WriteCells(so.FindProperty("_plazaCells"), plazas);
            HashSet<Vector2Int> road = RoadCells(spec.Size, paths, spec.RoadWidth, plazas);

            TerrainPatch[] terrain = spec.Terrain ?? new TerrainPatch[0];
            // Nests reserve their cells like blockades do, so no rock or mine lands on one.
            Vector2Int[] reserved = spec.BlockadeCells.Concat(NestCells(spec.Nests)).ToArray();
            Vector2Int[] blockedCells = ScatterBlocked(spec.Size, road, terrain, reserved, spec.BlockedSeed, BlockedFraction);
            WriteCells(so.FindProperty("_blockedCells"), blockedCells);
            WriteCells(so.FindProperty("_mineCells"), PickMineCells(spec.Size, road, terrain, blockedCells, reserved, spec.BlockedSeed + 5));
            TerrainPatch[] kept = WriteTerrain(so.FindProperty("_terrain"), terrain, road, spec.BlockadeCells);
            WriteNests(so.FindProperty("_nests"), spec.Nests, kept);

            // Bosses come up where the level says; a pool entrance snaps to the nearest liquid cell that survived erosion.
            SerializedProperty entrance = so.FindProperty("_bossEntrance");
            entrance.FindPropertyRelative("Kind").enumValueIndex = (int)spec.Entrance;
            entrance.FindPropertyRelative("Cell").vector2IntValue = spec.Entrance == EntranceKind.Pool ? NearestLiquid(kept, spec.EntranceCell) : spec.EntranceCell;

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
            List<DifficultyWaves> waveSets = BuildWaves(spec.Key, difficulties, spec.Script, secondRoadIndex);
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
            ConfigureLevelExtras(level, spec.PathObjects, spec.Recipes, repairDrone, harvestDrone, attackDrone);
            return level;
        }
    }
}
