using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Drones;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The imported (Blender) colony base on the field: its modules with the radar dish turning, the core crystal breathing
    /// and the mast beacon blinking, the shuttle on its round (<see cref="BaseShuttleView"/>), the drone bays
    /// (<see cref="BaseDockView"/>), smoke from its sockets and the damage overlay once it is badly hurt, and the wreck that
    /// replaces it in the flash when it falls (<see cref="BaseView"/> plays the blast itself). Reacts to the base's health
    /// and its drones; changes nothing.
    /// </summary>
    public sealed class BaseArtView : MonoBehaviour
    {
        private const string SmokePrefix = "Smoke_";
        private const string SparkPrefix = "Spark";
        private static readonly string[] ShuttleLiftSockets = { "Socket_ShuttleLift_L", "Socket_ShuttleLift_R" };
        private const float CrystalPulseDepth = 0.3f;
        private const float CrystalPulseSpeed = 1.6f;
        private const float SmokeRate = 9f;
        private const float SparkInterval = 0.25f;
        private const int SparkBurst = 4;
        private static readonly Color SmokeColor = new Color(0.2f, 0.19f, 0.18f, 0.35f);
        private static readonly Color SparkColor = new Color(1f, 0.8f, 0.4f) * 2f;

        // Where the wreck keeps burning, as shares of its footprint (x across, y towards the road), and how it flickers.
        private static readonly Vector2[] WreckFireSpots = { new Vector2(0.35f, 0.55f), new Vector2(0.7f, 0.3f), new Vector2(0.25f, 0.2f) };
        private const float WreckFireLift = 0.5f;
        private const float EmberGlow = 2.5f;
        private const float FlickerSpeed = 3.2f;
        private const float FlickerDepth = 0.35f;
        private static readonly Color WreckSmokeColor = new Color(0.22f, 0.18f, 0.16f, 0.4f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");

        private BaseArtDefinition _art;
        private ArtModel _model;
        private ArtModel _damage;
        private ArtModel _wreck;
        private Material _crystal;
        private ParticleSystem[] _smoke = new ParticleSystem[0];
        private ParticleSystem _sparks;
        private Transform[] _sparkSockets = new Transform[0];
        private Health _health;
        private bool _damaged;
        private float _sparkClock;
        private Material _beacon;
        private EmissionBlink _blink;
        private BaseShuttleView _shuttle;
        private BaseDockView _docks;
        private Light _fire;

        /// <summary>A display copy has no health to follow: it only turns and glows, its bays shut.</summary>
        public static GameObject BuildDisplay(Transform parent, BaseArtDefinition art)
        {
            var root = new GameObject("BaseModel");
            root.transform.SetParent(parent, false);
            ArtModel model = ArtModel.CreateStill(root.transform, art.Model);
            AddTurning(model, art);
            BaseDockView.CloseLids(art, model);
            return root;
        }

        public void Initialize(BaseArtDefinition art)
        {
            _art = art;
            _model = ArtModel.Create(transform, art.Model, ownMaterial: true);
            AddTurning(_model, art);
            AddBeacon();
            AddShuttle();
            _docks = gameObject.AddComponent<BaseDockView>();
            _docks.Initialize(art, _model);

            // The crystal breathes on its own copy of the material, so the rest of the base keeps its glow steady.
            Renderer[] crystal = _model.RenderersEndingWith(art.PulsingModule);
            if (crystal.Length > 0)
            {
                _crystal = new Material(_model.Material) { name = art.PulsingModule };
                foreach (Renderer renderer in crystal)
                {
                    renderer.sharedMaterial = _crystal;
                }

                gameObject.AddComponent<EmissionPulse>().Initialize(new[] { _crystal }, CrystalPulseDepth, CrystalPulseSpeed);
            }

            if (art.Damage != null)
            {
                _damage = ArtModel.Create(transform, art.Damage, _model.Material);
                _damage.SetActive(false);
            }

            BuildParticles();
        }

        /// <summary>The socket of that name on the model (a light, smoke or spark point), or null.</summary>
        public Transform Socket(string name) => _model?.Find(name);

        /// <summary>Follows the level's base health and its drones from now on.</summary>
        public void Bind(Health health, GameEvent<Drone> droneLaunched)
        {
            _health = health;
            _health.Changed += OnChanged;
            _health.Died += OnDied;
            _docks.Bind(droneLaunched);
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Changed -= OnChanged;
                _health.Died -= OnDied;
            }

            _model?.Dispose();
            _damage?.Dispose();
            _wreck?.Dispose();
            if (_crystal != null)
            {
                Destroy(_crystal);
            }

            if (_beacon != null)
            {
                Destroy(_beacon);
            }
        }

        // The beacon on the radar mast blinks on its own copy of the material, like the crystal breathes on its.
        private void AddBeacon()
        {
            Renderer[] beacon = string.IsNullOrEmpty(_art.BeaconModule) ? new Renderer[0] : _model.RenderersEndingWith(_art.BeaconModule);
            if (beacon.Length == 0)
            {
                return;
            }

            _beacon = new Material(_model.Material) { name = _art.BeaconModule };
            foreach (Renderer renderer in beacon)
            {
                renderer.sharedMaterial = _beacon;
            }

            _blink = gameObject.AddComponent<EmissionBlink>();
            _blink.Initialize(_beacon, _art.BeaconPeriod, _art.BeaconLitShare, _art.BeaconLitGlow, _art.BeaconDimGlow);
        }

        private void AddShuttle()
        {
            Renderer[] shuttle = string.IsNullOrEmpty(_art.ShuttleModule) ? new Renderer[0] : _model.RenderersEndingWith(_art.ShuttleModule);
            if (shuttle.Length == 0)
            {
                return;
            }

            var parts = new Transform[shuttle.Length];
            for (int i = 0; i < shuttle.Length; i++)
            {
                parts[i] = shuttle[i].transform;
            }

            _shuttle = gameObject.AddComponent<BaseShuttleView>();
            var lifts = new List<Transform>(ShuttleLiftSockets.Length);
            foreach (string socket in ShuttleLiftSockets)
            {
                Transform lift = _model.Find(socket);
                if (lift != null)
                {
                    lifts.Add(lift);
                }
            }

            _shuttle.Initialize(_art, parts, _model.Find(_art.ExhaustSocket), lifts.ToArray());
        }

        private static void AddTurning(ArtModel model, BaseArtDefinition art)
        {
            foreach (Renderer renderer in model.RenderersEndingWith(art.TurningModule))
            {
                // The FBX axis conversion sits in the part's own rotation: it turns around world up (Remco: horizontal only).
                renderer.gameObject.AddComponent<Spinner>().Initialize(art.TurnDegreesPerSecond, true);
            }
        }

        private void BuildParticles()
        {
            var smoke = new List<ParticleSystem>(3);
            foreach (Transform node in _model.Root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name.StartsWith(SmokePrefix))
                {
                    smoke.Add(ParticleKit.Create(node, "Smoke", Vector3.zero, SmokeColor, 1.2f, 2.4f, 0.2f, 0.5f, 0.3f, 0.7f, -0.12f, 30, 0.15f));
                }
            }

            _smoke = smoke.ToArray();
            _sparkSockets = _model.Numbered(SparkPrefix);
            _sparks = ParticleKit.Create(transform, "Sparks", Vector3.zero, SparkColor, 0.3f, 0.7f, 0.03f, 0.06f, 2f, 5f, 1.5f, 60, 0.1f);
        }

        // Smoke thickens as the base weakens; below the damage line the overlay shows its torn plates.
        private void OnChanged(float _)
        {
            float fraction = _health.Fraction;
            bool damaged = fraction < _art.DamagedBelow && !_health.IsDead;
            if (damaged != _damaged)
            {
                _damaged = damaged;
                _damage?.SetActive(damaged);
            }

            float hurt = _health.IsDead ? 0f : Mathf.Clamp01(1f - fraction / _art.DamagedBelow);
            foreach (ParticleSystem system in _smoke)
            {
                ParticleKit.SetRate(system, hurt * SmokeRate);
            }
        }

        // The lights go out at once: the beacon dims, the shuttle drops back onto its pad and the bays stop.
        private void OnDied()
        {
            if (_blink != null)
            {
                _blink.enabled = false;
            }

            _shuttle?.Stop();
            _docks.enabled = false;
            StartCoroutine(BecomeWreck());
        }

        // The blast hides the swap: a moment after the flash the modules give way to the wreck.
        private IEnumerator BecomeWreck()
        {
            yield return new WaitForSeconds(_art.WreckDelay);
            _damaged = false;
            _model.SetActive(false);
            _damage?.SetActive(false);
            if (_art.Wreck != null)
            {
                _wreck = ArtModel.Create(transform, _art.Wreck, ownMaterial: true);
                KeepBurning();
            }
        }

        // The fallen base keeps burning so it reads at the play zoom: its plates a little lighter, its painted embers hotter, a
        // warm rim from the fire on every edge, a flickering fire light and embers and smoke rising from the footprint.
        private void KeepBurning()
        {
            Material material = _wreck.Material;
            Color fire = _art.WreckFireColor;
            material.SetColor(BaseColorId, material.GetColor(BaseColorId) * _art.WreckLift);
            material.SetColor(EmissionColorId, material.GetColor(EmissionColorId) * _art.WreckGlow);
            material.SetColor(RimColorId, fire);
            material.SetFloat(RimStrengthId, _art.WreckRim);

            _fire = new GameObject("WreckFire").AddComponent<Light>();
            _fire.transform.SetParent(transform, false);
            _fire.transform.localPosition = Vector3.up * _art.WreckFireHeight;
            _fire.type = LightType.Point;
            _fire.color = fire;
            _fire.range = _art.WreckFireRange;
            _fire.intensity = _art.WreckFireIntensity;
            _fire.shadows = LightShadows.None;

            Vector2 min = _art.FootprintMin;
            Vector2 max = _art.FootprintMax;
            Color ember = fire * EmberGlow;
            ember.a = 1f;
            for (int i = 0; i < WreckFireSpots.Length; i++)
            {
                Vector2 share = WreckFireSpots[i];
                var spot = new Vector3(Mathf.Lerp(min.x, max.x, share.x), WreckFireLift, Mathf.Lerp(min.y, max.y, share.y));
                ParticleSystem embers = ParticleKit.UseCell(ParticleKit.Create(transform, "WreckEmbers", spot, ember, 1.2f, 2.4f, 0.04f, 0.09f, 0.3f, 0.9f, -0.08f, 60, 0.45f), MarkerArt.ParticleCell.Ember);
                ParticleKit.SetRate(embers, _art.WreckEmberRate);
                ParticleSystem smoke = ParticleKit.Create(transform, "WreckSmoke", spot, WreckSmokeColor, 2.5f, 4f, 0.5f, 1.1f, 0.3f, 0.6f, -0.1f, 30, 0.35f);
                ParticleKit.SetRate(smoke, _art.WreckSmokeRate);
            }
        }

        private void Update()
        {
            if (_fire != null)
            {
                _fire.intensity = _art.WreckFireIntensity * (1f - FlickerDepth + FlickerDepth * Mathf.PerlinNoise(Time.time * FlickerSpeed, 0f) * 2f);
            }

            if (!_damaged || _sparkSockets.Length == 0)
            {
                return;
            }

            _sparkClock -= Time.deltaTime;
            if (_sparkClock > 0f)
            {
                return;
            }

            _sparkClock = SparkInterval * Random.Range(0.5f, 1.5f);
            var spark = new ParticleSystem.EmitParams { position = _sparkSockets[Random.Range(0, _sparkSockets.Length)].position, applyShapeToPosition = false };
            _sparks.Emit(spark, SparkBurst);
        }
    }
}
