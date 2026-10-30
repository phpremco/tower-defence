using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Combat;
using Game.Enemies;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for level layouts: how big the landing zone is and where rock formations stand.</summary>
    public static partial class ProjectGenerator
    {
        private const float BlockedFraction = 0.13f;
        // A three-leg serpentine on the plain. Roads are two cells wide, with the second row to the right of travel.
        private static readonly Vector2Int LandingSize = new Vector2Int(32, 18);
        private const int LandingRoadWidth = 2;
        // A long serpentine across the plain: three switchbacks before the road climbs north and drops to the lander.
        private static readonly Vector2Int[] LandingRoad = Cells(0, 16, 13, 16, 13, 11, 3, 11, 3, 8, 20, 8, 20, 13, 28, 13, 28, 3, 31, 3);

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
        private static void SetShapes(Roster roster, TowerDefinition cannon, TowerDefinition rocket, TowerDefinition blockade)
        {
            SetShape(roster.Pulse, "_shape", (int)TowerShape.PulseRifle);
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
            SetShape(blockade, "_shape", (int)TowerShape.GateGun);

            SetShape(roster.Pulse, "_projectileStyle", (int)ProjectileStyle.Bolt);
            SetShape(cannon, "_projectileStyle", (int)ProjectileStyle.Shell);
            SetShape(rocket, "_projectileStyle", (int)ProjectileStyle.Rocket);
            SetShape(roster.Laser, "_projectileStyle", (int)ProjectileStyle.Laser);
            SetShape(roster.Cryo, "_projectileStyle", (int)ProjectileStyle.Frost);
            SetShape(roster.Tesla, "_projectileStyle", (int)ProjectileStyle.Lightning);
            SetShape(roster.Railgun, "_projectileStyle", (int)ProjectileStyle.Rail);
            SetShape(roster.Support, "_projectileStyle", (int)ProjectileStyle.Bolt);
            SetShape(roster.Artillery, "_projectileStyle", (int)ProjectileStyle.Mortar);
            SetShape(roster.StormRifle, "_projectileStyle", (int)ProjectileStyle.Lightning);
            SetShape(roster.CryoBeam, "_projectileStyle", (int)ProjectileStyle.Laser);
            SetShape(blockade, "_projectileStyle", (int)ProjectileStyle.Bolt);

            SetLooks();
        }

        // Look of each path object, by asset name.
        private static void SetLooks()
        {
            var looks = new (string Id, PathObjectLook Look)[]
            {
                ("SlowField", PathObjectLook.FieldEmitter), ("Mine", PathObjectLook.Mine), ("LavaBall", PathObjectLook.LavaBall),
                ("ExplosiveBarrel", PathObjectLook.Barrel), ("GroundDrone", PathObjectLook.GroundDrone), ("AntiAirNet", PathObjectLook.AirNet),
            };

            foreach ((string id, PathObjectLook look) in looks)
            {
                var asset = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>($"{DataRoot}/PathObjects/{id}.asset");
                if (asset != null)
                {
                    SetShape(asset, "_look", (int)look);
                }
            }
        }

        private static void SetShape(Object asset, string field, int value)
        {
            var so = new SerializedObject(asset);
            so.FindProperty(field).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A side road must carry on along the main road after the junction, so its enemies walk to the base too.
        // Every road cell of a level in the making: all rows of every road, plus the plazas.
        private static HashSet<Vector2Int> RoadCells(Vector2Int size, List<(string Name, bool Flying, Vector2Int[] Waypoints)> paths, int width, Vector2Int[] plazas)
        {
            var enemyPaths = new EnemyPath[paths.Count];
            for (int i = 0; i < enemyPaths.Length; i++)
            {
                enemyPaths[i] = new EnemyPath { Name = paths[i].Name, Flying = paths[i].Flying, Waypoints = paths[i].Waypoints };
            }

            return new HashSet<Vector2Int>(new RoadCorridor(new GridMap(size, 1f), enemyPaths, width, plazas).Cells);
        }

        private static Vector2Int[] MergeIntoMain(Vector2Int[] side, Vector2Int[] main)
        {
            Vector2Int junction = side[side.Length - 1];
            int index = System.Array.IndexOf(main, junction);
            if (index < 0)
            {
                return side;
            }

            var result = new List<Vector2Int>(side);
            for (int i = index + 1; i < main.Length; i++)
            {
                result.Add(main[i]);
            }

            return result.ToArray();
        }

        private const int MineCount = 3;
        private const int MineMinRoadDistance = 2;
        private const int MineMinSpacing = 5;

        // Resource nodes for the collector drones: a few free cells a little off the road, spread apart. The same seed
        // always gives the same cells.
        private static Vector2Int[] PickMineCells(Vector2Int size, HashSet<Vector2Int> road, TerrainPatch[] terrain, Vector2Int[] blocked, Vector2Int[] blockades, int seed)
        {
            var taken = new HashSet<Vector2Int>(terrain.Select(p => p.Cell));
            taken.UnionWith(blocked);
            var candidates = new List<Vector2Int>();
            for (int x = 2; x < size.x - 2; x++)
            {
                for (int y = 2; y < size.y - 2; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!taken.Contains(cell) && !NearAny(road, cell, MineMinRoadDistance - 1) && NearAny(road, cell, MineMinRoadDistance + 2) && !NearAny(blockades, cell, 2))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            var random = new System.Random(seed);
            var chosen = new List<Vector2Int>();
            while (candidates.Count > 0 && chosen.Count < MineCount)
            {
                Vector2Int pick = candidates[random.Next(candidates.Count)];
                chosen.Add(pick);
                candidates.RemoveAll(c => Mathf.Abs(c.x - pick.x) < MineMinSpacing && Mathf.Abs(c.y - pick.y) < MineMinSpacing);
            }

            return chosen.ToArray();
        }

        private static bool NearAny(IEnumerable<Vector2Int> cells, Vector2Int cell, int distance) =>
            cells.Any(c => Mathf.Abs(c.x - cell.x) <= distance && Mathf.Abs(c.y - cell.y) <= distance);
    }
}
