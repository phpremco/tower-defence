using Game.Combat;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// The commander in his imported (Blender) model: he turns towards where he faces, idles, walks or runs with the
    /// clip rate matched to his ground speed, draws the pistol from the thigh holster or the sword hilt from the pack
    /// for the weapon he uses (the blade lights up), shoots with a kick on the upper body over the legs, reloads,
    /// slashes, punches the shockwave into the ground, kneels to repair, flinches on hits, goes down with the suit's
    /// lights dying and gets back up. The spine turns the last bit towards a target he aims at. Reacts to Commander
    /// events and getters only; <see cref="HeroView"/> keeps the beacon, the rings and the health bar.
    /// </summary>
    public sealed class HeroArtView : MonoBehaviour
    {
        private const float TurnDegreesPerSecond = 260f;
        private const float TurnWalkingDegreesPerSecond = 540f;
        private const float CombatHoldSeconds = 4f;
        private const float IgniteSeconds = 0.18f;
        private const float SpeedSettle = 10f;
        private const float MovingSpeed = 0.2f;
        private const float AimMaxDegrees = 60f;
        private const float AimFollowSpeed = 10f;
        private const float AimHeight = 0.4f;

        // A slash starts this long before its hit frame and a shockwave before its impact frame, so the blow lands just
        // after the damage is dealt (the logic hits at once).
        private const float SlashLeadSeconds = 0.12f;
        private const float AbilityLeadSeconds = 0.1f;
        private const float HitIntervalSeconds = 0.7f;
        private const float ReloadRateMin = 0.6f;
        private const float ReloadRateMax = 2f;
        private const float BoltSeconds = 0.09f;
        private const float BoltWidth = 0.06f;
        private const float FlashSize = 0.14f;
        private const float MuzzleWhiteness = 0.4f;
        private const float FlashEmission = 5f;
        private const float BladeEmission = 3f;
        // The blade mesh is a thin core; the glow is drawn wider than it so it reads as a blade at the play zoom.
        private const float BladeGlowWidth = 2.5f;
        private const float ReviveFlashSeconds = 0.5f;
        private const float ReviveFlash = 2.5f;
        private const int ImpactSparks = 70;
        private const int ImpactDust = 24;
        private const int StaggerSparks = 25;
        private const float LyingSmokeRate = 4f;
        private const float LyingSparkRate = 1.5f;
        private const float ThrusterRate = 30f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color SparkColor = new Color(1f, 0.8f, 0.4f);
        private static readonly Color SmokeColor = new Color(0.25f, 0.24f, 0.24f, 0.4f);
        private static readonly Color DustColor = new Color(0.5f, 0.45f, 0.4f, 0.5f);
        private static readonly Color ThrusterColor = new Color(0.5f, 0.9f, 1f);

        private enum Hand
        {
            None,
            Melee,
            Ranged,
        }

        private Commander _hero;
        private HeroArtDefinition _art;
        private Transform _figure;
        private ArtModel _body;
        private ModelClipPlayer _player;
        private Material _suit;
        private Color _suitGlow;
        private Transform _hand;
        private Transform _holster;
        private Transform _back;
        private Transform[] _aimBones = new Transform[0];
        private ArtModel _pistol;
        private ArtModel _hilt;
        private ArtModel _blade;
        private Material _bladeMaterial;
        private Color _bladeColour;
        private Transform _muzzle;
        private LineRenderer _bolt;
        private Transform _flash;

        // The painted flash of the effect library (the pulse weapons' row), or null: then the glowing sphere stands in.
        private MuzzleFlashView _paintedFlash;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private ParticleSystem _dust;
        private ParticleSystem _thrusters;

        private int _idle;
        private int _walk;
        private int _run;
        private int _shoot;
        private int _reload;
        private int _slash;
        private int _ability;
        private int _repair;
        private int _hit;
        private int _down;
        private int _revive;

        private Hand _drawn = Hand.None;
        private float _combatUntil;
        private float _ignite = 1f;
        private float _boltAge = float.PositiveInfinity;
        private Vector3 _boltTarget;
        private Vector3 _lastPosition;
        private float _speed;
        private float _aimYaw;
        private float _lastHealth;
        private float _nextHit;
        private bool _isDown;
        private float _downAge;
        private bool _impacted;
        private float _reviveAge = float.PositiveInfinity;

        public void Initialize(Commander hero, HeroArtDefinition art)
        {
            _hero = hero;
            _art = art;
            _figure = new GameObject("Figure").transform;
            _figure.SetParent(transform, false);
            _figure.rotation = Quaternion.LookRotation(Facing());
            _body = ArtModel.Create(_figure, art.Body, ownMaterial: true);
            _player = _body.Player;
            _suit = _body.Material;
            _suitGlow = _suit.GetColor(EmissionColorId);
            FindClips(art.Body);
            _hand = _body.Find(art.HandSocket);
            _holster = _body.Find(art.HolsterSocket);
            _back = _body.Find(art.BackSocket);
            _aimBones = Bones(art.AimBones);
            BuildWeapons(hero.Definition);
            BuildParticles();

            _lastPosition = transform.position;
            _lastHealth = hero.Health.Current;
            _player?.Loop(_idle, 1f);
            _hero.Struck += OnStruck;
            _hero.Reloading += OnReloading;
            _hero.AbilityUsed += OnAbilityUsed;
            _hero.Downed += OnDowned;
            _hero.Revived += OnRevived;
            _hero.Changed += OnChanged;
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Struck -= OnStruck;
                _hero.Reloading -= OnReloading;
                _hero.AbilityUsed -= OnAbilityUsed;
                _hero.Downed -= OnDowned;
                _hero.Revived -= OnRevived;
                _hero.Changed -= OnChanged;
            }

            _body?.Dispose();
            if (_bladeMaterial != null)
            {
                Destroy(_bladeMaterial);
            }
        }

        /// <summary>
        /// The commander at rest for a menu, a portrait or a turntable: the body posed in its idle, the pistol in the
        /// holster and the hilt on the pack. Feet on y = 0, facing +z.
        /// </summary>
        public static GameObject BuildDisplay(Transform parent, HeroArtDefinition art, float idleSeconds)
        {
            var root = new GameObject("Figure");
            root.transform.SetParent(parent, false);
            ArtModel body = ArtModel.CreateStill(root.transform, art.Body);
            body.Sample(ModelClipRole.Idle, idleSeconds, loop: true);
            Attach(art.Pistol, body.Find(art.HolsterSocket), art.HiddenParts);
            Attach(art.SwordHilt, body.Find(art.BackSocket), art.HiddenParts);
            return root;
        }

        private static void Attach(ModelArtDefinition weapon, Transform socket, string[] hidden)
        {
            if (weapon != null && socket != null)
            {
                Hide(ArtModel.CreateStill(socket, weapon), hidden);
            }
        }

        private Vector3 Facing() => _hero.Facing.sqrMagnitude > 0.0001f ? _hero.Facing.normalized : Vector3.forward;

        private void FindClips(ModelArtDefinition body)
        {
            _idle = body.IndexOf(ModelClipRole.Idle);
            _walk = body.IndexOf(ModelClipRole.Walk);
            _run = body.IndexOf(ModelClipRole.Move);
            _shoot = body.IndexOf(ModelClipRole.Shoot);
            _reload = body.IndexOf(ModelClipRole.Reload);
            _slash = body.IndexOf(ModelClipRole.Slash);
            _ability = body.IndexOf(ModelClipRole.Ability);
            _repair = body.IndexOf(ModelClipRole.Repair);
            _hit = body.IndexOf(ModelClipRole.Hit);
            _down = body.IndexOf(ModelClipRole.Down);
            _revive = body.IndexOf(ModelClipRole.Revive);
        }

        private Transform[] Bones(string[] names)
        {
            var bones = new Transform[names.Length];
            int found = 0;
            foreach (string name in names)
            {
                Transform bone = _body.Find(name);
                if (bone != null)
                {
                    bones[found++] = bone;
                }
            }

            System.Array.Resize(ref bones, found);
            return bones;
        }

        // The weapons are modelled in their socket's grip space, so each sits at the socket's origin unturned.
        private void BuildWeapons(HeroDefinition definition)
        {
            if (_art.Pistol != null && definition.RangedWeapon != null)
            {
                _pistol = ArtModel.Create(_holster, _art.Pistol, ownMaterial: false);
                Hide(_pistol, _art.HiddenParts);
                _muzzle = new GameObject("Muzzle").transform;
                _muzzle.SetParent(_pistol.Root, false);
                _muzzle.localPosition = _art.Muzzle;
                BuildBolt(definition.RangedWeapon.Color);
            }

            if (_art.SwordHilt != null && definition.Weapon != null)
            {
                _hilt = ArtModel.Create(_back, _art.SwordHilt, ownMaterial: false);
                Hide(_hilt, _art.HiddenParts);
                if (_art.SwordBlade != null)
                {
                    _bladeColour = definition.Weapon.Color;
                    _bladeMaterial = GlowDecal.BladeMaterial(_bladeColour * BladeEmission);
                    _blade = ArtModel.Create(_hilt.Root, _art.SwordBlade, _bladeMaterial);
                    Hide(_blade, _art.HiddenParts);
                    foreach (Renderer renderer in _blade.Renderers)
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    }

                    _blade.SetActive(false);
                }
            }
        }

        private static void Hide(ArtModel model, string[] parts)
        {
            foreach (Renderer renderer in model.Renderers)
            {
                if (System.Array.IndexOf(parts, renderer.name) >= 0)
                {
                    renderer.enabled = false;
                }
            }
        }

        // The pistol fires pulse bolts, so it wears the pulse weapons' painted flash in its own colour.
        private MuzzleFlashView PaintedFlash(Color colour)
        {
            if (!Services.TryGet(out VfxLibrary library))
            {
                return null;
            }

            var view = new GameObject("MuzzleFlash").AddComponent<MuzzleFlashView>();
            view.transform.SetParent(transform, false);
            if (view.Initialize(library.WeaponFor(ProjectileStyle.Bolt), Color.Lerp(colour, Color.white, MuzzleWhiteness), ModelQuality.IsLow, Camera.main))
            {
                return view;
            }

            Destroy(view.gameObject);
            return null;
        }

        private void BuildBolt(Color colour)
        {
            _paintedFlash = PaintedFlash(colour);
            if (_paintedFlash == null)
            {
                _flash = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(colour, Color.white, 0.5f), _muzzle, Vector3.zero, Vector3.one * FlashSize, FlashEmission).transform;
                _flash.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                _flash.gameObject.SetActive(false);
            }

            var boltGo = new GameObject("Bolt");
            boltGo.transform.SetParent(transform, false);
            _bolt = boltGo.AddComponent<LineRenderer>();
            _bolt.positionCount = 2;
            _bolt.useWorldSpace = true;
            _bolt.widthMultiplier = BoltWidth;
            _bolt.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            _bolt.startColor = Color.Lerp(colour, Color.white, 0.6f);
            _bolt.endColor = colour;
            _bolt.shadowCastingMode = ShadowCastingMode.Off;
            _bolt.enabled = false;
        }

        private void BuildParticles()
        {
            Transform pack = _body.Find(_art.PackBone);
            Transform at = pack != null ? pack : _figure;
            _sparks = ParticleKit.Create(at, "Sparks", Vector3.zero, SparkColor * 2f, 0.3f, 0.7f, 0.025f, 0.06f, 1.5f, 4f, 1.2f, 100, 0.15f);
            _smoke = ParticleKit.Create(at, "Smoke", Vector3.zero, SmokeColor, 1.2f, 2.2f, 0.2f, 0.4f, 0.3f, 0.6f, -0.12f, 30, 0.15f);
            _dust = ParticleKit.Create(transform, "Dust", new Vector3(0f, 0.1f, 0f), DustColor, 0.5f, 0.9f, 0.2f, 0.4f, 0.8f, 1.8f, -0.05f, 40, 0.4f);
            _thrusters = ParticleKit.Create(at, "Thrusters", Vector3.zero, ThrusterColor * 2f, 0.12f, 0.25f, 0.04f, 0.08f, 1.2f, 2f, 0f, 30, 0.06f);
        }

        private void OnChanged()
        {
            // A downed hero sent to another post appears there already lying down.
            if (_hero.IsDown && !_isDown)
            {
                OnDowned();
            }

            float health = _hero.Health.Current;
            if (health < _lastHealth && !_isDown && Time.time >= _nextHit && _drawn != Hand.Ranged && _player != null && _player.Once < 0 && _player.Upper < 0)
            {
                _player.PlayUpper(_hit);
                _nextHit = Time.time + HitIntervalSeconds;
            }

            _lastHealth = health;
        }

        private void OnStruck(Vector3 target, HeroWeaponDefinition weapon)
        {
            if (_isDown || _player == null)
            {
                return;
            }

            bool ranged = weapon.Kind == HeroWeaponKind.Ranged;
            Draw(ranged ? Hand.Ranged : Hand.Melee);
            _combatUntil = Time.time + CombatHoldSeconds;
            if (ranged)
            {
                // The gun stays at aim on the last frame of the kick until the fighting is over.
                _player.PlayUpper(_shoot, 0f, hold: true);
                _boltAge = 0f;
                _boltTarget = target + Vector3.up * AimHeight;
                _paintedFlash?.Flash(_muzzle, Vector3.forward);
                return;
            }

            float hit = _slash >= 0 ? _body.Art.Clips[_slash].EventSeconds : 0f;
            _player.PlayUpper(_slash, Mathf.Max(0f, hit - SlashLeadSeconds));
        }

        private void OnReloading(float seconds)
        {
            if (_isDown || _player == null || _reload < 0)
            {
                return;
            }

            float length = _body.Art.Clips[_reload].Length;
            float rate = seconds > 0f ? Mathf.Clamp(length / seconds, ReloadRateMin, ReloadRateMax) : 1f;
            _player.PlayUpper(_reload, 0f, hold: false, rate: rate);
            _combatUntil = Time.time + Mathf.Max(seconds, CombatHoldSeconds);
        }

        private void OnAbilityUsed(int slot, HeroAbilityDefinition ability, Vector3 position)
        {
            if (_isDown || _player == null)
            {
                return;
            }

            if (ability.Kind == HeroAbilityKind.Shockwave)
            {
                float impact = _ability >= 0 ? _body.Art.Clips[_ability].EventSeconds : 0f;
                _player.StopUpper();
                _player.PlayOnce(_ability, Mathf.Max(0f, impact - AbilityLeadSeconds));
                return;
            }

            _player.StopUpper();
            _player.PlayOnce(_repair);
        }

        private void OnDowned()
        {
            _isDown = true;
            _downAge = 0f;
            _impacted = false;
            _reviveAge = float.PositiveInfinity;
            Draw(Hand.None);
            _player?.StopUpper();
            _player?.PlayOnce(_down, 0f, hold: true);
            _sparks.Emit(StaggerSparks);
        }

        private void OnRevived()
        {
            _isDown = false;
            _reviveAge = 0f;
            _player?.PlayOnce(_revive);
            ParticleKit.SetRate(_smoke, 0f);
            ParticleKit.SetRate(_sparks, 0f);
        }

        // The weapon used comes to the hand; whatever else was out goes back to its holster or the pack.
        private void Draw(Hand hand)
        {
            if (hand == _drawn)
            {
                return;
            }

            _drawn = hand;
            if (_pistol != null)
            {
                Seat(_pistol.Root, hand == Hand.Ranged ? _hand : _holster);
            }

            if (_hilt != null)
            {
                Seat(_hilt.Root, hand == Hand.Melee ? _hand : _back);
            }

            if (_blade != null)
            {
                _blade.SetActive(hand == Hand.Melee);
                _ignite = 0f;
            }

            if (hand != Hand.Ranged)
            {
                _boltAge = float.PositiveInfinity;
            }
        }

        private static void Seat(Transform weapon, Transform socket)
        {
            if (socket == null)
            {
                return;
            }

            weapon.SetParent(socket, false);
            weapon.localPosition = Vector3.zero;
            weapon.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            TrackSpeed(deltaTime);
            if (_isDown)
            {
                Fall(deltaTime);
            }
            else
            {
                Turn(deltaTime);
                Locomote();
                Holster();
            }

            Glow(deltaTime);
            Shot(deltaTime);
            _player.Tick(deltaTime);
        }

        private void LateUpdate()
        {
            if (_isDown || _aimBones.Length == 0)
            {
                return;
            }

            // The aim's last few degrees come from the spine, on top of the clips (which rewrite the bones every frame).
            float wanted = 0f;
            ITargetable target = _hero.Target;
            if (_drawn == Hand.Ranged && target != null && target.IsAlive)
            {
                Vector3 to = target.Position - _figure.position;
                to.y = 0f;
                wanted = to.sqrMagnitude > 0.0001f ? Mathf.Clamp(Vector3.SignedAngle(_figure.forward, to, Vector3.up), -AimMaxDegrees, AimMaxDegrees) : 0f;
            }

            _aimYaw = Mathf.MoveTowards(_aimYaw, wanted, AimFollowSpeed * AimMaxDegrees * Time.deltaTime);
            if (Mathf.Abs(_aimYaw) < 0.01f)
            {
                return;
            }

            Quaternion share = Quaternion.AngleAxis(_aimYaw / _aimBones.Length, Vector3.up);
            foreach (Transform bone in _aimBones)
            {
                bone.rotation = share * bone.rotation;
            }
        }

        private void TrackSpeed(float deltaTime)
        {
            Vector3 position = transform.position;
            float moved = Vector3.Distance(position, _lastPosition);
            _lastPosition = position;
            if (deltaTime <= 0f)
            {
                return;
            }

            _speed = Mathf.Lerp(_speed, moved / deltaTime, 1f - Mathf.Exp(-SpeedSettle * deltaTime));
        }

        private void Turn(float deltaTime)
        {
            float rate = _hero.IsWalking ? TurnWalkingDegreesPerSecond : TurnDegreesPerSecond;
            _figure.rotation = Quaternion.RotateTowards(_figure.rotation, Quaternion.LookRotation(Facing()), rate * deltaTime);
        }

        // Idle on the spot, walk a few slow steps, run between posts; the clip rate keeps the feet on the ground.
        private void Locomote()
        {
            bool moving = _hero.IsWalking && _speed > MovingSpeed;
            ParticleKit.SetRate(_thrusters, moving ? ThrusterRate : 0f);
            if (!moving)
            {
                _player.Loop(_idle, 1f);
                return;
            }

            int cycle = _speed < _art.WalkBelow && _walk >= 0 ? _walk : _run;
            ModelClip clip = _body.Art.Clips[cycle];
            float rate = clip.DistancePerLoop > 0f ? _speed * clip.Length / clip.DistancePerLoop : 1f;
            _player.Loop(cycle, rate);
        }

        private void Holster()
        {
            // A held aim lets go when the fighting is over; a cut or a reload first plays to its end.
            if (_drawn != Hand.None && Time.time >= _combatUntil && (_drawn == Hand.Ranged || _player.Upper < 0))
            {
                _player.StopUpper();
                Draw(Hand.None);
            }
        }

        // The stagger and the fall come from the Down clip; the suit's lights stutter out as he falls and die on impact.
        private void Fall(float deltaTime)
        {
            _downAge += deltaTime;
            float ground = _down >= 0 ? _body.Art.Clips[_down].EventSeconds : 0f;
            if (!_impacted && _downAge >= ground)
            {
                _impacted = true;
                _sparks.Emit(ImpactSparks);
                _dust.Emit(ImpactDust);
                ParticleKit.SetRate(_smoke, LyingSmokeRate);
                ParticleKit.SetRate(_sparks, LyingSparkRate);
            }
        }

        private void Glow(float deltaTime)
        {
            float glow = 1f;
            if (_isDown)
            {
                float ground = _down >= 0 ? Mathf.Max(0.01f, _body.Art.Clips[_down].EventSeconds) : 0.01f;
                float flicker = Mathf.PerlinNoise(Time.time * 30f, 0.3f) > 0.45f * (_downAge / ground) ? 1f : 0.2f;
                glow = _impacted ? 0f : flicker * (1f - Mathf.Clamp01(_downAge / ground));
            }
            else if (_reviveAge < ReviveFlashSeconds)
            {
                _reviveAge += deltaTime;
                glow = 1f + ReviveFlash * (1f - Mathf.Clamp01(_reviveAge / ReviveFlashSeconds));
            }

            _suit.SetColor(EmissionColorId, _suitGlow * glow);

            if (_blade != null && _ignite < 1f)
            {
                _ignite = Mathf.Min(1f, _ignite + deltaTime / IgniteSeconds);
                _blade.Root.localScale = new Vector3(BladeGlowWidth, Mathf.Max(0.01f, _ignite), BladeGlowWidth);
                GlowDecal.SetColour(_bladeMaterial, _bladeColour * (BladeEmission * _ignite));
            }
        }

        // The bolt flicks from the muzzle to the target and the muzzle flashes for a moment after each shot.
        private void Shot(float deltaTime)
        {
            if (_bolt == null)
            {
                return;
            }

            _boltAge += deltaTime;
            bool lit = _boltAge < BoltSeconds;
            _bolt.enabled = lit;
            _flash?.gameObject.SetActive(lit);
            if (!lit)
            {
                return;
            }

            if (_flash != null)
            {
                _flash.localScale = Vector3.one * (FlashSize * (1.3f - _boltAge / BoltSeconds));
            }

            _bolt.SetPosition(0, _muzzle.position);
            _bolt.SetPosition(1, _boltTarget);
        }
    }
}
