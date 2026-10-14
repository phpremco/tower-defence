using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Builds the static look of a level from its definition: a floating slab with glowing edges and path,
    /// slot pads, the base and a starfield underneath. Pure view code; gameplay never reads these objects.
    /// </summary>
    public static class LevelVisualBuilder
    {
        private const float GroundThickness = 0.3f;
        private const float TileHeight = 0.04f;
        private const float TileFill = 0.94f;
        private const float EdgeThickness = 0.08f;
        private const float EdgeEmission = 2.2f;
        private const float PathEmission = 0.5f;
        private const float BlockHeight = 0.5f;
        private const float BlockedEmission = 0.4f;
        private const float BaseSize = 1.2f;
        private const float BaseEmission = 2.5f;
        private const float RouteDotSpacing = 1.2f;
        private const float RouteDotSize = 0.12f;
        private const float RouteHeight = 0.1f;
        private const float RouteEmission = 2f;
        private static readonly Color RouteColor = new Color(0.6f, 0.9f, 0.4f);
        private const int StarCount = 700;
        private const float StarDepth = -7f;
        private const float StarFieldMargin = 40f;
        private const float StarSize = 0.12f;
        private static readonly Color StarColor = new Color(0.8f, 0.9f, 1f);

        public static void Build(LevelDefinition level, Transform parent)
        {
            VisualTheme theme = level.Theme;
            Vector2 size = level.WorldSize;
            GridMap grid = level.Grid;

            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.GroundColor, parent, new Vector3(0f, -GroundThickness * 0.5f, 0f), new Vector3(size.x, GroundThickness, size.y));
            BuildEdges(theme, parent, size);

            float tile = level.CellSize * TileFill;
            foreach (Vector2Int cell in LevelValidator.RoadCells(level))
            {
                PlaceholderVisuals.CreatePrimitive(
                    PrimitiveType.Cube, theme.PathColor, parent, grid.CellToWorld(cell) + Vector3.up * TileHeight * 0.5f, new Vector3(tile, TileHeight, tile), PathEmission);
            }

            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying && path.Waypoints != null)
                {
                    BuildFlightRoute(level, path, parent);
                }
            }

            var road = new System.Collections.Generic.HashSet<Vector2Int>(LevelValidator.RoadCells(level));
            var blocked = new System.Collections.Generic.HashSet<Vector2Int>(level.BlockedCells);
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (road.Contains(cell))
                    {
                        continue;
                    }

                    Vector3 position = grid.CellToWorld(cell);
                    if (blocked.Contains(cell))
                    {
                        float height = level.CellSize * BlockHeight;
                        PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.GroundColor * 0.5f, parent, position + Vector3.up * height * 0.5f, new Vector3(tile, height, tile));
                        PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.SlotColor, parent, position + Vector3.up * (height + TileHeight), new Vector3(tile * 0.6f, TileHeight, tile * 0.6f), BlockedEmission);
                    }
                    else
                    {
                        PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, theme.GroundColor * 1.25f, parent, position + Vector3.up * TileHeight * 0.5f, new Vector3(tile, TileHeight, tile));
                    }
                }
            }
            Vector2Int[] mainRoad = level.MainRoad;
            if (mainRoad.Length > 0)
            {
                Vector2Int end = mainRoad[mainRoad.Length - 1];
                float baseSize = level.CellSize * BaseSize;
                PlaceholderVisuals.CreatePrimitive(
                    PrimitiveType.Cylinder, theme.BaseColor, parent, grid.CellToWorld(end) + Vector3.up * baseSize * 0.5f, new Vector3(baseSize, baseSize * 0.5f, baseSize), BaseEmission);
            }

            BuildStarfield(parent, size);
        }

        // Dots along a flying route so the player can see where flyers will come from.
        private static void BuildFlightRoute(LevelDefinition level, EnemyPath path, Transform parent)
        {
            GridMap grid = level.Grid;
            for (int i = 1; i < path.Waypoints.Length; i++)
            {
                Vector3 from = grid.CellToWorld(path.Waypoints[i - 1]);
                Vector3 to = grid.CellToWorld(path.Waypoints[i]);
                int dots = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / RouteDotSpacing));
                for (int d = 0; d <= dots; d++)
                {
                    Vector3 position = Vector3.Lerp(from, to, d / (float)dots);
                    PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, RouteColor, parent, new Vector3(position.x, RouteHeight, position.z), Vector3.one * RouteDotSize, RouteEmission);
                }
            }
        }

        private static void BuildEdges(VisualTheme theme, Transform parent, Vector2 size)
        {
            Color color = theme.SlotColor;
            float y = EdgeThickness * 0.5f;
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, parent, new Vector3(0f, y, size.y * 0.5f), new Vector3(size.x, EdgeThickness, EdgeThickness), EdgeEmission);
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, parent, new Vector3(0f, y, -size.y * 0.5f), new Vector3(size.x, EdgeThickness, EdgeThickness), EdgeEmission);
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, parent, new Vector3(size.x * 0.5f, y, 0f), new Vector3(EdgeThickness, EdgeThickness, size.y), EdgeEmission);
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, parent, new Vector3(-size.x * 0.5f, y, 0f), new Vector3(EdgeThickness, EdgeThickness, size.y), EdgeEmission);
        }

        // A static particle field far below the slab gives the "floating in space" backdrop.
        private static void BuildStarfield(Transform parent, Vector2 size)
        {
            var go = new GameObject("Starfield");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, StarDepth, 0f);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = float.MaxValue;
            main.startSpeed = 0f;
            main.startSize = StarSize;
            main.startColor = StarColor;
            main.maxParticles = StarCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x + StarFieldMargin, 0.01f, size.y + StarFieldMargin);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            system.Emit(StarCount);
            system.Play();
        }
    }
}




