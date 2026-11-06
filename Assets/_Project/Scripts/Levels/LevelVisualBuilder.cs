using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
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
        // Road and flight light trails (README round 25b): strip width per cell, cells per streak tile, colour (alpha = strength).
        private const float RoadTrailWidth = 0.42f;
        private const float RoadTrailCells = 3f;
        private const float TrailStep = 0.5f;
        private const float FlightTrailWidth = 0.16f;
        private const float FlightTrailCells = 2f;
        private static readonly Color RoadTrailColour = new Color(1f, 0.85f, 0.45f, 0.6f);
        private static readonly Color FlightTrailColour = new Color(0.55f, 0.95f, 0.95f, 0.7f);
        private const float RouteEmission = 1.4f;
        private const float RouteAlpha = 0.45f;
        private const float BaseScale = 1.2f;
        private const float GateScale = 1.2f;

        // The gate of a flying route hangs in the air at flight height and a little smaller, so it reads as a sky
        // portal and never sits on top of the road gate beside it.
        private const float AirGateHeight = 1.6f;
        private const float AirGateScale = 0.8f;
        private const float BaseRingSpin = 24f;
        private const float BaseDishSpin = 40f;
        private const float BaseTowerLightIntensity = 1.2f;
        private const float BaseTowerLightRange = 3f;
        private const float BaseTowerLightHeight = 2.4f;
        private const float GateRingSpin = -30f;
        private const int StarCount = 700;
        private const float StarDepth = -7f;
        private const float StarFieldMargin = 40f;
        private const float StarSize = 0.12f;
        private const float BaseSetBackCells = 0.8f;
        private const float BaseLightIntensity = 1.5f;
        private const float BaseLightRange = 4f;
        private const float BaseLightHeight = 1.2f;
        private const float BasePoolRadius = 1.2f;
        private const float BasePoolBrightness = 0.18f;
        private const float BasePoolHeight = 0.05f;
        private const string BaseCoreLight = "Light_Core";
        private const string BaseCommandLight = "Light_Command";
        private const float ResourceNodeTurn = 137f;
        private const string GateLightSocket = "Socket_Light";
        private const string GateSporeSocket = "Socket_Spores";
        private const float GateLightIntensity = 1.6f;
        private const float GateLightRange = 3f;
        private const float GateLightHeight = 0.6f;
        private const float StructureFlicker = 0.12f;
        private const float StructureFlickerSpeed = 1.2f;
        private const float StructurePulseDepth = 0.25f;
        private const float StructurePulseSpeed = 1.4f;
        private const float SporeRate = 8f;
        private const float NestScale = 0.9f;
        private const float NestRingHeight = 0.18f;
        private const float NestRingSpin = 18f;
        private const float NestLightIntensity = 1.1f;
        private const float NestLightRange = 2.6f;
        private const float NestLightHeight = 0.5f;
        private const float NestPoolRadius = 0.7f;
        private const float NestPoolBrightness = 0.35f;
        private const float NestPoolHeight = 0.06f;
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color RouteColor = new Color(0.55f, 0.95f, 0.95f);
        private static readonly Color MineColor = new Color(1f, 0.8f, 0.25f);
        private const float MineCrackGlow = 0.6f;
        private static readonly Color StarColor = new Color(0.8f, 0.9f, 1f);
        private static readonly int WindId = Shader.PropertyToID("_EnvWind");
        private static readonly int WindParamsId = Shader.PropertyToID("_EnvWindParams");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>Builds the level and returns the flight route objects by path index, hidden until a wave uses them.</summary>
        /// <param name="particleScale">The quality profile's share of particles, one for the full amount.</param>
        /// <param name="groundMist">Whether the full mist is built: the theme's mist definition then draws every sheet, else only its
        /// Low sheets; without a definition the painted sheets are built only when this is set.</param>
        /// <param name="dressingOnly">Only the landscape, mist and stars: no road arrows, gates, base, flight routes, mining nodes or nests. For cutscenes that stage the field themselves.</param>
        public static Dictionary<int, GameObject> Build(LevelDefinition level, Transform parent, float particleScale = 1f, bool groundMist = true, bool dressingOnly = false)
        {
            VisualTheme theme = level.Theme;
            GridMap grid = level.Grid;

            // The imported environment (kit, decor, burrows) is drawn instanced; a theme without a set builds none.
            EnvironmentInstancer instancer = theme.Environment != null ? EnvironmentInstancer.Create(parent) : null;
            TerrainField field = BuildLandscape(level, theme, parent, particleScale, instancer);
            if (theme.Mist != null)
            {
                GroundMistBuilder.Build(level, theme.Mist, parent, !groundMist || ModelQuality.IsLow);
            }
            else if (groundMist)
            {
                GroundMistBuilder.Build(level, theme, parent);
            }

            if (dressingOnly)
            {
                instancer?.Finish();
                BuildStarfield(parent, level.WorldSize);
                return new Dictionary<int, GameObject>();
            }

            BuildRoadArrows(level, field, parent);
            Dictionary<int, GameObject> routes = BuildMarkers(level, theme, grid, parent);
            BuildMiningNodes(level, grid, parent);
            BuildNests(level, grid, parent, instancer);
            BuildColonyRig(level, theme, field, parent);
            if (instancer != null)
            {
                instancer.Finish();
                instancer.UseRemains(theme.Environment, ModelQuality.IsLow);
                instancer.gameObject.AddComponent<BuildPadHighlight>().Initialize(level);
            }

            BuildStarfield(parent, level.WorldSize);
            return routes;
        }

        private static TerrainField BuildLandscape(LevelDefinition level, VisualTheme theme, Transform parent, float particleScale, EnvironmentInstancer instancer)
        {
            var road = new RoadShape(level);
            var field = new TerrainField(level, road);
            TerrainSurfaceBuilder.Build(level, theme, field, road, parent);
            TerrainLiquidBuilder.Build(level, theme, parent, field, particleScale);
            PropScatterBuilder.Build(level, theme, field, parent, instancer);
            if (instancer != null)
            {
                EnvironmentSet set = theme.Environment;
                SetWind(set);
                EnvironmentKitBuilder.Build(level, set, field, instancer, ModelQuality.IsLow);
            }

            AmbientParticlesBuilder.Build(level, theme, parent, particleScale);
            return field;
        }

        // The flora sway in the environment shader reads the set's wind from shader globals.
        public static void SetWind(EnvironmentSet set)
        {
            Vector3 direction = set.WindDirection;
            Shader.SetGlobalVector(WindId, new Vector4(direction.x, direction.y, direction.z, set.SwayAmplitude));
            Shader.SetGlobalVector(WindParamsId, new Vector4(set.SwaySpeed, set.Flutter, 0f, 0f));
        }

        // Yellow chevrons on every other road cell show which way enemies walk.
        private static void BuildRoadArrows(LevelDefinition level, TerrainField field, Transform parent)
        {
            MarkerArt art = MarkerArt.Active;
            if (art != null && art.Has(MarkerKind.RoadCue))
            {
                BuildRoadTrails(level, field, art, parent);
                return;
            }

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

                    PlaceChevron(level.Road.CentreOf(cells[i]), forward, parent);
                }
            }
        }

        // README round 25b: soft light streaks that run along each road towards the base (one every three cells), on a
        // ribbon that follows the corridor's centre line through its bends; they fade in at the gate and out at the base.
        private static void BuildRoadTrails(LevelDefinition level, TerrainField field, MarkerArt art, Transform parent)
        {
            for (int i = 0; i < level.Paths.Length; i++)
            {
                EnemyPath path = level.Paths[i];
                if (path.Flying || path.Waypoints == null)
                {
                    continue;
                }

                List<Vector3> points = OnGround(level.Road.Route(i), field);

                RouteTrail.Build(art, MarkerKind.RoadCue, $"RoadTrail_{i}", parent, points, RoadTrailWidth * level.CellSize, 1f / (RoadTrailCells * level.CellSize), RoadTrailColour);
            }
        }

        // The route cut into short steps that follow the ground (pools dig basins beside the road; banks rise), just over it.
        private static List<Vector3> OnGround(Vector3[] route, TerrainField field)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < route.Length; i++)
            {
                int steps = i == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(route[i - 1], route[i]) / TrailStep));
                for (int s = i == 0 ? 0 : 1; s <= (i == 0 ? 0 : steps); s++)
                {
                    Vector3 p = i == 0 ? route[0] : Vector3.Lerp(route[i - 1], route[i], s / (float)steps);
                    p.y = field.HeightAt(p.x, p.z) + ArrowHeight;
                    points.Add(p);
                }
            }

            return points;
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

                SpawnOriginDefinition spawnArt = level.SpawnOrigins;
                ModelArtDefinition gateArt = spawnArt == null ? null : path.Flying ? spawnArt.AirGate : spawnArt.GroundGate;
                if (gateArt != null)
                {
                    BuildGateArt(level, spawnArt, gateArt, index, theme.SpawnColor, parent);
                }
                else
                {
                    // On a road the gate stands on the centre line of the corridor, not on its first row.
                    Vector3 start = level.Road.Route(index)[0] + (path.Flying ? Vector3.up * AirGateHeight : Vector3.zero);
                    float gateScale = path.Flying ? GateScale * AirGateScale : GateScale;
                    Transform gate = PlaceStructure(StructureModelFactory.EnemyGate(out Mesh gateRing), gateRing, Vector3.zero, EnemyModelFactory.CreateMaterials(theme.SpawnColor, CreatureSkin.Kind.Flesh), EnemyModelFactory.Glow, start, Quaternion.identity, gateScale, GateRingSpin, parent);
                    AddLight(gate, theme.SpawnColor, GateLightIntensity, GateLightRange, GateLightHeight);
                    AddSpores(gate, theme.SpawnColor);
                }

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
                // The outpost stands a little past the end of the road, door towards it, so the hero post stays clear.
                Vector2Int baseCell = road[road.Length - 1];
                Vector3 along = level.Road.DirectionAt(baseCell);
                Vector3 baseCenter = BasePosition(level);
                Quaternion facing = along.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(-along) : Quaternion.identity;
                if (theme.BaseArt != null)
                {
                    BuildBaseArt(theme, baseCenter, facing, parent);
                    return routes;
                }

                StructureModelFactory.BaseParts baseParts = StructureModelFactory.BaseOutpost();
                Material[] baseMaterials = ModelPalette.Create(theme.BaseColor);
                Transform baseRoot = PlaceStructure(baseParts.Body, baseParts.Ring, baseParts.RingPivot, baseMaterials, ModelPalette.Glow, baseCenter, facing, BaseScale, BaseRingSpin, parent);
                AddSpinner(baseRoot, "Dish", baseParts.Dish, baseParts.DishPivot, baseMaterials, BaseDishSpin);
                AddLight(baseRoot, theme.BaseColor, BaseLightIntensity, BaseLightRange, BaseLightHeight);
                AddLight(baseRoot, theme.BaseColor, BaseTowerLightIntensity, BaseTowerLightRange, BaseTowerLightHeight);
                GlowDecal.PlacePool(baseRoot, theme.BaseColor * BasePoolBrightness, BasePoolRadius, BasePoolHeight);
                baseRoot.gameObject.AddComponent<BaseView>();
            }

            return routes;
        }

        // The living route gate (README round 25b) at the start of a path: a mound on the road, a pod at flight height, with
        // the gate light and spores on its sockets. It plays its Spawn clip as creatures come through (SpawnGateView).
        private static void BuildGateArt(LevelDefinition level, SpawnOriginDefinition spawnArt, ModelArtDefinition art, int index, Color colour, Transform parent)
        {
            bool flying = level.Paths[index].Flying;
            Vector3 start = level.Road.Route(index)[0] + (flying ? Vector3.up * spawnArt.AirGateHeight : Vector3.zero);
            var root = new GameObject($"SpawnGate_{index}").transform;
            root.SetParent(parent, false);
            root.position = start;
            root.localScale = Vector3.one * (spawnArt.GateScale * (flying ? spawnArt.AirGateScale : 1f));
            var view = root.gameObject.AddComponent<SpawnGateView>();
            view.Initialize(art, index, spawnArt.GateGlow);
            Transform light = AddLight(root, colour, GateLightIntensity, GateLightRange, 0f);
            light.position = view.Socket(GateLightSocket).position;
            Transform spores = AddSpores(root, colour).transform;
            spores.position = view.Socket(GateSporeSocket).position;
        }

        // The imported base: built at its final size (no BaseScale), lit from its own light sockets.
        private static void BuildBaseArt(VisualTheme theme, Vector3 position, Quaternion facing, Transform parent)
        {
            var root = new GameObject("Structure").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, facing);
            var view = root.gameObject.AddComponent<BaseArtView>();
            view.Initialize(theme.BaseArt);
            AddLight(view.Socket(BaseCoreLight) ?? root, theme.BaseColor, BaseLightIntensity, BaseLightRange, 0f);
            AddLight(view.Socket(BaseCommandLight) ?? root, theme.BaseColor, BaseTowerLightIntensity, BaseTowerLightRange, 0f);
            GlowDecal.PlacePool(root, theme.BaseColor * BasePoolBrightness, BasePoolRadius, BasePoolHeight);
            root.gameObject.AddComponent<BaseView>();
        }

        /// <summary>Where the base stands: a little past the end of the main road, so the hero post stays clear.</summary>
        public static Vector3 BasePosition(LevelDefinition level)
        {
            Vector2Int[] road = level.MainRoad;
            Vector2Int baseCell = road[road.Length - 1];
            return level.Road.CentreOf(baseCell) + level.Road.DirectionAt(baseCell) * (BaseSetBackCells * level.CellSize);
        }

        // A second turning part on a structure, around its own pivot.
        /// <summary>
        /// The outpost as it stands in a level, without lights or the ground pool, for menus that show the base.
        /// Standing on y = 0, facing +z, in level scale; the ring and the dish turn like on the field.
        /// </summary>
        public static GameObject BuildBaseDisplay(Transform parent, Color glow) => BuildBaseDisplay(parent, glow, null);

        /// <summary>The base for a menu: the imported model when there is one (its dish turning), else the procedural outpost.</summary>
        public static GameObject BuildBaseDisplay(Transform parent, Color glow, BaseArtDefinition art)
        {
            if (art != null)
            {
                return BaseArtView.BuildDisplay(parent, art);
            }

            StructureModelFactory.BaseParts parts = StructureModelFactory.BaseOutpost();
            Material[] materials = ModelPalette.Create(glow);
            Transform root = PlaceStructure(parts.Body, parts.Ring, parts.RingPivot, materials, ModelPalette.Glow, Vector3.zero, Quaternion.identity, BaseScale, BaseRingSpin, parent);
            root.localPosition = Vector3.zero;
            AddSpinner(root, "Dish", parts.Dish, parts.DishPivot, materials, BaseDishSpin);
            return root.gameObject;
        }

        private static void AddSpinner(Transform root, string name, Mesh mesh, Vector3 pivot, Material[] materials, float degreesPerSecond)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pivot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            go.AddComponent<Spinner>().Initialize(degreesPerSecond);
        }

        private static Transform AddLight(Transform root, Color colour, float intensity, float range, float height)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>().Initialize(intensity, StructureFlicker, StructureFlickerSpeed);
            return go.transform;
        }

        // Spores drift up out of the hive mouth, so the entrance reads as alive even between waves.
        private static ParticleSystem AddSpores(Transform root, Color colour)
        {
            var go = new GameObject("Spores");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            Color glow = colour * 1.8f;
            glow.a = 1f;
            main.startColor = glow;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = SporeRate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.2f;
            noise.frequency = 0.5f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ParticleKit.UseCell(system, MarkerArt.ParticleCell.Spore);
            system.Play();
            return system;
        }

        // Golden crystal clusters that harvest drones mine; each turns dull once its ore is gone.
        // The colony's drilling rig beside the field, where the structure clearance found it a spot (decor already keeps
        // that ground free). Purely a view: nothing in the gameplay knows of it.
        private static void BuildColonyRig(LevelDefinition level, VisualTheme theme, TerrainField field, Transform parent)
        {
            ColonyRigDefinition rig = theme.Environment != null ? theme.Environment.ColonyRig : null;
            if (rig == null || rig.Drill == null)
            {
                return;
            }

            var clearance = new StructureClearance(level);
            if (!clearance.HasColonyRig)
            {
                return;
            }

            Vector3 at = clearance.ColonyRigPosition;
            at.y = field.HeightAt(at.x, at.z);
            var root = new GameObject("ColonyRig").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(at, clearance.ColonyRigFacing);
            root.gameObject.AddComponent<ColonyRigView>().Initialize(rig);
        }

        private static void BuildMiningNodes(LevelDefinition level, GridMap grid, Transform parent)
        {
            if (level.MineCells.Length == 0)
            {
                return;
            }

            if (level.Theme.ResourceNodeArt != null)
            {
                BuildResourceNodes(level, grid, parent);
                return;
            }

            Mesh mesh = StructureModelFactory.MiningNode();
            // The boulder wears the level's rock with ore glowing in its cracks; the crystals carry streaks of light.
            Material[] materials = ModelPalette.Create(MineColor);
            materials[ModelPalette.Dark].SetColor("_BaseColor", level.Theme.RockColor * 0.8f);
            CreatureSkin.Dress(materials[ModelPalette.Dark], CreatureSkin.Kind.Stone, MineColor * MineCrackGlow);
            PropFactory.Dress(materials[ModelPalette.Glow], PropTextures.Crystal());
            for (int i = 0; i < level.MineCells.Length; i++)
            {
                Vector2Int cell = level.MineCells[i];
                var go = new GameObject("MiningNode");
                go.transform.SetParent(parent, false);
                go.transform.position = grid.CellToWorld(cell) + Vector3.up * level.Terrain.HeightAt(cell);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = materials;
                go.AddComponent<MiningNodeView>().Initialize(i);
            }
        }

        // The imported ore veins, each turned its own way so neighbouring nodes do not look stamped.
        private static void BuildResourceNodes(LevelDefinition level, GridMap grid, Transform parent)
        {
            for (int i = 0; i < level.MineCells.Length; i++)
            {
                Vector2Int cell = level.MineCells[i];
                var go = new GameObject("MiningNode");
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(grid.CellToWorld(cell) + Vector3.up * level.Terrain.HeightAt(cell), Quaternion.Euler(0f, i * ResourceNodeTurn, 0f));
                go.AddComponent<MiningNodeView>().Initialize(i, level.Theme.ResourceNodeArt);
            }
        }

        // Element nests: a crater ringed with the element's colour that breathes and sheds motes, so the player sees
        // where the element creatures will rise. Liquid nests float on their pool, the rest sit on the ground.
        private static void BuildNests(LevelDefinition level, GridMap grid, Transform parent, EnvironmentInstancer instancer)
        {
            var tinted = new Dictionary<Element, Material>();
            for (int index = 0; index < level.Nests.Length; index++)
            {
                ElementNest nest = level.Nests[index];
                Color colour = NestColor(nest.Element);
                TerrainKind kind = level.Terrain.KindAt(nest.Cell);
                float height = TerrainMap.IsLiquid(kind) ? TerrainLiquidBuilder.SurfaceLevel(kind) : level.Terrain.HeightAt(nest.Cell);
                Vector3 position = grid.CellToWorld(nest.Cell) + Vector3.up * height;
                SpawnOriginArt living = level.SpawnOrigins != null ? level.SpawnOrigins.For(kind) : null;
                if (living != null)
                {
                    // A burrow in a rift sits on the ground over the fissure, its slabs covering the crack.
                    Vector3 at = kind == TerrainKind.Rift ? new Vector3(position.x, 0f, position.z) : position;
                    BuildLivingOrigin(index, nest, living, level.SpawnOrigins, at, colour, parent);
                    continue;
                }

                EnvironmentOrigin origin = instancer != null ? level.Theme.Environment.Origin(kind) : null;
                if (origin != null)
                {
                    BuildOrigin(nest, origin, position, colour, instancer, tinted, parent);
                    continue;
                }

                Material[] materials = EnemyModelFactory.CreateMaterials(colour, colour, 1f, NestSkin(nest.Element));
                Transform root = PlaceStructure(StructureModelFactory.ElementNest(nest.Element, out Mesh ring), ring, Vector3.up * NestRingHeight, materials, EnemyModelFactory.Glow, position, Quaternion.identity, NestScale, NestRingSpin, parent);
                root.name = $"Nest_{nest.Element}";
                AddLight(root, colour, NestLightIntensity, NestLightRange, NestLightHeight);
                AddSpores(root, colour);
                GlowDecal.PlacePool(root, colour * NestPoolBrightness, NestPoolRadius, NestPoolHeight);
            }
        }

        // The living opening (README round 23): marked, opening, open and closing as the waves use it (SpawnOriginView).
        // A burrow keeps its yaw so its camouflage lines up with the ground; a vent in a pool turns any way.
        private static void BuildLivingOrigin(int index, ElementNest nest, SpawnOriginArt art, SpawnOriginDefinition definition, Vector3 position, Color colour, Transform parent)
        {
            float yaw = art.TintWithElement ? TerrainNoise.Hash(nest.Cell.x, nest.Cell.y, 3) * 360f : 0f;
            var root = new GameObject($"Origin_{index}_{nest.Element}").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            ParticleSystem spores = AddSpores(root, colour);
            GameObject pool = GlowDecal.PlacePool(root, colour * NestPoolBrightness, NestPoolRadius, NestPoolHeight);
            root.gameObject.AddComponent<SpawnOriginView>().Initialize(index, art, definition, colour, spores, pool);
        }

        // The imported opening: a burrow in the ground or a vent on a pool (its white glow mask tinted with the element),
        // with the procedural nest's light, spores and glow pool, so a nest still reads as alive between waves.
        private static void BuildOrigin(ElementNest nest, EnvironmentOrigin origin, Vector3 position, Color colour, EnvironmentInstancer instancer, Dictionary<Element, Material> tinted, Transform parent)
        {
            bool low = ModelQuality.IsLow;
            Material material = null;
            if (origin.TintWithElement && !tinted.TryGetValue(nest.Element, out material))
            {
                material = instancer.Own(origin.Model.MaterialFor(low));
                material.SetColor(EmissionColorId, colour * material.GetColor(EmissionColorId).maxColorComponent);
                tinted[nest.Element] = material;
            }

            float yaw = TerrainNoise.Hash(nest.Cell.x, nest.Cell.y, 3) * 360f;
            instancer.Add(origin.Model, Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), Vector3.one * origin.Scale), low, material);
            var root = new GameObject($"Nest_{nest.Element}").transform;
            root.SetParent(parent, false);
            root.position = position;
            AddLight(root, colour, NestLightIntensity, NestLightRange, NestLightHeight);
            AddSpores(root, colour);
            GlowDecal.PlacePool(root, colour * NestPoolBrightness, NestPoolRadius, NestPoolHeight);
        }

        // The surface a nest is made of: a vent or burrow is cracked stone, a whirl in the water is slick, a stump is bark.
        private static CreatureSkin.Kind NestSkin(Element element)
        {
            switch (element)
            {
                case Element.Water:
                case Element.Bog:
                    return CreatureSkin.Kind.Slime;
                case Element.Wood:
                    return CreatureSkin.Kind.Bark;
                case Element.Fire:
                case Element.Sand:
                case Element.Stone:
                case Element.Ice:
                    return CreatureSkin.Kind.Stone;
                default:
                    return CreatureSkin.Kind.Chitin;
            }
        }

        /// <summary>The colour an element shows in: its nests, its bolts and its bats.</summary>
        public static Color NestColor(Element element)
        {
            switch (element)
            {
                case Element.Fire:
                    return new Color(1f, 0.45f, 0.12f);
                case Element.Water:
                    return new Color(0.3f, 0.75f, 1f);
                case Element.Wood:
                    return new Color(0.45f, 0.95f, 0.35f);
                case Element.Sand:
                    return new Color(0.95f, 0.8f, 0.4f);
                case Element.Ice:
                    return new Color(0.8f, 0.95f, 1f);
                case Element.Bog:
                    return new Color(0.55f, 0.8f, 0.3f);
                case Element.Stone:
                    // The amber seam of the rifts: warmer and paler than fire, so a stone nest never reads as a lava vent.
                    return new Color(1f, 0.62f, 0.28f);
                default:
                    return Color.white;
            }
        }

        // A structure mesh with a separate part that turns slowly around its own pivot; the glow material breathes.
        private static Transform PlaceStructure(Mesh mesh, Mesh ring, Vector3 ringPivot, Material[] materials, int glowSlot, Vector3 position, Quaternion rotation, float scale, float ringSpin, Transform parent)
        {
            var root = new GameObject("Structure").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, rotation);
            root.localScale = Vector3.one * scale;

            var body = new GameObject("Body");
            body.transform.SetParent(root, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            body.AddComponent<MeshRenderer>().sharedMaterials = materials;

            var spinning = new GameObject("Ring");
            spinning.transform.SetParent(root, false);
            spinning.transform.localPosition = ringPivot;
            spinning.AddComponent<MeshFilter>().sharedMesh = ring;
            spinning.AddComponent<MeshRenderer>().sharedMaterials = materials;
            spinning.AddComponent<Spinner>().Initialize(ringSpin);
            root.gameObject.AddComponent<EmissionPulse>().Initialize(new[] { materials[glowSlot] }, StructurePulseDepth, StructurePulseSpeed);
            return root;
        }

        // A dashed line with a small arrow head at the end of each segment shows where flyers travel.
        private static void BuildFlightRoute(GridMap grid, EnemyPath path, Transform parent)
        {
            MarkerArt art = MarkerArt.Active;
            if (art != null && art.Has(MarkerKind.FlightCue))
            {
                // README round 25b: beads of light along the flight line instead of dashes and arrow heads.
                var points = new Vector3[path.Waypoints.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 point = grid.CellToWorld(path.Waypoints[i]);
                    points[i] = new Vector3(point.x, RouteHeight, point.z);
                }

                RouteTrail.Build(art, MarkerKind.FlightCue, "FlightTrail", parent, points, FlightTrailWidth * grid.CellSize, 1f / (FlightTrailCells * grid.CellSize), FlightTrailColour);

                return;
            }

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

            ParticleKit.UseCell(system, MarkerArt.ParticleCell.Mote);
            system.Emit(StarCount);
            system.Play();
        }
    }
}
