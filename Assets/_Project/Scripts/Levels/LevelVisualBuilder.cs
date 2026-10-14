using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Builds the static look of a level from its definition and visual theme: the landscape (ground, road, liquids,
    /// decoration, drifting motes), direction arrows, the base, spawn markers, flight routes and a starfield
    /// underneath. Pure view code; gameplay never reads these objects.
    /// </summary>
    public static class LevelVisualBuilder
    {
        private const float ArrowEmission = 2.2f;
        private const float ArrowHeight = 0.07f;
        private const float RouteDotSpacing = 0.9f;
        private const float RouteDashLength = 0.45f;
        private const float RouteHeight = 0.12f;
        private const float RouteEmission = 1.4f;
        private const float RouteAlpha = 0.45f;
        private const float BaseScale = 1.35f;
        private const float GateScale = 1.2f;
        private const float BaseRingSpin = 45f;
        private const float GateRingSpin = -30f;
        private const int StarCount = 700;
        private const float StarDepth = -7f;
        private const float StarFieldMargin = 40f;
        private const float StarSize = 0.12f;
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color RouteColor = new Color(0.55f, 0.95f, 0.95f);
        private static readonly Color MineColor = new Color(1f, 0.8f, 0.25f);
        private static readonly Color SpawnColor = new Color(1f, 0.4f, 0.25f);
        private static readonly Color StarColor = new Color(0.8f, 0.9f, 1f);

        /// <summary>Builds the level and returns the flight route objects by path index, hidden until a wave uses them.</summary>
        public static Dictionary<int, GameObject> Build(LevelDefinition level, Transform parent)
        {
            VisualTheme theme = level.Theme;
            GridMap grid = level.Grid;

            BuildLandscape(level, theme, parent);
            BuildRoadArrows(level, grid, parent);
            Dictionary<int, GameObject> routes = BuildMarkers(level, theme, grid, parent);
            BuildMiningNodes(level, grid, parent);
            BuildStarfield(parent, level.WorldSize);
            return routes;
        }

        private static void BuildLandscape(LevelDefinition level, VisualTheme theme, Transform parent)
        {
            var road = new RoadShape(level);
            var field = new TerrainField(level, road);
            TerrainSurfaceBuilder.Build(level, theme, field, road, parent);
            TerrainLiquidBuilder.Build(level, theme, parent);
            PropScatterBuilder.Build(level, theme, field, parent);
            AmbientParticlesBuilder.Build(level, theme, parent);
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

        private static void PlaceChevron(Vector3 center, Vector3 forward, Transform parent, bool transparent = false)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 offset = (-forward * 0.12f) + (right * side * 0.14f);
                GameObject arm = PlaceholderVisuals.CreatePrimitive(
                    PrimitiveType.Cube, ArrowColor, parent, center + offset + Vector3.up * ArrowHeight, new Vector3(0.07f, 0.03f, 0.28f), ArrowEmission);
                arm.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, -side * 40f, 0f);
                if (transparent)
                {
                    arm.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(RouteColor, RouteAlpha, RouteEmission);
                }
            }
        }

        // Where enemies come from (red rings), the flight routes (dashes) and the base.
        private static Dictionary<int, GameObject> BuildMarkers(LevelDefinition level, VisualTheme theme, GridMap grid, Transform parent)
        {
            var routes = new Dictionary<int, GameObject>();
            for (int index = 0; index < level.Paths.Length; index++)
            {
                EnemyPath path = level.Paths[index];
                if (path.Waypoints == null || path.Waypoints.Length < 2)
                {
                    continue;
                }

                Vector3 start = grid.CellToWorld(path.Waypoints[0]);
                PlaceStructure(StructureModelFactory.EnemyGate(out Mesh gateRing), gateRing, SpawnColor, start, GateScale, GateRingSpin, parent);

                if (path.Flying)
                {
                    var routeRoot = new GameObject($"FlightRoute_{index}");
                    routeRoot.transform.SetParent(parent, false);
                    BuildFlightRoute(grid, path, routeRoot.transform);
                    routeRoot.SetActive(false);
                    routes[index] = routeRoot;
                }
            }

            Vector2Int[] road = level.MainRoad;
            if (road.Length > 0)
            {
                Vector3 baseCenter = grid.CellToWorld(road[road.Length - 1]);
                PlaceStructure(StructureModelFactory.BaseReactor(out Mesh reactorRing), reactorRing, theme.BaseColor, baseCenter, BaseScale, BaseRingSpin, parent);
            }

            return routes;
        }

        // Golden crystal clusters that collector drones mine; they never move or run out.
        private static void BuildMiningNodes(LevelDefinition level, GridMap grid, Transform parent)
        {
            if (level.MineCells.Length == 0)
            {
                return;
            }

            Mesh mesh = StructureModelFactory.MiningNode();
            Material[] materials = ModelPalette.Create(MineColor);
            foreach (Vector2Int cell in level.MineCells)
            {
                var go = new GameObject("MiningNode");
                go.transform.SetParent(parent, false);
                go.transform.position = grid.CellToWorld(cell) + Vector3.up * level.Terrain.HeightAt(cell);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }
        }

        // A structure mesh with a separate ring that turns slowly, in the given glow colour.
        private static void PlaceStructure(Mesh mesh, Mesh ring, Color glow, Vector3 position, float scale, float ringSpin, Transform parent)
        {
            Material[] materials = ModelPalette.Create(glow);
            var root = new GameObject("Structure").transform;
            root.SetParent(parent, false);
            root.position = position;
            root.localScale = Vector3.one * scale;

            var body = new GameObject("Body");
            body.transform.SetParent(root, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            body.AddComponent<MeshRenderer>().sharedMaterials = materials;

            var spinning = new GameObject("Ring");
            spinning.transform.SetParent(root, false);
            spinning.AddComponent<MeshFilter>().sharedMesh = ring;
            spinning.AddComponent<MeshRenderer>().sharedMaterials = materials;
            spinning.AddComponent<Spinner>().Initialize(ringSpin);
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
                    dash.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(RouteColor, RouteAlpha, RouteEmission);
                }

                Vector3 head = to - direction * 0.6f;
                PlaceChevron(new Vector3(head.x, RouteHeight - ArrowHeight, head.z), direction, parent, transparent: true);
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
    }
}
