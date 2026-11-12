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
    /// lights dying and gets back up, flies to another gate on his jetpack (take-off, flight, landing timed to the post,
    /// the jets burning from ignition to thrust-off) and blinks behind his visor. Draws and stows play their own clips
    /// with the weapon changing socket at the grab or release. The spine turns the last bit towards a target he aims at. Reacts to Commander
    /// events and getters only; <see cref="HeroView"/> keeps the beacon, the rings and the health bar.
    /// </summary>
    public sealed class HeroArtView : MonoBehaviour
    {
        private const float TurnDegreesPerSecond = 260f;
        private const float TurnWalkingDegreesPerSecond = 540f;
        private const float IgniteSeconds = 0.18f;
        private const float SpeedSettle = 10f;
        private const float MovingSpeed = 0.2f;
        private const float AimMaxDegrees = 60f;
        private const float AimFollowSpeed = 10f;
        private const float AimHeight = 0.4f;
        // README round 35: after the Startled flinch he holds the aim on the boss this long (game seconds), then lowers the
        // gun and holsters it; a bark may only point while nothing else plays on his upper body.
        private const float StartledHoldSeconds = 2.2f;

        // A slash starts this long before its hit frame and a shockwave before its impact frame, so the blow lands just
        // after the damage is dealt (the logic hits at once).
        private const float SlashLeadSeconds = 0.12f;
        private const float AbilityLeadSeconds = 0.1f;
        private const float HitIntervalSeconds = 0.7f;
        private const float ReloadRateMin = 0.6f;
        private const float ReloadRateMax = 2f;
        private const float BoltSeconds = 0.09f;
        private const float BoltWidth = 0.06f;
        // The soft glow where the bolt strikes, as a multiple of the bolt's width.
        private const float BoltImpactFlareScale = 5f;
        // The muzzle glow: this many bolt widths across on the shot frame (the flash), shrinking to the steady size while the bolt
        // is lit; both overlap the bolt's first stretch, so the bolt grows out of the glow.
        private const float BoltFlashScale = 9f;
        private const float BoltSourceFlareScale = 5f;
        private const float BoltFlashStrength = 1.4f;
        private const float BoltSourceFlareStrength = 0.9f;
        private const float BoltImpactFlareStrength = 1.2f;
        private const float BoltImpactFadeFloor = 0.6f;
        // How fast the gun arm swings onto the target (share of the full correction per second) and how many passes lay the
        // barrel on it: turning the arm at the shoulder also moves the muzzle, so a second pass takes up what the first left.
        private const float GunAimSpeed = 6f;
        private const int GunAimPasses = 2;
        // Round 57: repeated fire holds the aim (the Shoot clip's first frame) and kicks the gun arm up on top of it per shot (RecoilKick),
        // instead of restarting the Shoot clip, whose one-frame kick snapped the pose back to the aim and up again every shot. The
        // authored kick lifts the arm about 24 degrees; the shoulder alone carries this one. The last few shots add up.
        private const float RecoilDegrees = 16f;
        private const float RecoilPeakSeconds = 0.06f;
        private const float RecoilMaxShare = 1.5f;
        private const int RecoilShots = 3;
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
        private const float FramesPerSecond = 30f;
        // Spencer pass 2: after Lower_Weapon the stow picks up at its 'lower' frame, where the gun already points down.
        private const float StowAfterLowerFrame = 4f;
        // A new order in the air that pushes the touchdown this far back turns a landing into flight again.
        private const float RelandMargin = 0.2f;
        private const float BlinkClosed = 100f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color SparkColor = new Color(1f, 0.8f, 0.4f);
        private static readonly Color SmokeColor = new Color(0.5f, 0.48f, 0.46f, 0.95f);
        private static readonly Color DustColor = new Color(0.5f, 0.45f, 0.4f, 0.5f);
        private static readonly Color ThrusterColor = new Color(0.5f, 0.9f, 1f);

        // A boss's first entrance: the flinch, the held aim, lowering the gun.
        private enum Reaction
        {
            None,
            Startled,
            Holding,
            Lowering,
        }

        private enum Hand
        {
            None,
            Melee,
            Ranged,
        }

        // A jetpack flight to another gate: on the spot until liftoff, the flight cycle, the landing timed to touch down at the post.
        private enum Flight
        {
            None,
            Takeoff,
            Cruise,
            Landing,
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
        private Transform _gunArm;
        private float _gunAim;
        private BeamFlare _boltSource;
        private BeamFlare _boltImpact;
        private ArtModel _pistol;
        private ArtModel _hilt;
        private ArtModel _blade;
        private Material _bladeMaterial;
        private Color _bladeColour;
        private Transform _muzzle;
        private LineRenderer _bolt;

        // The painted flash of the effect library (the pulse weapons' row), or null: then the glowing sphere stands in.
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
        private int _startled;
        private int _startledHold;
        private int _pointShout;
        private int _lowerWeapon;
        private int _drawPistol;
        private int _stowPistol;
        private int _drawBlade;
        private int _stowBlade;
        private int _jetTakeoff;
        private int _jetFly;
        private int _jetLand;

        // The weapon change in progress: a draw or stow clip on the upper body, the weapon changing socket at its moment
        // (the grab, the release) and the blade lighting or going out at its second moment.
        private int _switchClip = -1;
        private float _switchAge;
        private Hand _switchTo;
        private bool _switchSeated;
        private bool _switchCued;

        private Flight _flight;
        private float _flightAge;
        private JetThrust _jets;
        private PistolGlow _pistolGlow;

        private SkinnedMeshRenderer[] _blinkers = new SkinnedMeshRenderer[0];
        private int[] _blinkShapes = new int[0];
        private float _blinkWait;
        private float _blinkAge = float.PositiveInfinity;

        private Hand _drawn = Hand.None;
        private float _combatUntil;
        private float _ignite = 1f;
        private float _boltAge = float.PositiveInfinity;
        private readonly float[] _shotTimes = RecoilKick.NoShots(RecoilShots);
        private int _nextShot;
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
        private Reaction _reaction;
        private float _reactionAge;
        private Vector3 _reactFacing;

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
            _gunArm = _body.Find(art.GunArmBone);
            BuildWeapons(hero.Definition);
            BuildParticles();
            BuildJets();
            FindBlinkers();

            _lastPosition = transform.position;
            _lastHealth = hero.Health.Current;
            _player?.Loop(_idle, 1f);
            _hero.Drawing += OnDrawing;
            _hero.Struck += OnStruck;
            _hero.Reloading += OnReloading;
            _hero.AbilityUsed += OnAbilityUsed;
            _hero.Downed += OnDowned;
            _hero.Revived += OnRevived;
            _hero.Changed += OnChanged;
            _hero.Startled += OnStartled;
            _hero.Called += OnCalled;
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Drawing -= OnDrawing;
                _hero.Struck -= OnStruck;
                _hero.Reloading -= OnReloading;
                _hero.AbilityUsed -= OnAbilityUsed;
                _hero.Downed -= OnDowned;
                _hero.Revived -= OnRevived;
                _hero.Changed -= OnChanged;
                _hero.Startled -= OnStartled;
                _hero.Called -= OnCalled;
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
            _startled = body.IndexOf(ModelClipRole.Startled);
            _startledHold = body.IndexOf(ModelClipRole.StartledHold);
            _pointShout = body.IndexOf(ModelClipRole.PointShout);
            _lowerWeapon = body.IndexOf(ModelClipRole.LowerWeapon);
            _drawPistol = body.IndexOf(ModelClipRole.DrawPistol);
            _stowPistol = body.IndexOf(ModelClipRole.StowPistol);
            _drawBlade = body.IndexOf(ModelClipRole.DrawBlade);
            _stowBlade = body.IndexOf(ModelClipRole.StowBlade);
            _jetTakeoff = body.IndexOf(ModelClipRole.JetTakeoff);
            _jetFly = body.IndexOf(ModelClipRole.JetFly);
            _jetLand = body.IndexOf(ModelClipRole.JetLand);
        }

        private void BuildJets()
        {
            Transform[] nozzles = Bones(_art.JetSockets);
            if (nozzles.Length > 0)
            {
                _jets = new JetThrust(transform, nozzles, _art);
            }
        }

        // The face meshes (near and far) that carry the blink shape; none on a model without one.
        private void FindBlinkers()
        {
            if (string.IsNullOrEmpty(_art.BlinkShape))
            {
                return;
            }

            var blinkers = new System.Collections.Generic.List<SkinnedMeshRenderer>(2);
            var shapes = new System.Collections.Generic.List<int>(2);
            foreach (Renderer renderer in _body.Renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    int shape = skinned.sharedMesh.GetBlendShapeIndex(_art.BlinkShape);
                    if (shape >= 0)
                    {
                        blinkers.Add(skinned);
                        shapes.Add(shape);
                    }
                }
            }

            _blinkers = blinkers.ToArray();
            _blinkShapes = shapes.ToArray();
            _blinkWait = Random.Range(_art.BlinkEvery.x, _art.BlinkEvery.y);
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
                _pistol.Root.localScale *= _art.PistolScale;
                Hide(_pistol, _art.HiddenParts);
                _muzzle = new GameObject("Muzzle").transform;
                _muzzle.SetParent(_pistol.Root, false);
                _muzzle.localPosition = _art.Muzzle;
                // Its +Z is the barrel: the bolt, the painted flash and the gun arm's aim all go along it.
                _muzzle.localRotation = Quaternion.LookRotation(_art.BarrelAxis, Vector3.up);
                BuildBolt(definition.RangedWeapon.Color);
                _pistolGlow = PistolGlow.Create(transform, _pistol.Root, _art, definition.RangedWeapon.Color);
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

        // Round 51: the muzzle flash is the bolt's soft source glow, swollen on the shot frame and shrinking back to the bolt's
        // own size; the painted pulse-weapon flash card showed hard-edged wedges this close to the camera.
        private void BuildBolt(Color colour)
        {
            var boltGo = new GameObject("Bolt");
            boltGo.transform.SetParent(transform, false);
            _bolt = boltGo.AddComponent<LineRenderer>();
            _bolt.useWorldSpace = true;
            _bolt.widthMultiplier = BoltWidth;
            LineKit.UseFlaredBeam(_bolt);
            _boltSource = BeamFlare.Create(transform, "BoltSource");
            _boltImpact = BeamFlare.Create(transform, "BoltImpact");
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
            _smoke = ParticleKit.UseSmoke(ParticleKit.Create(at, "Smoke", Vector3.zero, SmokeColor, 1.2f, 2.2f, 0.36f, 0.72f, 0.3f, 0.6f, -0.12f, 30, 0.15f));
            _dust = ParticleKit.UseSmoke(ParticleKit.Create(transform, "Dust", new Vector3(0f, 0.1f, 0f), DustColor, 0.5f, 0.9f, 0.2f, 0.4f, 0.8f, 1.8f, -0.05f, 40, 0.4f));
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

        // The logic takes the weapon in hand and waits its draw time before the first blow or shot, so the draw clip plays out
        // to the aim or the guard before anything fires (round 50: he shot while still drawing).
        private void OnDrawing(HeroWeaponDefinition weapon)
        {
            if (_isDown || _player == null)
            {
                return;
            }

            Draw(weapon.Kind == HeroWeaponKind.Ranged ? Hand.Ranged : Hand.Melee);
            _combatUntil = Time.time + weapon.DrawSeconds + weapon.StayDrawnSeconds;
        }

        private void OnStruck(Vector3 target, HeroWeaponDefinition weapon)
        {
            if (_isDown || _player == null)
            {
                return;
            }

            bool ranged = weapon.Kind == HeroWeaponKind.Ranged;
            Draw(ranged ? Hand.Ranged : Hand.Melee);
            _combatUntil = Time.time + weapon.StayDrawnSeconds;
            if (_switchClip >= 0)
            {
                // The draw plays out first (it ends in the aim or the guard); the logic's blow has landed already.
                return;
            }

            if (ranged)
            {
                // The gun stays at aim until the fighting is over; each shot kicks on top of it (round 57).
                HoldAim();
                _shotTimes[_nextShot] = Time.time;
                _nextShot = (_nextShot + 1) % _shotTimes.Length;
                _boltAge = 0f;
                _boltTarget = target + Vector3.up * AimHeight;
                return;
            }

            float hit = _slash >= 0 ? _body.Art.Clips[_slash].EventSeconds : 0f;
            _player.PlayUpper(_slash, Mathf.Max(0f, hit - SlashLeadSeconds));
        }

        private void OnReloading(float seconds)
        {
            if (_isDown || _player == null || _reload < 0 || _switchClip >= 0)
            {
                return;
            }

            float length = _body.Art.Clips[_reload].Length;
            float rate = seconds > 0f ? Mathf.Clamp(length / seconds, ReloadRateMin, ReloadRateMax) : 1f;
            _player.PlayUpper(_reload, 0f, hold: false, rate: rate);
            HeroWeaponDefinition gun = _hero.Definition.RangedWeapon;
            _combatUntil = Time.time + seconds + (gun != null ? gun.StayDrawnSeconds : 0f);
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

        // README round 35: turned to the boss, the pistol to hand, the flinch and the high aim; then the held aim.
        private void OnStartled(Vector3 at)
        {
            if (_isDown || _player == null || _startled < 0)
            {
                return;
            }

            Vector3 to = at - transform.position;
            to.y = 0f;
            _reactFacing = to.sqrMagnitude > 0.0001f ? to.normalized : Facing();
            // The flinch already holds the pistol at its first frame.
            Draw(Hand.Ranged, animate: false);
            _boltAge = float.PositiveInfinity;
            _player.StopUpper();
            _player.PlayOnce(_startled);
            _reaction = Reaction.Startled;
            _reactionAge = 0f;
        }

        // A bark: the point and shout on the upper body, only when nothing else plays there.
        private void OnCalled()
        {
            if (_isDown || _player == null || _pointShout < 0 || _player.Upper >= 0 || _reaction == Reaction.Startled)
            {
                return;
            }

            _player.PlayUpper(_pointShout);
        }

        // The flinch plays out into the held aim (Startled ends in Startled_Hold frame 0), the aim holds a moment, the gun
        // comes down (Lower_Weapon) and goes back into the holster at its end.
        private void React(float deltaTime)
        {
            _reactionAge += deltaTime;
            _figure.rotation = Quaternion.RotateTowards(_figure.rotation, Quaternion.LookRotation(_reactFacing), TurnWalkingDegreesPerSecond * deltaTime);
            switch (_reaction)
            {
                case Reaction.Startled:
                    _player.Loop(_startledHold >= 0 ? _startledHold : _idle, 1f);
                    if (_player.Once < 0 || _reactionAge >= _body.Art.Clips[_startled].Length)
                    {
                        _reaction = Reaction.Holding;
                        _reactionAge = 0f;
                    }

                    break;
                case Reaction.Holding:
                    _player.Loop(_startledHold >= 0 ? _startledHold : _idle, 1f);
                    if (_reactionAge >= StartledHoldSeconds)
                    {
                        _reaction = Reaction.Lowering;
                        _reactionAge = 0f;
                        if (_lowerWeapon >= 0)
                        {
                            _player.PlayOnce(_lowerWeapon);
                        }
                    }

                    break;
                default:
                    _player.Loop(_idle, 1f);
                    if (_lowerWeapon < 0 || _reactionAge >= _body.Art.Clips[_lowerWeapon].Length)
                    {
                        // The gun is down: it goes back into the holster from the stow's lowered frame.
                        Draw(Hand.None, animate: true, StowAfterLowerFrame / FramesPerSecond);
                        _reaction = Reaction.None;
                    }

                    break;
            }
        }

        /// <summary>True while the hero plays his reaction to a boss (flinch, held aim, lowering the gun).</summary>
        public bool IsReacting => _reaction != Reaction.None;

        /// <summary>A bone of his model by name (the head for the visor cut-in camera), or null.</summary>
        public Transform Bone(string name) => _body?.Find(name);

        /// <summary>Where he faces now (the figure, which turns towards a boss he reacts to).</summary>
        public Vector3 FigureForward => _figure != null ? _figure.forward : transform.forward;

        private void OnDowned()
        {
            _isDown = true;
            _reaction = Reaction.None;
            _downAge = 0f;
            _impacted = false;
            _reviveAge = float.PositiveInfinity;
            Draw(Hand.None, animate: false);
            EndFlight();
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

        // The weapon used comes to the hand; whatever else was out goes back to its holster or the pack. Animated, the
        // draw or stow clip plays on the upper body and the weapon changes socket at the clip's moment (the grab, the
        // release), where hand and socket meet in the export, so the swap never pops; the blade lights up or goes out at
        // the clip's second moment. Switching weapons puts the outgoing one away at once, so the new one is not held up.
        private void Draw(Hand hand, bool animate = true, float startSeconds = 0f)
        {
            if (hand == _drawn)
            {
                return;
            }

            FinishSwitch();
            Hand from = _drawn;
            _drawn = hand;
            if (hand != Hand.Ranged)
            {
                _boltAge = float.PositiveInfinity;
            }

            if (hand != Hand.None && from != Hand.None)
            {
                Seat(Hand.None);
                SetBlade(false);
            }

            int clip = !animate || _player == null ? -1 : hand == Hand.None ? StowClip(from) : DrawClip(hand);
            if (clip < 0)
            {
                Seat(hand);
                SetBlade(hand == Hand.Melee);
                if (hand == Hand.None)
                {
                    // A held aim lets go with the gun.
                    _player?.StopUpper();
                }

                return;
            }

            _switchClip = clip;
            _switchAge = startSeconds;
            _switchTo = hand;
            _switchSeated = false;
            _switchCued = false;
            // Round 57: the pistol draw ends in the aim and is held there until the aim takes over (TickSwitch), instead of
            // fading out to the legs' pose over its last frames and the aim fading in again after.
            _player.PlayUpper(clip, startSeconds, hold: clip == _drawPistol);
        }

        private int DrawClip(Hand hand) => hand == Hand.Ranged ? _drawPistol : _drawBlade;

        private int StowClip(Hand hand) => hand == Hand.Ranged ? _stowPistol : hand == Hand.Melee ? _stowBlade : -1;

        // The draw or stow in progress, on its own clock beside the clip's (another upper-body clip may cut the clip
        // short; the weapon still ends where it belongs).
        private void TickSwitch(float deltaTime)
        {
            if (_switchClip < 0)
            {
                return;
            }

            _switchAge += deltaTime;
            ModelClip clip = _body.Art.Clips[_switchClip];
            if (!_switchSeated && _switchAge >= clip.EventSeconds)
            {
                Seat(_switchTo);
                _switchSeated = true;
            }

            if (!_switchCued && clip.CueSeconds > 0f && _switchAge >= clip.CueSeconds)
            {
                SetBlade(_switchTo == Hand.Melee);
                _switchCued = true;
            }

            if (_switchAge < clip.Length)
            {
                return;
            }

            FinishSwitch();
            // The draw ends in the aim (Shoot frame 0): the gun stays up there while the fighting goes on.
            if (_drawn == Hand.Ranged && Time.time < _combatUntil)
            {
                // Round 57: the held draw's last frame is the aim, so the aim takes over at full weight (no dip to the legs' pose).
                HoldAim(carry: true);
            }
            else if (_drawn == Hand.Ranged && _player.Upper == _drawPistol)
            {
                _player.StopUpper();
            }
        }

        // The Shoot clip's first frame is the aim: held there (rate 0), so a shot never restarts the clip's own one-frame kick
        // (the kick is laid on top, RecoilKick). Asking again while it shows keeps its weight and its frame.
        private void HoldAim(bool carry = false) => _player.PlayUpper(_shoot, 0f, hold: true, rate: 0f, carry: carry);

        // Ends a draw or stow at once: the weapon in its socket, the blade as it should be.
        private void FinishSwitch()
        {
            if (_switchClip < 0)
            {
                return;
            }

            _switchClip = -1;
            if (!_switchSeated)
            {
                Seat(_switchTo);
            }

            if (!_switchCued)
            {
                SetBlade(_switchTo == Hand.Melee);
            }
        }

        // The weapon of the hand in the hand socket, the other ones in their holster and on the pack.
        private void Seat(Hand inHand)
        {
            if (_pistol != null)
            {
                Seat(_pistol.Root, inHand == Hand.Ranged ? _hand : _holster);
            }

            if (_hilt != null)
            {
                Seat(_hilt.Root, inHand == Hand.Melee ? _hand : _back);
            }
        }

        private void SetBlade(bool lit)
        {
            if (_blade == null || _blade.Root.gameObject.activeSelf == lit)
            {
                return;
            }

            _blade.SetActive(lit);
            _ignite = 0f;
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
            else if (_reaction != Reaction.None)
            {
                React(deltaTime);
            }
            else if (_hero.IsFlying || _flight != Flight.None)
            {
                Turn(deltaTime);
                Fly(deltaTime);
            }
            else
            {
                Turn(deltaTime);
                Locomote();
                Holster();
            }

            TickSwitch(deltaTime);
            Glow(deltaTime);
            Shot(deltaTime);
            Blink(deltaTime);
            _player.Tick(deltaTime);
            _jets?.Tick(deltaTime);
        }

        // The jetpack flight to another gate (Spencer pass 4): the take-off on the spot, the flight cycle while the
        // commander carries him over the field, and the landing started so its touchdown frame meets the post. The
        // weapons are put away at once before he lifts off; the thrust burns from the take-off's ignition to the landing's
        // thrust-off.
        private void Fly(float deltaTime)
        {
            _flightAge += deltaTime;
            ParticleKit.SetRate(_thrusters, 0f);
            switch (_flight)
            {
                case Flight.None:
                    BeginFlight();
                    break;
                case Flight.Takeoff:
                    float liftoff = _jetTakeoff >= 0 ? _body.Art.Clips[_jetTakeoff].EventSeconds : 0f;
                    if (!_hero.IsFlying && _flightAge < liftoff)
                    {
                        // Called off before he left the ground (a near post): he stays on his feet.
                        EndFlight();
                        _player.StopOnce();
                        break;
                    }

                    Ignite(_jetTakeoff);
                    if (_flightAge >= liftoff)
                    {
                        // Lifted: the flight cycle takes over underneath while the take-off plays out over it.
                        _player.Loop(_jetFly >= 0 ? _jetFly : _idle, 1f);
                    }

                    if (_jetTakeoff < 0 || _flightAge >= _body.Art.Clips[_jetTakeoff].Length)
                    {
                        _flight = Flight.Cruise;
                        _player.StopOnce();
                    }

                    LandIfDue();
                    break;
                case Flight.Cruise:
                    _player.Loop(_jetFly >= 0 ? _jetFly : _idle, 1f);
                    LandIfDue();
                    break;
                default:
                    Landing();
                    break;
            }
        }

        private void BeginFlight()
        {
            _flight = Flight.Takeoff;
            _flightAge = 0f;
            Draw(Hand.None, animate: false);
            _player.StopUpper();
            _player.PlayOnce(_jetTakeoff, 0f, hold: true);
            _jets?.Set(_jetTakeoff < 0 || _body.Art.Clips[_jetTakeoff].CueSeconds <= 0f);
        }

        // The take-off's ignition lights the jets.
        private void Ignite(int clip)
        {
            if (clip >= 0 && _flightAge >= _body.Art.Clips[clip].CueSeconds)
            {
                _jets?.Set(true);
            }
        }

        // The landing starts its touchdown-time before he reaches the post.
        private void LandIfDue()
        {
            float touchdown = _jetLand >= 0 ? _body.Art.Clips[_jetLand].EventSeconds : 0f;
            float left = _hero.FlightSecondsToTouchdown;
            if (_hero.IsAirborne && left > touchdown)
            {
                return;
            }

            if (!_hero.IsFlying || _hero.IsAirborne)
            {
                _flight = Flight.Landing;
                _flightAge = Mathf.Max(0f, touchdown - left);
                _player.PlayOnce(_jetLand, _flightAge);
            }
        }

        private void Landing()
        {
            float touchdown = _jetLand >= 0 ? _body.Art.Clips[_jetLand].EventSeconds : 0f;
            if (_hero.IsFlying && !_hero.IsAirborne)
            {
                // Sent on again from the ground: a fresh take-off.
                BeginFlight();
                return;
            }

            if (_hero.IsFlying && _hero.FlightSecondsToTouchdown > touchdown + RelandMargin)
            {
                // A new order in the air: back into the flight.
                _flight = Flight.Cruise;
                _player.StopOnce();
                return;
            }

            if (_flightAge >= touchdown)
            {
                _player.Loop(_idle, 1f);
            }

            if (_jetLand < 0 || _flightAge >= _body.Art.Clips[_jetLand].CueSeconds)
            {
                _jets?.Set(false);
            }

            if (_jetLand < 0 || _flightAge >= _body.Art.Clips[_jetLand].Length)
            {
                EndFlight();
            }
        }

        private void EndFlight()
        {
            _flight = Flight.None;
            _jets?.Set(false);
        }

        // A blink now and then; the eyes stay shut while he lies down.
        private void Blink(float deltaTime)
        {
            if (_blinkers.Length == 0)
            {
                return;
            }

            float weight = 0f;
            if (_isDown && _impacted)
            {
                weight = BlinkClosed;
            }
            else
            {
                _blinkWait -= deltaTime;
                if (_blinkWait <= 0f)
                {
                    _blinkAge = 0f;
                    _blinkWait = Random.Range(_art.BlinkEvery.x, _art.BlinkEvery.y);
                }

                if (_blinkAge < _art.BlinkSeconds)
                {
                    _blinkAge += deltaTime;
                    weight = BlinkClosed * Mathf.Sin(Mathf.Clamp01(_blinkAge / _art.BlinkSeconds) * Mathf.PI);
                }
            }

            for (int i = 0; i < _blinkers.Length; i++)
            {
                _blinkers[i].SetBlendShapeWeight(_blinkShapes[i], weight);
            }
        }

        // After the clips have posed the body: the spine turns towards the target, the gun arm lays the barrel on it, and only
        // then is the bolt drawn, from the muzzle where it is this frame.
        private void LateUpdate()
        {
            if (_player == null)
            {
                return;
            }

            if (!_isDown)
            {
                AimSpine();
                AimGun();
                Recoil();
            }

            // The light strip and the bolt follow the gun as the aim left it.
            _pistolGlow?.Tick(_pistol.Root.gameObject.activeInHierarchy);
            PlaceBolt();
        }

        private void AimSpine()
        {
            if (_aimBones.Length == 0)
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

        // Round 51: the Shoot clip holds the gun at one height whatever the target, so the barrel pointed over it while the
        // bolt ran down to the target from below the gun. The shoulder now swings the arm until the barrel points at the
        // aim point (the struck point while a bolt is lit), eased in after the draw and clamped by the art's limit.
        private void AimGun()
        {
            ITargetable target = _hero.Target;
            bool aiming = _gunArm != null && _muzzle != null && _drawn == Hand.Ranged && _switchClip < 0 && target != null && target.IsAlive;
            _gunAim = Mathf.MoveTowards(_gunAim, aiming ? 1f : 0f, GunAimSpeed * Time.deltaTime);
            if (aiming && _boltAge < BoltSeconds)
            {
                // While a bolt is lit the barrel lies on it fully, even if the shot came before the ease had settled.
                _gunAim = 1f;
            }

            if (_gunAim <= 0f || _muzzle == null || _gunArm == null)
            {
                return;
            }

            Vector3 aimPoint = _boltAge >= BoltSeconds && target != null ? target.Position + Vector3.up * AimHeight : _boltTarget;
            float limit = _art.GunArmMaxDegrees;
            for (int pass = 0; pass < GunAimPasses; pass++)
            {
                Vector3 wanted = aimPoint - _muzzle.position;
                if (wanted.sqrMagnitude < 0.0001f)
                {
                    return;
                }

                Quaternion turn = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(_muzzle.forward, wanted), limit);
                _gunArm.rotation = Quaternion.Slerp(Quaternion.identity, turn, _gunAim) * _gunArm.rotation;
            }
        }

        // Round 57: the shots' kicks lift the barrel at the shoulder, on top of the aim. Nothing on a shot's own frame and little
        // while its bolt is lit, so the bolt leaves the barrel in line; the bolt is laid after this, from the muzzle as kicked.
        private void Recoil()
        {
            if (_gunArm == null || _muzzle == null || _drawn != Hand.Ranged)
            {
                return;
            }

            float kick = Mathf.Min(RecoilKick.Sum(_shotTimes, Time.time, RecoilPeakSeconds, BoltSeconds), RecoilMaxShare);
            Vector3 lift = Vector3.Cross(_muzzle.forward, Vector3.up);
            if (kick <= 0f || lift.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _gunArm.rotation = Quaternion.AngleAxis(kick * RecoilDegrees, lift.normalized) * _gunArm.rotation;
        }

        // The bolt flicks from the muzzle as it is posed this frame to the struck point, half width at both ends under soft glows: the
        // muzzle glow is the flash on the shot frame and follows the gun's kick, so the bolt grows out of the gun; the other
        // marks where it lands.
        private void PlaceBolt()
        {
            if (_bolt == null || !_bolt.enabled)
            {
                _boltSource?.Hide();
                _boltImpact?.Hide();
                return;
            }

            LineKit.Straight(_bolt, _muzzle.position, _boltTarget, LineKit.FlaredEndWidth);
            float age = Mathf.Clamp01(_boltAge / BoltSeconds);
            float size = BoltWidth * Mathf.Lerp(BoltFlashScale, BoltSourceFlareScale, age);
            _boltSource.Show(_muzzle.position, size, _bolt.startColor, Mathf.Lerp(BoltFlashStrength, BoltSourceFlareStrength, age));
            // The bolt ends at half width under this glow, so it dims but never goes out while the bolt is lit.
            _boltImpact.Show(_boltTarget, BoltWidth * BoltImpactFlareScale, _bolt.endColor, BoltImpactFlareStrength * Mathf.Lerp(1f, BoltImpactFadeFloor, age));
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
            // The stow clip starts from the aim or the guard, so it takes over the held pose without a fade.
            if (_drawn != Hand.None && _switchClip < 0 && Time.time >= _combatUntil && (_drawn == Hand.Ranged || _player.Upper < 0))
            {
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

        // The bolt stays lit for a moment after each shot; it and its glows are laid out in LateUpdate.
        private void Shot(float deltaTime)
        {
            if (_bolt == null)
            {
                return;
            }

            _boltAge += deltaTime;
            bool lit = _boltAge < BoltSeconds;
            _bolt.enabled = lit;
        }
    }
}
