using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Builds the static look of a level from its definition and visual theme: ground tiles, road pieces with
    /// direction arrows, rocks on blocked cells, the base, spawn markers, flight routes, scenery around the field and a
    /// starfield underneath. Pure view code; gameplay never reads these objects.
    /// </summary>
    public static class LevelVisualBuilder
    {
        private const float TileScale = 1f;
        private const float RoadHeight = 0.05f;
        private const float RoadInlay = 0.62f;
        private const float RoadInlayEmission = 0.5f;
        private const float GroundTone = 0.07f;
        private const int SceneryRing = 3;
        private const float SceneryChance = 0.14f;
        private const float OuterDim = 0.55f;
        private const float EdgeThickness = 0.08f;
        private const float EdgeEmission = 2f;
        private const float ArrowEmission = 2.2f;
        private const float ArrowHeight = 0.07f;
        private const float RouteDotSpacing = 0.9f;
        private const float RouteDashLength = 0.45f;
        private const float RouteHeight = 0.12f;
        private const float RouteEmission = 2.2f;
        private const float MarkerEmission = 1.1f;
        private const float BaseScale = 1.25f;
        private const int StarCount = 700;
        private const float StarDepth = -7f;
        private const float StarFieldMargin = 40f;
        private const float StarSize = 0.12f;
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color RouteColor = new Color(0.55f, 0.95f, 0.95f);
        private static readonly Color SpawnColor = new Color(1f, 0.4f, 0.25f);
        private static readonly Color StarColor = new Color(0.8f, 0.9f, 1f);

        public static void Build(LevelDefinition level, Transform parent)
        {
            VisualTheme theme = level.Theme;
            GridMap grid = level.Grid;
            var road = new HashSet<Vector2Int>(LevelValidator.RoadCells(level));
            var blocked = new HashSet<Vector2Int>(level.BlockedCells);

            BuildGround(level, theme, grid, road, blocked, parent);
            BuildRoadArrows(level, grid, parent);
            BuildMarkers(level, theme, grid, parent);
            BuildEdges(theme, parent, level.WorldSize);
            BuildScenery(level, theme, grid, parent);
            BuildStarfield(parent, level.WorldSize);
        }

        private static void BuildGround(LevelDefinition level, VisualTheme theme, GridMap grid, HashSet<Vector2Int> road, HashSet<Vector2Int> blocked, Transform parent)
        {
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    Vector3 position = grid.CellToWorld(cell);

                    if (road.Contains(cell))
                    {
                        PlaceRoadPiece(theme, position, parent);
                        continue;
                    }

                    float tone = 1f + ((x + y) % 2 == 0 ? GroundTone : -GroundTone);
                    PlaceGround(theme, position, theme.GroundColor * tone, parent);

                    if (blocked.Contains(cell) && theme.BlockedProps.Length > 0)
                    {
                        int hash = Hash(cell);
                        GameObject prop = theme.BlockedProps[hash % theme.BlockedProps.Length];
                        float yaw = hash % 360;
                        float scale = 0.85f + (hash % 5) * 0.08f;
                        ModelFit.Place(prop, parent, position, yaw, scale, Color.Lerp(Color.white, theme.GroundColor * 2.2f, 0.5f));
                    }
                }
            }
        }

        private static void PlaceGround(VisualTheme theme, Vector3 position, Color tint, Transform parent)
        {
            if (theme.GroundTile != null)
            {
                ModelFit.Place(theme.GroundTile, parent, position, 0f, TileScale, tint);
            }
            else
            {
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, tint, parent, position + Vector3.down * 0.05f, new Vector3(TileScale, 0.1f, TileScale));
            }
        }

        // A raised slab with a lighter inlay: clear against the ground and the same for every orientation.
        private static void PlaceRoadPiece(VisualTheme theme, Vector3 position, Transform parent)
        {
            PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cube, theme.PathColor * 0.55f, parent, position + Vector3.up * RoadHeight * 0.5f, new Vector3(TileScale, RoadHeight, TileScale));
            PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cube, theme.PathColor, parent, position + Vector3.up * (RoadHeight + 0.005f), new Vector3(RoadInlay, 0.012f, RoadInlay), RoadInlayEmission);
        }
        // Yellow chevrons on every other road cell show which way enemies walk.
        private static void BuildRoadArrows(LevelDefinition level, GridMap grid, Transform parent)
        {
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying || path.Waypoints == null)
                {
                    continue;
                }

                var cells = new List<Vector2Int>(LevelValidator.LandPathCells(path.Waypoints));
                for (int i = 1; i < cells.Count - 1; i += 2)
                {
                    Vector2Int step = cells[i + 1] - cells[i - 1];
                    Vector3 forward = new Vector3(step.x, 0f, step.y).normalized;
                    if (forward == Vector3.zero)
                    {
                        continue;
                    }

                    PlaceChevron(grid.CellToWorld(cells[i]), forward, parent);
                }
            }
        }

        private static void PlaceChevron(Vector3 center, Vector3 forward, Transform parent)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 offset = (-forward * 0.12f) + (right * side * 0.14f);
                GameObject arm = PlaceholderVisuals.CreatePrimitive(
                    PrimitiveType.Cube, ArrowColor, parent, center + offset + Vector3.up * ArrowHeight, new Vector3(0.07f, 0.03f, 0.28f), ArrowEmission);
                arm.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, -side * 40f, 0f);
            }
        }

        // Where enemies come from (red rings), the flight routes (dashes) and the base.
        private static void BuildMarkers(LevelDefinition level, VisualTheme theme, GridMap grid, Transform parent)
        {
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Waypoints == null || path.Waypoints.Length < 2)
                {
                    continue;
                }

                Vector3 start = grid.CellToWorld(path.Waypoints[0]);
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, SpawnColor, parent, start + Vector3.up * 0.06f, new Vector3(0.75f, 0.02f, 0.75f), MarkerEmission);
                if (theme.SpawnModel != null)
                {
                    ModelFit.Place(theme.SpawnModel, parent, start, 0f, 1.1f, SpawnColor, 0.5f);
                }

                if (path.Flying)
                {
                    BuildFlightRoute(grid, path, parent);
                }
            }

            Vector2Int[] road = level.MainRoad;
            if (road.Length > 0)
            {
                Vector3 baseCenter = grid.CellToWorld(road[road.Length - 1]);
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, theme.BaseColor, parent, baseCenter + Vector3.up * 0.06f, new Vector3(1.5f, 0.02f, 1.5f), MarkerEmission);
                if (theme.BaseModel != null)
                {
                    ModelFit.Place(theme.BaseModel, parent, baseCenter, 0f, BaseScale, Color.Lerp(Color.white, theme.BaseColor, 0.4f), 0.3f);
                }
                else
                {
                    PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, theme.BaseColor, parent, baseCenter + Vector3.up * 0.6f, new Vector3(1.2f, 0.6f, 1.2f), 2.5f);
                }
            }
        }

        // A dashed line with a small arrow head at the end of each segment shows where flyers travel.
        private static void BuildFlightRoute(GridMap grid, EnemyPath path, Transform parent)
        {
            for (int i = 1; i < path.Waypoints.Length; i++)
            {
                Vector3 from = grid.CellToWorld(path.Waypoints[i - 1]);
                Vector3 to = grid.CellToWorld(path.Waypoints[i]);
                Vector3 direction = (to - from).normalized;
                float length = Vector3.Distance(from, to);

                for (float d = RouteDashLength; d < length - 0.5f; d += RouteDotSpacing)
                {
                    Vector3 center = from + direction * d;
                    GameObject dash = PlaceholderVisuals.CreatePrimitive(
                        PrimitiveType.Cube, RouteColor, parent, new Vector3(center.x, RouteHeight, center.z), new Vector3(0.08f, 0.03f, RouteDashLength), RouteEmission);
                    dash.transform.rotation = Quaternion.LookRotation(direction);
                }

                Vector3 head = to - direction * 0.6f;
                PlaceChevron(new Vector3(head.x, RouteHeight - ArrowHeight, head.z), direction, parent);
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

        // Dim tiles and scattered rocks around the field so it sits in a landscape instead of floating.
        private static void BuildScenery(LevelDefinition level, VisualTheme theme, GridMap grid, Transform parent)
        {
            int width = level.GridSize.x;
            int height = level.GridSize.y;
            for (int x = -SceneryRing; x < width + SceneryRing; x++)
            {
                for (int y = -SceneryRing; y < height + SceneryRing; y++)
                {
                    bool inside = x >= 0 && y >= 0 && x < width && y < height;
                    if (inside)
                    {
                        continue;
                    }

                    var cell = new Vector2Int(x, y);
                    Vector3 position = grid.CellToWorld(cell);
                    PlaceGround(theme, position, theme.GroundColor * OuterDim, parent);

                    int hash = Hash(cell);
                    if (theme.Scenery.Length > 0 && (hash % 100) / 100f < SceneryChance)
                    {
                        GameObject prop = theme.Scenery[hash % theme.Scenery.Length];
                        ModelFit.Place(prop, parent, position, hash % 360, 0.9f + (hash % 4) * 0.12f, Color.Lerp(Color.white, theme.GroundColor * 2f, 0.6f));
                    }
                }
            }
        }

        // A static particle field far below gives the "floating in space" backdrop.
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

        // A stable pseudo-random number per cell so the scenery looks the same every time.
        private static int Hash(Vector2Int cell)
        {
            unchecked
            {
                int h = cell.x * 73856093 ^ cell.y * 19349663;
                return h & 0x7fffffff;
            }
        }
    }
}

