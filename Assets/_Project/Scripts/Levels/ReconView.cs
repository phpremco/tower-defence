using System.Collections.Generic;
using Game.Core;
using Game.Platform;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// The look at the field before the fight: once the briefing is dismissed the camera shows the whole map with
    /// time held, soft sonar rings pulse out of every place the enemies come from, every flight route is lit, and a
    /// scout light runs each ground route from its gate to the base so the player sees where the waves will walk.
    /// Then the camera glides in to the hero at play zoom and time runs. Confirm or cancel skips it. Only shown
    /// when the session asks for it, like the briefing, so tests and tools are not held up.
    /// </summary>
    public sealed class ReconView : MonoBehaviour
    {
        private const float HoldSeconds = 4.5f;
        private const float GlideSeconds = 1.4f;
        private const float ScoutShare = 0.85f;
        private const int RingsPerGate = 3;
        private const float RingPeriod = 1.8f;
        private const float RingStartRadius = 0.4f;
        private const float RingEndRadius = 3.2f;
        private const float RingThickness = 0.09f;
        private const float RingHeight = 0.12f;
        private const float AirRingHeight = 3.4f;
        private const float RingAlpha = 0.8f;
        private const float RingEmission = 2.5f;
        private const int RingSegments = 40;
        private const float ScoutSize = 0.9f;
        private const float ScoutHeight = 0.6f;
        private const float ScoutEmission = 4f;
        private const float TrailWidth = 0.7f;
        private const float TrailSeconds = 60f;
        private static readonly Color GroundColor = new Color(1f, 0.45f, 0.25f);
        private static readonly Color AirColor = new Color(0.55f, 0.95f, 0.95f);

        private sealed class Scout
        {
            public Transform Light;
            public Vector3[] Route;
            public float[] Along;
            public float Length;
        }

        private sealed class Pulse
        {
            public Transform Ring;
            public Material Material;
            public Color Colour;
            public float Phase;
        }

        private enum Phase
        {
            Waiting,
            Holding,
            Gliding,
        }

        private LevelContext _context;
        private CameraPanZoom _pan;
        private TimeController _time;
        private IInputProvider _input;
        private Dictionary<int, GameObject> _routes;
        private readonly List<Pulse> _pulses = new List<Pulse>();
        private readonly List<Scout> _scouts = new List<Scout>();
        private Transform _root;
        private Mesh _ringMesh;
        private Phase _phase = Phase.Waiting;
        private float _elapsed;
        private Vector3 _glideFrom;
        private Vector3 _glideTo;

        public void Initialize(LevelContext context, CameraPanZoom pan, TimeController time, IInputProvider input, Dictionary<int, GameObject> routes)
        {
            _context = context;
            _pan = pan;
            _time = time;
            _input = input;
            _routes = routes;
            if (!context.ReconPending)
            {
                Destroy(this);
                return;
            }

            _input.Confirmed += Skip;
            _input.Cancelled += Skip;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Confirmed -= Skip;
                _input.Cancelled -= Skip;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            switch (_phase)
            {
                case Phase.Waiting:
                    if (!_context.BriefingPending)
                    {
                        Begin();
                    }

                    break;
                case Phase.Holding:
                    _elapsed += dt;
                    Animate();
                    if (_elapsed >= HoldSeconds)
                    {
                        StartGlide();
                    }

                    break;
                case Phase.Gliding:
                    _elapsed += dt;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / GlideSeconds));
                    // Zoom runs on a log scale, so the approach feels even from start to finish.
                    float zoom = Mathf.Exp(Mathf.Lerp(0f, Mathf.Log(_pan.PlayZoom), t));
                    _pan.SetView(Vector3.Lerp(_glideFrom, _glideTo, t), zoom);
                    if (t >= 1f)
                    {
                        Finish();
                    }

                    break;
            }
        }

        // The whole field, time held, rings over every gate, routes lit, scouts set off.
        private void Begin()
        {
            _phase = Phase.Holding;
            _elapsed = 0f;
            _time.Hold(true);
            _pan.Locked = true;
            _pan.SetView(Vector3.zero, 1f);
            _root = new GameObject("Recon").transform;

            LevelDefinition level = _context.Level;
            for (int index = 0; index < level.Paths.Length; index++)
            {
                EnemyPath path = level.Paths[index];
                if (path.Waypoints == null || path.Waypoints.Length < 2)
                {
                    continue;
                }

                Vector3[] route = level.Road.Route(index);
                Color colour = path.Flying ? AirColor : GroundColor;
                var centre = new Vector3(route[0].x, path.Flying ? AirRingHeight : RingHeight, route[0].z);
                for (int ring = 0; ring < RingsPerGate; ring++)
                {
                    _pulses.Add(BuildPulse(centre, colour, ring / (float)RingsPerGate));
                }

                if (path.Flying)
                {
                    if (_routes.TryGetValue(index, out GameObject flightRoute))
                    {
                        flightRoute.SetActive(true);
                    }
                }
                else
                {
                    _scouts.Add(BuildScout(route, colour));
                }
            }
        }

        // Rings swell out of the gate and fade, one after another, like a sonar ping; the scouts run their routes.
        private void Animate()
        {
            for (int i = 0; i < _pulses.Count; i++)
            {
                Pulse pulse = _pulses[i];
                float t = Mathf.Repeat(_elapsed / RingPeriod + pulse.Phase, 1f);
                float radius = Mathf.Lerp(RingStartRadius, RingEndRadius, Mathf.Sqrt(t));
                pulse.Ring.localScale = new Vector3(radius, 1f, radius);
                Color colour = pulse.Colour;
                colour.a = RingAlpha * (1f - t) * (1f - t);
                pulse.Material.color = colour;
            }

            float share = Mathf.Clamp01(_elapsed / (HoldSeconds * ScoutShare));
            for (int i = 0; i < _scouts.Count; i++)
            {
                Scout scout = _scouts[i];
                scout.Light.position = PointAlong(scout, share * scout.Length) + Vector3.up * ScoutHeight;
            }
        }

        private void StartGlide()
        {
            _phase = Phase.Gliding;
            _elapsed = 0f;
            _glideFrom = Vector3.zero;
            _glideTo = _context.Hero != null ? _context.Hero.transform.position : Vector3.zero;
            _glideTo.y = 0f;
        }

        private void Skip()
        {
            if (_phase == Phase.Holding)
            {
                StartGlide();
            }
            else if (_phase == Phase.Gliding)
            {
                _elapsed = GlideSeconds;
            }
        }

        // Back to the game: markers gone, the routes as the wave wants them, the player at the controls.
        private void Finish()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            _context.RoutesChanged.Raise();
            _pan.Locked = false;
            _time.Hold(false);
            _context.ReconPending = false;
            Destroy(this);
        }

        // One thin flat ring of unit radius, scaled out as it pulses, with its own see-through material to fade.
        private Pulse BuildPulse(Vector3 centre, Color colour, float phase)
        {
            if (_ringMesh == null)
            {
                var builder = new PropMeshBuilder(1);
                builder.AddTorus(Vector3.zero, Vector3.up, 1f, RingThickness, RingSegments, 6, 0);
                _ringMesh = builder.ToMesh("ReconRing");
            }

            var go = new GameObject("SpawnPulse");
            go.transform.SetParent(_root, false);
            go.transform.position = centre;
            go.AddComponent<MeshFilter>().sharedMesh = _ringMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(PlaceholderVisuals.TransparentMaterialFor(colour, RingAlpha, RingEmission));
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return new Pulse { Ring = go.transform, Material = material, Colour = colour, Phase = phase };
        }

        // A bright light that runs the route and leaves it lit behind it for as long as the recon lasts.
        private Scout BuildScout(Vector3[] route, Color colour)
        {
            GameObject light = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(colour, Color.white, 0.4f), _root, route[0] + Vector3.up * ScoutHeight, Vector3.one * ScoutSize, ScoutEmission);
            light.name = "Scout";
            light.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var trail = light.AddComponent<TrailRenderer>();
            trail.time = TrailSeconds;
            trail.widthMultiplier = TrailWidth;
            trail.minVertexDistance = 0.1f;
            trail.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(colour, 0f), new GradientColorKey(colour, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 1f) });
            trail.colorGradient = gradient;

            var scout = new Scout { Light = light.transform, Route = route, Along = new float[route.Length] };
            for (int i = 1; i < route.Length; i++)
            {
                scout.Along[i] = scout.Along[i - 1] + Vector3.Distance(route[i - 1], route[i]);
            }

            scout.Length = scout.Along[route.Length - 1];
            return scout;
        }

        private static Vector3 PointAlong(Scout scout, float distance)
        {
            for (int i = 1; i < scout.Route.Length; i++)
            {
                if (distance <= scout.Along[i])
                {
                    float segment = scout.Along[i] - scout.Along[i - 1];
                    float t = segment > 0f ? (distance - scout.Along[i - 1]) / segment : 1f;
                    return Vector3.Lerp(scout.Route[i - 1], scout.Route[i], t);
                }
            }

            return scout.Route[scout.Route.Length - 1];
        }
    }
}
