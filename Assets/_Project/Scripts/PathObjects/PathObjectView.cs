using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.PathObjects
{
    /// <summary>
    /// Placeholder look of a path object, chosen by kind: a dark disc with a red light, a see-through field or a
    /// glowing ball. A lava ball also throws embers behind it and lights the road it rolls over.
    /// </summary>
    [RequireComponent(typeof(PathObject))]
    public sealed class PathObjectView : MonoBehaviour
    {
        private const float MineEmission = 1.5f;
        private const float BallEmission = 2.5f;
        private const float FieldAlpha = 0.22f;
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
        private static readonly Color MineBase = new Color(0.12f, 0.13f, 0.16f);

        private Transform _light;
        private PathObject _object;

        public void Initialize(PathObject pathObject)
        {
            _object = pathObject;
            PathObjectDefinition definition = pathObject.Definition;
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
                GameObject disc = PlaceholderVisuals.CreatePrimitive(
                    PrimitiveType.Cylinder, definition.Color, transform, new Vector3(0f, 0.05f, 0f), new Vector3(definition.Radius * 2f, 0.01f, definition.Radius * 2f));
                disc.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(definition.Color, FieldAlpha, FieldEmission);
                _light = disc.transform;
            }

            if (definition.Look == PathObjectLook.LavaBall)
            {
                model.AddComponent<Spinner>().Initialize(LavaBallSpin);
                BuildEmbers(definition.Color);
                BuildGlowLight(definition.Color);
            }
        }

        // Embers spill from the ball as it moves: emission per distance, so a parked ball only smoulders.
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

        // The light of a mine and the surface of a field breathe slowly, so placed objects read as active.
        private void Update()
        {
            if (_light == null || _object == null)
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
                float diameter = _object.Definition.Radius * 2f * pulse;
                _light.localScale = new Vector3(diameter, 0.01f, diameter);
            }
        }
    }
}
