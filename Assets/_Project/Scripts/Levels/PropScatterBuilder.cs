using System.Collections.Generic;
using Game.Core;
using UnityEngine;

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
        private const int IceShardPercent = 45;
        private const int ReedPercent = 75;
        private const int ScreePercent = 60;
        private const int RiftCrystalOneIn = 4;
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
        // Round 39: a pool lies on the ground's own slope (fitted over its radius) this far above it; flat on a slope it cut
        // through the ground and read as a hard bright line from the side.
        private const float PoolLift = 0.04f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Vector2Int[] Steps = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        /// <param name="instancer">Draws the imported stand-ins of the theme's environment set; null keeps every kind procedural.</param>
        public static void Build(LevelDefinition level, VisualTheme theme, TerrainField field, Transform parent, EnvironmentInstancer instancer = null)
        {
            PropKind[] kinds = theme.PropKinds;
            if (kinds == null || kinds.Length == 0)
            {
                return;
            }

            var root = new GameObject("Props").transform;
            root.SetParent(parent, false);

            var placer = new Placer(theme, field, root, instancer, new StructureClearance(level));
            GridMap grid = level.Grid;
            TerrainMap terrain = level.Terrain;
            var road = new HashSet<Vector2Int>(level.Road.Cells);
            var blocked = new HashSet<Vector2Int>(level.BlockedCells);

            foreach (Vector2Int cell in blocked)
            {
                int hash = Hash(cell);
                Vector3 centre = grid.CellToWorld(cell);
                placer.Place(FormationAt(cell, kinds), hash, centre.x, centre.z, FormationScale(cell), formation: true);
            }

            foreach (KeyValuePair<Vector2Int, TerrainKind> patch in terrain.Cells)
            {
                switch (patch.Value)
                {
                    case TerrainKind.Lava:
                        PlaceRim(placer, patch.Key, grid, road, terrain, TerrainKind.Lava, PropKind.Spike, PropKind.Rock, LavaSpikePercent);
                        break;
                    case TerrainKind.Ice:
                        PlaceRim(placer, patch.Key, grid, road, terrain, TerrainKind.Ice, PropKind.IceShard, PropKind.Snowdrift, IceShardPercent);
                        break;
                    case TerrainKind.Bog:
                        PlaceRim(placer, patch.Key, grid, road, terrain, TerrainKind.Bog, PropKind.Reed, PropKind.DeadTree, ReedPercent);
                        break;
                    case TerrainKind.Rift:
                        // Spilled scree and boulders on the rim, and now and then a crystal pushed up by the crack.
                        PropKind rare = Hash(patch.Key) % RiftCrystalOneIn == 0 ? PropKind.Crystal : PropKind.Boulder;
                        PlaceRim(placer, patch.Key, grid, road, terrain, TerrainKind.Rift, PropKind.Scree, rare, ScreePercent);
                        break;
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
            // An imported environment lays a pad on every free build cell: a tuft would grow through its corner.
            if (placer.Imported || (hash % 1000) / 1000f > TuftChance || road.Contains(cell) || blocked.Contains(cell) || terrain.IsLiquid(cell))
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
        // Decoration along the shore of a pool: spikes and rock around lava, shards and drifts around ice, reeds
        // and dead trees around a bog. The first kind wins the given percentage of the spots.
        private static void PlaceRim(Placer placer, Vector2Int cell, GridMap grid, HashSet<Vector2Int> road, TerrainMap terrain, TerrainKind pool, PropKind first, PropKind second, int firstPercent)
        {
            int hash = Hash(cell);
            Vector3 centre = grid.CellToWorld(cell);
            for (int i = 0; i < Steps.Length; i++)
            {
                Vector2Int neighbour = cell + Steps[i];
                int roll = (hash >> (i * 5)) % 100;
                if (terrain.KindAt(neighbour) == pool || road.Contains(neighbour) || !grid.Contains(neighbour) || roll / 100f > LavaRimChance)
                {
                    continue;
                }

                float along = ((hash >> (i * 3 + 2)) % 100) / 100f - 0.5f;
                Vector3 outward = new Vector3(Steps[i].x, 0f, Steps[i].y) * (LavaRimOffsetCells * grid.CellSize);
                Vector3 sideways = new Vector3(Steps[i].y, 0f, Steps[i].x) * (along * 0.7f * grid.CellSize);
                Vector3 spot = centre + outward + sideways;
                float scale = LavaRimScaleMin + (roll / 100f) * LavaRimScaleSpread;
                PropKind kind = (hash >> (i * 2 + 7)) % 100 < firstPercent ? first : second;
                placer.Place(kind, hash + i * 1453, spot.x, spot.z, scale, formation: false, onRim: true);
            }
        }

        /// <summary>The formation the scatter puts on a blocked cell of a level with these decoration kinds.</summary>
        public static PropKind FormationAt(Vector2Int cell, PropKind[] kinds) => kinds[Hash(cell) % kinds.Length];

        /// <summary>How big the scatter makes the formation on a blocked cell, as a scale on the decoration kind's size.</summary>
        public static float FormationScale(Vector2Int cell) => BlockedScaleMin + (Hash(cell) % 100) / 100f * BlockedScaleSpread;

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
            private readonly EnvironmentSet _set;
            private readonly EnvironmentInstancer _instancer;
            private readonly StructureClearance _clearance;
            private readonly bool _low;
            private readonly Dictionary<Material, Material> _dimmed = new Dictionary<Material, Material>();
            private readonly Material[][] _materials;
            private readonly Dictionary<(PropKind, int), Mesh> _meshes = new Dictionary<(PropKind, int), Mesh>();
            private readonly List<(Transform Prop, PropKind Kind, float Scale, int Hash, Color Colour)> _lit = new List<(Transform, PropKind, float, int, Color)>();

            public Placer(VisualTheme theme, TerrainField field, Transform root, EnvironmentInstancer instancer, StructureClearance clearance)
            {
                _clearance = clearance;
                _field = field;
                _root = root;
                _instancer = instancer;
                _set = instancer != null ? theme.Environment : null;
                _low = ModelQuality.IsLow;
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

            /// <summary>Whether the theme's imported environment dresses this level.</summary>
            public bool Imported => _set != null;

            /// <param name="onRim">Allowed on the edge of a liquid, where ordinary decoration keeps its distance.</param>
            public void Place(PropKind kind, int hash, float x, float z, float scale, bool formation, bool onRim = false)
            {
                bool nearLiquid = _field.WaterAt(x, z) > LiquidAvoid || _field.LavaAt(x, z) > LiquidAvoid || _field.SandAt(x, z) > LiquidAvoid;
                if (nearLiquid && !onRim)
                {
                    return;
                }

                EnvironmentDecor decor = _set != null ? _set.Decor(kind) : null;
                if (decor != null)
                {
                    PlaceImported(decor, kind, hash, x, z, scale, formation);
                    return;
                }

                int variant = (hash >> 7) % PropFactory.Variants;
                if (!_meshes.TryGetValue((kind, variant), out Mesh mesh))
                {
                    mesh = PropFactory.Create(kind, variant);
                    _meshes[(kind, variant)] = mesh;
                }

                if (_clearance.Blocks(new Vector3(x, 0f, z), StructureClearance.Reach(mesh, Matrix4x4.Scale(Vector3.one * scale))))
                {
                    return;
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
                    LayOnSlope(GlowDecal.PlacePool(go.transform, colour * PoolBrightness, PoolRadius * scale), x, z, PoolRadius * scale);
                    if (formation)
                    {
                        _lit.Add((go.transform, kind, scale, hash, colour));
                    }
                }
            }

            // The imported stand-in, drawn instanced (plants sway in its shader). Glowing kinds still pool light on the
            // ground and the formations among them may get a light, so they need a small holder object.
            private void PlaceImported(EnvironmentDecor decor, PropKind kind, int hash, float x, float z, float scale, bool formation)
            {
                EnvironmentMesh variant = decor.Variants[(hash >> 7) % decor.Variants.Length];
                var position = new Vector3(x, _field.HeightAt(x, z) - SinkIntoGround, z);
                Quaternion rotation = Quaternion.Euler(0f, hash % 360, 0f);
                bool far = _field.OutsideAt(x, z) > _set.FarDecorStart;
                if (far && _low && (hash >> 13) % 100 >= _set.LowFarDecorKeep * 100f)
                {
                    // The Low profile thins out the far wilderness: it is dim and half hidden in the mist anyway.
                    return;
                }

                Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one * (scale * decor.Scale));
                if (_clearance.Blocks(position, StructureClearance.Reach(variant.Mesh, matrix * variant.Node)))
                {
                    return;
                }

                Material material = far ? Dimmed(variant.MaterialFor(_low)) : null;
                _instancer.AddDecor(variant, matrix, _low, material, castShadows: !far, formation: formation);
                if (!PropFactory.IsGlowing(kind) || kind == PropKind.Spike)
                {
                    return;
                }

                var holder = new GameObject(kind.ToString()).transform;
                holder.SetParent(_root, false);
                holder.position = position;
                _instancer.AttachToLast(holder);
                LayOnSlope(GlowDecal.PlacePool(holder, decor.Glow * PoolBrightness, PoolRadius * scale), x, z, PoolRadius * scale);
                if (formation)
                {
                    _lit.Add((holder, kind, scale, hash, decor.Glow));
                }
            }

            // Turns a flat pool onto the plane the ground makes under it and sets it just above that plane.
            private void LayOnSlope(GameObject pool, float x, float z, float radius)
            {
                float dx = _field.HeightAt(x + radius, z) - _field.HeightAt(x - radius, z);
                float dz = _field.HeightAt(x, z + radius) - _field.HeightAt(x, z - radius);
                Vector3 normal = new Vector3(-dx, 2f * radius, -dz).normalized;
                Transform transform = pool.transform;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * transform.rotation;
                transform.position = new Vector3(x, _field.HeightAt(x, z), z) + normal * PoolLift;
            }

            // The far wilderness wears a darker, dimmer copy of each material, as the painter darkens the far ground.
            private Material Dimmed(Material source)
            {
                if (!_dimmed.TryGetValue(source, out Material copy))
                {
                    copy = _instancer.Own(source);
                    copy.SetColor(BaseColorId, source.GetColor(BaseColorId) * _set.FarDecorAlbedo);
                    copy.SetColor(EmissionColorId, source.GetColor(EmissionColorId) * _set.FarDecorGlow);
                    _dimmed[source] = copy;
                }

                return copy;
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
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                ParticleKit.UseCell(system, MarkerArt.ParticleCell.Ember);
                system.Play();
            }
        }
    }
}
