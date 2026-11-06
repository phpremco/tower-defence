using System;
using System.Collections.Generic;
using System.Linq;
using Game.Balance;
using Game.Core;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the campaign levels: every <see cref="CampaignLevelSpec"/> becomes a level asset. The map comes
    /// from the pure layout generator: attempts are tried in a fixed order, the pure <see cref="LayoutChecks"/> choose,
    /// the game's <see cref="LevelValidator"/> asserts on the written asset, and the first attempt that passes both
    /// stays, so the same spec always gives the same level. Waves are solved later, once the whole campaign exists.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const int ScatterSeedFactor = 31;
        private const int MineSeedOffset = 5;

        private static void GenerateCampaignLevels(GeneratorContext context)
        {
            context.CampaignLevels.Clear();
            var forSale = new List<PathObjectDefinition>();
            CampaignLevelSpec[][] continents = LevelSpecs.Continents;
            for (int c = 0; c < continents.Length; c++)
            {
                for (int i = 0; i < continents[c].Length; i++)
                {
                    CampaignLevelSpec spec = continents[c][i];
                    LevelDefinition level = BuildCampaignLevel(spec, c, forSale.ToArray(), context);
                    context.CampaignLevels.Add(new SpecLevel(spec, level, c, i));

                    // A gadget is sold from the level after the one that grants it: the script's N-1 rule.
                    foreach (string id in spec.RewardGadgets)
                    {
                        PathObjectDefinition gadget = PathObjectById(context, id);
                        if (!forSale.Contains(gadget))
                        {
                            forSale.Add(gadget);
                        }
                    }
                }
            }
        }

        /// <summary>Writes a spec's field ease (balance model only) into its level; also used by Tools/PlayTest/Scripts/ApplyRoles.cs.</summary>
        internal static void WriteFieldEase(SerializedObject level, CampaignLevelSpec spec)
        {
            SerializedProperty ease = level.FindProperty("_balanceFieldEase");
            float[] values = spec.FieldEase != null && spec.FieldEase.Length > 0 ? spec.FieldEase : new[] { 1f };
            ease.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                ease.GetArrayElementAtIndex(i).floatValue = values[i];
            }
        }

        private static LevelDefinition BuildCampaignLevel(CampaignLevelSpec spec, int continent, PathObjectDefinition[] forSale, GeneratorContext context)
        {
            var theme = LoadOrCreate<VisualTheme>($"{DataRoot}/Levels/Theme_{spec.Key}.asset");
            ConfigureThemePalette(theme, ContinentPalette(spec.Tint));
            EditorUtility.SetDirty(theme);

            var level = LoadOrCreate<LevelDefinition>($"{DataRoot}/Levels/Level_{spec.Key}.asset");
            WriteLevelBasics(level, spec, continent, theme, context);
            ConfigureLevelExtras(level, forSale, new CombineRecipe[0]);
            ConfigureLevelDrones(level, spec.Key, context);

            var failures = new List<string>();
            for (int attempt = 0; attempt < RoadGenerator.Attempts; attempt++)
            {
                RoadLayout layout = RoadGenerator.Build(spec.Layout, attempt);
                List<string> errors = LayoutChecks.Check(layout, spec.Layout, BalanceSheet.Rules);
                if (errors.Count == 0)
                {
                    WriteLayout(level, spec, layout, context.Blockade);
                    errors = ValidateLayoutCopy(level);
                    if (errors.Count == 0)
                    {
                        EditorUtility.SetDirty(level);
                        return level;
                    }
                }

                failures.Add($"attempt {attempt}: {string.Join("; ", errors)}");
            }

            throw new InvalidOperationException($"Level {spec.Key}: none of {RoadGenerator.Attempts} layout attempts passed.\n{string.Join("\n", failures)}");
        }

        // The validator runs on a fresh copy: the asset caches its road and terrain, which the write just changed.
        private static List<string> ValidateLayoutCopy(LevelDefinition level)
        {
            LevelDefinition copy = Object.Instantiate(level);
            var errors = new List<string>();
            try
            {
                LevelValidator.ValidateLayout(copy, errors);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }

            return errors;
        }

        // Everything about a level that does not depend on its map.
        private static void WriteLevelBasics(LevelDefinition level, CampaignLevelSpec spec, int continent, VisualTheme theme, GeneratorContext context)
        {
            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = spec.Name;
            so.FindProperty("_regionName").stringValue = spec.Area;
            so.FindProperty("_briefing").stringValue = spec.Briefing;
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_theme").objectReferenceValue = theme;
            SerializedProperty camera = so.FindProperty("_camera");
            camera.FindPropertyRelative("PitchDegrees").floatValue = CameraPitchDegrees;
            camera.FindPropertyRelative("YawDegrees").floatValue = CameraYawDegrees;
            camera.FindPropertyRelative("FitMargin").floatValue = 0.04f;
            so.FindProperty("_startingMoney").intValue = spec.Money;
            so.FindProperty("_enemyHealthMultiplier").floatValue = 1f;
            WriteFieldEase(so, spec);
            so.FindProperty("_baseHealth").intValue = spec.BaseHealth;
            so.FindProperty("_mineStock").intValue = MineStock;
            so.FindProperty("_hero").objectReferenceValue = context.Hero;
            so.FindProperty("_baseTurret").objectReferenceValue = context.BaseTurret;
            SerializedProperty stars = so.FindProperty("_starBaseHealthFractions");
            stars.arraySize = 2;
            stars.GetArrayElementAtIndex(0).floatValue = 0.5f;
            stars.GetArrayElementAtIndex(1).floatValue = 0.9f;

            SerializedProperty reward = so.FindProperty("_reward");
            reward.FindPropertyRelative("NewTower").objectReferenceValue = string.IsNullOrEmpty(spec.RewardTower) ? null : TowerById(context, spec.RewardTower);
            reward.FindPropertyRelative("Source").intValue = (int)(UnlockSource)Enum.Parse(typeof(UnlockSource), spec.Source);
            reward.FindPropertyRelative("Story").stringValue = spec.RewardStory;
            reward.FindPropertyRelative("Credit").stringValue = spec.Credit;
            SetReferences(reward.FindPropertyRelative("NewPathObjects"), spec.RewardGadgets.Select(id => (Object)PathObjectById(context, id)).ToArray());
            reward.FindPropertyRelative("HeroPerk").objectReferenceValue = string.IsNullOrEmpty(spec.RewardHeroPerk) ? null : context.HeroPerks[spec.RewardHeroPerk];

            // The bar shows only what is unlocked, so every level sells its continent's whole roster.
            string[] roster = LevelSpecs.Rosters[continent];
            SetReferences(so.FindProperty("_availableTowers"), roster.Select(id => (Object)TowerById(context, id)).ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteLayout(LevelDefinition level, CampaignLevelSpec spec, RoadLayout layout, TowerDefinition blockade)
        {
            var so = new SerializedObject(level);
            var size = new Vector2Int(layout.Width, layout.Height);
            so.FindProperty("_gridSize").vector2IntValue = size;

            // Road 0, flying routes 1 and 2, and an optional second road as path 3, which the waves use by index.
            Vector2Int[] main = Vectors(layout.Road);
            var paths = new List<(string Name, bool Flying, Vector2Int[] Waypoints)>
            {
                ("Road", false, main),
                ("North air route", true, Vectors(layout.NorthAir)),
                ("South air route", true, Vectors(layout.SouthAir)),
            };
            if (layout.SecondRoad != null)
            {
                paths.Add(("Second road", false, MergeIntoMain(Vectors(layout.SecondRoad), main)));
            }

            WritePaths(so.FindProperty("_paths"), paths.ToArray());
            so.FindProperty("_roadWidth").intValue = layout.RoadWidth;
            Vector2Int[] plazas = Vectors(layout.PlazaCells);
            WriteCells(so.FindProperty("_plazaCells"), plazas);
            HashSet<Vector2Int> road = RoadCells(size, paths, layout.RoadWidth, plazas);

            TerrainPatch[] terrain = layout.Terrain.Select(p => new TerrainPatch { Cell = Vector(p.Cell), Kind = (TerrainKind)p.Kind }).ToArray();
            ElementNest[] nests = layout.Nests.Select(n => new ElementNest { Cell = Vector(n.Cell), Element = (Element)Enum.Parse(typeof(Element), n.Element) }).ToArray();
            Vector2Int[] gates = Vectors(layout.Gates);

            // Gates and nests reserve their cells, so no rock or mining node lands on one.
            Vector2Int[] reserved = gates.Concat(NestCells(nests)).ToArray();
            int seed = spec.Layout.Seed * ScatterSeedFactor + layout.Attempt;
            Vector2Int[] blocked = ScatterBlocked(size, road, terrain, reserved, seed, BlockedFraction);
            WriteCells(so.FindProperty("_blockedCells"), blocked);
            WriteCells(so.FindProperty("_mineCells"), PickMineCells(size, road, terrain, blocked, reserved, seed + MineSeedOffset));
            TerrainPatch[] kept = WriteTerrain(so.FindProperty("_terrain"), terrain, road, gates);
            WriteNests(so.FindProperty("_nests"), nests, kept);

            // The first continent's bosses come through the main gate (Docs/LEVEL_GUIDE.md).
            SerializedProperty entrance = so.FindProperty("_bossEntrance");
            entrance.FindPropertyRelative("Kind").enumValueIndex = (int)EntranceKind.Gate;
            entrance.FindPropertyRelative("Cell").vector2IntValue = Vector2Int.zero;

            SerializedProperty blockades = so.FindProperty("_blockades");
            blockades.arraySize = gates.Length;
            for (int i = 0; i < gates.Length; i++)
            {
                SerializedProperty element = blockades.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Cell").vector2IntValue = gates[i];
                element.FindPropertyRelative("Blockade").objectReferenceValue = blockade;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Vector2Int Vector(Cell cell) => new Vector2Int(cell.X, cell.Y);

        private static Vector2Int[] Vectors(IEnumerable<Cell> cells) => cells.Select(Vector).ToArray();

        private static TowerDefinition TowerById(GeneratorContext context, string id)
        {
            TowerDefinition tower = context.Roster.Standard.Concat(context.Roster.Hybrids).FirstOrDefault(t => t.Id == id);
            return tower != null ? tower : throw new InvalidOperationException($"No tower with id '{id}'.");
        }

        private static PathObjectDefinition PathObjectById(GeneratorContext context, string id)
        {
            PathObjectDefinition gadget = context.PathObjects.FirstOrDefault(p => p.name == id);
            return gadget != null ? gadget : throw new InvalidOperationException($"No path object with id '{id}'.");
        }

        // Crystal Plains: grey-blue stone under a violet sky, glowing crystal everywhere. Each level tints the palette
        // with its dominant element, so the continent reads as one place with its own corners.
        private static Palette ContinentPalette(string tint)
        {
            var palette = new Palette
            {
                Background = new Color(0.03f, 0.03f, 0.07f),
                Ground = new Color(0.15f, 0.16f, 0.22f),
                Path = new Color(0.8f, 0.84f, 0.96f),
                Slot = new Color(0.3f, 0.55f, 1f),
                Base = new Color(0.35f, 0.85f, 1f),
                Light = new Color(0.94f, 0.94f, 1f),
                LightIntensity = 1.25f,
                Sky = new Color(0.32f, 0.32f, 0.46f),
                Fog = new Color(0.04f, 0.04f, 0.09f),
                FogDensity = 0.01f,
                Bloom = 1.3f,
                Rock = new Color(0.36f, 0.36f, 0.44f),
                Accent = new Color(0.6f, 0.62f, 1f),
                Water = new Color(0.08f, 0.36f, 0.6f),
                Lava = new Color(1f, 0.42f, 0.1f),
                Sand = new Color(0.55f, 0.52f, 0.58f),
                Props = new[] { PropKind.Rock, PropKind.Crystal, PropKind.Boulder, PropKind.Crystal, PropKind.Frond, PropKind.Rock },
                Vein = 0.55f,
                AmbientRate = 10f,
                AmbientRise = 0.1f,
                Ambience = AmbienceKind.Wind,
                Plant = new Color(0.32f, 0.42f, 0.56f),
            };

            switch (tint)
            {
                case "Wood":
                    palette.Ground = new Color(0.12f, 0.17f, 0.18f);
                    palette.Accent = new Color(0.4f, 0.95f, 0.85f);
                    palette.Plant = new Color(0.2f, 0.5f, 0.44f);
                    palette.Props = new[] { PropKind.Frond, PropKind.GlowTree, PropKind.Crystal, PropKind.Frond, PropKind.Rock, PropKind.Mushroom };
                    palette.Ambience = AmbienceKind.Forest;
                    palette.AmbientRate = 18f;
                    break;
                case "Sand":
                    palette.Ground = new Color(0.2f, 0.18f, 0.2f);
                    palette.Sand = new Color(0.62f, 0.55f, 0.44f);
                    palette.Accent = new Color(0.9f, 0.78f, 1f);
                    palette.Rock = new Color(0.44f, 0.4f, 0.42f);
                    palette.Props = new[] { PropKind.Rock, PropKind.Boulder, PropKind.Crystal, PropKind.Spire, PropKind.Rock, PropKind.Boulder };
                    break;
                case "Water":
                    palette.Ground = new Color(0.13f, 0.16f, 0.23f);
                    palette.Water = new Color(0.06f, 0.42f, 0.66f);
                    palette.Accent = new Color(0.4f, 0.8f, 1f);
                    palette.Plant = new Color(0.24f, 0.44f, 0.5f);
                    palette.Props = new[] { PropKind.Frond, PropKind.Rock, PropKind.Crystal, PropKind.Reed, PropKind.Boulder, PropKind.Frond };
                    break;
                case "Stone":
                    // Grey-ochre basalt cut by amber rifts: the warmest of the plains without turning into lava.
                    palette.Ground = new Color(0.17f, 0.16f, 0.17f);
                    palette.Rock = new Color(0.42f, 0.38f, 0.36f);
                    palette.Accent = new Color(1f, 0.7f, 0.45f);
                    palette.Rift = new Color(1f, 0.6f, 0.28f);
                    palette.Vein = 0.65f;
                    palette.Props = new[] { PropKind.Boulder, PropKind.Rock, PropKind.Scree, PropKind.Crystal, PropKind.Boulder, PropKind.Spire };
                    break;
                case "Vein":
                    // The Crysium vein: bright crystal light running through the ground, more bloom, cyan and violet.
                    palette.Ground = new Color(0.12f, 0.12f, 0.2f);
                    palette.Accent = new Color(0.55f, 0.9f, 1f);
                    palette.Vein = 0.7f;
                    palette.Bloom = 1.6f;
                    palette.Props = new[] { PropKind.Crystal, PropKind.Spire, PropKind.Crystal, PropKind.Rock, PropKind.Crystal, PropKind.Boulder };
                    palette.AmbientRate = 16f;
                    break;
                case "Fire":
                    palette.Ground = new Color(0.17f, 0.13f, 0.16f);
                    palette.Accent = new Color(1f, 0.6f, 0.4f);
                    palette.Rock = new Color(0.36f, 0.3f, 0.34f);
                    palette.Bloom = 1.6f;
                    palette.Vein = 0.75f;
                    palette.Props = new[] { PropKind.Crystal, PropKind.Spike, PropKind.Rock, PropKind.Crystal, PropKind.Boulder, PropKind.Spire };
                    palette.Ambience = AmbienceKind.Lava;
                    palette.AmbientRate = 20f;
                    palette.AmbientRise = 0.4f;
                    break;
            }

            return palette;
        }
    }
}
