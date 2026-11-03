using System.Collections.Generic;
using System.IO;
using Game.Balance;
using Game.Hero;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the campaign around the levels: the globe's continents with their level sites, the hero perks
    /// the levels grant, and the retirement of the seven levels the continent campaign replaced.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string PerkFolder = DataRoot + "/Hero/Perks";

        /// <summary>A continent on the globe as the generator writes it.</summary>
        private readonly struct ContinentSpec
        {
            public ContinentSpec(string key, string name, float longitude, float latitude, float radius, Color colour, MapLayer layer, float bearing)
            {
                Key = key;
                Name = name;
                Longitude = longitude;
                Latitude = latitude;
                Radius = radius;
                Colour = colour;
                Layer = layer;
                Bearing = bearing;
            }

            public string Key { get; }
            public string Name { get; }
            public float Longitude { get; }
            public float Latitude { get; }
            public float Radius { get; }
            public Color Colour { get; }
            public MapLayer Layer { get; }

            /// <summary>Which way the rows of level sites run (degrees from east towards north); the first site is at the start of row one.</summary>
            public float Bearing { get; }
        }

        // The six continents of STORY.md. Surface centres lie at least 77 degrees apart, so the 30 degree caps never meet,
        // and the Crowns reach their poles. The Core is a fissure on Southern Crown's north rim, under the surface.
        private static readonly ContinentSpec[] ContinentTable =
        {
            new ContinentSpec("C1", "Crystal Plains", -40f, 6f, 30f, new Color(0.48f, 0.5f, 0.72f), MapLayer.Surface, 0f),
            new ContinentSpec("C2", "Ashen Reach", 60f, -15f, 30f, new Color(0.58f, 0.36f, 0.26f), MapLayer.Surface, 0f),
            new ContinentSpec("C3", "Northern Crown", 60f, 62f, 30f, new Color(0.78f, 0.84f, 0.9f), MapLayer.Surface, 0f),
            new ContinentSpec("C4", "Mirewood", -140f, 15f, 30f, new Color(0.26f, 0.42f, 0.3f), MapLayer.Surface, 0f),
            new ContinentSpec("C5", "Southern Crown", 150f, -60f, 30f, new Color(0.74f, 0.8f, 0.88f), MapLayer.Surface, 0f),
            new ContinentSpec("C6", "The Core", 150f, -32f, 12f, new Color(0.35f, 0.1f, 0.06f), MapLayer.Core, 0f),
        };

        // The levels the continent campaign replaced. Their ids are never reused; their data goes, their code support stays.
        private static readonly string[] RetiredLevelKeys = { "Landing", "Forest", "Lava", "Ruins", "Nest", "Ice", "Swamp" };

        private static CampaignCatalog CreateCampaign(GeneratorContext context)
        {
            var catalog = LoadOrCreate<CampaignCatalog>($"{DataRoot}/Levels/CampaignCatalog.asset");
            var so = new SerializedObject(catalog);

            SerializedProperty regionArray = so.FindProperty("_regions");
            regionArray.arraySize = ContinentTable.Length;
            for (int i = 0; i < ContinentTable.Length; i++)
            {
                ContinentSpec continent = ContinentTable[i];
                SerializedProperty element = regionArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("DisplayName").stringValue = continent.Name;
                element.FindPropertyRelative("Longitude").floatValue = continent.Longitude;
                element.FindPropertyRelative("Latitude").floatValue = continent.Latitude;
                element.FindPropertyRelative("RadiusDegrees").floatValue = continent.Radius;
                element.FindPropertyRelative("Colour").colorValue = continent.Colour;
                element.FindPropertyRelative("Key").stringValue = continent.Key;
                element.FindPropertyRelative("Layer").enumValueIndex = (int)continent.Layer;
            }

            // A continent with level specs holds all twenty of its sites; the ones without a level yet show "coming soon".
            var nodes = new List<(string Name, GlobePoint Place, int Region, LevelDefinition Level)>();
            for (int c = 0; c < ContinentTable.Length; c++)
            {
                List<SpecLevel> levels = context.CampaignLevels.FindAll(l => l.Continent == c);
                if (levels.Count == 0)
                {
                    continue;
                }

                ContinentSpec continent = ContinentTable[c];
                GlobePoint[] places = NodePlacer.Route(continent.Longitude, continent.Latitude, continent.Radius, BalanceSheet.Rules.LevelsPerContinent, continent.Bearing, SiteRuggedness);
                for (int i = 0; i < places.Length; i++)
                {
                    SpecLevel level = levels.Find(l => l.Index == i);
                    nodes.Add((level != null ? level.Spec.Name : $"{continent.Name} {i + 1}", places[i], c, level?.Level));
                }
            }

            SerializedProperty nodeArray = so.FindProperty("_nodes");
            nodeArray.arraySize = nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
            {
                SerializedProperty element = nodeArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("DisplayName").stringValue = nodes[i].Name;
                element.FindPropertyRelative("Longitude").floatValue = nodes[i].Place.Longitude;
                element.FindPropertyRelative("Latitude").floatValue = nodes[i].Place.Latitude;
                element.FindPropertyRelative("Region").intValue = nodes[i].Region;
                element.FindPropertyRelative("Level").objectReferenceValue = nodes[i].Level;
            }

            DifficultyProfile[] difficulties = context.Difficulties;
            SerializedProperty difficultyArray = so.FindProperty("_difficulties");
            difficultyArray.arraySize = difficulties.Length;
            for (int i = 0; i < difficulties.Length; i++)
            {
                difficultyArray.GetArrayElementAtIndex(i).objectReferenceValue = difficulties[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        // The route of level sites keeps off the mountain chains the globe painter draws.
        private static float SiteRuggedness(GlobePoint place) => Game.UI.GlobePainter.Ruggedness(PlanetCoordinates.ToDirection(place.Longitude, place.Latitude));

        // STORY.md, hero upgrades of continent 1: the pistol at level 5, the blade at 10, an ability at 15.
        private static void GenerateHeroPerks(GeneratorContext context)
        {
            AddPerk(context, "Perk_QuickReload", "Quick Reload", HeroPerkSlot.Pistol, "Mercer reloads his pistol in half the time.", reload: QuickReloadMultiplier);
            AddPerk(context, "Perk_WideSlash", "Wide Slash", HeroPerkSlot.Blade, "A wider sweep of the laser blade that hits several enemies at once.");
            AddPerk(context, "Perk_FieldRepair", "Field Repair", HeroPerkSlot.Ability, "Mercer repairs the gate and the turrets around him.");
            AssignHeroPerks(context);
        }

        private static void AddPerk(GeneratorContext context, string id, string displayName, HeroPerkSlot slot, string description, float interval = 1f, float reload = 1f)
        {
            var perk = LoadOrCreate<HeroPerkDefinition>($"{PerkFolder}/{id}.asset");
            var so = new SerializedObject(perk);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_slot").enumValueIndex = (int)slot;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_rangedIntervalMultiplier").floatValue = interval;
            so.FindProperty("_reloadMultiplier").floatValue = reload;
            so.ApplyModifiedPropertiesWithoutUndo();
            context.HeroPerks[id] = perk;
        }

        // Deletes the replaced levels' data: level, theme, waves and baked ground. Runs before the campaign is rebuilt and
        // does nothing once they are gone.
        private static void RetireOldLevels()
        {
            foreach (string key in RetiredLevelKeys)
            {
                string[] paths =
                {
                    $"{DataRoot}/Levels/Level_{key}.asset",
                    $"{DataRoot}/Levels/Theme_{key}.asset",
                    $"{WavesFolder}/Waves_{key}.asset",
                    $"{TerrainArtFolder}/Level_{key}_albedo.png",
                    $"{TerrainArtFolder}/Level_{key}_emission.png",
                };
                foreach (string path in paths)
                {
                    if (File.Exists(path) && !AssetDatabase.DeleteAsset(path))
                    {
                        throw new System.InvalidOperationException($"Could not delete the retired asset {path}.");
                    }
                }
            }
        }
    }
}
