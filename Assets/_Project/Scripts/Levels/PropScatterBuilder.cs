using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Places the level's decoration: formations on the blocked cells, a growing wilderness around the field, rubble
    /// along the lips of raised ground, rock around lava pools and a few small tufts between the build cells.
    /// Plants sway, glow breathes, the brightest formations light the ground and crystals shed sparks. Placement is
    /// deterministic, so a level looks the same every time.
    /// </summary>
    public static class PropScatterBuilder
    {
        private const float BlockedScaleMin = 0.95f;
        private const float BlockedScaleSpread = 0.45f;
        private const float OuterChanceNear = 0.55f;
        private const float OuterChanceFar = 0.4f;
        private const float OuterChanceDeep = 0.18f;
        private const float OuterScaleGrowth = 0.07f;
        private const float TuftChance = 0.2f;
        private const float TuftScale = 0.4f;
        private const float TuftCornerOffset = 0.36f;
        private const float SpireMinDistance = 3f;
        private const int SpireChancePercent = 9;
        private const float LiquidAvoid = 0.12f;
        private const float SinkIntoGround = 0.03f;
        private const float RubbleChance = 0.55f;
        private const float RubbleOffsetCells = 0.47f;
        private const float RubbleScaleMin = 0.28f;
        private const float RubbleScaleSpread = 0.18f;
        private const float LavaRimChance = 0.8f;
        private const float LavaRimOffsetCells = 0.62f;
        private const float LavaRimScaleMin = 0.5f;
        private const float LavaRimScaleSpread = 0.4f;
        private const int LavaSpikePercent = 30;
        private const float SwayDegrees = 2.5f;
        private const float SwaySpeedMin = 0.7f;
        private const float SwaySpeedSpread = 0.6f;
        private const float PulseDepth = 0.3f;
        private const float PulseSpeed = 0.9f;
        private const int MaxLights = 12;
        private const float LightHeight = 0.8f;
        private const float LightRange = 2.4f;
        private const float LightIntensity = 1.3f;
        private const float LightFlicker = 0.18f;
        private const float LightFlickerSpeed = 1.6f;
        private const float SparkRate = 3f;
        private const float SparkLifeMin = 1.2f;
        private const float SparkLifeMax = 2.4f;
        private const float SparkSizeMin = 0.025f;
        private const float SparkSizeMax = 0.06f;
        private const float SparkRise = 0.25f;
        private const float SparkRadius = 0.35f;
        private const float SparkBrightness = 1.8f;
        private const float PoolRadius = 0.55f;
        private const float PoolBrightness = 0.3f;

        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static void Build(LevelDefinition level, VisualTheme theme, TerrainField field, Transform parent)
        {
            PropKind[] kinds = theme.PropKinds;
            if (kinds == null || kinds.Length == 0)
            {
                return;
            }

            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);

            var placer = new Placer(theme, field, root);
            GridMap grid = level.Grid;
            TerrainMap terrain = level.Terrain;
            var road = new HashSet<Vector2Int>(level.Road.Cells);
            var blocked = new HashSet<Vector2Int>(level.BlockedCells);

            foreach (Vector2Int cell in blocked)
            {
                int hash = Hash(cell);
                Vector3 centre = grid.CellToWorld(cell);
                float scale = BlockedScaleMin + (hash % 100) / 100f * BlockedScaleSpread;
                placer.Place(kinds[hash % kinds.Length], hash, centre.x, centre.z, scale, formation: true);
            }

            foreach (KeyValuePair<Vector2Int, TerrainKind> patch in terrain.Cells)
            {
                if (patch.Value == TerrainKind.Lava)
                {
                    PlaceLavaRim(placer, patch.Key, grid, road, terrain);
                }
            }

            int width = level.GridSize.x;
            int height = level.GridSize.y;
            // The scatter reaches as far as the landscape does, less the last cell so nothing stands on its edge.
            int ringX = field.MarginCellsX - 1;
            int ringZ = field.MarginCellsZ - 1;
            for (int x = -ringX; x < width + ringX; x++)
            {
                for (int y = -ringZ; y < height + ringZ; y++)
                {
                    var cell = new Vector2Int(x, y);
                    bool inside = x >= 0 && y >= 0 && x < width && y < height;
                    int hash = Hash(cell);
                    Vector3 centre = grid.CellToWorld(cell);

                    if (inside)
                    {
                        PlaceTuft(placer, cell, hash, centre, road, blocked, terrain);
                        PlaceRubble(placer, cell, hash, centre, grid, road, terrain);
                        continue;
                    }

                    float distance = field.OutsideAt(centre.x, centre.z);
                    float chance = distance < 3f ? OuterChanceNear : distance < TerrainField.MinMarginCells ? OuterChanceFar : OuterChanceDeep;
                    if ((hash % 1000) / 1000f > chance || terrain.IsLiquid(cell))
                    {
                        continue;
                    }

                    float jitterX = ((hash >> 3) % 100) / 100f - 0.5f;
                    float jitterZ = ((hash >> 11) % 100) / 100f - 0.5f;
                    if (distance > SpireMinDistance && (hash >> 9) % 100 < SpireChancePercent)
                    {
                        placer.Place(PropKind.Spire, hash, centre.x + jitterX, centre.z + jitterZ, 0.8f + (hash % 5) * 0.12f, formation: false);
                        continue;
                    }

                    float scale = 0.85f + (hash % 7) * 0.05f + distance * OuterScaleGrowth;
                    placer.Place(kinds[(hash >> 5) % kinds.Length], hash, centre.x + jitterX * 0.9f, centre.z + jitterZ * 0.9f, scale, formation: false);
                }
            }

            placer.Finish();
        }

        // Small growth in the corner of a free build cell: it never reaches the middle where a tower stands.
        private static void PlaceTuft(Placer placer, Vector2Int cell, int hash, Vector3 centre, HashSet<Vector2Int> road, HashSet<Vector2Int> blocked, TerrainMap terrain)
        {
            if ((hash % 1000) / 1000f > TuftChance || road.Contains(cell) || blocked.Contains(cell) || terrain.IsLiquid(cell))
            {
                return;
            }

            float sx = (hash & 1) == 0 ? -TuftCornerOffset : TuftCornerOffset;
            float sz = (hash & 2) == 0 ? -TuftCornerOffset : TuftCornerOffset;
            PropKind kind = (hash >> 4) % 2 == 0 ? PropKind.Frond : PropKind.Rock;
            placer.Place(kind, hash, centre.x + sx, centre.z + sz, TuftScale, formation: false);
        }

        // Loose stones on the lip of raised ground, where it drops to a lower neighbour that is not the road.
        private static void PlaceRubble(Placer placer, Vector2Int cell, int hash, Vector3 centre, GridMap grid, HashSet<Vector2Int> road, TerrainMap terrain)
        {
            if (terrain.KindAt(cell) != TerrainKind.Raised)
            {
                return;
            }

            for (int i = 0; i < Steps.Length; i++)
            {
                Vector2Int neighbour = cell + Steps[i];
                int roll = (hash >> (i * 4)) % 100;
                if (terrain.KindAt(neighbour) == TerrainKind.Raised || road.Contains(neighbour) || roll / 100f > RubbleChance)
                {
                    continue;
                }

                float along = ((hash >> (i * 3 + 1)) % 100) / 100f - 0.5f;
                Vector3 outward = new Vector3(Steps[i].x, 0f, Steps[i].y) * (RubbleOffsetCells * grid.CellSize);
                Vector3 sideways = new Vector3(Steps[i].y, 0f, Steps[i].x) * (along * 0.6f * grid.CellSize);
                Vector3 spot = centre + outward + sideways;
                float scale = RubbleScaleMin + (roll / 100f) * RubbleScaleSpread;
                placer.Place(PropKind.Rock, hash + i * 977, spot.x, spot.z, scale, formation: false);
            }
        }

        // Broken rock and basalt teeth on the crater rim around a lava pool, wherever the rim is not the road.
        private static void PlaceLavaRim(Placer placer, Vector2Int cell, GridMap grid, HashSet<Vector2Int> road, TerrainMap terrain)
        {
            int hash = Hash(cell);
            Vector3 centre = grid.CellToWorld(cell);
            for (int i = 0; i < Steps.Length; i++)
            {
                Vector2Int neighbour = cell + Steps[i];
                int roll = (hash >> (i * 5)) % 100;
                if (terrain.KindAt(neighbour) == TerrainKind.Lava || road.Contains(neighbour) || !grid.Contains(neighbour) || roll / 100f > LavaRimChance)
                {
                    continue;
                }

                float along = ((hash >> (i * 3 + 2)) % 100) / 100f - 0.5f;
                Vector3 outward = new Vector3(Steps[i].x, 0f, Steps[i].y) * (LavaRimOffsetCells * grid.CellSize);
                Vector3 sideways = new Vector3(Steps[i].y, 0f, Steps[i].x) * (along * 0.7f * grid.CellSize);
                Vector3 spot = centre + outward + sideways;
                float scale = LavaRimScaleMin + (roll / 100f) * LavaRimScaleSpread;
                PropKind kind = (hash >> (i * 2 + 7)) % 100 < LavaSpikePercent ? PropKind.Spike : PropKind.Rock;
                placer.Place(kind, hash + i * 1453, spot.x, spot.z, scale, formation: false, onRim: true);
            }
        }

        // A stable pseudo-random number per cell.
        private static int Hash(Vector2Int cell)
        {
            unchecked
            {
                int h = cell.x * 73856093 ^ cell.y * 19349663;
                h ^= h >> 13;
                return h & 0x7fffffff;
            }
        }

        // Builds each mesh once and puts instances of it on the ground.
        private sealed class Placer
        {
            private readonly TerrainField _field;
            private readonly Transform _root;
            private readonly Material[][] _materials;
            private readonly Dictionary<(PropKind, int), Mesh> _meshes = new Dictionary<(PropKind, int), Mesh>();
            private readonly List<(Transform Prop, PropKind Kind, float Scale, int Hash, Color Colour)> _lit = new List<(Transform, PropKind, float, int, Color)>();

            public Placer(VisualTheme theme, TerrainField field, Transform root)
            {
                _field = field;
                _root = root;
                _materials = new Material[PropFactory.ColourVariants][];
                var pulsing = new List<Material>();
                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i] = PropFactory.CreateMaterials(theme, i);
                    pulsing.Add(_materials[i][PropFactory.FirstGlowSlot]);
                    pulsing.Add(_materials[i][PropFactory.SecondGlowSlot]);
                }

                root.gameObject.AddComponent<EmissionPulse>().Initialize(pulsing.ToArray(), PulseDepth, PulseSpeed);
            }

            /// <param name="onRim">Allowed on the edge of a liquid, where ordinary decoration keeps its distance.</param>
            public void Place(PropKind kind, int hash, float x, float z, float scale, bool formation, bool onRim = false)
            {
                bool nearLiquid = _field.WaterAt(x, z) > LiquidAvoid || _field.LavaAt(x, z) > LiquidAvoid || _field.SandAt(x, z) > LiquidAvoid;
                if (nearLiquid && !onRim)
                {
                    return;
                }

                int variant = (hash >> 7) % PropFactory.Variants;
                if (!_meshes.TryGetValue((kind, variant), out Mesh mesh))
                {
                    mesh = PropFactory.Create(kind, variant);
                    _meshes[(kind, variant)] = mesh;
                }

                Material[] materials = _materials[(hash >> 9) % _materials.Length];
                var go = new GameObject(kind.ToString());
                go.transform.SetParent(_root, false);
                go.transform.position = new Vector3(x, _field.HeightAt(x, z) - SinkIntoGround, z);
                go.transform.rotation = Quaternion.Euler(0f, hash % 360, 0f);
                go.transform.localScale = Vector3.one * scale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;

                if (PropFactory.IsPlant(kind))
                {
                    float speed = SwaySpeedMin + ((hash >> 2) % 100) / 100f * SwaySpeedSpread;
                    go.AddComponent<WindSway>().Initialize(SwayDegrees, speed, (hash % 628) / 100f);
                }

                // Only the bright kinds pool light on the ground, and only the formations get a real light.
                if (PropFactory.IsGlowing(kind) && kind != PropKind.Spike)
                {
                    bool second = (hash >> 6) % 2 == 0;
                    Color colour = materials[second ? PropFactory.SecondGlowSlot : PropFactory.FirstGlowSlot].GetColor("_BaseColor");
                    GlowDecal.PlacePool(go.transform, colour * PoolBrightness, PoolRadius * scale);
                    if (formation)
                    {
                        _lit.Add((go.transform, kind, scale, hash, colour));
                    }
                }
            }

            // The biggest glowing formations get a light and, for crystals, a drift of sparks. Lights are few, so
            // every renderer keeps its share of the per-object limit for the lava and the lava balls.
            public void Finish()
            {
                _lit.Sort((a, b) => b.Scale.CompareTo(a.Scale));
                int lights = Mathf.Min(MaxLights, _lit.Count);
                for (int i = 0; i < lights; i++)
                {
                    (Transform prop, PropKind kind, float scale, int _, Color colour) = _lit[i];
                    var lightObject = new GameObject("Glow");
                    lightObject.transform.SetParent(prop, false);
                    lightObject.transform.localPosition = new Vector3(0f, LightHeight, 0f);
                    var light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = colour;
                    light.range = LightRange * scale;
                    light.shadows = LightShadows.None;
                    lightObject.AddComponent<LightFlicker>().Initialize(LightIntensity, LightFlicker, LightFlickerSpeed);

                    if (kind == PropKind.Crystal)
                    {
                        Sparks(prop, colour, scale);
                    }
                }
            }

            private static void Sparks(Transform prop, Color colour, float scale)
            {
                var go = new GameObject("Sparks");
                go.transform.SetParent(prop, false);
                go.transform.localPosition = new Vector3(0f, 0.3f, 0f);

                var system = go.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = system.main;
                main.loop = true;
                main.prewarm = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(SparkLifeMin, SparkLifeMax);
                main.startSpeed = SparkRise;
                main.startSize = new ParticleSystem.MinMaxCurve(SparkSizeMin, SparkSizeMax);
                Color glow = colour * SparkBrightness;
                glow.a = 1f;
                main.startColor = glow;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = Mathf.CeilToInt(SparkRate * SparkLifeMax) + 4;

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTime = SparkRate;

                ParticleSystem.ShapeModule shape = system.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = SparkRadius * scale;

                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = 0.12f;
                noise.frequency = 0.6f;

                ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
                fade.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
                fade.color = gradient;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                system.Play();
            }
        }
    }
}
