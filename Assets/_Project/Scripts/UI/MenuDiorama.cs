using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The living scene behind the main menu: a floating island with the hive on its mound, a lava pool, a pond and
    /// a sand pit in the colours of the levels they come from, overgrown and glowing, with the creatures of each
    /// corner coming and going and flyers circling the hive. Built once from a <see cref="MenuStageDefinition"/>;
    /// everything that moves is the game's own view code.
    /// </summary>
    public sealed class MenuDiorama : MonoBehaviour
    {
        // Layout in world units. The menu's buttons cover the left of the screen, so the stage leans to the right.
        private static readonly Vector3 IslandCentre = new Vector3(3.6f, 0f, 2.2f);
        private const float IslandRadius = 11.5f;
        private static readonly Vector3 NestCentre = new Vector3(4.8f, 0f, 5.2f);
        private const float NestRadius = 4.2f;
        private const float NestHeight = 2.6f;
        private const float PorchHeight = 0.3f;
        private const float MouthRadius = 1.5f;
        private const float MouthRimThickness = 0.2f;
        private const float MouthInset = 1.4f;
        private static readonly Vector3 LavaCentre = new Vector3(8.6f, 0f, -0.9f);
        private const float LavaRadius = 2.1f;
        private static readonly Vector3 PondCentre = new Vector3(1.4f, 0f, -2.2f);
        private const float PondRadius = 2f;
        private static readonly Vector3 SandCentre = new Vector3(-1.8f, 0f, 6f);
        private const float SandRadius = 1.7f;
        private static readonly Vector3 IceCentre = new Vector3(-3.4f, 0f, 0.6f);
        private const float IceRadius = 1.7f;
        private static readonly Vector3 BogCentre = new Vector3(10.2f, 0f, 5.8f);
        private const float BogRadius = 1.8f;
        // The queen is modelled as a boss; on the porch she stands at this share of her field size.
        private const float GuardianScale = 0.55f;
        private const float PoolCellSize = 0.5f;
        private const float Seed = 3.7f;

        private static readonly Vector3 CameraFocus = new Vector3(3.1f, 0.6f, 1.8f);
        private const float CameraDistance = 15.5f;
        private const float CameraPitch = 29f;
        private const float CameraYaw = 6f;
        private const float CameraFieldOfView = 36f;

        private static readonly Vector3 SunRotation = new Vector3(46f, -38f, 0f);
        private static readonly Color SunColor = new Color(0.72f, 0.68f, 0.9f);
        private const float SunIntensity = 0.85f;
        private static readonly Color FogColor = new Color(0.05f, 0.025f, 0.055f);
        private const float FogDensity = 0.011f;
        private const float AmbientShare = 0.4f;
        private const float AmbientBrightness = 0.7f;

        private const float WalkerLapRadius = 3.9f;
        private const int LapPoints = 28;
        private const float WalkerSpeed = 0.75f;
        private const float WalkerRestMin = 3f;
        private const float WalkerRestMax = 9f;
        private const float WalkerDelayStep = 7f;
        private const float FlyerHeight = 2.1f;
        private const float FlyerLapRadius = 6.2f;
        private const int FlyerLaps = 40;
        private const float FlyerSpeed = 0.55f;
        private const int FlyerCount = 2;
        private const float PoolCreatureSpeed = 0.7f;
        private const float PoolRestMin = 5f;
        private const float PoolRestMax = 12f;
        private const float PoolBankMargin = 0.9f;
        private const float PoolBankArcDegrees = 150f;
        private const int BankPoints = 10;

        private const float PropSink = 0.05f;
        private const float PropSwayDegrees = 3.5f;
        private const float GlowPulseDepth = 0.3f;
        private const float GlowPulseSpeed = 1.1f;
        private const float GlowPoolRadius = 0.9f;
        private const float GlowPoolBrightness = 0.5f;
        private const float MouthLightIntensity = 2.6f;
        private const float MouthLightRange = 6f;
        private const float PondLightIntensity = 1.3f;
        private const float PondLightRange = 4.5f;
        private const float CrystalLightIntensity = 1.1f;
        private const float CrystalLightRange = 4f;
        private const float LightFlicker = 0.25f;
        private const float LightFlickerSpeed = 1.6f;

        private const int StarCount = 1600;
        private const float StarDepth = -9f;
        private const float StarFieldSize = 90f;
        private const float StarSize = 0.11f;
        private static readonly Color StarColor = new Color(0.75f, 0.85f, 1f);
        private const float MoteRate = 14f;
        private const float MoteHeight = 4f;
        private const float AmbienceVolume = 0.35f;

        private AudioMix _mix;
        private AudioSource _ambience;
        private readonly List<Material> _pulsing = new List<Material>();
        private readonly Dictionary<(PropKind, int), Mesh> _propMeshes = new Dictionary<(PropKind, int), Mesh>();
        private MenuTerrain _terrain;
        private TargetRegistry _targets;
        private StructureRegistry _structures;
        private System.Random _random;
        private float _emergeDelay;

        /// <summary>Builds the stage under <paramref name="camera"/>, which it also takes charge of.</summary>
        public static MenuDiorama Build(MenuStageDefinition stage, Camera camera, AudioMix mix, float particleScale)
        {
            var root = new GameObject("MenuDiorama");
            var diorama = root.AddComponent<MenuDiorama>();
            diorama._mix = mix;
            diorama._random = new System.Random(17);
            diorama._targets = new TargetRegistry();
            diorama._structures = new StructureRegistry();
            diorama.BuildAll(stage, camera, particleScale);
            return diorama;
        }

        private void OnDestroy()
        {
            if (_mix != null)
            {
                _mix.Changed -= ApplyAmbience;
            }
        }

        private void BuildAll(MenuStageDefinition stage, Camera camera, float particleScale)
        {
            Vector3 mouth = NestCentre + Vector3.back * (NestRadius - MouthInset);
            var poolList = new List<MenuTerrain.Pool>
            {
                new MenuTerrain.Pool(LavaCentre, LavaRadius, TerrainKind.Lava, stage.LavaTheme),
                new MenuTerrain.Pool(PondCentre, PondRadius, TerrainKind.Water, stage.ForestTheme),
                new MenuTerrain.Pool(SandCentre, SandRadius, TerrainKind.Quicksand, stage.RuinsTheme),
            };
            // The frozen lake and the bog only exist once their worlds do.
            if (stage.IceTheme != null)
            {
                poolList.Add(new MenuTerrain.Pool(IceCentre, IceRadius, TerrainKind.Ice, stage.IceTheme));
            }

            if (stage.SwampTheme != null)
            {
                poolList.Add(new MenuTerrain.Pool(BogCentre, BogRadius, TerrainKind.Bog, stage.SwampTheme));
            }

            MenuTerrain.Pool[] pools = poolList.ToArray();
            _terrain = new MenuTerrain(IslandCentre, IslandRadius, NestCentre, NestRadius, NestHeight, mouth, PorchHeight, pools, stage.NestTheme, Seed);

            BuildLighting();
            BuildCamera(camera);
            _terrain.Build(transform, stage.RuinsTheme);
            foreach (MenuTerrain.Pool pool in pools)
            {
                TerrainLiquidBuilder.BuildPool(pool.Kind, pool.Theme, _terrain.PoolCells(pool, PoolCellSize), PoolCellSize, transform, particleScale);
            }

            BuildMouth(mouth, stage.NestTheme);
            BuildGrowth(stage, mouth);
            BuildStarfield();
            if (particleScale > 0f)
            {
                BuildMotes(stage.NestTheme, particleScale);
            }

            BuildCreatures(stage, mouth);
            BuildAmbience(stage.NestTheme);
            gameObject.AddComponent<EmissionPulse>().Initialize(_pulsing.ToArray(), GlowPulseDepth, GlowPulseSpeed);
        }

        private static void BuildLighting()
        {
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = SunIntensity;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(SunRotation);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(FogColor, SunColor, AmbientShare) * AmbientBrightness;
        }

        private static void BuildCamera(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FogColor;
            camera.fieldOfView = CameraFieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 80f;
            camera.gameObject.AddComponent<MenuCameraDrift>().Initialize(CameraFocus, CameraDistance, CameraPitch, CameraYaw);
        }

        // The hive mouth: a black oval against the slope with a glowing chitin rim, lit red from inside.
        private void BuildMouth(Vector3 mouth, VisualTheme nest)
        {
            Vector3 onSlope = _terrain.OnGround(mouth.x, mouth.z + MouthInset * 0.55f);
            Vector3 normal = _terrain.Normal(onSlope.x, onSlope.z);
            Vector3 centre = onSlope + normal * 0.04f;

            var hole = new GameObject("MouthHole");
            hole.transform.SetParent(transform, false);
            hole.transform.position = centre;
            hole.transform.rotation = Quaternion.LookRotation(-normal, Vector3.forward);
            hole.transform.localScale = new Vector3(MouthRadius * 2f, MouthRadius * 1.5f, 1f);
            hole.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var holeRenderer = hole.AddComponent<MeshRenderer>();
            holeRenderer.sharedMaterial = SurfaceMaterials.Create(Color.black, 0f, 0f);
            holeRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var builder = new PropMeshBuilder(1);
            builder.AddTorus(Vector3.zero, Vector3.forward, 1f, MouthRimThickness, 40, 8, 0);
            var rim = new GameObject("MouthRim");
            rim.transform.SetParent(transform, false);
            rim.transform.position = centre;
            rim.transform.rotation = hole.transform.rotation;
            rim.transform.localScale = new Vector3(MouthRadius, MouthRadius * 0.75f, 1f);
            rim.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("MouthRim");
            Material rimMaterial = SurfaceMaterials.Create(nest.AccentColor, 1.6f, 0.7f);
            rim.AddComponent<MeshRenderer>().sharedMaterial = rimMaterial;
            _pulsing.Add(rimMaterial);

            AddLight(centre + normal * 0.6f, nest.AccentColor, MouthLightIntensity, MouthLightRange);
            GlowDecal.PlacePool(transform, nest.AccentColor * 0.6f, 2.2f).transform.position = _terrain.OnGround(mouth.x, mouth.z - 0.6f, 0.04f);
        }

        // Every corner is dressed with the props of its own theme; glow materials join the shared pulse.
        private void BuildGrowth(MenuStageDefinition stage, Vector3 mouth)
        {
            Material[][] forest = Palette(stage.ForestTheme);
            Material[][] lava = Palette(stage.LavaTheme);
            Material[][] ruins = Palette(stage.RuinsTheme);
            Material[][] nest = Palette(stage.NestTheme);

            Ring(PondCentre, PondRadius + 0.9f, PondRadius + 2.2f, 5, PropKind.Frond, forest, 0.8f, 1.2f, 150f, 400f);
            Ring(PondCentre, PondRadius + 1.1f, PondRadius + 2.4f, 2, PropKind.GlowTree, forest, 0.9f, 1.15f, 180f, 330f);
            Ring(PondCentre, PondRadius + 0.7f, PondRadius + 1.9f, 4, PropKind.Mushroom, forest, 0.6f, 1f, 100f, 420f);
            AddLight(_terrain.OnGround(PondCentre.x + 1.6f, PondCentre.z + 1.2f, 0.9f), stage.ForestTheme.AccentColor, PondLightIntensity, PondLightRange);

            Ring(LavaCentre, LavaRadius + 0.8f, LavaRadius + 1.9f, 4, PropKind.Spike, lava, 0.7f, 1.2f, 20f, 200f);
            Ring(LavaCentre, LavaRadius + 1.4f, LavaRadius + 2.6f, 2, PropKind.Boulder, lava, 0.8f, 1.1f, 40f, 160f);
            Ring(LavaCentre, LavaRadius + 1.6f, LavaRadius + 2.4f, 1, PropKind.Column, lava, 0.9f, 1f, 220f, 260f);
            Ring(LavaCentre, LavaRadius + 0.9f, LavaRadius + 2.2f, 3, PropKind.Rock, lava, 0.6f, 1f, 190f, 350f);

            Ring(SandCentre, SandRadius + 0.8f, SandRadius + 1.7f, 2, PropKind.Column, ruins, 0.8f, 1.1f, 200f, 320f);
            Ring(SandCentre, SandRadius + 0.7f, SandRadius + 1.6f, 1, PropKind.Crystal, ruins, 1.3f, 1.4f, 300f, 340f);
            Ring(SandCentre, SandRadius + 0.6f, SandRadius + 1.5f, 2, PropKind.Crystal, ruins, 0.6f, 0.9f, 20f, 160f);
            Ring(SandCentre, SandRadius + 0.9f, SandRadius + 1.8f, 2, PropKind.Rock, ruins, 0.6f, 0.9f, 170f, 250f);
            AddLight(_terrain.OnGround(SandCentre.x + 2.2f, SandCentre.z - 1.4f, 0.8f), stage.RuinsTheme.AccentColor, CrystalLightIntensity, CrystalLightRange);

            Ring(NestCentre, NestRadius + 0.3f, NestRadius + 1.3f, 5, PropKind.Egg, nest, 0.8f, 1.2f, -40f, 250f);
            Ring(NestCentre, NestRadius * 0.45f, NestRadius * 0.8f, 6, PropKind.Tendril, nest, 0.9f, 1.3f, -30f, 330f);
            Ring(mouth, MouthRadius + 0.6f, MouthRadius + 1.1f, 4, PropKind.Spike, nest, 0.6f, 0.9f, 200f, 340f);
            Ring(NestCentre, NestRadius * 0.2f, NestRadius * 0.5f, 3, PropKind.Spike, nest, 0.8f, 1.1f, 0f, 360f);

            Ring(IslandCentre, 6.5f, IslandRadius - 2.8f, 9, PropKind.Rock, ruins, 0.5f, 1.1f, 0f, 360f);
            Ring(IslandCentre, 7f, IslandRadius - 3f, 3, PropKind.Boulder, ruins, 0.7f, 1f, 0f, 360f);
        }

        private Material[][] Palette(VisualTheme theme)
        {
            var palette = new Material[PropFactory.ColourVariants][];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = PropFactory.CreateMaterials(theme, i);
                _pulsing.Add(palette[i][PropFactory.FirstGlowSlot]);
                _pulsing.Add(palette[i][PropFactory.SecondGlowSlot]);
            }

            return palette;
        }

        // Props of one kind spread over an arc around a centre; angles run counter-clockwise from +x.
        private void Ring(Vector3 centre, float minRadius, float maxRadius, int count, PropKind kind, Material[][] palette, float minScale, float maxScale, float fromDegrees, float toDegrees)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, (i + 0.5f) / count) + Range(-8f, 8f);
                float radius = Range(minRadius, maxRadius);
                float x = centre.x + Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
                float z = centre.z + Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
                PlaceProp(kind, palette[_random.Next(palette.Length)], x, z, Range(minScale, maxScale));
            }
        }

        private void PlaceProp(PropKind kind, Material[] materials, float x, float z, float scale)
        {
            int variant = _random.Next(PropFactory.Variants);
            if (!_propMeshes.TryGetValue((kind, variant), out Mesh mesh))
            {
                mesh = PropFactory.Create(kind, variant);
                _propMeshes[(kind, variant)] = mesh;
            }

            var go = new GameObject(kind.ToString());
            go.transform.SetParent(transform, false);
            go.transform.position = _terrain.OnGround(x, z, -PropSink);
            go.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;

            if (PropFactory.IsPlant(kind))
            {
                go.AddComponent<WindSway>().Initialize(PropSwayDegrees, Range(0.5f, 0.9f), Range(0f, 6.28f));
            }

            if (PropFactory.IsGlowing(kind) && kind != PropKind.Spike)
            {
                Color colour = materials[PropFactory.FirstGlowSlot].GetColor("_BaseColor");
                GlowDecal.PlacePool(go.transform, colour * GlowPoolBrightness, GlowPoolRadius);
            }
        }

        private void AddLight(Vector3 position, Color colour, float intensity, float range)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = range;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>().Initialize(intensity, LightFlicker, LightFlickerSpeed);
        }

        private void BuildCreatures(MenuStageDefinition stage, Vector3 mouth)
        {
            Vector3 inside = new Vector3(mouth.x, PorchHeight, mouth.z + MouthInset * 0.9f);
            Vector3 porch = new Vector3(mouth.x, PorchHeight, mouth.z - 0.7f);

            if (stage.NestGuardian != null)
            {
                Vector3 seat = new Vector3(mouth.x, PorchHeight, mouth.z + 0.1f);
                MenuCreature guardian = Creature(stage.NestGuardian, new[] { seat, seat + Vector3.back * 0.01f }, 0f, 0f, EnemyOrigin.Gate, 0f, 0f);
                guardian.transform.localScale = Vector3.one * GuardianScale;
                guardian.Emerge(0.6f);
            }

            EnemyDefinition[] walkers = stage.NestCreatures;
            for (int i = 0; i < walkers.Length; i++)
            {
                if (walkers[i] == null)
                {
                    continue;
                }

                bool clockwise = i % 2 == 0;
                var route = new List<Vector3> { inside, porch };
                route.AddRange(Lap(NestCentre, WalkerLapRadius, -90f, clockwise ? -360f : 360f, LapPoints, true));
                route.Add(porch);
                route.Add(inside);
                Creature(walkers[i], route.ToArray(), WalkerSpeed, 0f, EnemyOrigin.Gate, WalkerRestMin, WalkerRestMax).Emerge(NextDelay());
            }

            if (stage.Flyer != null)
            {
                for (int i = 0; i < FlyerCount; i++)
                {
                    float start = i * 360f / FlyerCount;
                    Vector3[] route = Lap(NestCentre, FlyerLapRadius, start, 360f * FlyerLaps, LapPoints * FlyerLaps, false);
                    Creature(stage.Flyer, route, FlyerSpeed, FlyerHeight, EnemyOrigin.Gate, 1f, 1f).Emerge(1f + i * 3f);
                }
            }

            PoolDweller(stage.WaterCreature, PondCentre, PondRadius, TerrainKind.Water, EnemyOrigin.Water, 20f);
            PoolDweller(stage.LavaCreature, LavaCentre, LavaRadius, TerrainKind.Lava, EnemyOrigin.Lava, 260f);
            PoolDweller(stage.SandCreature, SandCentre, SandRadius, TerrainKind.Quicksand, EnemyOrigin.Quicksand, 300f);
            if (stage.IceTheme != null)
            {
                PoolDweller(stage.IceCreature, IceCentre, IceRadius, TerrainKind.Ice, EnemyOrigin.Ice, 200f);
            }

            if (stage.SwampTheme != null)
            {
                PoolDweller(stage.BogCreature, BogCentre, BogRadius, TerrainKind.Bog, EnemyOrigin.Bog, 120f);
            }
        }

        // Out of the middle of the pool, up the bank, a stroll along it and back into the deep.
        private void PoolDweller(EnemyDefinition definition, Vector3 centre, float radius, TerrainKind kind, EnemyOrigin origin, float bankDegrees)
        {
            if (definition == null)
            {
                return;
            }

            // A flyer born in the pool circles above its bank instead of crawling along it.
            if (definition.Flies)
            {
                var circuit = new List<Vector3> { new Vector3(centre.x, 0f, centre.z) };
                circuit.AddRange(Lap(centre, radius + PoolBankMargin, bankDegrees - PoolBankArcDegrees * 0.5f, PoolBankArcDegrees, BankPoints, false));
                circuit.Add(new Vector3(centre.x, 0f, centre.z));
                Creature(definition, circuit.ToArray(), PoolCreatureSpeed * 1.5f, FlyerHeight, origin, PoolRestMin, PoolRestMax).Emerge(NextDelay());
                return;
            }

            float surface = TerrainLiquidBuilder.SurfaceLevel(kind);
            Vector3 deep = new Vector3(centre.x, surface - 0.15f, centre.z);
            var route = new List<Vector3> { deep };
            route.AddRange(Lap(centre, radius + PoolBankMargin, bankDegrees - PoolBankArcDegrees * 0.5f, PoolBankArcDegrees, BankPoints, true));
            route.Add(new Vector3(centre.x, surface - 0.2f, centre.z));
            Creature(definition, route.ToArray(), PoolCreatureSpeed, 0f, origin, PoolRestMin, PoolRestMax).Emerge(NextDelay());
        }

        private MenuCreature Creature(EnemyDefinition definition, Vector3[] route, float speed, float height, EnemyOrigin origin, float restMin, float restMax) =>
            MenuCreature.Create(transform, definition, route, speed, height, origin, restMin, restMax, _targets, _structures);

        // Points along an arc; grounded ones follow the terrain, airborne ones stay level.
        private Vector3[] Lap(Vector3 centre, float radius, float fromDegrees, float sweepDegrees, int points, bool grounded)
        {
            var result = new Vector3[points + 1];
            for (int i = 0; i <= points; i++)
            {
                float angle = (fromDegrees + sweepDegrees * i / points) * Mathf.Deg2Rad;
                float x = centre.x + Mathf.Cos(angle) * radius;
                float z = centre.z + Mathf.Sin(angle) * radius;
                result[i] = grounded ? _terrain.OnGround(x, z, 0.02f) : new Vector3(x, 0f, z);
            }

            return result;
        }

        private float NextDelay()
        {
            _emergeDelay += WalkerDelayStep * 0.5f;
            return _emergeDelay;
        }

        // A static field of stars far below the island, so it floats in space like the levels do.
        private void BuildStarfield()
        {
            var go = new GameObject("Starfield");
            go.transform.SetParent(transform, false);
            go.transform.position = IslandCentre + Vector3.up * StarDepth;

            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = float.MaxValue;
            main.startSpeed = 0f;
            main.startSize = StarSize;
            main.startColor = StarColor;
            main.maxParticles = StarCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(StarFieldSize, 6f, StarFieldSize);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            system.Emit(StarCount);
            system.Play();
        }

        // Spores drifting up over the whole island in the hive's colour.
        private void BuildMotes(VisualTheme nest, float particleScale)
        {
            var go = new GameObject("Spores");
            go.transform.SetParent(transform, false);
            go.transform.position = IslandCentre + Vector3.up * (MoteHeight * 0.5f);

            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            Color glow = nest.AccentColor * 1.6f;
            glow.a = 0.8f;
            main.startColor = glow;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = MoteRate * particleScale;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(IslandRadius * 2f, MoteHeight, IslandRadius * 2f);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = 0.15f;

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.3f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            system.Play();
        }

        private void BuildAmbience(VisualTheme nest)
        {
            AudioClip bed = ProceduralSfx.Ambience(nest.Ambience);
            if (bed == null || _mix == null)
            {
                return;
            }

            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.clip = bed;
            _ambience.loop = true;
            _ambience.playOnAwake = false;
            _ambience.spatialBlend = 0f;
            ApplyAmbience();
            _ambience.Play();
            _mix.Changed += ApplyAmbience;
        }

        private void ApplyAmbience()
        {
            if (_ambience != null)
            {
                _ambience.volume = AmbienceVolume * _mix.AmbienceGain;
            }
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
    }
}
