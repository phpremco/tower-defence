using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Towers
{
    /// <summary>
    /// The look of a road gate (a blockade): two pylons, a crossbeam and a humming energy field between them that
    /// holds the road. The field dims and flickers as the gate takes damage. When the gate falls it blows up: a
    /// flash, a fireball, a shower of sparks and flying debris, then the pylons keel over and the beam crashes
    /// down, and the wreck keeps smoking and sparking until it is rebuilt. With imported art (<see cref="GateArtDefinition"/>)
    /// the Blender gate of the gate's level replaces the pylons and beam (<see cref="GateArtRig"/>): it flinches on hits,
    /// sheds panels when badly hurt and sparks from its torn sockets, plays its collapse and leaves its wreck, and rises
    /// again on a rebuild; the field, the blast and the debris stay the same. Reacts to Tower events only.
    /// </summary>
    public sealed class GateView : MonoBehaviour
    {
        private const float BeamHeight = 1.08f;
        private const float FieldHeight = 0.9f;
        private const float FieldTop = 0.98f;
        private const float FieldInset = 0.22f;
        private const float FieldFloorHeight = 0.06f;
        private const float FieldFloorDepth = 0.6f;
        private const float FieldScrollSpeed = 0.6f;
        private const float FieldEmission = 1.4f;
        private const float FlickerBelow = 0.5f;
        private const float FlickerRecoverMargin = 0.15f;
        private const float FlickerSpeed = 23f;
        private const float CollapseDelay = 0.15f;
        private const float CollapseSeconds = 1.1f;
        private const float PylonTiltDegrees = 68f;
        private const float PylonSink = 0.12f;
        private const float BeamDrop = 0.95f;
        private const float BeamTiltDegrees = 34f;
        private const float BeamBounce = 0.06f;
        private const float FlashSeconds = 0.35f;
        private const float FlashStartScale = 0.4f;
        private const float FlashEndScale = 1.7f;
        private const float FlashEmission = 4f;
        private const int BurstSparks = 120;
        private const int BurstFire = 60;
        private const int DebrisCount = 12;
        private const float DebrisSeconds = 1.8f;
        private const float DebrisLaunch = 3.5f;
        private const float DebrisLift = 4f;
        private const float DebrisGravity = 9.5f;
        private const float DebrisSpin = 540f;
        private const float DebrisGround = 0.05f;
        private const float DebrisBounce = 0.3f;
        private const float WreckSmokeRate = 3f;
        private const float WreckSparkRate = 2f;
        private const float DamagedSparkInterval = 0.12f;
        private const int DamagedSparkBurst = 3;
        private static readonly Color WreckColor = new Color(0.1f, 0.1f, 0.11f);
        private static readonly Color SmokeColor = new Color(0.5f, 0.48f, 0.46f, 0.95f);
        private static readonly Color FireColor = new Color(1f, 0.5f, 0.15f);
        private static readonly Color FlashColor = new Color(1f, 0.85f, 0.6f);
        private static readonly Color EmberColor = new Color(1f, 0.45f, 0.1f);

        private Tower _tower;
        private float _width;
        private Color _colour;
        private Material[] _materials;
        private Material[] _wreckMaterials;
        private MeshRenderer[] _renderers;
        private Transform _leftPylon;
        private Transform _rightPylon;
        private Transform _beam;
        private Transform _field;
        private Material _fieldMaterial;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private ParticleSystem _fire;
        private Transform _flash;
        private Transform[] _debris;
        private Vector3[] _debrisSize;
        private Vector3[] _debrisVelocity;
        private Vector3[] _debrisSpin;
        private float _debrisAge = float.PositiveInfinity;
        private float _fieldOffset;
        private float _collapse = float.PositiveInfinity;
        private bool _wrecked;
        private float _leftTilt;
        private float _rightTilt;
        private GateArtRig _art;
        private float _lastFraction = 1f;
        private float _sparkClock;
        private GateDamageLook _damage;

        public void Initialize(Tower tower, float width, Color colour, GateArtDefinition art = null)
        {
            _tower = tower;
            _width = width;
            _colour = colour;
            if (art != null)
            {
                _art = new GateArtRig(transform, art, width);
                _art.ShowLevel(tower.LevelIndex);
                _tower.LevelChanged += OnLevelChanged;
            }

            _damage = _art != null ? new GateDamageLook(_art.DamagedBelow, _art.RecoverMargin) : new GateDamageLook(FlickerBelow, FlickerRecoverMargin);
            Build();
            _tower.StateChanged += OnStateChanged;
            OnStateChanged(tower);
        }

        private void OnDestroy()
        {
            if (_tower != null)
            {
                _tower.StateChanged -= OnStateChanged;
                _tower.LevelChanged -= OnLevelChanged;
            }

            _art?.Dispose();
        }

        private void OnLevelChanged(Tower tower) => _art.ShowLevel(tower.LevelIndex);

        private void Build()
        {
            _materials = ModelPalette.Create(_colour);
            Material wreck = SurfaceMaterials.Create(WreckColor, 0f, 0.15f);
            _wreckMaterials = new[] { wreck, wreck, wreck, wreck };

            float fieldWidth = _width - FieldInset;
            float fieldCentre = FieldTop - FieldHeight * 0.5f;
            if (_art == null)
            {
                Mesh pylon = StructureModelFactory.GatePylon();
                _leftPylon = Part("PylonL", pylon, new Vector3(-_width * 0.5f, 0f, 0f));
                _rightPylon = Part("PylonR", pylon, new Vector3(_width * 0.5f, 0f, 0f));
                _beam = Part("Beam", StructureModelFactory.GateBeam(_width + 0.3f), new Vector3(0f, BeamHeight, 0f));
                _renderers = new[] { _leftPylon.GetComponent<MeshRenderer>(), _rightPylon.GetComponent<MeshRenderer>(), _beam.GetComponent<MeshRenderer>() };
            }
            else
            {
                // The model brings its own pylons; the field fills the gap between their panels, centred on its socket.
                _renderers = new MeshRenderer[0];
                fieldWidth = _width * _art.FieldShare;
                fieldCentre = _art.FieldHeight;
            }

            // The field is a vertical sheet between the pylons plus a glowing strip on the road under it, so it
            // reads from the camera whichever way the road runs.
            _fieldMaterial = GlowDecal.ScanlineMaterial(_colour * FieldEmission);
            var field = new GameObject("Field");
            field.transform.SetParent(transform, false);
            _field = field.transform;
            FieldSheet("Sheet", new Vector3(0f, fieldCentre, 0f), Quaternion.identity, new Vector3(fieldWidth, FieldHeight, 1f));
            FieldSheet("Floor", new Vector3(0f, FieldFloorHeight, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(fieldWidth, FieldFloorDepth, 1f));

            _sparks = Burst("Sparks", Color.Lerp(_colour, Color.white, 0.5f) * 2f, 0.3f, 0.8f, 0.03f, 0.07f, 2.5f, 6f, 1.5f, 160, false);
            _fire = Burst("Fire", FireColor * 2.2f, 0.35f, 0.8f, 0.2f, 0.45f, 1f, 3.5f, -0.4f, 80, false);
            _smoke = Burst("Smoke", SmokeColor, 1.2f, 2.2f, 0.45f, 0.9f, 0.3f, 0.7f, -0.12f, 30, true);

            _flash = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, FlashColor, transform, new Vector3(0f, FieldTop * 0.6f, 0f), Vector3.one * FlashStartScale, FlashEmission).transform;
            _flash.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            _flash.gameObject.SetActive(false);
            BuildDebris();
        }

        private void FieldSheet(string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_field, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _fieldMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private Transform Part(string name, Mesh mesh, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = _materials;
            return go.transform;
        }

        // Chunks of plating that fly off when the gate blows: most dark, a few still glowing hot.
        private void BuildDebris()
        {
            _debris = new Transform[DebrisCount];
            _debrisSize = new Vector3[DebrisCount];
            _debrisVelocity = new Vector3[DebrisCount];
            _debrisSpin = new Vector3[DebrisCount];
            for (int i = 0; i < DebrisCount; i++)
            {
                bool hot = i % 4 == 0;
                GameObject chunk = DebrisChunks.Create(transform, hot, 1f, WreckColor, EmberColor, out _debrisSize[i]);
                _debris[i] = chunk.transform;
            }
        }

        private void OnStateChanged(Tower tower)
        {
            if (tower.IsWrecked && !_wrecked)
            {
                Explode();
            }
            else if (!tower.IsWrecked && _wrecked)
            {
                Restore();
            }

            float fraction = tower.Health.Fraction;
            if (_art != null && !tower.IsWrecked)
            {
                if (fraction < _lastFraction)
                {
                    _art.Hit();
                }
            }

            // One damaged state for the whole view (overlay, panels and the stuttering field), with a band between going in and
            // coming out so a repair trickled in against hits does not flip the look on every tick.
            _damage.Update(fraction, tower.IsWrecked);
            _art?.SetDamaged(_damage.IsDamaged);

            _lastFraction = fraction;
        }

        private void Explode()
        {
            _wrecked = true;
            _collapse = -CollapseDelay;
            // Positive roll leans a top towards -x, so the left pylon (at -x) rolls positive to fall outward.
            _leftTilt = PylonTiltDegrees * Random.Range(0.7f, 1f);
            _rightTilt = -PylonTiltDegrees * Random.Range(0.7f, 1f);
            _field.gameObject.SetActive(false);
            _flash.gameObject.SetActive(true);
            _flash.localScale = Vector3.one * FlashStartScale;
            _sparks.Emit(BurstSparks);
            _fire.Emit(BurstFire);
            LaunchDebris();
            ApplyMaterials();
            _art?.Collapse();
            SetLoop(_smoke, WreckSmokeRate);
            SetLoop(_sparks, WreckSparkRate);
        }

        private void LaunchDebris()
        {
            _debrisAge = 0f;
            for (int i = 0; i < _debris.Length; i++)
            {
                Transform chunk = _debris[i];
                chunk.gameObject.SetActive(true);
                chunk.localPosition = new Vector3(Random.Range(-_width * 0.5f, _width * 0.5f), Random.Range(0.3f, BeamHeight), 0f);
                chunk.localRotation = Random.rotation;
                chunk.localScale = _debrisSize[i];
                Vector3 outward = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                _debrisVelocity[i] = outward * (DebrisLaunch * Random.Range(0.4f, 1f)) + Vector3.up * (DebrisLift * Random.Range(0.5f, 1f));
                _debrisSpin[i] = Random.onUnitSphere * (DebrisSpin * Random.Range(0.5f, 1f));
            }
        }

        private void Restore()
        {
            _wrecked = false;
            _collapse = float.PositiveInfinity;
            _debrisAge = float.PositiveInfinity;
            if (_art != null)
            {
                _art.Rebuild();
            }
            else
            {
                _leftPylon.localPosition = new Vector3(-_width * 0.5f, 0f, 0f);
                _rightPylon.localPosition = new Vector3(_width * 0.5f, 0f, 0f);
                _leftPylon.localRotation = Quaternion.identity;
                _rightPylon.localRotation = Quaternion.identity;
                _beam.localPosition = new Vector3(0f, BeamHeight, 0f);
                _beam.localRotation = Quaternion.identity;
            }

            _field.gameObject.SetActive(true);
            _flash.gameObject.SetActive(false);
            ApplyMaterials();
            for (int i = 0; i < _debris.Length; i++)
            {
                _debris[i].gameObject.SetActive(false);
            }

            SetLoop(_smoke, 0f);
            SetLoop(_sparks, 0f);
        }

        private void ApplyMaterials()
        {
            Material[] set = _wrecked ? _wreckMaterials : _materials;
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sharedMaterials = set;
            }
        }

        private static void SetLoop(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = rate > 0f;
            emission.rateOverTime = rate;
        }

        private void Update()
        {
            _art?.Tick(Time.deltaTime);
            if (_wrecked)
            {
                Flash();
                Debris();
                if (_art == null)
                {
                    Collapse();
                }
                else
                {
                    // The model plays its own collapse; the clock still times the fireball.
                    _collapse += Time.deltaTime;
                }

                return;
            }

            DamagedSparks();

            _fieldOffset += FieldScrollSpeed * Time.deltaTime;
            GlowDecal.SetScroll(_fieldMaterial, new Vector2(0f, _fieldOffset));

            // The field weakens with the gate and starts to stutter once it is badly hurt.
            float fraction = _tower.Health.Fraction;
            float strength = 0.55f + 0.45f * fraction;
            if (_damage.IsDamaged)
            {
                float stutter = Mathf.PerlinNoise(Time.time * FlickerSpeed, 0.5f);
                strength *= stutter > 0.35f ? 1f : 0.25f;
            }

            GlowDecal.SetColour(_fieldMaterial, _colour * (FieldEmission * strength));
        }

        private void LateUpdate() => _art?.LateTick();

        // A badly hurt modelled gate spits sparks from its torn sockets now and then.
        private void DamagedSparks()
        {
            if (_art == null || !_art.IsDamaged || _art.Sparks.Length == 0)
            {
                return;
            }

            _sparkClock -= Time.deltaTime;
            if (_sparkClock > 0f)
            {
                return;
            }

            _sparkClock = DamagedSparkInterval * Random.Range(0.5f, 1.5f);
            var spark = new ParticleSystem.EmitParams { position = _art.Sparks[Random.Range(0, _art.Sparks.Length)].position, applyShapeToPosition = false };
            _sparks.Emit(spark, DamagedSparkBurst);
        }

        // The fireball: swells fast, then is gone.
        private void Flash()
        {
            if (!_flash.gameObject.activeSelf)
            {
                return;
            }

            float t = Mathf.Clamp01((_collapse + CollapseDelay) / FlashSeconds);
            _flash.localScale = Vector3.one * Mathf.Lerp(FlashStartScale, FlashEndScale, Mathf.Sqrt(t));
            if (t >= 1f)
            {
                _flash.gameObject.SetActive(false);
            }
        }

        private void Debris()
        {
            if (_debrisAge >= DebrisSeconds)
            {
                return;
            }

            _debrisAge += Time.deltaTime;
            float shrink = 1f - Mathf.Clamp01((_debrisAge - DebrisSeconds * 0.6f) / (DebrisSeconds * 0.4f));
            for (int i = 0; i < _debris.Length; i++)
            {
                Transform chunk = _debris[i];
                Vector3 velocity = _debrisVelocity[i];
                velocity.y -= DebrisGravity * Time.deltaTime;
                Vector3 position = chunk.position + velocity * Time.deltaTime;
                if (position.y < DebrisGround)
                {
                    position.y = DebrisGround;
                    velocity.y = -velocity.y * DebrisBounce;
                    velocity.x *= 0.5f;
                    velocity.z *= 0.5f;
                    _debrisSpin[i] *= 0.4f;
                }

                _debrisVelocity[i] = velocity;
                chunk.position = position;
                chunk.Rotate(_debrisSpin[i] * Time.deltaTime, Space.World);
                chunk.localScale = _debrisSize[i] * shrink;
                if (shrink <= 0f)
                {
                    chunk.gameObject.SetActive(false);
                }
            }
        }

        // After the blast the pylons keel over and the beam crashes down with a small bounce.
        private void Collapse()
        {
            if (_collapse >= CollapseSeconds)
            {
                return;
            }

            _collapse += Time.deltaTime;
            float t = Mathf.Clamp01(_collapse / CollapseSeconds);
            float fall = 1f - (1f - t) * (1f - t);
            float bounce = t > 0.8f ? Mathf.Sin((t - 0.8f) / 0.2f * Mathf.PI) * BeamBounce : 0f;
            _leftPylon.localRotation = Quaternion.Euler(0f, 0f, _leftTilt * fall);
            _rightPylon.localRotation = Quaternion.Euler(0f, 0f, _rightTilt * fall);
            _leftPylon.localPosition = new Vector3(-_width * 0.5f, -PylonSink * fall, 0f);
            _rightPylon.localPosition = new Vector3(_width * 0.5f, -PylonSink * fall, 0f);
            _beam.localPosition = new Vector3(0f, BeamHeight - BeamDrop * fall + bounce, 0f);
            _beam.localRotation = Quaternion.Euler(0f, 0f, BeamTiltDegrees * fall);
        }

        private ParticleSystem Burst(string name, Color color, float minLife, float maxLife, float minSize, float maxSize, float minSpeed, float maxSpeed, float gravity, int max, bool smoke)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(_width * 0.8f, 0.4f, 0.2f);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, gravity < 0f ? 2.5f : 0.2f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            go.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
            return smoke ? ParticleKit.UseSmoke(system) : ParticleKit.UseCell(system, MarkerArt.ParticleCell.Mote);
        }
    }
}
