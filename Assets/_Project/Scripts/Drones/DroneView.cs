using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// The look of a drone: its own skinned model, rotors that spin on their pivots, a nose that turns the way it
    /// flies and dips with speed, a gentle bob, blinking navigation lights, thrusters that flare and trail when it
    /// moves, a light beam and a pool of light on whatever it works on, welding sparks for the medic, a spinning
    /// drill with dust for the hauler, a cargo pod that lights up when loaded, muzzle flashes when it fires and a
    /// floating "+money" when it delivers. With imported art (<see cref="DroneArtRig"/>) the Blender drone replaces the
    /// procedural one: its Idle, Fly and Work cycles follow the drone, its rotors spin at speed, and the beam, sparks,
    /// flashes and trail leave from its sockets. Reads the drone, never drives it.
    /// </summary>
    [RequireComponent(typeof(Drone))]
    public sealed class DroneView : MonoBehaviour
    {
        private const float BeamThickness = 0.05f;
        private const float BeamEmission = 3f;
        private const float BeamFlicker = 0.4f;
        private const float RotorSpeed = 1500f;
        private const float TurnSpeed = 8f;
        private const float BobHeight = 0.06f;
        private const float BobSpeed = 2.3f;
        private const float BankDegrees = 14f;
        private const float PitchDegrees = 7f;
        private const float AttitudeFollow = 5f;
        private const float FlashSeconds = 0.06f;
        private const float FlashSize = 0.12f;
        private const float FlashEmission = 6f;
        private const float StrobePeriod = 1.3f;
        private const float StrobeOn = 0.08f;
        private const float BeaconPeriod = 0.9f;
        private const float ThrusterIdle = 1.5f;
        private const float ThrusterFull = 7f;
        private const float TrailSeconds = 0.35f;
        private const float TrailWidth = 0.05f;
        private const float TrailMinPace = 0.35f;
        private const float DrillSpeed = 900f;
        private const float SparkRate = 45f;
        private const float DustRate = 25f;
        private const float PoolRadius = 0.55f;
        private const float PoolLift = 0.03f;
        private const float FullTurn = 360f;

        // Above this share of its top speed the imported drone plays its Fly cycle instead of Idle.
        private const float MovingPace = 0.2f;

        // The flash sits just past the mouth of a modelled gun, along the muzzle bone's +Y.
        private const float ArtFlashForward = 0.04f;
        private const float MuzzleWhiteness = 0.6f;
        private static readonly Color MoneyColor = new Color(1f, 0.9f, 0.4f);
        private static readonly Color SparkColor = new Color(1f, 0.85f, 0.5f);
        private static readonly Color DustColor = new Color(0.55f, 0.45f, 0.32f, 0.7f);

        private Drone _drone;
        private DroneModelBuilder.Rig _rig;
        private DroneArtRig _art;
        private Transform _model;
        private float _rotorAngle;
        private Transform _beam;
        private Transform _pool;
        private Transform[] _flashes;

        // The painted flashes of the drone's weapon at its gun mouths, or null: then the glowing spheres stand in.
        private MuzzleFlashView[] _painted;
        private Transform[] _mouths;
        private Vector3 _mouthAxis;
        private ParticleSystem _sparks;
        private ParticleSystem _dust;
        private TrailRenderer[] _trails;
        private Material[] _cargoLoaded;
        private Material[] _cargoEmpty;
        private float _flashRemaining;
        private float _bobPhase;
        private float _lightPhase;
        private float _pitch;
        private Quaternion _facing = Quaternion.identity;

        public void Initialize(Drone drone)
        {
            _drone = drone;
            _bobPhase = Random.value * Mathf.PI * 2f;
            _lightPhase = Random.value * StrobePeriod;

            Color color = drone.Definition.Color;
            _beam = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, null, Vector3.zero, Vector3.one, BeamEmission).transform;
            _beam.SetParent(transform.parent, true);
            _beam.gameObject.SetActive(false);

            _pool = GlowDecal.PlacePool(transform.parent, color, PoolRadius).transform;
            _pool.gameObject.SetActive(false);

            if (drone.Definition.Art != null)
            {
                InitializeArt(drone.Definition, color);
            }
            else
            {
                InitializeProcedural(drone.Definition, color);
            }

            _drone.Fired += OnFired;
            _drone.Delivered += OnDelivered;
        }

        // The imported model brings its own rotors, lights and pod; the view adds the flashes, the welding sparks at the
        // emitter and the thruster trail at its sockets.
        private void InitializeArt(DroneDefinition definition, Color color)
        {
            _art = DroneArtRig.Create(transform, definition.Art);
            _model = _art.Holder;
            _flashes = PaintMuzzles(definition, color, _art.Muzzles, Vector3.up) ? new Transform[0] : new Transform[_art.Muzzles.Length];
            for (int i = 0; i < _flashes.Length; i++)
            {
                GameObject flash = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(color, Color.white, 0.6f), _art.Muzzles[i], Vector3.up * ArtFlashForward, Vector3.one * FlashSize, FlashEmission);
                flash.SetActive(false);
                _flashes[i] = flash.transform;
            }

            switch (definition.Kind)
            {
                case DroneKind.Repair when _art.Tool != null:
                    _sparks = ParticleKit.Create(_art.Tool, "Sparks", Vector3.zero, SparkColor, 0.15f, 0.4f, 0.015f, 0.035f, 1.5f, 3.5f, 1.2f, 60, 0.02f);
                    break;
                case DroneKind.Harvest:
                    _dust = ParticleKit.Create(transform.parent, "Dust", Vector3.zero, DustColor, 0.6f, 1.2f, 0.08f, 0.18f, 0.3f, 0.9f, -0.03f, 40, 0.25f);
                    break;
            }

            _trails = _art.Thruster != null ? new[] { CreateTrail(_art.Thruster, color) } : new TrailRenderer[0];
        }

        private void InitializeProcedural(DroneDefinition definition, Color color)
        {
            _rig = DroneModelBuilder.Build(transform, definition);
            _model = _rig.Root.transform;
            _flashes = PaintMuzzles(definition, color, ProceduralMouths(), Vector3.forward) ? new Transform[0] : new Transform[_rig.Muzzles.Length];
            for (int i = 0; i < _flashes.Length; i++)
            {
                GameObject flash = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(color, Color.white, 0.6f), _rig.Root.transform, _rig.Muzzles[i], Vector3.one * FlashSize, FlashEmission);
                flash.SetActive(false);
                _flashes[i] = flash.transform;
            }

            if (_rig.Cargo != null)
            {
                _cargoEmpty = _rig.Cargo.sharedMaterials;
                _cargoLoaded = (Material[])_cargoEmpty.Clone();
                _cargoEmpty[ModelPalette.Glow] = _rig.Materials[ModelPalette.Dark];
                _rig.Cargo.sharedMaterials = _cargoEmpty;
            }

            switch (definition.Kind)
            {
                case DroneKind.Repair:
                    _sparks = ParticleKit.Create(_rig.Root.transform, "Sparks", _rig.Tool, SparkColor, 0.15f, 0.4f, 0.015f, 0.035f, 1.5f, 3.5f, 1.2f, 60, 0.02f);
                    break;
                case DroneKind.Harvest:
                    _dust = ParticleKit.Create(transform.parent, "Dust", Vector3.zero, DustColor, 0.6f, 1.2f, 0.08f, 0.18f, 0.3f, 0.9f, -0.03f, 40, 0.25f);
                    break;
            }

            _trails = new TrailRenderer[_rig.Thrusters.Length];
            for (int i = 0; i < _trails.Length; i++)
            {
                _trails[i] = CreateTrail(_rig.Thrusters[i], color);
            }
        }

        // The drone's weapon's painted flash at every gun mouth, when the effect library has one for its projectile style.
        private bool PaintMuzzles(DroneDefinition definition, Color color, Transform[] mouths, Vector3 axis)
        {
            if (mouths.Length == 0 || !Services.TryGet(out VfxLibrary library))
            {
                return false;
            }

            WeaponVfxDefinition weapon = library.WeaponFor(definition.ProjectileStyle);
            var views = new MuzzleFlashView[mouths.Length];
            for (int i = 0; i < views.Length; i++)
            {
                var view = new GameObject("MuzzleFlash").AddComponent<MuzzleFlashView>();
                view.transform.SetParent(transform, false);
                if (!view.Initialize(weapon, Color.Lerp(color, Color.white, MuzzleWhiteness), ModelQuality.IsLow, Camera.main))
                {
                    Destroy(view.gameObject);
                    return false;
                }

                views[i] = view;
            }

            _painted = views;
            _mouths = mouths;
            _mouthAxis = axis;
            return true;
        }

        // The procedural rig names its gun mouths as points under its root; the flash needs a node to follow.
        private Transform[] ProceduralMouths()
        {
            var mouths = new Transform[_rig.Muzzles.Length];
            for (int i = 0; i < mouths.Length; i++)
            {
                mouths[i] = new GameObject("Mouth").transform;
                mouths[i].SetParent(_rig.Root.transform, false);
                mouths[i].localPosition = _rig.Muzzles[i];
            }

            return mouths;
        }

        private static TrailRenderer CreateTrail(Transform thruster, Color color)
        {
            var trail = thruster.gameObject.AddComponent<TrailRenderer>();
            trail.time = TrailSeconds;
            trail.startWidth = TrailWidth;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.material = PlaceholderVisuals.TransparentMaterialFor(color, 0.6f, 2f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.emitting = false;
            return trail;
        }

        private void OnDestroy()
        {
            if (_beam != null)
            {
                Destroy(_beam.gameObject);
            }

            if (_pool != null)
            {
                Destroy(_pool.gameObject);
            }

            if (_dust != null)
            {
                Destroy(_dust.gameObject);
            }

            if (_drone != null)
            {
                _drone.Fired -= OnFired;
                _drone.Delivered -= OnDelivered;
            }

            _art?.Dispose();
        }

        private void OnFired(Vector3 target)
        {
            _flashRemaining = FlashSeconds;
            foreach (Transform flash in _flashes)
            {
                flash.gameObject.SetActive(true);
            }

            if (_painted != null)
            {
                for (int i = 0; i < _painted.Length; i++)
                {
                    _painted[i].Flash(_mouths[i], _mouthAxis);
                }
            }
        }

        private void OnDelivered(int value) => MoneyPopup.Show(transform.parent, transform.position, value, MoneyColor);

        private void Update()
        {
            if (_drone == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            Attitude(dt);
            if (_art != null)
            {
                _rotorAngle = (_rotorAngle + RotorSpeed * dt) % FullTurn;
                ArtThruster();
                _art.SetLoaded(_drone.IsCarrying);
            }
            else
            {
                foreach (Transform rotor in _rig.Rotors)
                {
                    rotor.Rotate(0f, RotorSpeed * dt, 0f, Space.Self);
                }

                Lights(dt);
                Thrusters();
                Cargo();
            }

            Flashes(dt);
            bool working = Work();
            _art?.Animate(working, _drone.Pace > MovingPace, dt);
        }

        // The clips turn the rotors slowly; at speed they spin far faster, set after the clips have posed the rig.
        private void LateUpdate() => _art?.SpinRotors(_rotorAngle);

        // Turn towards the heading, lean into the turn and dip the nose with speed, like a real quadcopter.
        private void Attitude(float dt)
        {
            Quaternion wanted = Quaternion.LookRotation(_drone.Heading, Vector3.up);
            float turn = Quaternion.Angle(_facing, wanted);
            _facing = Quaternion.Slerp(_facing, wanted, TurnSpeed * dt);
            // Only a drone under way leans into a turn: one turning on the spot (hovering at its work) stays level.
            float bank = Mathf.Clamp(turn, -BankDegrees, BankDegrees) * Mathf.Sign(Vector3.Dot(Vector3.Cross(_facing * Vector3.forward, wanted * Vector3.forward), Vector3.up)) * Mathf.Clamp01(_drone.Pace);
            _pitch = Mathf.Lerp(_pitch, _drone.Pace * PitchDegrees, AttitudeFollow * dt);
            _bobPhase += BobSpeed * dt;
            _model.localRotation = _facing * Quaternion.Euler(_pitch, 0f, -bank);
            _model.localPosition = Vector3.up * (Mathf.Sin(_bobPhase) * BobHeight);
        }

        // Port and starboard burn steadily, the strobe flashes, the beacon blinks at an easy pace.
        private void Lights(float dt)
        {
            _lightPhase += dt;
            bool strobe = _lightPhase % StrobePeriod < StrobeOn;
            bool beacon = _lightPhase % BeaconPeriod < BeaconPeriod * 0.5f;
            for (int i = 0; i < _rig.Lights.Length; i++)
            {
                bool on = true;
                switch (_rig.LightKinds[i])
                {
                    case DroneModelFactory.LightKind.Strobe:
                        on = strobe;
                        break;
                    case DroneModelFactory.LightKind.Beacon:
                        on = beacon;
                        break;
                }

                if (_rig.Lights[i].activeSelf != on)
                {
                    _rig.Lights[i].SetActive(on);
                }
            }
        }

        // Exhausts flare with speed and only leave a trail at a real pace.
        private void Thrusters()
        {
            float pace = _drone.Pace;
            Color glow = _drone.Definition.Color * Mathf.Lerp(ThrusterIdle, ThrusterFull, pace);
            for (int i = 0; i < _rig.ThrusterMaterials.Length; i++)
            {
                _rig.ThrusterMaterials[i].SetColor("_EmissionColor", glow);
                _rig.Thrusters[i].localScale = new Vector3(1f, 1f, 1f + pace * 1.5f) * 0.05f;
                _trails[i].emitting = pace > TrailMinPace && _drone.IsAlive;
            }
        }

        private void Cargo()
        {
            if (_rig.Cargo != null)
            {
                _rig.Cargo.sharedMaterials = _drone.IsCarrying ? _cargoLoaded : _cargoEmpty;
            }
        }

        private void Flashes(float dt)
        {
            if (_flashRemaining <= 0f)
            {
                return;
            }

            _flashRemaining -= dt;
            if (_flashRemaining <= 0f)
            {
                foreach (Transform flash in _flashes)
                {
                    flash.gameObject.SetActive(false);
                }
            }
        }

        // The beam and the pool of light on the work point; sparks while welding, a spinning drill and dust while mining.
        // Returns whether the drone is at its job (welding, drilling or shooting), for the model's Work cycle.
        private bool Work()
        {
            bool working = _drone.IsAlive && _drone.WorkPoint.HasValue;
            bool repairing = working && _drone.Activity == DroneActivity.Repairing;
            bool mining = working && _drone.Activity == DroneActivity.Mining;
            _beam.gameObject.SetActive(working);
            _pool.gameObject.SetActive(working);
            if (_sparks != null)
            {
                ParticleKit.SetRate(_sparks, repairing ? SparkRate : 0f);
            }

            if (_dust != null)
            {
                ParticleKit.SetRate(_dust, mining ? DustRate : 0f);
            }

            if (_rig != null && _rig.Drill != null && mining)
            {
                _rig.Drill.Rotate(0f, 0f, DrillSpeed * Time.deltaTime, Space.Self);
            }

            bool fighting = _drone.IsAlive && _drone.Activity == DroneActivity.Fighting;
            if (!working)
            {
                return fighting;
            }

            Vector3 from = _art != null && _art.Tool != null ? _art.Tool.position : transform.position;
            Vector3 to = _drone.WorkPoint.Value;
            Vector3 delta = to - from;
            float thickness = BeamThickness * (repairing ? 1f + BeamFlicker * (Random.value - 0.5f) : 1f);
            _beam.position = from + delta * 0.5f;
            _beam.rotation = Quaternion.LookRotation(delta);
            _beam.localScale = new Vector3(thickness, thickness, delta.magnitude);
            _pool.position = to + Vector3.up * PoolLift;
            if (_dust != null)
            {
                _dust.transform.position = to;
            }

            return true;
        }

        // The imported drone has one exhaust: its trail streams only at a real pace.
        private void ArtThruster()
        {
            bool streaming = _drone.Pace > TrailMinPace && _drone.IsAlive;
            for (int i = 0; i < _trails.Length; i++)
            {
                _trails[i].emitting = streaming;
            }
        }
    }
}
