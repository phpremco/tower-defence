using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// The imported (Blender) look of a tower, driven by <see cref="TowerView"/>: the model of the current level (a
    /// tier swap on upgrade), the head turned and the barrels tilted towards the target by the rig's Yaw and Pitch bones,
    /// the barrel that fires named as the shot's origin, recoil, wind-up and firing cycles from the model's clips, rank
    /// stars hidden above the level, and the shared wreck with this tower's debris once it falls. Reacts only to what the
    /// tower tells it; changes nothing in the tower.
    /// </summary>
    public sealed class TowerArtView : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Enemies stand on the ground; the barrels aim at their middle, not their feet.
        private const float TargetAimHeight = 0.35f;

        // The firing cycle (a beam, a spinning cluster) keeps going this many fire intervals after the last shot.
        private const float EngagedIntervals = 1.6f;
        private const float EngagedGraceSeconds = 0.15f;

        // A wind-up's glow dies back this fast after the shot.
        private const float GlowRecovery = 4f;
        private const string DebrisPrefix = "Debris_";

        private Tower _tower;
        private TowerArtDefinition _art;
        private Transform _mount;
        private TowerModel _model;
        private GameObject _wreck;
        private bool _wrecked;
        private int _level;
        private int _nextMuzzle;
        private float _sinceFire = float.MaxValue / 2f;
        private float _idlePhase;
        private float _yaw;
        private float _pitch;
        private float _restYaw;
        private float _restPitch;
        private bool _charged;
        private float _glow = 1f;
        private float _afterGlow;
        private Color _restEmission;
        private int _idleClip = -1;
        private int _firingClip = -1;
        private int _chargeClip = -1;
        private bool _lowDetail;

        /// <summary>The barrel the last shot left from (for the muzzle flash), or the mount before the first shot.</summary>
        public Transform LastMuzzle { get; private set; }

        /// <summary>Where damage smoke rises, in the tower's own space: the model's Smoke socket at rest.</summary>
        public Vector3 SmokeLocal { get; private set; }

        /// <summary>Height of the model's top above the tower's ground, for the health bar.</summary>
        public float Top { get; private set; }

        public void Initialize(Tower tower, TowerArtDefinition art, Transform mount)
        {
            _tower = tower;
            _art = art;
            _mount = mount;
            _lowDetail = ModelQuality.IsLow;
            _idlePhase = Random.Range(0f, Mathf.PI * 2f);
            LastMuzzle = mount;
            _tower.Charging += OnCharging;
        }

        private void OnDestroy()
        {
            if (_tower != null)
            {
                _tower.Charging -= OnCharging;
            }

            _model?.Dispose(ownMaterial: true);
        }

        /// <summary>Wears the look of a level; a new tier replaces the old model.</summary>
        public void ShowLevel(int levelIndex)
        {
            _level = levelIndex;
            ModelArtDefinition wanted = _art.ArtFor(levelIndex);
            if (wanted == null || (_model != null && _model.Art == wanted))
            {
                return;
            }

            _model?.Dispose(ownMaterial: true);
            _model = TowerModel.Create(_mount, wanted, _art.MuzzlePrefix, _lowDetail);
            _model.Root.gameObject.SetActive(!_wrecked);
            _restEmission = _model.Material.GetColor(EmissionColorId);
            _glow = 1f;
            _nextMuzzle = 0;
            LastMuzzle = _model.Muzzles.Length > 0 ? _model.Muzzles[0] : _mount;
            Top = _mount.localPosition.y + wanted.Height * wanted.Scale;
            SmokeLocal = _model.Smoke != null ? transform.InverseTransformPoint(_model.Smoke.position) : _mount.localPosition + Vector3.up * Top * 0.5f;
            _idleClip = wanted.IndexOf(ModelClipRole.Idle);
            _firingClip = wanted.IndexOf(ModelClipRole.Firing);
            _chargeClip = wanted.IndexOf(ModelClipRole.Charge);
            _model.Player?.Loop(_idleClip, 1f);
            ApplyPose();
        }

        /// <summary>A wreck hides the tower and shows the burnt pad with this tower's debris; a rebuild swaps back.</summary>
        public void SetWrecked(bool wrecked)
        {
            if (wrecked == _wrecked)
            {
                return;
            }

            _wrecked = wrecked;
            if (_model != null)
            {
                _model.Root.gameObject.SetActive(!wrecked);
            }

            if (wrecked && _wreck == null && _art.Wreck != null)
            {
                _wreck = BuildWreck();
            }

            if (_wreck != null)
            {
                _wreck.SetActive(wrecked);
            }
        }

        /// <summary>Turns the head onto the target and names the barrel the shot leaves from.</summary>
        public Vector3 MuzzleFor(Vector3 target)
        {
            if (_model == null || _model.Muzzles.Length == 0)
            {
                return _mount.position;
            }

            AimAt(target);
            ApplyPose();
            Transform[] muzzles = _model.Muzzles;
            bool nearest = _art.MuzzleChoice == TowerMuzzleChoice.Nearest;
            int index = nearest ? Nearest(muzzles, target) : _nextMuzzle % muzzles.Length;
            _nextMuzzle = (index + 1) % muzzles.Length;
            LastMuzzle = muzzles[index];
            return LastMuzzle.position;
        }

        /// <summary>A shot left: recoil on the barrel that fired, unless a firing cycle (spin, beam) carries the motion.</summary>
        public void OnFired(Vector3 target)
        {
            _sinceFire = 0f;
            _charged = false;
            _afterGlow = _glow;
            AimAt(target);
            if (_model?.Player == null || _firingClip >= 0)
            {
                return;
            }

            int fire = _model.Art.IndexOf(ModelClipRole.Fire, LastMuzzle.name);
            _model.Player.PlayOnce(fire >= 0 ? fire : _model.Art.IndexOf(ModelClipRole.Fire));
        }

        // A beam locks on: the head swings round and the wind-up plays out over the moment before the first shot.
        private void OnCharging(Vector3 target, float seconds)
        {
            AimAt(target);
            _sinceFire = 0f;
            if (_model?.Player == null || _chargeClip < 0 || seconds <= 0f)
            {
                return;
            }

            _model.Player.PlayOnce(_chargeClip, rate: _model.Art.Clips[_chargeClip].Length / seconds);
            _charged = true;
        }

        private void Update()
        {
            if (_model == null || _wrecked)
            {
                return;
            }

            float delta = Time.deltaTime;
            _sinceFire += delta;
            UpdateIdle();
            UpdateCycles();
            UpdateGlow(delta);
            _model.Player?.Tick(delta);
        }

        // After the clips have posed the rig this frame, the aim and the stars have the last word.
        private void LateUpdate()
        {
            if (_model != null && !_wrecked)
            {
                ApplyPose();
            }
        }

        private void ApplyPose()
        {
            _model.ShowStars(_art.HideStars ? 0 : _level + 1);
            if (_art.Aim != TowerAimMode.None)
            {
                _model.Aim(_yaw, _pitch, _art.Aim == TowerAimMode.YawAndPitch);
            }
        }

        private void AimAt(Vector3 target)
        {
            if (_model == null || _art.Aim == TowerAimMode.None)
            {
                return;
            }

            Transform pivot = _model.Pitch != null ? _model.Pitch : _model.Yaw != null ? _model.Yaw : _model.Root;
            Vector3 to = target + Vector3.up * TargetAimHeight - pivot.position;
            Vector3 up = _model.Root.up;
            Vector3 flat = Vector3.ProjectOnPlane(to, up);
            if (flat.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _yaw = Vector3.SignedAngle(_model.Root.TransformDirection(_model.RestForward), flat, up);
            float elevation = Mathf.Atan2(Vector3.Dot(to, up), flat.magnitude) * Mathf.Rad2Deg;
            Vector2 limits = _art.PitchLimits;
            _pitch = Mathf.Clamp(elevation, limits.x, limits.y) - _model.RestElevation;
            _restYaw = _yaw;
            _restPitch = _pitch;
        }

        // Nothing to shoot: the head looks around where it last aimed and the barrels settle back to their rest.
        private void UpdateIdle()
        {
            if (!TowerIdleSweep.IsIdle(_sinceFire))
            {
                return;
            }

            _yaw = _restYaw + TowerIdleSweep.Degrees(_sinceFire, _idlePhase);
            _pitch = _restPitch * (1f - TowerIdleSweep.Ease(_sinceFire));
        }

        private void UpdateCycles()
        {
            ModelClipPlayer player = _model.Player;
            if (player == null)
            {
                return;
            }

            bool engaged = _sinceFire < _tower.Definition.GetLevel(_level).FireInterval * EngagedIntervals + EngagedGraceSeconds;
            if (_firingClip >= 0)
            {
                player.Loop(engaged ? _firingClip : _idleClip, 1f);
            }

            // A gun that winds up before each shot (rails drawing back, a coil charging) times the wind-up to end on it.
            bool beam = _tower.Definition.ProjectileStyle == ProjectileStyle.Laser;
            if (beam || _chargeClip < 0 || _charged || !engaged || player.Once >= 0)
            {
                return;
            }

            float length = _model.Art.Clips[_chargeClip].Length;
            float until = _tower.SecondsUntilReady;
            if (until > 0f && until <= length)
            {
                player.PlayOnce(_chargeClip, length - until);
                _charged = true;
            }
        }

        private void UpdateGlow(float delta)
        {
            if (_art.ChargeGlow <= 1f)
            {
                return;
            }

            ModelClipPlayer player = _model.Player;
            float glow;
            if (player != null && player.IsPlaying(ModelClipRole.Charge))
            {
                float progress = Mathf.Clamp01(player.OnceTime / _model.Art.Clips[_chargeClip].Length);
                glow = Mathf.Lerp(1f, _art.ChargeGlow, progress);
            }
            else
            {
                _afterGlow = Mathf.MoveTowards(_afterGlow, 1f, GlowRecovery * delta);
                glow = _afterGlow;
            }

            if (!Mathf.Approximately(glow, _glow))
            {
                _glow = glow;
                _model.Material.SetColor(EmissionColorId, _restEmission * glow);
            }
        }

        private static int Nearest(Transform[] muzzles, Vector3 target)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < muzzles.Length; i++)
            {
                float distance = (muzzles[i].position - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        // The shared wreck model holds the debris of every tower; only this tower's stays.
        private GameObject BuildWreck()
        {
            ModelArtDefinition art = _art.Wreck;
            GameObject wreck = Instantiate(art.Model, transform, false);
            wreck.name = art.name;
            wreck.transform.localScale = Vector3.one * art.Scale;
            Material material = art.MaterialFor(_lowDetail);
            string keep = _art.Debris;
            foreach (Transform part in wreck.GetComponentsInChildren<Transform>(true))
            {
                if (part.name.StartsWith(DebrisPrefix) && part.name != keep)
                {
                    part.gameObject.SetActive(false);
                }
            }

            foreach (Renderer renderer in wreck.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }

            return wreck;
        }
    }
}
