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
    /// The living scene behind the main menu: the floating island built in Blender (<see cref="MenuDioramaArt"/>) with
    /// the hive on its mound, five pools in the colours of the worlds they come from, its props, lights and camera as
    /// laid out there, and the creatures of each corner coming and going and flyers circling the hive. Built once from a
    /// <see cref="MenuStageDefinition"/>; this class only places, lights and animates.
    /// </summary>
    public sealed class MenuDiorama : MonoBehaviour
    {
        // The creature routes (unchanged from the code-built island; the Blender layout keeps them clear).
        private static readonly Vector3 IslandCentre = new Vector3(3.6f, 0f, 2.2f);
        private const float IslandRadius = 11.5f;
        private static readonly Vector3 NestCentre = new Vector3(4.8f, 0f, 5.2f);
        private const float NestRadius = 4.2f;
        private const float PorchHeight = 0.3f;
        private const float MouthInset = 1.4f;
        // The queen is modelled as a boss; on the porch she stands at this share of her field size.
        private const float GuardianScale = 0.55f;
        private const float PoolCellSize = 0.5f;
        // The Blender outline is a 72-point polygon; a light Gaussian rounds its corners for the sheet and the banks.
        private const float ShoreSmoothing = 0.12f;


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

        private const float GlowPulseDepth = 0.3f;
        private const float GlowPulseSpeed = 1.1f;
        private const float GlowPoolRadius = 0.9f;
        private const float GlowPoolBrightness = 0.5f;
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
        private float _ambienceVolume;
        private readonly List<Material> _pulsing = new List<Material>();
        private readonly List<Material> _owned = new List<Material>();
        private MenuTerrain _terrain;
        private TargetRegistry _targets;
        private StructureRegistry _structures;
        private float _emergeDelay;

        /// <summary>Builds the stage under <paramref name="camera"/>, which it also takes charge of.</summary>
        public static MenuDiorama Build(MenuStageDefinition stage, Camera camera, AudioMix mix, float particleScale)
        {
            var root = new GameObject("MenuDiorama");
            var diorama = root.AddComponent<MenuDiorama>();
            diorama._mix = mix;
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

            foreach (Material material in _owned)
            {
                Destroy(material);
            }
        }

        private void BuildAll(MenuStageDefinition stage, Camera camera, float particleScale)
        {
            MenuDioramaArt art = stage.Art;
            if (art == null || !art.IsComplete)
            {
                Debug.LogWarning("Menu diorama: the stage has no Blender island; run the generator step \"Menu art\".");
                art = null;
            }

            bool low = ModelQuality.IsLow;
            _terrain = new MenuTerrain(art);
            // The hive theme's own light rig (sun, rim, trilight ambient, sky reflection), so the island's Blender props
            // read as they do in the levels.
            ThemeLighting.Apply(stage.NestTheme, transform);
            BuildCamera(camera, art, stage.NestTheme.FogColor);
            if (art != null)
            {
                BuildIsland(art, low);
                EnvironmentInstancer instancer = EnvironmentInstancer.Create(transform);
                BuildProps(art, stage, instancer, low);
                BuildPools(art, stage, instancer, particleScale, low);
                instancer.Finish();
                BuildLights(art, stage);
                if (stage.NestTheme != null && stage.NestTheme.Environment != null)
                {
                    LevelVisualBuilder.SetWind(stage.NestTheme.Environment);
                }
            }

            BuildStarfield();
            if (particleScale > 0f)
            {
                BuildMotes(stage.NestTheme, particleScale);
            }

            BuildCreatures(stage, art);
            BuildAmbience(stage);
            gameObject.AddComponent<EmissionPulse>().Initialize(_pulsing.ToArray(), GlowPulseDepth, GlowPulseSpeed);
        }

        private static void BuildCamera(Camera camera, MenuDioramaArt art, Color background)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 80f;
            if (art == null)
            {
                return;
            }

            camera.fieldOfView = art.CameraFieldOfView;
            camera.gameObject.AddComponent<MenuCameraDrift>().Initialize(art.CameraFocus, art.CameraDistance, art.CameraPitch, art.CameraYaw);
        }

        // The island mesh at the origin, its painted glow (cracks, veins, Crysium dust) breathing with the shared pulse.
        private void BuildIsland(MenuDioramaArt art, bool low)
        {
            var island = new GameObject("Island");
            island.transform.SetParent(transform, false);
            island.AddComponent<MeshFilter>().sharedMesh = art.Island;
            Material[] source = art.IslandMaterials(low);
            var materials = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                materials[i] = Own(source[i]);
                _pulsing.Add(materials[i]);
            }

            var renderer = island.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        // Every export of the layout where Blender put it; static ones drawn instanced, rigged ones held in a pose.
        private void BuildProps(MenuDioramaArt art, MenuStageDefinition stage, EnvironmentInstancer instancer, bool low)
        {
            MenuDioramaModel[] models = art.Models;
            var worn = new Material[models.Length];
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i].Pulses && models[i].Mesh != null && models[i].Mesh.IsValid)
                {
                    worn[i] = Own(models[i].Mesh.MaterialFor(low));
                    _pulsing.Add(worn[i]);
                }
            }

            foreach (MenuDioramaPlacement placement in art.Placements)
            {
                if (placement.Model < 0 || placement.Model >= models.Length)
                {
                    continue;
                }

                MenuDioramaModel model = models[placement.Model];
                Quaternion turn = Quaternion.Euler(0f, placement.Yaw, 0f);
                if (model.IsRigged)
                {
                    PlaceRigged(model, placement.Position, turn, placement.Scale, low);
                }
                else
                {
                    instancer.AddDecor(model.Mesh, Matrix4x4.TRS(placement.Position, turn, Vector3.one * placement.Scale), low, worn[placement.Model]);
                }

                VisualTheme theme = stage.Theme(placement.Theme);
                if (placement.GlowPool && theme != null)
                {
                    GlowDecal.PlacePool(transform, theme.AccentColor * GlowPoolBrightness, GlowPoolRadius).transform.position += placement.Position;
                }
            }
        }

        // A tower or the beacon: its own rig, held still in the first frame of its idle so the parts sit where they belong.
        private void PlaceRigged(MenuDioramaModel model, Vector3 position, Quaternion turn, float scale, bool low)
        {
            GameObject go = Instantiate(model.Rigged, position, turn, transform);
            go.name = model.Name;
            go.transform.localScale = Vector3.one * scale;
            Material material = model.RiggedMaterial(low);
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                // Each tier file holds the near and the far mesh on one rig; the menu shows the near one.
                renderer.enabled = !renderer.name.EndsWith(MenuDioramaModel.FarSuffix, System.StringComparison.Ordinal);
                renderer.sharedMaterial = material;
            }

            if (model.Pose != null)
            {
                model.Pose.SampleAnimation(go, 0f);
            }
        }

        // Each pool: the round-24 liquid sheet cut along Blender's shore and the continent's swept banks round it.
        private void BuildPools(MenuDioramaArt art, MenuStageDefinition stage, EnvironmentInstancer instancer, float particleScale, bool low)
        {
            var banked = new List<LiquidShape>();
            EnvironmentSet banks = stage.NestTheme != null ? stage.NestTheme.Environment : null;
            foreach (MenuDioramaPool pool in art.Pools)
            {
                VisualTheme theme = stage.Theme(pool.Theme);
                if (theme == null || pool.Outline.Length < 3)
                {
                    continue;
                }

                var shape = new LiquidShape(pool.Kind, new[] { Shore(pool.Outline) });
                // The menu's own look first: the level water reads near black over the island's dark painted basin.
                LiquidLook look = (art.Liquids != null ? art.Liquids.For(pool.Kind) : null) ?? (theme.Liquids != null ? theme.Liquids.For(pool.Kind) : null);
                if (look != null)
                {
                    TerrainLiquidBuilder.BuildOutlinedPool(shape, pool.Surface, look, theme, PoolCellSize, transform, particleScale, low);
                    banked.Add(shape);
                }
                else
                {
                    TerrainLiquidBuilder.BuildPool(pool.Kind, theme, Cells(shape), PoolCellSize, transform, particleScale);
                }
            }

            if (banks != null && banks.HasBankSweeps)
            {
                LiquidBankBuilder.Build(banked, _terrain.Height, banks, instancer, transform, low);
            }
        }

        // Counter-clockwise (liquid on the left, as the liquid code expects) and lightly rounded.
        private static Vector2[] Shore(Vector2[] outline)
        {
            Vector2[] loop = (Vector2[])outline.Clone();
            if (LiquidContour.SignedArea(loop) < 0f)
            {
                System.Array.Reverse(loop);
            }

            return LiquidContour.Smooth(loop, 0, ShoreSmoothing, 0f);
        }

        private static List<Vector3> Cells(LiquidShape shape)
        {
            var cells = new List<Vector3>();
            Rect bounds = shape.Bounds;
            for (float z = bounds.yMin; z <= bounds.yMax; z += PoolCellSize)
            {
                for (float x = bounds.xMin; x <= bounds.xMax; x += PoolCellSize)
                {
                    if (shape.SignedDistance(x, z) > 0f)
                    {
                        cells.Add(new Vector3(x, 0f, z));
                    }
                }
            }

            return cells;
        }

        private void BuildLights(MenuDioramaArt art, MenuStageDefinition stage)
        {
            foreach (MenuDioramaLight entry in art.Lights)
            {
                VisualTheme theme = stage.Theme(entry.Theme);
                if (entry.Theme != MenuThemeSlot.None && theme == null)
                {
                    continue;
                }

                var go = new GameObject(entry.Name);
                go.transform.SetParent(transform, false);
                go.transform.position = entry.Position;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = theme != null ? theme.AccentColor : entry.Colour;
                light.range = entry.Range;
                light.shadows = LightShadows.None;
                go.AddComponent<LightFlicker>().Initialize(entry.Intensity, LightFlicker, LightFlickerSpeed);
            }
        }

        private Material Own(Material source)
        {
            var copy = new Material(source) { name = source.name + " (menu)" };
            _owned.Add(copy);
            return copy;
        }

        private void BuildCreatures(MenuStageDefinition stage, MenuDioramaArt art)
        {
            Vector3 mouth = NestCentre + Vector3.back * (NestRadius - MouthInset);
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

            if (art == null)
            {
                return;
            }

            foreach (MenuDioramaPool pool in art.Pools)
            {
                if (stage.Theme(pool.Theme) != null)
                {
                    PoolDweller(DwellerOf(stage, pool.Kind), pool, OriginOf(pool.Kind));
                }
            }
        }

        private static EnemyDefinition DwellerOf(MenuStageDefinition stage, TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return stage.WaterCreature;
                case TerrainKind.Lava: return stage.LavaCreature;
                case TerrainKind.Quicksand: return stage.SandCreature;
                case TerrainKind.Ice: return stage.IceCreature;
                case TerrainKind.Bog: return stage.BogCreature;
                default: return null;
            }
        }

        private static EnemyOrigin OriginOf(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Water: return EnemyOrigin.Water;
                case TerrainKind.Lava: return EnemyOrigin.Lava;
                case TerrainKind.Quicksand: return EnemyOrigin.Quicksand;
                case TerrainKind.Ice: return EnemyOrigin.Ice;
                case TerrainKind.Bog: return EnemyOrigin.Bog;
                default: return EnemyOrigin.Ground;
            }
        }

        // Out of the middle of the pool, up the bank, a stroll along it and back into the deep.
        private void PoolDweller(EnemyDefinition definition, MenuDioramaPool pool, EnemyOrigin origin)
        {
            if (definition == null)
            {
                return;
            }

            Vector3 centre = pool.Centre;
            float radius = pool.Radius;
            float bankDegrees = pool.BankDegrees;

            // A flyer born in the pool circles above its bank instead of crawling along it.
            if (definition.Flies)
            {
                var circuit = new List<Vector3> { centre };
                circuit.AddRange(Lap(centre, radius + PoolBankMargin, bankDegrees - PoolBankArcDegrees * 0.5f, PoolBankArcDegrees, BankPoints, false));
                circuit.Add(centre);
                Creature(definition, circuit.ToArray(), PoolCreatureSpeed * 1.5f, FlyerHeight, origin, PoolRestMin, PoolRestMax).Emerge(NextDelay());
                return;
            }

            Vector3 deep = new Vector3(centre.x, pool.Surface - 0.15f, centre.z);
            var route = new List<Vector3> { deep };
            route.AddRange(Lap(centre, radius + PoolBankMargin, bankDegrees - PoolBankArcDegrees * 0.5f, PoolBankArcDegrees, BankPoints, true));
            route.Add(new Vector3(centre.x, pool.Surface - 0.2f, centre.z));
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
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ParticleKit.UseCell(system, MarkerArt.ParticleCell.Mote);
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
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ParticleKit.UseCell(system, MarkerArt.ParticleCell.Spore);
            system.Play();
        }

        // Round 41: the procedural wind read as a loud sea; a recorded bed from the stage's data replaces it when set.
        private void BuildAmbience(MenuStageDefinition stage)
        {
            AudioClip bed = stage.Ambience != null ? stage.Ambience : ProceduralSfx.Ambience(stage.NestTheme.Ambience);
            _ambienceVolume = stage.Ambience != null ? stage.AmbienceVolume : AmbienceVolume;
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
                _ambience.volume = _ambienceVolume * _mix.AmbienceGain;
            }
        }
    }
}
