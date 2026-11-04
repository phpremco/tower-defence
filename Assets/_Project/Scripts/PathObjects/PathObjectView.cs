using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.PathObjects
{
    /// <summary>
    /// Look of a path object: its imported (Blender) model when it has one (<see cref="PathObjectArtRig"/>), else a
    /// procedural model or a placeholder chosen by kind: a dark disc with a red light, a see-through field or a glowing
    /// ball. A slow field keeps its see-through reach; a lava ball also throws embers behind it and lights the road.
    /// </summary>
    [RequireComponent(typeof(PathObject))]
    public sealed class PathObjectView : MonoBehaviour
    {
        private const float MineEmission = 1.5f;
        private const float BallEmission = 2.5f;
        private const float FieldAlpha = 0.22f;
        // README round 25b: the reach lies 0.05 over the ground.
        private const float FieldHeight = 0.05f;
        private const float FieldEmission = 1.2f;
        private const float PulseSpeed = 3f;
        private const float LavaBallSpin = 120f;
        private const float MineLightSize = 0.16f;
        private const float EmberRatePerUnit = 28f;
        private const float EmberBrightness = 2.2f;
        private const float EmberLifeMin = 0.4f;
        private const float EmberLifeMax = 0.9f;
        private const float EmberSizeMin = 0.03f;
        private const float EmberSizeMax = 0.08f;
        private const float EmberGravity = 0.6f;
        private const float EmberSpread = 0.3f;
        private const int EmberMax = 80;
        private const float BallLightHeight = 0.5f;
        private const float BallLightRange = 2.6f;
        private const float BallLightIntensity = 1.8f;
        private const float BallLightFlicker = 0.3f;
        private const float BallLightFlickerSpeed = 4f;
        private const float ChronoOuterShare = 0.8f;
        private const float ChronoInnerShare = 0.55f;
        private const float ChronoRingHeight = 0.22f;
        private const float ChronoRingThickness = 0.035f;
        private const float ChronoSpin = 40f;
        private const float ChronoTilt = 8f;
        private const int ChronoMotes = 40;
        private const float ChronoMoteRate = 9f;
        private static readonly Color MineBase = new Color(0.12f, 0.13f, 0.16f);

        private Transform _light;
        private bool _paintedField;
        private PathObject _object;
        private PathObjectArtRig _art;

        public void Initialize(PathObject pathObject)
        {
            _object = pathObject;
            PathObjectDefinition definition = pathObject.Definition;
            if (definition.Art != null)
            {
                BuildArt(definition);
                return;
            }

            if (definition.Look != PathObjectLook.None)
            {
                BuildLook(definition);
                return;
            }

            switch (definition.Kind)
            {
                case PathObjectKind.Mine:
                    PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, MineBase, transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.5f, 0.05f, 0.5f));
                    _light = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, definition.Color, transform, new Vector3(0f, 0.16f, 0f), Vector3.one * MineLightSize, MineEmission).transform;
                    break;
                case PathObjectKind.SlowField:
                    GameObject field = PlaceholderVisuals.CreatePrimitive(
                        PrimitiveType.Cylinder, definition.Color, transform, new Vector3(0f, 0.05f, 0f), new Vector3(definition.Radius * 2f, 0.01f, definition.Radius * 2f));
                    field.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(definition.Color, FieldAlpha, FieldEmission);
                    _light = field.transform;
                    break;
                default:
                    PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, definition.Color, transform, new Vector3(0f, 0.35f, 0f), Vector3.one * Mathf.Max(0.5f, definition.Radius * 0.8f), BallEmission);
                    break;
            }
        }

        // The procedural model; a slow field also gets its translucent disc so its reach stays readable.
        private void BuildLook(PathObjectDefinition definition)
        {
            Mesh mesh = PathObjectModels.MeshFor(definition.Look);

            var model = new GameObject("Model");
            model.transform.SetParent(transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterials = ModelPalette.Create(definition.Color);

            bool field = definition.Kind == PathObjectKind.SlowField;
            if (field)
            {
                _light = BuildFieldDisc(definition);
            }

            if (definition.Look == PathObjectLook.Chrono)
            {
                BuildChrono(definition);
            }

            if (definition.Look == PathObjectLook.LavaBall)
            {
                model.AddComponent<Spinner>().Initialize(LavaBallSpin);
                BuildEmbers(definition.Color);
                BuildGlowLight(definition.Color);
            }
        }

        // The imported model brings its own rings, cap and shell; the view keeps what is light, not mesh: a slow field's
        // reach on the ground and its rising motes, a lava ball's embers and glow.
        private void BuildArt(PathObjectDefinition definition)
        {
            _art = new PathObjectArtRig(transform, definition);
            if (definition.Kind == PathObjectKind.SlowField)
            {
                _light = BuildFieldDisc(definition);
            }

            if (definition.Look == PathObjectLook.Chrono)
            {
                ParticleSystem motes = ParticleKit.UseCell(ParticleKit.Create(transform, "ChronoMotes", new Vector3(0f, 0.1f, 0f), Color.Lerp(definition.Color, Color.white, 0.6f) * 1.6f, 2.2f, 3.2f, 0.03f, 0.06f, 0.05f, 0.15f, -0.02f, ChronoMotes, definition.Radius * ChronoOuterShare), MarkerArt.ParticleCell.ChronoMote);
                ParticleKit.SetRate(motes, ChronoMoteRate);
            }

            if (definition.Look == PathObjectLook.LavaBall)
            {
                BuildEmbers(definition.Color);
                BuildGlowLight(definition.Color);
            }
        }

        // Two glowing rings turning against each other over the field, and motes that rise in slow motion: time runs
        // slow in here. The rings are their own meshes so a later model can replace them without touching the field.
        private void BuildChrono(PathObjectDefinition definition)
        {
            Ring("ChronoRingOuter", definition.Color, definition.Radius * ChronoOuterShare, ChronoRingHeight, ChronoSpin, ChronoTilt);
            Ring("ChronoRingInner", Color.Lerp(definition.Color, Color.white, 0.5f), definition.Radius * ChronoInnerShare, ChronoRingHeight + 0.12f, -ChronoSpin * 1.6f, -ChronoTilt);
            ParticleSystem motes = ParticleKit.UseCell(ParticleKit.Create(transform, "ChronoMotes", new Vector3(0f, 0.1f, 0f), Color.Lerp(definition.Color, Color.white, 0.6f) * 1.6f, 2.2f, 3.2f, 0.03f, 0.06f, 0.05f, 0.15f, -0.02f, ChronoMotes, definition.Radius * ChronoOuterShare), MarkerArt.ParticleCell.ChronoMote);
            ParticleKit.SetRate(motes, ChronoMoteRate);
        }

        private void Ring(string objectName, Color color, float radius, float height, float spin, float tilt)
        {
            var builder = new PropMeshBuilder();
            builder.AddTorus(Vector3.zero, Vector3.up, radius, ChronoRingThickness, 40, 6, ModelPalette.Glow);
            var pivot = new GameObject(objectName);
            pivot.transform.SetParent(transform, false);
            pivot.transform.localPosition = new Vector3(0f, height, 0f);
            pivot.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            var ring = new GameObject("Ring");
            ring.transform.SetParent(pivot.transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh(objectName);
            var renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = ModelPalette.Create(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            ring.AddComponent<Spinner>().Initialize(spin);
        }

        // Embers spill from the ball as it moves: emission per distance, so a parked ball only smoulders.
        // The field's reach on the ground: the painted ring with ripples running outwards (README round 25b), or a plain disc.
        private Transform BuildFieldDisc(PathObjectDefinition definition)
        {
            MarkerArt art = MarkerArt.Active;
            MeshRenderer painted = art != null ? art.Place(MarkerKind.SlowField, transform, "FieldReach") : null;
            if (painted != null)
            {
                painted.transform.localPosition = new Vector3(0f, FieldHeight, 0f);
                painted.transform.localScale = new Vector3(definition.Radius, 1f, definition.Radius);
                MarkerArt.SetTint(painted, new MaterialPropertyBlock(), definition.Color);
                _paintedField = true;
                return painted.transform;
            }

            GameObject disc = PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cylinder, definition.Color, transform, new Vector3(0f, FieldHeight, 0f), new Vector3(definition.Radius * 2f, 0.01f, definition.Radius * 2f));
            disc.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(definition.Color, FieldAlpha, FieldEmission);
            return disc.transform;
        }

        private void BuildEmbers(Color color)
        {
            var go = new GameObject("Embers");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(EmberLifeMin, EmberLifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(EmberSizeMin, EmberSizeMax);
            Color glow = color * EmberBrightness;
            glow.a = 1f;
            main.startColor = glow;
            main.gravityModifier = EmberGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = EmberMax;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 4f;
            emission.rateOverDistance = EmberRatePerUnit;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = EmberSpread;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            ParticleKit.UseCell(system, MarkerArt.ParticleCell.Ember);
            system.Play();
        }

        private void BuildGlowLight(Color color)
        {
            var go = new GameObject("Glow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, BallLightHeight, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = BallLightRange;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>().Initialize(BallLightIntensity, BallLightFlicker, BallLightFlickerSpeed);
        }

        private void LateUpdate() => _art?.LateTick();

        private void OnDestroy() => _art?.Dispose();

        // The light of a mine and the surface of a field breathe slowly, so placed objects read as active.
        private void Update()
        {
            if (_object == null)
            {
                return;
            }

            _art?.Tick(transform.position, Time.deltaTime);
            if (_light == null)
            {
                return;
            }

            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * PulseSpeed);
            if (_object.Definition.Kind == PathObjectKind.Mine)
            {
                _light.localScale = Vector3.one * MineLightSize * pulse;
            }
            else
            {
                float radius = _object.Definition.Radius * pulse;
                _light.localScale = _paintedField ? new Vector3(radius, 1f, radius) : new Vector3(radius * 2f, 0.01f, radius * 2f);
            }
        }
    }
}
