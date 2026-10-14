using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for level layouts: how big the landing zone is and where rock formations stand.</summary>
    public static partial class ProjectGenerator
    {
        private const float BlockedFraction = 0.13f;
        private static readonly Vector2Int LandingSize = new Vector2Int(20, 12);
        private static readonly Vector2Int[] LandingRoad = Cells(0, 10, 15, 10, 15, 7, 4, 7, 4, 4, 14, 4, 14, 1, 19, 1);

        // Rock formations that keep out of the way: never on or next to the road (so towers stay possible there),
        // never on terrain patches or blockades. The same seed always gives the same cells.
        private static Vector2Int[] ScatterBlocked(Vector2Int size, HashSet<Vector2Int> road, TerrainPatch[] terrain, Vector2Int[] blockades, int seed, float fraction)
        {
            var taken = new HashSet<Vector2Int>(terrain.Select(p => p.Cell));
            var result = new List<Vector2Int>();
            var random = new System.Random(seed);

            for (int x = 1; x < size.x - 1; x++)
            {
                for (int y = 1; y < size.y - 1; y++)
                {
                    var cell = new Vector2Int(x, y);
                    bool skip = random.NextDouble() > fraction || taken.Contains(cell) || NearAny(road, cell, 1) || NearAny(blockades, cell, 2);
                    if (!skip)
                    {
                        result.Add(cell);
                    }
                }
            }

            return result.ToArray();
        }


        private const string TerrainArtFolder = "Assets/_Project/Art/Terrain";
        private const int TerrainTextureMaxSize = 2048;

        // Painting the ground takes seconds, so it happens here once and the level only loads the pictures.
        private static void BakeTerrain(LevelDefinition level)
        {
            var road = new RoadShape(level);
            var field = new TerrainField(level, road);
            TerrainPainter.Result painted = TerrainPainter.Paint(level, level.Theme, field, road);

            var so = new SerializedObject(level);
            so.FindProperty("_terrainAlbedo").objectReferenceValue = SaveTerrainTexture(painted.Albedo, $"{level.name}_albedo");
            so.FindProperty("_terrainEmission").objectReferenceValue = SaveTerrainTexture(painted.Emission, $"{level.name}_emission");
            so.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(painted.Albedo);
            Object.DestroyImmediate(painted.Emission);
        }

        private static Texture2D SaveTerrainTexture(Texture2D texture, string name)
        {
            if (!AssetDatabase.IsValidFolder(TerrainArtFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Terrain");
            }

            string path = $"{TerrainArtFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = TerrainTextureMaxSize;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }


        // Every tower and enemy wears a procedural model chosen by data; the pulse rifle keeps its hand-made gun.
        private static void SetShapes(
            Roster roster,
            TowerDefinition cannon,
            TowerDefinition rocket,
            EnemyDefinition scout,
            EnemyDefinition grunt,
            EnemyDefinition brute,
            EnemyDefinition flyer,
            EnemyDefinition overseer,
            EnemyDefinition warden,
            EnemyDefinition queen)
        {
            SetShape(cannon, "_shape", (int)TowerShape.Cannon);
            SetShape(rocket, "_shape", (int)TowerShape.Rocket);
            SetShape(roster.Laser, "_shape", (int)TowerShape.Laser);
            SetShape(roster.Cryo, "_shape", (int)TowerShape.Cryo);
            SetShape(roster.Tesla, "_shape", (int)TowerShape.Tesla);
            SetShape(roster.Railgun, "_shape", (int)TowerShape.Railgun);
            SetShape(roster.Support, "_shape", (int)TowerShape.Support);
            SetShape(roster.Artillery, "_shape", (int)TowerShape.Artillery);
            SetShape(roster.StormRifle, "_shape", (int)TowerShape.StormRifle);
            SetShape(roster.CryoBeam, "_shape", (int)TowerShape.CryoBeam);

            SetShape(scout, "_shape", (int)EnemyShape.Scout);
            SetShape(grunt, "_shape", (int)EnemyShape.Grunt);
            SetShape(brute, "_shape", (int)EnemyShape.Brute);
            SetShape(flyer, "_shape", (int)EnemyShape.Flyer);
            SetShape(overseer, "_shape", (int)EnemyShape.Overseer);
            SetShape(warden, "_shape", (int)EnemyShape.Warden);
            SetShape(queen, "_shape", (int)EnemyShape.Queen);
        }

        private static void SetShape(Object asset, string field, int value)
        {
            var so = new SerializedObject(asset);
            so.FindProperty(field).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool NearAny(IEnumerable<Vector2Int> cells, Vector2Int cell, int distance) =>
            cells.Any(c => Mathf.Abs(c.x - cell.x) <= distance && Mathf.Abs(c.y - cell.y) <= distance);
    }
}
